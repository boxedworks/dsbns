using Assets.Scripts.Ragdoll.Equippables;
using UnityEngine;

namespace Assets.Scripts.Ragdoll.Animation
{

  public class AnimationPoseData
  {
    public Vector3 _arm_lower_l, _arm_upper_l,
      _arm_lower_r, _arm_upper_r,
      _spine_upper,
      _item_mesh;

    public void Set(ActiveRagdoll ragdoll, Transform item_mesh)
    {
      ItemScript.SetRotationLocal(ragdoll._transform_parts._arm_lower_l, _arm_lower_l);
      ItemScript.SetRotationLocal(ragdoll._transform_parts._arm_upper_l, _arm_upper_l);
      ItemScript.SetRotationLocal(ragdoll._transform_parts._arm_lower_r, _arm_lower_r);
      ItemScript.SetRotationLocal(ragdoll._transform_parts._arm_upper_r, _arm_upper_r);
      ItemScript.SetRotationLocal(ragdoll._transform_parts._spine, _spine_upper);
      ItemScript.SetRotationLocal(item_mesh, _item_mesh);
    }

    public static void Animate(ItemScript item, AnimationPoseData start, AnimationPoseData end, Transform item_mesh, float totalTime, float halfTime)
    {
      if (item._Anims[0] != null) item.StopCoroutine(item._Anims[0]);
      item._Anims[0] = item.AnimateTransformSimple(item._ragdoll._transform_parts._arm_lower_l, start._arm_lower_l, end._arm_lower_l, totalTime, halfTime, () => { item._Anims[0] = null; });
      if (item._Anims[1] != null) item.StopCoroutine(item._Anims[1]);
      item._Anims[1] = item.AnimateTransformSimple(item._ragdoll._transform_parts._arm_upper_l, start._arm_upper_l, end._arm_upper_l, totalTime, halfTime, () => { item._Anims[1] = null; });
      if (item._Anims[2] != null) item.StopCoroutine(item._Anims[2]);
      item._Anims[2] = item.AnimateTransformSimple(item._ragdoll._transform_parts._arm_lower_r, start._arm_lower_r, end._arm_lower_r, totalTime, halfTime, () => { item._Anims[2] = null; });
      if (item._Anims[3] != null) item.StopCoroutine(item._Anims[3]);
      item._Anims[3] = item.AnimateTransformSimple(item._ragdoll._transform_parts._arm_upper_r, start._arm_upper_r, end._arm_upper_r, totalTime, halfTime, () => { item._Anims[3] = null; });
      if (item._Anims[4] != null) item.StopCoroutine(item._Anims[4]);
      item._Anims[4] = item.AnimateTransformSimple(item._ragdoll._transform_parts._spine, start._spine_upper, end._spine_upper, totalTime, halfTime, () => { item._Anims[4] = null; });
      if (item._Anims[5] != null) item.StopCoroutine(item._Anims[5]);
      item._Anims[5] = item.AnimateTransformSimple(item_mesh, start._item_mesh, end._item_mesh, totalTime, halfTime * 1.2f, () => { item._Anims[5] = null; });
    }
  }
}