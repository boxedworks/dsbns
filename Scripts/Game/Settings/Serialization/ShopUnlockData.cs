
using Assets.Scripts.Game.Items;

namespace Assets.Scripts.Settings.Serialization
{
  [System.Serializable]
  public struct ShopUnlockData
  {
    public ShopHelper.Unlocks Unlock;
    public enum UnlockValueType
    {
      LOCKED,
      AVAILABLE,
      UNLOCKED
    }
    public UnlockValueType UnlockValue;
  }
}