
using SneakyEngine.Engine;
using SneakyEngine.Ragdoll.Body;
using UnityEngine;

namespace SneakyEngine.Ragdoll
{

  public class RagdollController
  {

    public static int s_ID;
    public int Id;


    public Transform Transform, Controller;
    public Rig Rig;

    public Rigidbody Hip => Rig.HipRb;

    public RagdollController(GameObject ragdoll, Transform controller)
    {
      Id = s_ID++;

      Transform = ragdoll.transform;
      Controller = controller;

      var hip = Transform.GetChild(1).GetChild(0).GetChild(0);
      Rig = new Rig(hip);
    }

  }

}