using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollTriggerScript : MonoBehaviour
{

  ActiveRagdoll _ragdoll;

  private void OnTriggerEnter(Collider other)
  {
    if (_ragdoll == null)
    {
      _ragdoll = ActiveRagdoll.GetRagdoll(transform.gameObject);
      if (_ragdoll == null) return;
    }
    _ragdoll.OnTriggerEnter(other);
  }

  private void OnTriggerExit(Collider other)
  {
    if (_ragdoll == null)
    {
      _ragdoll = ActiveRagdoll.GetRagdoll(transform.gameObject);
      if (_ragdoll == null) return;
    }
    _ragdoll.OnTriggerExit(other);
  }

  private void OnTriggerStay(Collider other)
  {
    if (_ragdoll == null)
    {
      _ragdoll = ActiveRagdoll.GetRagdoll(transform.gameObject);
      if (_ragdoll == null) return;
    }
    _ragdoll.OnTriggerStay(other);
  }

  /*private void OnCollisionEnter(Collision collision)
  {
    if (_ragdoll == null)
    {
      _ragdoll = ActiveRagdoll.GetRagdoll(transform.gameObject);
      if (_ragdoll == null) return;
    }
    _ragdoll.OnCollisionEnter(collision);
  }*/
}
