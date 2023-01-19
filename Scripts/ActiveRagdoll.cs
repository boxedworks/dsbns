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

  // Grabbed rd
  ActiveRagdoll _ragdollGrappled;

  public bool _forceRun;

  public Vector3 _distance;

  // Color of the ragdoll
  public Color _color;

  // If > 0f, provide a speed boost in the forward direction (used for double knives)
  float _forwardDashTimer;

  // What frame the hinge 'animations' are on
  float _movementIter, _movementIter2;
  public float _rotSpeed;

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

  public AudioSource _audioPlayer_steps, _audioPlayer, _audioPlayer_extra;

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

    _audioPlayer_steps = transform.GetComponent<AudioSource>();
    _audioPlayer = transform.GetComponents<AudioSource>()[1];
    _audioPlayer_extra = transform.GetComponents<AudioSource>()[2];

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

  float _invisibility_timer;
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
    if (_ragdollGrappled != null)
    {
      if (_dead || _ragdollGrappled._dead)
      {
        _ragdollGrappled = null;
      }
      else
      {
        // Get melee side
        var left_weapon = _itemL != null && _itemL.IsMelee();

        _ragdollGrappled._hip.position = _hip.position + _controller.forward * 0.4f + _controller.right * 0.18f * (left_weapon ? -1f : 1f);
        _ragdollGrappled._hip.rotation = _hip.rotation;
      }
    }

    // Update arm spread
    if (_spine != null)
    {
      if (_itemL != null && _itemL._twoHanded) { }
      else
      {
        float spread_speed = 20f * dt;
        if (_itemL != null)
          if (_la_limit - _la_targetLimit != 0f)
          {
            // Start z rotation of upper arm
            float startPos = 73f;

            // Lerp
            _la_limit = Mathf.Clamp(_la_limit + ((_la_targetLimit - _la_limit) > 0f ? 1f : -1f) * 0.5f * spread_speed, 0f, 1f);
            Transform arm_upper = _spine.transform.GetChild(0).GetChild(0).GetChild(0);
            Vector3 localrot = arm_upper.localRotation.eulerAngles;
            localrot.z = Mathf.Lerp(startPos, startPos - 90f, _la_limit);
            Quaternion q = arm_upper.localRotation;
            q.eulerAngles = localrot;
            arm_upper.localRotation = q;
          }
        if (_itemR != null)
          if (_ra_limit - _ra_targetLimit != 0f)
          {
            // Start z rotation of upper arm
            float startPos = -73f;

            // Lerp
            _ra_limit = Mathf.Clamp(_ra_limit + ((_ra_targetLimit - _ra_limit) > 0f ? 1f : -1f) * 0.5f * spread_speed, 0f, 1f);
            Transform arm_upper = _spine.transform.GetChild(0).GetChild(1).GetChild(0);
            Vector3 localrot = arm_upper.localRotation.eulerAngles;
            localrot.z = Mathf.Lerp(startPos, startPos + 90f, _ra_limit);
            Quaternion q = arm_upper.localRotation;
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

        UnityEngine.AI.NavMeshAgent agent = (_isPlayer ? _playerScript._agent : _enemyScript._agent);
        agent.Move(force_apply);
        _force -= force_apply;
      }
      if (_forwardForce > 0f)
      {
        var max_velocity = 3.2f;
        var force_normalized = _forwardForce > max_velocity ? max_velocity : _forwardForce;
        var force_apply = force_normalized * MathC.Get2DVector(_hip.transform.forward) * Time.deltaTime * 10f;

        UnityEngine.AI.NavMeshAgent agent = (_isPlayer ? _playerScript._agent : _enemyScript._agent);
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
    /*if (_isPlayer)
    {
      var source = GameObject.Find("test").GetComponent<AudioSource>();
      source.pitch = Mathf.Clamp(source.pitch + ((Mathf.Clamp(_distance.magnitude, 0f, 0.15f) / 0.1f) - source.pitch) * Time.deltaTime * 5f, 0.05f, 2f);
    }*/
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
        if (_isPlayer && _playerScript._canDetect && _distance.magnitude > 0.1f)
          EnemyScript.CheckSound(_controller.position, (_distance.magnitude > 0.2f ? EnemyScript.Loudness.SOFT : (EnemyScript.Loudness.SUPERSOFT)));
        PlaySound(ref _audioPlayer_steps, SceneThemes._footstep, 0.9f, 1.1f);

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
      if (_playerScript != null) dt = Time.unscaledDeltaTime;
      _hip.MoveRotation(Quaternion.RotateTowards(_hip.rotation, rot, dt * 14f * _rotSpeed * Mathf.Abs(Quaternion.Angle(_hip.rotation, rot))));
      // Use iter to move joints
      _movementIter += (_distance.magnitude / 3f) * Time.deltaTime * 65f;
    }
    else
    {
      _controller.position = _hip.position;
      // Use iter to move joints
      _movementIter += (_distance.magnitude / 10f) * Time.deltaTime * 50f;
    }

    // Animate joints; walking
    var joints = new HingeJoint[] { _leg_upper_l, _leg_upper_r, _arm_lower_l, _arm_lower_r };
    bool opposite = false;
    foreach (HingeJoint joint in joints)
    {
      if (joint == null) continue;
      JointSpring j = joint.spring;
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

    // Play noise
    FunctionsC.PlaySound(ref _audioPlayer_steps, "Ragdoll/Combust", 0.9f, 1.1f);
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
        _playerScript._profile._equipment._item_left0 = itemL_type;
        _playerScript._profile._equipment._item_right0 = itemR_type;
      }
      else
      {
        _playerScript._profile._equipment._item_left1 = itemL_type;
        _playerScript._profile._equipment._item_right1 = itemR_type;
      }
    }
    FunctionsC.PlaySound(ref _audioPlayer_steps, "Ragdoll/Weapon_Switch");
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
      if (_isPlayer && !_playerScript._profile._reloadSidesSameTime) return;
      else
      {
        IEnumerator delayedReload()
        {
          yield return new WaitForSeconds(0.1f);
          if (_itemR != null && _itemR.CanReload())
            _itemR.Reload();
        }
        GameScript._Singleton.StartCoroutine(delayedReload());
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
      FunctionsC.PlaySound(ref s._audio, "Ragdoll/Tick", 0.95f, 1.1f);
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
          FunctionsC.PlaySound(ref rag._audioPlayer_extra, $"Ragdoll/{soundName}", 0.7f, 1.25f);
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
      GameScript._Singleton.StopCoroutine(_color_Coroutine);
    _color_Coroutine = GameScript._Singleton.StartCoroutine(LerpColor(mesh, c, lerpAmount));
  }
  void SetLerpAmount(ref SkinnedMeshRenderer mesh, Color c, float normalized, Color sc0, Color sc1)
  {
    Color c0 = Color.Lerp(sc1, c / 2f, normalized);
    c = Color.Lerp(sc0, c + Color.white / 2f, normalized);
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

  public bool TakeDamage(ActiveRagdoll source, DamageSourceType damageSourceType, Vector3 damageSource, int damage = 1, bool spawnBlood = true)
  {
    return TakeDamage(source, damageSourceType, Vector3.zero, damageSource, damage, spawnBlood);
  }
  float _lastMetalHit;
  public bool TakeDamage(ActiveRagdoll source, DamageSourceType damageSourceType, Vector3 hitForce, Vector3 damageSource, int damage = 1, bool spawnBlood = true)
  {
    // If is robot, do not damage from bullet and play 'dink'
    if (_enemyScript != null && _enemyScript._enemyType == EnemyScript.EnemyType.ROBOT && damage <= 10f)
    {
      if (Time.time - _lastMetalHit > 0.3f && damageSourceType == DamageSourceType.BULLET)
      {
        _lastMetalHit = Time.time;
        FunctionsC.PlaySound(ref _audioPlayer, "Enemies/Metal_hit", 0.8f, 1.2f);
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
        FunctionsC.PlaySound(ref _audioPlayer, "Enemies/Ugh0", 0.9f, 1.1f);

        if (_hasArmor)
        {
          spawnBlood = false;
          var health_threshhold = GameScript.IsSurvival() ? 3 : 1;
          if (save_health > health_threshhold && _health <= health_threshhold)
            RemoveArmor(hitForce, false);
          else
            FunctionsC.PlaySound(ref _audioPlayer, "Enemies/Metal_hit", 0.8f, 1.2f);
        }
        else if (_health > 0)
          if (damageSourceType == DamageSourceType.FIRE)
          {
            FunctionsC.PlaySound(ref _audioPlayer_steps, "Ragdoll/Combust", 0.9f, 1.1f);
            FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.FIREBALL, _hip.position);
          }
          else
            FunctionsC.PlaySound(ref _audioPlayer_steps, "Ragdoll/Punch", 0.9f, 1.1f);
      }
      // Enemy armor
      else
      {
        if (_hasArmor)
        {
          spawnBlood = false;
          if (save_health > 1 && damage > 0)
          {
            RemoveArmor(hitForce, false);
            // Change color to normal
            ChangeColor(Color.green, 0.8f);
          }
          else if (_health > 0)
            FunctionsC.PlaySound(ref _audioPlayer, "Enemies/Metal_hit", 0.8f, 1.2f);
        }
        else
        {
          if (damageSourceType == DamageSourceType.FIRE)
          {
            FunctionsC.PlaySound(ref _audioPlayer, "Ragdoll/Combust", 0.9f, 1.1f);
            FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.FIREBALL, _hip.position);
            FunctionsC.SpawnExplosionScar(_hip.position);
          }
          else
            FunctionsC.PlaySound(ref _audioPlayer_steps, "Ragdoll/Punch", 0.9f, 1.1f);
        }
      }

      if (spawnBlood) SpawnBlood(damageSource);
    }
    return true;
  }

  // Grab a ragdoll in front of
  public bool _grappling { get { return _ragdollGrappled != null; } }
  public bool _grappled;
  public ActiveRagdoll _grappler;
  public void Grapple()
  {

    // Throw grappled ragdoll
    if (_ragdollGrappled != null)
    {

      if (!_ragdollGrappled._isPlayer)
      {
        _ragdollGrappled.TakeDamage(this, DamageSourceType.MELEE, _hip.position, 100, false);
        _ragdollGrappled._hip.AddForce(_controller.forward * (4000f + Random.value * 1500f));
        _ragdollGrappled._hip.AddTorque(new Vector3(-1f + Random.value * 2f, -1f + Random.value * 2f, -1f + Random.value * 2f) * 10000f);
      }
      else
      {
        _ragdollGrappled._controller.position = _ragdollGrappled._hip.position;
        _ragdollGrappled._controller.rotation = _controller.rotation;
      }
      _ragdollGrappled._grappled = false;
      _ragdollGrappled._grappler = null;
      _ragdollGrappled = null;

    }

    // Grapple a ragdoll
    else
    {

      // Raycast for ragdoll
      RaycastHit hit;
      var layermask = 1 << LayerMask.NameToLayer("Ragdoll");
      ToggleRaycasting(false);
      if (Physics.SphereCast(_controller.position, 0.25f, _controller.forward, out hit, 0.5f, layermask))
      {
        var ragdoll = ActiveRagdoll.GetRagdoll(hit.collider.gameObject);
        if (ragdoll == null)
        {
          ToggleRaycasting(true);
          return;
        }

        // Check facing somewhat away dir
        if ((_controller.forward - ragdoll._controller.forward).magnitude > 0.9f)
        {
          ToggleRaycasting(true);
          return;
        }

        // Grab ragdoll
        _ragdollGrappled = ragdoll;
        _ragdollGrappled._grappled = true;
        _ragdollGrappled._grappler = this;
      }
      ToggleRaycasting(true);

    }

  }

  public void Kill(ActiveRagdoll source, DamageSourceType damageSourceType, Vector3 hitForce)
  {
    if (_isPlayer && GameScript.Settings._PLAYER_INVINCIBLE) return;

    /*/ Have a delayed death; bleed out
    int r = Mathf.RoundToInt(Random.value * 2f);
    if (_coroutine_dying == null)
    {
      if (r < 1 && hitForce.magnitude == 0f)
      {
        _coroutine_dying = GameScript._Instance.StartCoroutine(DelayedKill(0.5f + Random.value * 0.5f));
        return;
      }
    }
    // Kill if hit again when bleeding
    else
    {
      GameScript._Instance.StopCoroutine(_coroutine_dying);
      _coroutine_dying = null;
      FunctionsC.PlaySound(ref _audioPlayer_steps, "Enemies/Ugh0", 0.9f, 1.1f);
    }*/

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
  }

  public void SpawnBlood(Vector3 damageSource)
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
      float time = system.main.duration;
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
    // Check global blood setting
    if (!GameScript.Settings._Blood) return;
    // Particles
    var particles = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BLOOD);
    if (particles == null || particles.Length == 0) return;
    var blood = particles[0];
    var emission = blood.emission;
    emission.enabled = false;
    blood.transform.position = _hip.position;// + _hip.transform.forward * 0.2f;//point;
    blood.transform.LookAt(damageSource);
    blood.transform.Rotate(new Vector3(0f, 1f, 0f) * 180f);
    var q = blood.transform.localRotation;
    q.eulerAngles = new Vector3(0f, q.eulerAngles.y, q.eulerAngles.z);
    blood.transform.localRotation = q;
    blood.transform.Rotate(new Vector3(1f, 0f, 0f), UnityEngine.Random.value * -20f);
    // Audio
    var aSource = blood.GetComponent<AudioSource>();
    FunctionsC.PlaySound(ref aSource, "Ragdoll/Blood", 1.2f, 1.5f);
    GameScript._Singleton.StartCoroutine(BloodFollow(blood));
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

    // Invert values
    _hip.isKinematic = !_hip.isKinematic;
    ToggleRaycasting(false);
    _ragdolled = !_ragdolled;
    if (!_ragdolled)
    {
      Ragdoll(false);
      _controller.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;

      // If not dead anymore, rise
      _reviving = true;
      GameScript._Singleton.StartCoroutine(Rise());
      if (changeColor) ChangeColor(_color, 2f);
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
      if (source._isPlayer)
      {
        Stats.RecordKill(source._playerScript._id);
        if (_isPlayer) Stats.RecordTeamkill(source._playerScript._id);
      }
      // Fire function per script type
      _enemyScript?.OnToggle(source, damageSourceType);
      _playerScript?.OnToggle(source, damageSourceType);
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

  bool _hasArmor;
  public void GiveArmor()
  {
    if (_hasArmor) return;

    var armor = GameObject.Instantiate(GameScript.GameResources._Armor);
    var helmet = armor.transform.GetChild(0);

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

    FunctionsC.PlaySound(ref _audioPlayer_extra, "Enemies/Armor_break", 0.9f, 1.1f);

    var helmet = _head.transform.GetChild(0).gameObject;
    helmet.transform.parent =
      save_til_die ? _controller.parent : GameScript.GameResources._Container_Objects;

    var rb = helmet.AddComponent<Rigidbody>();
    helmet.GetComponent<BoxCollider>().enabled = true;

    rb.interpolation = RigidbodyInterpolation.Interpolate;
    rb.isKinematic = false;
    rb.AddForce(hitForce * 0.8f);
    rb.AddTorque(new Vector3(1f, 0f, 0f) * 500f);

    _head.transform.localScale = new Vector3(1f, 1f, 1f);

    _hasArmor = false;
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

  public bool Active()
  {
    return !_ragdolled && !_reviving;
  }

  // Return a true if o is in _parts
  public bool IsSelf(GameObject o)
  {
    foreach (GameObject part in _parts)
      if (GameObject.ReferenceEquals(part, o))
        return true;
    return false;
  }

  public void ToggleRaycasting(bool toggle)
  {
    if (_ragdolled) return;
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
    foreach (ActiveRagdoll r in _Ragdolls)
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
    g.transform.parent = GameScript.GameResources._Container_Objects;
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

  public void PlaySound(string soundPath, float min = 1f, float max = 1f)
  {
    FunctionsC.PlaySound(ref _audioPlayer, soundPath, min, max);
  }
  public void PlaySound(ref AudioSource audioPlayer, AudioSource sfx_source, float min = 1f, float max = 1f)
  {
    FunctionsC.PlaySound(ref audioPlayer, sfx_source, min, max);
  }
  public void PlaySound(ref AudioSource audioPlayer, string soundPath, float min = 1f, float max = 1f)
  {
    FunctionsC.PlaySound(ref audioPlayer, soundPath, min, max);
  }

  public void StopSound()
  {
    _audioPlayer.Stop();
  }

  public static void Reset()
  {
    _ID = 0;
    _Ragdolls = null;
    BodyPart_Handler.Reset();
    Jobs_Clean();
    FunctionsC.Reset();
  }
  public static void SoftReset()
  {
    if (PlayerScript._Players == null || _Ragdolls == null) return;
    // Remove current ragdolls if dead
    for (var i = _Ragdolls.Count - 1; i > 0; i--)
    {
      var r = _Ragdolls[i];
      if (r._dead && r._isPlayer)
      {
        _Ragdolls.Remove(r);
        PlayerScript._Players.Remove(r._playerScript);
        if (r._controller == null)
          continue;
        GameObject.Destroy(r._controller.parent.gameObject);
      }
    }
    // Add players' ragdolls
    _Ragdolls.Clear();
    foreach (var p in PlayerScript._Players)
    {
      _Ragdolls.Add(p._ragdoll);
    }
  }
}
