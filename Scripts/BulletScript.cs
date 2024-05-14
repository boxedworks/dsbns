using System.Collections;
using UnityEngine;

public class BulletScript : MonoBehaviour
{

  // Source info
  ItemScript _sourceItem;
  ActiveRagdoll _sourceItemRagdoll;
  EnemyScript.Loudness _sourceLoudness;
  float _sourceHitForce;
  GameScript.ItemManager.Items _sourceType;
  bool _sourceDismember;

  //
  bool _triggered, _canDamageSource;

  Vector3 _lastPos;

  public int _id;
  public static int _ID;

  int _hitAmount, _lastRagdollId;
  Vector3 _shootPosition;

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

  //
  public const float s_BULLET_HEIGHT = -0.2397795f;

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
    size.x = 0.9f * scale;
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

  int _penatrationAmount;
  float _initialForce;
  public void OnShot(int penatrationAmount, float force)
  {
    _penatrationAmount = penatrationAmount;
    _initialForce = force;
    _shootPosition = transform.position;
  }

  public int GetPenatrationAmount(bool total)
  {
    if (total)
      return _penatrationAmount;
    return _penatrationAmount - _hitAmount;
  }

  float _saveSmartVelocity;
  private void OnTriggerStay(Collider collider)
  {

    // Local function to apply ragdoll damage
    bool TakeDamage(ActiveRagdoll r)
    {
      var hitForce = MathC.Get2DVector(
        -(_shootPosition - collider.transform.position).normalized * (4000f + (Random.value * 2000f)) * (_deflected ? Mathf.Clamp(_sourceHitForce * 1.5f, 0.5f, 2f) : _sourceHitForce)
      );
      var pen = _penatrationAmount + 1;
      var health = r._health;
      var max = Mathf.Clamp(pen - _hitAmount, 1f, 10f);
      var damage = Mathf.Clamp(health, 1f, max);
      var damageSource = _sourceDamageRagdoll;
      //if (r._grappled && r._grappler._IsPlayer)
      //  damageSource = r._grappler;

      // Hurt ragdoll
      if (r.TakeDamage(
        new ActiveRagdoll.RagdollDamageSource()
        {
          Source = damageSource,

          HitForce = hitForce,

          Damage = (int)damage,
          DamageSource = _shootPosition,
          DamageSourceType = _sourceType == GameScript.ItemManager.Items.FLAMETHROWER ? ActiveRagdoll.DamageSourceType.FIRE : ActiveRagdoll.DamageSourceType.BULLET,

          SpawnBlood = _sourceType != GameScript.ItemManager.Items.FLAMETHROWER,
          SpawnGiblets = _penatrationAmount > 1
        }
        ))
      {
        if (_sourceDismember && r._health <= 0) r.Dismember(r._spine, hitForce);
        _hitAmount += (int)damage;
        if (_hitAmount <= _penatrationAmount)
        {

          // Check smart bullets
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

              _canDamageSource = true;
            }
          }

          return true;
        }
      }

      return false;
    }

    //
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
              // Check achievements
              if (item._ragdoll._IsPlayer)
              {
                SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.BAT_DEFLECT);

                if (item._type == GameScript.ItemManager.Items.FRYING_PAN && SceneThemes._Theme._name == "Hedge")
                  SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.FRYING_PAN_RAIN);
              }
#endif
            }
            else
            {
              PlaySparks(true);
              PlayBulletEffectDropBullets(transform.position, 1);

              item._ragdoll.Recoil(-(_sourceItemRagdoll._Hip.position - item.transform.position).normalized, _rb.velocity.magnitude / 25f);
              OnHideBullet();
              Hide();
            }
          }
        }
        return;
      }

      // Projectile handler
      else if (UtilityScript.SimpleProjectileHandler(_c, collider))
      {
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
        if (r._Id == _sourceDamageRagdoll._Id && !_canDamageSource) return;

        // If bullet hit is swinging and has a two-handed weapon (sword), reflect bullet
        if (r._IsSwinging && r.HasBulletDeflector())
        {

#if UNITY_STANDALONE
          // Check achievements
          if (r._IsPlayer)
          {
            SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.BAT_DEFLECT);

            ItemScript swingingItem = (r._ItemR?._IsSwinging ?? false) ? r._ItemR : ((r._ItemL?._IsSwinging ?? false) ? r._ItemL : null);
            if ((swingingItem?._type ?? GameScript.ItemManager.Items.NONE) == GameScript.ItemManager.Items.FRYING_PAN && SceneThemes._Theme._name == "Hedge")
              SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.FRYING_PAN_RAIN);
          }
#endif

          // Deflect self
          Deflect(r._ItemL == null ? r._ItemR : r._ItemL);
          return;
        }
        if (TakeDamage(r)) return;
      }
      else
        hit_wall = true;
    }

    else
    {
      hit_wall = true;
    }

    // Wall hit sfx
    if (hit_wall)
    {

      //Debug.Log(collider.gameObject.name);

      var impactType = BulletImpactType.NORMAL;
      if (SceneThemes._Theme._name == "Hedge" || collider.gameObject.name == "BookcaseOpen_Bush" || collider.gameObject.name == "BookcaseBig_Bush")
        impactType = BulletImpactType.BUSHES;
      else if (collider.gameObject.name == "BookcaseOpen" || collider.gameObject.name == "BookcaseBig" || collider.gameObject.name == "Table_Flipped")
        impactType = BulletImpactType.WOOD;


      PlaySparks(false, 0, impactType);
    }

    // Remove bullet
    OnHideBullet();
    Hide();
  }

  //
  public Vector3 GetShootPosition()
  {
    return _shootPosition;
  }
  public ActiveRagdoll GetDamageSource()
  {
    return _sourceDamageRagdoll;
  }

  //
  public bool RecordHit()
  {
    return ++_hitAmount <= _penatrationAmount;
  }
  public void RecordHitFull()
  {
    if (++_hitAmount > _penatrationAmount)
    {
      Hide();
      OnHideBullet();
    }
  }

  //
  bool _explodeOnHide;
  public void OnHideBullet()
  {
    if (_explodeOnHide)
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
  public void PlaySparks(bool other_bullet = false, int drop_bullet_casings = 0, BulletImpactType bulletImpactType = BulletImpactType.NORMAL)
  {
    if (other_bullet)
    {
      PlayBulletEffect(0, transform.position, transform.forward, bulletImpactType);
    }
    else
    {
      PlayBulletEffect(1, transform.position, transform.forward, bulletImpactType);
    }

    // Hot bullet casings
    if (drop_bullet_casings > 0)
    {
      PlayBulletEffectDropBullets(transform.position, drop_bullet_casings);
    }
  }

  public enum BulletImpactType
  {
    NORMAL,

    WOOD,
    BUSHES,
  }
  public static void PlayBulletEffect(int effectIter, Vector3 position, Vector3 forward, BulletImpactType bulletImpactType = BulletImpactType.NORMAL)
  {
    switch (effectIter)
    {
      case 0:
        SfxManager.PlayAudioSourceSimple(position, "Etc/Bullet_ricochet");

        var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
        parts.transform.position = position;
        parts.Play();

        break;

      case 1:
        SfxManager.PlayAudioSourceSimple(position, bulletImpactType == BulletImpactType.NORMAL ? "Etc/Bullet_impact" : (bulletImpactType == BulletImpactType.BUSHES ? "Etc/Bullet_impact_bushes" : "Etc/Bullet_impact_wood"));

        parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.SPARKS)[0];
        parts.transform.position = position;
        parts.transform.LookAt(position + -forward);
        parts.Play();

        break;
    }
  }

  public static void PlayBulletEffectDropBullets(Vector3 position, int numBullets)
  {
    var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_CASING_HOT)[0];
    parts.transform.position = position;
    parts.Emit(numBullets);
  }

  public void SetSourceItem(ItemScript item)
  {
    _sourceItem = item;
    SetBulletData(
      item._ragdoll,
      _sourceItem._silenced,
      _sourceItem._hit_force,
      _sourceItem._dismember,
      _sourceItem._type
    );
  }

  public bool CanInteractWithOther(BulletScript other)
  {
    if (
      other._triggered ||
      (other._sourceType == GameScript.ItemManager.Items.FLAMETHROWER) ||
      (!_canDamageSource && !other._canDamageSource && GetRagdollID() == other.GetRagdollID())
    )
      return false;

    return true;
  }

  public void SetBulletData(
    ActiveRagdoll sourceRagdoll,
    bool silenced,
    float hitForce,
    bool dismember,
    GameScript.ItemManager.Items itemType,

    bool canDamageSource = false
  )
  {
    _sourceDamageRagdoll = _sourceItemRagdoll = sourceRagdoll;

    _sourceLoudness = silenced ? EnemyScript.Loudness.SUPERSOFT : EnemyScript.Loudness.SOFT;
    _sourceHitForce = hitForce;
    _sourceDismember = dismember;
    _sourceType = itemType;

    _canDamageSource = canDamageSource;

    // Special case
    switch (_sourceType)
    {
      case GameScript.ItemManager.Items.FLAMETHROWER:
        _maxDistance = 3.8f;
        break;

      case GameScript.ItemManager.Items.ROCKET_FIST:
        _maxDistance = 0.9f;
        break;

      default:
        _maxDistance = 0f;
        break;
    }

    // Delay the start of playing the system to keep particles from emitting across screen
    if (itemType != GameScript.ItemManager.Items.FLAMETHROWER && itemType != GameScript.ItemManager.Items.ROCKET_FIST)
    {
      if (_particles != null)
      {
        _particles.Play();
        _light.enabled = true;
      }
    }
  }

  public void Reset(ActiveRagdoll damageSource, Vector3 spawnPosition)
  {
    _id = _ID++;
    _sourceDamageRagdoll = damageSource;

    _lastRagdollId = -1;
    _hitAmount = 0;
    _lastPos = spawnPosition;
    _triggered = false;
    _deflected = false;
    _particles.transform.parent = transform;
    _particles.transform.localPosition = new Vector3(0f, 0f, 0.5f);
    _saveSmartVelocity = -1f;

    _explodeOnHide = false;

    if (_rb != null)
      _rb.isKinematic = false;

    _startTime = Time.time;

    _distanceTraveled = 0f;
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
    rb.velocity = -MathC.Get2DVector(rb.position - _sourceDamageRagdoll._Hip.position).normalized * Mathf.Clamp(speed * 1.6f, 0f, 30f);

    // Do not hurt person who just redirected
    _sourceDamageRagdoll = redirectorItem._ragdoll;
    _canDamageSource = false;

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

    // Check special
    if (redirectorItem._ragdoll._IsPlayer && redirectorItem._ragdoll._PlayerScript.HasPerk(Shop.Perk.PerkType.EXPLOSIVE_PARRY))
      _explodeOnHide = true;
  }
}