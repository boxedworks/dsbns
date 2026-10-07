using Assets.Scripts.Ragdoll;
using UnityEngine;

public class RagdollTriggerScript : MonoBehaviour
{

  ActiveRagdoll _ragdoll
  {
    get
    {
      if (__ragdoll == null)
        _ragdoll = ActiveRagdoll.GetRagdoll(transform.gameObject);
      return __ragdoll;
    }
    set { __ragdoll = value; }
  }
  ActiveRagdoll __ragdoll;

  private void OnTriggerEnter(Collider other)
  {
    if (_ragdoll == null) return;
    _ragdoll.OnTriggerEnter(other);
  }

  private void OnTriggerExit(Collider other)
  {
    if (_ragdoll == null) return;
    _ragdoll.OnTriggerExit(other);
  }

  private void OnTriggerStay(Collider other)
  {
    if (_ragdoll == null) return;
    _ragdoll.OnTriggerStay(other);
  }
}
