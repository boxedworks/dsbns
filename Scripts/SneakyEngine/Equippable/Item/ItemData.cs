
using SneakyEngine.Equippable.Utility;

namespace SneakyEngine.Equippable.Item
{

  [System.Serializable]
  public class ItemData
  {
    public int ClipSize;
    public int MinimumClipToFire;
    public int ProjectilesPerShot;
    public int BurstPerShot;
    public int PenetrationAmount;

    public bool IsMelee;
    public bool IsTwoHanded;
    public bool ReloadOneAtTime;
    public bool IsSilenced;
    public bool IsThrowable;
    public bool CanDismember;
    public bool UseOnRelease;
    public bool UseRandomSpread;
    public bool IsChargeHold;

    public float ReloadTime;
    public float UseRate;
    public float BurstRate;
    public float HitForce;
    public float BulletSpread;
    public float ShootForce;
    public float ShootForwardForce;

    public int FireMode;
    public UtilityType CustomProjectile;
    public int Type;
    public UtilityType UtilityType;
  }

}