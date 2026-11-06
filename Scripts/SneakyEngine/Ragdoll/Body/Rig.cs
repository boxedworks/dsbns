
using System.Collections.Generic;
using UnityEngine;

namespace SneakyEngine.Ragdoll.Body
{
  public class Rig
  {

    public GameObject[] BodyParts;

    public Transform
      Hip,
      Spine,

      ArmUpperL,
      ArmUpperR,
      ArmLowerL,
      ArmLowerR,

      Head,

      LegUpperL,
      LegUpperR,
      LegLowerL,
      LegLowerR;

    public Rigidbody HipRb;

    public HingeJoint
      SpineJoint,
      ArmUpperLJoint,
      ArmUpperRJoint,
      ArmLowerLJoint,
      ArmLowerRJoint,
      LegUpperLJoint,
      LegUpperRJoint,
      LegLowerLJoint,
      LegLowerRJoint;

    Dictionary<EntityId, TransformState> _ragdollStateInfo;

    public Rig(Transform hip)
    {

      BodyParts = GetParts(hip);

      //
      Hip = BodyParts[0].transform;
      LegUpperL = BodyParts[1].transform;
      LegUpperR = BodyParts[2].transform;
      LegLowerL = BodyParts[3].transform;
      LegLowerR = BodyParts[4].transform;
      ArmUpperL = BodyParts[6].transform;
      ArmUpperR = BodyParts[7].transform;
      ArmLowerL = BodyParts[8].transform;
      ArmLowerR = BodyParts[9].transform;
      Spine = BodyParts[5].transform.GetChild(0);
      Head = BodyParts[10].transform;

      HipRb = Hip.GetComponent<Rigidbody>();

      LegUpperLJoint = LegUpperL.GetComponent<HingeJoint>();
      LegUpperRJoint = LegUpperR.GetComponent<HingeJoint>();
      LegLowerLJoint = LegLowerL.GetComponent<HingeJoint>();
      LegLowerRJoint = LegLowerR.GetComponent<HingeJoint>();
      SpineJoint = Spine.GetComponent<HingeJoint>();

      // Take snapshot of initial ragdoll transform states
      _ragdollStateInfo = new();
      void AddSaveInfo(Transform part)
      {
        _ragdollStateInfo.Add(part.GetEntityId(), new TransformState(part));

      }
      AddSaveInfo(Spine);
      AddSaveInfo(ArmUpperL);
      AddSaveInfo(ArmUpperR);
      AddSaveInfo(ArmLowerL);
      AddSaveInfo(ArmLowerR);
    }

    public void SetDefault(Transform t)
    {
      var info = _ragdollStateInfo[t.GetEntityId()];
      t.localPosition = info._pos;
      t.localEulerAngles = info._rot;
    }

    GameObject[] GetParts(Transform hip)
    {

      // Manually return body parts
      GameObject[] returnList = new GameObject[11];

      // Hip
      returnList[0] = hip.gameObject;

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

    public HingeJoint GetRandomJoint()
    {
      return Random.Range(0, 5) switch
      {
        0 => SpineJoint,
        1 => ArmUpperLJoint,
        2 => ArmUpperRJoint,
        3 => LegUpperLJoint,
        4 => LegUpperRJoint,

        _ => null,
      };
    }

    static public void SetJointLimits(HingeJoint joint, float min = 0f, float max = 0f)
    {
      var limits = joint.limits;

      limits.min = min;
      limits.max = max;

      joint.limits = limits;
    }

    public void AddArmJoints(Side side)
    {
      var upper = (side == Side.LEFT ? ArmUpperL : ArmUpperR).gameObject;
      var upperJoint = side == Side.LEFT ? ArmUpperLJoint : ArmUpperRJoint;
      if (upperJoint != null) return;

      var lower = (side == Side.LEFT ? ArmLowerL : ArmLowerR).gameObject;

      var upper_rb = upper.AddComponent<Rigidbody>();
      var lower_rb = lower.AddComponent<Rigidbody>();

      upper_rb.interpolation = lower_rb.interpolation = RigidbodyInterpolation.Interpolate;
      upper_rb.mass = lower_rb.mass = 0.2f;

      var side_mod = side == Side.LEFT ? -1f : 1f;

      var upper_joint = upper.AddComponent<HingeJoint>();
      upper_joint.axis = new Vector3(0f, 0f, 1f);
      upper_joint.useLimits = true;
      SetJointLimits(upper_joint, -70f * side_mod, 15f * side_mod);

      var lower_joint = lower.AddComponent<HingeJoint>();
      lower_joint.axis = new Vector3(0f, 0f, 1f);
      lower_joint.useLimits = true;
      SetJointLimits(lower_joint, -150f * side_mod, 0f);

      upper_joint.connectedBody = Spine.GetComponent<Rigidbody>();
      lower_joint.connectedBody = upper_rb;

      if (side == Side.LEFT)
      {
        ArmUpperLJoint = upper_joint;
        ArmLowerLJoint = lower_joint;
      }
      else
      {
        ArmUpperRJoint = upper_joint;
        ArmLowerRJoint = lower_joint;
      }
    }

    public void RemoveArmJoints(Side side)
    {
      var joints = new HingeJoint[] {
        side == Side.LEFT ? ArmUpperLJoint : ArmUpperRJoint,
        side == Side.LEFT ? ArmLowerLJoint : ArmLowerRJoint
      };
      foreach (var joint in joints)
      {
        if (joint == null) continue;

        var rb = joint.gameObject.GetComponent<Rigidbody>();
        Object.DestroyImmediate(joint);
        Object.DestroyImmediate(rb);
      }

      // Set arms to default
      SetDefault(Spine);
      if (side == Side.LEFT)
      {
        SetDefault(ArmUpperL);
        SetDefault(ArmLowerL);
      }
      else
      {
        SetDefault(ArmUpperR);
        SetDefault(ArmLowerR);
      }
    }
  }
}