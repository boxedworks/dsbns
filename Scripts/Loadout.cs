using Assets.Scripts.Game.Items;
using Assets.Scripts.Ragdoll;
using Assets.Scripts.Settings;
using Assets.Scripts.Settings.Serialization;
using UnityEngine;

using Items = ItemManager.Items;

public class Loadout
{
  //
  static LevelSaveData LevelModule { get { return SettingsHelper.s_SaveData.LevelData; } }

  //
  public static Loadout[] _Loadouts;
  public static int _CurrentLoadoutIndex;
  public static Loadout _CurrentLoadout { get { if (Levels._EditingLoadout) return Levels._HardcodedLoadout; if (_Loadouts == null) return null; return _Loadouts[_CurrentLoadoutIndex]; } }

  public static void Init()
  {
    _Loadouts = new Loadout[ShopHelper.s_ShopLoadoutCount];
    for (var i = 0; i < _Loadouts.Length; i++)
      _Loadouts[i] = new Loadout(i);
  }

  public static int _POINTS_MAX
  {
    get
    {
      // Return mission editor max points
      if (Levels._EditingLoadout)
        return Levels._LOADOUT_MAX_POINTS;

      // Else, return MISSION mode shop points
      return ShopHelper._Max_Equipment_Points;
    }
  }

  public int _Id;
  public int _available_points
  {
    get
    {
      var total = 0;
      // Items
      foreach (var item in _two_weapon_pairs ? new Items[] {
            _Equipment._ItemLeft0, _Equipment._ItemRight0,
            _Equipment._ItemLeft1, _Equipment._ItemRight1,
          } : new Items[] {
            _Equipment._ItemLeft0, _Equipment._ItemRight0,
          })
        total += ItemManager.GetItemValue(item);
      // Utils
      foreach (var utility in _Equipment._UtilitiesLeft)
        total += ItemManager.GetUtilityValue(utility);
      foreach (var utility in _Equipment._UtilitiesRight)
        total += ItemManager.GetUtilityValue(utility);
      // Perks
      foreach (var perk in _Equipment._Perks)
        total += ItemManager.GetPerkValue(perk);
      return _POINTS_MAX - total;
    }
  }

  bool two_weapon_pairs;
  public bool _two_weapon_pairs
  {
    get
    {
      return two_weapon_pairs;
    }
    set
    {
      two_weapon_pairs = value;
    }
  }

  public PlayerProfile.Equipment _Equipment;

  public Loadout()
  {

  }

  public Loadout(int id)
  {
    _Id = id;

    // Load saved equipment
    Load();
  }

  public bool CanEquipItem(ActiveRagdoll.Side side, int index, Items item)
  {
    var currentEquipValue = ItemManager.GetItemValue(side == ActiveRagdoll.Side.LEFT ? (index == 0 ? _Equipment._ItemLeft0 : _Equipment._ItemLeft1) : (index == 0 ? _Equipment._ItemRight0 : _Equipment._ItemRight1));
    return _available_points + currentEquipValue - ItemManager.GetItemValue(item) >= 0;
  }
  public bool CanEquipUtility(ActiveRagdoll.Side side, UtilityScript.UtilityType utility)
  {
    var currentUtilities = side == ActiveRagdoll.Side.LEFT ? _Equipment._UtilitiesLeft : _Equipment._UtilitiesRight;
    var currentEquipValue = 0;
    // Check if adding a new utility or adding additional
    if (currentUtilities.Length > 0 && utility != currentUtilities[0])
    {
      currentEquipValue = ItemManager.GetUtilityValue(currentUtilities[0]) * currentUtilities.Length;
    }
    return _available_points + currentEquipValue - ItemManager.GetUtilityValue(utility) >= 0;
  }

  // Save the loadout
  public void Save()
  {
    var savestring = "";
    if (_Equipment._ItemLeft0 != Items.NONE) savestring += $"item_left0:{_Equipment._ItemLeft0.ToString()}|";
    if (_Equipment._ItemRight0 != Items.NONE) savestring += $"item_right0:{_Equipment._ItemRight0.ToString()}|";
    if (_Equipment._ItemLeft1 != Items.NONE) savestring += $"item_left1:{_Equipment._ItemLeft1.ToString()}|";
    if (_Equipment._ItemRight1 != Items.NONE) savestring += $"item_right1:{_Equipment._ItemRight1.ToString()}|";
    if (_Equipment._UtilitiesLeft != null && _Equipment._UtilitiesLeft.Length > 0) savestring += $"utility_left:{_Equipment._UtilitiesLeft[0].ToString()},{_Equipment._UtilitiesLeft.Length}|";
    if (_Equipment._UtilitiesRight != null && _Equipment._UtilitiesRight.Length > 0) savestring += $"utility_right:{_Equipment._UtilitiesRight[0].ToString()},{_Equipment._UtilitiesRight.Length}|";
    foreach (var perk in _Equipment._Perks)
      savestring += $"perk:{perk}|";
    var pairs = _two_weapon_pairs ? 1 : 0;
    savestring += $"two_pairs:{pairs}|";

    LevelModule.SetLoadout(_Id, savestring);
  }

  public void Load()
  {
    _Equipment = new PlayerProfile.Equipment();

    try
    {
      var loadstring = LevelModule.GetLoadout(_Id);

      if (loadstring.Trim().Length == 0) return;
      // Parse load string
      foreach (var split in loadstring.Split('|'))
      {
        if (split.Trim().Length == 0) continue;
        var split0 = split.Split(':');
        var variable = split0[0];
        var val = split0[1];

        // Special case
        val = val switch
        {
          "SWORD" => "KATANA",
          "CHARGE_PISTOL" => "PISTOL_CHARGE",
          "DOUBLE_PISTOL" => "PISTOL_DOUBLE",
          "MACHINE_PISTOL" => "PISTOL_MACHINE",
          _ => val
        };

        //
        switch (variable)
        {
          case "item_left0":
            var item = (Items)System.Enum.Parse(typeof(Items), val, true);
            _Equipment._ItemLeft0 = item;
            break;
          case "item_right0":
            item = (Items)System.Enum.Parse(typeof(Items), val, true);
            _Equipment._ItemRight0 = item;
            break;
          case "item_left1":
            item = (Items)System.Enum.Parse(typeof(Items), val, true);
            _Equipment._ItemLeft1 = item;
            break;
          case "item_right1":
            item = (Items)System.Enum.Parse(typeof(Items), val, true);
            _Equipment._ItemRight1 = item;
            break;
          case "utility_left":
            var split1 = val.Split(',');
            var util = (UtilityScript.UtilityType)System.Enum.Parse(typeof(UtilityScript.UtilityType), split1[0], true);
            var count = split1[1].ParseIntInvariant();
            var utils = new UtilityScript.UtilityType[count];
            for (var i = 0; i < count; i++)
              utils[i] = util;
            _Equipment._UtilitiesLeft = utils;
            break;
          case "utility_right":
            split1 = val.Split(',');
            util = (UtilityScript.UtilityType)System.Enum.Parse(typeof(UtilityScript.UtilityType), split1[0], true);
            count = split1[1].ParseIntInvariant();
            utils = new UtilityScript.UtilityType[count];
            for (var i = 0; i < count; i++)
              utils[i] = util;
            _Equipment._UtilitiesRight = utils;
            break;
          case "two_pairs":
            _two_weapon_pairs = val.ParseIntInvariant() == 1;
            break;
          case "perk":
            var perk = (Perk.PerkType)System.Enum.Parse(typeof(Perk.PerkType), val, true);
            _Equipment._Perks.Add(perk);
            break;
        }
      }
    }
    catch (System.Exception e)
    {
      Debug.LogError($"Tried to load loadout.. failure.\n{e.Message}\n{e.StackTrace}");
    }
  }

}