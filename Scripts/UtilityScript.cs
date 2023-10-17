using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityScript : ItemScript
{
  public enum UtilityType
  {
    NONE,
    GRENADE,
    GRENADE_IMPACT,
    C4,
    SHURIKEN,
    SHURIKEN_BIG,
    KUNAI_EXPLOSIVE,
    KUNAI_STICKY,

    STOP_WATCH,
    INVISIBILITY,
    DASH,

    TEMP_SHIELD,
    GRENADE_STUN,

    STICKY_GUN_BULLET,
    MORTAR_STRIKE,
    TACTICAL_BULLET
  }

  public UtilityType _utility_type;

  Rigidbody _rb;
  Collider _c;

  bool _thrown,
    _spinYAxis,
    _spin;

  int _flag;

  ExplosiveScript _explosion;

  delegate void CollisionEvent(Collision c);
  CollisionEvent _onCollisionEnter;

  System.Action<Collider> _onTriggerEnter;

  float _throwSpeed,
    _expirationTimer;

  int _customProjectileId;
  public void RegisterCustomProjectile(ItemScript source)
  {
    _customProjectileId = source._ItemId;
  }

  static Dictionary<UtilityType, List<UtilityScript>> _Utilities_Thrown;
  public static void Reset()
  {
    _Utilities_Thrown = new Dictionary<UtilityType, List<UtilityScript>>();
    foreach (var enum_ in System.Enum.GetValues(typeof(UtilityType)))
    {
      var t = (UtilityType)enum_;
      _Utilities_Thrown[t] = new List<UtilityScript>();
    }
  }

  //
  Transform _mortarAim;
  AudioSource _mortarAudioSource;

  //
  public static void Detonate_UtilitiesById(ItemScript source, UtilityType utilityType, int count = -1)
  {

    var utilities = _Utilities_Thrown[utilityType];
    var exploded = 0;
    for (var i = 0; i < utilities.Count; i++)
    {
      var utility = utilities[i];
      if (utility._customProjectileId == source._ItemId)
      {
        utility.Explode(Random.Range(0.05f, 0.15f));
        utilities.RemoveAt(i);
        i--;

        if (count != -1 && ++exploded > count - 1)
          break;
      }
    }
  }
  static public void ParentUtilitiesById(int itemIdOld, int itemIdNew, UtilityType utilityType)
  {
    foreach (var utility in _Utilities_Thrown[utilityType])
      if (utility._customProjectileId == itemIdOld)
        utility._customProjectileId = itemIdNew;
  }

  // Start is called before the first frame update
  new void Start()
  {
    _disableOnRagdollDeath = false;

    //
    if (_utility_type == UtilityType.MORTAR_STRIKE)
    {
      _mortarAim = GameObject.Instantiate(GameObject.Find("ring")).transform;
      _mortarAim.name = "MortarAim";
      GameObject.Destroy(_mortarAim.GetChild(1).gameObject);
      //_mortarAim.localScale *= 0.5f;
    }
  }

  MeshRenderer _ring;

  public Rigidbody GetRigidbody()
  {
    return _rb;
  }
  public bool Explosive()
  {
    return _explosion != null;
  }
  public bool Thrown()
  {
    return _thrown;
  }

  bool _unregister;
  Vector3 _spawnLocation, _spawnDirection;
  float _forceModifier;
  public void SetSpawnLocation(Vector3 location)
  {
    _spawnLocation = location;
  }
  public void SetSpawnDirection(Vector3 direction)
  {
    _spawnDirection = direction;
  }
  public void SetForceModifier(float mod)
  {
    _forceModifier = mod;
  }

  public void RegisterUtility(ActiveRagdoll source, bool unregister = true)
  {
    // Set ammo to 1
    _clip = 2;

    // Release to use item
    _useOnRelease = true;

    //  Set other defaults
    _spawnLocation = Vector3.zero;
    _spawnDirection = Vector3.zero;
    _forceModifier = 1f;
    _expirationTimer = 30f;
    var explosionType = ExplosiveScript.ExplosionType.AWAY;

    // Check utility-specific items
    float explosion_radius = -1f;
    _throwSpeed = 1f;
    _spinYAxis = false;
    _spin = true;
    switch (_utility_type)
    {
      case UtilityType.GRENADE:
        explosion_radius = 4f;
        _throwSpeed = 1.5f;
        break;

      case UtilityType.GRENADE_IMPACT:
        explosion_radius = 3f;
        _throwSpeed = 2f;
        break;

      case UtilityType.GRENADE_STUN:
        explosion_radius = 2f;
        _throwSpeed = 2f;
        explosionType = ExplosiveScript.ExplosionType.STUN;
        break;

      case UtilityType.TACTICAL_BULLET:
        _throwSpeed = 3f;
        _spinYAxis = true;
        break;

      case UtilityType.C4:
        explosion_radius = 3f;
        _expirationTimer = 90f;
        break;

      case UtilityType.MORTAR_STRIKE:
        explosion_radius = 2f;
        _throwSpeed = 0f;
        break;

      case UtilityType.SHURIKEN:
        _throwSpeed = 3f;
        _spinYAxis = true;
        break;

      case UtilityType.SHURIKEN_BIG:
        _throwSpeed = 2.5f;
        _spinYAxis = true;
        break;

      case UtilityType.KUNAI_EXPLOSIVE:
      case UtilityType.KUNAI_STICKY:
        _throwSpeed = 5f;
        explosion_radius = 3f;
        _spin = false;
        break;

      case UtilityType.STICKY_GUN_BULLET:
        _throwSpeed = 5f;
        explosion_radius = 1.5f;
        _spin = false;
        break;

      case UtilityType.STOP_WATCH:
        _useOnRelease = false;
        break;
      case UtilityType.INVISIBILITY:
        _useOnRelease = false;
        break;
      case UtilityType.DASH:
        _useOnRelease = false;
        break;
    }

    // If has radius, add ring and explosion
    if (explosion_radius != -1f)
    {
      _ring = GameObject.Instantiate(TileManager._Ring.gameObject).transform.GetChild(0).GetComponent<MeshRenderer>();
      _ring.transform.parent.gameObject.SetActive(true);

      _explosion = gameObject.AddComponent<ExplosiveScript>();
      _explosion._explosionType = explosionType;
      _explosion._onExplode += () =>
      {
        _c.enabled = false;
      };
    }

    // Gather components
    _rb = GetComponent<Rigidbody>();

    if (_utility_type == UtilityType.SHURIKEN_BIG)
      _c = transform.GetChild(2).GetComponent<Collider>();
    else
      _c = GetComponent<Collider>();

    // Set use
    _onUse = () =>
    {
      // Set explosion size
      if (_explosion != null && _utility_type != UtilityType.GRENADE)
      {
        if (_ragdoll._IsPlayer && _ragdoll._PlayerScript.HasPerk(Shop.Perk.PerkType.EXPLOSIONS_UP))
          explosion_radius *= 1.25f;

        var localscale = _ring.transform.localScale;
        localscale *= explosion_radius * 1.1f;
        localscale.z = 2f;
        _ring.transform.localScale = localscale;

        _explosion._radius = explosion_radius * 0.8f;
      }

      // Check invisible
      if (_ragdoll._invisible && (
        _utility_type != UtilityType.INVISIBILITY ||
        _utility_type != UtilityType.STOP_WATCH ||
        _utility_type != UtilityType.DASH ||
        _utility_type != UtilityType.TEMP_SHIELD
      ))
        _ragdoll.SetInvisible(false);

      var incrementClip = true;
      switch (_utility_type)
      {
        case UtilityType.GRENADE:

          //
          _onTriggerEnter += (Collider c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c))
              return;
          };
          _onCollisionEnter += (Collision c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c.collider))
              return;
          };

          //
          Throw();
          Unregister();
          break;

        case UtilityType.MORTAR_STRIKE:

          transform.GetChild(0).GetComponent<Renderer>().enabled = false;

          _spawnLocation = _mortarAim.position;
          _spawnLocation.y = -1.2f;
          Throw();
          Unregister();

          Explode(2f);
          PlaySound(Audio.UTILITY_EXTRA, 1f, 1f);

          //
          _explosion._onExplode += () =>
          {
            // Disable rung
            _ring.enabled = false;
          };

          //
          _mortarAim.position = new Vector3(0f, -100f, 0f);

          _mortarAudioSource.loop = false;
          _mortarAudioSource.Stop();
          _mortarAudioSource = null;

          break;

        case UtilityType.SHURIKEN:

          //
          _onTriggerEnter += (Collider c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c))
              return;
          };

          // Add kill on impact event
          _onCollisionEnter += (Collision c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c.collider))
              return;

            // Ragdoll
            var rag = ActiveRagdoll.GetRagdoll(c.collider.gameObject);
            var killed = false;
            if (rag != null)
            {
              if (rag._Id == _ragdoll._Id) return;
              if (rag._IsDead) return;
              killed = true;
              transform.parent = c.transform;
              rag.TakeDamage(
                new ActiveRagdoll.RagdollDamageSource()
                {
                  Source = _ragdoll,

                  HitForce = new Vector3(0f, 1f, 0f),

                  Damage = 1,
                  DamageSource = _ragdoll._Hip.position,
                  DamageSourceType = ActiveRagdoll.DamageSourceType.THROW_MELEE,

                  SpawnBlood = true,
                  SpawnGiblets = false
                });
              PlaySound(Audio.UTILITY_ACTION);
            }

            EnemyScript.CheckSound(transform.position, killed ? EnemyScript.Loudness.SUPERSOFT : EnemyScript.Loudness.SOFT);
            transform.GetChild(1).GetComponent<ParticleSystem>().Stop();
            GameObject.Destroy(_rb);
            _stuck = true;

            // Stop ignoring holder's ragdoll
            _ragdoll.IgnoreCollision(_c, false);
            _c.isTrigger = true;
            PlaySound(Audio.UTILITY_HIT_FLOOR);

          };
          // Throw and queue next
          transform.GetChild(1).GetComponent<ParticleSystem>().Play();
          /*var mode = 0;
          if (_ragdoll._isPlayer)
          {
            if (ControllerManager.GetPlayerGamepad(_ragdoll._playerScript._id).buttonSouth.isPressed)
              mode = 1;
          }*/
          Throw();
          Unregister();
          break;

        case UtilityType.SHURIKEN_BIG:

          //
          _onCollisionEnter += (Collision c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c.collider))
              return;
          };
          _onTriggerEnter += (Collider c) =>
          {
            // Projectile handler
            if (SimpleProjectileHandler(_c, c))
              return;

            // Ragdolls
            var rag = ActiveRagdoll.GetRagdoll(c.gameObject);
            if (rag != null)
            {
              if (rag._Id == _ragdoll._Id) return;
              if (rag._Id == (_ragdoll._grapplee?._Id ?? -1)) return;
              if (!rag._IsDead)
              {
                // Check for same ragdoll
                if (_hitRagdolls.Contains(rag._Id)) return;
                _hitRagdolls.Add(rag._Id);

                rag.TakeDamage(
                  new ActiveRagdoll.RagdollDamageSource()
                  {
                    Source = _ragdoll,

                    HitForce = new Vector3(0f, 1f, 0f),

                    Damage = 1,
                    DamageSource = _ragdoll._Hip.position,
                    DamageSourceType = ActiveRagdoll.DamageSourceType.THROW_MELEE,

                    SpawnBlood = true,
                    SpawnGiblets = true
                  });
                rag.Dismember(rag._spine);
                EnemyScript.CheckSound(transform.position, EnemyScript.Loudness.SUPERSOFT);
                PlaySound(Audio.UTILITY_ACTION);
              }
            }
            else
            {
              transform.GetChild(1).GetComponent<ParticleSystem>().Stop();
              GameObject.Destroy(_rb);
              _stuck = true;

              // Stop ignoring holder's ragdoll
              source.IgnoreCollision(transform.GetChild(2).GetComponent<Collider>(), false);
              source.IgnoreCollision(transform.GetChild(3).GetComponent<Collider>(), false);
              source.IgnoreCollision(transform.GetChild(4).GetComponent<Collider>(), false);

              //((SphereCollider)_c).radius *= 1.7f;
              PlaySound(Audio.UTILITY_HIT_FLOOR);
              EnemyScript.CheckSound(transform.position, EnemyScript.Loudness.SOFT);
            }
          };

          // Throw and queue next
          transform.GetChild(1).GetComponent<ParticleSystem>().Play();
          Throw();
          Unregister();
          _hitRagdolls = new List<int>();
          break;

        case UtilityType.KUNAI_EXPLOSIVE:

          //
          _onTriggerEnter += (Collider c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c))
              return;
          };
          _onCollisionEnter += (Collision c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c.collider))
              return;

            // Explode on impact
            Explode();

          };

          // Throw and queue next
          transform.GetChild(1).GetComponent<ParticleSystem>().Play();
          Throw();
          Unregister();
          break;

        case UtilityType.KUNAI_STICKY:


          //
          _onTriggerEnter += (Collider c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c))
              return;
          };
          _onCollisionEnter += (Collision c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c.collider))
              return;

            // Stick to enemy and delayed explode
            if (!_stuck)
            {
              _stuck = true;

              var rag = ActiveRagdoll.GetRagdoll(c.collider.gameObject);
              if (rag != null)
              {
                if (rag._Id == _ragdoll._Id) return;
                transform.parent = c.transform;
                PlaySound(Audio.UTILITY_ACTION);
              }
              else
                PlaySound(Audio.UTILITY_HIT_FLOOR);
              transform.GetChild(1).GetComponent<ParticleSystem>().Stop();
              GameObject.Destroy(_rb);
              //_rb.isKinematic = true;

              // Trigger explosion
              EnemyScript.CheckSound(transform.position, EnemyScript.Loudness.SOFT);
              Explode(1f);
            }
          };

          _explosion._onExplode += () =>
          {
            // Hide ring
            if (_ring != null)
              _ring.enabled = false;
          };

          // Throw and queue next
          transform.GetChild(1).GetComponent<ParticleSystem>().Play();
          Throw();
          Unregister();
          break;

        case UtilityType.STICKY_GUN_BULLET:

          //
          _onTriggerEnter += (Collider c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c))
              return;
          };
          _onCollisionEnter += (Collision c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c.collider))
              return;

            // Stick to enemy and delayed explode
            if (!_stuck)
            {
              _stuck = true;

              var rag = ActiveRagdoll.GetRagdoll(c.collider.gameObject);
              if (rag != null)
              {
                if (rag._Id == _ragdoll._Id) return;
                transform.parent = c.transform;
                PlaySound(Audio.UTILITY_ACTION);
              }
              else
                PlaySound(Audio.UTILITY_HIT_FLOOR);
              transform.GetChild(1).GetComponent<ParticleSystem>().Stop();
              GameObject.Destroy(_rb);
            }
          };
          _explosion._onExplode += () =>
          {
            // Hide ring
            if (_ring != null)
              _ring.enabled = false;
          };

          // Throw and queue next
          transform.GetChild(1).GetComponent<ParticleSystem>().Play();
          Throw();
          Unregister();
          break;

        case UtilityType.GRENADE_IMPACT:
        case UtilityType.GRENADE_STUN:

          //
          _onTriggerEnter += (Collider c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c))
              return;
          };
          _onCollisionEnter += (Collision c) =>
          {
            // Projectile handler
            if (SimpleProjectileHandler(_c, c.collider))
              return;

            // Explode on impact
            Explode();
          };

          // Throw and queue next
          Throw();
          Unregister();

          break;

        //
        case UtilityType.TACTICAL_BULLET:

          // Bullet FX
          void TactBulletFX()
          {
            var particles = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.TACTICAL_BULLET)[0];
            particles.transform.position = transform.position;
            particles.Play();

            EnemyScript.CheckSound(transform.position, EnemyScript.Loudness.SOFT);
            PlaySound(3);
          }
          System.Action<ProjectileCollisionData> onDisable = (ProjectileCollisionData p) =>
          {
            TactBulletFX();
          };

          //
          _onTriggerEnter += (Collider c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c, onDisable))
              return;
          };
          _onCollisionEnter += (Collision c) =>
          {

            // Projectile handler
            if (SimpleProjectileHandler(_c, c.collider, onDisable))
              return;

            // Spawn bullet toward closes enemy on impact event
            if (_stuck) return;
            _stuck = true;

            //
            var rag = ActiveRagdoll.GetRagdoll(c.collider.gameObject);
            var killed = false;
            if (rag != null)
            {
              if (rag._Id == _ragdoll._Id || rag._IsDead) return;
              transform.parent = c.transform;

              killed = true;
              rag.TakeDamage(
                new ActiveRagdoll.RagdollDamageSource()
                {
                  Source = _ragdoll,

                  HitForce = new Vector3(0f, 1f, 0f),

                  Damage = 1,
                  DamageSource = _ragdoll._Hip.position,
                  DamageSourceType = ActiveRagdoll.DamageSourceType.THROW_MELEE,

                  SpawnBlood = true,
                  SpawnGiblets = false
                });
              PlaySound(Audio.UTILITY_ACTION);
            }
            else
              PlaySound(Audio.UTILITY_HIT_FLOOR);
            transform.GetChild(1).GetComponent<ParticleSystem>().Stop();
            EnemyScript.CheckSound(transform.position, killed ? EnemyScript.Loudness.SUPERSOFT : EnemyScript.Loudness.SOFT);
            GameObject.Destroy(_rb);

            //
            IEnumerator SpawnBulletCo(float delay)
            {

              var gameid = GameScript._GameId;
              yield return new WaitForSeconds(delay);
              if (gameid != GameScript._GameId) { }
              else
              {

                for (var i = 0; i < _flag + 1; i++)
                {

                  //
                  if (this == null || !gameObject.activeSelf) break;

                  //
                  var usePlayers = true;
                  var targetPosition = Vector3.zero;
                  if (!EnemyScript.AllDead())
                  {
                    var closestEnemy = FunctionsC.GetClosestEnemyTo(transform.position, false);
                    if (closestEnemy._ragdoll != null)
                    {
                      usePlayers = false;
                      targetPosition = closestEnemy._ragdoll._Hip.position;
                    }
                  }
                  if (usePlayers)
                  {
                    var closestPlayer = FunctionsC.GetClosestPlayerTo(targetPosition, _ragdoll._PlayerScript._Id);
                    if (closestPlayer._ragdoll != null)
                    {
                      targetPosition = closestPlayer._ragdoll._Hip.position;
                    }
                  }

                  if (targetPosition != Vector3.zero)
                  {

                    var shootDir = (targetPosition - transform.position).normalized;
                    var bullet = SpawnBulletTowards(
                      _ragdoll,
                      new Vector3(transform.position.x, _ragdoll._spine.transform.position.y, transform.position.z) + shootDir * 0.2f + MathC.Get2DVector(_throwPosition - transform.position).normalized * 0.2f,
                      shootDir,
                      GameScript.ItemManager.Items.NONE,
                      0
                    );
                    bullet.SetBulletData(
                      _ragdoll,
                      true,
                      0.25f,
                      false,
                      GameScript.ItemManager.Items.PISTOL_SILENCED,

                      true
                    );

                  }

                  // FX
                  TactBulletFX();

                  // Delay repeat
                  if (_flag > 0 && i == 0)
                    yield return new WaitForSeconds(0.3f);
                }

                if (this != null)
                  gameObject.SetActive(false);
              }
            }
            GameScript._s_Singleton.StartCoroutine(SpawnBulletCo(0.5f));
          };

          // Throw and queue next
          Throw();
          Unregister();
          transform.GetChild(1).GetComponent<ParticleSystem>().Play();

          break;

        case UtilityType.C4:

          incrementClip = false;

          if (!_thrown)
          {
            _stuck = false;

            //
            _onTriggerEnter += (Collider c) =>
            {

              // Projectile handler
              if (SimpleProjectileHandler(_c, c))
                return;
            };
            _onCollisionEnter += (Collision c) =>
            {

              // Projectile handler
              if (SimpleProjectileHandler(_c, c.collider))
                return;

              // Add stick on impact event
              if (!_stuck)
              {
                _stuck = true;
                var rag = ActiveRagdoll.GetRagdoll(c.collider.gameObject);
                if (rag != null)
                {
                  if (rag._Id == _ragdoll._Id) return;
                  transform.parent = c.transform;
                  GameObject.Destroy(_rb);
                  _explosion.enabled = false;
                }
              }
            };

            _explosion._onExplode += () =>
            {
              if (_unregister) _ragdoll._PlayerScript?._Profile.UtilityUse(_side);

              // Hide ring
              if (_ring != null)
                _ring.enabled = false;
            };

            // Throw
            Throw();
          }

          // Once thrown, detonate on use and queue next
          else
          {
            Explode();
            Unregister();
          }
          break;

        case UtilityType.STOP_WATCH:

          PlayerScript._SlowmoTimer += 2f;
          Unregister();

          break;

        case UtilityType.TEMP_SHIELD:

          if (_ragdoll._grappling)
          {
            _ragdoll.Grapple(false);
          }

          _ragdoll.AddGrappler();
          Unregister();

          break;

        case UtilityType.INVISIBILITY:

          _ragdoll.SetInvisible(true);
          Unregister();

          break;

        case UtilityType.DASH:

          _ragdoll.SetInvisible(true);
          Unregister();

          break;
      }

      // Increment clip
      _clip--;
      if (incrementClip && _unregister) _ragdoll._PlayerScript?._Profile.UtilityUse(_side);

      // Extra; infinite ammo
      if (_ragdoll._IsPlayer && Settings._Extras_CanUse && Settings._Extra_PlayerAmmo._value == 3)
      {
        _clip++;
        _ragdoll._PlayerScript.AddUtility(_utility_type, _side);
      }
    };

    _onUpdate = () =>
    {

      //
      if (_thrown)
      {
        if (Time.time - _thrownTimer > _expirationTimer)
        {
          if (_utility_type == UtilityType.C4)
          {
            Explode();
            Unregister();
          }
          else
            Destroy(gameObject);
        }
      }

      // Mortar aimer
      if (_utility_type == UtilityType.MORTAR_STRIKE && _mortarAim != null)
        if (_triggerDown)
        {
          var desiredPos = _ragdoll._Hip.position + MathC.Get2DVector(_ragdoll._Hip.transform.forward) * Mathf.Clamp(_downTime * 6f, 0f, 3f);
          _mortarAim.position += (desiredPos - _mortarAim.position) * Time.deltaTime * 20f;

          var scaleMod = 2f + Mathf.Sin(Time.time * 6f) * 0.1f;
          _mortarAim.localScale = new Vector3(scaleMod, scaleMod, scaleMod);
        }

      //
      if (_ring != null)
      {
        // Show ring for cooked grenade
        if (_utility_type == UtilityType.GRENADE && extra && !_thrown)
          _ring.transform.position = _ragdoll._transform_parts._hip.position + _ragdoll._transform_parts._hip.forward * 0.3f;
        // Normal ring behavior
        else
          _ring.transform.position = transform.position;
      }

      switch (_utility_type)
      {

        // Check for grenade cook
        case UtilityType.GRENADE:
          if (_downTime != 0f && !_thrown && !_explosion._triggered && !_explosion._exploded)
          {
            // Set scale
            if (_ragdoll._IsPlayer && _ragdoll._PlayerScript.HasPerk(Shop.Perk.PerkType.EXPLOSIONS_UP))
              explosion_radius *= 1.25f;

            var localscale = _ring.transform.localScale;
            localscale *= explosion_radius * 1.1f;
            localscale.z = 2f;
            _ring.transform.localScale = localscale;

            _ring.enabled = true;

            extra = true;

            _explosion._radius = explosion_radius * 0.8f;

            // Delayed explode grenade
            _explosion.Trigger(_ragdoll, 1.3f);
            PlaySound(Audio.UTILITY_EXTRA);
            EnemyScript.CheckSound(transform.position, EnemyScript.Loudness.SUPERSOFT);

            _explosion._onExplode += () =>
            {
              // If not thrown, explode in hand
              if (!_thrown)
              {
                var spawnPos = _ragdoll._transform_parts._hip.position + _ragdoll._transform_parts._hip.forward * 0.3f;
                spawnPos.y = BulletScript.s_BULLET_HEIGHT;
                transform.position = spawnPos;
                if (_unregister) _ragdoll._PlayerScript?._Profile.UtilityUse(_side);
              }
              // Disable rung
              _ring.enabled = false;
            };
          }
          break;

        // Bullet knife
        case UtilityType.TACTICAL_BULLET:

          if (_flag == 0 && _downTime > 1f)
          {
            _flag = 1;
            PlaySound(4);
          }

          break;
      }
    };

    // Unregister on explosion
    if (_explosion)
      _explosion._onExplode += () =>
      {
        Unregister();
      };

    // Register ragdoll
    _ragdoll = source;

    // Ignore holder's ragdoll
    if (_utility_type == UtilityType.SHURIKEN_BIG)
    {
      source.IgnoreCollision(transform.GetChild(2).GetComponent<Collider>());
      source.IgnoreCollision(transform.GetChild(3).GetComponent<Collider>());
      source.IgnoreCollision(transform.GetChild(4).GetComponent<Collider>());
    }
    else if (_c != null)
      source.IgnoreCollision(_c);

    // Register
    _unregister = unregister;
  }

  bool extra;

  void Unregister()
  {
    if (_unregister)
      _ragdoll._PlayerScript.NextUtility(_side, this);
  }

  // Throw the item
  float _thrownTimer;
  Vector3 _throwPosition;
  void Throw(int mode = 0)
  {
    // Set switch
    if (_thrown) return;
    _thrown = true;
    _thrownTimer = Time.time;

    // Check for max utils
    var utils_thrown = _Utilities_Thrown[_utility_type];
    var maxed = utils_thrown.Count > 25;
    if (maxed)
    {
      var util = utils_thrown[0];
      utils_thrown.Remove(util);
      if (util != null)
        GameObject.Destroy(util.gameObject);
    }
    utils_thrown.Add(this);

    // Ignore collisions
    if (_utility_type != UtilityType.C4)
    {
      _ragdoll._grapplee?.IgnoreCollision(_c);
    }

    //
    var forward = _ragdoll._Hip.transform.forward;
    if (mode == 1) forward = _ragdoll._Hip.transform.right;
    else if (mode == 2) forward = -_ragdoll._Hip.transform.right;

    // Rotation
    if (_spawnDirection != Vector3.zero)
    {
      forward = _spawnDirection;
    }

    var spawnPosition = _spawnLocation != Vector3.zero ? _spawnLocation :
      _ragdoll._spine.transform.position + forward * 0.5f + new Vector3(0f, (_explosion != null ? 0.25f : 0.1f), 0f);
    spawnPosition.y = BulletScript.s_BULLET_HEIGHT;
    _rb.position = _throwPosition = spawnPosition;

    // Configure Rigidbody
    if (_utility_type != UtilityType.MORTAR_STRIKE)
      _rb.isKinematic = false;
    _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

    // Add force
    _rb.AddForce(
      MathC.Get2DVector(forward * 250f * (_throwSpeed + Mathf.Clamp(_downTime, 0f, 4f)) +
      Vector3.up * 55f +
      _ragdoll._Hip.velocity * 1.3f) * _forceModifier);

    // Rotate
    if (_spin)
    {
      if (_spinYAxis)
        _rb.maxAngularVelocity = 45f + 5f * Random.value;
      _rb.AddTorque(_spinYAxis ? Vector3.up * 1000f : _ragdoll._Hip.transform.right * 300f);
    }
    else
    {
      transform.LookAt(transform.position + forward);
    }
  }

  //
  public override void UseDown()
  {
    base.UseDown();

    //
    if (_utility_type == UtilityType.MORTAR_STRIKE && _mortarAim != null)
    {
      _mortarAim.position = _ragdoll._Hip.position;

      if (_mortarAudioSource == null)
      {
        _mortarAudioSource = PlaySound(4, 1f, 1f);
        _mortarAudioSource.loop = true;
      }
    }
  }

  // Pick up utility off ground / corpse
  public void PickUp(PlayerScript player)
  {
    // Check if can pickup
    if (_rb != null || Time.time - _thrownTimer < 0.5f) return;
    var util_data = player.HasUtility(_utility_type);
    if (util_data.Item1)
    {
      var side = util_data.Item2;
      if (!player._Profile.CanUtilityReload(side))
      {
        // Check for right side
        if (side == ActiveRagdoll.Side.LEFT && player.HasUtility(_utility_type, ActiveRagdoll.Side.RIGHT).Item1)
        {
          side = ActiveRagdoll.Side.RIGHT;
          if (player._Profile.CanUtilityReload(side))
          {
            // Give player utility and update player UI
            player.AddUtility(_utility_type, side);
            player._Profile.UtilityReload(side, 1);

            // Play noise
            player._ragdoll.PlaySound("Ragdoll/Pickup");

            // Disable and hide
            GameObject.Destroy(gameObject, 2f);
            gameObject.SetActive(false);
          }
        }

        return;
      }

      // Give player utility and update player UI
      player.AddUtility(_utility_type, side);
      player._Profile.UtilityReload(side, 1);

      // Play noise
      player._ragdoll.PlaySound("Ragdoll/Pickup");

      // Disable and hide
      GameObject.Destroy(gameObject, 2f);
      gameObject.SetActive(false);
    }
  }

  bool _exploded;
  public void Explode(float delay = 0f)
  {
    if (_exploded) return;
    _exploded = true;
    if (_explosion == null) return;

    // Explode effect
    if (delay > 0f)
      _explosion.Trigger(_ragdoll, delay, true, true);
    else
    {
      _explosion.Explode(_ragdoll, true, false);

      // Play noise
      PlaySound(Audio.UTILITY_ACTION);

      // Hide ring
      _ring.enabled = false;
    }
  }

  private void OnDestroy()
  {
    //
    if (_ring != null && _ring.transform.parent != null) GameObject.Destroy(_ring.transform.parent.gameObject);

    //
    if (_utility_type == UtilityType.MORTAR_STRIKE)
    {
      if (_mortarAim != null)
        GameObject.Destroy(_mortarAim.gameObject);

      if (_mortarAudioSource != null)
      {
        _mortarAudioSource.loop = false;
        _mortarAudioSource.Stop();
        _mortarAudioSource = null;
      }
    }
  }

  bool _stuck;
  float _lastSound;
  private void OnCollisionEnter(Collision collision)
  {
    // Fire action
    _onCollisionEnter?.Invoke(collision);

    // Check for collision on throw based on velocity
    if (_rb != null && _rb.velocity.magnitude > 0.1f && Time.time - _lastSound > 0.1f && _sfx_clip.Length > 3)
    {
      _lastSound = Time.time;

      // Play noise
      PlaySound(Audio.UTILITY_HIT_FLOOR);
    }
  }
  private void OnTriggerEnter(Collider other)
  {
    // Fire action
    _onTriggerEnter?.Invoke(other);
  }

  // Static function to load a utility from Resources
  public static UtilityScript GetUtility(UtilityType type)
  {
    var utilityName = type.ToString();

    var gameObject = GameObject.Instantiate(Resources.Load($"Items\\{utilityName}")) as GameObject;
    gameObject.name = utilityName;
    gameObject.transform.parent = GameResources._Container_Objects;
    gameObject.transform.position = new Vector3(1000, -100f, 0f);
    return gameObject.GetComponent<UtilityScript>();
  }

  //
  public struct ProjectileCollisionData
  {
    public GameObject _GameObject;

    public int _PenatrationAmount;
    public System.Action<ProjectileCollisionData> _OnDisable;
    public bool _IsBullet, _CanDestroyObjects;
    public Vector3 _SpawnPosition;
    public ActiveRagdoll _DamageSource;
  }
  public static void HandleProjectileCollision(ProjectileCollisionData p0, ProjectileCollisionData p1)
  {
    // Sanitize
    if (!p0._GameObject.activeSelf || !p1._GameObject.activeSelf)
      return;

    //
    var numBullets = 0;

    // Destroy both
    if (p0._PenatrationAmount == p1._PenatrationAmount)
    {

      p0._OnDisable?.Invoke(p0);
      p1._OnDisable?.Invoke(p1);

      p0._GameObject.SetActive(false);
      p1._GameObject.SetActive(false);

      if (p0._IsBullet) numBullets++;
      if (p1._IsBullet) numBullets++;
    }

    // Destroy one
    else
    {

      var greater = p0;
      var lesser = p1;
      if (p0._PenatrationAmount < p1._PenatrationAmount)
      {
        greater = p1;
        lesser = p0;
      }

      lesser._OnDisable?.Invoke(lesser);
      lesser._GameObject.SetActive(false);
      if (lesser._IsBullet) numBullets++;

      if (greater._IsBullet)
      {
        var bs = greater._GameObject.GetComponent<BulletScript>();
        for (var i = 0; i < lesser._PenatrationAmount + 1; i++)
          bs.RecordHit();
      }

      // Check achievement
#if UNITY_STANDALONE
      if (p0._IsBullet && p1._IsBullet && greater._DamageSource._IsPlayer)
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.BULLET_DESTROY);
#endif
    }

    //
    BulletScript.PlayBulletEffect(0, p0._GameObject.transform.position, Vector3.zero);
    if (numBullets > 0)
      BulletScript.PlayBulletEffectDropBullets(p0._GameObject.transform.position, numBullets);
  }

  public static ProjectileCollisionData? GetProjectileCollisionData(Collider c)
  {

    var projectileData = new ProjectileCollisionData()
    {
      _GameObject = c.gameObject
    };
    switch (c.name.ToLower())
    {

      // Bullet
      case "bullet":

        var bulletScript = c.gameObject.GetComponent<BulletScript>();

        projectileData._PenatrationAmount = bulletScript.GetPenatrationAmount(false);
        projectileData._IsBullet = true;
        projectileData._CanDestroyObjects = true;
        projectileData._SpawnPosition = bulletScript.GetShootPosition();
        projectileData._DamageSource = bulletScript.GetDamageSource();
        break;

      // Explode on disable
      case "grenade":
      case "grenade_impact":
      case "grenade_sticky":
      case "grenade_stun":
      case "sticky_gun_bullet":
      case "c4":
      case "kunai_explosive":
      case "kunai_sticky":
        projectileData._PenatrationAmount = 0;
        projectileData._OnDisable += (ProjectileCollisionData p) =>
        {
          p._GameObject.GetComponent<UtilityScript>().Explode();
        };
        break;

      // Normal
      case "shuriken":
      case "tactical_bullet":
        projectileData._PenatrationAmount = 0;
        break;

      // Shuriken big!
      case "shuriken_big":
        projectileData._GameObject = c.transform.parent.gameObject;
        projectileData._PenatrationAmount = 99999;
        projectileData._CanDestroyObjects = true;
        break;

      // Not found
      default:
        projectileData._GameObject = null;
        break;

    }

    //
    if (projectileData._GameObject != null)
      if (!projectileData._IsBullet)
      {
        var utilityScript = projectileData._GameObject.GetComponent<UtilityScript>();

        projectileData._SpawnPosition = utilityScript._throwPosition;
        projectileData._DamageSource = utilityScript._ragdoll;
      }

    //
    return projectileData._GameObject == null ? null : projectileData;
  }

  //
  public static bool SimpleProjectileHandler(Collider c0, Collider c1, System.Action<ProjectileCollisionData> onDisable = null)
  {
    // Projectile handler
    if (c1.gameObject.layer == 3)
    {
      var pSelf = GetProjectileCollisionData(c0);
      var pOther = GetProjectileCollisionData(c1);
      if (pSelf != null && pOther != null)
      {

        var pSelf_ = pSelf.Value;
        var pOther_ = pOther.Value;

        if (onDisable != null)
          pSelf_._OnDisable += onDisable;

        HandleProjectileCollision(pSelf_, pOther_);
      }

      return true;
    }

    // Books
    else if (c1.name.ToLower() == "books")
    {

      var pSelf = GetProjectileCollisionData(c0);
      if (pSelf != null)
      {
        var pSelf_ = pSelf.Value;
        if (pSelf_._CanDestroyObjects)
        {
          FunctionsC.BookManager.ExplodeBooks(c1, pSelf_._SpawnPosition);

          // Handle bullet
          if (pSelf_._IsBullet)
            pSelf_._GameObject.GetComponent<BulletScript>().RecordHitFull();

          return true;
        }
      }
    }

    // TV
    else if (c1.name.ToLower() == "television")
    {

      var pSelf = GetProjectileCollisionData(c0);
      if (pSelf != null)
      {
        var pSelf_ = pSelf.Value;
        if (pSelf_._CanDestroyObjects)
        {
          c1.GetComponent<TVScript>().Explode(pSelf_._DamageSource);

          // Handle bullet
          if (pSelf_._IsBullet)
            pSelf_._GameObject.GetComponent<BulletScript>().RecordHitFull();

          return true;
        }
      }
    }

    return false;
  }

}