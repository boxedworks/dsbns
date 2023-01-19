using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathC {

  public static Vector3 Get2DVector(Vector3 v)
  {
    return new Vector3(v.x, 0f, v.z);
  }

  public static float Get2DDistance(Vector3 pos0, Vector3 pos1)
  {
    return Vector3.Distance(Get2DVector(pos0), Get2DVector(pos1));
  }
}
