using System;
using System.Collections.Generic;
using SneakyEngine.Ragdoll.Body;
using SneakyEngine.Utility;
using UnityEngine;

namespace SneakyEngine.Ragdoll
{

  public class RagdollController
  {

    //
    public static int s_ID;
    public int Id;

    // Transform and rig references
    public Transform Transform, Controller;
    public Rig Rig;
    public Rigidbody Hip => Rig.HipRb;

    // Rendering
    protected Color color;
    protected SkinnedMeshRenderer meshRenderer;

    static List<Tuple<Material, Material>> s_ragdollMaterials;

    // Stats
    public int Health;

    public RagdollController(GameObject ragdoll, Transform controller)
    {
      Id = s_ID++;

      Transform = ragdoll.transform;
      Controller = controller;

      var hip = Transform.GetChild(1).GetChild(0).GetChild(0);
      Rig = new Rig(hip);

      // Set default health
      Health = 1;

      // Set materials
      meshRenderer = Transform.GetChild(0).GetComponent<SkinnedMeshRenderer>();

      s_ragdollMaterials ??= new();
      if (Id == s_ragdollMaterials.Count)
      {
        var mat0 = new Material(meshRenderer.sharedMaterials[0]);
        mat0.SetFloat("_Metallic", 0.8f);
        var mat1 = new Material(meshRenderer.sharedMaterials[1]);
        mat1.SetFloat("_Metallic", 0.8f);

        s_ragdollMaterials.Add(new Tuple<Material, Material>(mat0, mat1));
      }
      Resources.UnloadAsset(meshRenderer.sharedMaterials[0]);
      Resources.UnloadAsset(meshRenderer.sharedMaterials[1]);
      meshRenderer.sharedMaterials = new Material[] { s_ragdollMaterials[Id].Item1, s_ragdollMaterials[Id].Item2 };
    }

    public static void Reset()
    {
      Rig.Reset();
    }

    public float GetDistanceTo(Vector3 position)
    {
      return MathC.Get2DDistance(Hip.position, position);
    }
  }

}