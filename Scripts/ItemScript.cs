using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using ItemType = GameScript.ItemManager.Items;

public class ItemScript : MonoBehaviour
{
  // Information about weapon holder
  public ActiveRagdoll _ragdoll;
  ActiveRagdoll.Side _side;

  public int _clipSize, _minimumClipToFire, _projectilesPerShot, _burstPerShot, _penatrationAmount;
  public bool _reloading, _melee, _twoHanded, _runWhileUse, _reloadOneAtTime, _silenced, _throwable, _dismember, _useOnRelease, _randomSpread, _chargeHold;
  public float _useTime, _reloadTime, _useRate, _downTime, _burstRate, _hit_force, _bullet_spread, _shoot_force, _shoot_forward_force;
  public FireMode _fireMode;

  public UtilityScript.UtilityType _customProjetile;
  public UtilityScript[] _customProjectiles;
  int _customProjectileIter;
  float _customProjectileVelocityMod;

  int _last_magazineDropId;

  // Custom components
  ParticleSystem[] _customParticles;
  Light[] _customLights;

  protected bool _disableOnRagdollDeath;

  public int ClipSize()
  {
    if (_melee && !IsChargeWeapon()) return 1;

    var clip_size = _clipSize;

    // Check perk
    if (_ragdoll != null && _ragdoll._isPlayer && _ragdoll._playerScript.HasPerk(Shop.Perk.PerkType.MAX_AMMO_UP))
      clip_size = Mathf.RoundToInt(_clipSize * 1.5f);

    // Check extra
    if (_ragdoll._isPlayer)
      switch (GameScript.Settings._Extra_PlayerAmmo._value)
      {
        case 1:
          clip_size = Mathf.RoundToInt(_clipSize * 2f);
          break;
        case 2:
          clip_size = Mathf.RoundToInt(_clipSize * 0.5f);
          break;
      }

    return clip_size;
  }
  public float UseRate()
  {
    if (!_melee && _ragdoll != null && _ragdoll._isPlayer && Shop.Perk.HasPerk(_ragdoll._playerScript._id, Shop.Perk.PerkType.FIRE_RATE_UP))
      return _useRate * 0.5f;
    return _useRate;
  }

  bool _meleePenatrate { get { return _twoHanded || _type == ItemType.AXE || _type == ItemType.ROCKET_FIST; } }

  float _time { get { return /*_ragdoll._isPlayer_twoHanded ? Time.unscaledTime : */Time.time; } }

  public bool _swinging;

  bool _swang;

  //
  protected int _clip, _bursts, _meleeIter;
  protected bool _triggerDown, _triggerDown_last, _used, _hitAnthing, _hitEnemy;

  public void HitSomething()
  {
    if (_swinging) { _hitAnthing = true; }
  }

  public bool Used() { return _used; }

  protected System.Action _onUse, _onUpdate;

  public Transform _handle, _forward;

  // Check for perk
  public int GetPenatrationAmount()
  {
    if (_ragdoll._isPlayer && Shop.Perk.HasPerk(_ragdoll._playerScript._id, Shop.Perk.PerkType.PENETRATION_UP))
      return _penatrationAmount + 1;
    return _penatrationAmount;
  }

  Transform _arm_lower { get { return transform.parent.parent; } }
  Transform _arm_upper { get { return _arm_lower.parent; } }
  Vector3 _save_rot_lower, _save_rot_upper,
    _original_rot_lower, _original_rot_upper;

  // Audio
  public AudioClip[] _sfx_clip;
  public float[] _sfx_volume;

  public ItemType _type;

  protected AudioSource[] _audioSources;

  List<int> _hitRagdolls;

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
    UTILITY_THROW,
    UTILITY_HIT_FLOOR,
    UTILITY_ACTION,
    UTILITY_EXTRA
  }

  Coroutine[] _anims;

  // Play SFX via enum
  protected int GetAudioSource(Audio audio)
  {
    if (_audioSources == null) return -1;

    int iter = 0;
    switch (audio)
    {
      case Audio.GUN_SHOOT:
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

    if (iter >= _audioSources.Length) return -1;

    return iter;
  }
  // Play SFX via enum
  protected void PlaySound(Audio audioType, float pitchMin = 0.9f, float pitchMax = 1.1f)
  {
    PlaySound(GetAudioSource(audioType), pitchMin, pitchMax);
  }
  protected void PlaySound(int source_index, float pitchMin = 0.9f, float pitchMax = 1.1f)
  {
    if (source_index == -1) return;

    var source = _audioSources[source_index];
    source.clip = _sfx_clip[source_index];
    source.volume = _sfx_volume[source_index];

    FunctionsC.ChangePitch(ref source, pitchMin, pitchMax);
    FunctionsC.PlayOneShot(source);
  }

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
    if (_laserSight) GameObject.Destroy(_laserSight);
  }

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
      var bullets = new GameObject();
      bullets.name = "Bullets";
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
      /*/ Ignore bullet collisions
      foreach (var c0 in rbs)
        foreach (var c1 in rbs)
          if (c0.GetInstanceID() == c1.GetInstanceID())
            continue;
          else
            Physics.IgnoreCollision(c0, c1, true);*/
    }

    // Get item sounds
    _audioSources = new AudioSource[_sfx_clip.Length];
    for (var i = 0; i < _audioSources.Length; i++)
    {
      _audioSources[i] = gameObject.AddComponent<AudioSource>();
      _audioSources[i].playOnAwake = false;
      _audioSources[i].spatialBlend = 0.7f;
    }

    // Check special types
    if (_type == ItemType.FLAMETHROWER)
    {
      // Set audio
      _audioSources[3].clip = _sfx_clip[3];
      _audioSources[3].volume = _sfx_volume[3];
      _audioSources[3].loop = true;

      _audioSources[4].clip = _sfx_clip[4];
      _audioSources[4].volume = _sfx_volume[4];
      _audioSources[4].loop = true;

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
          if (!_ragdoll._dead)
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
        var hit = MeleeCast(raycastInfo, _meleeIter++);

        // Check for already hit ragdoll or grapple
        if (hit)
        {
          if (_hitRagdolls.Contains(raycastInfo._ragdoll._id))
            hit = false;

          if (_ragdoll._grappler != null && raycastInfo._ragdoll._id == _ragdoll._grappler._id)
            hit = false;
        }

        var damageSource = _ragdoll;
        if (hit)
        {
          //bool hitEnemy = !raycastInfo._ragdoll._isPlayer;
          // If is enemy, and isn't two handed, don't kill friendlies
          if (!_ragdoll._isPlayer && !_meleePenatrate && !raycastInfo._ragdoll._isPlayer && !raycastInfo._ragdoll._grappled) return;
          // If is player v player, is two handed, and hit enemy before, dont hit
          if (_ragdoll._isPlayer && raycastInfo._ragdoll._isPlayer && _meleePenatrate && _hitEnemy) return;
          // If is enemy and target is player, and player is swinging, dont hit
          if (!_ragdoll._isPlayer && raycastInfo._ragdoll._isPlayer && raycastInfo._ragdoll._swinging)
          {
            //FunctionsC.PlaySound(ref raycastInfo._ragdoll._audioPlayer_steps, "Ragdoll/Deflect");
            return;
          }
          // Check grappler
          if (raycastInfo._ragdoll._grappling)
          {

            // Get dir from target to swinger; if hostage is held in front of knife, kill hostage
            var dir = (raycastInfo._ragdoll._hip.position - _ragdoll._hip.position).normalized;
            var target_frwd = raycastInfo._ragdoll._controller.forward;

            if ((dir - target_frwd).magnitude > 1f)
            {
              damageSource = raycastInfo._ragdoll;
              raycastInfo._ragdoll = raycastInfo._ragdoll._grapplee;
              return;
            }
          }

          _hitAnthing = true;
          if (!_hitEnemy && !raycastInfo._ragdoll._isPlayer) _hitEnemy = true;

          // Record ragdoll id to stop multiple hits
          _hitRagdolls.Add(raycastInfo._ragdoll._id);

          // Damage
          var force = MathC.Get2DVector(-(_ragdoll._hip.position - raycastInfo._raycastHit.point).normalized * (4000f + (Random.value * 2000f)) * _hit_force);
          raycastInfo._ragdoll.TakeDamage(damageSource, ActiveRagdoll.DamageSourceType.MELEE, force, _ragdoll._hip.position, 1, true);
          if (raycastInfo._ragdoll._dead && _dismember)
          {
            raycastInfo._ragdoll.DismemberRandomTimes(force, 3);
          }

          // Play noise
          PlaySound(Audio.MELEE_HIT);
          EnemyScript.CheckSound(_ragdoll._hip.transform.position, EnemyScript.Loudness.SUPERSOFT);

          // If not two handed, stop checking
          if (!_meleePenatrate)
            _bursts = _burstPerShot + 1;
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
          var bullet = _BulletPool[_BulletPool_Iter++ % _BulletPool.Length];
          var spawn_pos = _forward.position;
          if (_type == ItemType.ROCKET_FIST) spawn_pos = _ragdoll._hip.position;
          var new_position = new Vector3(spawn_pos.x, _ragdoll._spine.transform.position.y, spawn_pos.z);
          bullet.gameObject.SetActive(true);
          var use_size = use_penatrationAmount > 0 ? use_penatrationAmount > 3 ? 3.5f : 1.2f : 1f;
          if (_type == ItemType.FLAMETHROWER) use_size = 2.1f;
          else if (_type == ItemType.ROCKET_FIST) use_size = 4.5f;
          bullet.SetSize(use_size);
          bullet.Reset(this, new_position);
          rb = bullet._rb;

          rb.velocity = Vector3.zero;
          rb.position = new_position;
          var f = MathC.Get2DVector(transform.forward).normalized;
          var speedMod = 0.5f * (_type == ItemType.FLAMETHROWER ? 1f : (use_penatrationAmount > 0 ? 1.35f : 1f)) + (i == 0 ? 0f : (UnityEngine.Random.value * 0.15f) - (0.075f));
          var addforce = Vector3.zero;
          if (_projectilesPerShot > 1)
          {
            float mod = 1f;
            if (i % 2 == 1) mod = -1f;
            if (!_randomSpread && i == 0 && _projectilesPerShot % 2 == 1) mod = 0f;
            addforce = transform.right * _bullet_spread * (_randomSpread ? Random.value : 1f) * mod;
          }
          var force = MathC.Get2DVector(f + addforce) * 2100f * speedMod;
          rb.transform.LookAt(rb.position + force);
          rb.AddForce(force);

          bullet.OnShot(use_penatrationAmount, force.magnitude);

          // Bullet case
          if ((_type != ItemType.CROSSBOW && _type != ItemType.FLAMETHROWER && _type != ItemType.ROCKET_FIST) && (_projectilesPerShot <= 2 || i == 0))
          {
            var bullet_casing = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_CASING)[0];
            var q = bullet_casing.transform.rotation;
            q.eulerAngles = new Vector3(q.eulerAngles.x, Random.value * 360f, q.eulerAngles.z);
            bullet_casing.transform.rotation = q;
            var p = new ParticleSystem.EmitParams();
            p.position = transform.position + Vector3.up * 0.5f;
            p.rotation3D = q.eulerAngles;
            bullet_casing.Emit(p, 1);
            FunctionsC.PlaySound(ref _ragdoll._audioPlayer_extra, "Ragdoll/Bullet_Casing", 0.8f, 1.2f);

            // Barrel smoke fx
            var ps_gunsmoke = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.GUN_SMOKE);
            foreach (var p_gunsmoke in ps_gunsmoke)
            {
              p_gunsmoke.transform.position = transform.position + transform.forward * (0.3f + (_type == ItemType.DMR || _type == ItemType.AK47 || _type == ItemType.M16 || _type == ItemType.RIFLE || _type == ItemType.RIFLE_LEVER || _type == ItemType.ROCKET_LAUNCHER || _type == ItemType.SHOTGUN_BURST || _type == ItemType.SHOTGUN_PUMP ? 0.35f : 0f));
              p_gunsmoke.transform.LookAt(transform.position + transform.forward);
              p_gunsmoke.Play();
            }
          }
        }
        // Custom projectile
        else
        {
          // Spawn projectiles
          if (_customProjectileIter >= _customProjectiles.Length || _customProjectiles[_customProjectileIter] == null)
            ReloadCustom();
          // Spawn utility
          UtilityScript utility = _customProjectiles[_customProjectileIter++];
          utility._side = _side;
          utility.SetSpawnLocation(new Vector3(_forward.position.x, _ragdoll._spine.transform.position.y, _forward.position.z) - _ragdoll._spine.transform.forward * 0.5f);
          utility.UseDown();
          utility.UseUp();
          // Set custom velocity
          _customProjectileVelocityMod = 1f;
          if (_type == ItemType.GRENADE_LAUNCHER) _customProjectileVelocityMod = 2f;
          utility.SetForceModifier(_customProjectileVelocityMod);
        }
      }

      // Add force to spine
      var torqueForce = -(Vector3.right).normalized * 20000f * _hit_force;
      _ragdoll._spine.GetComponent<Rigidbody>().AddRelativeTorque(torqueForce);
      _ragdoll._head.GetComponent<Rigidbody>().AddRelativeTorque(torqueForce);

      // Recoil player
      _ragdoll._force += MathC.Get2DVector(-transform.forward) * _shoot_force;
      if (_ragdoll._grappler != null)
      {
        _ragdoll._grappler._force += MathC.Get2DVector(-transform.forward) * _shoot_force;
      }
      //_ragdoll._forwardForce += _shoot_forward_force;

      // Recoil arm
      if (_type != ItemType.FLAMETHROWER && _type != ItemType.ROCKET_FIST)
      {
        float totalTime = 0.3f,
          halfTime = totalTime * 0.4f;
        AnimateTransformAdditive(_arm_lower, _save_rot_lower, Mathf.Clamp(50f * _hit_force, 20f, 50f), 0f, 0f, totalTime, halfTime);
      }

      // Deincrement ammo
      _clip--;
      _ragdoll._playerScript?._profile.ItemUse(_side);

      if (_clip == 0) OnClipEmpty();
    };
  }

  void OnClipEmpty()
  {
    if (_type == ItemType.DMR)
      PlaySound(Audio.GUN_EXTRA);
  }

  public void Update()
  {
    _onUpdate?.Invoke();

    if (_ragdoll == null) return;

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
    }

    // Check for dead
    if (_ragdoll._dead)
    {
      OnDestroy();
      if (_disableOnRagdollDeath) this.enabled = false;
      return;
    }
#if UNITY_EDITOR
    if (_ragdoll == null) return;
    if (IsGun())
      Debug.DrawRay(transform.position, MathC.Get2DVector(transform.forward) * 100f, Color.red);
    Debug.DrawRay(_ragdoll._hip.position, _ragdoll._hip.transform.forward * 100f, Color.cyan);
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
    if (_triggerDown) _downTime += Time.deltaTime;

    // Check special cases
    if (_type == ItemType.FLAMETHROWER)
    {
      var flames = _customParticles[0];
      var light = _customLights[0];

      // Particles
      if (flames.isPlaying && (!_used || _reloading || _clip == 0))
        flames.Stop();
      else if (!flames.isEmitting && (_used && !_reloading && _clip > 0))
        flames.Play();

      // Sound
      if (flames.isEmitting)
      {
        if (!_audioSources[4].isPlaying)
          FunctionsC.PlayAudioSource(ref _audioSources[4]);
      }
      else if (_audioSources[4].isPlaying)
        _audioSources[4].Stop();

      if ((_clip < 10 && !flames.isEmitting) && _audioSources[3].isPlaying)
        _audioSources[3].Stop();
      else if ((_clip >= 10) && !_audioSources[3].isPlaying)
        FunctionsC.PlayAudioSource(ref _audioSources[3]);
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
            if (!(_ragdoll._itemL?.IsMelee() ?? false))
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
        _ragdoll._playerScript?._profile.ItemUse(_side);
      }
      // Play sound
      //if(_customProjetile != UtilityScript.UtilityType.NONE) playSound = false;
      if (playSound)
      {
        PlaySound(Audio.GUN_SHOOT);
        if (IsGun())
        {
          EnemyScript.CheckSound(_ragdoll._hip.transform.position, _silenced ? EnemyScript.Loudness.SUPERSOFT : EnemyScript.Loudness.NORMAL);
        }
        else
        {
          EnemyScript.CheckSound(_ragdoll._hip.transform.position, EnemyScript.Loudness.SUPERSOFT);
        }
      }
    }

    // Check use conditions
    if (_fireMode == FireMode.BURST && _used)
    {
      if (CanUse() && (_time >= _useTime + _burstRate) && _bursts < _burstPerShot)
      {
        if (!_ragdoll._isPlayer && _downTime > 0.2f || _ragdoll._isPlayer)
        {
          Local_Use(_melee && _bursts > 0 ? false : true);
          if ((_clip == 0 && (!_melee)) || _bursts++ >= _burstPerShot - 1)
          {
            _bursts = 0;
            _used = false;
            if (!IsChargeWeapon()) _triggerDown = false;
            // If melee, stop swinging
            if (_melee)
            {
              _swinging = false;

              // If hit anything and two handed, allow to use immediatly
              if (_hitAnthing && _twoHanded)
                _useTime = -1f;
            }
          }
        }
      }
    }
    else if ((!_useOnRelease && _triggerDown) || (_useOnRelease && (!_triggerDown && _triggerDown_last)))
    {

      // If last use time is less than use rate, check fire rate
      if (CanUse())
      {
        if (_fireMode == FireMode.BURST)
        {

          if (IsChargeWeapon() && _clip < _minimumClipToFire)
          {
            _clip = 0;
            _ragdoll._playerScript?._profile.ItemSetClip(_side, _clip);
            PlayEmpty();
          }
          else
          {
            if (IsChargeWeapon()) _burstPerShot = _clip;
            _used = true;
            _ragdoll._forwardForce += _shoot_forward_force * _clip;
          }

          // Melee animation
          if (_melee)
          {
            if (_hitRagdolls == null)
              _hitRagdolls = new List<int>();
            else
              _hitRagdolls.Clear();

            switch (_type)
            {
              case (ItemType.KNIFE):
                float totalTime = UseRate(),
                  halfTime = _burstPerShot * _burstRate,
                  side_mod = (_side == ActiveRagdoll.Side.LEFT ? -1f : 1f);
                AnimateTransformAdditive(_arm_upper, _save_rot_upper, 0f, 50f * side_mod, 0f, totalTime, halfTime);
                AnimateTransformAdditive(_arm_lower, _save_rot_lower, -40f, 0f, 0f, totalTime, halfTime);
                break;
              case (ItemType.AXE):
                totalTime = UseRate();
                halfTime = totalTime * 0.03f;
                side_mod = (_side == ActiveRagdoll.Side.LEFT ? -1f : 1f);
                AnimateTransformAdditive(_arm_upper, _save_rot_upper, 0f, 50f * side_mod, 0f, totalTime, halfTime);
                AnimateTransformAdditive(_arm_lower, _save_rot_lower, -40f, 0f, 0f, totalTime, halfTime);
                break;
              case (ItemType.BAT):
              case (ItemType.SWORD):
                totalTime = UseRate();
                halfTime = totalTime * 0.2f;
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
        if (_melee) _swinging = true;
      }

    }

    _triggerDown_last = _triggerDown;

    // Check for melee ui
    if (_swang && _time >= _useTime + UseRate() && !IsChargeWeapon())
    {
      _swang = false;
      _ragdoll._playerScript?._profile.ItemReload(_side, false);
    }
  }

  public bool IsChargeWeapon()
  {
    return _useOnRelease && _chargeHold;
  }

  void EasyAnimate(AnimData data, float totalTime, float halfTime)
  {
    AnimationData.Animate(this, data._start, data._end, transform.GetChild(0), totalTime, halfTime);

  }

  public void OnEquip(ActiveRagdoll ragdoll, ActiveRagdoll.Side side, int clipSize = -1, float useTime = -1f)
  {
    _ragdoll = ragdoll;
    _side = side;

    // Check for laser sight
    if (!_melee && _ragdoll._isPlayer && _ragdoll._playerScript.HasPerk(Shop.Perk.PerkType.LASER_SIGHTS))
      AddLaserSight();

    SetClip(clipSize);
    _useTime = useTime;

    {
      // Save arm rots
      _original_rot_lower = _arm_lower.localEulerAngles;
      _original_rot_upper = _arm_upper.localEulerAngles;

      // Set arm pos per weapon
      if (_type == ItemType.SWORD || _type == ItemType.BAT)
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
          (_side == ActiveRagdoll.Side.LEFT ? -50f : -21f),
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
    }

    // Scale
    transform.localScale = new Vector3(1f, 1f, 1f);
    // Set rotation
    Vector3 dir = _ragdoll._hip.transform.forward;
    dir.y = 0f;
    transform.forward = dir;
    //transform.Rotate(new Vector3(0f, 1f, 0f) * (side == ActiveRagdoll.Side.LEFT ? -2f : 2.25f));
    // Set position
    Vector3 dist = (transform.parent.position - _handle.position);
    transform.position += dist;
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

  public void UseDown()
  {
    //_ragdoll._playerScript?.ResetLoadout();
    //if (this == null) return;

    _triggerDown = true;

    _downTime = 0f;

    _meleeIter = 0;
    _hitAnthing = false;
    _hitEnemy = false;

    // Check timer
    if (!PlayerScript._TimerStarted && (_ragdoll?._isPlayer ?? false))
    {
      PlayerScript._TimerStarted = true;
    }

    //
    Update();
  }
  public void UseUp()
  {
    //_ragdoll._playerScript?.ResetLoadout();
    //if (this == null) return;

    _triggerDown = false;
  }
  // Can you use the item?
  public bool CanUse()
  {
    if (_melee) return (_time >= _useTime + UseRate()) || (_bursts > 0 && _fireMode == FireMode.BURST && _time >= _useTime + _burstRate);
    // Check clip
    if (_clip > 0)
      return ((_time >= _useTime + UseRate()) || (_fireMode == FireMode.BURST && _bursts > 0 && _time >= _useTime + _burstRate)) && ((!_used || _fireMode == FireMode.BURST)) && (!_reloading || IsChargeWeapon());
    PlayEmpty();
    return false;
  }

  void PlayEmpty()
  {
    PlaySound(Audio.GUN_EMPTY);
    EnemyScript.CheckSound(_ragdoll._hip.transform.position, EnemyScript.Loudness.SUPERSOFT);
    _triggerDown = false;
  }

  public void PlayBulletHit()
  {
    PlaySound(Audio.MELEE_HIT_BULLET, 0.8f, 1.2f);
    EnemyScript.CheckSound(_ragdoll._hip.transform.position, EnemyScript.Loudness.SOFT);
  }

  // Reload function
  public void Reload()
  {
    // If melee, do nothing
    if (_melee && !IsChargeWeapon()) return;
    // Check perk
    var reload_speed_mod = 1f;
    if (_ragdoll._isPlayer && _ragdoll._playerScript.HasPerk(Shop.Perk.PerkType.FASTER_RELOAD))
      reload_speed_mod = 1.4f;
    // Play noise and set clip
    PlaySound(Audio.GUN_RELOAD, reload_speed_mod - 0.1f, reload_speed_mod + 0.1f);
    if (_ragdoll._isPlayer) EnemyScript.CheckSound(_ragdoll._hip.transform.position, EnemyScript.Loudness.SUPERSOFT);
    _clip = (_reloadOneAtTime ? _clip + 1 : ClipSize());
    // Show progress bar
    var reloadTime = _reloadTime / reload_speed_mod;
    _reloading = true;
    ProgressBar.GetProgressBar(transform, reloadTime,
      () =>
      {
        _reloading = false;
        if (!IsChargeWeapon()) _ragdoll._playerScript?._profile.ItemReload(_side, _reloadOneAtTime);
      },
      (ProgressBar.instance instance) =>
      {
        if (_ragdoll._dead)
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
    if (IsChargeWeapon()) _ragdoll._playerScript?._profile.ItemReload(_side, _reloadOneAtTime);

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
    return _clip < ClipSize() && ((_time >= _useTime + (UseRate() * 0.6f)) || (_bursts > 0 && _fireMode == FireMode.BURST && _time >= _useTime + _burstRate * 0.3f)) && !_used && !_reloading;
  }
  public bool NeedsReload()
  {
    if (IsChargeWeapon()) return false;
    return !_melee && _clip == 0;
  }
  public void SetClip(int clip = -1)
  {
    if (_melee || _chargeHold) return;
    _clip = clip == -1 ? ClipSize() : clip;
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
  public void OnToggle()
  {
    // Stop audio
    foreach (var index in _melee ? new int[] { GetAudioSource(Audio.MELEE_SWING) } : new int[] { GetAudioSource(Audio.GUN_RELOAD) })
      if (index == -1)
        continue;
      else
        _audioSources[index].Stop();

    // Stop custom components
    if (_customParticles != null)
      foreach (var particle in _customParticles)
        particle.Stop();
    if (_customLights != null)
      foreach (var light in _customLights)
        light.enabled = false;
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
      case (1):
        add = _ragdoll._hip.transform.right;
        break;
      case (2):
        add = -_ragdoll._hip.transform.right;
        break;
    }

    // Cast ray from startPos to dir (cast a ray from the gun barrel forwards)
    var forward = _ragdoll._controller.forward;
    forward.y = 0f;
    var ray = new Ray(
      _ragdoll._hip.position - _ragdoll._hip.transform.forward * 0.1f + Vector3.up * 0.3f + add * 0.1f,
      forward + -add
    );
    var hit = false;
    var maxDistance = 0.4f * (_meleePenatrate ? 1.3f : 1f) * (_ragdoll._isPlayer ? 1f : _meleePenatrate ? 0.75f : 0.65f);
    if (Physics.SphereCast(ray, (0.4f), out raycastInfo._raycastHit, maxDistance, EnemyScript._Layermask_Ragdoll))
    {
      raycastInfo._ragdoll = ActiveRagdoll.GetRagdoll(raycastInfo._raycastHit.collider.gameObject);
      if (raycastInfo._ragdoll != null && !raycastInfo._ragdoll._dead)
        hit = true;
      raycastInfo._hitPoint = raycastInfo._raycastHit.point;
    }
    _ragdoll.ToggleRaycasting(true);
    _ragdoll._grappler?.ToggleRaycasting(true);
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