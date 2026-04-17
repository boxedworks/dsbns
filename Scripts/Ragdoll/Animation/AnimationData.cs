using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Ragdoll.Animation
{

  class AnimationData
  {
    // Instance data
    public AnimationPoseData _Start, _End;

    // Static data
    public static AnimationData _Sword, _Bat;

    static List<AnimationData> _AnimDatas;
    static public void Init()
    {
      if (_AnimDatas == null)
      {
        _AnimDatas = new List<AnimationData>();

        _Sword = new AnimationData()
        {
          _Start = new AnimationPoseData()
          {
            _arm_lower_l = new Vector3(-50f, -30f, 0f),
            _arm_upper_l = new Vector3(-70f, -10f, 0f),
            _arm_lower_r = new Vector3(-60f, 0f, 0f),
            _arm_upper_r = new Vector3(-30f, -40f, 0f),
            _spine_upper = new Vector3(-5f, 65f, 0f),
            _item_mesh = new Vector3(-50f, 10f, 150f)
          },
          _End = new AnimationPoseData()
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

        _Bat = new AnimationData()
        {
          _Start = new AnimationPoseData()
          {
            _arm_lower_l = new Vector3(-50f, -30f, 0f),
            _arm_upper_l = new Vector3(-70f, -10f, 0f),
            _arm_lower_r = new Vector3(-60f, 0f, 0f),
            _arm_upper_r = new Vector3(-30f, -40f, 0f),
            _spine_upper = new Vector3(-5f, 65f, 0f),
            _item_mesh = new Vector3(-50f, 10f, 150f)
          },
          _End = new AnimationPoseData()
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

}