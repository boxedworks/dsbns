using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{

  public ItemScript _sourceItem;

  bool _triggered;

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

  // Update is called once per frame
  void FixedUpdate()
  {
    if (_sourceItem == null || _triggered)
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
      var loudness = (_sourceItem._silenced ? EnemyScript.Loudness.SUPERSOFT : EnemyScript.Loudness.SOFT);
      foreach (var e in EnemyScript.CheckSound(transform.position, loudness, _id, false))
        e.Suspicious(_sourceRagdoll._hip.transform.position, loudness, 0.2f * Random.value);
    }
  }

  public int GetRagdollID()
  {
    return _sourceRagdoll._id;
  }

  int _penatrationAmount;
  float _initialForce;
  public void OnShot(int penatrationAmount, float force)
  {
    _penatrationAmount = penatrationAmount;
    _initialForce = force;
  }

  private void OnTriggerStay(Collider collider)
  {
    // Local function to apply ragdoll damage
    bool TakeDamage(ActiveRagdoll r)
    {

      var use_position = (_lastRagdollPosition == Vector3.zero ? _sourceItem.transform.position : _lastRagdollPosition);
      var hitForce = MathC.Get2DVector(
        -(use_position - collider.transform.position).normalized * (4000f + (Random.value * 2000f)) * (_deflected ? Mathf.Clamp(_sourceItem._hit_force * 1.5f, 0.5f, 2f) : _sourceItem._hit_force)
      );
      var pen = _penatrationAmount + 1;
      var health = r._health;
      var max = Mathf.Clamp(pen - _hitAmount, 1f, 10f);
      var damage = Mathf.Clamp(health, 1f, max);
      ActiveRagdoll damageSource = _sourceRagdoll;
      if (r._grappled && r._grappler._isPlayer) { damageSource = r._grappler; }

      // Hurt ragdoll
      if (r.TakeDamage(
        new ActiveRagdoll.RagdollDamageSource()
        {
          Source = damageSource,

          HitForce = hitForce,

          Damage = (int)damage,
          DamageSource = use_position,
          DamageSourceType = (_sourceItem._type == GameScript.ItemManager.Items.FLAMETHROWER ? ActiveRagdoll.DamageSourceType.FIRE : ActiveRagdoll.DamageSourceType.BULLET),

          SpawnBlood = _sourceItem._type != GameScript.ItemManager.Items.FLAMETHROWER,
          SpawnGiblets = _sourceItem._penatrationAmount > 1
        }
        ))
      {
        _lastRagdollPosition = r._hip.position;
        if (_sourceItem._dismember && r._health <= 0) r.Dismember(r._spine, hitForce);
        _hitAmount += (int)damage;
        if (_hitAmount <= _penatrationAmount)
        {
          if (_sourceRagdoll._playerScript?.HasPerk(Shop.Perk.PerkType.SMART_BULLETS) ?? false)
          {
            var enemy = FunctionsC.GetClosestEnemyTo(transform.position);
            if (enemy != null && enemy._ragdoll != null)
            {
              var new_vel = (enemy._ragdoll._hip.transform.position - _rb.position).normalized * _rb.velocity.magnitude;
              _rb.velocity = new_vel;
            }
          }
          return true;
        }
      }

      return false;
    }

    var hit_wall = false;
    if (!collider.name.Equals("Tile"))
    {
      if (_triggered)
        return;
      else if (collider.name.Equals("Item_Mesh") || collider.name.Equals("Powerup") || collider.name.Equals("Goal") ||
          collider.name.Equals("Barrel") || collider.name.Equals("Table") || collider.name.Equals("Button") ||
          collider.name.Contains("Candel"))
        return;
      else if (collider.transform.parent.name == "FRYING_PAN")
      {
        var item = collider.transform.parent.GetComponent<ItemScript>();
        if ((item._ragdoll?._swinging ?? false))
        {
          if (item._ragdoll._id != _sourceRagdoll._id)
            Deflect(item._ragdoll, true);
        }
        else
        {
          PlaySparks(true);

          var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_CASING_HOT)[0];
          parts.transform.position = transform.position;
          parts.Emit(1);

          item._ragdoll.Recoile(-(_sourceItem.transform.position - item.transform.position).normalized, _rb.velocity.magnitude / 15f);

          Hide();
        }
        return;
      }
      else if (collider.name.Equals("Bullet"))
      {
        // Check bullet damage
        var bullet_other = collider.GetComponent<BulletScript>();
        if (bullet_other._triggered) return;
        if (bullet_other.GetRagdollID() == GetRagdollID()) return;

        var damage_self = _penatrationAmount;
        var damage_othe = bullet_other._penatrationAmount;

        var hit_bullet = false;
        var hotbullets = 0;

        if (damage_self == damage_othe)
        {
          Hide();
          bullet_other.Hide();

          hit_bullet = true;
          hotbullets = 2;
        }

        else if (damage_self > damage_othe)
        {
          _penatrationAmount -= damage_othe;
          bullet_other.Hide();

          hit_bullet = true;
          hotbullets = 1;
        }

        else
        {
          bullet_other._penatrationAmount -= damage_self;
          Hide();

          hit_bullet = true;
          hotbullets = 1;
        }

        // Sparks
        PlaySparks(hit_bullet);

        // Hot bullets
        if (hotbullets > 0)
        {
          var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_CASING_HOT)[0];
          parts.transform.position = transform.position;
          //parts.transform.LookAt(transform.position + -transform.forward);
          parts.Emit(hotbullets);
        }

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
        if ((r._grappler?._id ?? -1) == GetRagdollID()) return;
        _lastRagdollId = r._id;
        if (_sourceItem == null || (r._id == _sourceRagdoll._id)) return;

        // If bullet hit is swinging and has a two-handed weapon (sword), reflect bullet
        if (r._swinging && r.HasBulletDeflector())
        {

#if UNITY_STANDALONE
          // Check achievement
          if (r._isPlayer && r._playerScript != null)
            SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.BAT_DEFLECT);
#endif

          // Deflect self
          Deflect(r);
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
          FunctionsC.BookManager.ExplodeBooks(collider, _sourceItem.transform.position);
          if (++_hitAmount <= _penatrationAmount) return;
        }

        else
        {
          var tv = collider.GetComponent<TVScript>();
          if (tv != null)
          {
            tv.Explode(_sourceRagdoll);
            if (++_hitAmount <= _penatrationAmount) return;
          }

          else
          {

            //Debug.Log(collider.gameObject.name);
            var s = collider.transform?.parent.GetComponent<ExplosiveScript>() ?? null;
            if (s != null)
            {
              s.Explode(_sourceRagdoll);
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
    var s = _audioSource;
    if (other_bullet)
    {
      s.volume = 0.8f;
      FunctionsC.PlaySound(ref s, "Etc/Bullet_ricochet", 0.9f, 1.1f);

      /*var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
      parts.transform.position = transform.position;
      parts.Play();*/
      var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
      parts.transform.position = transform.position;
      //parts.transform.LookAt(transform.position + -transform.forward);
      parts.Play();
    }
    else
    {
      s.volume = 0.5f;
      FunctionsC.PlaySound(ref s, "Etc/Bullet_impact", 0.9f, 1.1f);

      var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.SPARKS)[0];
      parts.transform.position = transform.position;
      parts.transform.LookAt(transform.position + -transform.forward);
      parts.Play();
    }
  }

  public void Reset(ItemScript item, Vector3 position)
  {
    _sourceItem = item;
    _sourceRagdoll = item._ragdoll;
    _id = _ID++;
    _lastRagdollId = -1;
    _hitAmount = 0;
    _lastPos = position;
    _triggered = false;
    _deflected = false;
    _p.transform.parent = transform;
    _p.transform.localPosition = new Vector3(0f, 0f, 0.5f);
    _lastRagdollPosition = Vector3.zero;

    if (item._type != GameScript.ItemManager.Items.FLAMETHROWER && item._type != GameScript.ItemManager.Items.ROCKET_FIST)
    {
      // Delay the start of playing the system to keep particles from emitting across screen
      if (_p != null)
      {
        _p.Play();
        _light.enabled = true;
      }
    }

    if (_rb != null)
      _rb.isKinematic = false;

    _startTime = Time.time;

    _distanceTraveled = 0f;
    switch (_sourceItem._type)
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

  ActiveRagdoll _sourceRagdoll;
  bool _deflected;
  void Deflect(ActiveRagdoll redirector, bool recoil = false)
  {
    _deflected = true;

    // Change direction
    var speed = rb.velocity.magnitude;
    rb.velocity = -MathC.Get2DVector(rb.position - _sourceRagdoll._hip.position).normalized * speed * 1.6f;

    // Do not hurt person who just redirected
    _sourceRagdoll = redirector;

    //
    PlayerScript._SlowmoTimer += 1.3f;

    // Refresh weapon timer if reflected bullets; use again instantly
    redirector._itemL.PlayBulletHit();
    redirector._itemL?.DeflectedBullet();
    redirector._itemR?.DeflectedBullet();

    // Sparks
    PlaySparks(true);

    // Recoil char
    if (recoil)
      redirector.Recoile(-(_sourceItem.transform.position - redirector._controller.transform.position).normalized, _rb.velocity.magnitude / 20f);
  }
}