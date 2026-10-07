using UnityEngine;

namespace Assets.Scripts.Ragdoll.Equippables
{
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

}