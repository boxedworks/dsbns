
//
public static class TipHelper
{

  static string[] _Tips_General = new[]
  {
        "it is highly recommended to play this game with a controller",
        "don't know the controls? check the options menu",
        "want to play with your friend? use remote-play on Steam!",
        "check out per-player control preferences in the options menu",
        "some utilities can be picked up to use again",
        "melee and range weapons have cooldowns before you can use them again",
        "&LT is for the left weapon, &RT is for the right weapon",
        "&LB is for the left utility, &RB is for the right utility",
        "switch weapon pairs with the &NB button",
        "watch your ammo. reload with the &WB button",
        "move with the &LS stick",
        "aim with the &RS stick",
        "try moving and aiming at the same time",
        "have a gun in each hand? try pressing the &SB button",
        "pause with the   &PU   button",
        "try using both weapons and utilities",
        "you have unlimited ammo! shoot and reload as fast as you can",
        "you have limited utility uses",
        "you can change your player color in the control options",
        "hold down the button before throwing a utility to make it go further",
        "your health is displayed under your 'P1', 'P2', etc, ui",
        "powerful gun's bullets penetrate through enemies",
        "explosives have a ring around them showing how big the explosion will be",
        "some guns reload bullets one-by-one; make sure to reload each bullet",
        "bullets will collide with each other; stronger ones beating weaker ones!",
        "need to reload multiple times? hold down &WB",
        "you can hide from explosions behind walls",
        "some melee weapons can kill more than one person per swing",
        "some weapons are two-handed which means you can't equip a second weapon",
        "check out some common stats for your playtime in the pause menu",
        "shoot your teammates",
        "press the &UD button to swap which hand each of your weapons are in",
        "you can find overall stats in the options menu",
        "if you have a one-handed melee weapon, press &RS behind an enemy!",
        "if melee weapons clash, you can use them again instantly!",

        "equip mods to gain special attributes",
        "you can equip up to 4 mods",
    };
  static string[] _Tips_Classic = new string[]
  {
        "beat MISSION levels quickly to get the highest rank and $$",
        "each new level rank gives money for the shop (up to $4 per level)",
        "complete MISSION level directories to add more unlocks to the shop",
        "buy 'MAX_EQUIPMENT_POINTS' in the shop to equip more in your loadouts",
        "you can only equip as many items as you have equipment points",
        "use filters in the shop or unlock menus to make them easier to read",
        "most actions make noise that can alert enemies",
        "stuck on a level? try making a different loadout",
        "you only have 1 health in MISSION mode",
        "equip two of the same weapon or utility if you have enough points",
        "switch weapons or edit a loadout mid-game if you are at the start area",
        "use the &LD or &RD buttons on the D-PAD to cycle through your custom loadouts",
        "quickly restart the level with the   &RE   button",
        "playing with more than one person? you cannot restart unless you are alive",
        "when editing loadouts, read the tags on items to learn more about them",
        "when editing loadouts, press &WB to quickly remove a peice of equipment",
        "you need to have enough money + max equipment points to buy things in the shop",
  };
  static string[] _Tips_Survival = new string[]
  {
        "use the &LD or &RD buttons on the D-PAD to specify a side to buy an item for",
        "you start with 3 health in ZOMBIE mode",
        "you always start with just a knife in ZOMBIE mode",
        "buy upgrades in ZOMBIE with the &EB button",
        "playing with other people? share money with the &DD button",
        "the more people there are, the more zombies spawn each wave",
        "the last kill of each wave grants bonus points",
        "if you play with a friend and die, you will respawn at the beginning of the next wave",
        "if you play with a friend, only one person has to survive",
        "the waves won't stop coming",
        "you get your utilities back at the start of each wave",
        "earn points to buy upgrades by killing enemies",
        "explore the map to find better upgrades",
        "you can't hide in ZOMBIE mode",
        "weapon placement is somewhat random each time you play",
        "on death, you lose all of your equipment",
        "in ZOMBIE mode, you need the akimbo perk to two-hand some weapons"
  };
  static string[] _Tips_Versus = new string[]
  {
        "try out different versus settings",
  };

  public static string GetTip(GameScript.GameModes mode)
  {
    // Check modes
    var tipArray = mode == GameScript.GameModes.MISSIONS ? _Tips_Classic : (mode == GameScript.GameModes.ZOMBIE ? _Tips_Survival : _Tips_Versus);
    var mode_string = mode.ToString().ToLower();
    if (UnityEngine.Random.value <= 0.5f || mode == GameScript.GameModes.PARTY)
    {
      tipArray = _Tips_General;
      mode_string = "general";
    }

    // Format tip
    var selectedTip = "";
    while (true)
    {
      selectedTip = tipArray[UnityEngine.Random.Range(0, tipArray.Length)];
      if (GameScript.s_IsVr && !selectedTip.Contains("&"))
        break;
      else if (!GameScript.s_IsVr)
        break;
    }

    return string.Format("*tip(<color=yellow>{0}</color>): {1}", mode_string, selectedTip);
  }

}