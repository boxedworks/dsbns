
using UnityEngine;

namespace SneakyEngine.Ragdoll.Body
{

  public class TransformState
  {
    public Vector3 _pos, _rot;
    public TransformState(Transform t)
    {
      _pos = t.localPosition;
      _rot = t.localEulerAngles;
    }
  }

}
