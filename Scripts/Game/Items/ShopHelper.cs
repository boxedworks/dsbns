using System;
using System.Collections.Generic;
using Assets.Scripts.Ragdoll.Equippables;
using Assets.Scripts.Settings;
using Assets.Scripts.Settings.Localization;
using Assets.Scripts.Settings.Serialization;
using Assets.Scripts.UI.Menus;
using UnityEngine;

namespace Assets.Scripts.Game.Items
{
  public static class ShopHelper
  {
    //
    static LevelSaveData LevelModule { get { return SettingsHelper.s_SaveData.LevelData; } }

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
      UTILITY_MINE,
      UTILITY_COIN,
      MOD_BULLET_DESTROYER,
    }
    public static string GetUnlockStringLocalized(Unlocks unlock)
    {
      return unlock.ToString();
      //return LocalizationController.GetString($"unlocks.{unlock}");
    }
    public static string GetUnlockDescriptionStringLocalized(Unlocks unlock)
    {
      // Parse english desc, sep by comma and localize
      throw new NotImplementedException();
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

    static List<ItemManager.Items> _TwoHanded_Dictionary, _ActualTwoHanded_Dictionary;

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
      { Unlocks.UTILITY_SHURIKEN_BIG, new Tuple<string, int>("throwable, pick-up, large", 15) },
      { Unlocks.UTILITY_KUNAI_EXPLOSIVE, new Tuple<string, int>("throwable, explodes, small", 15) },
      { Unlocks.UTILITY_KUNAI_STICKY, new Tuple<string, int>("throwable, delalyed-explosion, small", 15) },
      { Unlocks.UTILITY_TACTICAL_BULLET, new Tuple<string, int>("throwable, stun", 10) },
      { Unlocks.UTILITY_COIN, new Tuple<string, int>("throwable, reflect", 10) },
      { Unlocks.UTILITY_MIRROR, new Tuple<string, int>("throwable, reflect", 10) },
      { Unlocks.UTILITY_GRENADE, new Tuple<string, int>("throwable, explosive, large-radius", 10) },
      { Unlocks.UTILITY_GRENADE_IMPACT, new Tuple<string, int>("throwable, contact-explosive", 15) },
      { Unlocks.UTILITY_GRENADE_STUN, new Tuple<string, int>("throwable, corner-killer", 10) },
      { Unlocks.UTILITY_C4, new Tuple<string, int>("throwable, explosive, remote-controlled", 10) },
      { Unlocks.UTILITY_MINE, new Tuple<string, int>("throwable, trap", 5) },
      { Unlocks.UTILITY_BEAR_TRAP, new Tuple<string, int>("throwable, trap", 5) },
      //{ Unlocks.UTILITY_MOLOTOV, new Tuple<string, int>("throwable, fire, duration", 10) },
      { Unlocks.UTILITY_MORTAR_STRIKE, new Tuple<string, int>("ranged, explosive, remote-controlled", 10) },
      { Unlocks.UTILITY_STOP_WATCH, new Tuple<string, int>("useable, slows-time", 10) },
      { Unlocks.UTILITY_INVISIBILITY, new Tuple<string, int>("useable, short-invisibility", 10) },
      { Unlocks.UTILITY_TEMP_SHIELD, new Tuple<string, int>("useable, shield, requires-melee", 10) },
      //_Unlocks_Descriptions.Add(Unlocks.UTILITY_DASH, new Tuple<string, int>("useable, quick speed boost", 0));

      { Unlocks.MOD_LASER_SIGHTS, new Tuple<string, int>("-", 5) },
      { Unlocks.MOD_NO_SLOWMO, new Tuple<string, int>("-", 1) },
      { Unlocks.MOD_FASTER_RELOAD, new Tuple<string, int>("-", 15) },
      { Unlocks.MOD_MAX_AMMO_UP, new Tuple<string, int>("-", 15) },
      { Unlocks.MOD_EXPLOSION_RESISTANCE, new Tuple<string, int>("-", 10) },
      { Unlocks.MOD_EXPLOSIONS_UP, new Tuple<string, int>("-", 10) },
      { Unlocks.MOD_ARMOR_UP, new Tuple<string, int>("-", 0) },
      { Unlocks.MOD_PENETRATION_UP, new Tuple<string, int>("-", 0) },
      { Unlocks.MOD_SPEED_UP, new Tuple<string, int>("-", 10) },
      { Unlocks.MOD_SMART_BULLETS, new Tuple<string, int>("-", 20) },
      { Unlocks.MOD_THRUST, new Tuple<string, int>("-", 10) },
      { Unlocks.MOD_GRAPPLE_MASTER, new Tuple<string, int>("-", 5) },
      { Unlocks.MOD_MARTIAL_ARTIST, new Tuple<string, int>("-", 5) },
      { Unlocks.MOD_EXPLOSIVE_PARRY, new Tuple<string, int>("-", 10) },
      { Unlocks.MOD_TWIN, new Tuple<string, int>("-", 10) },
      { Unlocks.MOD_BULLET_DESTROYER, new Tuple<string, int>("-", 10) },

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

      { Unlocks.LOADOUT_SLOT_X2_0, new Tuple<string, int>("loadout slot (+2)", 4) },
      { Unlocks.LOADOUT_SLOT_X2_1, new Tuple<string, int>("loadout slot (+2)", 10) },
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

      Unlocks.EXTRA_CHASE,
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
        _Unlocks_Vault.Add("classic_1", new Unlocks[] { Unlocks.MOD_TWIN, Unlocks.MOD_LASER_SIGHTS, Unlocks.MOD_NO_SLOWMO, Unlocks.ITEM_PISTOL_CHARGE, Unlocks.UTILITY_COIN, Unlocks.UTILITY_KUNAI_EXPLOSIVE });
        _Unlocks_Vault.Add("classic_2", new Unlocks[] { Unlocks.MODE_ZOMBIE, Unlocks.ITEM_STUN_BATON, Unlocks.ITEM_RIFLE, Unlocks.ITEM_PISTOL_MACHINE, Unlocks.UTILITY_BEAR_TRAP, Unlocks.UTILITY_MIRROR, Unlocks.MAX_EQUIPMENT_POINTS_3, Unlocks.LOADOUT_SLOT_X2_1 });
        _Unlocks_Vault.Add("classic_3", new Unlocks[] { Unlocks.ITEM_PISTOL_DOUBLE, Unlocks.UTILITY_MINE, Unlocks.UTILITY_STOP_WATCH, Unlocks.UTILITY_TEMP_SHIELD, Unlocks.MOD_SPEED_UP });
        _Unlocks_Vault.Add("classic_4", new Unlocks[] { Unlocks.MOD_BULLET_DESTROYER, Unlocks.ITEM_REVOLVER, Unlocks.UTILITY_C4, Unlocks.UTILITY_GRENADE_STUN, Unlocks.UTILITY_SHURIKEN_BIG });
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
      { UtilityScript.UtilityType.COIN, 6 },
    };

      //
      _TwoHanded_Dictionary = new List<ItemManager.Items>
    {
      ItemManager.Items.KATANA,
      ItemManager.Items.BAT,
      ItemManager.Items.DMR,
      ItemManager.Items.RIFLE,
      ItemManager.Items.RIFLE_LEVER,
      ItemManager.Items.CROSSBOW,
      ItemManager.Items.SNIPER,
      ItemManager.Items.AK47,
      ItemManager.Items.M16,
      ItemManager.Items.SHOTGUN_BURST,
      ItemManager.Items.SHOTGUN_PUMP,
      ItemManager.Items.SHOTGUN_DOUBLE,
      ItemManager.Items.GRENADE_LAUNCHER,
      ItemManager.Items.FLAMETHROWER
    };

      _ActualTwoHanded_Dictionary = new List<ItemManager.Items>
    {
      ItemManager.Items.KATANA,
      ItemManager.Items.BAT
    };
    }

    // Get a list of items with ItemManager.Items enum
    public static ItemManager.Items[] GetItemList()
    {
      var list = new List<ItemManager.Items>();
      list.Add(ItemManager.Items.NONE);
      foreach (var entry in _Unlocks_Descriptions)
        if (entry.Key.ToString().StartsWith("ITEM_"))
          list.Add((ItemManager.Items)Enum.Parse(typeof(ItemManager.Items), entry.Key.ToString()[5..]));
      return list.ToArray();
    }
    // Get a list of utilities with UtilityScript.UtilityType enum
    public static UtilityScript.UtilityType[] GetUtilityList()
    {
      var list = new List<UtilityScript.UtilityType>();
      list.Add(UtilityScript.UtilityType.NONE);
      foreach (var entry in _Unlocks_Descriptions)
        if (entry.Key.ToString().StartsWith("UTILITY_"))
          list.Add((UtilityScript.UtilityType)Enum.Parse(typeof(UtilityScript.UtilityType), entry.Key.ToString()[8..]));
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
          list.Add((Perk.PerkType)Enum.Parse(typeof(Perk.PerkType), entry.Key.ToString()[4..]));
      return list.ToArray();
    }

    public static bool IsTwoHanded(ItemManager.Items item)
    {
      return _TwoHanded_Dictionary.Contains(item);
    }
    public static bool IsActuallyTwoHanded(ItemManager.Items item)
    {
      return _ActualTwoHanded_Dictionary.Contains(item);
    }

    // Check available / unlocked
    public static bool UnlockLocked(Unlocks unlock)
    {
      return LevelModule.ShopUnlocksOrdered.ContainsKey(unlock) && LevelModule.ShopUnlocksOrdered[unlock].UnlockValue == ShopUnlockData.UnlockValueType.LOCKED;
    }
    public static bool UnlockAvailable(Unlocks unlock)
    {
      return LevelModule.ShopUnlocksOrdered.ContainsKey(unlock) && LevelModule.ShopUnlocksOrdered[unlock].UnlockValue != ShopUnlockData.UnlockValueType.LOCKED;
    }
    public static bool UnlockUnlocked(Unlocks unlock)
    {
      return LevelModule.ShopUnlocksOrdered.ContainsKey(unlock) && LevelModule.ShopUnlocksOrdered[unlock].UnlockValue == ShopUnlockData.UnlockValueType.UNLOCKED;
    }


    // Append unlock
    public static void AddAvailableUnlock(Unlocks unlock, bool alert = false)
    {
      if (!UnlockLocked(unlock)) return;

      var unlockDat = LevelModule.ShopUnlocksOrdered[unlock];
      unlockDat.UnlockValue = ShopUnlockData.UnlockValueType.AVAILABLE;
      LevelModule.ShopUnlocksOrdered[unlock] = unlockDat;
      LevelModule.SyncShopUnlocks();

      // Auto unlock modes
      switch (unlock)
      {
        case Unlocks.MODE_ZOMBIE:
        case Unlocks.MODE_EXTRAS:

        case Unlocks.EXTRA_CHASE:
        case Unlocks.EXTRA_CROWNMODE:
          Unlock(unlock);
          break;
      }

      // Check notify player
      var unlockString = GetUnlockStringLocalized(unlock);
      if (alert)
      {
        if (unlock == Unlocks.MODE_ZOMBIE)
          s_UnlockString += $"- {LocalizationController.GetString("modeUnlocked")} <color=red>{unlockString}</color>\n";
        else if (unlock == Unlocks.MODE_EXTRAS)
          s_UnlockString += $"- {LocalizationController.GetString("pauseOptionUnlocked")} <color=magenta>{LocalizationController.GetString("extras")}</color>\n";
        else if (unlock.ToString().StartsWith("EXTRA_"))
          s_UnlockString += $"- {LocalizationController.GetString("extras_unlocked")} <color=magenta>{unlockString}</color>\n";
        else
          s_UnlockString += $"- {LocalizationController.GetString("shop_newUnlock")} <color=yellow>{unlockString}</color>\n";
      }
    }

    //
    public static bool AllExtrasUnlocked()
    {
      return
      UnlockUnlocked(Unlocks.EXTRA_BLOOD_FX) &&
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
    public static bool s_SetLevelsMenuAfterUnlockString
    {
      get { return LevelModule.SwitchLevelsMenuAfterUnlockString; }
      set
      {
        LevelModule.SwitchLevelsMenuAfterUnlockString = value;
      }
    }

    public static void ShowUnlocks(Menu.MenuType toMenu)
    {
      Menu.GenericMenu(
        new string[] { $"{LocalizationController.GetString("unlocks_newUnlock")}\n\n", $"{s_UnlockString}" },
        UnityEngine.Random.value < 0.5f ? $"{LocalizationController.GetString("unlocks_wow")}\n\n" : $"{LocalizationController.GetString("unlocks_nice")}\n\n",
        toMenu
      );
      s_UnlockString = "";
      s_SetLevelsMenuAfterUnlockString = false;
    }

    // Unlock from available unlocks
    public static void Unlock(Unlocks unlock)
    {
      if (UnlockUnlocked(unlock)) return;
      if (!_Unlocks_Descriptions.ContainsKey(unlock)) throw new Exception($"No description for unlock: {unlock}");

      var unlockDat = LevelModule.ShopUnlocksOrdered[unlock];
      unlockDat.UnlockValue = ShopUnlockData.UnlockValueType.UNLOCKED;
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
          Loadout.Init();
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

      // Allow all if in debug mode
      if (Debug.isDebugBuild)
        return true;

      // Allow all if editor
      if (Levels._EditingLoadout)
        return true;

      // Check if unlocked
      return UnlockUnlocked(unlock);
    }


  }
}