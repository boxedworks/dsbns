using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

public class ActiveRagdoll
{
  public static List<ActiveRagdoll> _Ragdolls;
  public static int _ID;
  public int _id;
  public Transform transform, _controller;
  // Body parts
  public Rigidbody _hip;
  public HingeJoint _head, _spine, _arm_upper_l, _arm_upper_r, _arm_lower_l, _arm_lower_r, _leg_upper_l, _leg_upper_r, _leg_lower_l, _leg_lower_r;
  // Item info
  public ItemScript _itemL, _itemR;

  public EnemyScript _enemyScript;
  public PlayerScript _playerScript;

  public bool _forceRun;

  public Vector3 _distance;

  // Color of the ragdoll
  public Color _color;

  // If > 0f, provide a speed boost in the forward direction (used for double knives)
  float _forwardDashTimer;

  // What frame the hinge 'animations' are on
  float _movementIter, _movementIter2;
  public float _rotSpeed, _time_dead;

  bool _footprint;

  public bool _ragdolled,
    _reviving,
    _isPlayer,
    _dead,
    _disabled,
    _canMove,
    _canReceiveInput,
    _diving,
    _canDie;

  public bool _swinging
  {
    get { return (_itemL != null && _itemL._swinging) || (_itemR != null && _itemR._swinging); }
  }
  public bool _items_in_use
  {
    get { return (_itemL != null && _itemL.Used()) || (_itemR != null && _itemR.Used()); }
  }
  public bool _reloading
  {
    get { return (_itemL != null && _itemL._reloading) || (_itemR != null && _itemR._reloading); }
  }

  public Vector3 _force;
  public float _forwardForce;

  // Height above ground
  static readonly public Vector3 _GROUNDHEIGHT = new Vector3(0f, 0.73f, 0f);

  // Holds rotation before Toggle()
  Quaternion _saveRot;

  public int _health;

  public GameObject[] _parts;

  static List<System.Tuple<Material, Material>> _Materials_Ragdoll;

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
    _id = _ID++;

    // Add to static _Ragdolls list
    if (_Ragdolls == null) _Ragdolls = new List<ActiveRagdoll>();
    _Ragdolls.Add(this);

    // Defaults
    _controller = follow;
    ragdoll.name = "Ragdoll";
    transform = ragdoll.transform;
    _time_dead = -1f;

    // Set materials
    if (_Materials_Ragdoll == null) _Materials_Ragdoll = new List<System.Tuple<Material, Material>>();
    _renderer = transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
    if (_id == _Materials_Ragdoll.Count) _Materials_Ragdoll.Add(new System.Tuple<Material, Material>(new Material(_renderer.sharedMaterials[0]), new Material(_renderer.sharedMaterials[1])));
    Resources.UnloadAsset(_renderer.sharedMaterials[0]);
    Resources.UnloadAsset(_renderer.sharedMaterials[1]);
    _renderer.sharedMaterials = new Material[] { _Materials_Ragdoll[_id].Item1, _Materials_Ragdoll[_id].Item2 };
    // Register enemy / player script
    _enemyScript = follow.GetComponent<EnemyScript>();
    _playerScript = follow.GetComponent<PlayerScript>();
    // Set default health
    _health = 1;
    // Index body parts
    _hip = ragdoll.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<Rigidbody>();
    _leg_upper_l = _hip.transform.GetChild(0).GetComponent<HingeJoint>();
    _leg_upper_r = _hip.transform.GetChild(1).GetComponent<HingeJoint>();
    _leg_lower_l = _leg_upper_l.transform.GetChild(0).GetComponent<HingeJoint>();
    _leg_lower_r = _leg_upper_r.transform.GetChild(0).GetComponent<HingeJoint>();
    _spine = _hip.transform.GetChild(2).GetChild(0).GetComponent<HingeJoint>();
    /*_arm_upper_l = _spine.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<HingeJoint>();
    _arm_upper_r = _spine.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<HingeJoint>();
    _arm_lower_l = _arm_upper_l.transform.GetChild(0).GetComponent<HingeJoint>();
    _arm_lower_r = _arm_upper_r.transform.GetChild(0).GetComponent<HingeJoint>();*/
    _head = _spine.transform.GetChild(0).GetChild(2).GetComponent<HingeJoint>();

    _parts = GetParts();
    _transform_parts = new Parts(this);

    _color = Color.blue * 1.3f;

    _rotSpeed = 1f;

    _canMove = _canReceiveInput = true;

    _canDie = true;

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
    if (_itemR != null && _itemR.IsGun())
      Debug.DrawRay(_itemR.transform.position, MathC.Get2DVector(_itemR.transform.forward) * 100f);
    if (_itemL != null && _itemL.IsGun())
      Debug.DrawRay(_itemL.transform.position, MathC.Get2DVector(_itemL.transform.forward) * 100f);
    Debug.DrawRay(_hip.position, _hip.transform.forward * 100f, Color.yellow);
#endif

    var dt = (_playerScript != null ? Time.unscaledDeltaTime : Time.deltaTime);

    // Update grabbed rd
    if (_grapplee != null)
    {
      if (_grapplee._dead)
      {
        _grapplee = null;
      }
      else
      {
        // Get melee side
        var left_weapon = _itemL != null && _itemL.IsMelee();

        _grapplee._hip.position = _hip.position + _controller.forward * 0.45f + _controller.right * 0.09f * (left_weapon ? -1f : 1f);
        _grapplee._hip.rotation = _hip.rotation;
      }
    }

    // Update arm spread
    if (_spine != null)
    {
      if (_itemL != null && _itemL._twoHanded) { }
      else
      {
        var spread_speed = 20f * dt;
        if (_itemL != null)
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
        if (_itemR != null)
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

    if (!_ragdolled)
    {
      // Add gun recoil
      if (_force.magnitude > 0f)
      {
        var force_normalize = _force.magnitude < 1f ? _force : _force.normalized;
        var force_apply = _force * Time.deltaTime * 10f;

        var agent = (_isPlayer ? _playerScript._agent : _enemyScript._agent);
        agent.Move(force_apply);
        _force -= force_apply;
      }
      if (_forwardForce > 0f)
      {
        var max_velocity = 3.2f;
        var force_normalized = _forwardForce > max_velocity ? max_velocity : _forwardForce;
        var force_apply = force_normalized * MathC.Get2DVector(_hip.transform.forward) * Time.deltaTime * 10f;

        var agent = (_isPlayer ? _playerScript._agent : _enemyScript._agent);
        agent.Move(force_apply);
        _forwardForce -= force_normalized;
      }
      // Only update if enabled
      if (!_canReceiveInput) return;

      // If double knives or bat/sword, move forward when swinging
      if (_swinging)
      {
        if ((_itemL != null && (_itemL._twoHanded || _itemL._runWhileUse)) || (_itemR != null && _itemR._runWhileUse) ||
          (_itemL != null && _itemL._type == GameScript.ItemManager.Items.KNIFE && _itemR != null && _itemR._type == GameScript.ItemManager.Items.KNIFE))
        {
          var amount = 1.2f;
          var dis = new Vector3(_controller.forward.x, 0f, _controller.forward.z).normalized * PlayerScript.MOVESPEED * amount * Time.deltaTime;

          var agent = (_isPlayer ? _playerScript._agent : _enemyScript._agent);
          agent.Move(dis);
        }
      }

      // Check hip
      if(_hip.transform.position.y > 0f){
        Debug.Log("defective ragdoll");
        Kill(null, DamageSourceType.MELEE, Vector3.zero);
        return;
      }
    }

    // Move / rotate based on a rigidbody
    var movePos = _controller.position + _GROUNDHEIGHT;
    _distance = (movePos - _hip.position) * (Time.timeScale < 1f ? 0.3f : 1f);

    // Moving ragdoll too fast will break joints, clamp to .35f magnitude
    if (_distance.magnitude > 0.35f)
    {
      movePos = _hip.position + _distance.normalized * 0.35f;
      _distance = (movePos - _hip.position) * (Time.timeScale < 1f ? 0.3f : 1f);
    }

    // If can't move, set movepos and distance to origin
    if (!_canMove || _grappled)
    {
      movePos = _hip.position;
      _distance = Vector3.zero;
    }

    // Stand if not moving, else iterate
    if (_distance.magnitude < 0.005f)
      _movementIter2 += (0.5f - _movementIter2) * dt * 12f;
    else
    {
      _totalDistanceMoved += _distance.magnitude;
      var save = _movementIter2;
      _movementIter2 = Mathf.PingPong(_movementIter * 2.8f, 1f);

      // Play footsteps  based on controller distance moved
      if (!_ragdolled && !_grappled && ((save < 0.5f && _movementIter2 >= 0.5f) || (save > 0.5f && _movementIter2 <= 0.5f)))
      {
        // If player, send footstep sound to enemies to check for detection
        if (_isPlayer && _playerScript._CanDetect && _distance.magnitude > 0.1f)
          EnemyScript.CheckSound(_controller.position, (_distance.magnitude > 0.2f ? EnemyScript.Loudness.SOFT : (EnemyScript.Loudness.SUPERSOFT)));

        // Sfx
        SfxManager.PlayAudioSourceSimple(_controller.position, SceneThemes._footstep.clip, SceneThemes._footstep.volume, 0.9f, 1.1f, SfxManager.AudioClass.FOOTSTEP);

        // Footprint
        var footprint_pos = _footprint ? _leg_lower_l.transform.position : _leg_lower_r.transform.position;
        footprint_pos.y -= 0.4f;
        _footprint = !_footprint;
        FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.FOOTPRINT, footprint_pos);
      }
    }
    var rot = _hip.rotation;
    var f = _controller.rotation.eulerAngles.y;
    if (f > 180.0f)
      f -= 360.0f;
    rot.eulerAngles = new Vector3(rot.eulerAngles.x, f, rot.eulerAngles.z);

    // Don't move to ragdoll if is not active
    if (Active())
    {
      // Move / rotate hip (base)
      _hip.MovePosition(movePos);
      if (_isPlayer)
      {
        dt = Time.unscaledDeltaTime;
      }

      _hip.MoveRotation(Quaternion.RotateTowards(_hip.rotation, rot, dt * 14f * _rotSpeed * Mathf.Abs(Quaternion.Angle(_hip.rotation, rot))));

      // Use iter to move joints
      _movementIter += (_distance.magnitude / 3f) * Time.deltaTime * 65f;
    }
    else
    {
      _controller.position = _hip.position;

      // Use iter to move joints
      _movementIter += (_distance.magnitude / 10f) * Time.deltaTime * 50f;

      // Check stun FX
      if (!_dead && _stunned)
      {
        if (Time.time - _confusedTimer > 0f)
        {
          _confusedTimer = Time.time + Random.Range(0.2f, 0.5f);
          var p = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.CONFUSED)[0];
          p.transform.position = _head != null ? _head.transform.position : _controller.position;
          p.Play();
        }
      }
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
          if (Physics.SphereCast(_controller.position, 0.25f, _controller.forward, out hit, 0.5f, EnemyScript._Layermask_Ragdoll))
          {
            var ragdoll = ActiveRagdoll.GetRagdoll(hit.collider.gameObject);
            if (ragdoll != null)
            {
              var hitForce = MathC.Get2DVector(
                -(_hip.transform.position - ragdoll._hip.transform.position).normalized * (4000f + (Random.value * 2000f)) * 1f
              );
              if (ragdoll.TakeDamage(
                new RagdollDamageSource()
                {
                  Source = this,

                  HitForce = hitForce,

                  Damage = 1,
                  DamageSource = _hip.position,
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

  public void SetActive(bool toggle)
  {
    transform.gameObject.SetActive(toggle);
  }
  public bool IsActive()
  {
    return transform.gameObject.activeSelf;
  }

  // Remove all HingeJoints, Rigidbodies, and Colliders except for _Hip
  public void Disable()
  {
    if (_disabled) return;
    _disabled = true;
    IEnumerator delayDisable()
    {
      yield return new WaitForSecondsRealtime(5f);
      if (_hip != null) _hip.isKinematic = true;
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
    _enemyScript.StartCoroutine(delayDisable());
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
    FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.SMOKEBALL, _hip.position);

    // Set timer
    if (toggle)
      _invisibility_timer += 1f;
    else
      _invisibility_timer = 0f;
  }

  // Use item(s) in hand(s)
  public void UseLeft()
  {
    _itemL?.UseDown();
  }
  public void UseRight()
  {
    _itemR?.UseDown();
  }

  public void UseLeftDown()
  {
    if (_itemL == null) return;
    // Check if should hold weapon down / charge
    {
      bool two_handed = _itemL._twoHanded;
      if (!two_handed)
      {
        _itemL.UseDown();
        return;
      }
    }
  }
  public void UseLeftUp()
  {
    if (_itemL == null || _dead) return;
    // Check if should release weapon down / charge
    {
      bool two_handed = _itemL._twoHanded;
      if (!two_handed)
      {
        _itemL.UseUp();
        return;
      }
      _itemL.UseDown();
    }
  }
  public void UseRightDown()
  {
    if (_itemR != null)
      _itemR.UseDown();
  }
  public void UseRightUp()
  {
    if (_itemR != null)
      _itemR.UseUp();
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

  public void EquipItem(GameScript.ItemManager.Items itemType, Side side, int clipSize = -1, float useTime = -1f)
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
    var script = item.GetComponent<ItemScript>();

    bool two_hands = script._twoHanded;
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
    float side_mod = side == Side.LEFT ? -1f : 1f;

    // Remove joints for better aiming
    RemoveArmJoint(side);
    if (two_hands) RemoveArmJoint(side_other);

    // Decide which hand to place in
    Transform arm;
    if (side == Side.LEFT || two_hands)
    {
      arm = _transform_parts._arm_lower_l;
      _itemL = script;
    }
    else
    {
      arm = _transform_parts._arm_lower_r;
      _itemR = script;
    }
    item.transform.parent = arm.GetChild(0);
    script.OnEquip(this, side, clipSize, useTime);
  }

  public void UnequipItem(Side side)
  {
    var item = (side == Side.LEFT ? _itemL : _itemR);
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
    if (side == Side.LEFT) _itemL = null;
    else _itemR = null;
  }

  // Switch item hands
  public void SwapItemHands(int index)
  {
    //_playerScript?.ResetLoadout();

    SwapItems(
      System.Tuple.Create(
        _itemR == null ? GameScript.ItemManager.Items.NONE : _itemR._type,
        _itemR != null ? _itemR.Clip() : -1,
        _itemR != null ? _itemR._useTime : -1f
      ),
      System.Tuple.Create(
        _itemL == null ? GameScript.ItemManager.Items.NONE : _itemL._type,
        _itemL != null ? _itemL.Clip() : -1,
        _itemL != null ? _itemL._useTime : -1f
      ),
      index
    );
  }

  float _lastItemSwap;
  public bool CanSwapWeapons()
  {
    return !_dead && !_swinging && Time.time - _lastItemSwap > 0.5f && !_reloading && !_items_in_use;
  }

  // Change weapons mid-game
  public void SwapItems(System.Tuple<GameScript.ItemManager.Items, int, float> item_left, System.Tuple<GameScript.ItemManager.Items, int, float> item_right, int index, bool checkCanSwap = true)
  {
    if (!CanSwapWeapons() && checkCanSwap) return;
    if (_itemL != null && _itemL._twoHanded || _itemR != null && _itemR._twoHanded)
    {
      //DisplayText("two handed problems");
      //return;
    }
    _lastItemSwap = Time.time;
    var itemL_type = item_left.Item1;
    var itemL_clip = item_left.Item2;
    var itemL_useTime = item_left.Item3;
    var itemR_type = item_right.Item1;
    var itemR_clip = item_right.Item2;
    var itemR_useTime = item_right.Item3;

    // Equip items
    if (itemL_type != GameScript.ItemManager.Items.NONE)
      EquipItem(itemL_type, Side.LEFT, itemL_clip, itemL_useTime);
    else
    {
      AddArmJoint(Side.LEFT);
      UnequipItem(Side.LEFT);
    }
    if (itemR_type != GameScript.ItemManager.Items.NONE)
      EquipItem(itemR_type, Side.RIGHT, itemR_clip, itemR_useTime);
    else
    {
      if (_itemL == null || !_itemL._twoHanded)
      {
        AddArmJoint(Side.RIGHT);
        UnequipItem(Side.RIGHT);
      }
    }

    if (_isPlayer)
    {
      if (index == 0)
      {
        _playerScript._Profile._equipment._item_left0 = itemL_type;
        _playerScript._Profile._equipment._item_right0 = itemR_type;
      }
      else
      {
        _playerScript._Profile._equipment._item_left1 = itemL_type;
        _playerScript._Profile._equipment._item_right1 = itemR_type;
      }
    }

    // Sfx
    PlaySound("Ragdoll/Weapon_Switch");
  }

  // Check for and reload items in both hands
  public void Reload()
  {
    // Don't reload if dead
    if (_dead) return;
    // Left item
    if (_itemL != null && _itemL.CanReload() && !_itemL.IsChargeWeapon())
    {
      _itemL.Reload();
      // Check player settings
      if (_isPlayer && !_playerScript._Profile._reloadSidesSameTime) return;
      else
      {
        IEnumerator delayedReload()
        {
          yield return new WaitForSeconds(0.1f);
          if (_itemR != null && _itemR.CanReload())
            _itemR.Reload();
        }
        GameScript._s_Singleton.StartCoroutine(delayedReload());
      }
    }
    //else if (_itemL != null && !_itemL._melee && _isPlayer) DisplayText("already reloading L");
    // Right item
    if (_itemR != null && _itemR.CanReload() && !_itemR.IsChargeWeapon())
      _itemR.Reload();
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
    if (_isPlayer) _playerScript.OnTriggerEnter(other);

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
    else if (other.name.Equals("Goal") && !_dead && _isPlayer)
    {
      other.transform.parent.GetComponent<Powerup>()
        .Activate(this);
    }
  }

  public void OnTriggerExit(Collider other)
  {
    /*/ Explode mine stepping on
    if (other.name.Equals("Mine"))
    {
      ExplosiveScript s = other.transform.parent.GetComponent<ExplosiveScript>();
      s.Trigger();
    }*/
    if (_isPlayer) _playerScript.OnTriggerExit(other);
  }
  public void OnTriggerStay(Collider other)
  {
    if (_isPlayer) _playerScript.OnTriggerStay(other);
  }

  // Handle body part noises
  public static class BodyPart_Handler
  {
    static List<System.Tuple<Rigidbody, ActiveRagdoll, float, float>> _Rbs;
    static int _RbsIndex;

    //
    public static void Init()
    {
      _Rbs = new List<System.Tuple<Rigidbody, ActiveRagdoll, float, float>>();
    }

    //
    public static void Update()
    {
      //Debug.Log(_Rbs.Count);
      // If no rbs, ignore
      if (_Rbs.Count == 0) return;

      for (var u = 0; u < (int)Mathf.Clamp(3, 1, _Rbs.Count); u++)
      {

        // Gather data
        var index = _RbsIndex++ % _Rbs.Count;
        var rb_data = _Rbs[index];
        var rag = rb_data.Item2;

        // Check for null and remove
        if (rb_data.Item1 == null || rb_data.Item2 == null)
        {
          //Debug.Log("removed");
          _Rbs.RemoveAt(index);
          return;
        }

        //
        var rb = rb_data.Item1;
        var mag = rb.velocity.magnitude;
        var mag_old = rb_data.Item3;
        var last_sound = rb_data.Item4;

        //Debug.Log($"{mag_old} .. {mag} .. {rb.velocity}");

        //
        var min = 4.5f;
        var max = 0.8f;
        if (mag > mag_old && mag > min)
        {
          _Rbs[index] = System.Tuple.Create(rb, rag, mag, last_sound);
          return;
        }

        //
        if (mag < (mag_old > 13f ? max * 2f : max) && Time.time - last_sound > 0.25f && mag_old > mag && mag_old > 1f)
        {
          _Rbs[index] = System.Tuple.Create(rb, rag, mag, Time.time);

          //Debug.Log($"sound: {rb.name} {mag_old} .. {mag}");

          var soundName = mag_old > 13f ? "Thud_loud" : "Thud";
          SfxManager.PlayAudioSourceSimple(rb.position, $"Ragdoll/{soundName}", 0.7f, 1.25f, SfxManager.AudioClass.NONE);

          //FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.SMOKE_WHITE, rb.position);
        }
      }
    }

    public static void AddListener(Rigidbody rb, ActiveRagdoll rag)
    {
      _Rbs.Add(System.Tuple.Create(rb, rag, 0f, 0f));
    }

    public static void Reset()
    {
      _Rbs.Clear();
    }
  }

  void AddPartListener(Rigidbody rb)
  {
    BodyPart_Handler.AddListener(rb, this);
  }

  Coroutine _color_Coroutine;
  public void ChangeColor(Color c, float lerpAmount = 0f)
  {
    if (Color.Equals(_color, Color.blue * 1.3f))
      _color = c;

    // Gather mesh renderers by amount of materials and final color
    var mesh = transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();
    if (lerpAmount == 0f)
    {
      Color startColor0 = mesh.sharedMaterials[0].color,
       startColor1 = mesh.sharedMaterials[1].color;
      SetLerpAmount(ref mesh, c, 1f, startColor0, startColor1);
      _playerScript?.ChangeRingColor(c);
      return;
    }
    if (_color_Coroutine != null)
      GameScript._s_Singleton.StopCoroutine(_color_Coroutine);
    _color_Coroutine = GameScript._s_Singleton.StartCoroutine(LerpColor(mesh, c, lerpAmount));
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
    float timer = 0f, waitTime = 0.05f;
    // Skin
    Color startColor0 = mesh.sharedMaterials[1].color,
    // Clothes
      startColor1 = mesh.sharedMaterials[0].color;
    while (true)
    {
      yield return new WaitForSeconds(waitTime);
      if (_hip == null) break;
      timer = Mathf.Clamp(timer + 0.07f, 0f, lerpAmount);
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
    if (_enemyScript != null && _enemyScript._enemyType == EnemyScript.EnemyType.ROBOT && damage <= 10f)
    {
      if (Time.time - _lastMetalHit > 0.05f && damageSourceType == DamageSourceType.BULLET)
      {
        _lastMetalHit = Time.time;
        PlaySound("Enemies/Metal_hit", 0.8f, 1.2f);

        var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
        parts.transform.position = _hip.position;
        parts.Play();
      }
      return false;
    }

    var save_health = _health;
    if (!_dead)
    {
      _health -= damage;

      if (_health <= 0f)
      {
        Kill(source, damageSourceType, hitForce);
      }

      // Player armor
      if (_isPlayer)
      {
        // Fire player events
        _playerScript.OnDamageTaken();
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
            parts.transform.position = _hip.position;
            parts.Play();
          }
        }
        else if (_health > 0)
          if (damageSourceType == DamageSourceType.FIRE)
          {
            PlaySound("Ragdoll/Combust");
            FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.FIREBALL, _hip.position);

            var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
            parts.transform.position = _hip.position;
            parts.Play();
          }
          else
            PlaySound("Ragdoll/Punch");

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
            if (_enemyScript?._survivalAttributes != null)
              ChangeColor(Color.green, 0.8f);
          }
          else if (_health > 0)
          {
            PlaySound("Enemies/Metal_hit", 0.8f, 1.2f);
            var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
            parts.transform.position = _hip.position;
            parts.Play();
          }
        }
        else
        {
          if (damageSourceType == DamageSourceType.FIRE)
          {
            PlaySound("Ragdoll/Combust", 0.9f, 1.1f);
            FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.FIREBALL, _hip.position);
            FunctionsC.SpawnExplosionScar(_hip.position);
            var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
            parts.transform.position = _hip.position;
            parts.Play();
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
            DamageSource = _hip.position,
            DamageSourceType = DamageSourceType.MELEE,

            SpawnBlood = false,
            SpawnGiblets = false
          });
        _grapplee._hip.AddForce(_controller.forward * (1000f + Random.value * 250f));
        _grapplee._hip.AddTorque(new Vector3(Random.value < 0.5f ? -1f : 1f, 0f, 0f) * 10000000f);

        PlaySound("Ragdoll/Neck_snap", 0.85f, 1.2f);
      }

      // Else, gently let go (?)
      else
      {
        _grapplee._controller.position = _grapplee._hip.position;
        _grapplee._controller.rotation = _controller.rotation;

        var agent = _grapplee._isPlayer ? (_grapplee._playerScript?._agent) : (_grapplee._enemyScript?._agent);
        if (agent != null)
        {
          agent.enabled = true;
        }

        // Check armor perk
        if (_playerScript?.HasPerk(Shop.Perk.PerkType.GRAPPLE_MASTER) ?? false)
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
          var dir = _controller.forward;
          if (i == 2)
            dir += _controller.right * 0.25f;
          else if (i == 1)
            dir += -_controller.right * 0.25f;
          if (Physics.SphereCast(_spine != null ? _spine.transform.position : _hip.transform.position, 0.3f, dir, out hit, 0.75f, EnemyScript._Layermask_Ragdoll))
          {
            var ragdoll = ActiveRagdoll.GetRagdoll(hit.collider.gameObject);
            if (ragdoll == null)
            {
              ToggleRaycasting(true);
              break;
            }

            // Check facing somewhat away dir
            if ((_controller.forward - ragdoll._controller.forward).magnitude > 0.9f || (ragdoll._enemyScript?.IsChaser() ?? false))
            {
              ToggleRaycasting(true);
              break;
            }

            // Grab ragdoll
            Grapple(ragdoll);
            break;
          }

          yield return new WaitForSeconds(0.1f);
          if (_dead) break;
        }
      }
      GameScript._s_Singleton.StartCoroutine(TryGrapple());

      /*/ Kick
      else if (!_kicking && Time.time - _kickTimer_start >= 2f)
      {
        _kickTimer = Time.time - 1f;
        _kickTimer_start = Time.time;
        _kicking = true;
      }*/

      // Clean up
      ToggleRaycasting(true);
    }

  }

  void Grapple(ActiveRagdoll other)
  {
    if (_grappling) return;

    _grapplee = other;
    other._grappled = true;
    other._grappler = this;
    var agent = other._isPlayer ? (other._playerScript?._agent) : (other._enemyScript?._agent);
    if (agent != null)
    {
      agent.enabled = false;
    }

    // Check armor perk
    if (_playerScript?.HasPerk(Shop.Perk.PerkType.GRAPPLE_MASTER) ?? false)
    {
      other.AddArmor();
      other._health = 3;
    }

    // Fire event
    other._enemyScript?.OnGrappled();
  }

  public void Kill(ActiveRagdoll source, DamageSourceType damageSourceType, Vector3 hitForce)
  {
    if (_isPlayer && Settings._PLAYER_INVINCIBLE) return;

    // Disintegrate
    if (damageSourceType == DamageSourceType.FIRE)
    {
      _renderer.enabled = false;
      _itemL?.gameObject.SetActive(false);
      _itemR?.gameObject.SetActive(false);
    }
    // Add to part listener for audio
    else
      AddPartListener(_hip);

    // Die instantly
    if (!_canDie) return;
    Toggle(source, damageSourceType, hitForce);
    _dead = true;
    _time_dead = Time.unscaledTime;

    // Check grappler
    if (_grapplee != null)
      if (!_grapplee._dead)
      {
        Grapple(true);

        _grapplee?._enemyScript?.OnGrapplerRemoved();
      }
  }

  public void SpawnBlood(Vector3 damageSource, bool spawnGiblets)
  {
    IEnumerator BloodFollow(ParticleSystem system)
    {
      var saveParent = system.transform.parent;
      system.transform.parent = system.transform.parent.parent;
      system.transform.position = _hip.position;
      EnemyScript.CheckSound(system.transform.position, EnemyScript.Loudness.SUPERSOFT);
      yield return new WaitForSeconds(0.05f);
      var emission2 = system.emission;
      emission2.enabled = true;
      system.Play();
      var time = system.main.duration;
      while (time > 0f)
      {
        time -= 0.05f;
        yield return new WaitForSeconds(0.05f);
        if (_hip == null) break;
        system.transform.position = _hip.position;
      }
      system.transform.parent = saveParent;
      if (Time.time - GameScript._LevelStartTime < 1f)
      {
        system.Stop();
        system.Clear();
      }
      //if(this != null)
      //  EnemyScript.CheckSound(system.transform.position, EnemyScript.Loudness.SOFT);
    }

    // Check follower
    if (_enemyScript?.IsChaser() ?? false)
    {

      void PlaySpark()
      {
        var particles_sparks = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.SPARKS_ROBOT)[0];
        particles_sparks.transform.position = _hip.position;
        particles_sparks.transform.Rotate(new Vector3(0f, 1f, 0f) * (Random.value * 360f));
        particles_sparks.Play();

        PlaySound("Enemies/Electric_Spark");
      }
      IEnumerator PlaySparks()
      {

        while (_hip != null)
        {
          if (_hip != null)
            PlaySpark();
          yield return new WaitForSeconds(0.13f + Random.value * 0.7f);
        }
      }
      PlaySound("Enemies/Electric_Shock");
      GameScript._s_Singleton.StartCoroutine(PlaySparks());
      return;
    }

    // Check global blood setting
    if (!Settings._Blood) return;

    /// Particles
    // Confetti
    var useConfetti = Settings._Extra_BloodType._value == 1;
    if (useConfetti)
    {
      var particlesConfetti = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.CONFETTI);
      if (particlesConfetti == null || particlesConfetti.Length == 0) { }
      else
      {
        var confetti = particlesConfetti[0];

        var emissionconfetti = confetti.emission;
        emissionconfetti.enabled = false;
        confetti.transform.position = _hip.position;
        confetti.transform.LookAt(damageSource);
        confetti.transform.Rotate(new Vector3(0f, 1f, 0f) * 180f);
        var rotationConfetti = confetti.transform.localRotation;
        rotationConfetti.eulerAngles = new Vector3(0f, rotationConfetti.eulerAngles.y, rotationConfetti.eulerAngles.z);
        confetti.transform.localRotation = rotationConfetti;
        confetti.transform.Rotate(new Vector3(1f, 0f, 0f), UnityEngine.Random.value * -20f);
        rotationConfetti = confetti.transform.localRotation;

        GameScript._s_Singleton.StartCoroutine(BloodFollow(confetti));

        // Audio
        PlaySound("Ragdoll/Confetti", 0.6f, 1.2f);
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
      blood.transform.position = _hip.position;// + _hip.transform.forward * 0.2f;//point;
      blood.transform.LookAt(damageSource);
      blood.transform.Rotate(new Vector3(0f, 1f, 0f) * 180f);
      var rotationBlood = blood.transform.localRotation;
      rotationBlood.eulerAngles = new Vector3(0f, rotationBlood.eulerAngles.y, rotationBlood.eulerAngles.z);
      blood.transform.localRotation = rotationBlood;
      blood.transform.Rotate(new Vector3(1f, 0f, 0f), UnityEngine.Random.value * -20f);
      rotationBlood = blood.transform.localRotation;

      // Giblets
      if (spawnGiblets)
      {
        var bloodIndex = int.Parse(blood.name.Split('_')[1]);
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
        giblets.transform.position = _hip.position;
        giblets.transform.LookAt(damageSource);
        giblets.transform.localRotation = rotationBlood;

        giblets.Play();
      }

      GameScript._s_Singleton.StartCoroutine(BloodFollow(blood));

      // Audio
      PlaySound("Ragdoll/Blood", 1.2f, 1.5f);
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
    if (_dead) return;

    // If about to die, save hip rotation for later
    if (!_ragdolled) _saveRot = _hip.rotation;

    // Check crown
    if (Settings._Extra_CrownMode._value != 0)
      if (_hasCrown)
      {
        GameScript.s_CrownPlayer = GameScript.s_CrownEnemy = -1;
        if (source != null)
        {

          RemoveCrown();
          source.AddCrown();

          if (source._isPlayer)
            GameScript.s_CrownPlayer = source._playerScript._Profile._Id;
          else
            GameScript.s_CrownEnemy = source._enemyScript._Id;
        }
      }

    // Invert values
    _hip.isKinematic = !_hip.isKinematic;
    ToggleRaycasting(false);
    _ragdolled = !_ragdolled;
    if (!_ragdolled)
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
      _hip.AddForce(hitForce);
      _controller.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
      if (changeColor) ChangeColor(_isPlayer ? Color.black * 0.5f : Color.black, 0.8f);
    }

    if (changeColor)
    {
      // Record stats
      if (source?._isPlayer ?? false)
      {
        Stats.RecordKill(source._playerScript._Id);
        if (_isPlayer) Stats.RecordTeamkill(source._playerScript._Id);
      }

      // Fire function per script type
      _enemyScript?.OnToggle(source, damageSourceType);
      _playerScript?.OnToggle(source, damageSourceType);

      // Check extras
      if (Settings._Extras_CanUse)
      {
        var explode_self = false;
        switch (Settings._Extra_BodyExplode._value)
        {

          // All
          case 1:
            explode_self = true;
            break;

          // Enemies
          case 2:
            explode_self = !_isPlayer;
            break;

          // Players
          case 3:
            explode_self = _isPlayer;
            break;
        }
        if (explode_self)
        {
          var explode_script = _hip.gameObject.AddComponent<ExplosiveScript>();
          explode_script._explosionType = ExplosiveScript.ExplosionType.AWAY;
          explode_script._radius = 3 * 0.8f;
          explode_script.Trigger(source, (damageSourceType == DamageSourceType.MELEE ? 1f : 0.1f), false, true);
        }
      }

      // Fire item functions
      _itemL?.OnToggle();
      _itemR?.OnToggle();
    }
  }

  public void RefillAmmo()
  {
    _itemL?.SetClip();
    _itemR?.SetClip();
    _playerScript?.OnRefill();
  }

  // Stun the ragdoll
  bool _stunned { get { return Time.time - _stunTimer < 0f; } }
  float _stunTimer;
  public void Stun(float duration = 1.5f)
  {
    _stunTimer = Time.time + duration;
  }

  System.Tuple<bool, bool>[] _saveRagdollState;
  public void Ragdoll(bool toggle)
  {
    var joints = GetJoints();
    if (toggle)
    {
      _saveRagdollState = new System.Tuple<bool, bool>[joints.Length];
      for (var i = 0; i < joints.Length; i++)
      {
        if (joints[i] == null) continue;
        _saveRagdollState[i] = new System.Tuple<bool, bool>(joints[i].useSpring, joints[i].useLimits);
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

  public void Recoil(Vector3 dir, float force)
  {
    _force += MathC.Get2DVector(dir) * force;
    if (_grappler != null)
    {
      _grappler._force += MathC.Get2DVector(dir) * force;
    }
  }
  public void RecoilSimple(float force)
  {
    Recoil(-_controller.forward, force);
  }

  IEnumerator Rise()
  {
    var startRot = _hip.rotation;

    // Pick ragdoll back up using Lerp with current rotation and saved rotation from before fall
    var iter = 0f;
    while (iter < 1f)
    {
      yield return new WaitForSeconds(0.005f);
      iter += 0.07f;
      _hip.rotation = Quaternion.Lerp(startRot, _saveRot, iter);
    }
    iter = 1f;
    _hip.rotation = Quaternion.Lerp(startRot, _saveRot, iter);

    // Set controller to position
    _controller.position = new Vector3(_hip.position.x, _controller.position.y, _hip.position.z);
    _reviving = false;
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
    parts.transform.position = _hip.position;
    parts.Play();

    var helmet = _head.transform.GetChild(0).gameObject;
    helmet.transform.parent =
      save_til_die ? _controller.parent : GameResources._Container_Objects;

    var rb = helmet.AddComponent<Rigidbody>();
    helmet.GetComponent<BoxCollider>().enabled = true;

    rb.interpolation = RigidbodyInterpolation.Interpolate;
    rb.isKinematic = false;
    rb.AddForce(hitForce * 0.8f);
    rb.AddTorque(new Vector3(1f, 0f, 0f) * 500f);

    _head.transform.localScale = new Vector3(1f, 1f, 1f);

    _hasArmor = false;
  }

  // Add a grappler
  public void AddGrappler(bool playSound = true)
  {

    // Checks
    if (!HasMelee()) return;
    if (_dead) return;
    if (_grappling) return;
    if (GameScript._EditorEnabled) return;

    // Spawn enemy
    var spawn_pos = _hip.position + transform.forward * 0.3f;
    var enemy = EnemyScript.SpawnEnemyAt(
      new EnemyScript.SurvivalAttributes()
      {
        _enemyType = GameScript.SurvivalMode.EnemyType.KNIFE_RUN
      },
      new Vector2(spawn_pos.x, spawn_pos.y)
    );

    // Set grapplee
    Grapple(enemy.GetRagdoll());

    // FX
    if (playSound)
    {
      PlaySound("Ragdoll/Combust");
    }
    FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.SMOKEBALL, spawn_pos);
  }

  public bool HasWeapon()
  {
    return (_itemL != null) || (_itemR != null);
  }
  public bool HasTwohandedWeapon()
  {
    return (_itemL != null && _itemL._twoHanded) || (_itemR != null && _itemR._twoHanded);
  }
  public bool HasGun()
  {
    return (_itemL != null && _itemL.IsGun()) || (_itemR != null && _itemR.IsGun());
  }
  public bool HasAutomatic()
  {
    return (_itemL != null && _itemL._fireMode == ItemScript.FireMode.AUTOMATIC) || (_itemR != null && _itemR._fireMode == ItemScript.FireMode.AUTOMATIC);
  }
  public bool HasSilencedWeapon()
  {
    return (_itemL != null && _itemL._silenced) || (_itemR != null && _itemR._silenced);
  }
  public bool HasMelee()
  {
    return (_itemL?.IsMelee() ?? false) || (_itemR?.IsMelee() ?? false);
  }
  public bool HasEmpty()
  {
    return ((_itemL?.IsEmpty() ?? false) || (_itemR?.IsEmpty() ?? false));
  }
  public bool HasItem(GameScript.ItemManager.Items item)
  {
    return (
      (_itemL?._type ?? GameScript.ItemManager.Items.NONE) == item ||
      (_itemR?._type ?? GameScript.ItemManager.Items.NONE) == item);
  }
  public bool HasBulletDeflector()
  {
    return
      (_itemL?._IsBulletDeflector ?? false) ||
      (_itemR?._IsBulletDeflector ?? false);
  }

  public bool Active()
  {
    return !_ragdolled && !_reviving && !_stunned;
  }

  // Return a true if o is in _parts
  public bool IsSelf(GameObject o)
  {
    foreach (GameObject part in _parts)
      if (GameObject.ReferenceEquals(part, o))
        return true;
    return false;
  }

  public void ToggleRaycasting(bool toggle, bool override_ = false)
  {
    if (_ragdolled && !override_) return;
    foreach (var part in _parts)
    {
      if (part == null) continue;
      part.layer = (toggle ? 10 : 2);
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
    foreach (var r in _Ragdolls)
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
    if (_enemyScript != null && _enemyScript.IsChaser()) return;
    if (_bubbleScript != null && (Time.time - _lastBubbleScriptTime) < (_isPlayer ? 0.5f : 1f))
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
    bubbleScript.Init(text, _head.transform, _color).fontSize = 3.5f * size;
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
    if (_hip == null)
      throw new System.NullReferenceException("Attempting to access a null ActiveRagdoll.");
    // Manually return body parts
    GameObject[] returnList = new GameObject[11];
    // Hip
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
    if (!_canDie || !_dead) return false;

    // Check if already dismembered
    if (joint == null) return false;
    var t = joint.transform;
    joint.gameObject.layer = 2;
    GameObject.Destroy(joint);
    t.parent = _hip.transform.parent;

    // Set to render outside of camera
    _renderer.updateWhenOffscreen = true;

    // Add force to body part
    if (force.magnitude != 0f)
    {
      var rb = t.GetComponent<Rigidbody>();
      AddPartListener(rb);
      rb.AddForce(force * 0.5f);
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
    SfxManager.PlayAudioSourceSimple(_controller.position, soundPath, min, max, audioClass, changePitch);
  }

  public static void Reset()
  {
    _ID = 0;
    _Ragdolls = null;
    BodyPart_Handler.Reset();
    Jobs_Clean();
    SfxManager.Reset();
    UtilityScript.Reset();
  }
  public static void SoftReset()
  {
    if (PlayerScript.s_Players == null || _Ragdolls == null) return;
    // Remove current ragdolls if dead
    for (var i = _Ragdolls.Count - 1; i > 0; i--)
    {
      var r = _Ragdolls[i];
      if (r._dead && r._isPlayer)
      {
        _Ragdolls.Remove(r);
        PlayerScript.s_Players.Remove(r._playerScript);
        if (r._controller == null)
          continue;
        GameObject.Destroy(r._controller.parent.gameObject);
      }
    }
    // Add players' ragdolls
    _Ragdolls.Clear();
    foreach (var p in PlayerScript.s_Players)
    {
      _Ragdolls.Add(p._ragdoll);
    }
  }
}
