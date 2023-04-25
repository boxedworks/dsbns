using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class Shop
{

  public static class Tip
  {

    static string[] _Tips_General = new string[]
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
      "need to reload multiple times? hold down &WB",
      "you can hide from explosions behind walls",
      "some melee weapons can kill more than one person per swing",
      "some weapons are two-handed which means you can't equip a second weapon",
      "check out some common stats for your playtime in the pause menu",
      "shoot your teammates",
      "press the &UD button to swap which hand each of your weapons are in",
      "you can find overall stats in the options menu",
      "if you have a one-handed melee weapon, press &RS behind an enemy!",

      "equip mods to gain special attributes",
      "you can equip up to 4 mods",

      "every steam review helps this game :)"
    };
    static string[] _Tips_Classic = new string[]
    {
      "beat classic levels quickly to get the highest rank and $$$",
      "each new level rank gives money for the shop (up to $4 per level)",
      "complete classic level directories to add more unlocks to the shop",
      "buy 'MAX_EQUIPMENT_POINTS' in the shop to equip more in your loadouts",
      "you can only equip as many items as you have equipment points",
      "use filters in the shop or unlock menus to make them easier to read",
      "most actions make noise that can alert enemies",
      "stuck on a level? try making a different loadout",
      "you only have 1 health in classic mode",
      "equip two of the same weapon or utility if you have enough points",
      "switch weapons or edit a loadout mid-game if you are at the start area",
      "use the &LD or &RD buttons on the D-PAD to cycle through your custom loadouts",
      "quickly restart the level with the   &RE   button",
      "playing with more than one person? you cannot restart if you are dead",
      "when changing loadouts, read the tags on items to learn more about them",
      "when changing loadouts, press &WB to quickly remove a peice of equipment",
      "you need to have enough money + max equipment points to buy things in the shop",
    };
    static string[] _Tips_Survival = new string[]
    {
      "use the &LD or &RD buttons on the D-PAD to specify a side to buy an item for",
      "you start with 3 health in survival mode",
      "you always start with just a knife in survival mode",
      "buy upgrades in survival with the &EB button",
      "playing with other people? share money with the &DD button",
      "the more people there are, the more zombies spawn each wave",
      "the last kill of each wave grants bonus points",
      "if you play with a friend and die, you will respawn at the beginning of the next wave",
      "if you play with a friend, only one person has to survive",
      "the waves won't stop coming",
      "you get your utilities back at the start of each wave",
      "earn points to buy upgrades by killing enemies",
      "explore the map to find better upgrades",
      "you can't hide in survival",
      "weapon placement is somewhat random each time you play",
      "on death, you lose all of your equipment",
      "in survival mode, you need the akimbo perk to two-hand some weapons"
    };

    public static string GetTip(GameScript.GameModes mode)
    {
      // Check modes
      var array = mode == GameScript.GameModes.CLASSIC ? _Tips_Classic : _Tips_Survival;
      var mode_string = mode.ToString().ToLower();
      if (UnityEngine.Random.value <= 0.5f)
      {
        array = _Tips_General;
        mode_string = "general";
      }
      // Format tip
      return string.Format("*tip(<color=yellow>{0}</color>): {1}", mode_string, array[UnityEngine.Random.Range(0, array.Length)]);
    }

  }

  static int AvailablePoints;
  public static int _AvailablePoints
  {
    get
    {
      if ((Settings._Classic_0_TopRated?._value ?? false) && (Settings._Classic_1_TopRated?._value ?? false))
        return 999;
      return AvailablePoints;
    }
    set
    {
      AvailablePoints = value;
      PlayerPrefs.SetInt("Shop_availablePoints", AvailablePoints);
    }
  }

  static int DisplayMode;
  public static int _DisplayMode
  {
    get { return DisplayMode; }
    set
    {
      DisplayMode = value % 3;
      PlayerPrefs.SetInt("Shop_DisplayMode", DisplayMode);
    }
  }
  public enum DisplayModes
  {
    AVAILABLE,
    ALL,
    PURCHASED,
  }

  static int LoadoutDisplayMode;
  public static int _LoadoutDisplayMode
  {
    get { return LoadoutDisplayMode; }
    set
    {
      LoadoutDisplayMode = value % 2;
      PlayerPrefs.SetInt("Shop_LoadoutDisplayMode", DisplayMode);
    }
  }

  // Max amount of a utility you can have in one side
  public static Dictionary<UtilityScript.UtilityType, int> _Utility_Cap;

  public enum Unlocks
  {
    ITEM_KNIFE,
    ITEM_FRYING_PAN,
    ITEM_PISTOL_SILENCED,
    ITEM_MACHINE_PISTOL,
    ITEM_DOUBLE_PISTOL,
    ITEM_REVOLVER,
    ITEM_RIFLE,
    ITEM_RIFLE_LEVER,
    ITEM_DMR,
    ITEM_SNIPER,
    ITEM_UZI,
    ITEM_CROSSBOW,
    ITEM_SHOTGUN_PUMP,
    ITEM_SHOTGUN_DOUBLE,
    ITEM_SHOTGUN_BURST,
    ITEM_AK47,
    ITEM_M16,
    ITEM_BAT,
    ITEM_SWORD,
    ITEM_AXE,

    ITEM_FLAMETHROWER,
    ITEM_ROCKET_FIST,
    ITEM_STICKY_GUN,

    ITEM_GRENADE_LAUNCHER,

    UTILITY_GRENADE,
    UTILITY_GRENADE_IMPACT,
    UTILITY_C4,
    UTILITY_SHURIKEN,
    UTILITY_SHURIKEN_BIG,
    UTILITY_KUNAI_EXPLOSIVE,
    UTILITY_KUNAI_STICKY,
    UTILITY_STOP_WATCH,
    UTILITY_INVISIBILITY,
    UTILITY_DASH,

    //PERK_PENETRATION_UP,
    //PERK_ARMOR_UP,
    //PERK_SPEED_UP,
    MOD_EXPLOSION_RESISTANCE,
    MOD_EXPLOSIONS_UP,
    MOD_FASTER_RELOAD,
    MOD_MAX_AMMO_UP,
    MOD_LASER_SIGHTS,
    MOD_NO_SLOWMO,
    MOD_ARMOR_UP,
    MOD_PENETRATION_UP,
    MOD_SMART_BULLETS,

    MAX_EQUIPMENT_POINTS_0,
    MAX_EQUIPMENT_POINTS_1,
    MAX_EQUIPMENT_POINTS_2,
    MAX_EQUIPMENT_POINTS_3,
    MAX_EQUIPMENT_POINTS_4,
    MAX_EQUIPMENT_POINTS_5,
    MAX_EQUIPMENT_POINTS_6,
    MAX_EQUIPMENT_POINTS_7,
    MAX_EQUIPMENT_POINTS_8,
    MAX_EQUIPMENT_POINTS_9,

    MODE_SURVIVAL,

    EXTRA_TIME,
    EXTRA_GRAVITY,
    EXTRA_HORDE,
    EXTRA_CHASE,
    EXTRA_PLAYER_AMMO,
    EXTRA_ENEMY_MULTI,
    EXTRA_BLOOD_FX,
    EXTRA_EXPLODED,
    EXTRA_CROWNMODE,

    TUTORIAL_PART0,
    TUTORIAL_PART1
  }

  static int Max_Equipment_Points;
  public static int _Max_Equipment_Points
  {
    get { return Max_Equipment_Points; }
    set
    {
      Max_Equipment_Points = value;
      PlayerPrefs.SetInt("Shop_maxEquipmentPoints", value);
    }
  }

  public static List<Unlocks> _Unlocks_Available,
    _Unlocks;
  public static Dictionary<Unlocks, Tuple<string, int>> _Unlocks_Descriptions;
  static Dictionary<string, Unlocks[]> _Unlocks_Vault;
  public static Unlocks[] _Unlocks_Ignore_Shop;

  static List<GameScript.ItemManager.Items> _TwoHanded_Dictionary, _ActualTwoHanded_Dictionary;

  public static void Init()
  {
    DisplayMode = PlayerPrefs.GetInt("Shop_DisplayMode", 0);
    LoadoutDisplayMode = PlayerPrefs.GetInt("Shop_LoadoutDisplayMode", 0);

    Perk.Init();

    _AvailablePoints = PlayerPrefs.GetInt("Shop_availablePoints", 3);
    _Max_Equipment_Points = PlayerPrefs.GetInt("Shop_maxEquipmentPoints", 1);

    _Unlocks = new List<Unlocks>();
    _Unlocks_Available = new List<Unlocks>();

    _UnlockString = "";

    _Unlocks_Descriptions = new Dictionary<Unlocks, Tuple<string, int>>();
    _Unlocks_Descriptions.Add(Unlocks.ITEM_KNIFE, new Tuple<string, int>("melee, fast", 3));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_FRYING_PAN, new Tuple<string, int>("melee, fast, block", 10));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_AXE, new Tuple<string, int>("melee, slower, wide-sweep", 15));
    //_Unlocks_Descriptions.Add(Unlocks.ITEM_BAT, new Tuple<string, int>("melee, two-handed, wide-sweep", 10));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_SWORD, new Tuple<string, int>("melee, two-handed, wide-sweep", 35));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_PISTOL_SILENCED, new Tuple<string, int>("handgun, silenced, fast-reload", 15));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_MACHINE_PISTOL, new Tuple<string, int>("handgun, 3-burst, fast-reload", 15));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_DOUBLE_PISTOL, new Tuple<string, int>("handgun, double-barrel", 15));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_REVOLVER, new Tuple<string, int>("handgun, powerful, slower-reload", 25));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_UZI, new Tuple<string, int>("gun, automatic, small-magazine", 20));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_CROSSBOW, new Tuple<string, int>("bow, powerful, slow-reload", 15));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_SHOTGUN_PUMP, new Tuple<string, int>("shotgun, silenced, reload", 20));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_SHOTGUN_DOUBLE, new Tuple<string, int>("shotgun, powerful, reload", 30));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_SHOTGUN_BURST, new Tuple<string, int>("shotgun, two-burst, reload", 40));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_AK47, new Tuple<string, int>("rifle, automatic, slow-reload", 40));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_M16, new Tuple<string, int>("rifle, burst, slow-reload", 40));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_RIFLE, new Tuple<string, int>("rifle, semi-automatic, slow-fire", 15));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_RIFLE_LEVER, new Tuple<string, int>("rifle, semi-automatic, fast-fire", 30));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_DMR, new Tuple<string, int>("rifle, semi-automatic, slow-reload", 25));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_SNIPER, new Tuple<string, int>("bolt-action, semi-automatic, powerful", 25));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_GRENADE_LAUNCHER, new Tuple<string, int>("explosive, semi-automatic, slow-reload", 20));

    _Unlocks_Descriptions.Add(Unlocks.ITEM_FLAMETHROWER, new Tuple<string, int>("charge-shot, incendiary, slow-reload", 25));
    //_Unlocks_Descriptions.Add(Unlocks.ITEM_ROCKET_FIST, new Tuple<string, int>("charge-shot, melee, slow-reload", 0));
    _Unlocks_Descriptions.Add(Unlocks.ITEM_STICKY_GUN, new Tuple<string, int>("stealthy, chain, slow-reload", 20));

    _Unlocks_Descriptions.Add(Unlocks.UTILITY_SHURIKEN, new Tuple<string, int>("throwable, pick-up, small", 3));
    _Unlocks_Descriptions.Add(Unlocks.UTILITY_SHURIKEN_BIG, new Tuple<string, int>("throwable, pick-up, large", 25));
    _Unlocks_Descriptions.Add(Unlocks.UTILITY_KUNAI_EXPLOSIVE, new Tuple<string, int>("throwable, explodes, small", 15));
    _Unlocks_Descriptions.Add(Unlocks.UTILITY_KUNAI_STICKY, new Tuple<string, int>("throwable, delalyed-explosion, small", 25));
    _Unlocks_Descriptions.Add(Unlocks.UTILITY_GRENADE, new Tuple<string, int>("throwable, explosive, large-radius", 20));
    _Unlocks_Descriptions.Add(Unlocks.UTILITY_GRENADE_IMPACT, new Tuple<string, int>("throwable, contact-explosive", 25));
    _Unlocks_Descriptions.Add(Unlocks.UTILITY_C4, new Tuple<string, int>("throwable, explosive, remote-controlled", 15));
    _Unlocks_Descriptions.Add(Unlocks.UTILITY_STOP_WATCH, new Tuple<string, int>("useable, slows-time", 20));
    _Unlocks_Descriptions.Add(Unlocks.UTILITY_INVISIBILITY, new Tuple<string, int>("useable, short-invisibility", 20));
    //_Unlocks_Descriptions.Add(Unlocks.UTILITY_DASH, new Tuple<string, int>("useable, quick speed boost", 0));

    _Unlocks_Descriptions.Add(Unlocks.MOD_LASER_SIGHTS, new Tuple<string, int>("-", 5));
    _Unlocks_Descriptions.Add(Unlocks.MOD_NO_SLOWMO, new Tuple<string, int>("-", 1));
    _Unlocks_Descriptions.Add(Unlocks.MOD_FASTER_RELOAD, new Tuple<string, int>("-", 25));
    _Unlocks_Descriptions.Add(Unlocks.MOD_MAX_AMMO_UP, new Tuple<string, int>("-", 25));
    _Unlocks_Descriptions.Add(Unlocks.MOD_EXPLOSION_RESISTANCE, new Tuple<string, int>("-", 25));
    _Unlocks_Descriptions.Add(Unlocks.MOD_EXPLOSIONS_UP, new Tuple<string, int>("-", 25));

    _Unlocks_Descriptions.Add(Unlocks.MOD_ARMOR_UP, new Tuple<string, int>("-", 0));
    _Unlocks_Descriptions.Add(Unlocks.MOD_PENETRATION_UP, new Tuple<string, int>("-", 0));

    _Unlocks_Descriptions.Add(Unlocks.MOD_SMART_BULLETS, new Tuple<string, int>("-", 30));

    _Unlocks_Descriptions.Add(Unlocks.MAX_EQUIPMENT_POINTS_0, new Tuple<string, int>("equipment-points, (+1)", 5));
    _Unlocks_Descriptions.Add(Unlocks.MAX_EQUIPMENT_POINTS_1, new Tuple<string, int>("equipment-points, (+1)", 5));
    _Unlocks_Descriptions.Add(Unlocks.MAX_EQUIPMENT_POINTS_2, new Tuple<string, int>("equipment-points, (+1)", 10));
    _Unlocks_Descriptions.Add(Unlocks.MAX_EQUIPMENT_POINTS_3, new Tuple<string, int>("equipment-points, (+1)", 15));
    _Unlocks_Descriptions.Add(Unlocks.MAX_EQUIPMENT_POINTS_4, new Tuple<string, int>("equipment-points, (+1)", 20));
    _Unlocks_Descriptions.Add(Unlocks.MAX_EQUIPMENT_POINTS_5, new Tuple<string, int>("equipment-points, (+1)", 25));
    _Unlocks_Descriptions.Add(Unlocks.MAX_EQUIPMENT_POINTS_6, new Tuple<string, int>("equipment-points, (+1)", 30));
    _Unlocks_Descriptions.Add(Unlocks.MAX_EQUIPMENT_POINTS_7, new Tuple<string, int>("equipment-points, (+1)", 35));
    _Unlocks_Descriptions.Add(Unlocks.MAX_EQUIPMENT_POINTS_8, new Tuple<string, int>("equipment-points, (+1)", 40));
    _Unlocks_Descriptions.Add(Unlocks.MAX_EQUIPMENT_POINTS_9, new Tuple<string, int>("equipment-points, (+1)", 50));

    _Unlocks_Descriptions.Add(Unlocks.MODE_SURVIVAL, new Tuple<string, int>("unlocks 'survival' mode", 11));

    _Unlocks_Descriptions.Add(Unlocks.EXTRA_GRAVITY, new Tuple<string, int>("unlocks 'gravity' extra", 0));
    _Unlocks_Descriptions.Add(Unlocks.EXTRA_PLAYER_AMMO, new Tuple<string, int>("unlocks 'player ammo' extra", 0));
    _Unlocks_Descriptions.Add(Unlocks.EXTRA_ENEMY_MULTI, new Tuple<string, int>("unlocks 'enemy multiplier' extra", 0));
    _Unlocks_Descriptions.Add(Unlocks.EXTRA_CHASE, new Tuple<string, int>("unlocks 'chaser' extra", 0));
    _Unlocks_Descriptions.Add(Unlocks.EXTRA_TIME, new Tuple<string, int>("unlocks 'time' extra", 0));
    _Unlocks_Descriptions.Add(Unlocks.EXTRA_HORDE, new Tuple<string, int>("unlocks 'horde' extra", 0));
    _Unlocks_Descriptions.Add(Unlocks.EXTRA_BLOOD_FX, new Tuple<string, int>("unlocks 'blood fx' extra", 0));
    _Unlocks_Descriptions.Add(Unlocks.EXTRA_EXPLODED, new Tuple<string, int>("unlocks 'explode death' extra", 0));
    _Unlocks_Descriptions.Add(Unlocks.EXTRA_CROWNMODE, new Tuple<string, int>("unlocks 'crown' extra", 0));

    _Unlocks_Descriptions.Add(Unlocks.TUTORIAL_PART0, new Tuple<string, int>("", 0));
    _Unlocks_Descriptions.Add(Unlocks.TUTORIAL_PART1, new Tuple<string, int>("", 0));

    // Add unlocks to ignore in classic shop
    _Unlocks_Ignore_Shop = new Unlocks[]{
      Unlocks.TUTORIAL_PART0,
      Unlocks.TUTORIAL_PART1,

      Unlocks.MOD_ARMOR_UP,
      Unlocks.MOD_PENETRATION_UP,
    };

    // Load available / purchased unlocks
    var shopPointsTotaled = 0;
    foreach (var pair in _Unlocks_Descriptions)
    {
      var unlock = pair.Key;
      if (PlayerPrefs.GetInt($"Shop_UnlocksAvailable_{unlock}", 0) == 1)
        AddAvailableUnlock(unlock);
      if (PlayerPrefs.GetInt($"Shop_Unlocks_{unlock}", 0) == 1)
        Unlock(unlock);

      shopPointsTotaled += pair.Value.Item2;
    }
    Debug.Log($"Total points: ({_AvailablePoints}) {shopPointsTotaled} / {((1 * 11 * 2) + (10 * 12 * 2)) * 4}");

    // Set starter unlocks
    AddAvailableUnlock(Unlocks.ITEM_KNIFE);
    AddAvailableUnlock(Unlocks.UTILITY_SHURIKEN);

    AddAvailableUnlock(Unlocks.MAX_EQUIPMENT_POINTS_0);
    AddAvailableUnlock(Unlocks.MAX_EQUIPMENT_POINTS_1);
    AddAvailableUnlock(Unlocks.ITEM_PISTOL_SILENCED);

    AddAvailableUnlock(Unlocks.ITEM_STICKY_GUN);
    AddAvailableUnlock(Unlocks.MOD_SMART_BULLETS);
    AddAvailableUnlock(Unlocks.ITEM_FRYING_PAN);

    //AddAvailableUnlock(Unlocks.ITEM_FLAMETHROWER);
    //AddAvailableUnlock(Unlocks.ITEM_ROCKET_FIST);

    // Add unlocks to vault
    _Unlocks_Vault = new Dictionary<string, Unlocks[]>();

    if (GameScript._Singleton._IsDemo)
    {
      _Unlocks_Vault.Add("classic_0", new Unlocks[] { Unlocks.ITEM_AXE, Unlocks.UTILITY_GRENADE });
      _Unlocks_Vault.Add("classic_1", new Unlocks[] { Unlocks.MOD_LASER_SIGHTS, Unlocks.UTILITY_KUNAI_EXPLOSIVE, Unlocks.MAX_EQUIPMENT_POINTS_2 });
      _Unlocks_Vault.Add("classic_2", new Unlocks[] { Unlocks.ITEM_RIFLE, Unlocks.ITEM_MACHINE_PISTOL });
    }
    else
    {
      _Unlocks_Vault.Add("classic_0", new Unlocks[] { Unlocks.ITEM_AXE, Unlocks.UTILITY_GRENADE });
      _Unlocks_Vault.Add("classic_1", new Unlocks[] { Unlocks.MOD_LASER_SIGHTS, Unlocks.UTILITY_KUNAI_EXPLOSIVE, Unlocks.MAX_EQUIPMENT_POINTS_2 });
      _Unlocks_Vault.Add("classic_2", new Unlocks[] { Unlocks.MODE_SURVIVAL, Unlocks.ITEM_RIFLE, Unlocks.ITEM_MACHINE_PISTOL });
      _Unlocks_Vault.Add("classic_3", new Unlocks[] { Unlocks.ITEM_DOUBLE_PISTOL, Unlocks.UTILITY_STOP_WATCH, Unlocks.MAX_EQUIPMENT_POINTS_3 });
      _Unlocks_Vault.Add("classic_4", new Unlocks[] { Unlocks.ITEM_AXE, Unlocks.ITEM_REVOLVER, Unlocks.UTILITY_C4, Unlocks.UTILITY_SHURIKEN_BIG });
      _Unlocks_Vault.Add("classic_5", new Unlocks[] { Unlocks.MOD_NO_SLOWMO, Unlocks.UTILITY_GRENADE_IMPACT });
      _Unlocks_Vault.Add("classic_6", new Unlocks[] { Unlocks.ITEM_CROSSBOW, Unlocks.MAX_EQUIPMENT_POINTS_4 });
      _Unlocks_Vault.Add("classic_7", new Unlocks[] { Unlocks.UTILITY_KUNAI_STICKY, Unlocks.UTILITY_INVISIBILITY });
      _Unlocks_Vault.Add("classic_8", new Unlocks[] { Unlocks.ITEM_UZI, Unlocks.ITEM_SHOTGUN_PUMP });
      _Unlocks_Vault.Add("classic_9", new Unlocks[] { Unlocks.ITEM_RIFLE_LEVER, Unlocks.MAX_EQUIPMENT_POINTS_5 });
      _Unlocks_Vault.Add("classic_10", new Unlocks[] { Unlocks.MOD_EXPLOSIONS_UP, Unlocks.ITEM_GRENADE_LAUNCHER });

      _Unlocks_Vault.Add("classic_12", new Unlocks[] { Unlocks.ITEM_SNIPER, Unlocks.ITEM_SWORD });
      _Unlocks_Vault.Add("classic_13", new Unlocks[] { Unlocks.ITEM_DMR, Unlocks.MAX_EQUIPMENT_POINTS_6 });
      _Unlocks_Vault.Add("classic_14", new Unlocks[] { Unlocks.MOD_EXPLOSION_RESISTANCE, Unlocks.ITEM_SHOTGUN_DOUBLE });
      _Unlocks_Vault.Add("classic_15", new Unlocks[] { Unlocks.ITEM_M16, Unlocks.MAX_EQUIPMENT_POINTS_7 });
      _Unlocks_Vault.Add("classic_16", new Unlocks[] { Unlocks.MOD_FASTER_RELOAD });
      _Unlocks_Vault.Add("classic_17", new Unlocks[] { Unlocks.ITEM_FLAMETHROWER });
      _Unlocks_Vault.Add("classic_18", new Unlocks[] { Unlocks.ITEM_AK47, Unlocks.MAX_EQUIPMENT_POINTS_8 });
      _Unlocks_Vault.Add("classic_19", new Unlocks[] { Unlocks.MOD_MAX_AMMO_UP });
      _Unlocks_Vault.Add("classic_22", new Unlocks[] { Unlocks.ITEM_SHOTGUN_BURST, Unlocks.MAX_EQUIPMENT_POINTS_9 });
    }

    // Create utility cap
    _Utility_Cap = new Dictionary<UtilityScript.UtilityType, int>();
    _Utility_Cap.Add(UtilityScript.UtilityType.GRENADE, 5);
    _Utility_Cap.Add(UtilityScript.UtilityType.GRENADE_IMPACT, 5);
    _Utility_Cap.Add(UtilityScript.UtilityType.C4, 5);
    _Utility_Cap.Add(UtilityScript.UtilityType.SHURIKEN, 5);
    _Utility_Cap.Add(UtilityScript.UtilityType.SHURIKEN_BIG, 5);
    _Utility_Cap.Add(UtilityScript.UtilityType.KUNAI_EXPLOSIVE, 5);
    _Utility_Cap.Add(UtilityScript.UtilityType.KUNAI_STICKY, 5);
    _Utility_Cap.Add(UtilityScript.UtilityType.STOP_WATCH, 2);
    _Utility_Cap.Add(UtilityScript.UtilityType.INVISIBILITY, 2);
    _Utility_Cap.Add(UtilityScript.UtilityType.DASH, 5);

    //
    _TwoHanded_Dictionary = new List<GameScript.ItemManager.Items>();
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.SWORD);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.BAT);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.DMR);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.RIFLE);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.RIFLE_LEVER);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.CROSSBOW);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.SNIPER);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.AK47);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.M16);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.SHOTGUN_BURST);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.SHOTGUN_PUMP);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.SHOTGUN_DOUBLE);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.GRENADE_LAUNCHER);
    _TwoHanded_Dictionary.Add(GameScript.ItemManager.Items.FLAMETHROWER);

    _ActualTwoHanded_Dictionary = new List<GameScript.ItemManager.Items>();
    _ActualTwoHanded_Dictionary.Add(GameScript.ItemManager.Items.SWORD);
    _ActualTwoHanded_Dictionary.Add(GameScript.ItemManager.Items.BAT);
  }

  // Get a list of items with ItemManager.Items enum
  public static GameScript.ItemManager.Items[] GetItemList()
  {
    var list = new List<GameScript.ItemManager.Items>();
    list.Add(GameScript.ItemManager.Items.NONE);
    foreach (var entry in _Unlocks_Descriptions)
      if (entry.Key.ToString().StartsWith("ITEM_"))
        list.Add((GameScript.ItemManager.Items)Enum.Parse(typeof(GameScript.ItemManager.Items), entry.Key.ToString().Substring(5)));
    return list.ToArray();
  }
  // Get a list of utilities with UtilityScript.UtilityType enum
  public static UtilityScript.UtilityType[] GetUtilityList()
  {
    var list = new List<UtilityScript.UtilityType>();
    list.Add(UtilityScript.UtilityType.NONE);
    foreach (var entry in _Unlocks_Descriptions)
      if (entry.Key.ToString().StartsWith("UTILITY_"))
        list.Add((UtilityScript.UtilityType)Enum.Parse(typeof(UtilityScript.UtilityType), entry.Key.ToString().Substring(8)));
    return list.ToArray();
  }
  // Get a list of perks with Perk.PerkType enum
  public static Perk.PerkType[] GetPerkList()
  {
    var list = new List<Perk.PerkType>();
    list.Add(Perk.PerkType.NONE);
    foreach (var entry in _Unlocks_Descriptions)
      if (entry.Key.ToString().StartsWith("MOD_"))
        list.Add((Perk.PerkType)Enum.Parse(typeof(Perk.PerkType), entry.Key.ToString().Substring(4)));
    return list.ToArray();
  }

  public static bool IsTwoHanded(GameScript.ItemManager.Items item)
  {
    return _TwoHanded_Dictionary.Contains(item);
  }
  public static bool IsActuallyTwoHanded(GameScript.ItemManager.Items item)
  {
    return _ActualTwoHanded_Dictionary.Contains(item);
  }

  // Append unlock
  public static void AddAvailableUnlock(Unlocks unlock, bool alert = false)
  {
    if (_Unlocks_Available.Contains(unlock)) return;
    _Unlocks_Available.Add(unlock);
    PlayerPrefs.SetInt($"Shop_UnlocksAvailable_{unlock}", 1);

    if (alert)
      _UnlockString += $"- new unlock added to shop: <color=yellow>{unlock}</color>\n";
  }
  public static string _UnlockString;

  public static void ShowUnlocks(Menu2.MenuType toMenu)
  {
    Menu2.GenericMenu(new string[] { "new unlocks\n\n", _UnlockString }, (UnityEngine.Random.value < 0.5f ? "wow" : "nice"), toMenu);
    _UnlockString = "";
  }

  // Unlock from available unlocks
  public static void Unlock(Unlocks unlock)
  {
    if (_Unlocks.Contains(unlock)) return;
    if (!_Unlocks_Descriptions.ContainsKey(unlock)) throw new Exception();
    _Unlocks.Add(unlock);

    if (PlayerPrefs.GetInt($"Shop_Unlocks_{unlock}", 0) == 0)
    {
      switch (unlock)
      {
        case Unlocks.MAX_EQUIPMENT_POINTS_0:
        case Unlocks.MAX_EQUIPMENT_POINTS_1:
        case Unlocks.MAX_EQUIPMENT_POINTS_2:
        case Unlocks.MAX_EQUIPMENT_POINTS_3:
        case Unlocks.MAX_EQUIPMENT_POINTS_4:
        case Unlocks.MAX_EQUIPMENT_POINTS_5:
        case Unlocks.MAX_EQUIPMENT_POINTS_6:
        case Unlocks.MAX_EQUIPMENT_POINTS_7:
        case Unlocks.MAX_EQUIPMENT_POINTS_8:
        case Unlocks.MAX_EQUIPMENT_POINTS_9:
          _Max_Equipment_Points++;
          break;
      }
      // Save pref
      PlayerPrefs.SetInt($"Shop_Unlocks_{unlock}", 1);
    }
  }

  public static int GetUtilityCount(UtilityScript.UtilityType utility)
  {
    int count = 1;
    switch (utility)
    {
      case UtilityScript.UtilityType.SHURIKEN:
      case UtilityScript.UtilityType.SHURIKEN_BIG:
        count = 2;
        break;
    }
    return count;
  }

  // Add available unlocks per vault
  public static void AddAvailableUnlockVault(string key)
  {
    if (!_Unlocks_Vault.ContainsKey(key)) return;
    foreach (var unlock in _Unlocks_Vault[key])
      AddAvailableUnlock(unlock, true);
  }

  public static bool Unlocked(Unlocks unlock)
  {

    // Disallow beta items
    //if (unlock == Unlocks.ITEM_ROCKET_FIST || unlock == Unlocks.ITEM_BAT)
    //  return false;

    // Allow all if editor
    if (Levels._EditingLoadout)
      return true;

    // Check if unlocked
    return _Unlocks.Contains(unlock);
  }

  public static class Perk
  {

    public enum PerkType
    {
      PENETRATION_UP,
      ARMOR_UP,
      SPEED_UP,
      EXPLOSION_RESISTANCE,
      EXPLOSIONS_UP,
      FASTER_RELOAD,
      MAX_AMMO_UP,
      FIRE_RATE_UP,
      LASER_SIGHTS,
      AKIMBO,
      NO_SLOWMO,
      SMART_BULLETS,

      NONE
    }

    public static Dictionary<PerkType, string> _PERK_DESCRIPTIONS;

    public static void Init()
    {
      _PERK_DESCRIPTIONS = new Dictionary<PerkType, string>();

      _PERK_DESCRIPTIONS.Add(PerkType.PENETRATION_UP, "bullets penetrate deeper");
      _PERK_DESCRIPTIONS.Add(PerkType.ARMOR_UP, "gain 2 extra health");
      _PERK_DESCRIPTIONS.Add(PerkType.SPEED_UP, "always run");
      _PERK_DESCRIPTIONS.Add(PerkType.EXPLOSION_RESISTANCE, "survive self-explosions");
      _PERK_DESCRIPTIONS.Add(PerkType.EXPLOSIONS_UP, "1.25x explosion radius");
      _PERK_DESCRIPTIONS.Add(PerkType.FASTER_RELOAD, "1.4x reload speed");
      _PERK_DESCRIPTIONS.Add(PerkType.MAX_AMMO_UP, "1.5x clip size");
      _PERK_DESCRIPTIONS.Add(PerkType.AKIMBO, "dual wield big guns");
      _PERK_DESCRIPTIONS.Add(PerkType.LASER_SIGHTS, "guns have laser sights");
      _PERK_DESCRIPTIONS.Add(PerkType.FIRE_RATE_UP, "guns shoot faster");
      _PERK_DESCRIPTIONS.Add(PerkType.NO_SLOWMO, "no slowmo; harder");
      _PERK_DESCRIPTIONS.Add(PerkType.SMART_BULLETS, "strong gun = smart bullet");
    }

    public static bool HasPerk(int playerId, PerkType perk)
    {
      return GameScript.PlayerProfile.s_Profiles[playerId]._equipment._perks.Contains(perk);
    }

    public static List<PerkType> GetPerks(int playerId)
    {
      return GameScript.PlayerProfile.s_Profiles[playerId]._equipment._perks;
    }

    public static int GetNumPerks(int playerId)
    {
      return GameScript.PlayerProfile.s_Profiles[playerId]._equipment._perks.Count;
    }

    public static void BuyPerk(int playerId, PerkType perk)
    {
      if (GameScript._GameMode != GameScript.GameModes.SURVIVAL) return;

      GameScript.PlayerProfile.s_Profiles[playerId]._equipment._perks.Add(perk);
      GameScript.PlayerProfile.s_Profiles[playerId].UpdatePerkIcons();

      switch (perk)
      {
        // Increase max ammo
        case (PerkType.MAX_AMMO_UP):
          foreach (var player in PlayerScript.s_Players)
          {
            if (player._id == playerId)
            {
              player._ragdoll.RefillAmmo();
              player._Profile.UpdateIcons();
              return;
            }
          }
          break;
        // Give laser sights to ranged weapons
        case (PerkType.LASER_SIGHTS):
          foreach (var player in PlayerScript.s_Players)
          {
            if (player._id == playerId)
            {
              player._ragdoll._itemL?.AddLaserSight();
              player._ragdoll._itemR?.AddLaserSight();
              return;
            }
          }
          break;
        // Give two extra health
        case (PerkType.ARMOR_UP):
          foreach (var player in PlayerScript.s_Players)
          {
            if (player._id == playerId)
            {
              player._ragdoll._health += 2;
              player._Profile.UpdateHealthUI();
              if (player._ragdoll._health > 3)
                player._ragdoll.AddArmor();
              return;
            }
          }
          break;
      }
    }

  }
}