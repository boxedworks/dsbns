using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

using Random = UnityEngine.Random;

public class ActiveRagdoll
{
  //
  static Settings.LevelSaveData LevelModule { get { return Settings.s_SaveData.LevelData; } }
  static Settings.SettingsSaveData SettingsModule { get { return Settings.s_SaveData.Settings; } }

  //
  public static List<ActiveRagdoll> s_Ragdolls;
  public static int s_ID;
  public int _Id;
  public Transform Transform, _Controller;
  // Body parts
  public Rigidbody _Hip;
  public HingeJoint _head, _spine, _arm_upper_l, _arm_upper_r, _arm_lower_l, _arm_lower_r, _leg_upper_l, _leg_upper_r, _leg_lower_l, _leg_lower_r;

  // Item info
  public ItemScript _ItemL, _ItemR;

  // Script references
  public EnemyScript _EnemyScript;
  public PlayerScript _PlayerScript;

  public Vector3 _Distance;

  // Color of the ragdoll
  public Color _Color;

  // What frame the hinge 'animations' are on
  float _movementIter, _movementIter2;
  public float _rotSpeed, _time_dead;

  bool _footprint;
  float _bloodFootprintTimer;

  public bool _IsRagdolled,
    _IsReviving,
    _IsPlayer,
    _IsDead,
    _IsDisabled,
    _CanMove,
    _CanReceiveInput,
    _CanDie;

  public bool _IsSwinging
  {
    get { return (_ItemL?._IsSwinging ?? false) || (_ItemR?._IsSwinging ?? false); }
  }
  public bool _IsSwingingSurvival
  {
    get { return (_ItemL?.IsSwingingSurvival() ?? false) || (_ItemR?.IsSwingingSurvival() ?? false); }
  }
  public bool _HasItemsInUse
  {
    get { return (_ItemL?.Used() ?? false) || (_ItemR?.Used() ?? false); }
  }
  public bool _IsReloading
  {
    get { return (_ItemL?._reloading ?? false) || (_ItemR?._reloading ?? false); }
  }
  public bool _IsEnemy { get { return !_IsPlayer; } }

  public Vector3 _ForceGlobal;

  // Height above ground
  static readonly public Vector3 _GROUNDHEIGHT = new Vector3(0f, 0.73f, 0f);

  // Holds rotation before Toggle()
  Quaternion _saveRot;

  public int _health;

  public GameObject[] _parts;

  static List<Tuple<Material, Material>> s_Materials_Ragdoll;

  SkinnedMeshRenderer _renderer;

  public float _totalDistanceMoved;

  public Parts _transform_parts;
  public class Parts
  {
    public Transform _hip, _spine, _arm_upper_l, _arm_upper_r, _arm_lower_l, _arm_lower_r, _head;

    Dictionary<int, SaveInfo> _saveInfo;
    public class SaveInfo
    {
      public Vector3 _pos, _rot;
      public SaveInfo(Transform t)
      {
        _pos = t.localPosition;
        _rot = t.localEulerAngles;
      }
    }

    public Parts(ActiveRagdoll ragdoll)
    {
      _hip = ragdoll._parts[0].transform;
      _spine = ragdoll._parts[5].transform.GetChild(0);
      _arm_upper_l = ragdoll._parts[6].transform;
      _arm_upper_r = ragdoll._parts[7].transform;
      _arm_lower_l = ragdoll._parts[8].transform;
      _arm_lower_r = ragdoll._parts[9].transform;
      _head = ragdoll._parts[10].transform;

      _saveInfo = new Dictionary<int, SaveInfo>();

      void AddSaveInfo(Transform part)
      {
        _saveInfo.Add(part.GetInstanceID(), new SaveInfo(part));

      }
      AddSaveInfo(_spine);
      AddSaveInfo(_arm_upper_l);
      AddSaveInfo(_arm_upper_r);
      AddSaveInfo(_arm_lower_l);
      AddSaveInfo(_arm_lower_r);
    }

    public void SetDefault(Transform t)
    {
      var info = _saveInfo[t.GetInstanceID()];
      t.localPosition = info._pos;
      t.localEulerAngles = info._rot;
    }

    /*    // Hip
    returnList[0] = _hip.gameObject;
    // Upper leg l
    returnList[1] = returnList[0].transform.GetChild(0).gameObject;
    // Upper leg r
    returnList[2] = returnList[0].transform.GetChild(1).gameObject;
    // Lower leg l
    returnList[3] = returnList[1].transform.GetChild(0).gameObject;
    // Lower leg r
    returnList[4] = returnList[2].transform.GetChild(0).gameObject;
    // Spine
    returnList[5] = returnList[0].transform.GetChild(2).GetChild(0).gameObject;
    // Arm upper l
    returnList[6] = returnList[5].transform.GetChild(0).GetChild(0).GetChild(0).gameObject;
    // Arm upper r
    returnList[7] = returnList[5].transform.GetChild(0).GetChild(1).GetChild(0).gameObject;
    // Arm lower l
    returnList[8] = returnList[6].transform.GetChild(0).gameObject;
    // Arm lower r
    returnList[9] = returnList[7].transform.GetChild(0).gameObject;
    // Head
    returnList[10] = returnList[5].transform.GetChild(0).GetChild(2).gameObject;
    */
  }

  public ActiveRagdoll(GameObject ragdoll, Transform follow)
  {
    // Assign unique ID
    _Id = s_ID++;

    // Add to static _Ragdolls list
    if (s_Ragdolls == null) s_Ragdolls = new List<ActiveRagdoll>();
    s_Ragdolls.Add(this);

    // Defaults
    _Controller = follow;
    ragdoll.name = "Ragdoll";
    Transform = ragdoll.transform;
    _time_dead = -1f;

    // Set materials
    _renderer = Transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
    s_Materials_Ragdoll ??= new List<Tuple<Material, Material>>();
    if (_Id == s_Materials_Ragdoll.Count)
    {
      var mat0 = new Material(_renderer.sharedMaterials[0]);
      mat0.SetFloat("_Metallic", 0.8f);
      var mat1 = new Material(_renderer.sharedMaterials[1]);
      mat1.SetFloat("_Metallic", 0.8f);

      s_Materials_Ragdoll.Add(new Tuple<Material, Material>(mat0, mat1));
    }
    Resources.UnloadAsset(_renderer.sharedMaterials[0]);
    Resources.UnloadAsset(_renderer.sharedMaterials[1]);
    _renderer.sharedMaterials = new Material[] { s_Materials_Ragdoll[_Id].Item1, s_Materials_Ragdoll[_Id].Item2 };

    // Register enemy / player script
    _EnemyScript = follow.GetComponent<EnemyScript>();
    _PlayerScript = follow.GetComponent<PlayerScript>();

    // Set default health
    _health = 1;

    // Index body parts
    _Hip = ragdoll.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<Rigidbody>();
    _leg_upper_l = _Hip.transform.GetChild(0).GetComponent<HingeJoint>();
    _leg_upper_r = _Hip.transform.GetChild(1).GetComponent<HingeJoint>();
    _leg_lower_l = _leg_upper_l.transform.GetChild(0).GetComponent<HingeJoint>();
    _leg_lower_r = _leg_upper_r.transform.GetChild(0).GetComponent<HingeJoint>();
    _spine = _Hip.transform.GetChild(2).GetChild(0).GetComponent<HingeJoint>();
    /*_arm_upper_l = _spine.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<HingeJoint>();
    _arm_upper_r = _spine.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<HingeJoint>();
    _arm_lower_l = _arm_upper_l.transform.GetChild(0).GetComponent<HingeJoint>();
    _arm_lower_r = _arm_upper_r.transform.GetChild(0).GetComponent<HingeJoint>();*/
    _head = _spine.transform.GetChild(0).GetChild(2).GetComponent<HingeJoint>();

    _parts = GetParts();
    _transform_parts = new Parts(this);

    _Color = Color.blue * 1.3f;

    _rotSpeed = 1f;

    _CanMove = _CanReceiveInput = true;

    _CanDie = true;

    SetCollisionMode(CollisionDetectionMode.Continuous);

    /*if (GameScript._GameMode == GameScript.GameModes.SURVIVAL)
    {
      if (_isPlayer)
      {
        var joints = new HingeJoint[] { _arm_lower_l, _arm_lower_r, _arm_upper_l, _arm_upper_r };
        foreach (var joint in joints)
        {
          var rb = joint.gameObject.GetComponent<Rigidbody>();
          GameObject.DestroyImmediate(joint);
          GameObject.DestroyImmediate(rb);

        }
      }
    }*/
  }

  public void AddArmJoint(Side side)
  {
    var upper = (side == Side.LEFT ? _transform_parts._arm_upper_l : _transform_parts._arm_upper_r).gameObject;
    if (upper.GetComponent<Rigidbody>()) return;

    var lower = (side == Side.LEFT ? _transform_parts._arm_lower_l : _transform_parts._arm_lower_r).gameObject;
    var side_mod = side == Side.LEFT ? -1f : 1f;
    var upper_rb = upper.AddComponent<Rigidbody>();
    var lower_rb = lower.AddComponent<Rigidbody>();

    upper_rb.interpolation = lower_rb.interpolation = RigidbodyInterpolation.Interpolate;
    upper_rb.mass = lower_rb.mass = 0.2f;

    var upper_joint = upper.AddComponent<HingeJoint>();
    upper_joint.axis = new Vector3(0f, 0f, 1f);
    upper_joint.useLimits = true;
    SetJointLimits(upper_joint, -70f * side_mod, 15f * side_mod);
    var lower_joint = lower.AddComponent<HingeJoint>();
    lower_joint.axis = new Vector3(0f, 0f, 1f);
    lower_joint.useLimits = true;
    SetJointLimits(lower_joint, -150f * side_mod, 0f);
    upper_joint.connectedBody = _spine.GetComponent<Rigidbody>();
    lower_joint.connectedBody = upper_rb;
    if (side == Side.LEFT)
    {
      _arm_upper_l = upper_joint;
      _arm_lower_l = lower_joint;
    }
    else
    {
      _arm_upper_r = upper_joint;
      _arm_lower_r = lower_joint;
    }
  }

  public void RemoveArmJoint(Side side)
  {
    var joints = new HingeJoint[] {
      (side == Side.LEFT ? _arm_upper_l : _arm_upper_r),
      (side == Side.LEFT ? _arm_lower_l : _arm_lower_r)
    };
    foreach (var joint in joints)
    {
      if (joint == null) continue;
      var rb = joint.gameObject.GetComponent<Rigidbody>();
      GameObject.DestroyImmediate(joint);
      GameObject.DestroyImmediate(rb);
    }
    // Set arms to default
    _transform_parts.SetDefault(_transform_parts._spine);
    if (side == Side.LEFT)
    {
      _transform_parts.SetDefault(_transform_parts._arm_upper_l);
      _transform_parts.SetDefault(_transform_parts._arm_lower_l);
    }
    else
    {
      _transform_parts.SetDefault(_transform_parts._arm_upper_r);
      _transform_parts.SetDefault(_transform_parts._arm_lower_r);
    }
  }

  void SetJointLimits(HingeJoint joint, float min = 0f, float max = 0f)
  {
    var limits = joint.limits;
    limits.min = min;
    limits.max = max;
    joint.limits = limits;
  }

  float _invisibility_timer, _confusedTimer;
  public void Update()
  {
#if DEBUG
    /*if (_ItemR != null && _ItemR.IsGun())
      Debug.DrawRay(_ItemR.transform.position, MathC.Get2DVector(_ItemR.transform.forward) * 100f);
    if (_ItemL != null && _ItemL.IsGun())
      Debug.DrawRay(_ItemL.transform.position, MathC.Get2DVector(_ItemL.transform.forward) * 100f);
    Debug.DrawRay(_Hip.position, _Hip.transform.forward * 100f, Color.yellow);*/
#endif

    var dt = _PlayerScript != null ? Time.unscaledDeltaTime : Time.deltaTime;

    // Update grabbed rd
    if (_grapplee != null)
    {
      if (_grapplee._IsDead)
      {
        _grapplee = null;
      }
      else
      {
        // Get melee side
        var left_weapon = _ItemL != null && _ItemL.IsMelee();

        _grapplee._Hip.position = _Hip.position + _Hip.transform.forward * 0.45f + _Hip.transform.right * 0.09f * (left_weapon ? -1f : 1f);
        _grapplee._Hip.rotation = _Hip.rotation;
      }
    }

    // Update arm spread
    if (_spine != null)
    {
      if (_ItemL != null && _ItemL._twoHanded) { }
      else
      {
        var spread_speed = 20f * dt * (Time.timeScale < 0.75f ? 0.17f : 1f);
        if (_ItemL != null)
          if (_la_limit - _la_targetLimit != 0f)
          {
            // Start z rotation of upper arm
            var startPos = 73f;

            // Lerp
            _la_limit = Mathf.Clamp(_la_limit + ((_la_targetLimit - _la_limit) > 0f ? 1f : -1f) * 0.5f * spread_speed, 0f, 1f);
            var arm_upper = _spine.transform.GetChild(0).GetChild(0).GetChild(0);
            var localrot = arm_upper.localRotation.eulerAngles;
            localrot.z = Mathf.Lerp(startPos, startPos - 90f, _la_limit);
            var q = arm_upper.localRotation;
            q.eulerAngles = localrot;
            arm_upper.localRotation = q;
          }
        if (_ItemR != null)
          if (_ra_limit - _ra_targetLimit != 0f)
          {
            // Start z rotation of upper arm
            var startPos = -73f;

            // Lerp
            _ra_limit = Mathf.Clamp(_ra_limit + ((_ra_targetLimit - _ra_limit) > 0f ? 1f : -1f) * 0.5f * spread_speed, 0f, 1f);
            var arm_upper = _spine.transform.GetChild(0).GetChild(1).GetChild(0);
            var localrot = arm_upper.localRotation.eulerAngles;
            localrot.z = Mathf.Lerp(startPos, startPos + 90f, _ra_limit);
            var q = arm_upper.localRotation;
            q.eulerAngles = localrot;
            arm_upper.localRotation = q;
          }
      }
    }

    if (!_IsRagdolled)
    {

      // Add outside recoil
      if (_ForceGlobal.magnitude > 0f)
      {
        var force_normalize = _ForceGlobal.magnitude < 1f ? _ForceGlobal : _ForceGlobal.normalized;
        var force_apply = _ForceGlobal * Time.deltaTime * 10f;

        var agent = _IsPlayer ? _PlayerScript._agent : _EnemyScript._agent;
        if (!_grappled)
          agent.Move(force_apply);
        _ForceGlobal -= force_apply;
      }

      // Only update if enabled
      if (!_CanReceiveInput) return;

      /*/ Check hip
      if (_Hip.transform.position.y > 0f)
      {
        Debug.Log("defective ragdoll");
        Kill(null, DamageSourceType.MELEE, Vector3.zero);
        return;
      }*/
    }

    // Move / rotate based on a rigidbody
    var movePos = _Controller.position + _GROUNDHEIGHT;
    _Distance = (movePos - _Hip.position) * (Time.timeScale < 1f ? 0.3f : 1f);

    // Moving ragdoll too fast will break joints, clamp to .35f magnitude
    if (_Distance.magnitude > 0.35f)
    {
      movePos = _Hip.position + _Distance.normalized * 0.35f;
      _Distance = (movePos - _Hip.position) * (Time.timeScale < 1f ? 0.3f : 1f);
    }

    // If can't move, set movepos and distance to origin
    if (!_CanMove || _grappled)
    {
      movePos = _Hip.position;
      _Distance = Vector3.zero;
    }

    // Stand if not moving, else iterate
    if (_Distance.magnitude < 0.005f)
      _movementIter2 += (0.5f - _movementIter2) * dt * 12f;
    else
    {
      _totalDistanceMoved += _Distance.magnitude;
      var save = _movementIter2;
      _movementIter2 = Mathf.PingPong(_movementIter * 2.8f, 1f);

      // Play footsteps  based on controller distance moved
      if (!_IsRagdolled && !_grappled && ((save < 0.5f && _movementIter2 >= 0.5f) || (save > 0.5f && _movementIter2 <= 0.5f)))
      {
        // If player, send footstep sound to enemies to check for detection
        if (_IsPlayer && _PlayerScript._CanDetect && _Distance.magnitude > 0.1f)
          EnemyScript.CheckSound(_Controller.position, (_Distance.magnitude > 0.2f ? EnemyScript.Loudness.SOFT : (EnemyScript.Loudness.SUPERSOFT)));

        // Sfx
        var footstepAudioSource = false && _bloodFootprintTimer > 0f ? SceneThemes._footstepBloody : SceneThemes._footstep;
        SfxManager.PlayAudioSourceSimple(_Controller.position, footstepAudioSource.clip, footstepAudioSource.volume, 0.89f, 1.11f, SfxManager.AudioClass.FOOTSTEP);

        // Footprint
        var footprint_pos = _footprint ? _leg_lower_l.transform.position : _leg_lower_r.transform.position;
        footprint_pos.y -= 0.4f;
        _footprint = !_footprint;
        FunctionsC.PlayComplexParticleSystemAt(_bloodFootprintTimer > 0f ? FunctionsC.ParticleSystemType.FOOTPRINT_BLOOD : FunctionsC.ParticleSystemType.FOOTPRINT, footprint_pos);
      }
    }
    if (_bloodFootprintTimer > 0f)
      _bloodFootprintTimer -= Time.deltaTime;

    //
    var posSave = _Hip.position;
    var rot = _Hip.rotation;
    var f = _Controller.rotation.eulerAngles.y;
    if (f > 180.0f)
      f -= 360.0f;
    rot.eulerAngles = new Vector3(rot.eulerAngles.x, f, rot.eulerAngles.z);

    // Don't move to ragdoll if is not active
    if (Active())
    {
      // Move / rotate hip (base)
      _Hip.MovePosition(movePos);
      if (_IsPlayer)
      {
        dt = Time.unscaledDeltaTime;
      }

      _Hip.MoveRotation(Quaternion.RotateTowards(_Hip.rotation, rot, dt * (Time.timeScale < 0.75f && _IsPlayer ? 4f : 14f) * _rotSpeed * Mathf.Abs(Quaternion.Angle(_Hip.rotation, rot))));

      // Use iter to move joints
      _movementIter += (_Distance.magnitude / 3f) * Time.deltaTime * 65f;

      // Check melee movement
      var moveDir = (movePos - posSave).normalized;
      if (
        _IsPlayer &&
        Time.time - _meleeStartTime < 0.25f &&
        _PlayerScript.GetInput().magnitude > 0.7f &&
        (moveDir - _Controller.forward).magnitude > 1.4f)
      {
        _meleeStartTime = -1f;

        _ForceGlobal += MathC.Get2DVector(moveDir * 0.5f);

        // FX
        SfxManager.PlayAudioSourceSimple(_Hip.position, "Ragdoll/Quickstep", 0.85f, 1.15f, SfxManager.AudioClass.NONE, true);
        var particles = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.CLOUD_RING)[0];
        particles.transform.position = _Hip.position + new Vector3(0f, -0.5f, 0f);
        particles.Play();
      }

      // Check stun FX
      if (!_IsDead && _IsStunned)
      {
        if (Time.time - _confusedTimer > 0f)
        {
          _confusedTimer = Time.time + Random.Range(0.2f, 0.5f);
          var p = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.CONFUSED)[0];
          p.transform.position = _head != null ? _head.transform.position : _Controller.position;
          p.Play();
        }
      }
    }
    else
    {
      _Controller.position = _Hip.position;

      // Use iter to move joints
      _movementIter += (_Distance.magnitude / 10f) * Time.deltaTime * 50f;
    }

    // Kick
    if (_kicking)
    {
      if (Time.time - _kickTimer_start >= 1f)
      {

        if (Time.time - _kickTimer_start >= 1.2f)
        {
          _kicking = false;
        }

        else if (Time.time - _kickTimer >= 0.05f)
        {

          _kickTimer = Time.time;

          RaycastHit hit;
          ToggleRaycasting(false);
          if (Physics.SphereCast(_Controller.position, 0.25f, _Controller.forward, out hit, 0.5f, GameResources._Layermask_Ragdoll))
          {
            var ragdoll = ActiveRagdoll.GetRagdoll(hit.collider.gameObject);
            if (ragdoll != null)
            {
              var hitForce = MathC.Get2DVector(
                -(_Hip.transform.position - ragdoll._Hip.transform.position).normalized * (4000f + (Random.value * 2000f)) * 1f
              );
              if (ragdoll.TakeDamage(
                new RagdollDamageSource()
                {
                  Source = this,

                  HitForce = hitForce,

                  Damage = 1,
                  DamageSource = _Hip.position,
                  DamageSourceType = DamageSourceType.MELEE,

                  SpawnBlood = true,
                  SpawnGiblets = true
                }))
              {
                _kicking = false;
              }
            }
          }
          ToggleRaycasting(true);

        }
      }
    }

    // Animate joints; walking
    var joints = new HingeJoint[] { _leg_upper_l, _leg_upper_r, _arm_lower_l, _arm_lower_r };
    var opposite = false;
    var first = true;
    foreach (var joint in joints)
    {
      if (joint == null) continue;
      var j = joint.spring;

      // Kick with leg
      if (first && _kicking)
      {
        first = false;

        var movementIter = 0f;
        if (Time.time - _kickTimer_start < 1f)
        {
          movementIter = -(Time.time - _kickTimer_start) * 1.4f;
        }

        else if (Time.time - _kickTimer_start < 1.2f)
        {
          movementIter = (Time.time - _kickTimer_start - 1f) * 1f;
        }

        else
        {
          movementIter = _movementIter2;
        }

        j.targetPosition = (opposite ?
          joint.limits.min + (joint.limits.max - joint.limits.min) * movementIter :
          joint.limits.max - (joint.limits.max - joint.limits.min) * movementIter);
      }

      // Normal walking
      else
        j.targetPosition = (opposite ?
          joint.limits.min + (joint.limits.max - joint.limits.min) * _movementIter2 :
          joint.limits.max - (joint.limits.max - joint.limits.min) * _movementIter2);
      joint.spring = j;
      opposite = !opposite;
    }

    // Invisiblity timer
    if (_invisibility_timer > 0f)
    {

      _invisibility_timer -= Time.deltaTime;
      if (_invisibility_timer <= 0f)
      {
        _invisibility_timer = 0f;

        SetInvisible(false);
      }

    }
  }

  // Bloody footprint increment
  public void SetBloodTimer()
  {
    _bloodFootprintTimer = 1.5f;
  }

  //
  public void SetActive(bool toggle)
  {
    Transform.gameObject.SetActive(toggle);
  }
  public bool IsActive()
  {
    return Transform.gameObject.activeSelf;
  }

  // Remove all HingeJoints, Rigidbodies, and Colliders except for _Hip
  public void Disable()
  {
    if (_IsDisabled) return;
    _IsDisabled = true;
    IEnumerator delayDisable()
    {
      yield return new WaitForSecondsRealtime(5f);
      if (_Hip != null) _Hip.isKinematic = true;
      foreach (var part in _parts)
      {
        if (part == null) continue;

        // Remove HingeJoint
        var j = part.GetComponent<HingeJoint>();
        if (j)
          GameObject.Destroy(j);

        // Remove Rigidbody
        var r = part.GetComponent<Rigidbody>();
        if (r)
          GameObject.Destroy(r);

        // Remove Collider
        var c = part.GetComponent<Collider>();
        if (c)
          GameObject.Destroy(c);
        yield return new WaitForSecondsRealtime(0.05f);
      }
    }
    _EnemyScript.StartCoroutine(delayDisable());
  }

  // Invisibility
  public bool _invisible;
  public void SetInvisible(bool toggle)
  {
    // Set toggle
    _invisible = toggle;

    // Set renderer / layer
    _renderer.enabled = !toggle;
    //ToggleRaycasting(!toggle);

    // Play FX
    PlaySound("Ragdoll/Combust", 0.9f, 1.1f);
    FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.SMOKEBALL, _Hip.position);

    // Set timer
    if (toggle)
      _invisibility_timer += 1f;
    else
      _invisibility_timer = 0f;
  }

  // Use item(s) in hand(s)
  public void UseLeft()
  {
    var item = _ItemL;
    if (item == null) return;

    item.UseDown();
  }
  public void UseRight()
  {
    var item = _ItemR;
    if (item == null) return;

    item.UseDown();
  }

  public void UseLeftDown()
  {
    if (_ItemL == null) return;
    _ItemL.UseDown();
  }
  public void UseLeftUp()
  {
    if (_ItemL == null || _IsDead) return;
    _ItemL.UseUp();
  }
  public void UseRightDown()
  {
    if (_ItemR != null)
      _ItemR.UseDown();
  }
  public void UseRightUp()
  {
    if (_ItemR != null)
      _ItemR.UseUp();
  }

  float _la_targetLimit = 0f, _ra_targetLimit = 0f,
    _la_limit, _ra_limit;

  public void ArmsDown()
  {
    LeftArmCenter();
    RightArmCenter();
  }
  public void LeftArmOut()
  {
    _la_targetLimit = 1f;
  }
  public void RightArmOut()
  {
    _ra_targetLimit = 1f;
  }
  public void LeftArmCenter()
  {
    _la_targetLimit = 0f;
  }
  public void RightArmCenter()
  {
    _ra_targetLimit = 0f;
  }

  public enum Side
  {
    LEFT,
    RIGHT
  }

  public void EquipItem(GameScript.ItemManager.Items itemType, Side side, int clipSize = -1, float useTime = -1f, int itemId = -1)
  {
    if (itemType == GameScript.ItemManager.Items.NONE)
    {
      AddArmJoint(side);
      return;
    }

    // Spawn item
    var item = GameScript.ItemManager.GetItem(itemType);
    if (item == null)
    {
      GameScript.ItemManager.SpawnItem(itemType);
      item = GameScript.ItemManager.GetItem(itemType);
    }
    var itemScript = item.GetComponent<ItemScript>();

    bool two_hands = itemScript._twoHanded;
    var side_other = side == Side.LEFT ? Side.RIGHT : Side.LEFT;

    // Unequip if equipped already
    if (side == Side.LEFT)
      UnequipItem(Side.LEFT);
    else
      UnequipItem(Side.RIGHT);
    if (two_hands)
      UnequipItem(side_other);

    // Rotate / positon per specific item
    var rb = item.GetComponent<Rigidbody>();
    if (rb != null) GameObject.Destroy(rb);

    // Remove joints for better aiming
    RemoveArmJoint(side);
    if (two_hands) RemoveArmJoint(side_other);

    // Decide which hand to place in
    Transform arm;
    if (side == Side.LEFT || two_hands)
    {
      arm = _transform_parts._arm_lower_l;
      _ItemL = itemScript;
    }
    else
    {
      arm = _transform_parts._arm_lower_r;
      _ItemR = itemScript;
    }
    item.transform.parent = arm.GetChild(0);
    itemScript.OnEquip(this, side, clipSize, useTime, itemId);
  }

  public void UnequipItem(Side side)
  {
    var item = (side == Side.LEFT ? _ItemL : _ItemR);
    if (side == Side.LEFT)
    {
      _transform_parts.SetDefault(_transform_parts._arm_upper_l);
      _transform_parts.SetDefault(_transform_parts._arm_lower_l);
    }
    else
    {
      _transform_parts.SetDefault(_transform_parts._arm_upper_r);
      _transform_parts.SetDefault(_transform_parts._arm_lower_r);
    }
    if (item == null) return;
    GameObject.DestroyImmediate(item.gameObject);
    if (side == Side.LEFT) _ItemL = null;
    else _ItemR = null;
  }

  // Switch item hands
  public void SwapItemHands(int index)
  {
    //_playerScript?.ResetLoadout();

    SwapItems(
      new WeaponSwapData()
      {
        ItemType = _ItemR?._type ?? GameScript.ItemManager.Items.NONE,
        ItemId = _ItemR?._ItemId ?? -1,
        ItemClip = _ItemR?.Clip() ?? -1,
        ItemUseItem = _ItemR?._useTime ?? -1f
      },
      new WeaponSwapData()
      {
        ItemType = _ItemL?._type ?? GameScript.ItemManager.Items.NONE,
        ItemId = _ItemL?._ItemId ?? -1,
        ItemClip = _ItemL?.Clip() ?? -1,
        ItemUseItem = _ItemL?._useTime ?? -1f
      },
      index
    );
  }

  float _lastItemSwap;
  public bool CanSwapWeapons()
  {
    return !_IsDead && !_IsSwinging && Time.time - _lastItemSwap > 0.5f && !_IsReloading && !_HasItemsInUse;
  }

  //
  public ItemScript TryDeflectMelee(ActiveRagdoll ragdollOther)
  {

    // Check facing each other
    var forwardSelf = _Hip.transform.forward;
    var forwardOther = ragdollOther._Hip.transform.forward;
    var forwardMagnitude = (forwardOther - forwardSelf).magnitude;
    //Debug.Log(forwardMagnitude);
    if (forwardMagnitude < 1.7f)
    {
      return null;
    }

    /*/ Check each item vs each other
    foreach (var itemSelf in new ItemScript[] { _ItemL, _ItemR })
    {

      // Check null item
      if (itemSelf == null) continue;

      // Make sure item is swinging
      if (_IsPlayer && !ragdollOther._IsPlayer && ragdollOther._EnemyScript._IsZombieReal)
        if (!itemSelf.IsSwingingSurvival()) continue;
        else if (!itemSelf._IsSwinging) continue;

      // Make sure has not already damaged ragdoll this swing
      if (itemSelf.HasHitRagdoll(ragdollOther)) continue;

      // Compare item deflects
      foreach (var itemOther in new ItemScript[] { ragdollOther._ItemL, ragdollOther._ItemR })
      {

        // Check null item
        if (itemOther == null) continue;

        // Make sure item is swinging
        if (ragdollOther._IsPlayer && !_IsPlayer && _EnemyScript._IsZombieReal)
          if (!itemOther.IsSwingingSurvival()) continue;
          else if (!itemOther._IsSwinging) continue;

        // Make sure has not already damaged ragdoll this swing
        if (itemOther.HasHitRagdoll(this)) continue;

        //
        return itemSelf;
      }
    }

    return null;*/

    // Check weapons in order L / R
    bool CheckHit(ItemScript item)
    {

      //
      if (
        item == null ||
        !(_IsPlayer && ragdollOther._IsEnemy && ragdollOther._EnemyScript._IsZombieReal ? item.IsSwingingSurvival() : item._IsSwinging) ||
        item.HasHitRagdoll(ragdollOther)
       )
        return false;

      //
      item.RegisterHitRagdoll(ragdollOther);
      item.SetHitOverride();

      return true;
    }

    //
    if (CheckHit(_ItemL))
      return _ItemL;
    if (CheckHit(_ItemR))
      return _ItemR;
    return null;
  }

  // Change weapons mid-game
  public struct WeaponSwapData
  {
    public GameScript.ItemManager.Items ItemType;
    public int ItemClip, ItemId;
    public float ItemUseItem;
  }
  public void SwapItems(WeaponSwapData item_left, WeaponSwapData item_right, int index, bool checkCanSwap = true)
  {
    if (!CanSwapWeapons() && checkCanSwap) return;
    if (_ItemL != null && _ItemL._twoHanded || _ItemR != null && _ItemR._twoHanded)
    {
      //DisplayText("two handed problems");
      //return;
    }
    _lastItemSwap = Time.time;

    var itemL_type = item_left.ItemType;
    var itemL_id = item_left.ItemId;
    var itemL_clip = item_left.ItemClip;
    var itemL_useTime = item_left.ItemUseItem;

    var itemR_type = item_right.ItemType;
    var itemR_id = item_right.ItemId;
    var itemR_clip = item_right.ItemClip;
    var itemR_useTime = item_right.ItemUseItem;

    // Equip items
    if (itemL_type != GameScript.ItemManager.Items.NONE)
      EquipItem(itemL_type, Side.LEFT, itemL_clip, itemL_useTime, itemL_id);
    else
    {
      AddArmJoint(Side.LEFT);
      UnequipItem(Side.LEFT);
    }
    if (itemR_type != GameScript.ItemManager.Items.NONE)
      EquipItem(itemR_type, Side.RIGHT, itemR_clip, itemR_useTime, itemR_id);
    else
    {
      if (_ItemL == null || !_ItemL._twoHanded)
      {
        AddArmJoint(Side.RIGHT);
        UnequipItem(Side.RIGHT);
      }
    }

    if (_IsPlayer)
    {
      if (index == 0)
      {
        _PlayerScript._Profile._Equipment._ItemLeft0 = itemL_type;
        _PlayerScript._Profile._Equipment._ItemRight0 = itemR_type;
      }
      else
      {
        _PlayerScript._Profile._Equipment._ItemLeft1 = itemL_type;
        _PlayerScript._Profile._Equipment._ItemRight1 = itemR_type;
      }
    }

    // Sfx
    PlaySound("Ragdoll/Weapon_Switch");
  }

  // Check for and reload items in both hands
  public void Reload()
  {
    // Don't reload if dead
    if (_IsDead) return;
    // Left item
    if (_ItemL != null && _ItemL.CanReload() && !_ItemL.IsChargeWeapon())
    {
      _ItemL.Reload();
      // Check player settings
      if (_IsPlayer && !_PlayerScript._Profile._reloadSidesSameTime) return;
      else
      {
        IEnumerator delayedReload()
        {
          yield return new WaitForSeconds(0.1f);
          if (_ItemR != null && _ItemR.CanReload())
            _ItemR.Reload();
        }
        GameScript.s_Singleton.StartCoroutine(delayedReload());
      }
    }
    //else if (_itemL != null && !_itemL._melee && _isPlayer) DisplayText("already reloading L");
    // Right item
    if (_ItemR != null && _ItemR.CanReload() && !_ItemR.IsChargeWeapon())
      _ItemR.Reload();
    //else if (_itemR != null && !_itemR._melee && _isPlayer) DisplayText("already reloading R");
  }

  public void IgnoreCollision(Collider c, bool ignore = true)
  {
    foreach (var part in _parts)
    {
      var c0 = part.GetComponent<Collider>();
      if (c0 == null) continue;
      Physics.IgnoreCollision(c, c0, ignore);
    }
  }

  public void OnTriggerEnter(Collider other)
  {
    // Check player / enemy specific triggers
    if (_IsPlayer) _PlayerScript.OnTriggerEnter(other);

    // Check general triggers
    var u = other.GetComponent<CustomEntityUI>();
    if (u != null)
    {
      u.Activate();
    }

    // Check stepped on mine, play audio; Explodes in OnTriggerExit()
    else if (other.name.Equals("Mine"))
    {
      var s = other.transform.parent.GetComponent<ExplosiveScript>();
      PlaySound("Ragdoll/Tick", 0.95f, 1.1f);
    }

    // If ran into powerup, activate it
    else if (other.name.Equals("Goal") && !_IsDead && _IsPlayer)
    {
      other.transform.parent.GetComponent<Powerup>()
        .Activate(this);
    }
  }

  //
  float _meleeStartTime;
  public void OnMeleeStart()
  {
    _meleeStartTime = Time.time;
  }

  //
  public void BounceFromPosition(Vector3 position, float force, bool overright = true)
  {
    var bounceForce = (MathC.Get2DVector(_Hip.position) - MathC.Get2DVector(position)).normalized * force;
    Recoil(bounceForce, 1f, overright);
  }

  //
  public void OnTriggerExit(Collider other)
  {
    /*/ Explode mine stepping on
    if (other.name.Equals("Mine"))
    {
      ExplosiveScript s = other.transform.parent.GetComponent<ExplosiveScript>();
      s.Trigger();
    }*/
    if (_IsPlayer) _PlayerScript.OnTriggerExit(other);
  }
  public void OnTriggerStay(Collider other)
  {
    if (_IsPlayer) _PlayerScript.OnTriggerStay(other);
  }

  // Handle body part noises
  public static class Rigidbody_Handler
  {
    static List<RigidbodyData> _Rbs;
    struct RigidbodyData
    {
      public Rigidbody Rigidbody;
      public RigidbodyType RigidbodyType;
      public float MagnitudeOld;
      public float LastSoundFX;
    }

    static int _RbsIndex;

    public enum RigidbodyType
    {
      BODY,
      WOOD,
    }

    //
    public static void Init()
    {
      _Rbs = new List<RigidbodyData>();
    }

    //
    public static void Update()
    {
      // If no rbs, ignore
      if (_Rbs.Count == 0) return;

      for (var u = Mathf.Clamp(3, 1, _Rbs.Count) - 1; u >= 0; u--)
      {

        // Gather data
        var index = _RbsIndex++ % _Rbs.Count;
        var rbData = _Rbs[index];

        // Check for null and remove
        var rb = rbData.Rigidbody;
        if (rb == null)
        {
          _Rbs.RemoveAt(index);
          continue;
        }

        //
        var mag = rb.linearVelocity.magnitude;
        var mag_old = rbData.MagnitudeOld;
        var lastSound = rbData.LastSoundFX;
        var bodyType = rbData.RigidbodyType;

        //
        var min = 3.5f;
        if (mag > mag_old && mag > min)
        {
          rbData.MagnitudeOld = mag;
          rbData.LastSoundFX = lastSound;

          _Rbs[index] = rbData;

          //Debug.Log($"buildup: {rb.name} {mag_old} .. {mag}");
          continue;
        }

        //
        if (Time.time - lastSound > 0.25f && mag_old > mag && mag_old > 0.5f)
        {
          rbData.MagnitudeOld = mag;
          rbData.LastSoundFX = Time.time;

          _Rbs[index] = rbData;

          //Debug.Log($"sound: {rb.name} {mag_old} .. {mag}");

          switch (bodyType)
          {

            case RigidbodyType.BODY:
              var soundName = mag_old > 7f ? "Thud_loud" : "Thud";
              SfxManager.PlayAudioSourceSimple(rb.position, $"Ragdoll/{soundName}", 0.7f, 1.25f, SfxManager.AudioClass.NONE);
              break;
            case RigidbodyType.WOOD:
              soundName = mag_old > 6f ? "Wood_hard" : "Wood_soft";
              SfxManager.PlayAudioSourceSimple(rb.position, $"Etc/{soundName}", 0.7f, 1.25f, SfxManager.AudioClass.NONE);
              break;

          }

        }
      }
    }

    public static void AddListener(Rigidbody rb, RigidbodyType rigidbodyType)
    {
      _Rbs.Add(
        new RigidbodyData()
        {
          Rigidbody = rb,
          RigidbodyType = rigidbodyType,

          MagnitudeOld = 0f,
          LastSoundFX = 0f
        });
    }

    public static void ApplyExplosion(Vector3 position, float radius)
    {
      foreach (var bodyData in _Rbs)
      {
        if (bodyData.RigidbodyType == RigidbodyType.BODY) continue;

        var dir = MathC.Get2DVector(bodyData.Rigidbody.position - position);
        var dist = dir.magnitude;
        if (dist > radius * 1.5f) continue;
        bodyData.Rigidbody.AddForce(dir.normalized * 3000f);
      }
    }

    public static void Reset()
    {
      _Rbs.Clear();
    }
  }

  void AddPartListener(Rigidbody rb)
  {
    Rigidbody_Handler.AddListener(rb, Rigidbody_Handler.RigidbodyType.BODY);
  }

  Coroutine _color_Coroutine;
  public void ChangeColor(Color c, float lerpAmount = 0f)
  {
    if (Color.Equals(_Color, Color.blue * 1.3f))
      _Color = c;

    // Gather mesh renderers by amount of materials and final color
    var mesh = Transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
    if (lerpAmount == 0f)
    {
      Color startColor0 = mesh.sharedMaterials[0].color,
       startColor1 = mesh.sharedMaterials[1].color;

      SetLerpAmount(ref mesh, c, 1f, startColor0, startColor1);
      _PlayerScript?.ChangeRingColor(GameScript.s_GameMode == GameScript.GameModes.VERSUS && !VersusMode.s_Settings._FreeForAll ? VersusMode.GetTeamColorFromPlayerId(_PlayerScript._Id) : c);
      return;
    }
    if (_color_Coroutine != null)
      GameScript.s_Singleton.StopCoroutine(_color_Coroutine);
    _color_Coroutine = GameScript.s_Singleton.StartCoroutine(LerpColor(mesh, c, lerpAmount));
  }
  void SetLerpAmount(ref SkinnedMeshRenderer mesh, Color c, float normalized, Color sc0, Color sc1)
  {
    var c0 = Color.Lerp(sc1, c / 2f, normalized);
    c = Color.Lerp(sc0, c + Color.white / 3f, normalized);

    // Skin
    mesh.sharedMaterials[1].color = c;
    // Clothes
    mesh.sharedMaterials[0].color = c0;
  }
  IEnumerator LerpColor(SkinnedMeshRenderer mesh, Color c, float lerpAmount)
  {
    var timer = 0f;
    var timeLast = Time.time;

    // Skin
    Color startColor0 = mesh.sharedMaterials[1].color,
      // Clothes
      startColor1 = mesh.sharedMaterials[0].color;

    while (true)
    {
      yield return new WaitForSecondsRealtime(0.02f);
      if (_Hip == null) break;

      timer = Mathf.Clamp(timer + (Time.time - timeLast) * 1.25f, 0f, lerpAmount);
      timeLast = Time.time;
      SetLerpAmount(ref mesh, c, timer / lerpAmount, startColor0, startColor1);

      if (timer == lerpAmount) break;
    }
    _color_Coroutine = null;
  }

  public struct RagdollDamageSource
  {
    public ActiveRagdoll Source;
    public DamageSourceType DamageSourceType;
    public Vector3 HitForce, DamageSource;
    public int Damage;
    public bool SpawnBlood, SpawnGiblets;
  }
  float _lastMetalHit;
  public bool TakeDamage(RagdollDamageSource ragdollDamageSource)
  {

    var source = ragdollDamageSource.Source;
    var hitForce = ragdollDamageSource.HitForce;
    var damage = ragdollDamageSource.Damage;
    var damageSource = ragdollDamageSource.DamageSource;
    var damageSourceType = ragdollDamageSource.DamageSourceType;
    var spawnBlood = ragdollDamageSource.SpawnBlood;
    var spawnGiblets = ragdollDamageSource.SpawnGiblets;

    // If is robot, do not damage from bullet and play 'dink'
    if (_EnemyScript != null && _EnemyScript._enemyType == EnemyScript.EnemyType.ROBOT && damage <= 10f)
    {
      if (Time.time - _lastMetalHit > 0.05f && damageSourceType == DamageSourceType.BULLET)
      {
        _lastMetalHit = Time.time;
        PlaySound("Enemies/Metal_hit", 0.8f, 1.2f);

        var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
        parts.transform.position = _Hip.position;
        parts.Play();
      }
      return false;
    }

    var save_health = _health;
    if (!_IsDead)
    {
      _health -= damage;

      if (_health <= 0f)
      {
        Kill(source, damageSourceType, hitForce);
      }

      // Player armor
      if (_IsPlayer)
      {
        // Fire player events
        _PlayerScript.OnDamageTaken();
        PlaySound("Enemies/Ugh0");

        if (_hasArmor)
        {
          spawnBlood = false;
          var health_threshhold = GameScript.IsSurvival() ? 3 : 1;
          if (save_health > health_threshhold && _health <= health_threshhold)
            RemoveArmor(hitForce, false);
          else
          {
            PlaySound("Enemies/Metal_hit", 0.8f, 1.2f);
            var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
            parts.transform.position = _Hip.position;
            parts.Play();
          }
        }
        else
        {
          if (damageSourceType == DamageSourceType.FIRE)
          {
            PlaySound("Ragdoll/Combust");
            FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.FIREBALL, _Hip.position);
          }
          else
            PlaySound("Ragdoll/Punch");
        }
      }
      // Enemy armor
      else
      {
        if (_hasArmor)
        {
          spawnBlood = false;
          if (_health <= 1 && save_health > 1)
          {
            RemoveArmor(hitForce, false);

            if (_health < 1)
            {
              spawnBlood = true;
            }
            else

            // Change color to normal for survival
            if (_EnemyScript?._survivalAttributes != null)
              ChangeColor(Color.green, 0.8f);
          }
          else if (_health > 0)
          {
            PlaySound("Enemies/Metal_hit", 0.8f, 1.2f);
            var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
            parts.transform.position = _Hip.position;
            parts.Play();
          }
        }
        else
        {
          if (damageSourceType == DamageSourceType.FIRE)
          {
            PlaySound("Ragdoll/Combust", 0.9f, 1.1f);
            FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.FIREBALL, _Hip.position);
            FunctionsC.SpawnExplosionScar(_Hip.position);

            var smokeParts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.EXPLOSION_SMOKE)[0];
            var spawnPos = _Hip.position;
            spawnPos.y = 1f;
            smokeParts.transform.position = spawnPos;
            smokeParts.Emit(3);
          }
          else
            PlaySound("Ragdoll/Punch", 0.9f, 1.1f);
        }
      }

      if (spawnBlood) SpawnBlood(damageSource, spawnGiblets);
    }
    return true;
  }

  // Grab a ragdoll in front of
  public bool _grappling { get { return _grapplee != null; } }
  public bool _grappled;
  public ActiveRagdoll _grappler, _grapplee;
  float _lastTryGrapple;
  float _kickTimer, _kickTimer_start;
  bool _kicking;
  public void Grapple(bool gentle)
  {

    // Throw grappled ragdoll
    if (_grapplee != null)
    {

      // If not player, throw
      if (!gentle)
      {
        _grapplee.TakeDamage(
          new RagdollDamageSource()
          {
            Source = this,

            HitForce = Vector3.zero,

            Damage = 100,
            DamageSource = _Hip.position,
            DamageSourceType = DamageSourceType.MELEE,

            SpawnBlood = false,
            SpawnGiblets = false
          });
        _grapplee._Hip.AddForce(_Controller.forward * (1000f + Random.value * 250f));
        _grapplee._Hip.AddTorque(new Vector3(Random.value < 0.5f ? -1f : 1f, 0f, 0f) * 10000000f);

        PlaySound("Ragdoll/Neck_snap", 0.85f, 1.2f);
      }

      // Else, gently let go (?)
      else
      {
        _grapplee._Controller.position = _grapplee._Hip.position;
        _grapplee._Controller.rotation = _Controller.rotation;

        var agent = _grapplee._IsPlayer ? (_grapplee._PlayerScript?._agent) : (_grapplee._EnemyScript?._agent);
        if (agent != null)
        {
          agent.enabled = true;
        }

        // Check armor perk
        if (_PlayerScript?.HasPerk(Shop.Perk.PerkType.GRAPPLE_MASTER) ?? false)
        {
          if (_grapplee._hasArmor)
          {
            _grapplee.RemoveArmor(Vector3.zero);
            _grapplee._health = 1;
          }
        }
      }
      _grapplee._grappled = false;
      _grapplee._grappler = null;
      _grapplee = null;

      PlaySound("Enemies/Grapple", 0.65f, 0.8f);
    }

    // Grapple a ragdoll
    else if (Time.time - _lastTryGrapple > 0.5f)
    {
      _lastTryGrapple = Time.time;

      IEnumerator TryGrapple()
      {

        // FX
        PlaySound("Enemies/Grapple", 0.9f, 1.1f);

        // Try a few times to feel better with controls
        var i = 4;
        while (i-- > 0)
        {

          // Raycast for ragdoll
          RaycastHit hit;
          ToggleRaycasting(false);
          var dir = _Controller.forward;
          if (i == 2)
            dir += _Controller.right * 0.25f;
          else if (i == 1)
            dir += -_Controller.right * 0.25f;
          if (Physics.SphereCast(_spine != null ? _spine.transform.position : _Hip.transform.position, 0.3f, dir, out hit, 0.75f, GameResources._Layermask_Ragdoll))
          {
            var ragdoll = ActiveRagdoll.GetRagdoll(hit.collider.gameObject);
            if (ragdoll == null)
            {
              continue;
            }

            // Check facing somewhat away dir
            //Debug.Log((_Controller.forward - ragdoll._Controller.forward).magnitude);
            if ((_Controller.forward - ragdoll._Controller.forward).magnitude > 1.1f || (ragdoll._EnemyScript?.IsChaser() ?? false))
            {
              continue;
            }

            // Grab ragdoll
            Grapple(ragdoll);
            break;
          }

          yield return new WaitForSeconds(0.1f);
          if (_IsDead) break;
        }

        // Clean up
        ToggleRaycasting(true);
      }
      GameScript.s_Singleton.StartCoroutine(TryGrapple());

      /*/ Kick
      else if (!_kicking && Time.time - _kickTimer_start >= 2f)
      {
        _kickTimer = Time.time - 1f;
        _kickTimer_start = Time.time;
        _kicking = true;
      }*/
    }

  }

  void Grapple(ActiveRagdoll other)
  {
    if (_grappling) return;

    _grapplee = other;
    other._grappled = true;
    other._grappler = this;
    var agent = other._IsPlayer ? (other._PlayerScript?._agent) : (other._EnemyScript?._agent);
    if (agent != null)
    {
      agent.enabled = false;
    }

    // Check armor perk
    if (_PlayerScript?.HasPerk(Shop.Perk.PerkType.GRAPPLE_MASTER) ?? false)
    {
      other.AddArmor();
      other._health = 3;
    }

    // Fire events
    other._EnemyScript?.OnGrappled();
    _ItemL?.OnGrappled();
    _ItemR?.OnGrappled();
  }

  public void HideCompletely()
  {
    _renderer.enabled = false;
    _ItemL?.gameObject.SetActive(false);
    _ItemR?.gameObject.SetActive(false);
  }

  public void Kill(ActiveRagdoll source, DamageSourceType damageSourceType, Vector3 hitForce)
  {
    if (_IsPlayer && Settings._PLAYER_INVINCIBLE) return;

    // Disintegrate
    if (damageSourceType == DamageSourceType.FIRE)
    {
      HideCompletely();
    }

    // Add to part listener for audio
    else
      AddPartListener(_Hip);

    // Die instantly
    if (!_CanDie) return;
    Toggle(source, damageSourceType, hitForce);
    _IsDead = true;
    _time_dead = Time.unscaledTime;

    // Check grapplee
    if (_grapplee != null)
      if (!_grapplee._IsDead)
      {
        var graplee = _grapplee;
        Grapple(true);
        graplee?._EnemyScript?.OnGrapplerRemoved();
      }
  }

  public void SpawnBlood(Vector3 damageSource, bool spawnGiblets)
  {
    IEnumerator BloodFollow(ParticleSystem system)
    {
      var saveParent = system.transform.parent;
      system.transform.parent = system.transform.parent.parent;
      system.transform.position = _Hip.position;

      EnemyScript.CheckSound(system.transform.position, EnemyScript.Loudness.SUPERSOFT);
      yield return new WaitForSeconds(0.05f);

      var emission2 = system.emission;
      emission2.enabled = true;
      system.Play();
      var time = system.main.duration;
      var timeLast = Time.time;
      while (time > 0f)
      {
        time -= (Time.time - timeLast) * 1f;
        timeLast = Time.time;

        yield return new WaitForSecondsRealtime(0.02f);
        if (_Hip == null) break;

        system.transform.position = _Hip.position;
      }

      system.transform.parent = saveParent;
      if (Time.time - GameScript.s_LevelStartTime < 1f)
      {
        system.Stop();
        system.Clear();
      }

      //if(this != null)
      //  EnemyScript.CheckSound(system.transform.position, EnemyScript.Loudness.SOFT);
    }

    // Check follower
    if (_EnemyScript?.IsChaser() ?? false)
    {

      void PlaySpark()
      {
        var particles_sparks = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.SPARKS_ROBOT)[0];
        particles_sparks.transform.position = _Hip.position;
        particles_sparks.transform.Rotate(new Vector3(0f, 1f, 0f) * (Random.value * 360f));
        particles_sparks.Play();

        PlaySound("Enemies/Electric_Spark");
      }
      IEnumerator PlaySparks()
      {

        while (_Hip != null)
        {
          if (_Hip != null)
            PlaySpark();
          yield return new WaitForSeconds(0.13f + Random.value * 0.7f);
        }
      }
      PlaySound("Enemies/Electric_Shock");
      GameScript.s_Singleton.StartCoroutine(PlaySparks());
      return;
    }

    // Check global blood setting
    if (!Settings.s_SaveData.Settings.UseBlood) return;

    /// Particles
    // Confetti
    var useConfetti = LevelModule.ExtraBloodType == 1;
    if (useConfetti)
    {
      var particlesConfetti = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.CONFETTI);
      if (particlesConfetti == null || particlesConfetti.Length == 0) { }
      else
      {
        var confetti = particlesConfetti[0];

        var emissionconfetti = confetti.emission;
        emissionconfetti.enabled = false;
        confetti.transform.position = _Hip.position;
        confetti.transform.LookAt(damageSource);
        confetti.transform.Rotate(new Vector3(0f, 1f, 0f) * 180f);
        var rotationConfetti = confetti.transform.localRotation;
        rotationConfetti.eulerAngles = new Vector3(0f, rotationConfetti.eulerAngles.y, rotationConfetti.eulerAngles.z);
        confetti.transform.localRotation = rotationConfetti;
        confetti.transform.Rotate(new Vector3(1f, 0f, 0f), UnityEngine.Random.value * -20f);
        rotationConfetti = confetti.transform.localRotation;

        GameScript.s_Singleton.StartCoroutine(BloodFollow(confetti));

        /*/
        if (SettingsModule.UseSmokeFx)
        {
          var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BLOOD_SMOKE_CONFETTI)[0];
          parts.transform.position = _Hip.position + new Vector3(0f, 0.5f, 0f);
          parts.Emit(4);
        }*/


        // Audio
        PlaySound("Ragdoll/Confetti", 0.6f, 1.2f);

        // Achievement
#if UNITY_STANDALONE
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.EXTRA_USE_CONFETTI);
#endif
      }

    }

    // Blood
    else
    {

      var particlesBlood = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BLOOD);
      if (particlesBlood == null || particlesBlood.Length == 0) return;

      var blood = particlesBlood[0];

      var emissionBlood = blood.emission;
      emissionBlood.enabled = false;
      blood.transform.position = _Hip.position;// + _hip.transform.forward * 0.2f;//point;
      blood.transform.LookAt(damageSource);
      blood.transform.Rotate(new Vector3(0f, 1f, 0f) * 180f);
      var rotationBlood = blood.transform.localRotation;
      rotationBlood.eulerAngles = new Vector3(0f, rotationBlood.eulerAngles.y, rotationBlood.eulerAngles.z);
      blood.transform.localRotation = rotationBlood;
      blood.transform.Rotate(new Vector3(1f, 0f, 0f), Random.value * -20f);
      rotationBlood = blood.transform.localRotation;

      // Giblets
      if (spawnGiblets)
      {
        var bloodIndex = blood.name.Split('_')[1].ParseIntInvariant();
        var gibletIndex = -1;
        switch (bloodIndex)
        {
          case 0:
          case 8:
          case 9:
          case 11:
          case 12:
          case 13:
          case 14:

            gibletIndex = 0;
            break;

          case 3:
          case 6:
          case 10:

            gibletIndex = 1;
            break;

          case 1:
          case 2:
          case 4:
          case 5:
          case 7:

            gibletIndex = 2;
            break;
        }

        var particlesGiblet = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.GIBLETS, gibletIndex);
        if (particlesGiblet == null || particlesGiblet.Length == 0) return;

        var giblets = particlesGiblet[0];
        giblets.transform.position = _Hip.position;
        giblets.transform.LookAt(damageSource);
        giblets.transform.localRotation = rotationBlood;

        giblets.Play();
      }

      GameScript.s_Singleton.StartCoroutine(BloodFollow(blood));

      //
      if (SettingsModule.UseSmokeFx)
      {
        var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BLOOD_SMOKE)[0];
        parts.transform.position = _Hip.position + new Vector3(0f, 0.5f, 0f);
        parts.Emit(2);
      }

      // Bloody footprint
      FunctionsC.AoeHandler.RegisterAoeEffect(this, FunctionsC.AoeHandler.AoeType.BLOOD, _Hip.position, 1f, 20f);

      // Audio
      PlaySound($"Ragdoll/Blood{(/*Random.Range(0, 10) < 3 ? 1 : */0)}", 1.1f, 1.5f);
    }
  }

  public enum DamageSourceType
  {
    MELEE,
    BULLET,
    THROW_MELEE,
    EXPLOSION,
    LASER,
    LARGE_FAST_OBJECT,
    FIRE
  }

  public void Toggle(ActiveRagdoll source, DamageSourceType damageSourceType, Vector3 hitForce, bool changeColor = true)
  {
    if (_IsDead) return;

    // If about to die, save hip rotation for later
    if (!_IsRagdolled) _saveRot = _Hip.rotation;

    // Check crown
    if (LevelModule.ExtraCrownMode != 0)
      if (_hasCrown)
      {
        GameScript.s_CrownPlayer = GameScript.s_CrownEnemy = -1;
        if (source != null)
        {

          RemoveCrown();
          source.AddCrown();

          if (source._IsPlayer)
            GameScript.s_CrownPlayer = source._PlayerScript._Profile._Id;
          else
            GameScript.s_CrownEnemy = source._EnemyScript._Id;
        }
      }

    // Invert values
    _Hip.isKinematic = !_Hip.isKinematic;
    ToggleRaycasting(false);
    _IsRagdolled = !_IsRagdolled;
    if (!_IsRagdolled)
    {
      /*Ragdoll(false);
      _controller.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;

      // If not dead anymore, rise
      _reviving = true;
      GameScript._Singleton.StartCoroutine(Rise());
      if (changeColor) ChangeColor(_color, 2f);*/
    }

    else
    {
      Ragdoll(true);
      _Hip.AddForce(hitForce);
      _Controller.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
      if (changeColor) ChangeColor(_IsPlayer ? Color.black * 0.5f : Color.black, 0.8f);
    }

    if (changeColor)
    {
      // Record stats
      if (source?._IsPlayer ?? false)
      {
        Stats.RecordKill(source._PlayerScript._Id);
        if (_IsPlayer) Stats.RecordTeamkill(source._PlayerScript._Id);
      }

      // Fire function per script type
      _EnemyScript?.OnToggle(source, damageSourceType);
      _PlayerScript?.OnToggle(source, damageSourceType);

      // Check extras
      if (Settings._Extras_CanUse)
      {
        var explode_self = false;
        switch (LevelModule.ExtraBodyExplode)
        {

          // All
          case 1:
            explode_self = true;
            break;

          // Enemies
          case 2:
            explode_self = !_IsPlayer;
            break;

          // Players
          case 3:
            explode_self = _IsPlayer;
            break;
        }
        if (explode_self)
        {
          var explode_script = _Hip.gameObject.AddComponent<ExplosiveScript>();
          explode_script._explosionType = ExplosiveScript.ExplosionType.AWAY;
          explode_script._radius = 3 * 0.8f;
          explode_script.Trigger(source, (damageSourceType == DamageSourceType.MELEE ? 1f : 0.1f), false, true);
        }
      }

      // Fire item functions
      _ItemL?.OnToggle();
      _ItemR?.OnToggle();
    }
  }

  public void RefillAmmo()
  {
    _ItemL?.SetClip();
    _ItemR?.SetClip();
    _PlayerScript?.OnRefill();
  }

  // Stun the ragdoll
  public bool _IsStunned { get { return Time.time - _stunTimer < 0f; } }
  [System.NonSerialized]
  public bool _HasBeenStunned;
  float _stunTimer;
  public void Stun(float duration = 1.5f)
  {
    if (_EnemyScript?.IsChaser() ?? false) { return; }

    _HasBeenStunned = true;
    _stunTimer = Time.time + duration;
  }

  Tuple<bool, bool>[] _saveRagdollState;
  public void Ragdoll(bool toggle)
  {
    var joints = GetJoints();
    if (toggle)
    {
      _saveRagdollState = new Tuple<bool, bool>[joints.Length];
      for (var i = 0; i < joints.Length; i++)
      {
        if (joints[i] == null) continue;
        _saveRagdollState[i] = new Tuple<bool, bool>(joints[i].useSpring, joints[i].useLimits);
        joints[i].useSpring = false;
        joints[i].useLimits = true;
      }
      if (_arm_upper_l != null) _arm_upper_l.useLimits = false;
      if (_arm_upper_r != null) _arm_upper_r.useLimits = false;
    }
    else
    {
      for (var i = 0; i < joints.Length; i++)
      {
        if (joints[i] == null) continue;
        joints[i].useSpring = _saveRagdollState[i].Item1;
        joints[i].useLimits = _saveRagdollState[i].Item2;
      }
    }
  }

  public void Recoil(Vector3 dir, float force, bool overright = true)
  {
    _grappler?.Recoil(dir, force * 0.75f, overright);

    if (overright)
    {
      _ForceGlobal = MathC.Get2DVector(dir) * force;
      return;
    }
    _ForceGlobal += MathC.Get2DVector(dir) * force;
  }
  public void RecoilSimple(float force)
  {
    // Mod
    if (force < 0f)
      if (_IsPlayer && _PlayerScript.HasPerk(Shop.Perk.PerkType.THRUST))
        force *= 2f;

    //
    Recoil(_grappled ? -_Hip.transform.forward : -_Controller.forward, force);
  }

  IEnumerator Rise()
  {
    var startRot = _Hip.rotation;

    // Pick ragdoll back up using Lerp with current rotation and saved rotation from before fall
    var iter = 0f;
    while (iter < 1f)
    {
      yield return new WaitForSeconds(0.005f);
      iter += 0.07f;
      _Hip.rotation = Quaternion.Lerp(startRot, _saveRot, iter);
    }
    iter = 1f;
    _Hip.rotation = Quaternion.Lerp(startRot, _saveRot, iter);

    // Set controller to position
    _Controller.position = new Vector3(_Hip.position.x, _Controller.position.y, _Hip.position.z);
    _IsReviving = false;
  }

  bool _hasCrown;
  public void AddCrown()
  {
    if (_hasCrown) return;

    var crown = GameObject.Instantiate(GameResources._Crown).transform;

    crown.transform.parent = _head.transform;
    crown.localScale = Vector3.one * 2f;
    crown.localEulerAngles = new Vector3(-60f, 90f, 0f);
    crown.localPosition = new Vector3(0.067f, 1f, 0.095f);

    _hasCrown = true;
  }
  public void RemoveCrown()
  {
    if (!_hasCrown) return;

    GameObject.Destroy(_head.transform.GetChild(0).gameObject);

    _hasCrown = false;
  }

  bool _hasArmor;
  public void AddArmor()
  {
    if (_hasArmor) return;

    var armor = GameObject.Instantiate(GameResources._Armor);
    var helmet = armor.transform.GetChild(0);

    PlaySound("Enemies/Armor_give", 0.9f, 1.1f);

    // Equip helmet
    _head.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

    helmet.transform.parent = _head.transform;
    helmet.localScale = new Vector3(5f, 5f, 5f);
    helmet.localEulerAngles = new Vector3(-90f, 180f, 0f);
    helmet.localPosition = new Vector3(0f, 2.1f, 0.9f);

    GameObject.Destroy(armor);

    _hasArmor = true;
  }
  public void RemoveArmor(Vector3 hitForce, bool save_til_die = true)
  {
    if (!_hasArmor) return;

    PlaySound("Enemies/Armor_break", 0.9f, 1.1f);

    var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
    parts.transform.position = _Hip.position;
    parts.Play();

    var helmet = _head.transform.GetChild(0).gameObject;
    helmet.transform.parent =
      save_til_die ? _Controller.parent : GameResources._Container_Objects;

    var rb = helmet.AddComponent<Rigidbody>();
    helmet.GetComponent<BoxCollider>().enabled = true;

    rb.interpolation = RigidbodyInterpolation.Interpolate;
    rb.isKinematic = false;
    rb.AddForce(hitForce * 0.8f);
    rb.AddTorque(new Vector3(1f, 0f, 0f) * 500f);

    _head.transform.localScale = new Vector3(1f, 1f, 1f);

    _hasArmor = false;
  }

  //
  public bool _CanGrapple
  {
    get
    {
      return (
        HasMelee() && !HasTwohandedWeapon()) ||
        (_ItemL == null && !(_ItemR?._twoHanded ?? false)) ||
        (_ItemR == null && !(_ItemL?._twoHanded ?? false)
      );
    }
  }

  // Add a grappler
  public void AddGrappler(bool playSound = true)
  {

    // Checks
    if (_IsDead) return;
    if (_grappling) return;
    if (GameScript.s_EditorEnabled) return;
    if (!_CanGrapple) return;

    // Spawn enemy
    var spawn_pos = _Hip.position + Transform.forward * 0.3f;
    var enemy = EnemyScript.SpawnEnemyAt(
      new EnemyScript.SurvivalAttributes()
      {
        _enemyType = GameScript.SurvivalMode.EnemyType.KNIFE_RUN
      },
      new Vector2(spawn_pos.x, spawn_pos.z),
      true
    );

    // Set grapplee
    Grapple(enemy._Ragdoll);

    // FX
    if (playSound)
    {
      PlaySound("Ragdoll/Combust");
    }
    FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.SMOKEBALL, spawn_pos);
  }

  public bool HasWeapon()
  {
    return (_ItemL != null) || (_ItemR != null);
  }
  public bool HasTwohandedWeapon()
  {
    return (_ItemL?._twoHanded ?? false) || (_ItemR?._twoHanded ?? false);
  }
  public bool HasGun()
  {
    return (_ItemL?.IsGun() ?? false) || (_ItemR?.IsGun() ?? false);
  }
  public bool HasAutomatic()
  {
    return (_ItemL != null && _ItemL._fireMode == ItemScript.FireMode.AUTOMATIC) || (_ItemR != null && _ItemR._fireMode == ItemScript.FireMode.AUTOMATIC);
  }
  public bool HasSilencedWeapon()
  {
    return (_ItemL != null && _ItemL._silenced) || (_ItemR != null && _ItemR._silenced);
  }
  public bool HasMelee()
  {
    return (_ItemL?.IsMelee() ?? false) || (_ItemR?.IsMelee() ?? false);
  }
  public bool HasEmpty()
  {
    return (_ItemL?.IsEmpty() ?? false) || (_ItemR?.IsEmpty() ?? false);
  }
  public bool HasItem(GameScript.ItemManager.Items item)
  {
    return (
      (_ItemL?._type ?? GameScript.ItemManager.Items.NONE) == item ||
      (_ItemR?._type ?? GameScript.ItemManager.Items.NONE) == item);
  }
  public bool HasBulletDeflector()
  {
    return
      (_ItemL?._IsBulletDeflector ?? false) ||
      (_ItemR?._IsBulletDeflector ?? false);
  }

  public bool Active()
  {
    return !_IsRagdolled && !_IsReviving;
  }

  // Return a true if o is in _parts
  public bool IsSelf(GameObject o)
  {
    foreach (GameObject part in _parts)
      if (GameObject.ReferenceEquals(part, o))
        return true;
    return false;
  }

  public void ToggleRaycasting(bool enable, bool override_ = false)
  {
    if (_IsRagdolled && !override_) return;
    foreach (var part in _parts)
    {
      if (part == null) continue;
      part.layer = (enable ? 10 : 2);
    }
  }

  /*[BurstCompile]
  struct GetRagdollJob : IJob
  {
    public int _id;
    [ReadOnly]
    public NativeArray<int> _ids;
    public NativeArray<int> _index;

    public void Execute()
    {
      for (; _index[0] < _ids.Length;)
      {
        if (_id == _ids[_index[0]])
          return;
        _index[0]++;
      }
      _index[0] = -1;
    }
  }*/

  public static void Jobs_Init()
  {
    /*_GetRagdollJob = new GetRagdollJob();
    _GetRagdollJob._ids = new NativeArray<int>(_Parts.ToArray(), Allocator.Persistent);
    _GetRagdollJob._index = new NativeArray<int>(1, Allocator.Persistent);*/
  }
  public static void Jobs_Clean()
  {
    /*_GetRagdollJob._ids.Dispose();
    _GetRagdollJob._index.Dispose();*/
  }

  //static GetRagdollJob _GetRagdollJob;
  public static ActiveRagdoll GetRagdoll(GameObject o)
  {
    /*/ Return if null parameters
    if (_Ragdolls == null || o == null) return null;
    // Set up job parameters
    _GetRagdollJob._id = o.GetInstanceID();
    _GetRagdollJob._index[0] = 0;
    // Schedule and complete the job
    JobHandle handle = _GetRagdollJob.Schedule();
    handle.Complete();
    // Read output
    int index = _GetRagdollJob._index[0];
    // Use output
    if (index == -1)
      return null;
    return _Ragdolls[index / 11];*/
    foreach (var r in s_Ragdolls)
      if (r.IsSelf(o)) return r;
    return null;
  }

  TextBubbleScript _bubbleScript;
  public float _lastBubbleScriptTime;
  /// <summary>
  /// Displays a text bubble over a ragdoll to convey something
  /// </summary>
  /// <param name="text"></param>
  public void DisplayText(string text, float size = 0.75f)
  {
    if (_EnemyScript != null && _EnemyScript.IsChaser()) return;
    if (_bubbleScript != null && (Time.time - _lastBubbleScriptTime) < (_IsPlayer ? 0.5f : 1f))
    {
      _bubbleScript._textMesh.text = text;
      return;
    }
    // Create a new container for the text
    GameObject g = GameObject.Instantiate(Resources.Load("TextBubble") as GameObject);
    g.transform.parent = GameResources._Container_Objects;
    g.transform.position = _head.transform.position;
    // Get the TextBubbleScript and init with text, position, and colot
    TextBubbleScript bubbleScript = g.GetComponent<TextBubbleScript>();
    bubbleScript.Init(text, _head.transform, _Color).fontSize = 3.5f * size;
    // Save for later
    _lastBubbleScriptTime = Time.time;
    _bubbleScript = bubbleScript;
  }

  void SetCollisionMode(CollisionDetectionMode mode)
  {
    foreach (GameObject t in _parts)
    {
      if (t.name.Equals("Hip") && mode == CollisionDetectionMode.Continuous) mode = CollisionDetectionMode.ContinuousSpeculative;
      Rigidbody rb = t.GetComponent<Rigidbody>();
      if (rb == null) continue;
      rb.collisionDetectionMode = mode;
    }
  }

  /// <summary>
  /// Return all the body parts in the ActiveRagdoll in array of GameObjects; head, arms, legs, etc.
  /// </summary>
  /// <returns></returns>
  GameObject[] GetParts()
  {
    // If hip is null, ActiveRagdoll is null. Throw exception
    if (_Hip == null)
      throw new System.NullReferenceException("Attempting to access a null ActiveRagdoll.");
    // Manually return body parts
    GameObject[] returnList = new GameObject[11];
    // Hip
    returnList[0] = _Hip.gameObject;
    // Upper leg l
    returnList[1] = returnList[0].transform.GetChild(0).gameObject;
    // Upper leg r
    returnList[2] = returnList[0].transform.GetChild(1).gameObject;
    // Lower leg l
    returnList[3] = returnList[1].transform.GetChild(0).gameObject;
    // Lower leg r
    returnList[4] = returnList[2].transform.GetChild(0).gameObject;
    // Spine
    returnList[5] = returnList[0].transform.GetChild(2).GetChild(0).gameObject;
    // Arm upper l
    returnList[6] = returnList[5].transform.GetChild(0).GetChild(0).GetChild(0).gameObject;
    // Arm upper r
    returnList[7] = returnList[5].transform.GetChild(0).GetChild(1).GetChild(0).gameObject;
    // Arm lower l
    returnList[8] = returnList[6].transform.GetChild(0).gameObject;
    // Arm lower r
    returnList[9] = returnList[7].transform.GetChild(0).gameObject;
    // Head
    returnList[10] = returnList[5].transform.GetChild(0).GetChild(2).gameObject;
    return returnList;
  }
  HingeJoint[] GetJoints()
  {
    return new HingeJoint[]
    {
      _spine, _arm_upper_l, _head, _arm_upper_r, _arm_lower_l, _arm_lower_r, _leg_upper_l, _leg_upper_r, _leg_lower_l, _leg_lower_r
    };
  }

  public bool Dismember(HingeJoint joint)
  {
    return Dismember(joint, Vector3.zero);
  }
  public bool Dismember(HingeJoint joint, Vector3 force)
  {
    if (!_CanDie || !_IsDead) return false;

    // Check if already dismembered
    if (joint == null) return false;

    // Dismember
    var t = joint.transform;
    joint.gameObject.layer = 2;
    GameObject.Destroy(joint);
    t.parent = _Hip.transform.parent;

    // Make sure has collider (arm joint)
    if (t.GetComponent<Collider>() == null)
    {
      t.gameObject.AddComponent<BoxCollider>().center = new Vector3(0f, 1f, 0f);
    }

    // Set to render outside of camera
    _renderer.updateWhenOffscreen = true;

    // Add force to body part
    if (force.magnitude != 0f)
    {
      var rb = t.GetComponent<Rigidbody>();
      AddPartListener(rb);
      rb.AddForce(force * 0.25f);
    }
    return true;
  }
  public bool DismemberRandom(Vector3 force)
  {
    return Dismember(GetRandomJoint(), force);
  }
  public void DismemberRandomTimes(Vector3 force, int n)
  {
    for (var i = 0; i < n; i++)
    {
      Dismember(GetRandomJoint(), force * (0.7f + Random.value * 0.4f));
    }
  }

  public HingeJoint GetRandomJoint()
  {
    switch (Random.Range(0, 10))
    {
      case 0:
        return _spine;
      case 1:
        return _arm_upper_l;
      case 2:
        return _head;
      case 3:
        return _arm_upper_r;
      case 4:
        return _arm_lower_l;
      case 5:
        return _arm_lower_r;
      case 6:
        return _leg_upper_l;
      case 7:
        return _leg_upper_r;
      case 8:
        return _leg_lower_l;
      case 9:
        return _leg_lower_r;

      default:
        return null;
    }
  }

  public void PlaySound(string soundPath, float min = 1f, float max = 1f, SfxManager.AudioClass audioClass = SfxManager.AudioClass.NONE, bool changePitch = true)
  {
    SfxManager.PlayAudioSourceSimple(_Controller.position, soundPath, min, max, audioClass, changePitch);
  }

  public static void Reset()
  {
    s_ID = 0;
    s_Ragdolls = null;
    Rigidbody_Handler.Reset();
    Jobs_Clean();
    SfxManager.Reset();
    UtilityScript.Reset();
  }
  public static void SoftReset()
  {
    if (PlayerScript.s_Players == null || s_Ragdolls == null) return;
    // Remove current ragdolls if dead
    for (var i = s_Ragdolls.Count - 1; i > 0; i--)
    {
      var r = s_Ragdolls[i];
      if (r._IsDead && r._IsPlayer)
      {
        s_Ragdolls.Remove(r);
        PlayerScript.s_Players.Remove(r._PlayerScript);
        if (r._Controller == null)
          continue;
        GameObject.Destroy(r._Controller.parent.gameObject);
      }
    }
    // Add players' ragdolls
    s_Ragdolls.Clear();
    foreach (var p in PlayerScript.s_Players)
    {
      s_Ragdolls.Add(p._Ragdoll);
    }
  }
}
