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

  Coroutine _hidingCoroutine;
  public bool _Available { get { return _hidingCoroutine == null && _Particles.transform.parent == transform; } }

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
  public ParticleSystem _Particles
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
    var module = _Particles.main;
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
  public void OnShot(int penatrationAmount, float force, Vector3 shootPos)
  {
    _penatrationAmount = penatrationAmount;
    _initialForce = force;
    _shootPosition = shootPos;
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
            RedirectToClosestTarget(r._Hip.gameObject, true);
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

              item._ragdoll.Recoil(-(_sourceItemRagdoll._Hip.position - item.transform.position).normalized, _rb.linearVelocity.magnitude / 25f);
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

      Debug.Log(collider.gameObject.name);

      var impactType = BulletImpactType.NORMAL;
      if (collider.gameObject.name == "Door_Obstacle")
        ;//impactType = BulletImpactType.LASER;
      else if (SceneThemes._Theme._name == "Hedge" || collider.gameObject.name == "BookcaseOpen_Bush" || collider.gameObject.name == "BookcaseBig_Bush")
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
  int _lastRedirectedId, _lastLastRedirectedId;
  public bool RedirectToClosestTarget(GameObject usingGameObject, bool smartBullets)
  {
    // Don't redirect from same entity in a row
    var id = usingGameObject.GetHashCode();
    if (_lastRedirectedId == id) return false;
    _lastLastRedirectedId = _lastRedirectedId;
    _lastRedirectedId = id;

    //
    Vector3 GetLocalPosition(Vector3 position)
    {
      position.y = _rb.position.y;
      return position;
    }

    //
    var targetPosition = Vector3.zero;

    var setPosition = GetLocalPosition(usingGameObject.transform.position);
    _rb.position = setPosition;

    // Check closest enemy
    var target = FunctionsC.GetClosestTargetTo(_sourceDamageRagdoll, transform.position, -1, false);
    if (target != null && target._ragdoll != null)
      targetPosition = GetLocalPosition(target._ragdoll._Hip.position);

    // Check 2nd closest enemy
    var bulletPos = _rb.position + new Vector3(0f, 0.6f, 0f);
    var raycastinfo = new RaycastHit();
    if (targetPosition != Vector3.zero && (Physics.SphereCast(new Ray(bulletPos, MathC.Get2DVector(targetPosition - _rb.position).normalized), 0.05f, out raycastinfo, 100f, LayerMask.GetMask("ParticleCollision")) && raycastinfo.distance < target._distance))
    {
      //Debug.Log($"{raycastinfo.distance} <? {target._distance * 0.95f} [{raycastinfo.collider.name}]");

      target = FunctionsC.GetClosestTargetTo(_sourceDamageRagdoll, transform.position, target._ragdoll._Id, false);
      if (target != null && target._ragdoll != null)
        targetPosition = GetLocalPosition(target._ragdoll._Hip.position);
    }

    // Check nearest mirrors
    raycastinfo = new RaycastHit();
    if (targetPosition == Vector3.zero || (Physics.SphereCast(new Ray(bulletPos, MathC.Get2DVector(targetPosition - _rb.position).normalized), 0.05f, out raycastinfo, 100f, LayerMask.GetMask("ParticleCollision")) && raycastinfo.distance < target._distance))
    {

      var mirrors = UtilityScript.s_Utilities_Thrown.ContainsKey(UtilityScript.UtilityType.MIRROR) ? UtilityScript.s_Utilities_Thrown[UtilityScript.UtilityType.MIRROR] : null;
      if (mirrors != null && mirrors.Count > 0)
      {

        var index = -1;
        var closestVisibleMirror = -1;
        var mirrorDistance = 10000f;
        foreach (var mirror in mirrors)
        {
          index++;

          // Check for self
          if (mirror == null) continue;
          if (mirror.transform.parent.name != "Objects") continue;

          var mirrorId = mirror.gameObject.transform.GetChild(0).GetChild(0).gameObject.GetHashCode();
          if (
            mirrorId == id ||
            mirrorId == _lastRedirectedId ||
            mirrorId == _lastLastRedirectedId
          )
            continue;

          //
          var mirrorPosition = mirror.transform.GetChild(0).GetChild(0).position;
          var distance = MathC.Get2DDistance(_rb.position, mirrorPosition);
          if (distance > mirrorDistance) continue;

          //
          raycastinfo = new RaycastHit();
          var dir = (mirrorPosition - _rb.position).normalized;
          if (Physics.SphereCast(new Ray(_rb.position + dir * 0.15f, dir), 0.025f, out raycastinfo, 100f, LayerMask.GetMask("ParticleCollision")))
          {
            //Debug.DrawLine(_rb.position, raycastinfo.point, raycastinfo.distance < distance ? Color.red : Color.green, 5f);
            //Debug.Log($"{raycastinfo.distance} <? {distance}");
            if (raycastinfo.distance < distance * 0.95f) continue;
          }

          closestVisibleMirror = index;
          mirrorDistance = distance;
        }

        //
        if (closestVisibleMirror != -1)
          targetPosition = GetLocalPosition(mirrors[closestVisibleMirror].transform.GetChild(0).GetChild(0).position);
      }
    }

    //
    if (targetPosition != Vector3.zero)
    {
      if (_saveSmartVelocity == -1f)
        _saveSmartVelocity = _rb.linearVelocity.magnitude;

      var dir = (targetPosition - _rb.position).normalized;
      if (!smartBullets)
        _rb.position += dir * 0.25f;

      dir = (targetPosition - _rb.position).normalized;
      var new_vel = dir * _saveSmartVelocity;
      _rb.linearVelocity = new_vel;

      _startTime = Time.time;

      _canDamageSource = true;
    }

    return true;
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
      PlayBulletEffect(0, _rb.position, transform.forward, bulletImpactType);
    }
    else
    {
      PlayBulletEffect(1, _rb.position, transform.forward, bulletImpactType);
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

    MIRROR,

    LASER,
  }
  public static void PlayBulletEffect(int effectIter, Vector3 position, Vector3 forward, BulletImpactType bulletImpactType = BulletImpactType.NORMAL)
  {
    switch (effectIter)
    {
      case 0:
        if (bulletImpactType == BulletImpactType.MIRROR)
          SfxManager.PlayAudioSourceSimple(position, "Etc/Mirror_Reflect", 0.65f, 0.85f);
        else
          SfxManager.PlayAudioSourceSimple(position, "Etc/Bullet_ricochet");

        var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
        parts.transform.position = position;
        parts.Play();

        break;

      case 1:
        SfxManager.PlayAudioSourceSimple(position, bulletImpactType switch
        {
          BulletImpactType.WOOD => "Etc/Bullet_impact_wood",
          BulletImpactType.BUSHES => "Etc/Bullet_impact_bushes",
          //BulletImpactType.LASER => "Etc/Laser_sizzle",
          _ => "Etc/Bullet_impact"
        }, 0.9f, 1.1f, SfxManager.AudioClass.BULLET_SFX);

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
      if (_Particles != null)
      {
        _Particles.gameObject.SetActive(true);
        _Particles.Play();
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
    _Particles.transform.parent = transform;
    _Particles.transform.localPosition = new Vector3(0f, 0f, 0.5f);
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
    var colors = _Particles.colorOverLifetime.color.gradient.colorKeys;
    var alphas_original = _Particles.colorOverLifetime.color.gradient.alphaKeys;
    var alphas = new GradientAlphaKey[alphas_original.Length];
    System.Array.Copy(alphas_original, 0, alphas, 0, alphas_original.Length);
    g.SetKeys(new GradientColorKey[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) }, alphas);

    var lifetime = _Particles.colorOverLifetime;
    lifetime.color = g;

    _light.color = start;
  }

  public void SetLifetime(float lifetime)
  {
    var particles = _Particles.main;
    particles.startLifetime = lifetime;
  }

  public void SetNoise(float strength, float frequency)
  {
    var particles = _Particles.noise;
    particles.strength = strength;
    particles.frequency = frequency;
  }

  public void Hide(bool forceHide = false)
  {
    _triggered = true;
    _light.enabled = false;
    if (_Particles == null) return;
    if (forceHide)
    {
      _Particles.Stop();
      _Particles.gameObject.SetActive(false);
    }
    else
    {
      if (_hidingCoroutine != null)
        GameScript.s_Singleton.StopCoroutine(_hidingCoroutine);
      _hidingCoroutine = GameScript.s_Singleton.StartCoroutine(LagParticles(1f));
    }
  }
  public static void HideAll()
  {
    if (ItemScript._BulletPool == null) return;
    foreach (var b in ItemScript._BulletPool)
    {
      b.Hide(true);
    }
  }

  IEnumerator LagParticles(float time)
  {
    _Particles.transform.parent = transform.parent;
    _Particles.Stop();
    gameObject.SetActive(false);

    yield return new WaitForSeconds(time);

    _Particles.transform.parent = transform;
    _Particles.gameObject.SetActive(false);

    _hidingCoroutine = null;
  }

  ActiveRagdoll _sourceDamageRagdoll;
  bool _deflected;
  void Deflect(ItemScript redirectorItem, bool recoil = false)
  {
    _deflected = true;
    _startTime = Time.time;

    // Change direction
    var speed = rb.linearVelocity.magnitude;
    rb.linearVelocity = -MathC.Get2DVector(rb.position - _sourceDamageRagdoll._Hip.position).normalized * Mathf.Clamp(speed * 1.6f, 0f, 30f);

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
      _sourceDamageRagdoll.Recoil(-(_sourceItemRagdoll._Hip.position - _sourceDamageRagdoll._Controller.transform.position).normalized, _rb.linearVelocity.magnitude / 30f);

    // Check special
    if (redirectorItem._ragdoll._IsPlayer && redirectorItem._ragdoll._PlayerScript.HasPerk(Shop.Perk.PerkType.EXPLOSIVE_PARRY))
      _explodeOnHide = true;
  }
}