using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

using ItemType = GameScript.ItemManager.Items;

public class ItemScript : MonoBehaviour
{
  //
  static Settings.LevelSaveData LevelModule { get { return Settings.s_SaveData.LevelData; } }

  // Information about weapon holder
  public ActiveRagdoll _ragdoll;
  protected ActiveRagdoll.Side _side;
  public void SetSide(ActiveRagdoll.Side side)
  {
    _side = side;
  }

  AudioSource _sfx0, _sfx1;

  public int _clipSize, _minimumClipToFire, _projectilesPerShot, _burstPerShot, _penatrationAmount;
  public bool _reloading, _melee, _twoHanded, _runWhileUse, _reloadOneAtTime, _silenced, _throwable, _dismember, _useOnRelease, _randomSpread, _chargeHold;
  public float _useTime, _reloadTime, _useRate, _downTime, _burstRate, _hit_force, _bullet_spread, _shoot_force, _shoot_forward_force;
  float _downTimeSave;
  public FireMode _fireMode;

  float _upTime;

  public UtilityScript.UtilityType _customProjetile;
  public UtilityScript[] _customProjectiles;
  int _customProjectileIter;
  float _customProjectileVelocityMod;

  public bool _IsBulletDeflector { get { return _type == ItemType.FRYING_PAN || _type == ItemType.KATANA; } }

  // Custom components
  ParticleSystem[] _customParticles;
  Light[] _customLights;

  //
  protected bool _disableOnRagdollDeath;

  bool _meleeStartSwinging;

  public int GetClipSize()
  {
    if (_melee && !IsChargeWeapon()) return 1;

    var clip_size = _clipSize;

    // Check perk
    if (_ragdoll != null && _ragdoll._IsPlayer && _ragdoll._PlayerScript.HasPerk(Shop.Perk.PerkType.MAX_AMMO_UP))
      clip_size = Mathf.CeilToInt(_clipSize * 1.5f);

    // Check extra
    if (Settings._Extras_CanUse)
    {
      if (_ragdoll?._IsPlayer ?? false)
        switch (LevelModule.ExtraPlayerAmmo)
        {
          case 1:
            clip_size = Mathf.CeilToInt(_clipSize * 2f);
            break;
          case 2:
            clip_size = Mathf.CeilToInt(_clipSize * 0.5f);
            break;
        }
    }

    return clip_size;
  }
  public float UseRate()
  {
    if (!_melee && _ragdoll != null && _ragdoll._IsPlayer && Shop.Perk.HasPerk(_ragdoll._PlayerScript._Id, Shop.Perk.PerkType.FIRE_RATE_UP))
      return _useRate * 0.5f;
    return _useRate;
  }

  bool _isZombie { get { return !_ragdoll._IsPlayer && _ragdoll._EnemyScript._IsZombieReal; } }
  bool _canMeleePenatrate { get { return _twoHanded || (_type == ItemType.AXE && !_isZombie) || _type == ItemType.ROCKET_FIST; } }

  float _time { get { return /*_ragdoll._isPlayer_twoHanded ? Time.unscaledTime : */Time.time; } }

  public bool _IsSwinging;

  bool _swang;

  //
  protected int _clip, _bursts, _meleeIter;
  protected bool _triggerDown, _triggerDown_last, _used, _hitAnthing, _hitAnthingOverride, _hasHitEnemy;
  public bool _HitAnything { get { return _hitAnthing; } }

  bool _triggerDownReal;

  bool _damageAnything;

  public bool _TriggerDown { get { return _triggerDown; } }

  public void DeflectedBullet()
  {
    if (_IsSwinging) { _hitAnthing = true; }
  }

  public bool Used() { return _used; }

  protected System.Action _onUse, _onUpdate;

  public Transform _handle, _forward;

  // Check for perk
  public int GetPenatrationAmount()
  {
    if (_ragdoll._IsPlayer && Shop.Perk.HasPerk(_ragdoll._PlayerScript._Id, Shop.Perk.PerkType.PENETRATION_UP))
      return _penatrationAmount + 1;
    return _penatrationAmount;
  }

  Transform _arm_lower { get { return transform.parent.parent; } }
  Transform _arm_upper { get { return _arm_lower.parent; } }
  Vector3 _save_rot_lower, _save_rot_upper,
    _original_rot_lower, _original_rot_upper;

  Vector3 _swordLerp0, _swordLerpDesired0, _swordLerp1, _swordLerpDesired1;

  // Audio
  public AudioClip[] _sfx_clip;
  public float[] _sfx_volume;

  public ItemType _type;

  protected List<int> _hitRagdolls;
  public bool HasHitRagdoll(ActiveRagdoll ragdoll)
  {
    return _hitRagdolls.Contains(ragdoll._Id);
  }
  public void RegisterHitRagdoll(ActiveRagdoll ragdoll)
  {
    _hitRagdolls.Add(ragdoll._Id);
  }

  public enum Audio
  {
    GUN_SHOOT,
    GUN_RELOAD,
    GUN_EMPTY,
    GUN_EXTRA,
    MELEE_SWING,
    MELEE_HIT,
    MELEE_HIT_METAL,
    MELEE_HIT_BULLET,
    MELEE_EXTRA,
    UTILITY_THROW,
    UTILITY_HIT_FLOOR,
    UTILITY_ACTION,
    UTILITY_EXTRA
  }

  Coroutine[] _anims;

  // Play SFX via enum
  protected int GetAudioSource(Audio audio)
  {
    int iter = 0;
    switch (audio)
    {
      case Audio.GUN_SHOOT:
        if (_type == ItemType.FLAMETHROWER)
        {
          iter = -1;
        }
        else if (_type == ItemType.CHARGE_PISTOL)
        {
          if (_downTimeSave >= 1.25f)
            iter = 6;
          else if (_downTimeSave >= 0.5f)
            iter = 5;
        }
        break;
      case Audio.GUN_RELOAD:
        if (_type == ItemType.FLAMETHROWER && _clip % 15 == 14)
        {
          iter = 5;
          break;
        }
        iter = 1;
        break;
      case Audio.GUN_EMPTY:
        iter = 2;
        break;
      case Audio.GUN_EXTRA:
        iter = 3;
        break;
      case Audio.MELEE_SWING:
        break;
      case Audio.MELEE_HIT:
        iter = 1;
        break;
      case Audio.MELEE_HIT_METAL:
        iter = 2;
        break;
      case Audio.MELEE_HIT_BULLET:
        iter = 3;
        break;
      case Audio.MELEE_EXTRA:
        iter = 4;
        break;
      case Audio.UTILITY_THROW:
        break;
      case Audio.UTILITY_HIT_FLOOR:
        iter = 1;
        break;
      case Audio.UTILITY_ACTION:
        iter = 2;
        break;
      case Audio.UTILITY_EXTRA:
        iter = 3;
        break;
    }

    if (iter >= _sfx_clip.Length) return -1;

    return iter;
  }
  // Play SFX via enum
  protected void PlaySound(Audio audioType, float pitchMin = 0.9f, float pitchMax = 1.1f)
  {
    var sfx = PlaySound(GetAudioSource(audioType), pitchMin, pitchMax);
    if (audioType == Audio.MELEE_SWING || audioType == Audio.GUN_RELOAD)
    {
      _sfx_hold = sfx;
    }
  }
  protected AudioSource PlaySound(int source_index, float pitchMin = 0.9f, float pitchMax = 1.1f)
  {
    if (source_index == -1) return null;
    return transform.PlayAudioSourceSimple(_sfx_clip[source_index], SfxManager.AudioClass.NONE, _sfx_volume[source_index], Random.Range(pitchMin, pitchMax));
  }

  //
  public void SetHitOverride()
  {
    if (!_ragdoll._IsPlayer) return;
    _hitAnthingOverride = true;
  }

  //
  GameObject _laserSight;
  public void AddLaserSight()
  {
    if (_melee) return;

    // Make sure doesn't have laser sight
    if (_laserSight) return;

    // Spawn lasersight
    _laserSight = GameObject.Instantiate(GameResources._LaserBeam) as GameObject;
    _laserSight.name = "laser";
    _laserSight.layer = 2;
    GameObject.Destroy(_laserSight.GetComponent<Collider>());
    var renderer = _laserSight.GetComponent<Renderer>();
  }

  public enum FireMode
  {
    SEMI,
    AUTOMATIC,
    BURST
  }

  static GameObject _Bullet;
  public static BulletScript[] _BulletPool;
  static int _BulletPool_Iter;

  Item _itemTemplate;

  private void OnDestroy()
  {

    // Mod
    if (_laserSight) GameObject.Destroy(_laserSight);

    // Sfx
    if (_sfx0 != null)
    {
      _sfx0.loop = false;
      _sfx0.Stop();
      _sfx0 = null;
    }
    if (_sfx1 != null)
    {
      _sfx1.loop = false;
      _sfx1.Stop();
      _sfx1 = null;
    }
  }

  static int s_itemID;
  public int _ItemId; // Unique id per item / weapon
  public void Start()
  {
    _disableOnRagdollDeath = true;

    if (_twoHanded)
      _anims = new Coroutine[6];

    // Spawn bullet pool
    if (_Bullet == null)
    {
      _Bullet = Instantiate(GameResources._Bullet);
      _Bullet.SetActive(false);
    }
    if (_BulletPool == null)
    {
      _BulletPool = new BulletScript[30];
      var bullets = new GameObject
      {
        name = "Bullets"
      };
      var rbs = new List<Collider>();
      for (var i = 0; i < _BulletPool.Length; i++)
      {
        var bullet = Instantiate(GameResources._Bullet);
        bullet.name = "Bullet";
        bullet.SetActive(false);
        bullet.transform.parent = bullets.transform;
        bullet.layer = 3;
        _BulletPool[i] = bullet.GetComponent<BulletScript>();
        rbs.Add(bullet.GetComponent<Collider>());
      }
    }

    // Check special types
    if (_type == ItemType.FLAMETHROWER)
    {
      // Gather components
      _customParticles = new ParticleSystem[] { transform.GetChild(4).gameObject.GetComponent<ParticleSystem>() };
      _customLights = new Light[] { transform.GetChild(5).gameObject.GetComponent<Light>() };
    }

    // Set onuse
    _onUse = () =>
    {
      if (_throwable)
      {
        IEnumerator DelayedExplode()
        {
          yield return new WaitForSeconds(0.2f);
          if (!_ragdoll._IsDead)
            transform.GetChild(0).GetComponent<ExplosiveScript>().Trigger(_ragdoll, 0f);
        }
        StartCoroutine(DelayedExplode());
        return;
      }

      // Check invisible
      if (_ragdoll._invisible)
        _ragdoll.SetInvisible(false);

      // Melee
      if (_melee)
      {

        var raycastInfo = new RaycastInfo();
        var isHit = MeleeCast(raycastInfo, _meleeIter++);

        // Sanitize
        if (isHit)
        {

          // Check ragdoll already checked
          if (HasHitRagdoll(raycastInfo._ragdoll))
            isHit = false;

          // Check ragdoll grappled is targetted by holder
          if (raycastInfo._ragdoll._Id == (_ragdoll._grappler?._Id ?? -1))
            isHit = false;
        }

        if (isHit)
        {
          //bool hitEnemy = !raycastInfo._ragdoll._isPlayer;

          // Check stunned
          if (!_ragdoll.Active()) return;

          // Check grappler
          if (raycastInfo._ragdoll._grappling)
          {

            // Get dir from target to swinger; if hostage is held in front of knife, kill hostage
            var dir = (raycastInfo._ragdoll._Hip.position - _ragdoll._Hip.position).normalized;
            var target_frwd = raycastInfo._ragdoll._Controller.forward;

            if ((dir - target_frwd).magnitude > 1f)
            {
              raycastInfo._ragdoll = raycastInfo._ragdoll._grapplee;
            }
          }

          // Check graplee
          if (_ragdoll._grapplee == raycastInfo._ragdoll) return;

          // Check zombie invisible 2nd weapon
          if (_isZombie && _side == ActiveRagdoll.Side.RIGHT) return;

          // If is enemy, and isn't two handed, don't kill friendlies
          if (!_ragdoll._IsPlayer && !_canMeleePenatrate && !raycastInfo._ragdoll._IsPlayer && !raycastInfo._ragdoll._grappled && !_ragdoll._grappled) return;

          // If is player v player, is two handed, and hit enemy before, dont hit
          if (_ragdoll._IsPlayer && raycastInfo._ragdoll._IsPlayer && _canMeleePenatrate && _hasHitEnemy) return;

          // If both are swinging, bounce back both and ignore
          if (_ragdoll._IsSwinging && raycastInfo._ragdoll._IsSwinging)
          {

            // Chaser
            if ((_ragdoll._EnemyScript?.IsChaser() ?? false) || (raycastInfo._ragdoll._EnemyScript?.IsChaser() ?? false))
            {
            }

            // Zombie
            else if (raycastInfo._ragdoll._IsEnemy && raycastInfo._ragdoll._EnemyScript._IsZombieReal)
            {
            }

            // Normal
            else
            {

              var deflectData = raycastInfo._ragdoll.TryDeflectMelee(_ragdoll);
              if (deflectData != null)
              {

                // Check zombie
                if (_isZombie)
                {

                  // Die self
                  deflectData.MeleeDamageOther(_ragdoll);
                  return;
                }

                //
                RegisterHitRagdoll(raycastInfo._ragdoll);
                SetHitOverride();

                // Bounce both ragdolls backward
                _ragdoll.BounceFromPosition(raycastInfo._ragdoll._Hip.position, 1.5f);
                raycastInfo._ragdoll.BounceFromPosition(_ragdoll._Hip.position, 1.5f);

                // Fx
                SfxManager.PlayAudioSourceSimple(_ragdoll._Hip.position, "Ragdoll/MeleeClash", 0.85f, 1.15f, SfxManager.AudioClass.NONE, true);

                var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
                parts.transform.position = (_ragdoll._Hip.position + raycastInfo._ragdoll._Hip.position) / 2f;
                parts.Play();

                return;
              }
            }
          }

          /*/ If is enemy and not chaser and target is player, and player is swinging, dont hit
          if (!_ragdoll._isPlayer && !_ragdoll._EnemyScript.IsChaser() && raycastInfo._ragdoll._isPlayer && raycastInfo._ragdoll._swinging)
          {
            //FunctionsC.PlaySound(ref raycastInfo._ragdoll._audioPlayer_steps, "Ragdoll/Deflect");
            return;
          }*/

          //if (_twoHanded)
          _hitAnthing = true;

          if (!_hasHitEnemy && !raycastInfo._ragdoll._IsPlayer) _hasHitEnemy = true;

          //
          if (_damageAnything) return;

          // Record ragdoll id to stop multiple hits
          _hitRagdolls.Add(raycastInfo._ragdoll._Id);

          // Damage
          _damageAnything = true;
          MeleeDamageOther(raycastInfo._ragdoll);

          // Zombie knockback
          if (!_ragdoll._IsPlayer && _ragdoll._EnemyScript._IsZombieReal)
            raycastInfo._ragdoll.BounceFromPosition(_ragdoll._Hip.position, 0.5f);

          // Play noise
          if (_type == ItemType.FRYING_PAN)
          {
            PlaySound(Audio.MELEE_HIT, 0.78f, 1.1f);
            EnemyScript.CheckSound(_ragdoll._Hip.position, EnemyScript.Loudness.SOFT);
          }
          else
          {
            PlaySound(raycastInfo._ragdoll._IsEnemy && raycastInfo._ragdoll._EnemyScript.IsChaser() ? Audio.MELEE_HIT_METAL : Audio.MELEE_HIT);
            EnemyScript.CheckSound(_ragdoll._Hip.position, EnemyScript.Loudness.SUPERSOFT);
          }

          // If not two handed, stop checking
          if (_canMeleePenatrate)
            _damageAnything = false;
        }
        return;
      }

      // Shoot bullet(s)
      var penatrationAmount = GetPenatrationAmount();
      for (var i = 0; i < _projectilesPerShot; i++)
      {

        var use_penatrationAmount = penatrationAmount;

        Rigidbody rb = null;

        // Normal bullet
        if (_customProjetile == UtilityScript.UtilityType.NONE)
        {

          // Special
          if (_type == ItemType.CHARGE_PISTOL)
          {
            if (_downTimeSave >= 1.25f)
            {
              use_penatrationAmount = 2;
              _shoot_force = 0.6f;
            }
            else if (_downTimeSave >= 0.5f)
            {
              use_penatrationAmount = 1;
              _shoot_force = 0.25f;
            }
            else
            {
              _shoot_force = 0.05f;
            }
          }

          // Spawn bullet
          var spawn_pos = _forward.position;
          if (_type == ItemType.ROCKET_FIST)
            spawn_pos = _ragdoll._Hip.position;

          var bullet = SpawnBulletTowards(
            _ragdoll,
            new Vector3(spawn_pos.x, _ragdoll._spine.transform.position.y, spawn_pos.z),
            transform.forward,
            _type,
            use_penatrationAmount,

            _randomSpread,
            _bullet_spread,
            _projectilesPerShot,
            i
          );
          bullet.SetSourceItem(this);

          /*/ Check gun smoke on
          if (_type == ItemType.REVOLVER)
          {
            _gunSmoke = transform.GetChild(3).GetComponent<ParticleSystem>();
            _gunSmoke.Play();
          }*/
        }

        // Custom projectile
        else
        {
          // Spawn projectiles
          if (_customProjectileIter >= _customProjectiles.Length || _customProjectiles[_customProjectileIter] == null)
            ReloadCustom();

          // Spawn utility
          var spawn_pos = _forward.position;
          var new_position = new Vector3(spawn_pos.x, _ragdoll._spine.transform.position.y, spawn_pos.z);
          var forward = MathC.Get2DVector(transform.forward).normalized;

          //
          var utility = _customProjectiles[_customProjectileIter++];
          utility._side = _side;
          utility._ragdoll = _ragdoll;
          utility.SetSpawnLocation(spawn_pos);
          utility.SetSpawnDirection(forward);
          utility.UseDown();
          utility.UseUp();

          // Set custom velocity
          _customProjectileVelocityMod = 1f;
          if (_type == ItemType.GRENADE_LAUNCHER) _customProjectileVelocityMod = 2f;
          utility.SetForceModifier(_customProjectileVelocityMod);
        }
      }

      // Add force to spine
      var torqueForce = -Vector3.right.normalized * 20000f * _hit_force;
      _ragdoll._spine.GetComponent<Rigidbody>().AddRelativeTorque(torqueForce);
      _ragdoll._head.GetComponent<Rigidbody>().AddRelativeTorque(torqueForce);

      // Recoil player
      _ragdoll.RecoilSimple(_shoot_force);

      // Recoil arm
      if (_type != ItemType.FLAMETHROWER && _type != ItemType.ROCKET_FIST)
      {
        float totalTime = 0.3f,
          halfTime = totalTime * 0.4f;
        AnimateTransformAdditive(_arm_lower, _save_rot_lower, Mathf.Clamp(50f * _hit_force, 20f, 50f), 0f, 0f, totalTime, halfTime);
      }

      // Deincrement ammo
      _clip--;

      // Extra; infinite ammo
      if (_ragdoll._IsPlayer && Settings._Extras_CanUse && LevelModule.ExtraPlayerAmmo == 3)
      {
        _clip++;
      }

      _ragdoll._PlayerScript?._Profile.ItemUse(_side);

      if (_clip == 0) OnClipEmpty();
    };
  }

  void OnClipEmpty()
  {
    if (_type == ItemType.DMR)
      PlaySound(Audio.GUN_EXTRA);
  }

  float _meleeReleaseTimer, _meleeReleaseTimerOverfill, _meleeLerper;
  bool _meleeReleaseTrigger, _meleeComplexResetTrigger;
  //ParticleSystem _gunSmoke;
  public void Update()
  {
    _onUpdate?.Invoke();

    if (_ragdoll == null) return;

    /*/ Check gun smoke off
    if (_type == ItemType.REVOLVER)
    {

      if (_gunSmoke != null && _gunSmoke.isPlaying && Time.time - _useTime > 5f)
        transform.GetChild(3).GetComponent<ParticleSystem>().Stop();
    }*/

    // Custom swing melee
    if (_melee && _useOnRelease)
    {

      if (_triggerDown && !_meleeReleaseTrigger && _meleeReleaseTimer <= 0f)
        _meleeReleaseTrigger = true;

      //
      var swinging = false;
      var swingingRelease = false;
      if (_meleeReleaseTimerOverfill > 0f || _meleeComplexResetTrigger)
      {
        _meleeLerper += (1f - _meleeLerper) * Time.deltaTime * 20f;
        if (_meleeLerper > 0.95f)
        {
          _meleeReleaseTimerOverfill = 0f;
          _meleeComplexResetTrigger = false;
        }
      }
      else if (_meleeReleaseTimer > 0f)
      {
        //_meleeReleaseTimer -= Time.deltaTime;
        _meleeLerper += (0f - _meleeLerper) * Time.deltaTime * 17f;
        if (_meleeLerper < 0.05f)
        {
          _meleeReleaseTimer = 0f;

          if (_type == ItemType.KATANA)
            _meleeComplexResetTrigger = true;
        }

        swinging = true;
      }
      else if (_meleeReleaseTrigger)
      {
        var slowGetup = !CanUse();
        var meleeLerperSave = _meleeLerper;
        _meleeLerper += (Mathf.Clamp(1f, 0f, slowGetup ? 0.5f : 1f) - _meleeLerper) * Time.deltaTime * (slowGetup ? 1.5f : 6f);

        if (_meleeLerper >= 0.8f && meleeLerperSave < 0.8f)
          PlaySound(Audio.MELEE_EXTRA);
      }
      else
      {
        _meleeLerper += (0f - _meleeLerper) * Time.deltaTime * 2f;
      }

      //
      var sideMod = _side == ActiveRagdoll.Side.LEFT ? 1f : -1f;
      switch (_type)
      {
        case ItemType.KNIFE:
        case ItemType.RAPIER:
          SetRotationLocal(_arm_upper, Vector3.Lerp(_save_rot_upper, _save_rot_upper + new Vector3(-60f, -130f * sideMod, -20f * sideMod), _meleeLerper));
          SetRotationLocal(_arm_lower, Vector3.Lerp(_save_rot_lower, _save_rot_lower + new Vector3(110f, 0f, 0f), _meleeLerper));
          break;

        case ItemType.FRYING_PAN:
        case ItemType.AXE:
          SetRotationLocal(_arm_upper, Vector3.Lerp(_save_rot_upper, _save_rot_upper + new Vector3(0f, 70f * sideMod, 0f), _meleeLerper));
          SetRotationLocal(_arm_lower, Vector3.Lerp(_save_rot_lower, _save_rot_lower + new Vector3(55f, 0f, 0f), _meleeLerper));
          break;

        case ItemType.KATANA:

          _swordLerpDesired0 = swinging || _meleeComplexResetTrigger ? new Vector3(70f, 250f, 0f) : new Vector3(0f, 0f, 0f);
          _swordLerpDesired1 = swinging || _meleeComplexResetTrigger ? new Vector3(40f, 0f, 0f) : new Vector3(0f, 0f, 0f);

          _swordLerp0 += (_swordLerpDesired0 - _swordLerp0) * Time.deltaTime * 50f;
          _swordLerp1 += (_swordLerpDesired1 - _swordLerp1) * Time.deltaTime * 50f;

          SetRotationLocal(
            _arm_upper,
            Vector3.Lerp(
              _save_rot_upper + _swordLerp0,
              _save_rot_upper + (swingingRelease ? new Vector3(70f, 250f, 0f) : new Vector3(-20f, -10f, 0f)),
              _meleeLerper
          ));
          SetRotationLocal(
            _arm_lower,
            Vector3.Lerp(
              _save_rot_lower,
              _save_rot_upper + _swordLerp1,
              _meleeLerper
          ));
          break;
      }

      // Check melee dodge
      if (!_meleeStartSwinging && _downTime != 0f && CanUse())
      {
        _meleeStartSwinging = true;

        _ragdoll.OnMeleeStart();
      }
    }

    // Custom type before death check
    if (_type == ItemType.FLAMETHROWER)
    {
      var flames = _customParticles[0];
      var light = _customLights[0];

      // Light
      var min_range = _clip == 20 ? 1f : _clip > 9 ? 0.5f : 0f;
      if (flames.isEmitting)
        light.range += (9f - light.range) * Time.deltaTime * 7f;
      else if (light.range != min_range)
      {
        if (light.range < min_range + 0.1f) light.range = min_range;
        else light.range += (min_range - light.range) * Time.deltaTime * 5f;
      }

      if (_sfx0 != null)
      {
        _sfx0.transform.position = transform.position;
      }
      if (_sfx1 != null)
      {
        _sfx1.transform.position = transform.position;
      }
    }

    // Check for dead
    if (_ragdoll._IsDead)
    {
      OnDestroy();
      if (_disableOnRagdollDeath) this.enabled = false;
      return;
    }
#if UNITY_EDITOR
    /*if (_ragdoll == null) return;
    if (IsGun())
      Debug.DrawRay(transform.position, MathC.Get2DVector(transform.forward) * 100f, Color.red);
    Debug.DrawRay(_ragdoll._Hip.position, _ragdoll._Hip.transform.forward * 100f, Color.cyan);*/
#endif

    // Update laser sight
    if (_laserSight)
    {
      if (_reloading)
        _laserSight.SetActive(false);
      else
      {
        _laserSight.SetActive(true);

        var dist = 0f;
        var forward = MathC.Get2DVector(transform.forward).normalized;
        var start = _forward.position + forward * -0.5f;

        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(new Ray(start, forward), out hit))
          dist = hit.distance;

        if (dist <= 0.5f) dist = 0f;

        start = _forward.position + forward * 0.1f;
        dist -= 0.6f;

        _laserSight.transform.position = start + (forward * (dist / 2f));
        _laserSight.transform.localScale = new Vector3(0.03f, 0.03f, dist);
        _laserSight.transform.LookAt(start + forward * dist * 1.2f);
        _laserSight.transform.position += new Vector3(0f, -0.2f, 0f);
      }
    }

    // Increment down timer
    var downTimeLast = _downTime;
    if (_triggerDown) _downTime += Time.deltaTime;
    else _upTime += Time.deltaTime;

    // Check special cases
    if (_type == ItemType.FLAMETHROWER)
    {
      var flames = _customParticles[0];
      var light = _customLights[0];

      // Particles
      if (flames.isPlaying && (!_used || _reloading || _clip == 0))
        flames.Stop(true);
      else if (!flames.isEmitting && (_used && !_reloading && _clip > 0))
        flames.Play(true);

      // Sfx
      if (flames.isEmitting)
      {

        if (_sfx0 == null)
        {
          _sfx0 = PlaySound(4, 1f, 1f);
          _sfx0.loop = true;
        }

      }
      else if (_sfx0 != null)
      {
        _sfx0.loop = false;
        _sfx0.Stop();
        _sfx0 = null;
      }

      if ((_clip < 10 && !flames.isEmitting) && _sfx1 != null)
      {
        _sfx1.loop = false;
        _sfx1.Stop();
        _sfx1 = null;
      }
      else if ((_clip >= 10) && _sfx1 == null)
      {
        _sfx1 = PlaySound(3, 1f, 1f);
        _sfx1.loop = true;
      }
    }

    // Charge pistol
    if (_type == ItemType.CHARGE_PISTOL)
    {

      if (downTimeLast < 0.02f && _downTime >= 0.02f)
      {
        PlaySound(3, 0.8f, 0.8f);
      }
      else if (downTimeLast < 0.5f && _downTime >= 0.5f)
      {
        PlaySound(4, 1f, 1f);
      }
      else if (downTimeLast < 1.25f && _downTime >= 1.25f)
      {
        PlaySound(4, 1.2f, 1.2f);
      }

    }

    // Check charge weapons
    if (IsChargeWeapon())
    {
      if (_triggerDown && CanReload())
        Reload();
    }

    void Local_Use(bool playSound = true)
    {

      // Check grapple release
      if (_melee)
      {
        if (_ragdoll._grappling)
        {
          var snap_neck = false;
          if (_side == ActiveRagdoll.Side.RIGHT)
          {
            if (!(_ragdoll._ItemL?.IsMelee() ?? false))
            {
              snap_neck = true;
            }
          }
          else if (_side == ActiveRagdoll.Side.LEFT)
          {
            snap_neck = true;
          }

          if (snap_neck)
          {
            _ragdoll.Grapple(false);

#if UNITY_STANDALONE
            // Grapple achievement
            if (_ragdoll._IsPlayer)
              SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.GRAPPLE_NECK);
#endif

            /*_swinging = false;
            _bursts = 0;
            _used = false;
            _triggerDown = false;
            _useTime = _time;*/
            return;
          }
        }
      }

      //
      _onUse?.Invoke();
      _useTime = _time;

      if (_melee)
      {
        // Update UI
        _swang = true;
        _ragdoll._PlayerScript?._Profile.ItemUse(_side);
      }

      // Play sound
      //if(_customProjetile != UtilityScript.UtilityType.NONE) playSound = false;
      if (playSound)
      {
        PlaySound(Audio.GUN_SHOOT);
        if (IsGun())
        {
          EnemyScript.CheckSound(_ragdoll._Hip.position, _silenced ? EnemyScript.Loudness.SUPERSOFT : EnemyScript.Loudness.NORMAL);
        }
        else
        {
          EnemyScript.CheckSound(_ragdoll._Hip.position, EnemyScript.Loudness.SUPERSOFT);
        }
      }
    }

    // Check use conditions
    if (_fireMode == FireMode.BURST && _used)
    {
      if (CanUse() && (_time >= _useTime + _burstRate) && _bursts < _burstPerShot)
      {
        if (!_ragdoll._IsPlayer && _downTime > 0.2f || _ragdoll._IsPlayer)
        {
          Local_Use(_melee && _bursts > 0 ? false : true);
          if ((_clip == 0 && !_melee) || _bursts++ >= _burstPerShot - 1)
          {
            StopUse();
          }
        }
      }
    }
    else if (_bufferUse || (!_useOnRelease && _triggerDown) || (_useOnRelease && !_triggerDown && _triggerDown_last))
    {

      // If last use time is less than use rate, check fire rate
      if (CanUse())
      {
        _bufferUse = false;

        if (_fireMode == FireMode.BURST)
        {

          if (IsChargeWeapon() && _clip < _minimumClipToFire)
          {
            _clip = 0;
            _ragdoll._PlayerScript?._Profile.ItemSetClip(_side, _clip);
            PlayEmpty();
          }
          else
          {
            if (IsChargeWeapon()) _burstPerShot = _clip;
            _used = true;
            _ragdoll.RecoilSimple(_shoot_forward_force * _clip);
          }

          // Melee animation
          if (_melee)
          {
            if (_hitRagdolls == null)
              _hitRagdolls = new();
            else
              _hitRagdolls.Clear();

            _meleeReleaseTimer = 1f;
            if (_meleeLerper < 0.75f)
            {
              _meleeReleaseTimerOverfill = 1f;
            }

            switch (_type)
            {
              case ItemType.BAT:
                //case ItemType.SWORD:
                var totalTime = UseRate();
                var halfTime = totalTime * 0.2f;
                EasyAnimate(AnimData._Sword, totalTime, halfTime);
                break;
            }
          }
        }
        else
        {
          if (_fireMode == FireMode.SEMI) { _triggerDown = false; }
          Local_Use();
        }
        if (_melee)
        {

          switch (_type)
          {
            case ItemType.KATANA:
            case ItemType.BAT:
            case ItemType.RAPIER:
              _ragdoll.RecoilSimple(_downTimeSave > 1f ? -1.85f : -1.35f);
              break;
            case ItemType.KNIFE:
            case ItemType.AXE:
            case ItemType.FRYING_PAN:
              _ragdoll.RecoilSimple(-0.65f);
              break;
          }

          _IsSwinging = true;
        }
      }

      // Check buffer use
      else
      {
        if (_melee && _ragdoll._IsPlayer)
        {
          if (Time.time - _useTime <= 0.5f)
            _bufferUse = true;
          else
            Debug.Log($"mis-use time: {Time.time - _useTime}");
        }
      }
    }

    _triggerDown_last = _triggerDown;

    // Check for melee ui
    if (_swang && _time >= _useTime + UseRate() && !IsChargeWeapon())
    {
      _swang = false;
      _ragdoll._PlayerScript?._Profile.ItemReload(_side, 0);
    }
  }
  bool _bufferUse;

  //
  public void OnGrappled()
  {
    StopUse();
    _useTime = -1f;
  }

  //
  public static BulletScript SpawnBulletTowards(
    ActiveRagdoll source,
    Vector3 spawnPos,
    Vector3 shootDirNormalized,
    ItemType itemType,
    int penatrationAmount,

    bool randomSpread = false,
    float bulletSpread = 0f,
    int projectilesPerShot = 1,
    int bulletIter = 0
  )
  {
    var newBullet = _BulletPool[_BulletPool_Iter++ % _BulletPool.Length];
    newBullet.gameObject.SetActive(true);

    // Size
    var use_size = Mathf.Clamp(0.9f + penatrationAmount * 0.2f, 0.9f, 2.5f);
    if (itemType == ItemType.FLAMETHROWER) use_size = 2.1f;
    else if (itemType == ItemType.ROCKET_FIST) use_size = 4.5f;

    //
    newBullet.SetSize(use_size);
    newBullet.Reset(source, spawnPos);

    // Laser
    var bulletSpeedMod = 1f;
    if (itemType == ItemType.CHARGE_PISTOL)
    {
      newBullet.SetColor(new Color(1f, 0.5f, 0.5f), Color.red);
      newBullet.SetLifetime(0.04f);
      newBullet.SetNoise(0.1f, 0.1f);
      bulletSpeedMod = 1.25f;
    }

    // Normal bullet
    else
    {
      newBullet.SetColor(new Color(1f, 0.9f, 0f), new Color(1f, 0.07f, 0f));
      newBullet.SetLifetime(0.05f);
      newBullet.SetNoise(0.25f, 0.5f);
    }

    // Physics
    var bulletRb = newBullet._rb;
    bulletRb.velocity = Vector3.zero;
    bulletRb.transform.position = spawnPos;
    var speedMod = 0.5f * (itemType == ItemType.FLAMETHROWER ? 1f : (penatrationAmount > 0 ? 1.35f : 1f)) + (bulletIter == 0 ? 0f : (UnityEngine.Random.value * 0.15f) - 0.075f);
    speedMod *= bulletSpeedMod;
    var addforce = Vector3.zero;
    if (projectilesPerShot > 1)
    {
      float mod = 1f;
      if (bulletIter % 2 == 1) mod = -1f;
      if (!randomSpread && bulletIter == 0 && projectilesPerShot % 2 == 1) mod = 0f;
      addforce = Quaternion.AngleAxis(90f, Vector3.up) * shootDirNormalized * bulletSpread * (randomSpread ? Random.value : 1f) * mod;
    }
    var bulletForce = MathC.Get2DVector(shootDirNormalized + addforce) * 2100f * speedMod;
    bulletRb.transform.LookAt(bulletRb.position + bulletForce);
    bulletRb.AddForce(bulletForce);

    newBullet.OnShot(penatrationAmount, bulletForce.magnitude);

    // Bullet casing
    if (
      itemType != ItemType.CROSSBOW &&
      itemType != ItemType.FLAMETHROWER &&
      itemType != ItemType.ROCKET_FIST &&
      (projectilesPerShot <= 2 || bulletIter == 0)
    )
    {
      var bullet_casing = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_CASING)[0];
      var bulletRot = bullet_casing.transform.rotation;
      bulletRot.eulerAngles = new Vector3(bulletRot.eulerAngles.x, Random.value * 360f, bulletRot.eulerAngles.z);
      bullet_casing.transform.rotation = bulletRot;
      var emitParams = new ParticleSystem.EmitParams
      {
        position = spawnPos + Vector3.up * 0.5f,
        rotation3D = bulletRot.eulerAngles
      };
      bullet_casing.Emit(emitParams, 1);
      SfxManager.PlayAudioSourceSimple(spawnPos, "Ragdoll/Bullet_Casing", 0.8f, 1.2f);

      // Gun embers
      var ps_gunsmoke = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.GUN_FIRE)[0];
      //Debug.Log(ps_gunsmoke.Length);
      //foreach (var psGunFire in ps_gunsmoke)
      {
        ps_gunsmoke.transform.position = spawnPos + shootDirNormalized * (0.3f + (
          itemType == ItemType.DMR ||
          itemType == ItemType.AK47 ||
          itemType == ItemType.M16 ||
          itemType == ItemType.RIFLE ||
          itemType == ItemType.RIFLE_LEVER ||
          itemType == ItemType.ROCKET_LAUNCHER ||
          itemType == ItemType.SHOTGUN_BURST ||
          itemType == ItemType.GRENADE_LAUNCHER ||
          itemType == ItemType.SHOTGUN_PUMP
          ? 0.35f : 0f));
        ps_gunsmoke.transform.LookAt(spawnPos + shootDirNormalized);
        //var mainmodule = p_gunsmoke.main;
        //mainmodule.emitterVelocity = source._head?.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero;
        ps_gunsmoke.Play();
      }
    }

    //
    return newBullet;
  }

  //
  void StopUse()
  {
    _bursts = 0;
    _used = false;
    if (!IsChargeWeapon() && !_useOnRelease) _triggerDown = false;

    // If melee, stop swinging
    if (_melee)
    {
      _IsSwinging = false;
      _meleeStartSwinging = false;

      // If hit anything and two handed, allow to use immediatly
      if ((_hitAnthing && _IsBulletDeflector) || _hitAnthingOverride)
        _useTime = -1f;
    }
  }

  //
  public void MeleeDamageOther(ActiveRagdoll ragdoll)
  {

    // Damage
    var hitForce = MathC.Get2DVector(-(_ragdoll._Hip.position - ragdoll._Hip.position).normalized * (3750f + (Random.value * 1750f)) * _hit_force);
    ragdoll.TakeDamage(
       new ActiveRagdoll.RagdollDamageSource()
       {
         Source = _ragdoll,

         HitForce = hitForce,

         Damage = 1,
         DamageSource = _ragdoll._Hip.position,
         DamageSourceType = ActiveRagdoll.DamageSourceType.MELEE,

         SpawnBlood = true,
         SpawnGiblets = _dismember
       });

    // Dismember
    if (ragdoll._IsDead && _dismember)
      ragdoll.DismemberRandomTimes(hitForce, 3);
  }

  //
  public bool IsChargeWeapon()
  {
    return _useOnRelease && _chargeHold;
  }

  void EasyAnimate(AnimData data, float totalTime, float halfTime)
  {
    AnimationData.Animate(this, data._start, data._end, transform.GetChild(0), totalTime, halfTime);

  }

  // Equip this item to ragdoll on side, utilizing cached item data if passed (weapon pair swapping)
  public void OnEquip(ActiveRagdoll ragdoll, ActiveRagdoll.Side side, int clipSize = -1, float useTime = -1f, int itemId = -1)
  {
    _ItemId = s_itemID++;

    _ragdoll = ragdoll;
    _side = side;

    // Check for laser sight
    if (!_melee && _ragdoll._IsPlayer && _ragdoll._PlayerScript.HasPerk(Shop.Perk.PerkType.LASER_SIGHTS))
      AddLaserSight();

    // Set clip and use time if applicable
    SetClip(clipSize);
    _useTime = useTime;

    // Gather custom utilities
    if (itemId != -1)
    {
      if (_type == ItemType.STICKY_GUN)
        UtilityScript.ParentUtilitiesById(itemId, _ItemId, UtilityScript.UtilityType.STICKY_GUN_BULLET);
    }

    /*/ Gather bullets
    if(!_melee){
      foreach(var bullet in _BulletPool){
        if(!bullet.gameObject.activeSelf)continue;
        if(bullet.)
      }
    }*/

    // Save arm rots
    _original_rot_lower = _arm_lower.localEulerAngles;
    _original_rot_upper = _arm_upper.localEulerAngles;

    // Set arm pos per weapon
    if (_type == ItemType.KATANA || _type == ItemType.BAT)
    {
      AnimData.Init();
      AnimData._Bat._start.Set(_ragdoll, transform.GetChild(0));
    }
    // Check everything else
    else
    {
      // Upper arm
      SetRotationLocal(_arm_upper, new Vector3(
        29f,
        _side == ActiveRagdoll.Side.LEFT ? -50f : -21f,
        (_side == ActiveRagdoll.Side.LEFT ? 1f : -1f) * 73f
      ));

      // Lower arm
      SetRotationLocal(_arm_lower, new Vector3(
        0f,
        0f,
        (_side == ActiveRagdoll.Side.LEFT ? 1f : -1f) * 7f
      ));
    }

    // Save new rotation
    _save_rot_lower = _arm_lower.localEulerAngles;
    _save_rot_upper = _arm_upper.localEulerAngles;

    // Scale
    transform.localScale = new Vector3(1f, 1f, 1f);

    // Set rotation
    var dir = _ragdoll._Hip.transform.forward;
    dir.y = 0f;
    transform.forward = dir;
    //transform.Rotate(new Vector3(0f, 1f, 0f) * (side == ActiveRagdoll.Side.LEFT ? -2f : 2.25f));
    // Set position
    Vector3 dist = transform.parent.position - _handle.position;
    transform.position += dist;

    // Check zombie
    if (_isZombie)
    {
      _burstPerShot = 3;
    }
  }

  public void OnUnequip()
  {
    _arm_lower.localEulerAngles = _original_rot_lower;
    _arm_upper.localEulerAngles = _original_rot_upper;
  }

  class AnimationData
  {
    public Vector3 _arm_lower_l, _arm_upper_l,
      _arm_lower_r, _arm_upper_r,
      _spine_upper,
      _item_mesh;

    public void Set(ActiveRagdoll ragdoll, Transform item_mesh)
    {
      SetRotationLocal(ragdoll._transform_parts._arm_lower_l, _arm_lower_l);
      SetRotationLocal(ragdoll._transform_parts._arm_upper_l, _arm_upper_l);
      SetRotationLocal(ragdoll._transform_parts._arm_lower_r, _arm_lower_r);
      SetRotationLocal(ragdoll._transform_parts._arm_upper_r, _arm_upper_r);
      SetRotationLocal(ragdoll._transform_parts._spine, _spine_upper);
      SetRotationLocal(item_mesh, _item_mesh);
    }

    public static void Animate(ItemScript item, AnimationData start, AnimationData end, Transform item_mesh, float totalTime, float halfTime)
    {
      if (item._anims[0] != null) item.StopCoroutine(item._anims[0]);
      item._anims[0] = item.AnimateTransformSimple(item._ragdoll._transform_parts._arm_lower_l, start._arm_lower_l, end._arm_lower_l, totalTime, halfTime, () => { item._anims[0] = null; });
      if (item._anims[1] != null) item.StopCoroutine(item._anims[1]);
      item._anims[1] = item.AnimateTransformSimple(item._ragdoll._transform_parts._arm_upper_l, start._arm_upper_l, end._arm_upper_l, totalTime, halfTime, () => { item._anims[1] = null; });
      if (item._anims[2] != null) item.StopCoroutine(item._anims[2]);
      item._anims[2] = item.AnimateTransformSimple(item._ragdoll._transform_parts._arm_lower_r, start._arm_lower_r, end._arm_lower_r, totalTime, halfTime, () => { item._anims[2] = null; });
      if (item._anims[3] != null) item.StopCoroutine(item._anims[3]);
      item._anims[3] = item.AnimateTransformSimple(item._ragdoll._transform_parts._arm_upper_r, start._arm_upper_r, end._arm_upper_r, totalTime, halfTime, () => { item._anims[3] = null; });
      if (item._anims[4] != null) item.StopCoroutine(item._anims[4]);
      item._anims[4] = item.AnimateTransformSimple(item._ragdoll._transform_parts._spine, start._spine_upper, end._spine_upper, totalTime, halfTime, () => { item._anims[4] = null; });
      if (item._anims[5] != null) item.StopCoroutine(item._anims[5]);
      item._anims[5] = item.AnimateTransformSimple(item_mesh, start._item_mesh, end._item_mesh, totalTime, halfTime * 1.2f, () => { item._anims[5] = null; });
    }
  }

  class AnimData
  {
    // Instance data
    public AnimationData _start, _end;

    // Static data
    public static AnimData _Sword, _Bat;

    static List<AnimData> _AnimDatas;
    static public void Init()
    {
      if (_AnimDatas == null)
      {
        _AnimDatas = new List<AnimData>();

        _Sword = new AnimData()
        {
          _start = new AnimationData()
          {
            _arm_lower_l = new Vector3(-50f, -30f, 0f),
            _arm_upper_l = new Vector3(-70f, -10f, 0f),
            _arm_lower_r = new Vector3(-60f, 0f, 0f),
            _arm_upper_r = new Vector3(-30f, -40f, 0f),
            _spine_upper = new Vector3(-5f, 65f, 0f),
            _item_mesh = new Vector3(-50f, 10f, 150f)
          },
          _end = new AnimationData()
          {
            _arm_lower_l = new Vector3(-20f, 0f, 0f),
            _arm_upper_l = new Vector3(-70f, 90f, 0f),
            _arm_lower_r = new Vector3(-60f, 5f, 0f),
            _arm_upper_r = new Vector3(-30f, -10f, 0f),
            _spine_upper = new Vector3(-5f, -10f, 0f),
            _item_mesh = new Vector3(-50f, -55f, 200f)
          }
        };
        _AnimDatas.Add(_Sword);

        _Bat = new AnimData()
        {
          _start = new AnimationData()
          {
            _arm_lower_l = new Vector3(-50f, -30f, 0f),
            _arm_upper_l = new Vector3(-70f, -10f, 0f),
            _arm_lower_r = new Vector3(-60f, 0f, 0f),
            _arm_upper_r = new Vector3(-30f, -40f, 0f),
            _spine_upper = new Vector3(-5f, 65f, 0f),
            _item_mesh = new Vector3(-50f, 10f, 150f)
          },
          _end = new AnimationData()
          {
            _arm_lower_l = new Vector3(-20f, 0f, 0f),
            _arm_upper_l = new Vector3(-70f, 90f, 0f),
            _arm_lower_r = new Vector3(-60f, 5f, 0f),
            _arm_upper_r = new Vector3(-30f, -10f, 0f),
            _spine_upper = new Vector3(-5f, -10f, 0f),
            _item_mesh = new Vector3(-50f, -55f, 200f)
          }
        };
        _AnimDatas.Add(_Bat);
      }
    }
  }

  static void SetRotationLocal(Transform t, Vector3 euler)
  {
    t.localEulerAngles = euler;
  }

  void ResetV()
  {
    _meleeIter = 0;
    _hitAnthing = false;
    _hitAnthingOverride = false;
    _hasHitEnemy = false;
    _damageAnything = false;
  }

  //
  public virtual void UseDown()
  {
    //_ragdoll._playerScript?.ResetLoadout();
    //if (this == null) return;

    _triggerDown = true;
    _triggerDownReal = true;

    _upTime = 0f;
    _downTime = 0.01f;

    if (!_useOnRelease)
      ResetV();

    // Check timer
    if (!PlayerScript._TimerStarted && (_ragdoll?._IsPlayer ?? false))
    {
      PlayerScript.StartLevelTimer();
    }

    //
    Update();
  }
  public void UseUp()
  {
    //_ragdoll._playerScript?.ResetLoadout();
    //if (this == null) return;

    _triggerDown = false;
    _triggerDownReal = false;

    _upTime = 0.01f;
    _downTimeSave = _downTime;
    _downTime = 0f;
    _meleeReleaseTrigger = false;

    if (_useOnRelease)
      ResetV();
  }
  // Can you use the item?
  public bool CanUse()
  {
    if (_melee)
      return (_time >= _useTime + UseRate()) || (_bursts > 0 && _fireMode == FireMode.BURST && _time >= _useTime + _burstRate);

    // Check clip
    if (_clip > 0)
      return ((_time >= _useTime + UseRate()) || (_fireMode == FireMode.BURST && _bursts > 0 && _time >= _useTime + _burstRate)) && (!_used || _fireMode == FireMode.BURST) && (!_reloading || IsChargeWeapon());
    PlayEmpty();
    return false;
  }

  void PlayEmpty()
  {
    PlaySound(Audio.GUN_EMPTY);
    EnemyScript.CheckSound(_ragdoll._Hip.position, EnemyScript.Loudness.SUPERSOFT);
    _triggerDown = false;
  }

  public void PlayBulletHit()
  {
    PlaySound(Audio.MELEE_HIT_BULLET, 0.8f, 1.2f);
    EnemyScript.CheckSound(_ragdoll._Hip.position, EnemyScript.Loudness.SOFT);
  }

  // Reload function
  public void Reload()
  {
    // If melee, do nothing
    if (_melee && !IsChargeWeapon()) return;

    // Check perk
    var reload_speed_mod = 1f;
    if (_ragdoll._IsPlayer && _ragdoll._PlayerScript.HasPerk(Shop.Perk.PerkType.FASTER_RELOAD))
      reload_speed_mod = 1.4f;

    // Play noise and set clip
    PlaySound(Audio.GUN_RELOAD, reload_speed_mod - 0.1f, reload_speed_mod + 0.1f);
    if (_ragdoll._IsPlayer) EnemyScript.CheckSound(_ragdoll._Hip.position, EnemyScript.Loudness.SUPERSOFT);
    var clipSave = _clip;
    _clip = Mathf.Clamp(
      _reloadOneAtTime ? _clip + (_type == ItemType.CHARGE_PISTOL ? 2 : 1) : GetClipSize(),
      0,
      GetClipSize()
    );
    var clipDiff = _clip - clipSave;

    // Check special
    if (_type == ItemType.STICKY_GUN)
      UtilityScript.Detonate_UtilitiesById(this, UtilityScript.UtilityType.STICKY_GUN_BULLET, _triggerDownReal ? 1 : -1);

    // Show progress bar
    var reloadTime = _reloadTime / reload_speed_mod;
    _reloading = true;
    ProgressBar.GetProgressBar(transform, reloadTime,
      () =>
      {
        _reloading = false;
        if (!IsChargeWeapon()) _ragdoll._PlayerScript?._Profile.ItemReload(_side, _reloadOneAtTime ? clipDiff : 0);
      },
      (ProgressBar.instance instance) =>
      {
        if (_ragdoll._IsDead)
          instance._enabled = false;

        /*/ Drop magazine
        if (_last_magazineDropId != instance._id && instance._timer / instance._timer_start < 0.6f)
        {
          _last_magazineDropId = instance._id;

          var magazine = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.MAGAZINE)[0];
          var q = magazine.transform.rotation;
          q.eulerAngles = new Vector3(q.eulerAngles.x, Random.value * 360f, q.eulerAngles.z);
          magazine.transform.rotation = q;
          var p = new ParticleSystem.EmitParams();
          p.position = transform.position + Vector3.up * 0.5f;
          p.rotation3D = q.eulerAngles;
          magazine.Emit(p, 1);
        }*/
      });

    // Charge weapon
    if (IsChargeWeapon()) _ragdoll._PlayerScript?._Profile.ItemReload(_side, _reloadOneAtTime ? 1 : 0);

    // Reload animation
    float totalTime = reloadTime,
      halfTime = totalTime * 0.5f,
      side_mod = (_side == ActiveRagdoll.Side.LEFT ? -1f : 1f);
    if (_reloadTime > 1f)
    {
      AnimateTransformAdditive(_arm_lower, _save_rot_lower, -60f, 0f, 20f * -side_mod, totalTime * 0.7f, halfTime * 0.7f,
        () =>
        {
          AnimateTransformAdditive(_arm_lower, _save_rot_lower, 30f, 0f, 10f * -side_mod, totalTime * 0.3f, halfTime * 0.3f);
        });
    }
    else
      AnimateTransformAdditive(_arm_lower, _save_rot_lower, -60f, 0f, 20f * -side_mod, totalTime, halfTime);

    // Reload custom projectiles
    ReloadCustom();
  }
  // Can the item reload?
  public bool CanReload()
  {
    if (_melee && !IsChargeWeapon()) return false;
    if (!_melee && _useOnRelease && !IsChargeWeapon() && _triggerDownReal) return false;
    return _clip < GetClipSize() && ((_time >= _useTime + (UseRate() * 0.6f)) || (_bursts > 0 && _fireMode == FireMode.BURST && _time >= _useTime + _burstRate * 0.3f)) && !_used && !_reloading;
  }
  public bool NeedsReload()
  {
    if (IsChargeWeapon()) return false;
    return !_melee && _clip == 0;
  }
  public void SetClip(int clip = -1)
  {
    if (_melee || _chargeHold) return;
    _clip = clip == -1 ? GetClipSize() : clip;
  }

  // Reload custom projectiles
  public void ReloadCustom()
  {
    if (_customProjetile != UtilityScript.UtilityType.NONE)
    {

      // Delete old
      if (_customProjectiles != null)
        for (var i = _customProjectiles.Length - 1; i >= 0; i--)
        {
          var projectile = _customProjectiles[i];
          if (projectile == null || (projectile.Thrown() && projectile.Explosive())) continue;
          GameObject.Destroy(projectile.gameObject);
        }

      // Spawn in new
      _customProjectiles = new UtilityScript[_clipSize * _projectilesPerShot];
      for (var i = 0; i < _customProjectiles.Length; i++)
      {
        _customProjectiles[i] = UtilityScript.GetUtility(_customProjetile);
        _customProjectiles[i].RegisterUtility(_ragdoll, false);
        _customProjectiles[i].RegisterCustomProjectile(this);
      }
      _customProjectileIter = 0;
    }
  }

  public bool IsGun()
  {
    return !_melee && !_throwable;
  }

  public bool IsMelee()
  {
    return _melee;
  }
  public bool IsEmpty()
  {
    return _type == ItemType.NONE;
  }

  public bool IsThrowable()
  {
    return _throwable;
  }

  // Fired on user death
  AudioSource _sfx_hold;
  public void OnToggle()
  {

    // Stop sfx if playing
    if (_sfx_hold?.isPlaying ?? false)
      _sfx_hold.Stop();

    // Stop custom components
    if (_customParticles != null)
      foreach (var particle in _customParticles)
        particle.Stop();
    if (_customLights != null)
      foreach (var light in _customLights)
        light.enabled = false;

    // Hide colliders
    if (_type == ItemType.FRYING_PAN)
    {
      transform.GetChild(0).GetComponent<BoxCollider>().enabled = false;
    }
  }

  class Item
  {

    //public Item(ItemScript script)
    //{
    //
    //}

    public System.Action _OnUse;
  }

  public int Clip()
  {
    if (_melee) return 1;
    return _clip;
  }

  static Item _ITEM_Knife = new Item()
  {
    _OnUse = () =>
    {

    }
  };

  public class RaycastInfo
  {
    public RaycastHit _raycastHit;
    public Vector3 _hitPoint;
    public ActiveRagdoll _ragdoll;

    public RaycastInfo()
    {
      _raycastHit = new RaycastHit();
      _hitPoint = Vector3.zero;
      _ragdoll = null;
    }
  }

  bool MeleeCast(RaycastInfo raycastInfo, int iter)
  {
    _ragdoll.ToggleRaycasting(false);
    _ragdoll._grappler?.ToggleRaycasting(false);

    var add = Vector3.zero;
    switch (iter % 3)
    {
      case 1:
        add = _ragdoll._Hip.transform.right;
        break;
      case 2:
        add = -_ragdoll._Hip.transform.right;
        break;
    }

    // Cast ray from startPos to dir (cast a ray from the handle forwards)
    var forward = _ragdoll._Controller.forward;
    forward.y = 0f;
    var ray = new Ray(
      _ragdoll._Hip.position - _ragdoll._Hip.transform.forward * 0.1f + Vector3.up * 0.4f + add * 0.1f,
      forward + -add * (_twoHanded ? 1f : 0.2f)
    );
    var hit = false;
    var canMeleePenatrate = _canMeleePenatrate && (_ragdoll._EnemyScript?._IsZombieReal ?? true);
    var maxDistance = (!_ragdoll._IsPlayer && _ragdoll._EnemyScript._IsZombieReal) ? 0.25f : 0.6f * (canMeleePenatrate ? 1.3f : 1f);
    if (_type == ItemType.RAPIER)
      maxDistance *= 2.4f;
    if (Physics.SphereCast(ray, Mathf.Clamp(0.22f, 0.05f, maxDistance), out raycastInfo._raycastHit, maxDistance, GameResources._Layermask_Ragdoll))
    {
      //Debug.Log(raycastInfo._raycastHit.collider.gameObject.name);
      //Debug.DrawLine(ray.origin, raycastInfo._raycastHit.point, Color.red, 5f);

      if (raycastInfo._raycastHit.collider.gameObject.name == "Books")
      {
        FunctionsC.BookManager.ExplodeBooks(raycastInfo._raycastHit.collider, _ragdoll._Hip.position);
      }
      else
      {
        raycastInfo._ragdoll = ActiveRagdoll.GetRagdoll(raycastInfo._raycastHit.collider.gameObject);
        if (raycastInfo._ragdoll != null && !raycastInfo._ragdoll._IsDead)
          hit = true;
      }
      raycastInfo._hitPoint = raycastInfo._raycastHit.point;
    }

    //
    _ragdoll.ToggleRaycasting(true);
    _ragdoll._grappler?.ToggleRaycasting(true);

    //
    return hit;
  }

  // Animation for swinging / reloading
  Coroutine AnimateTransform(Transform t, Vector3 startEulerRot, Vector3 desiredEulerRot, float totalTime, System.Action onEnd = null)
  {
    IEnumerator AnimateCo()
    {
      var timer = 0f;
      while (timer < totalTime)
      {
        yield return null;
        timer += Time.deltaTime;
        SetRotationLocal(t, Vector3.Lerp(startEulerRot, desiredEulerRot, timer / totalTime));
      }

      // Return to original rot
      SetRotationLocal(t, desiredEulerRot);

      // Fire action
      onEnd?.Invoke();
    }
    return StartCoroutine(AnimateCo());
  }
  Coroutine AnimateTransformSimple(Transform t, Vector3 startEulerRot, Vector3 desiredEulerRot, float totalTime, float halfwayTime, System.Action onEnd = null)
  {
    return AnimateTransform(t, startEulerRot, desiredEulerRot, halfwayTime, () =>
    {
      AnimateTransform(t, desiredEulerRot, startEulerRot, totalTime - halfwayTime, onEnd);
    });
  }
  Coroutine AnimateTransformAdditive(Transform t, Vector3 startEulerRot, float x, float y, float z, float totalTime, float halfwayTime, System.Action onEnd = null)
  {
    var desiredRotEuler = startEulerRot + new Vector3(x, y, z);
    return AnimateTransformSimple(t, startEulerRot, desiredRotEuler, totalTime, totalTime / 5f, onEnd);
  }
}