using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Ragdoll.Equippables
{

  //
  public struct ProjectileCollisionData
  {
    public GameObject _GameObject;

    public int _PenatrationAmount;
    public System.Action<ProjectileCollisionData> _OnDisable;
    public bool _ShouldDisable;
    public bool _CanDestroyObjects;
    public Vector3 _SpawnPosition;
    public ActiveRagdoll _DamageSource;

    //
    public BulletScript _BulletScript;
    public bool _IsBullet { get { return _BulletScript != null; } }
  }

}