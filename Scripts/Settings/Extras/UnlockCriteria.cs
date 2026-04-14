
using Assets.Scripts.Game.Items;

namespace Assets.Scripts.Settings.Extras
{

  public struct UnlockCriteria
  {
    // Level and difficulty
    public int level;
    public int difficulty;
    public int rating;

    // Extras
    public ShopHelper.Unlocks[] extras;

    // Loadout
    public string loadoutDesc;
    public ItemManager.Items[] items;
    public UtilityScript.UtilityType[] utilities;
    public Perk.PerkType[] perks;
  }

}