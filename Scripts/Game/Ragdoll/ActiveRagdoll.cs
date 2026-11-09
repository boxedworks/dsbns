using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Random = UnityEngine.Random;

using SneakyGame.Settings;
using SneakyGame.Objects.CustomEntities;
using SneakyGame.Game.Items;
using SneakyGame.Ragdoll.Equippables;
using SneakyGame.Game.Player;
using SneakyGame.Objects;
using SneakyGame.Enemy;

using SneakyEngine.Effects;
using SneakyEngine.Equippable.Item;
using SneakyEngine.Utility;
using SneakyEngine.Ragdoll;
using SneakyEngine.Engine;
using SneakyGame.Ragdoll.Actions;
using SneakyEngine.Ragdoll.Actions;

namespace SneakyGame.Ragdoll
{

  public class ActiveRagdoll : RagdollController
  {

    static RagdollSystem RagdollSystem => RagdollSystem.Singleton;
    static VfxSystem VfxSystem => VfxSystem.Singleton;

    //
    public static List<ActiveRagdoll> s_Ragdolls;

    // Item info
    public ItemScript _ItemL, _ItemR;

    // Script references
    public EnemyScript _EnemyScript;
    public PlayerScript _PlayerScript;

    public Vector3 _Distance;

    // What frame the hinge 'animations' are on
    float _movementIter, _movementIter2;
    public float _rotSpeed, _time_died;

    bool _footprintSwitch;
    float _bloodFootprintTimer;

    public bool _IsRagdolled,
      _IsPlayer,
      _IsDead,
      _IsDisabled,
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
      get { return (_ItemL?._IsReloading ?? false) || (_ItemR?._IsReloading ?? false); }
    }
    public bool _IsEnemy { get { return !_IsPlayer; } }

    Vector3 _forceGlobal;

    static readonly Vector3
      _GROUNDHEIGHT = new(0f, 0.73f, 0f),

      collider_upperLegPosition = new(0f, 0.71f, 0f),
      collider_upperLegScale = new(0.78f, 1.5f, 0.6f),

      collider_lowerLegPosition = new(0f, 0.96f, 0f),
      collider_lowerLegScale = new(0.5f, 2.24f, 0.36f);

    public float _totalDistanceMoved;

    public ActiveRagdoll(GameObject ragdoll, Transform controller, PlayerScript playerScript, EnemyScript enemyScript) : base(ragdoll, controller)
    {

      // Add to static _Ragdolls list
      s_Ragdolls ??= new();
      s_Ragdolls.Add(this);

      Grapple = new();

      // Defaults
#if UNITY_EDITOR
      ragdoll.name = "Ragdoll";
#endif
      _time_died = -1f;

      // Register enemy / player script
      _EnemyScript = enemyScript;
      _PlayerScript = playerScript;

      color = Color.blue * 1.3f;

      _rotSpeed = 1f;
      _CanDie = true;

      SetCollisionMode(CollisionDetectionMode.Continuous);
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

      var dt = _IsPlayer ? Time.unscaledDeltaTime : Time.deltaTime;

      // Update arm spread
      if (Rig.SpineJoint != null)
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
              var arm_upper = Rig.SpineJoint.transform.GetChild(0).GetChild(0).GetChild(0);
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
              var arm_upper = Rig.SpineJoint.transform.GetChild(0).GetChild(1).GetChild(0);
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
        if (_forceGlobal.magnitude > 0f)
        {
          var force_normalize = _forceGlobal.magnitude < 1f ? _forceGlobal : _forceGlobal.normalized;
          var force_apply = 10f * Time.deltaTime * _forceGlobal;

          var agent = _IsPlayer ? _PlayerScript._agent : _EnemyScript._agent;
          if (!Grapple.IsGrappled)
            agent.Move(force_apply);
          _forceGlobal -= force_apply;
        }
      }

      // Move / rotate based on a rigidbody
      var movePos = Controller.position + _GROUNDHEIGHT;
      _Distance = (movePos - Hip.position) * (Time.timeScale < 1f ? 0.3f : 1f);

      // Moving ragdoll too fast will break joints, clamp to .35f magnitude
      if (_Distance.magnitude > 0.35f)
      {
        movePos = Hip.position + _Distance.normalized * 0.35f;
        _Distance = (movePos - Hip.position) * (Time.timeScale < 1f ? 0.3f : 1f);
      }

      // Grapple logic
      if (Grapple.IsGrappling)
      {

        // End grappled
        if (Grapple.Grapplee._IsDead)
        {
          Grapple.Grapplee = null;
        }

        // Continue grapple
        else
        {
          // Get melee side
          var sideMod = _GrappleSide == Side.LEFT ? -1f : 1f;
          Grapple.Grapplee.Hip.position = Hip.position + Hip.transform.forward * 0.45f + sideMod * 0.09f * Hip.transform.right;
          Grapple.Grapplee.Hip.rotation = Hip.rotation;
        }
      }

      // If grappled, set movepos and distance to origin
      if (Grapple.IsGrappled)
      {
        movePos = Hip.position;
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
        if (!_IsRagdolled && !Grapple.IsGrappled && ((save < 0.5f && _movementIter2 >= 0.5f) || (save > 0.5f && _movementIter2 <= 0.5f)))
        {
          // If player, send footstep sound to enemies to check for detection
          if (_IsPlayer && _PlayerScript._CanDetect && _Distance.magnitude > 0.1f)
            EnemyScript.CheckSound(new CheckSoundData()
            {
              NoisePosition = Controller.position,
              LoudnessLevel = _Distance.magnitude > 0.2f ? Loudness.SOFT : Loudness.SUPERSOFT,
              RagdollSource = this
            });

          // Sfx
          var footstepAudioSource = false && _bloodFootprintTimer > 0f ? RagdollSystem.SfxFootstepBloody : RagdollSystem.SfxFootstep;
          SfxManager.PlayAudioSourceSimple(Controller.position, footstepAudioSource.clip, footstepAudioSource.volume, 0.89f, 1.11f, SfxManager.AudioClass.FOOTSTEP);

          // Footprint
          var footprint_pos = _footprintSwitch ? Rig.LegLowerL.transform.position : Rig.LegLowerR.transform.position;
          footprint_pos.y -= 0.4f;
          _footprintSwitch = !_footprintSwitch;
          FunctionsC.PlayComplexParticleSystemAt(_bloodFootprintTimer > 0f ? FunctionsC.ParticleSystemType.FOOTPRINT_BLOOD : FunctionsC.ParticleSystemType.FOOTPRINT, footprint_pos);
        }
      }
      if (_bloodFootprintTimer > 0f)
        _bloodFootprintTimer -= Time.deltaTime;

      // Set rotation
      var posSave = Hip.position;
      var rot = Hip.rotation;
      var f = Controller.rotation.eulerAngles.y;
      if (f > 180.0f)
        f -= 360.0f;
      rot.eulerAngles = new Vector3(rot.eulerAngles.x, f, rot.eulerAngles.z);

      // Don't move to ragdoll if is not active
      if (Active())
      {

        // Move / rotate hip (base)
        if (!Grapple.IsGrappled)
        {
          Hip.MovePosition(movePos);
          if (_IsPlayer)
            dt = Time.unscaledDeltaTime;

          Hip.MoveRotation(Quaternion.RotateTowards(Hip.rotation, rot, dt * (Time.timeScale < 0.75f && _IsPlayer ? 4f : 14f) * _rotSpeed * Mathf.Abs(Quaternion.Angle(Hip.rotation, rot))));
        }

        // Use iter to move joints
        _movementIter += _Distance.magnitude / 3f * Time.deltaTime * 65f;

        // Check melee quickstep
        var moveDir = (movePos - posSave).normalized;
        var martialArts = false;//HasTwohandedWeapon() || (_ItemL != null && _ItemR != null && _ItemL._type == _ItemR._type);
        if (
          _IsPlayer &&
          Time.time - _meleeStartTime < 0.25f &&
          _PlayerScript.GetInput().magnitude > 0.7f &&
          (martialArts || (moveDir - Controller.forward).magnitude > 1.4f))
        {
          _meleeStartTime = -1f;

          _forceGlobal += MathC.Get2DVector(moveDir * 0.5f);

          // FX
          SfxManager.PlayAudioSourceSimple(Hip.position, "Ragdoll/Quickstep", 0.85f, 1.15f, SfxManager.AudioClass.NONE, true);
          var particles = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.CLOUD_RING)[0];
          particles.transform.position = Hip.position + new Vector3(0f, -0.5f, 0f);
          particles.Play();
        }

        // Check stun FX
        if (!_IsDead && _IsStunned)
        {
          if (Time.time - _confusedTimer > 0f)
          {
            _confusedTimer = Time.time + Random.Range(0.2f, 0.5f);
            var p = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.CONFUSED)[0];
            p.transform.position = Rig.Head != null ? Rig.Head.position : Controller.position;
            p.Play();
          }
        }
      }
      else
      {
        Controller.position = Hip.position;

        // Use iter to move joints
        _movementIter += _Distance.magnitude / 10f * Time.deltaTime * 50f;
      }

      // Kick
      if (_kicking)
      {
        if (Time.time - _kickTimer_start >= 1f)
        {

          if (Time.time - _kickTimer_start >= 1.2f)
          {
            _kicking = false;
            _kickTimer_start = -1f;
          }

          else if (Time.time - _kickTimer >= 0.05f)
          {

            _kickTimer = Time.time;

            RaycastHit hit;
            ToggleRaycasting(false, true);
            if (Physics.SphereCast(Controller.position, 0.25f, Controller.forward, out hit, 0.5f, GameResources._Layermask_Ragdoll))
            {
              var ragdoll = GetRagdoll(hit.collider.gameObject);
              if (ragdoll != null)
              {
                var hitForce = 2f;
                if (ItemScript.MeleeDamageOther(ItemType.FIST, this, ragdoll, hitForce, false, 0f))
                {
                  _kicking = false;
                }
              }
            }
            ToggleRaycasting(true, true);

          }
        }
      }

      // Animate joints; walking
      var joints = new HingeJoint[] { Rig.LegUpperLJoint, Rig.LegUpperRJoint, Rig.ArmLowerLJoint, Rig.ArmLowerRJoint };
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

          j.targetPosition = opposite ?
            joint.limits.min + (joint.limits.max - joint.limits.min) * movementIter :
            joint.limits.max - (joint.limits.max - joint.limits.min) * movementIter;
        }

        // Normal walking
        else
          j.targetPosition = opposite ?
            joint.limits.min + (joint.limits.max - joint.limits.min) * _movementIter2 :
            joint.limits.max - (joint.limits.max - joint.limits.min) * _movementIter2;
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

    //
    public bool IsGrappleRelated(ActiveRagdoll other)
    {
      if (IsGrappledBy(other) || IsGrappling(other))
        return true;
      return false;
    }
    public bool IsGrappledBy(ActiveRagdoll other)
    {
      return Grapple.IsGrappled && Grapple.Grappler.Id == other.Id;
    }
    public bool IsGrappling(ActiveRagdoll other)
    {
      return Grapple.IsGrappling && Grapple.Grapplee.Id == other.Id;
    }

    public void OnGrapplee(Action<ActiveRagdoll> onGrapplee)
    {
      if (Grapple.IsGrappling)
        onGrapplee?.Invoke(Grapple.Grapplee);
    }
    public void OnGrappler(Action<ActiveRagdoll> onGrappler)
    {
      if (Grapple.IsGrappled)
        onGrappler?.Invoke(Grapple.Grappler);
    }

    public bool _IsGrapplerPlayer { get { return Grapple.IsGrappled && Grapple.Grappler._IsPlayer; } }

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

    // Remove all HingeJoints, Rigidbodies, and Colliders
    public void Disable()
    {
      if (_IsDisabled) return;
      _IsDisabled = true;

      IEnumerator delayDisable()
      {
        yield return new WaitForSecondsRealtime(5f);
        var waitForSeconds = new WaitForSecondsRealtime(0.05f);

        foreach (var part in Rig.BodyParts)
        {
          if (part == null) continue;

          // Remove HingeJoint
          if (part.TryGetComponent<HingeJoint>(out var j))
            UnityEngine.Object.Destroy(j);

          // Remove Rigidbody
          if (part.TryGetComponent<Rigidbody>(out var r))
            UnityEngine.Object.Destroy(r);

          // Remove Collider
          if (part.TryGetComponent<Collider>(out var c))
            UnityEngine.Object.Destroy(c);

          yield return waitForSeconds;
        }
      }
      CoroutineSystem.InitiateCoroutine(delayDisable());
    }

    // Invisibility
    public bool _invisible;
    public void SetInvisible(bool toggle, float duration = 1f)
    {
      // Set toggle
      _invisible = toggle;

      // Set renderer / layer
      meshRenderer.enabled = !toggle;
      //ToggleRaycasting(!toggle);

      // Play FX
      PlaySound("Ragdoll/Combust", 0.9f, 1.1f);
      FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.SMOKEBALL, Hip.position);

      // Set timer
      if (toggle)
        _invisibility_timer += duration;
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

    void AddLegCollider(Side side)
    {
      var upperLeg = side == Side.LEFT ? Rig.LegUpperL : Rig.LegUpperR;
      var lowerLeg = side == Side.LEFT ? Rig.LegLowerL : Rig.LegLowerR;

      if (upperLeg != null)
      {
        var upperLegCollider = upperLeg.gameObject.AddComponent<BoxCollider>();
        upperLegCollider.center = collider_upperLegPosition;
        upperLegCollider.size = collider_upperLegScale;
      }

      if (lowerLeg != null)
      {
        var lowerLegCollider = lowerLeg.gameObject.AddComponent<BoxCollider>();
        lowerLegCollider.center = collider_lowerLegPosition;
        lowerLegCollider.size = collider_lowerLegScale;
      }
    }

    public void EquipItem(ItemType itemType, Side side, int clipSize = -1, float useTime = -1f, int itemId = -1)
    {
      if (itemType == ItemType.NONE)
      {
        Rig.AddArmJoints(side);
        return;
      }

      // Spawn item
      var itemScript = EquippableManager.SpawnItem(itemType);

      bool two_hands = itemScript._twoHanded;
      var side_other = side == Side.LEFT ? Side.RIGHT : Side.LEFT;

      // Unequip if equipped already
      UnequipItem(side);
      if (two_hands)
        UnequipItem(side_other);

      // Rotate / positon per specific item
      var rb = itemScript.GetComponent<Rigidbody>();
      if (rb != null) UnityEngine.Object.Destroy(rb);

      // Remove joints for better aiming
      Rig.RemoveArmJoints(side);
      if (two_hands) Rig.RemoveArmJoints(side_other);

      // Decide which hand to place in
      Transform arm;
      if (side == Side.LEFT || two_hands)
      {
        arm = Rig.ArmLowerL;
        _ItemL = itemScript;
      }
      else
      {
        arm = Rig.ArmLowerR;
        _ItemR = itemScript;
      }
      itemScript.transform.parent = arm.GetChild(0);
      itemScript.OnEquip(this, side, clipSize, useTime, itemId);
    }

    public void UnequipItem(Side side)
    {
      var item = side == Side.LEFT ? _ItemL : _ItemR;
      if (side == Side.LEFT)
      {
        Rig.SetDefault(Rig.ArmUpperL);
        Rig.SetDefault(Rig.ArmLowerL);
      }
      else
      {
        Rig.SetDefault(Rig.ArmUpperR);
        Rig.SetDefault(Rig.ArmLowerR);
      }
      if (item == null) return;
      UnityEngine.Object.DestroyImmediate(item.gameObject);
      if (side == Side.LEFT) _ItemL = null;
      else _ItemR = null;
    }

    // Switch item hands
    public void SwapItemHands(int index)
    {

      SwapItems(
        new WeaponSwapData()
        {
          ItemType = _ItemR?._ItemType ?? ItemType.NONE,
          ItemId = _ItemR?._ItemId ?? -1,
          ItemClip = _ItemR?.GetClip() ?? -1,
          ItemUseTime = _ItemR?._UseTime ?? -1f
        },
        new WeaponSwapData()
        {
          ItemType = _ItemL?._ItemType ?? ItemType.NONE,
          ItemId = _ItemL?._ItemId ?? -1,
          ItemClip = _ItemL?.GetClip() ?? -1,
          ItemUseTime = _ItemL?._UseTime ?? -1f
        },
        index
      );
    }

    public bool CanSwapWeapons()
    {
      return !_IsDead && !_IsSwinging && !_IsReloading && !_HasItemsInUse;
    }

    //
    public ItemScript TryDeflectMelee(ActiveRagdoll ragdollOther)
    {

      // Check facing each other
      var forwardSelf = Hip.transform.forward;
      var forwardOther = ragdollOther.Hip.transform.forward;
      var forwardMagnitude = (forwardOther - forwardSelf).magnitude;
      if (forwardMagnitude < 1.7f)
      {
        return null;
      }

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
    public void SwapItems(WeaponSwapData item_left, WeaponSwapData item_right, int index)
    {
      if (!CanSwapWeapons()) return;

      var itemL_type = item_left.ItemType;
      var itemL_id = item_left.ItemId;
      var itemL_clip = item_left.ItemClip;
      var itemL_useTime = item_left.ItemUseTime;

      var itemR_type = item_right.ItemType;
      var itemR_id = item_right.ItemId;
      var itemR_clip = item_right.ItemClip;
      var itemR_useTime = item_right.ItemUseTime;

      // Equip items
      if (_IsPlayer && _PlayerScript._HasTwin && !_PlayerScript._IsOriginalTwin)
      {
        UnequipItem(Side.LEFT);
        Rig.AddArmJoints(Side.LEFT);
      }
      else if (itemL_type != ItemType.NONE)
        EquipItem(itemL_type, Side.LEFT, itemL_clip, itemL_useTime, itemL_id);
      else
      {
        UnequipItem(Side.LEFT);
        Rig.AddArmJoints(Side.LEFT);
      }

      if (_IsPlayer && _PlayerScript._IsOriginalTwin)
      {
        if (_ItemL == null || !_ItemL._twoHanded)
        {
          UnequipItem(Side.RIGHT);
          Rig.AddArmJoints(Side.RIGHT);
        }
      }
      else if (itemR_type != ItemType.NONE)
        EquipItem(itemR_type, Side.RIGHT, itemR_clip, itemR_useTime, itemR_id);
      else
      {
        if (_ItemL == null || !_ItemL._twoHanded)
        {
          UnequipItem(Side.RIGHT);
          Rig.AddArmJoints(Side.RIGHT);
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
      PlaySound("Ragdoll/Weapon_Switch", 0.9f, 1.1f);
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
          CoroutineSystem.InitiateCoroutine(delayedReload());
        }
      }
      // Right item
      if (_ItemR != null && _ItemR.CanReload() && !_ItemR.IsChargeWeapon())
        _ItemR.Reload();
    }

    public void IgnoreCollision(Collider c, bool ignore = true)
    {
      foreach (var part in Rig.BodyParts)
      {
        if (part.TryGetComponent<Collider>(out var c_))
          Physics.IgnoreCollision(c, c_, ignore);
      }
    }
    public void IgnoreGrappleCollision(Collider c, bool ignore = true)
    {
      Grapple.Grapplee?.IgnoreCollision(c, ignore);
      Grapple.Grappler?.IgnoreCollision(c, ignore);
    }

    //
    float _meleeStartTime;
    public void OnMeleeStart()
    {
      _meleeStartTime = Time.time;
    }

    //
    public void BounceFromPosition(Vector3 position, float force, bool overwright)
    {
      var bounceForce = (MathC.Get2DVector(Hip.position) - MathC.Get2DVector(position)).normalized * force;
      Recoil(bounceForce, 1f, overwright);
    }

    //
    public void OnTriggerEnter(Collider other)
    {
      // Check player / enemy specific triggers
      if (_IsPlayer)
        _PlayerScript.OnTriggerEnter(other);

      // Check general triggers
      if (other.TryGetComponent<CustomEntityUI>(out var u))
      {
        u.Activate();
      }
    }
    public void OnTriggerExit(Collider other)
    {
      if (_IsPlayer) _PlayerScript.OnTriggerExit(other);
    }
    public void OnTriggerStay(Collider other)
    {
      if (_IsPlayer) _PlayerScript.OnTriggerStay(other);
    }

    public void SetColors(Color skinColor, Color clothesColor)
    {
      color = skinColor;
      meshRenderer.sharedMaterials[1].color = skinColor;
      meshRenderer.sharedMaterials[0].color = clothesColor;
    }

    Coroutine _color_Coroutine;
    public void ChangeColor(Color c, float lerpAmount = 0f)
    {
      // Color override
      var colorOverride = RagdollSystem.ColorOverride;
      if (colorOverride != default)
      {
        SetColors(colorOverride, colorOverride);
        return;
      }

      //
      if (Equals(color, Color.blue * 1.3f))
        color = c;

      // Gather mesh renderers by amount of materials and final color
      if (lerpAmount == 0f)
      {
        Color startColor0 = meshRenderer.sharedMaterials[0].color,
         startColor1 = meshRenderer.sharedMaterials[1].color;

        SetLerpAmount(ref meshRenderer, c, 1f, startColor0, startColor1);
        _PlayerScript?.ChangeRingColor(GameScript.s_IsPartyGameMode && !VersusMode.s_Settings._FreeForAll ? VersusMode.GetTeamColorFromPlayerId(_PlayerScript._Id) : c);
        return;
      }
      if (_color_Coroutine != null)
        CoroutineSystem.TerminateCoroutine(_color_Coroutine);
      _color_Coroutine = CoroutineSystem.InitiateCoroutine(LerpColor(meshRenderer, c, lerpAmount));
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

      var levelId = TileManager._s_MapIndex;

      // Skin
      Color startColor0 = mesh.sharedMaterials[1].color,
        // Clothes
        startColor1 = mesh.sharedMaterials[0].color;

      var waitForSeconds = new WaitForSecondsRealtime(0.02f);
      while (true)
      {
        yield return waitForSeconds;
        if (Hip == null || levelId != TileManager._s_MapIndex) break;

        timer = Mathf.Clamp(timer + (Time.time - timeLast) * 1.25f, 0f, lerpAmount);
        timeLast = Time.time;
        SetLerpAmount(ref mesh, c, timer / lerpAmount, startColor0, startColor1);

        if (timer == lerpAmount) break;
      }
      _color_Coroutine = null;
    }

    public class RagdollDamageSource
    {
      public ActiveRagdoll Source;
      public DamageSourceType DamageSourceType;
      public Vector3 HitForce, DamageSource;
      public int Damage;
      public bool SpawnBlood, SpawnGiblets, MuteAudio;
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
      var muteAudio = ragdollDamageSource.MuteAudio;

      void PlaySoundLocal(String soundPath, float pitchMin, float pitchMax)
      {
        if (!muteAudio)
          PlaySound(soundPath, pitchMin, pitchMax);
      }

      // If is robot, do not damage from bullet and play 'dink'
      if (_EnemyScript != null && _EnemyScript._enemyType == EnemyScript.EnemyType.ROBOT && damage <= 10f)
      {
        if (Time.time - _lastMetalHit > 0.05f && damageSourceType == DamageSourceType.BULLET)
        {
          _lastMetalHit = Time.time;
          PlaySoundLocal("Enemies/Metal_hit", 0.8f, 1.2f);

          var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
          parts.transform.position = Hip.position;
          parts.Play();
        }
        return false;
      }

      var save_health = Health;
      if (!_IsDead)
      {
        Health -= damage;

        if (Health <= 0f)
        {
          Kill(source, damageSourceType, hitForce);
        }

        if (_IsPlayer)
        {
          // Fire player events
          _PlayerScript.OnDamageTaken(ragdollDamageSource);
          PlaySoundLocal("Enemies/Ugh0", 0.9f, 1.1f);

          // Player armor
          if (_hasArmor)
          {
            spawnBlood = false;
            var health_threshhold = GameScript.s_IsZombieGameMode ? 3 : 1;
            if (save_health > health_threshhold && Health <= health_threshhold)
              RemoveArmor(hitForce, false);
            else
            {
              PlaySoundLocal("Enemies/Metal_hit", 0.8f, 1.2f);
              var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
              parts.transform.position = Hip.position;
              parts.Play();
            }
          }
          else
          {
            if (damageSourceType == DamageSourceType.FIRE)
            {
              PlaySoundLocal("Ragdoll/Combust", 0.9f, 1.1f);
              FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.FIREBALL, Hip.position);
            }
            else
              PlaySoundLocal("Ragdoll/Punch", 0.9f, 1.1f);
          }
        }
        // Enemy armor
        else
        {
          if (_hasArmor)
          {
            spawnBlood = false;
            if (Health <= 1 && save_health > 1)
            {
              RemoveArmor(hitForce, false);

              if (Health < 1)
              {
                spawnBlood = true;
              }
            }
            else if (Health > 0)
            {
              PlaySoundLocal("Enemies/Metal_hit", 0.8f, 1.2f);
              var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
              parts.transform.position = Hip.position;
              parts.Play();
            }
          }
          else
          {
            if (damageSourceType == DamageSourceType.FIRE)
            {
              PlaySoundLocal("Ragdoll/Combust", 0.9f, 1.1f);
              FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.FIREBALL, Hip.position);
              FunctionsC.SpawnExplosionScar(Hip.position);

              var smokeParts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.EXPLOSION_SMOKE)[0];
              var spawnPos = Hip.position;
              spawnPos.y = 1f;
              smokeParts.transform.position = spawnPos;
              smokeParts.Emit(3);
            }
            else
              PlaySoundLocal("Ragdoll/Punch", 0.9f, 1.1f);
          }
        }

        if (spawnBlood) SpawnBlood(damageSource, spawnGiblets);
      }
      return true;
    }

    // Grab a ragdoll in front of
    public Side _GrappleSide
    {
      get
      {
        return _ItemL == null || (_ItemL != null && _ItemL.IsMelee()) ? Side.LEFT : Side.RIGHT;
      }
    }
    public Grapple Grapple;
    float _kickTimer, _kickTimer_start;
    bool _kicking;
    public void ToggleGrapple(bool gentle)
    {

      // Throw grappled ragdoll
      if (Grapple.IsGrappling)
      {

        // If not player, throw
        if (!gentle)
        {
          Grapple.Grapplee.TakeDamage(
            new RagdollDamageSource()
            {
              Source = this,

              HitForce = Vector3.zero,

              Damage = 100,
              DamageSource = Hip.position,
              DamageSourceType = DamageSourceType.MELEE,

              SpawnBlood = false,
              SpawnGiblets = false
            });
          Grapple.Grapplee.Hip.AddForce(Controller.forward * (20f + Random.value * 5f), ForceMode.Impulse);
          Grapple.Grapplee.Hip.AddTorque(new Vector3(0f, Random.value < 0.5f ? -1f : 1f, 0f) * 100f, ForceMode.Impulse);

          PlaySound("Ragdoll/Neck_snap", 0.85f, 1.2f);
        }

        // Else, gently let go (?)
        else
        {
          Grapple.Grapplee.Controller.position = Grapple.Grapplee.Hip.position;
          Grapple.Grapplee.Controller.rotation = Controller.rotation;

          var agent = Grapple.Grapplee._IsPlayer ? (Grapple.Grapplee._PlayerScript?._agent) : (Grapple.Grapplee._EnemyScript?._agent);
          if (agent != null)
          {
            agent.enabled = true;
          }

          // Check armor perk
          if (_PlayerScript?.HasPerk(Perk.PerkType.GRAPPLE_MASTER) ?? false)
          {
            if (Grapple.Grapplee._hasArmor)
            {
              Grapple.Grapplee.RemoveArmor(Vector3.zero);
              Grapple.Grapplee.Health = 1;
            }
          }
        }
        Grapple.Grapplee.Grapple.Grappler = null;
        Grapple.Grapplee = null;

        PlaySound("Enemies/Grapple", 0.65f, 0.8f);
      }

      // Grapple a ragdoll
      else if (Time.time - Grapple._LastGrappleTime > 0.5f)
      {
        Grapple._LastGrappleTime = Time.time;

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
            ToggleRaycasting(false, true);
            var dir = Controller.forward;
            if (i == 2)
              dir += Controller.right * 0.25f;
            else if (i == 1)
              dir += -Controller.right * 0.25f;
            if (Physics.SphereCast(Rig.SpineJoint != null ? Rig.SpineJoint.transform.position : Hip.transform.position, 0.3f, dir, out hit, 0.75f, GameResources._Layermask_Ragdoll))
            {
              var ragdoll = GetRagdoll(hit.collider.gameObject);
              if (ragdoll == null)
              {
                continue;
              }

              // Check facing somewhat away dir
              if ((Controller.forward - ragdoll.Controller.forward).magnitude > 1.1f || (ragdoll._EnemyScript?.IsChaser() ?? false))
              {
                continue;
              }

              // Grab ragdoll
              ToggleGrapple(ragdoll);
              break;
            }

            yield return new WaitForSeconds(0.1f);
            if (_IsDead) break;
          }

          // Clean up
          ToggleRaycasting(true, true);
        }
        CoroutineSystem.InitiateCoroutine(TryGrapple());
      }

    }

    void ToggleGrapple(ActiveRagdoll other)
    {
      if (Grapple.IsGrappling) return;

      Grapple.Grapplee = other;
      other.Grapple.Grappler = this;

      var agent = other._IsPlayer ? (other._PlayerScript?._agent) : (other._EnemyScript?._agent);
      if (agent != null)
      {
        agent.enabled = false;
      }

      // Check armor perk
      if (_PlayerScript?.HasPerk(Perk.PerkType.GRAPPLE_MASTER) ?? false)
      {
        other.AddArmor();
        other.Health = 3;
      }

      // Fire events
      other._EnemyScript?.OnGrappled();
      if (_ItemL != null)
        _ItemL.OnGrappled();
      if (_ItemR != null)
        _ItemR.OnGrappled();
    }

    public void HideCompletely()
    {
      meshRenderer.enabled = false;
      if (_ItemL != null)
        _ItemL.gameObject.SetActive(false);
      if (_ItemR != null)
        _ItemR.gameObject.SetActive(false);
    }

    public void Kill(ActiveRagdoll source, DamageSourceType damageSourceType, Vector3 hitForce)
    {
      // Disintegrate
      if (damageSourceType == DamageSourceType.FIRE)
      {
        HideCompletely();
      }

      // Add to part listener for audio
      else
        Rig.AddPartListener(Hip);

      // Die instantly
      if (!_CanDie) return;
      Toggle(source, damageSourceType, hitForce);
      _IsDead = true;
      _time_died = Time.unscaledTime;

      // Check grapplee
      if (Grapple.IsGrappling)
        if (!Grapple.Grapplee._IsDead)
        {
          var graplee = Grapple.Grapplee;
          ToggleGrapple(true);
          graplee?._EnemyScript?.OnGrapplerRemoved(this);
        }
    }

    public void SpawnBlood(Vector3 damageSource, bool spawnGiblets)
    {
      IEnumerator BloodFollow(ParticleSystem system)
      {
        var saveParent = system.transform.parent;
        system.transform.parent = GameScript.s_Singleton.transform;

        system.transform.position = Hip.position;

        EnemyScript.CheckSound(new CheckSoundData()
        {
          NoisePosition = system.transform.position,
          LoudnessLevel = Loudness.SUPERSOFT,
          RagdollSource = this
        });
        yield return new WaitForSeconds(0.05f);

        var emission = system.emission;
        emission.enabled = true;
        system.Play();
        var time = system.main.duration;
        var timeLast = Time.time;
        var waitForSeconds2 = new WaitForSecondsRealtime(0.02f);
        while (time > 0f)
        {
          time -= (Time.time - timeLast) * 1f;
          timeLast = Time.time;

          yield return waitForSeconds2;
          if (Hip == null) break;

          system.transform.position = Hip.position;
        }

        if (Time.time - GameScript.s_LevelStartTime < 1f)
        {
          system.Stop();
          system.Clear();
        }
        system.transform.parent = saveParent;
      }

      // Check follower
      if (_EnemyScript?.IsChaser() ?? false)
      {

        void PlaySpark()
        {
          var particles_sparks = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.SPARKS_ROBOT)[0];
          particles_sparks.transform.position = Hip.position;
          particles_sparks.transform.Rotate(new Vector3(0f, 1f, 0f) * (Random.value * 360f));
          particles_sparks.Play();

          PlaySound("Enemies/Electric_Spark", 0.9f, 1.1f);
        }
        IEnumerator PlaySparks()
        {

          while (Hip != null)
          {
            if (Hip != null)
              PlaySpark();
            yield return new WaitForSeconds(0.13f + Random.value * 0.7f);
          }
        }
        PlaySound("Enemies/Electric_Shock", 0.9f, 1.1f);
        CoroutineSystem.InitiateCoroutine(PlaySparks());
        return;
      }

      // Check global blood setting
      if (!VfxSystem.UseBlood) return;

      /// Particles
      // Confetti
      var useConfetti = VfxSystem.BloodType == VfxSystem.BloodParticleType.CONFETTI;
      if (useConfetti)
      {
        var particlesConfetti = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.CONFETTI);
        if (particlesConfetti == null || particlesConfetti.Length == 0) return;

        var confetti = particlesConfetti[0];
        if (confetti != null)
        {
          var emissionconfetti = confetti.emission;
          emissionconfetti.enabled = false;
          confetti.transform.position = Hip.position;
          confetti.transform.LookAt(damageSource);
          confetti.transform.Rotate(new Vector3(0f, 1f, 0f) * 180f);
          var rotationConfetti = confetti.transform.localRotation;
          rotationConfetti.eulerAngles = new Vector3(0f, rotationConfetti.eulerAngles.y, rotationConfetti.eulerAngles.z);
          confetti.transform.localRotation = rotationConfetti;
          confetti.transform.Rotate(new Vector3(1f, 0f, 0f), Random.value * -20f);
          rotationConfetti = confetti.transform.localRotation;

          CoroutineSystem.InitiateCoroutine(BloodFollow(confetti));

          // Audio
          PlaySound("Ragdoll/Confetti", 0.6f, 1.2f);

          // Achievement
#if UNITY_STANDALONE
          Achievements.UnlockAchievement(Achievements.Achievement.EXTRA_USE_CONFETTI);
#endif
        }

      }

      // Blood
      else
      {

        var particlesBlood = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BLOOD);
        if (particlesBlood == null || particlesBlood.Length == 0) return;

        var blood = particlesBlood[0];
        if (blood != null)
        {
          var emissionBlood = blood.emission;
          emissionBlood.enabled = false;
          blood.transform.position = Hip.position;
          blood.transform.LookAt(damageSource);
          blood.transform.Rotate(new Vector3(0f, 1f, 0f) * 180f);
          var rotationBlood = blood.transform.localRotation;
          rotationBlood.eulerAngles = new Vector3(0f, rotationBlood.eulerAngles.y, rotationBlood.eulerAngles.z);
          blood.transform.localRotation = rotationBlood;
          blood.transform.Rotate(new Vector3(1f, 0f, 0f), Random.value * -20f);
          rotationBlood = blood.transform.localRotation;

          CoroutineSystem.InitiateCoroutine(BloodFollow(blood));

          // Bloody footprint
          FunctionsC.AoeHandler.RegisterAoeEffect(this, FunctionsC.AoeHandler.AoeType.BLOOD, Hip.position, 1f, 20f);

          // Audio
          PlaySound($"Ragdoll/Blood{/*Random.Range(0, 10) < 3 ? 1 : */0}", 1.1f, 1.5f);

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
            giblets.transform.position = Hip.position;
            giblets.transform.LookAt(damageSource);
            giblets.transform.localRotation = rotationBlood;

            giblets.Play();
          }
        }

        // Blood smoke
        if (VfxSystem.UseBloodSmoke)
        {
          var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BLOOD_SMOKE)[0];
          parts.transform.position = Hip.position + new Vector3(0f, 0.5f, 0f);
          parts.Emit(2);
        }
      }
    }

    public void Toggle(ActiveRagdoll source, DamageSourceType damageSourceType, Vector3 hitForce, bool changeColor = true)
    {
      if (_IsDead) return;

      // Check crown
      if (GameScript.s_IsCrownModeEnabled)
        if (source != null && !source._IsDead)
          if (_hasCrown)
          {
            GameScript.s_CrownPlayer = GameScript.s_CrownEnemy = -1;

            RemoveCrown();
            source.AddCrown(true);

            if (source._IsPlayer)
              GameScript.s_CrownPlayer = source._PlayerScript._Profile._Id;
            else if (!source._EnemyScript.IsChaser())
              GameScript.s_CrownEnemy = source._EnemyScript._Id;

          }
          else if (GameScript.s_CrownPlayer == -1 && GameScript.s_CrownEnemy == -1)
          {
            if (_IsPlayer && source._IsEnemy)
            {
              if (!source._EnemyScript.IsChaser())
                GameScript.s_CrownEnemy = source._EnemyScript._Id;
              source.AddCrown(true);
            }
          }

      // Invert values
      Hip.isKinematic = !Hip.isKinematic;
      ToggleRaycasting(false, false);
      _IsRagdolled = !_IsRagdolled;
      if (!_IsRagdolled)
      {
      }

      else
      {
        Ragdoll(true);

        if (changeColor)
          ChangeColor(_IsPlayer ? Color.black * 0.5f : Color.black, 0.8f);

        //
        Controller.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;

        Hip.AddForce(hitForce, ForceMode.Impulse);
        AddLegCollider(Side.LEFT);
        AddLegCollider(Side.RIGHT);
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
        if (SettingsHelper._Extras_CanUse)
        {
          var shouldExplode = false;
          switch (RagdollSystem.ExplodeOnDeath)
          {

            // All
            case RagdollSystem.TargetAlignmentType.ALL:
              shouldExplode = true;
              break;

            // Enemies
            case RagdollSystem.TargetAlignmentType.ENEMIES:
              shouldExplode = !_IsPlayer;
              break;

            // Players
            case RagdollSystem.TargetAlignmentType.PLAYERS:
              shouldExplode = _IsPlayer;
              break;
          }
          if (shouldExplode)
          {
            var explosion = Hip.gameObject.AddComponent<ExplosiveScript>();
            explosion._explosionType = ExplosiveScript.ExplosionType.AWAY;
            explosion._radius = 3 * 0.8f;
            explosion.Trigger(source, damageSourceType == DamageSourceType.MELEE ? 1f : 0.1f, false, true);
          }
        }

        // Fire item functions
        if (_ItemL != null)
          _ItemL.OnToggle();
        if (_ItemR != null)
          _ItemR.OnToggle();
      }
    }

    public void RefillAmmo()
    {
      if (_ItemL != null)
        _ItemL.SetClip();
      if (_ItemR != null)
        _ItemR.SetClip();
      _PlayerScript?.OnRefill();
    }

    // Stun the ragdoll
    public bool _IsStunned { get { return Time.time - _stunTimer < 0f; } }
    [NonSerialized]
    public bool _HasBeenStunned;
    float _stunTimer;
    public void Stun(float duration = 1.5f)
    {
      if (_EnemyScript?.IsChaser() ?? false) { return; }

      _HasBeenStunned = true;
      _stunTimer = Time.time + duration;

      // Release grapple
      if (Grapple.IsGrappling)
        ToggleGrapple(true);
    }

    Tuple<bool, bool>[] _savedJointStates;
    public void Ragdoll(bool shouldDisable)
    {
      var joints = GetJoints();

      // Ragdoll
      if (shouldDisable)
      {
        _savedJointStates = new Tuple<bool, bool>[joints.Length];
        for (var i = 0; i < joints.Length; i++)
        {
          if (joints[i] == null) continue;
          _savedJointStates[i] = new Tuple<bool, bool>(joints[i].useSpring, joints[i].useLimits);
          joints[i].useSpring = false;
          joints[i].useLimits = true;
        }
        if (Rig.ArmUpperLJoint != null) Rig.ArmUpperLJoint.useLimits = false;
        if (Rig.ArmUpperRJoint != null) Rig.ArmUpperRJoint.useLimits = false;
      }

      // Get up; Reset ragdoll joints to saved state
      else
      {
        for (var i = 0; i < joints.Length; i++)
        {
          if (joints[i] == null) continue;
          joints[i].useSpring = _savedJointStates[i].Item1;
          joints[i].useLimits = _savedJointStates[i].Item2;
        }
      }
    }

    public void Recoil(Vector3 dir, float force, bool overwrite)
    {
      Grapple.Grappler?.Recoil(dir, force * 0.75f, overwrite);

      if (overwrite)
      {
        _forceGlobal = MathC.Get2DVector(dir) * force;
        return;
      }
      _forceGlobal += MathC.Get2DVector(dir) * force;
    }
    public void RecoilSimple(float force, bool overwrite)
    {
      // Mod
      if (force < 0f)
        if (_IsPlayer && _PlayerScript.HasPerk(Perk.PerkType.THRUST))
          force *= 2f;

      //
      Recoil(Grapple.IsGrappled ? -Hip.transform.forward : -Controller.forward, force, overwrite);
    }

    bool _hasCrown;
    public void AddCrown(bool playSfx)
    {
      if (_hasCrown) return;

      var crown = UnityEngine.Object.Instantiate(GameResources._Crown).transform;

      crown.transform.parent = Rig.Head;
      crown.localScale = Vector3.one * 2f;
      crown.localEulerAngles = new Vector3(-60f, 90f, 0f);
      crown.localPosition = new Vector3(0.067f, 1f, 0.095f);

      _hasCrown = true;

      if (playSfx)
        PlaySound("Etc/Crown_collect", 0.9f, 1.1f);
    }
    public void RemoveCrown()
    {
      if (!_hasCrown) return;

      UnityEngine.Object.Destroy(Rig.Head.GetChild(0).gameObject);

      _hasCrown = false;
    }

    bool _hasArmor;
    public void AddArmor()
    {
      if (_hasArmor) return;

      var armor = UnityEngine.Object.Instantiate(GameResources._Armor);
      var helmet = armor.transform.GetChild(0);

      PlaySound("Enemies/Armor_give", 0.9f, 1.1f);

      // Equip helmet
      Rig.Head.localScale = new Vector3(0.3f, 0.3f, 0.3f);

      helmet.transform.parent = Rig.Head;
      helmet.localScale = new Vector3(5f, 5f, 5f);
      helmet.localEulerAngles = new Vector3(-90f, 180f, 0f);
      helmet.localPosition = new Vector3(0f, 2.1f, 0.9f);

      UnityEngine.Object.Destroy(armor);

      _hasArmor = true;
    }
    public void RemoveArmor(Vector3 hitForce, bool save_til_die = true)
    {
      if (!_hasArmor) return;

      PlaySound("Enemies/Armor_break", 0.9f, 1.1f);

      var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.BULLET_COLLIDE)[0];
      parts.transform.position = Hip.position;
      parts.Play();

      var helmet = Rig.Head.GetChild(0).gameObject;
      helmet.transform.parent =
        save_til_die ? Controller.parent : GameResources._Container_Objects;

      var rb = helmet.AddComponent<Rigidbody>();
      helmet.GetComponent<BoxCollider>().enabled = true;

      rb.interpolation = RigidbodyInterpolation.Interpolate;
      rb.isKinematic = false;
      rb.AddForce(hitForce.normalized * 4f, ForceMode.Impulse);
      rb.AddTorque(new Vector3(1f, 0f, 0f) * 50f, ForceMode.Impulse);

      Rig.Head.localScale = new Vector3(1f, 1f, 1f);

      _hasArmor = false;
    }

    //
    public bool _CanGrapple
    {
      get
      {
        return !_IsDead && (
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
      if (Grapple.IsGrappling) return;
      if (GameScript.s_EditorEnabled) return;
      if (!_CanGrapple) return;

      // Spawn enemy
      var spawn_pos = Hip.position + Transform.forward * 0.3f;
      var enemy = EnemyScript.SpawnEnemyAt(
        new Vector2(spawn_pos.x, spawn_pos.z),
        true
      );

      // Set grapplee
      ToggleGrapple(enemy._Ragdoll);

      // FX
      if (playSound)
      {
        PlaySound("Ragdoll/Combust", 0.9f, 1.1f);
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
      return (_ItemL != null && _ItemL._FireMode == ItemScript.FireMode.AUTOMATIC) || (_ItemR != null && _ItemR._FireMode == ItemScript.FireMode.AUTOMATIC);
    }
    public bool HasUseOnReleaseWeapon()
    {
      return (_ItemL != null && _ItemL._useOnRelease) || (_ItemR != null && _ItemR._useOnRelease);
    }
    public bool HasSemiAutomatic()
    {
      return (_ItemL != null && _ItemL._FireMode == ItemScript.FireMode.SEMI) || (_ItemR != null && _ItemR._FireMode == ItemScript.FireMode.SEMI);
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
    public bool HasItem(ItemType item)
    {
      return
        (_ItemL?._ItemType ?? ItemType.NONE) == item ||
        (_ItemR?._ItemType ?? ItemType.NONE) == item;
    }
    public bool HasActiveBulletDeflector()
    {
      return
        ((_ItemL?._IsSwinging ?? false) && _ItemL._IsBulletDeflector) ||
        ((_ItemR?._IsSwinging ?? false) && _ItemR._IsBulletDeflector);
    }

    public bool Active()
    {
      return !_IsRagdolled;
    }

    // Return a true if o is in _parts
    public bool IsSelf(GameObject o)
    {
      foreach (var part in Rig.BodyParts)
        if (ReferenceEquals(part, o))
          return true;
      return false;
    }

    public void ToggleRaycasting(bool enable, bool andGrappleRelated, bool override_ = false)
    {
      if (_IsRagdolled && !override_) return;
      foreach (var part in Rig.BodyParts)
      {
        if (part == null) continue;
        part.layer = enable ? 10 : 2;
      }

      if (andGrappleRelated)
      {
        Grapple.Grapplee?.ToggleRaycasting(enable, false, override_);
        Grapple.Grappler?.ToggleRaycasting(enable, false, override_);
      }
    }

    public static ActiveRagdoll GetRagdoll(GameObject o)
    {
      foreach (var r in s_Ragdolls)
        if (r.IsSelf(o)) return r;
      return null;
    }

    TextBubbleScript _bubbleScript;
    public float _lastBubbleScriptTime;
    public void DisplayText(string text, float size = 0.75f)
    {
      if (_EnemyScript != null && _EnemyScript.IsChaser()) return;
      if (_bubbleScript != null && (Time.time - _lastBubbleScriptTime) < (_IsPlayer ? 0.5f : 1f))
      {
        _bubbleScript._textMesh.text = text;
        return;
      }

      // Save for later
      _lastBubbleScriptTime = Time.time;
      _bubbleScript = TextBubbleScript.SpawnBubbleWith(Rig.Head, text, color, size);
    }

    void SetCollisionMode(CollisionDetectionMode mode)
    {
      foreach (var part in Rig.BodyParts)
      {
        if (part.name.Equals("Hip") && mode == CollisionDetectionMode.Continuous) mode = CollisionDetectionMode.ContinuousSpeculative;
        var rb = part.GetComponent<Rigidbody>();
        if (rb == null) continue;
        rb.collisionDetectionMode = mode;
      }
    }

    HingeJoint[] GetJoints()
    {
      return new HingeJoint[]
      {
        Rig.SpineJoint,
        Rig.ArmUpperLJoint,
        Rig.ArmUpperRJoint,
        Rig.ArmLowerLJoint,
        Rig.ArmLowerRJoint,
        Rig.LegUpperLJoint,
        Rig.LegUpperRJoint,
        Rig.LegLowerLJoint,
        Rig.LegLowerRJoint
      };
    }

    public bool Dismember(HingeJoint joint, Vector3 force = default)
    {
      if (!_CanDie || !_IsDead) return false;

      // Check if already dismembered
      if (joint == null) return false;

      var t = joint.transform;
      if (t.parent == Hip.transform.parent) return false;

      // Dismember
      joint.gameObject.layer = 2;
      UnityEngine.Object.Destroy(joint);
      t.parent = Hip.transform.parent;

      // Make sure has collider (arm joint)
      if (t.GetComponent<Collider>() == null)
      {
        t.gameObject.AddComponent<BoxCollider>().center = new Vector3(0f, 1f, 0f);
      }

      // Set to render outside of camera
      meshRenderer.updateWhenOffscreen = true;

      // Add force to body part
      if (force.magnitude != 0f)
      {
        var rb = t.GetComponent<Rigidbody>();
        Rig.AddPartListener(rb);
        rb.AddForce(force * (t.name == "Head" ? 0.1f : 0.2f), ForceMode.Impulse);
      }
      return true;
    }
    public bool DismemberRandom(Vector3 force)
    {
      return Dismember(Rig.GetRandomJoint(), force);
    }
    public void DismemberRandomTimes(Vector3 force, int n)
    {
      for (var i = 0; i < n; i++)
      {
        Dismember(Rig.GetRandomJoint(), force * (0.7f + Random.value * 0.4f));
      }
    }

    public void PlaySound(string soundPath, float min = 1f, float max = 1f, SfxManager.AudioClass audioClass = SfxManager.AudioClass.NONE, bool changePitch = true)
    {
      SfxManager.PlayAudioSourceSimple(Controller.position, soundPath, min, max, audioClass, changePitch);
    }

    public void Destroy()
    {
      _PlayerScript = null;
      _EnemyScript = null;
      if (Controller != null)
        UnityEngine.Object.Destroy(Controller.parent.gameObject);
    }

    public static void Reset()
    {
      if (s_Ragdolls != null)
      {
        for (var i = s_Ragdolls.Count - 1; i > -1; i--)
        {
          var ragdoll = s_Ragdolls[i];
          ragdoll.Destroy();
        }
        s_Ragdolls = null;
      }
      s_ID = 0;

      RagdollController.Reset();
      SfxManager.Reset();
      UtilityScript.Reset();
    }
    public static void SoftReset()
    {
      if (PlayerScript.s_Players == null || s_Ragdolls == null) return;

      // Remove current ragdolls if dead
      for (var i = s_Ragdolls.Count - 1; i > 0; i--)
      {
        var ragdoll = s_Ragdolls[i];
        if (ragdoll._IsDead && ragdoll._IsPlayer)
        {
          s_Ragdolls.Remove(ragdoll);
          PlayerScript.s_Players.Remove(ragdoll._PlayerScript);
          ragdoll.Destroy();
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
}