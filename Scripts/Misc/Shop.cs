using System;
using System.Collections.Generic;
using UnityEngine;

public static class Shop
{
  //
  static Settings.LevelSaveData LevelModule { get { return Settings.s_SaveData.LevelData; } }

  //
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
      var array = mode == GameScript.GameModes.MISSIONS ? _Tips_Classic : (mode == GameScript.GameModes.ZOMBIE ? _Tips_Survival : _Tips_Versus);
      var mode_string = mode.ToString().ToLower();
      if (UnityEngine.Random.value <= 0.5f || mode == GameScript.GameModes.PARTY)
      {
        array = _Tips_General;
        mode_string = "general";
      }
      // Format tip
      return string.Format("*tip(<color=yellow>{0}</color>): {1}", mode_string, array[UnityEngine.Random.Range(0, array.Length)]);
    }

  }

  public static int _AvailablePoints
  {
    get
    {

#if UNITY_EDITOR
      //      return 999;
#endif

      if (LevelModule.IsTopRatedClassic0 && LevelModule.IsTopRatedClassic1)
        return 999;
      return LevelModule.ShopPoints;
    }
    set
    {
      LevelModule.ShopPoints = value;
    }
  }

  public static int _DisplayMode
  {
    get { return LevelModule.ShopDisplayMode; }
    set
    {
      LevelModule.ShopDisplayMode = value % 3;
    }
  }
  public enum DisplayModes
  {
    AVAILABLE,
    ALL,
    PURCHASED,
  }

  public static int _LoadoutDisplayMode
  {
    get { return LevelModule.ShopLoadoutDisplayMode; }
    set
    {
      LevelModule.ShopLoadoutDisplayMode = value % 2;
    }
  }

  // Max amount of a utility you can have in one side
  public static Dictionary<UtilityScript.UtilityType, int> _Utility_Cap;

  public enum Unlocks
  {
    ITEM_KNIFE,
    ITEM_FRYING_PAN,
    ITEM_PISTOL_SILENCED,
    ITEM_PISTOL_MACHINE,
    ITEM_PISTOL_DOUBLE,
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
    ITEM_KATANA,
    ITEM_AXE,

    ITEM_FLAMETHROWER,
    ITEM_ROCKET_FIST,
    ITEM_STICKY_GUN,
    ITEM_PISTOL_CHARGE,

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
    UTILITY_TEMP_SHIELD,
    UTILITY_DASH,

    UTILITY_GRENADE_STUN,
    UTILITY_TACTICAL_BULLET,
    UTILITY_MORTAR_STRIKE,

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
    MOD_GRAPPLE_MASTER,
    MOD_SPEED_UP,

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
    MAX_EQUIPMENT_POINTS_10,

    LOADOUT_SLOT_X2_0,
    LOADOUT_SLOT_X2_1,
    LOADOUT_SLOT_X2_2,
    LOADOUT_SLOT_X2_3,
    LOADOUT_SLOT_X2_4,
    LOADOUT_SLOT_X2_5,

    MODE_ZOMBIE,
    MODE_EXTRAS,

    EXTRA_TIME,
    EXTRA_GRAVITY,
    EXTRA_HORDE,
    EXTRA_CHASE,
    EXTRA_PLAYER_AMMO,
    EXTRA_ENEMY_OFF,
    EXTRA_BLOOD_FX,
    EXTRA_EXPLODED,
    EXTRA_CROWNMODE,

    TUTORIAL_PART0,
    TUTORIAL_PART1,

    ITEM_RAPIER,
    UTILITY_MOLOTOV,
    ITEM_RIFLE_CHARGE,
    MOD_EXPLOSIVE_PARRY,

    // Cannot re-order unlocks
    UTILITY_MIRROR,
    MOD_MARTIAL_ARTIST,
    MOD_THRUST,
    MOD_TWIN,
    ITEM_STUN_BATON,
    UTILITY_BEAR_TRAP,
  }

  public static int _Max_Equipment_Points
  {
    get { return s_ShopEquipmentPoints; }
    set
    {
      s_ShopEquipmentPoints = value;
    }
  }

  public static Dictionary<Unlocks, Tuple<string, int>> _Unlocks_Descriptions;
  static Dictionary<string, Unlocks[]> _Unlocks_Vault;
  public static Unlocks[] _Unlocks_Ignore_Shop;

  static List<GameScript.ItemManager.Items> _TwoHanded_Dictionary, _ActualTwoHanded_Dictionary;

  public static void Init()
  {
    Perk.Init();

    _Unlocks_Descriptions = new Dictionary<Unlocks, Tuple<string, int>>
    {
      { Unlocks.ITEM_KNIFE, new Tuple<string, int>("melee, fast", 3) },
      { Unlocks.ITEM_STUN_BATON, new Tuple<string, int>("melee, stuns", 5) },
      { Unlocks.ITEM_FRYING_PAN, new Tuple<string, int>("melee, fast, block", 10) },
      { Unlocks.ITEM_AXE, new Tuple<string, int>("melee, slower, wide-sweep", 10) },
      //_Unlocks_Descriptions.Add(Unlocks.ITEM_BAT, new Tuple<string, int>("melee, two-handed, wide-sweep", 10));
      { Unlocks.ITEM_RAPIER, new Tuple<string, int>("melee, one-handed, lunge", 15) },
      { Unlocks.ITEM_KATANA, new Tuple<string, int>("melee, two-handed, wide-sweep", 20) },

      { Unlocks.ITEM_PISTOL_SILENCED, new Tuple<string, int>("handgun, silenced, fast-reload", 15) },
      { Unlocks.ITEM_PISTOL_MACHINE, new Tuple<string, int>("handgun, 3-burst, fast-reload", 10) },
      { Unlocks.ITEM_PISTOL_DOUBLE, new Tuple<string, int>("handgun, double-barrel", 10) },
      { Unlocks.ITEM_PISTOL_CHARGE, new Tuple<string, int>("handgun, silenced, charged", 10) },
      { Unlocks.ITEM_REVOLVER, new Tuple<string, int>("handgun, powerful, slower-reload", 20) },
      { Unlocks.ITEM_UZI, new Tuple<string, int>("gun, automatic, small-magazine", 15) },
      { Unlocks.ITEM_CROSSBOW, new Tuple<string, int>("bow, powerful, slow-reload", 15) },
      { Unlocks.ITEM_SHOTGUN_PUMP, new Tuple<string, int>("shotgun, silenced, reload", 15) },
      { Unlocks.ITEM_SHOTGUN_DOUBLE, new Tuple<string, int>("shotgun, powerful, reload", 15) },
      { Unlocks.ITEM_SHOTGUN_BURST, new Tuple<string, int>("shotgun, two-burst, reload", 25) },
      { Unlocks.ITEM_AK47, new Tuple<string, int>("rifle, automatic, slow-reload", 25) },
      { Unlocks.ITEM_M16, new Tuple<string, int>("rifle, burst, slow-reload", 15) },
      { Unlocks.ITEM_RIFLE, new Tuple<string, int>("rifle, semi-automatic, slow-fire", 10) },
      { Unlocks.ITEM_RIFLE_LEVER, new Tuple<string, int>("rifle, semi-automatic, fast-fire", 20) },
      { Unlocks.ITEM_RIFLE_CHARGE, new Tuple<string, int>("rifle, semi/automatic, charged", 15) },
      { Unlocks.ITEM_DMR, new Tuple<string, int>("rifle, semi-automatic, slow-reload", 22) },
      { Unlocks.ITEM_SNIPER, new Tuple<string, int>("bolt-action, semi-automatic, powerful", 20) },
      { Unlocks.ITEM_GRENADE_LAUNCHER, new Tuple<string, int>("explosive, semi-automatic, slow-reload", 15) },
      { Unlocks.ITEM_STICKY_GUN, new Tuple<string, int>("stealthy, chain, slow-reload", 15) },

      { Unlocks.ITEM_FLAMETHROWER, new Tuple<string, int>("charge-shot, incendiary, slow-reload", 20) },

      //_Unlocks_Descriptions.Add(Unlocks.ITEM_ROCKET_FIST, new Tuple<string, int>("charge-shot, melee, slow-reload", 0));

      { Unlocks.UTILITY_SHURIKEN, new Tuple<string, int>("throwable, pick-up, small", 3) },
      { Unlocks.UTILITY_SHURIKEN_BIG, new Tuple<string, int>("throwable, pick-up, large", 20) },
      { Unlocks.UTILITY_KUNAI_EXPLOSIVE, new Tuple<string, int>("throwable, explodes, small", 15) },
      { Unlocks.UTILITY_KUNAI_STICKY, new Tuple<string, int>("throwable, delalyed-explosion, small", 15) },
      { Unlocks.UTILITY_TACTICAL_BULLET, new Tuple<string, int>("throwable, stun", 10) },
      { Unlocks.UTILITY_MIRROR, new Tuple<string, int>("throwable, reflect", 10) },
      { Unlocks.UTILITY_GRENADE, new Tuple<string, int>("throwable, explosive, large-radius", 10) },
      { Unlocks.UTILITY_GRENADE_IMPACT, new Tuple<string, int>("throwable, contact-explosive", 15) },
      { Unlocks.UTILITY_GRENADE_STUN, new Tuple<string, int>("throwable, corner-killer", 10) },
      { Unlocks.UTILITY_C4, new Tuple<string, int>("throwable, explosive, remote-controlled", 10) },
      { Unlocks.UTILITY_BEAR_TRAP, new Tuple<string, int>("throwable, trap", 5) },
      { Unlocks.UTILITY_MOLOTOV, new Tuple<string, int>("throwable, fire, duration", 10) },
      { Unlocks.UTILITY_MORTAR_STRIKE, new Tuple<string, int>("ranged, explosive, remote-controlled", 15) },
      { Unlocks.UTILITY_STOP_WATCH, new Tuple<string, int>("useable, slows-time", 10) },
      { Unlocks.UTILITY_INVISIBILITY, new Tuple<string, int>("useable, short-invisibility", 10) },
      { Unlocks.UTILITY_TEMP_SHIELD, new Tuple<string, int>("useable, shield, requires-melee", 15) },
      //_Unlocks_Descriptions.Add(Unlocks.UTILITY_DASH, new Tuple<string, int>("useable, quick speed boost", 0));

      { Unlocks.MOD_LASER_SIGHTS, new Tuple<string, int>("-", 5) },
      { Unlocks.MOD_NO_SLOWMO, new Tuple<string, int>("-", 1) },
      { Unlocks.MOD_FASTER_RELOAD, new Tuple<string, int>("-", 15) },
      { Unlocks.MOD_MAX_AMMO_UP, new Tuple<string, int>("-", 15) },
      { Unlocks.MOD_EXPLOSION_RESISTANCE, new Tuple<string, int>("-", 10) },
      { Unlocks.MOD_EXPLOSIONS_UP, new Tuple<string, int>("-", 20) },
      { Unlocks.MOD_ARMOR_UP, new Tuple<string, int>("-", 0) },
      { Unlocks.MOD_PENETRATION_UP, new Tuple<string, int>("-", 0) },
      { Unlocks.MOD_SPEED_UP, new Tuple<string, int>("-", 10) },
      { Unlocks.MOD_SMART_BULLETS, new Tuple<string, int>("-", 20) },
      { Unlocks.MOD_THRUST, new Tuple<string, int>("-", 10) },
      { Unlocks.MOD_GRAPPLE_MASTER, new Tuple<string, int>("-", 5) },
      { Unlocks.MOD_MARTIAL_ARTIST, new Tuple<string, int>("-", 5) },
      { Unlocks.MOD_EXPLOSIVE_PARRY, new Tuple<string, int>("-", 10) },
      { Unlocks.MOD_TWIN, new Tuple<string, int>("-", 10) },

      { Unlocks.MAX_EQUIPMENT_POINTS_0, new Tuple<string, int>("equipment points (+1)", 5) },
      { Unlocks.MAX_EQUIPMENT_POINTS_1, new Tuple<string, int>("equipment points (+1)", 5) },
      { Unlocks.MAX_EQUIPMENT_POINTS_2, new Tuple<string, int>("equipment points (+1)", 10) },
      { Unlocks.MAX_EQUIPMENT_POINTS_3, new Tuple<string, int>("equipment points (+1)", 10) },
      { Unlocks.MAX_EQUIPMENT_POINTS_4, new Tuple<string, int>("equipment points (+1)", 15) },
      { Unlocks.MAX_EQUIPMENT_POINTS_5, new Tuple<string, int>("equipment points (+1)", 20) },
      { Unlocks.MAX_EQUIPMENT_POINTS_6, new Tuple<string, int>("equipment points (+1)", 25) },
      { Unlocks.MAX_EQUIPMENT_POINTS_7, new Tuple<string, int>("equipment points (+1)", 30) },
      { Unlocks.MAX_EQUIPMENT_POINTS_8, new Tuple<string, int>("equipment points (+1)", 40) },
      { Unlocks.MAX_EQUIPMENT_POINTS_9, new Tuple<string, int>("equipment points (+1)", 40) },
      { Unlocks.MAX_EQUIPMENT_POINTS_10, new Tuple<string, int>("equipment points (+1)", 45) },

      { Unlocks.LOADOUT_SLOT_X2_0, new Tuple<string, int>("loadout slot (+2)", 3) },
      { Unlocks.LOADOUT_SLOT_X2_1, new Tuple<string, int>("loadout slot (+2)", 8) },
      { Unlocks.LOADOUT_SLOT_X2_2, new Tuple<string, int>("loadout slot (+2)", 15) },
      { Unlocks.LOADOUT_SLOT_X2_3, new Tuple<string, int>("loadout slot (+2)", 20) },
      { Unlocks.LOADOUT_SLOT_X2_4, new Tuple<string, int>("loadout slot (+2)", 25) },
      { Unlocks.LOADOUT_SLOT_X2_5, new Tuple<string, int>("loadout slot (+2)", 30) },

      { Unlocks.MODE_ZOMBIE, new Tuple<string, int>("unlocks 'zombie' mode", 0) },
      { Unlocks.MODE_EXTRAS, new Tuple<string, int>("unlocks 'extras' menu", 0) },

      { Unlocks.EXTRA_GRAVITY, new Tuple<string, int>("unlocks 'gravity' extra", 0) },
      { Unlocks.EXTRA_PLAYER_AMMO, new Tuple<string, int>("unlocks 'player ammo' extra", 0) },
      { Unlocks.EXTRA_ENEMY_OFF, new Tuple<string, int>("unlocks 'enemy off' extra", 0) },
      { Unlocks.EXTRA_CHASE, new Tuple<string, int>("unlocks 'chaser' extra", 0) },
      { Unlocks.EXTRA_TIME, new Tuple<string, int>("unlocks 'time' extra", 0) },
      { Unlocks.EXTRA_HORDE, new Tuple<string, int>("unlocks 'horde' extra", 0) },
      { Unlocks.EXTRA_BLOOD_FX, new Tuple<string, int>("unlocks 'blood fx' extra", 0) },
      { Unlocks.EXTRA_EXPLODED, new Tuple<string, int>("unlocks 'explode death' extra", 0) },
      { Unlocks.EXTRA_CROWNMODE, new Tuple<string, int>("unlocks 'crown' extra", 0) },

      { Unlocks.TUTORIAL_PART0, new Tuple<string, int>("", 0) },
      { Unlocks.TUTORIAL_PART1, new Tuple<string, int>("", 0) }
    };

    // Add unlocks to ignore in classic shop
    _Unlocks_Ignore_Shop = new Unlocks[]{
      Unlocks.TUTORIAL_PART0,
      Unlocks.TUTORIAL_PART1,

      Unlocks.UTILITY_MOLOTOV,

      Unlocks.MOD_ARMOR_UP,
      Unlocks.MOD_PENETRATION_UP,

      Unlocks.EXTRA_CROWNMODE,
    };

    // Load available / purchased unlocks
    var shopPointsTotaled = 0;
    foreach (var pair in _Unlocks_Descriptions)
      shopPointsTotaled += pair.Value.Item2;
#if UNITY_EDITOR
    Debug.Log($"Total points: ({/*_AvailablePoints*/-1}) {shopPointsTotaled} / {((1 * 11 * 2) + (10 * 12 * 2)) * 4}");
#endif

    // Add unlocks to vault
    _Unlocks_Vault = new Dictionary<string, Unlocks[]>();

    if (GameScript.s_Singleton._IsDemo)
    {
      _Unlocks_Vault.Add("classic_0", new Unlocks[] { Unlocks.ITEM_AXE, Unlocks.UTILITY_GRENADE });
      _Unlocks_Vault.Add("classic_1", new Unlocks[] { Unlocks.MOD_LASER_SIGHTS, Unlocks.UTILITY_KUNAI_EXPLOSIVE, Unlocks.MAX_EQUIPMENT_POINTS_2 });
      _Unlocks_Vault.Add("classic_2", new Unlocks[] { Unlocks.ITEM_RIFLE, Unlocks.ITEM_PISTOL_MACHINE });
    }
    else
    {
      _Unlocks_Vault.Add("classic_0", new Unlocks[] { Unlocks.MOD_MARTIAL_ARTIST, Unlocks.ITEM_AXE, Unlocks.UTILITY_GRENADE, Unlocks.MAX_EQUIPMENT_POINTS_2, Unlocks.LOADOUT_SLOT_X2_0 });
      _Unlocks_Vault.Add("classic_1", new Unlocks[] { Unlocks.MOD_TWIN, Unlocks.MOD_LASER_SIGHTS, Unlocks.MOD_NO_SLOWMO, Unlocks.UTILITY_KUNAI_EXPLOSIVE, Unlocks.ITEM_PISTOL_CHARGE });
      _Unlocks_Vault.Add("classic_2", new Unlocks[] { Unlocks.MODE_ZOMBIE, Unlocks.ITEM_STUN_BATON, Unlocks.ITEM_RIFLE, Unlocks.ITEM_PISTOL_MACHINE, Unlocks.UTILITY_MIRROR, Unlocks.MAX_EQUIPMENT_POINTS_3, Unlocks.LOADOUT_SLOT_X2_1 });
      _Unlocks_Vault.Add("classic_3", new Unlocks[] { Unlocks.ITEM_PISTOL_DOUBLE, Unlocks.UTILITY_STOP_WATCH, Unlocks.UTILITY_TEMP_SHIELD, Unlocks.MOD_SPEED_UP });
      _Unlocks_Vault.Add("classic_4", new Unlocks[] { Unlocks.ITEM_REVOLVER, Unlocks.UTILITY_C4, Unlocks.UTILITY_BEAR_TRAP, Unlocks.UTILITY_GRENADE_STUN, Unlocks.UTILITY_SHURIKEN_BIG });
      _Unlocks_Vault.Add("classic_5", new Unlocks[] { Unlocks.ITEM_RAPIER, Unlocks.ITEM_STICKY_GUN, Unlocks.UTILITY_GRENADE_IMPACT });
      _Unlocks_Vault.Add("classic_6", new Unlocks[] { Unlocks.ITEM_FRYING_PAN, Unlocks.ITEM_CROSSBOW, Unlocks.ITEM_GRENADE_LAUNCHER, Unlocks.UTILITY_TACTICAL_BULLET, Unlocks.MAX_EQUIPMENT_POINTS_4 });
      _Unlocks_Vault.Add("classic_7", new Unlocks[] { Unlocks.MOD_THRUST, Unlocks.UTILITY_KUNAI_STICKY, Unlocks.UTILITY_INVISIBILITY, Unlocks.LOADOUT_SLOT_X2_2 });
      _Unlocks_Vault.Add("classic_8", new Unlocks[] { Unlocks.ITEM_UZI, Unlocks.ITEM_SHOTGUN_PUMP, Unlocks.MOD_GRAPPLE_MASTER });
      _Unlocks_Vault.Add("classic_9", new Unlocks[] { Unlocks.ITEM_RIFLE_LEVER, Unlocks.MOD_EXPLOSIVE_PARRY, Unlocks.LOADOUT_SLOT_X2_3, Unlocks.MAX_EQUIPMENT_POINTS_5 });
      _Unlocks_Vault.Add("classic_10", new Unlocks[] { Unlocks.ITEM_RIFLE_CHARGE, Unlocks.UTILITY_MORTAR_STRIKE, Unlocks.MOD_EXPLOSIONS_UP });

      _Unlocks_Vault.Add("classic_11", new Unlocks[] { Unlocks.MODE_EXTRAS, Unlocks.EXTRA_CHASE, Unlocks.ITEM_SNIPER, Unlocks.LOADOUT_SLOT_X2_4 });
      _Unlocks_Vault.Add("classic_12", new Unlocks[] { Unlocks.ITEM_KATANA, Unlocks.ITEM_DMR, Unlocks.ITEM_SHOTGUN_DOUBLE, });
      _Unlocks_Vault.Add("classic_13", new Unlocks[] { Unlocks.MOD_EXPLOSION_RESISTANCE, Unlocks.MAX_EQUIPMENT_POINTS_6 });
      _Unlocks_Vault.Add("classic_14", new Unlocks[] { Unlocks.LOADOUT_SLOT_X2_5, });
      _Unlocks_Vault.Add("classic_15", new Unlocks[] { Unlocks.ITEM_M16 });
      _Unlocks_Vault.Add("classic_16", new Unlocks[] { Unlocks.MOD_FASTER_RELOAD });
      _Unlocks_Vault.Add("classic_17", new Unlocks[] { Unlocks.ITEM_FLAMETHROWER, Unlocks.MOD_SMART_BULLETS, Unlocks.MAX_EQUIPMENT_POINTS_7, });
      _Unlocks_Vault.Add("classic_18", new Unlocks[] { Unlocks.ITEM_AK47, });
      _Unlocks_Vault.Add("classic_19", new Unlocks[] { Unlocks.ITEM_SHOTGUN_BURST, Unlocks.MOD_MAX_AMMO_UP, Unlocks.MAX_EQUIPMENT_POINTS_8, });
      _Unlocks_Vault.Add("classic_20", new Unlocks[] { Unlocks.MAX_EQUIPMENT_POINTS_9, Unlocks.MAX_EQUIPMENT_POINTS_10 });
    }

    // Create utility cap
    _Utility_Cap = new Dictionary<UtilityScript.UtilityType, int>
    {
      { UtilityScript.UtilityType.GRENADE, 6 },
      { UtilityScript.UtilityType.GRENADE_IMPACT, 6 },
      { UtilityScript.UtilityType.C4, 6 },
      { UtilityScript.UtilityType.SHURIKEN, 6 },
      { UtilityScript.UtilityType.SHURIKEN_BIG, 6 },
      { UtilityScript.UtilityType.KUNAI_EXPLOSIVE, 6 },
      { UtilityScript.UtilityType.KUNAI_STICKY, 6 },
      { UtilityScript.UtilityType.STOP_WATCH, 6 },
      { UtilityScript.UtilityType.TEMP_SHIELD, 6 },
      { UtilityScript.UtilityType.INVISIBILITY, 6 },
      { UtilityScript.UtilityType.DASH, 6 },
      { UtilityScript.UtilityType.STICKY_GUN_BULLET, 50 },
      { UtilityScript.UtilityType.MORTAR_STRIKE, 6 },
      { UtilityScript.UtilityType.TACTICAL_BULLET, 6 },
      { UtilityScript.UtilityType.MIRROR, 6 },
      { UtilityScript.UtilityType.MOLOTOV, 6 },
    };

    //
    _TwoHanded_Dictionary = new List<GameScript.ItemManager.Items>
    {
      GameScript.ItemManager.Items.KATANA,
      GameScript.ItemManager.Items.BAT,
      GameScript.ItemManager.Items.DMR,
      GameScript.ItemManager.Items.RIFLE,
      GameScript.ItemManager.Items.RIFLE_LEVER,
      GameScript.ItemManager.Items.CROSSBOW,
      GameScript.ItemManager.Items.SNIPER,
      GameScript.ItemManager.Items.AK47,
      GameScript.ItemManager.Items.M16,
      GameScript.ItemManager.Items.SHOTGUN_BURST,
      GameScript.ItemManager.Items.SHOTGUN_PUMP,
      GameScript.ItemManager.Items.SHOTGUN_DOUBLE,
      GameScript.ItemManager.Items.GRENADE_LAUNCHER,
      GameScript.ItemManager.Items.FLAMETHROWER
    };

    _ActualTwoHanded_Dictionary = new List<GameScript.ItemManager.Items>
    {
      GameScript.ItemManager.Items.KATANA,
      GameScript.ItemManager.Items.BAT
    };
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
    var list = new List<Perk.PerkType>
    {
      Perk.PerkType.NONE
    };
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

  // Check available / unlocked
  public static bool UnlockLocked(Unlocks unlock)
  {
    return LevelModule.ShopUnlocksOrdered.ContainsKey(unlock) && LevelModule.ShopUnlocksOrdered[unlock].UnlockValue == Settings.LevelSaveData.ShopUnlock.UnlockValueType.LOCKED;
  }
  public static bool UnlockAvailable(Unlocks unlock)
  {
    return LevelModule.ShopUnlocksOrdered.ContainsKey(unlock) && LevelModule.ShopUnlocksOrdered[unlock].UnlockValue != Settings.LevelSaveData.ShopUnlock.UnlockValueType.LOCKED;
  }
  public static bool UnlockUnlocked(Unlocks unlock)
  {
    return LevelModule.ShopUnlocksOrdered.ContainsKey(unlock) && LevelModule.ShopUnlocksOrdered[unlock].UnlockValue == Settings.LevelSaveData.ShopUnlock.UnlockValueType.UNLOCKED;
  }


  // Append unlock
  public static void AddAvailableUnlock(Unlocks unlock, bool alert = false)
  {
    if (!UnlockLocked(unlock)) return;

    var unlockDat = LevelModule.ShopUnlocksOrdered[unlock];
    unlockDat.UnlockValue = Settings.LevelSaveData.ShopUnlock.UnlockValueType.AVAILABLE;
    LevelModule.ShopUnlocksOrdered[unlock] = unlockDat;
    LevelModule.SyncShopUnlocks();

    // Auto unlock modes
    switch (unlock)
    {
      case Unlocks.MODE_ZOMBIE:
      case Unlocks.MODE_EXTRAS:

      case Unlocks.EXTRA_CHASE:
        Unlock(unlock);
        break;
    }

    // Check notify player
    if (alert)
    {
      if (unlock == Unlocks.MODE_ZOMBIE)
        s_UnlockString += $"- new mode unlocked: <color=red>{unlock}</color>\n";
      else if (unlock == Unlocks.MODE_EXTRAS)
        s_UnlockString += $"- pause menu option unlocked: <color=magenta>EXTRAS</color>\n";
      else if (unlock.ToString().StartsWith("EXTRA_"))
        s_UnlockString += $"- new extra unlocked: <color=magenta>{unlock}</color>\n";
      else
        s_UnlockString += $"- new unlock added to shop: <color=yellow>{unlock}</color>\n";
    }
  }

  //
  public static bool AllExtrasUnlocked()
  {
    return
    UnlockUnlocked(Unlocks.EXTRA_BLOOD_FX) &&
    UnlockUnlocked(Unlocks.EXTRA_CHASE) &&
    UnlockUnlocked(Unlocks.EXTRA_ENEMY_OFF) &&
    UnlockUnlocked(Unlocks.EXTRA_EXPLODED) &&
    UnlockUnlocked(Unlocks.EXTRA_GRAVITY) &&
    UnlockUnlocked(Unlocks.EXTRA_HORDE) &&
    UnlockUnlocked(Unlocks.EXTRA_PLAYER_AMMO) &&
    UnlockUnlocked(Unlocks.EXTRA_TIME);
  }
  public static bool AnyExtrasUnlocked()
  {
    return
    UnlockUnlocked(Unlocks.EXTRA_BLOOD_FX) ||
    UnlockUnlocked(Unlocks.EXTRA_CHASE) ||
    UnlockUnlocked(Unlocks.EXTRA_ENEMY_OFF) ||
    UnlockUnlocked(Unlocks.EXTRA_EXPLODED) ||
    UnlockUnlocked(Unlocks.EXTRA_GRAVITY) ||
    UnlockUnlocked(Unlocks.EXTRA_HORDE) ||
    UnlockUnlocked(Unlocks.EXTRA_PLAYER_AMMO) ||
    UnlockUnlocked(Unlocks.EXTRA_TIME);
  }

  public static string s_UnlockString
  {
    get { return LevelModule.ShopUnlockString; }
    set
    {
      LevelModule.ShopUnlockString = value;
    }
  }

  public static void ShowUnlocks(Menu.MenuType toMenu)
  {
    Menu.GenericMenu(new string[] { "new unlocks\n\n", $"{s_UnlockString}" }, UnityEngine.Random.value < 0.5f ? "wow\n\n" : "nice\n\n", toMenu);
    s_UnlockString = "";
  }

  // Unlock from available unlocks
  public static void Unlock(Unlocks unlock)
  {
    if (UnlockUnlocked(unlock)) return;
    if (!_Unlocks_Descriptions.ContainsKey(unlock)) throw new Exception($"No description for unlock: {unlock}");

    var unlockDat = LevelModule.ShopUnlocksOrdered[unlock];
    unlockDat.UnlockValue = Settings.LevelSaveData.ShopUnlock.UnlockValueType.UNLOCKED;
    LevelModule.ShopUnlocksOrdered[unlock] = unlockDat;
    LevelModule.SyncShopUnlocks();

    switch (unlock)
    {

      // Add equipment points
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
      case Unlocks.MAX_EQUIPMENT_POINTS_10:
        _Max_Equipment_Points++;
        break;

      // Add loadout slots
      case Unlocks.LOADOUT_SLOT_X2_0:
      case Unlocks.LOADOUT_SLOT_X2_1:
      case Unlocks.LOADOUT_SLOT_X2_2:
      case Unlocks.LOADOUT_SLOT_X2_3:
      case Unlocks.LOADOUT_SLOT_X2_4:
      case Unlocks.LOADOUT_SLOT_X2_5:
        s_ShopLoadoutCount += 2;
        GameScript.ItemManager.Loadout.Init();
        break;
    }
  }

  public static int s_ShopLoadoutCount, s_ShopEquipmentPoints;

  public static int GetUtilityCount(UtilityScript.UtilityType utility)
  {
    var count = 1;
    switch (utility)
    {
      case UtilityScript.UtilityType.SHURIKEN:
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
    return UnlockUnlocked(unlock);
  }

  public static class Perk
  {

    public enum PerkType
    {
      PENETRATION_UP,
      ARMOR_UP,
      EXPLOSION_RESISTANCE,
      EXPLOSIONS_UP,
      FASTER_RELOAD,
      MAX_AMMO_UP,
      FIRE_RATE_UP,
      LASER_SIGHTS,
      AKIMBO,
      NO_SLOWMO,
      SMART_BULLETS,
      GRAPPLE_MASTER,
      SPEED_UP,
      EXPLOSIVE_PARRY,
      MARTIAL_ARTIST,
      THRUST,

      TWIN,

      NONE
    }

    public static Dictionary<PerkType, string> _PERK_DESCRIPTIONS;

    public static void Init()
    {
      _PERK_DESCRIPTIONS = new Dictionary<PerkType, string>
      {
        { PerkType.PENETRATION_UP, "bullets penetrate deeper" },
        { PerkType.ARMOR_UP, "gain 2 extra health" },
        { PerkType.EXPLOSION_RESISTANCE, "survive self-explosions" },
        { PerkType.EXPLOSIONS_UP, "1.25x explosion radius" },
        { PerkType.FASTER_RELOAD, "1.4x reload speed" },
        { PerkType.MAX_AMMO_UP, "1.5x clip size" },
        { PerkType.AKIMBO, "dual wield big guns" },
        { PerkType.LASER_SIGHTS, "guns have laser sights" },
        { PerkType.FIRE_RATE_UP, "guns shoot faster" },
        { PerkType.NO_SLOWMO, "no slowmo; harder" },
        { PerkType.SMART_BULLETS, "strong gun = smart bullet" },
        { PerkType.THRUST, "extra melee range" },
        { PerkType.GRAPPLE_MASTER, "grapple armor" },
        { PerkType.MARTIAL_ARTIST, "empty hands are fists" },
        { PerkType.SPEED_UP, "1.15x movement speed" },
        { PerkType.EXPLOSIVE_PARRY, "parried bullets explode" },
        { PerkType.TWIN, "summon linked twin" },
      };
    }

    public static bool HasPerk(int playerId, PerkType perk)
    {
      return GameScript.PlayerProfile.s_Profiles[playerId]._Equipment._Perks.Contains(perk);
    }

    public static List<PerkType> GetPerks(int playerId)
    {
      return GameScript.PlayerProfile.s_Profiles[playerId]._Equipment._Perks;
    }

    public static int GetNumPerks(int playerId)
    {
      return GameScript.PlayerProfile.s_Profiles[playerId]._Equipment._Perks.Count;
    }

    public static void BuyPerk(int playerId, PerkType perk)
    {
      if (GameScript.s_GameMode != GameScript.GameModes.ZOMBIE) return;

      GameScript.PlayerProfile.s_Profiles[playerId]._Equipment._Perks.Add(perk);
      GameScript.PlayerProfile.s_Profiles[playerId].UpdatePerkIcons();

      switch (perk)
      {
        // Increase max ammo
        case (PerkType.MAX_AMMO_UP):
          foreach (var player in PlayerScript.s_Players)
          {
            if (player._Id == playerId)
            {
              player._Ragdoll.RefillAmmo();
              player._Profile.UpdateIcons();
              return;
            }
          }
          break;
        // Give laser sights to ranged weapons
        case (PerkType.LASER_SIGHTS):
          foreach (var player in PlayerScript.s_Players)
          {
            if (player._Id == playerId)
            {
              player._Ragdoll._ItemL?.AddLaserSight();
              player._Ragdoll._ItemR?.AddLaserSight();
              return;
            }
          }
          break;
        // Give two extra health
        case (PerkType.ARMOR_UP):
          foreach (var player in PlayerScript.s_Players)
          {
            if (player._Id == playerId)
            {
              player._Ragdoll._health += 2;
              player._Profile.UpdateHealthUI();
              if (player._Ragdoll._health > 3)
                player._Ragdoll.AddArmor();
              return;
            }
          }
          break;
      }
    }

  }
}