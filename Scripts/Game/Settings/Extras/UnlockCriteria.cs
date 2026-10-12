
using SneakyGame.Game.Items;
using SneakyEngine.Equippable.Utility;

namespace SneakyGame.Settings.Extras
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
    public UtilityType[] utilities;
    public Perk.PerkType[] perks;
  }

}