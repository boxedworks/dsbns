using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabScript : MonoBehaviour
{

  bool _connected = true;

  ActiveRagdoll _ragdoll;

  public void Start()
  {
    _ragdoll = ActiveRagdoll.GetRagdoll(transform.parent.gameObject);
  }

  void OnCollisionEnter(Collision c)
  {
    // Check if already grabbing something
    if (_connected) return;

    // Check if has rb
    Rigidbody r = c.collider.transform.GetComponent<Rigidbody>();
    if (r == null) return;

    // Check if is self
    if (_ragdoll.IsSelf(r.gameObject)) return;

    // Check if is ragdoll
    ActiveRagdoll rag = ActiveRagdoll.GetRagdoll(r.gameObject);
    if (rag != null)
    {
      if (rag.Active()) return;
    }

    Debug.Log(c.collider.name);

    _connected = true;

    Joint j = gameObject.AddComponent<HingeJoint>();
    j.connectedBody = r;
  }

  public void Release()
  {
    if (!_connected) return;

    Destroy(gameObject.GetComponents<HingeJoint>()[1]);
    _connected = false;
  }
}