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
  }

  public UtilityType _utility_type;
  public ActiveRagdoll.Side _side;

  Rigidbody _rb;
  Collider _c;

  bool _thrown,
    _spinYAxis,
    _spin;

  ExplosiveScript _explosion;

  delegate void CollisionEvent(Collision c);
  CollisionEvent _onCollisionEnter;

  System.Action<Collider> _onTriggerEnter;

  float _throwSpeed,
    _expirationTimer;

  List<int> _hitRagdolls;

  int _customProjectileId;
  public void RegisterCustomProjectile(ItemScript source)
  {
    _customProjectileId = source._id;
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

  public static void Detonate_StickyBullets(ItemScript source)
  {
    foreach (var bullet in _Utilities_Thrown[UtilityType.STICKY_GUN_BULLET])
    {
      if (bullet._customProjectileId == source._id)
      {
        bullet.Explode(Random.Range(0.05f, 0.15f));
      }
    }
  }

  // Start is called before the first frame update
  new void Start()
  {
    _disableOnRagdollDeath = false;
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
        explosion_radius = 3f;
        _throwSpeed = 2f;
        explosionType = ExplosiveScript.ExplosionType.STUN;
        break;

      case UtilityType.C4:
        explosion_radius = 3f;
        _expirationTimer = 90f;
        break;

      case UtilityType.SHURIKEN:
        _throwSpeed = 4f;
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
    _c = GetComponent<Collider>();

    // Set use
    _onUse = () =>
    {
      // Set explosion size
      if (_explosion != null && _utility_type != UtilityType.GRENADE)
      {
        if (_ragdoll._isPlayer && _ragdoll._playerScript.HasPerk(Shop.Perk.PerkType.EXPLOSIONS_UP))
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
          Throw();
          Unregister();
          break;
        case UtilityType.SHURIKEN:
          // Add kill on impact event
          _onCollisionEnter += (Collision c) =>
          {
            if (!_c.enabled) return;
            if (_rb == null) return;

            // Bullet
            if (c.gameObject.name == "Bullet")
            {
              return;
            }

            // Books
            /*else if (c.gameObject.name == "Books")
            {
              FunctionsC.BookManager.ExplodeBooks(c.collider, _ragdoll.transform.position);
            }*/

            // other..
            else if (c.gameObject.layer == 3)
            {
              return;
            }

            var rag = ActiveRagdoll.GetRagdoll(c.collider.gameObject);
            var killed = false;
            if (rag != null)
            {
              if (rag._id == _ragdoll._id) return;
              if (rag._dead) return;
              killed = true;
              transform.parent = c.transform;
              rag.TakeDamage(
                new ActiveRagdoll.RagdollDamageSource()
                {
                  Source = _ragdoll,

                  HitForce = new Vector3(0f, 1f, 0f),

                  Damage = 1,
                  DamageSource = _ragdoll._hip.position,
                  DamageSourceType = ActiveRagdoll.DamageSourceType.THROW_MELEE,

                  SpawnBlood = true,
                  SpawnGiblets = false
                });
              PlaySound(Audio.UTILITY_ACTION);
            }

            EnemyScript.CheckSound(transform.position, killed ? EnemyScript.Loudness.SUPERSOFT : EnemyScript.Loudness.SOFT);
            transform.GetChild(1).GetComponent<ParticleSystem>().Stop();
            GameObject.Destroy(_rb);

            // Stop ignoring holder's ragdoll
            _ragdoll.IgnoreCollision(_c, false);
            ((SphereCollider)_c).radius *= killed ? 10f : 4f;
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

          // Add kill on impact event
          _onTriggerEnter += (Collider c) =>
          {
            if (!_c.enabled || _rb == null) return;

            // Books
            if (c.name == "Books")
            {
              FunctionsC.BookManager.ExplodeBooks(c, _ragdoll.transform.position);
              PlaySound(Audio.UTILITY_ACTION);
              return;
            }

            // Bullet
            else if (c.name == "Bullet")
            {
              var bullet = c.gameObject.GetComponent<BulletScript>();
              bullet.Hide();
              bullet.PlaySparks();
              return;
            }

            // other..
            else if (c.gameObject.layer == 3)
            {
              return;
            }

            var rag = ActiveRagdoll.GetRagdoll(c.gameObject);
            if (rag != null)
            {
              if (rag._id == _ragdoll._id) return;
              if (rag._id == (_ragdoll._grapplee?._id ?? -1)) return;
              if (!rag._dead)
              {
                // Check for same ragdoll
                if (_hitRagdolls.Contains(rag._id)) return;
                _hitRagdolls.Add(rag._id);

                rag.TakeDamage(
                  new ActiveRagdoll.RagdollDamageSource()
                  {
                    Source = _ragdoll,

                    HitForce = new Vector3(0f, 1f, 0f),

                    Damage = 1,
                    DamageSource = _ragdoll._hip.position,
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
              // Stop ignoring holder's ragdoll
              _ragdoll.IgnoreCollision(_c, false);
              ((SphereCollider)_c).radius *= 1.7f;
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
          // Add explode on impact event
          _onCollisionEnter += (Collision c) =>
          {
            Explode();
          };
          // Throw and queue next
          transform.GetChild(1).GetComponent<ParticleSystem>().Play();
          Throw();
          Unregister();
          break;
        case UtilityType.KUNAI_STICKY:

          // Stick to enemy and delayed explode
          _onCollisionEnter += (Collision c) =>
          {

            //Debug.Log($"{_stuck} {c.gameObject.name}");

            if (!_stuck)
            {
              _stuck = true;

              // Bullet
              if (c.gameObject.name == "Bullet")
              {
                _exploded = false;
                Explode();
                return;
              }

              var rag = ActiveRagdoll.GetRagdoll(c.collider.gameObject);
              if (rag != null)
              {
                if (rag._id == _ragdoll._id) return;
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

          // Stick to enemy and delayed explode
          _onCollisionEnter += (Collision c) =>
          {

            //Debug.Log($"{_stuck} {c.gameObject.name}");

            if (!_stuck)
            {
              _stuck = true;

              // Bullet
              if (c.gameObject.name == "Bullet" || c.gameObject.name == "STICKY_GUN_BULLET")
              {
                _exploded = false;
                Explode();
                return;
              }

              var rag = ActiveRagdoll.GetRagdoll(c.collider.gameObject);
              if (rag != null)
              {
                if (rag._id == _ragdoll._id) return;
                transform.parent = c.transform;
                PlaySound(Audio.UTILITY_ACTION);
              }
              else
                PlaySound(Audio.UTILITY_HIT_FLOOR);
              transform.GetChild(1).GetComponent<ParticleSystem>().Stop();
              GameObject.Destroy(_rb);
              //_rb.isKinematic = true;

              // Stick on surface
              //EnemyScript.CheckSound(transform.position, EnemyScript.Loudness.SOFT);
              //Explode(1f);
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
          // Add explode on impact event
          _onCollisionEnter += (Collision c) =>
          {
            Explode();
          };
          // Throw and queue next
          Throw();
          Unregister();
          break;

        case UtilityType.C4:

          incrementClip = false;

          if (!_thrown)
          {
            _stuck = false;
            // Add stick on impact event
            _onCollisionEnter += (Collision c) =>
            {

              // Bullet
              if (c.gameObject.name == "Bullet")
              {
                Explode();
                return;
              }

              if (!_stuck)
              {
                _stuck = true;
                var rag = ActiveRagdoll.GetRagdoll(c.collider.gameObject);
                if (rag != null)
                {
                  if (rag._id == _ragdoll._id) return;
                  transform.parent = c.transform;
                  GameObject.Destroy(_rb);
                  _explosion.enabled = false;
                }
              }
            };
            _explosion._onExplode += () =>
            {
              if (_unregister) _ragdoll._playerScript?._Profile.UtilityUse(_side);
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

          if(_ragdoll._grappling){
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
      if (incrementClip && _unregister) _ragdoll._playerScript?._Profile.UtilityUse(_side);

      // Extra; infinite ammo
      if (_ragdoll._isPlayer && Settings._Extras_CanUse && Settings._Extra_PlayerAmmo._value == 3)
      {
        _clip++;
        _ragdoll._playerScript.AddUtility(_utility_type, _side);
      }
    };

    _onUpdate = () =>
    {
      if (_thrown)
      {
        _thrownTimer += Time.deltaTime;
        if (_thrownTimer > _expirationTimer)
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
        case (UtilityType.GRENADE):
          if (_downTime != 0f && !_thrown && !_explosion._triggered && !_explosion._exploded)
          {
            // Set scale
            if (_ragdoll._isPlayer && _ragdoll._playerScript.HasPerk(Shop.Perk.PerkType.EXPLOSIONS_UP))
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
                transform.position = _ragdoll._transform_parts._hip.position + _ragdoll._transform_parts._hip.forward * 0.3f;
                if (_unregister) _ragdoll._playerScript?._Profile.UtilityUse(_side);
              }
              // Disable rung
              _ring.enabled = false;
            };
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
    if (_c != null)
      source.IgnoreCollision(_c);

    // Register
    _unregister = unregister;
  }

  bool extra;

  void Unregister()
  {
    if (_unregister)
      _ragdoll._playerScript.NextUtility(_side, this);
  }

  // Throw the item
  float _thrownTimer;
  void Throw(int mode = 0)
  {
    // Set switch
    if (_thrown) return;
    _thrown = true;

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
    var forward = _ragdoll._hip.transform.forward;
    if (mode == 1) forward = _ragdoll._hip.transform.right;
    else if (mode == 2) forward = -_ragdoll._hip.transform.right;

    // Rotation
    if (_spawnDirection != Vector3.zero)
    {
      forward = _spawnDirection;
    }

    _rb.position = _spawnLocation != Vector3.zero ? _spawnLocation :
      _ragdoll._spine.transform.position + forward * 0.5f + new Vector3(0f, (_explosion != null ? 0.25f : 0.1f), 0f);

    // Configure Rigidbody
    _rb.isKinematic = false;
    _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

    // Add force
    _rb.AddForce(
      MathC.Get2DVector(forward * 250f * (_throwSpeed + Mathf.Clamp(_downTime, 0f, 4f)) +
      Vector3.up * 55f +
      (_ragdoll._hip.velocity) * 1.3f) * _forceModifier);
    // Rotate
    if (_spin)
    {
      if (_spinYAxis)
        _rb.maxAngularVelocity = 50f + 10f * Random.value;
      _rb.AddTorque(_spinYAxis ? Vector3.up * 100000f : _ragdoll._hip.transform.right * 300f);
    }
    else
    {
      transform.LookAt(transform.position + forward);
    }
  }

  // Pick up utility off ground / corpse
  public void PickUp(PlayerScript player)
  {
    // Check if can pickup
    if (_rb != null) return;
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
            player._Profile.UtilityReload(side, true);

            // Play noise
            player._ragdoll.PlaySound("Ragdoll/Pickup");

            // Disable and hide
            _c.enabled = false;
            transform.GetChild(0).GetComponent<Renderer>().enabled = false;
            GameObject.Destroy(gameObject, 2f);
          }
        }

        return;
      }

      // Give player utility and update player UI
      player.AddUtility(_utility_type, side);
      player._Profile.UtilityReload(side, true);

      // Play noise
      player._ragdoll.PlaySound("Ragdoll/Pickup");

      // Disable and hide
      _c.enabled = false;
      transform.GetChild(0).GetComponent<Renderer>().enabled = false;
      GameObject.Destroy(gameObject, 2f);
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
    if (_ring != null && _ring.transform.parent != null) GameObject.Destroy(_ring.transform.parent.gameObject);
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
}