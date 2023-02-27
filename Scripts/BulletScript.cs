using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{

  public ItemScript _source;

  bool _triggered, _redirected;

  Vector3 _lastPos;

  public int _id;
  public static int _ID;

  int _hitAmount, _lastRagdollId;
  Vector3 _lastRagdollPosition;

  float _distanceTraveled;
  public float _maxDistance;

  Rigidbody rb;
  public Rigidbody _rb
  {
    get
    {
      if (rb == null)
        rb = GetComponent<Rigidbody>();
      return rb;
    }
  }

  BoxCollider c;
  BoxCollider _c
  {
    get { if (c == null) c = GetComponent<BoxCollider>(); return c; }
    set { c = value; }
  }

  ParticleSystem p;
  ParticleSystem _p
  {
    get { if (p == null) p = transform.GetChild(0).GetComponent<ParticleSystem>(); return p; }
    set { p = value; }
  }

  AudioSource audioSource;
  AudioSource _audioSource
  {
    get
    {
      if (audioSource == null) audioSource = _p.GetComponent<AudioSource>();
      return audioSource;
    }
    set { audioSource = value; }
  }

  float _startTime;

  new Light light;
  Light _light
  {
    get
    {
      if (light == null) light = GetComponent<Light>();
      return light;
    }
    set { light = value; }
  }

  // Use this for initialization
  void Start()
  {
    _id = _ID++;
    _lastRagdollId = -1;

    rb = GetComponent<Rigidbody>();
    _light = GetComponent<Light>();
  }

  public void SetSize(float scale)
  {
    // Set collider scale
    var size = _c.size;
    size.x = 1f * scale;
    _c.size = size;
    // Set particle system scale
    var module = _p.main;
    module.startSize = 0.13f * scale;
  }

  public Transform _target;
  // Update is called once per frame
  void FixedUpdate()
  {
    if (_source == null || _triggered)
      return;
    // Check for timeout
    if (Time.time - _startTime > 1.25f)
    {
      Hide();
      return;
    }

    // Check max distance for special
    var dis = (_lastPos - _rb.position).magnitude;
    _distanceTraveled += dis;
    //if (_maxDistance > 0f) Debug.Log($"{_lastPos} | {_rb.position} == {dis} | {_maxDistance}");
    if (_maxDistance > 0f && _distanceTraveled >= _maxDistance)
    {
      Hide();
      return;
    }

    // Check distance to enemies to alert if goes by enemy
    if (dis > 0.1f)
    {
      _lastPos = _rb.position;
      if (dis > 1.5f) return;
      var loudness = (_source._silenced ? EnemyScript.Loudness.SUPERSOFT : EnemyScript.Loudness.SOFT);
      foreach (var e in EnemyScript.CheckSound(transform.position, loudness, _id, false))
        e.Suspicious(_source._ragdoll._hip.transform.position, loudness, 0.2f * Random.value);
    }
  }
  private void Update()
  {
    // Check for start
    if (_delayStart > 0 && _p != null)
    {
      _delayStart--;

      if (_delayStart == 0)
      {
        _p.Play();
        _light.enabled = true;
      }
    }

    // Heat seeking bullet; used when reflected back by bat
    if (_rb.isKinematic && _redirected)
    {
      //if (_bounceToSourceEase < 1f)
      {
        //_bounceToSourceEase += Time.deltaTime * 5f;
        Vector3 gotopos = _source._ragdoll._hip.position;
        gotopos.y = transform.position.y;
        Vector3 dis = (transform.position - gotopos);
        //_rb.position = Vector3.Lerp(_bounceToSourceSource, gotopos, _bounceToSourceEase);
        if (dis.magnitude > 1f)
          dis = dis.normalized;
        dis *= Time.deltaTime * 40f;
        if (dis.magnitude < 0.02f) { if (_source._ragdoll != null) OnTriggerStay(_source._ragdoll._hip.GetComponent<Collider>()); Hide(); return; }
        _rb.position += -dis;
      }
      //else if (_bounceToSourceEase >= 1f) Hide();
    }
  }

  public int GetRagdollID()
  {
    return _source._ragdoll._id;
  }

  int _penatrationAmount;
  float _initialForce;
  public void OnShot(int penatrationAmount, float force)
  {
    _penatrationAmount = penatrationAmount;
    _initialForce = force;
  }

  int _delayStart;
  private void OnTriggerStay(Collider collider)
  {
    // Local function to apply ragdoll damage
    bool TakeDamage(ActiveRagdoll r)
    {

      var use_position = (_lastRagdollPosition == Vector3.zero ? _source.transform.position : _lastRagdollPosition);
      var hitForce = MathC.Get2DVector(
        -(use_position - collider.transform.position).normalized * (4000f + (Random.value * 2000f)) * (_redirected ? Mathf.Clamp(_source._hit_force * 1.5f, 0.5f, 2f) : _source._hit_force)
      );
      var pen = _penatrationAmount + 1;
      var health = r._health;
      var max = Mathf.Clamp(pen - _hitAmount, 1f, 10f);
      var damage = Mathf.Clamp(health, 1f, max);
      ActiveRagdoll damageSource = null;

      if (r._grappled) { damageSource = r._grappler; }
      else if (_redirected) { damageSource = _redirector; }
      else { damageSource = _source._ragdoll; }

      // Hurt ragdoll
      if (r.TakeDamage(damageSource, (_source._type == GameScript.ItemManager.Items.FLAMETHROWER ? ActiveRagdoll.DamageSourceType.FIRE : ActiveRagdoll.DamageSourceType.BULLET), hitForce, use_position, (int)damage, _source._type != GameScript.ItemManager.Items.FLAMETHROWER))
      {
        _lastRagdollPosition = r._hip.position;
        if (_source._dismember && r._health <= 0) r.Dismember(r._spine, hitForce);
        _hitAmount += (int)damage;
        if (_hitAmount <= _penatrationAmount)
        {
          //RedirectToOther();
          return true;
        }
      }

      return false;
    }

    // Local function to redirect to next enemy
    void RedirectToOther()
    {
      if (_source._ragdoll._isPlayer)
      {
        var enemy = FunctionsC.GetClosestEnemyTo(transform.position);
        if (enemy != null && enemy._ragdoll != null)
        {
          var new_vel = (enemy._ragdoll._hip.transform.position - _rb.position).normalized * _rb.velocity.magnitude;
          _rb.velocity = new_vel;
        }
      }
    }

    var hit_wall = false;
    if (!collider.name.Equals("Tile"))
    {
      if (_triggered)
        return;
      if (collider.name.Equals("Item_Mesh") || collider.name.Equals("Powerup") || collider.name.Equals("Goal") ||
          collider.name.Equals("Barrel") || collider.name.Equals("Table") || collider.name.Equals("Button") ||
          collider.name.Contains("Candel"))
        return;
      else if (collider.name.Equals("Bullet"))
      {
        // Check bullet damage
        var bullet_other = collider.GetComponent<BulletScript>();
        if (bullet_other._triggered) return;
        if (bullet_other._source._ragdoll._id == _source._ragdoll._id) return;

        var damage_self = _penatrationAmount;
        var damage_othe = bullet_other._penatrationAmount;

        var hit_bullet = false;

        if (damage_self == damage_othe)
        {
          Hide();
          bullet_other.Hide();

          hit_bullet = true;
        }

        else if (damage_self > damage_othe)
        {
          _penatrationAmount -= damage_othe;
          bullet_other.Hide();

          hit_bullet = true;
        }

        else
        {
          bullet_other._penatrationAmount -= damage_self;
          Hide();

          hit_bullet = true;
        }

        // Sparks
        PlaySparks(hit_bullet);

        return;
      }

      // other..
      else if (collider.gameObject.layer == 3)
      {
        collider.gameObject.GetComponent<UtilityScript>()?.Explode();
        return;
      }

      // Hurt ragdoll
      var r = ActiveRagdoll.GetRagdoll(collider.gameObject);
      if (r != null)
      {
        if (r._dead) return;
        if (r._id == _lastRagdollId) return;
        if ((r._grappler?._id ?? -1) == _source._ragdoll._id) return;
        _lastRagdollId = r._id;
        if (_source == null || (_source._ragdoll._id == r._id && !_redirected)) return;
        // If bullet hit is swinging and has a two-handed weapon (bat), reflect bullet
        if (r._swinging && r.HasTwohandedWeapon() && (_source._type != GameScript.ItemManager.Items.FLAMETHROWER || _source._type != GameScript.ItemManager.Items.ROCKET_FIST))
        {

#if UNITY_STANDALONE
          // Check achievement
          if (r._isPlayer && r._playerScript != null)
            SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.BAT_DEFLECT);
#endif

          // Deflect
          BounceToSource(true);
          _redirector = r;
          r._itemL.PlayBulletHit();
          PlayerScript._SlowmoTimer += 1.3f;
          // Refresh weapon timer if reflected bullets; use again instantly
          r._itemL?.HitSomething();
          r._itemR?.HitSomething();
          return;
        }
        if (TakeDamage(r)) return;
      }
      //else if (c.gameObject.name.Equals("Spine_Middle") || c.gameObject.name.Equals("Arm_Lower_L") || c.gameObject.name.Equals("Arm_Lower_R"))
      //  return;
      else
      {
        if (collider.gameObject.name == "Books")
        {
          FunctionsC.BookManager.ExplodeBooks(collider, _source.transform.position);
          if (++_hitAmount <= _penatrationAmount) return;
        }

        else
        {
          var tv = collider.GetComponent<TVScript>();
          if (tv != null)
          {
            var damageSource = _source._ragdoll;
            if (_redirected) { damageSource = _redirector; }
            tv.Explode(damageSource);
            if (++_hitAmount <= _penatrationAmount) return;
          }

          else
          {

            //Debug.Log(collider.gameObject.name);
            var s = collider.transform?.parent.GetComponent<ExplosiveScript>() ?? null;
            if (s != null)
            {
              s.Explode(_source._ragdoll);
              if (++_hitAmount <= _penatrationAmount) return;
            }

            else
              hit_wall = true;
          }
        }
      }
    }
    else
    {
      hit_wall = true;
    }
    // Check richochet
    if (_target != null && !_source._ragdoll._dead)
    {
      if (TakeDamage(_source._ragdoll)) return;
    }

    // Wall hit sfx
    if (hit_wall)
    {
      PlaySparks();
    }

    // Remove bullet
    Hide();
  }

  public void PlaySparks(bool other_bullet = false)
  {
    var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.SPARKS)[0];
    parts.transform.position = transform.position;
    parts.transform.LookAt(transform.position + -transform.forward);
    parts.Play();

    var s = _audioSource;
    if (other_bullet)
    {
      s.volume = 0.8f;
      FunctionsC.PlaySound(ref s, "Etc/Bullet_ricochet", 0.9f, 1.1f);
    }
    else
    {
      s.volume = 0.5f;
      FunctionsC.PlaySound(ref s, "Etc/Bullet_impact", 0.9f, 1.1f);
    }
  }

  public void Reset(ItemScript source, Vector3 position)
  {
    _source = source;
    _id = _ID++;
    _lastRagdollId = -1;
    _hitAmount = 0;
    _lastPos = position;
    _triggered = false;
    _redirected = false;
    _redirector = null;
    _p.transform.parent = transform;
    _p.transform.localPosition = new Vector3(0f, 0f, 0.5f);
    _lastRagdollPosition = Vector3.zero;

    if (source._type != GameScript.ItemManager.Items.FLAMETHROWER && source._type != GameScript.ItemManager.Items.ROCKET_FIST)
    {

      // Delay the start of playing the system to keep particles from emitting across screen
      _delayStart = 7;
    }

    if (_rb != null)
      _rb.isKinematic = false;

    _startTime = Time.time;

    _distanceTraveled = 0f;
    switch (_source._type)
    {
      case (GameScript.ItemManager.Items.FLAMETHROWER):
        _maxDistance = 3.8f;
        break;
      case (GameScript.ItemManager.Items.ROCKET_FIST):
        _maxDistance = 0.9f;
        break;
      default:
        _maxDistance = 0f;
        break;
    }

    _target = null;
  }

  public void SetColor(Color start, Color end)
  {
    var g = new Gradient();
    var colors = _p.colorOverLifetime.color.gradient.colorKeys;
    var alphas_original = _p.colorOverLifetime.color.gradient.alphaKeys;
    var alphas = new GradientAlphaKey[alphas_original.Length];
    System.Array.Copy(alphas_original, 0, alphas, 0, alphas_original.Length);
    g.SetKeys(new GradientColorKey[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) }, alphas);

    var lifetime = _p.colorOverLifetime;
    lifetime.color = g;

    _light.color = start;
  }

  public void Hide()
  {
    _triggered = true;
    _delayStart = 0;
    _light.enabled = false;
    if (_p == null) return;
    GameScript._Singleton.StartCoroutine(LagParticles(1f));
  }
  public static void HideAll()
  {
    if (ItemScript._BulletPool == null) return;
    foreach (var b in ItemScript._BulletPool)
    {
      b.Hide();
    }
  }

  IEnumerator LagParticles(float time)
  {
    _p.transform.parent = transform.parent;
    _p.Stop();
    gameObject.SetActive(false);
    yield return new WaitForSeconds(time);
    _p.transform.parent = transform;
  }

  ActiveRagdoll _redirector;
  void BounceToSource(bool speedUp = false)
  {
    // Redirect bullet
    _rb.isKinematic = true;
    //_rb.velocity = (_source._ragdoll._controller.position - _rb.position).normalized * _rb.velocity.magnitude * (speedUp ? 2.25f : 1f);
    _redirected = true;
    // Resize collider to make easier to hit source
    _c.size *= 1.25f;
    // Change target to soure
    _target = _source._ragdoll.transform;
  }
}