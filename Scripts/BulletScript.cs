using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{

  // Source info
  ItemScript _sourceItem;
  ActiveRagdoll _sourceItemRagdoll;
  EnemyScript.Loudness _sourceLoudness;
  float _sourceHitForce;
  GameScript.ItemManager.Items _sourceType;
  int _sourcePenetration;
  bool _sourceDismember;

  //
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

  ParticleSystem __particles;
  ParticleSystem _particles
  {
    get { if (__particles == null) __particles = transform.GetChild(0).GetComponent<ParticleSystem>(); return __particles; }
    set { __particles = value; }
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
    var module = _particles.main;
    module.startSize = 0.13f * scale;
  }

  // Update is called once per frame
  void FixedUpdate()
  {
    if (_sourceItemRagdoll == null || _triggered)
      return;

    // Check for timeout
    if (Time.time - _startTime > 3f)
    {
      Hide();
      return;
    }

    // Check max distance for special
    var dis = (_lastPos - _rb.position).magnitude;
    _distanceTraveled += dis;
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
      EnemyScript.CheckSound(transform.position, _sourceLoudness, _id, false);
    }
  }

  public int GetRagdollID()
  {
    return _sourceDamageRagdoll._Id;
  }

  int _penatrationAmount, _penatrationAmountSave;
  float _initialForce;
  public void OnShot(int penatrationAmount, float force)
  {
    _penatrationAmount = _penatrationAmountSave = penatrationAmount;
    _initialForce = force;
  }

  float _saveSmartVelocity;
  private void OnTriggerStay(Collider collider)
  {
    // Local function to apply ragdoll damage
    bool TakeDamage(ActiveRagdoll r)
    {

      var use_position = _lastRagdollPosition == Vector3.zero ? _sourceDamageRagdoll._Hip.transform.position : _lastRagdollPosition;
      var hitForce = MathC.Get2DVector(
        -(use_position - collider.transform.position).normalized * (4000f + (Random.value * 2000f)) * (_deflected ? Mathf.Clamp(_sourceHitForce * 1.5f, 0.5f, 2f) : _sourceHitForce)
      );
      var pen = _penatrationAmount + 1;
      var health = r._health;
      var max = Mathf.Clamp(pen - _hitAmount, 1f, 10f);
      var damage = Mathf.Clamp(health, 1f, max);
      ActiveRagdoll damageSource = _sourceDamageRagdoll;
      if (r._grappled && r._grappler._IsPlayer) { damageSource = r._grappler; }

      // Hurt ragdoll
      if (r.TakeDamage(
        new ActiveRagdoll.RagdollDamageSource()
        {
          Source = damageSource,

          HitForce = hitForce,

          Damage = (int)damage,
          DamageSource = use_position,
          DamageSourceType = _sourceType == GameScript.ItemManager.Items.FLAMETHROWER ? ActiveRagdoll.DamageSourceType.FIRE : ActiveRagdoll.DamageSourceType.BULLET,

          SpawnBlood = _sourceType != GameScript.ItemManager.Items.FLAMETHROWER,
          SpawnGiblets = _sourcePenetration > 1
        }
        ))
      {
        _lastRagdollPosition = r._Hip.position;
        if (_sourceDismember && r._health <= 0) r.Dismember(r._spine, hitForce);
        _hitAmount += (int)damage;
        if (_hitAmount <= _penatrationAmount)
        {
          if ((_sourceDamageRagdoll._PlayerScript?.HasPerk(Shop.Perk.PerkType.SMART_BULLETS) ?? false) && _sourceType != GameScript.ItemManager.Items.FLAMETHROWER)
          {
            var enemy = FunctionsC.GetClosestEnemyTo(transform.position, false);
            if (enemy != null && enemy._ragdoll != null)
            {
              if (_saveSmartVelocity == -1f)
                _saveSmartVelocity = _rb.velocity.magnitude;
              var new_vel = (enemy._ragdoll._Hip.transform.position - _rb.position).normalized * _saveSmartVelocity;
              _rb.velocity = new_vel;

              _startTime = Time.time;
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
        if (item._ragdoll._Id != _sourceDamageRagdoll._Id)
        {

          var logic = true;
          if (_sourceItemRagdoll._grappled)
          {
            if (_sourceDamageRagdoll._Id == _sourceItemRagdoll._Id && item._ragdoll._Id == _sourceDamageRagdoll._grappler._Id)
              logic = false;
          }
          if (logic)
          {
            if (item._ragdoll?._IsSwinging ?? false)
            {

              Deflect(item, true);

#if UNITY_STANDALONE
              // Check achievement
              if (item._ragdoll._IsPlayer && item._ragdoll._PlayerScript != null)
                SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.BAT_DEFLECT);
#endif
            }
            else
            {
              PlaySparks(true);

              var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_CASING_HOT)[0];
              parts.transform.position = transform.position;
              parts.Emit(1);

              item._ragdoll.Recoil(-(_sourceItemRagdoll._Hip.position - item.transform.position).normalized, _rb.velocity.magnitude / 25f);
              OnHideBullet();
              Hide();
            }
          }
        }
        return;
      }
      else if (collider.name.Equals("Bullet"))
      {
        // Check bullet damage
        var bullet_other = collider.GetComponent<BulletScript>();
        if (bullet_other._triggered || (bullet_other._sourceType == GameScript.ItemManager.Items.FLAMETHROWER)) return;
        if (bullet_other.GetRagdollID() == GetRagdollID()) return;

        var damage_self = _penatrationAmount;
        var damage_othe = bullet_other._penatrationAmount;

        var hit_bullet = false;
        var hotbullets = 0;

        if (damage_self == damage_othe)
        {
          Hide();
          OnHideBullet();
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
          OnHideBullet();
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
        if (r._IsDead) return;
        if (r._Id == _lastRagdollId) return;
        if ((r._grappler?._Id ?? -1) == GetRagdollID()) return;
        _lastRagdollId = r._Id;
        if (_sourceItemRagdoll == null || (r._Id == _sourceDamageRagdoll._Id)) return;

        // If bullet hit is swinging and has a two-handed weapon (sword), reflect bullet
        if (r._IsSwinging && r.HasBulletDeflector())
        {

#if UNITY_STANDALONE
          // Check achievement
          if (r._IsPlayer && r._PlayerScript != null)
            SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.BAT_DEFLECT);
#endif

          // Deflect self
          Deflect(r._ItemL == null ? r._ItemR : r._ItemL);
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
          FunctionsC.BookManager.ExplodeBooks(collider, _sourceItemRagdoll._Hip.position);
          if (++_hitAmount <= _penatrationAmount) return;
        }

        else
        {
          var tv = collider.GetComponent<TVScript>();
          if (tv != null)
          {
            tv.Explode(_sourceDamageRagdoll);
            if (++_hitAmount <= _penatrationAmount) return;
          }

          else
          {

            //Debug.Log(collider.gameObject.name);
            var s = collider.transform?.parent.GetComponent<ExplosiveScript>() ?? null;
            if (s != null)
            {
              s.Explode(_sourceDamageRagdoll);
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
    OnHideBullet();
    Hide();
  }

  //
  public void OnHideBullet()
  {
    return;
    if (_sourceType == GameScript.ItemManager.Items.CHARGE_PISTOL && _penatrationAmountSave == 1)
    {

      // Explode
      var es = gameObject.AddComponent<ExplosiveScript>();
      es._radius = 1.5f;
      es.Start();
      es.Explode(_sourceDamageRagdoll, false, true);

      GameObject.Destroy(es);
    }

  }

  //
  public void PlaySparks(bool other_bullet = false)
  {
    if (other_bullet)
    {
      SfxManager.PlayAudioSourceSimple(transform.position, "Etc/Bullet_ricochet");

      var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
      parts.transform.position = transform.position;
      parts.Play();
    }
    else
    {
      SfxManager.PlayAudioSourceSimple(transform.position, "Etc/Bullet_impact");

      var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.SPARKS)[0];
      parts.transform.position = transform.position;
      parts.transform.LookAt(transform.position + -transform.forward);
      parts.Play();
    }
  }

  public void SetSourceItem(ItemScript item)
  {
    _sourceItem = item;
    _sourceItemRagdoll = _sourceItem._ragdoll;

    _sourceLoudness = _sourceItem._silenced ? EnemyScript.Loudness.SUPERSOFT : EnemyScript.Loudness.SOFT;
    _sourceHitForce = _sourceItem._hit_force;
    _sourceDismember = _sourceItem._dismember;
    _sourceType = _sourceItem._type;
    _sourcePenetration = _sourceItem._penatrationAmount;
  }

  public void Reset(ItemScript item, Vector3 position)
  {
    SetSourceItem(item);
    _sourceDamageRagdoll = item._ragdoll;
    _id = _ID++;
    _lastRagdollId = -1;
    _hitAmount = 0;
    _lastPos = position;
    _triggered = false;
    _deflected = false;
    _particles.transform.parent = transform;
    _particles.transform.localPosition = new Vector3(0f, 0f, 0.5f);
    _lastRagdollPosition = Vector3.zero;
    _saveSmartVelocity = -1f;

    if (item._type != GameScript.ItemManager.Items.FLAMETHROWER && item._type != GameScript.ItemManager.Items.ROCKET_FIST)
    {
      // Delay the start of playing the system to keep particles from emitting across screen
      if (_particles != null)
      {
        _particles.Play();
        _light.enabled = true;
      }
    }

    if (_rb != null)
      _rb.isKinematic = false;

    _startTime = Time.time;

    _distanceTraveled = 0f;
    switch (_sourceType)
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
    var colors = _particles.colorOverLifetime.color.gradient.colorKeys;
    var alphas_original = _particles.colorOverLifetime.color.gradient.alphaKeys;
    var alphas = new GradientAlphaKey[alphas_original.Length];
    System.Array.Copy(alphas_original, 0, alphas, 0, alphas_original.Length);
    g.SetKeys(new GradientColorKey[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) }, alphas);

    var lifetime = _particles.colorOverLifetime;
    lifetime.color = g;

    _light.color = start;
  }

  public void SetLifetime(float lifetime)
  {
    var particles = _particles.main;
    particles.startLifetime = lifetime;
  }

  public void SetNoise(float strength, float frequency)
  {
    var particles = _particles.noise;
    particles.strength = strength;
    particles.frequency = frequency;
  }

  public void Hide()
  {
    _triggered = true;
    _light.enabled = false;
    if (_particles == null) return;
    GameScript._s_Singleton.StartCoroutine(LagParticles(1f));
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
    _particles.transform.parent = transform.parent;
    _particles.Stop();
    gameObject.SetActive(false);
    yield return new WaitForSeconds(time);
    _particles.transform.parent = transform;
  }

  ActiveRagdoll _sourceDamageRagdoll;
  bool _deflected;
  void Deflect(ItemScript redirectorItem, bool recoil = false)
  {
    _deflected = true;
    _startTime = Time.time;

    // Change direction
    var speed = rb.velocity.magnitude;
    rb.velocity = -MathC.Get2DVector(rb.position - _sourceDamageRagdoll._Hip.position).normalized * speed * 1.6f;

    // Do not hurt person who just redirected
    _sourceDamageRagdoll = redirectorItem._ragdoll;

    //
    PlayerScript._SlowmoTimer += 1.3f;

    // Refresh weapon timer if reflected bullets; use again instantly
    redirectorItem.PlayBulletHit();
    redirectorItem.DeflectedBullet();

    // Sparks
    PlaySparks(true);

    // Recoil char
    if (recoil)
      _sourceDamageRagdoll.Recoil(-(_sourceItemRagdoll._Hip.position - _sourceDamageRagdoll._Controller.transform.position).normalized, _rb.velocity.magnitude / 30f);
  }
}