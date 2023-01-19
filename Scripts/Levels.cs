using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Levels : MonoBehaviour
{

  public static List<LevelCollection> _LevelCollections;
  public class LevelCollection
  {
    public string _name;
    public string[] _leveldata;
  }
  public static LevelCollection _CurrentLevelCollection { get { if (Levels._LevelPack_Playing) return Levels._LevelPack_Current; return _LevelCollections[_CurrentLevelCollectionIndex]; } }
  public static int _CurrentLevelCollectionIndex;
  public static string _CurrentLevelCollection_Name { get { return _CurrentLevelCollection._name; } }

  public static int _CurrentLevelIndex;
  public static string _CurrentLevelData { get { return _CurrentLevelCollection._leveldata[_CurrentLevelIndex]; } }

  // If true, will unlock all levels in the level selector
  public static bool _UnlockAllLevels;

  // Write to file
  public static void WriteToFile(string path, params string[] data)
  {
    // Open writer stream
    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path))
    {
      // Write data line-by-line
      foreach (string s in data)
      {
        // Check empty string
        //if (s.Trim().Equals("")) continue;
        // Write to file
        writer.WriteLine(s.Trim());
      }
      // Close stream
      writer.Close();
    }
  }

  public static string ReadFromFile(string path)
  {
    string data = null;
    using (System.IO.StreamReader reader = new System.IO.StreamReader(path))
    {
      data = reader.ReadToEnd();
      reader.Close();
    }
    return data.Trim();
  }

  public static void LoadLevels()
  {

    var leveldata = "";

    //if (Application.isEditor)
    {

      if (_CurrentLevelCollection._name == "levels_editor_local")
      {
#if UNITY_STANDALONE
        if (!System.IO.File.Exists("levels_editor_local.txt"))
          System.IO.File.CreateText("levels_editor_local.txt");
        else
          leveldata = ReadFromFile("levels_editor_local.txt");
#endif
      }
      else
      {
        var loadPath = "Maps/" + _CurrentLevelCollection._name;

        // Check if developmental build; load from editor
        //if (Debug.isDebugBuild && System.IO.Directory.Exists("C:/Users/thoma/Desktop/Projects/Unity/PolySneak/Assets/Resources/"))
        //  loadPath = "C:/Users/thoma/Desktop/Projects/Unity/PolySneak/Assets/Resources/" + loadPath + ".txt";

        // Load map data
        leveldata = /*Debug.isDebugBuild && System.IO.Directory.Exists("C:/Users/thoma/Desktop/Projects/Unity/PolySneak/Assets/Resources/") ?
        ReadFromFile(loadPath) :*/
          Resources.Load<TextAsset>(loadPath).text;
      }

      //Debug.Log($"===== Loaded map data: \n\n{leveldata}");
    }

    /*/ Load hardcoded levels
    /else
    {
      switch (_CurrentLevelCollectionIndex)
      {
        case 0:
          leveldata = Hardcoded_levels._Levels0;
          break;
        case 1:
          leveldata = Hardcoded_levels._Levels1;
          break;
        case 2:
          leveldata = Hardcoded_levels._Levels_Survival;
          break;
      }
    }*/

    // Split into array
    var levels = new List<string>();
    foreach (var data in leveldata.Split('\n'))
    {

      var data0 = data.Trim();

      // Do not save if empty string (data)
      if (data0.Equals("")) continue;
      if (data0.StartsWith("#")) continue;

      // Save to list
      levels.Add(data0);
    }

    // Convert list to array
    _LevelCollections[_CurrentLevelCollectionIndex]._leveldata = levels.ToArray();
  }

  public static bool LevelCompleted(int leveliter)
  {
    return GameScript.Settings._LevelsCompleted[_CurrentLevelCollection_Name].Contains(leveliter);
  }

  public static void SaveLevels()
  {

    if (_CurrentLevelCollection._name == "levels_editor_local")
    {
#if UNITY_STANDALONE
      WriteToFile("levels_editor_local.txt", _CurrentLevelCollection._leveldata);
#endif
    }
    else if (Debug.isDebugBuild)
    {
      // Save each map as a new line in file
      string writePath = "Assets/Resources/Maps/";
      // Check if developmental build; write to editor and return
      /*if (Debug.isDebugBuild && System.IO.Directory.Exists("C:/Users/thoma/Desktop/Projects/Unity/PolySneak/Assets/Resources/"))
      {
        WriteToFile("C:/Users/thoma/Desktop/Projects/Unity/PolySneak/" + writePath + _LevelCollections[_CurrentLevelCollectionIndex]._name + ".txt", _LevelCollections[_CurrentLevelCollectionIndex]._leveldata);
        return;
      }*/

      WriteToFile(writePath + _CurrentLevelCollection._name + ".txt", _CurrentLevelCollection._leveldata);
      //Debug.Log($"Saved levels to {"Assets/Resources/Maps/" + _LevelCollections[_CurrentLevelCollection]._name + ".txt"}");
    }
  }

  public static void DeleteLevel(int mapiter)
  {
    // Resize array to size - 1 and remove current map
    string[] leveldata = _CurrentLevelCollection._leveldata;
    string[] newmaps = new string[leveldata.Length - 1];
    int index = 0;
    for (int i = 0; i < newmaps.Length + 1; i++)
    {
      if (i == mapiter) continue;
      newmaps[index++] = leveldata[i];
    }
    _LevelCollections[Levels._CurrentLevelCollectionIndex]._leveldata = newmaps;
    // Save to file
    SaveLevels();
  }

  /// <summary>
  /// Inserts level index mapIter at position posIter, moving all maps behind posIter down
  /// </summary>
  /// <param name="mapIter"></param>
  /// <param name="posIter"></param>
  public static void InsertLevelAt(int mapIter, int posIter)
  {
    string mapdata = _LevelCollections[_CurrentLevelCollectionIndex]._leveldata[mapIter];
    // Create a list of rearranged levels
    List<string> leveldata_new = new List<string>();
    for (int i = 0; i < _LevelCollections[_CurrentLevelCollectionIndex]._leveldata.Length; i++)
    {
      if (i == mapIter) continue;
      if (i == posIter) leveldata_new.Add(mapdata);
      string data = _LevelCollections[_CurrentLevelCollectionIndex]._leveldata[i];
      leveldata_new.Add(data);
    }
    // Overwrite and save new map data
    _LevelCollections[_CurrentLevelCollectionIndex]._leveldata = leveldata_new.ToArray();
    SaveLevels();
  }

  /// <summary>
  /// Copies map of index mapIter to end of leveldata array
  /// </summary>
  /// <param name="mapIter">The index of the map to copy</param>
  public static void CopyMap(int mapIter)
  {
    string mapdata = _LevelCollections[_CurrentLevelCollectionIndex]._leveldata[mapIter];
    // Resize levels
    string[] leveldata = _LevelCollections[_CurrentLevelCollectionIndex]._leveldata;
    System.Array.Resize<string>(ref leveldata, leveldata.Length + 1);
    _LevelCollections[_CurrentLevelCollectionIndex]._leveldata = leveldata;
    // Add map to end of selection
    _LevelCollections[_CurrentLevelCollectionIndex]._leveldata[leveldata.Length - 1] = mapdata;
    // Save to file
    SaveLevels();
  }

  public static int _Delete_Iter;
  public static void LevelEditor_NewMap(string map_name)
  {
    string mapdata = _LevelCollections[0]._leveldata[0];

    // Resize levels
    string[] leveldata = _LevelCollections[_CurrentLevelCollectionIndex]._leveldata;
    System.Array.Resize<string>(ref leveldata, leveldata.Length + 1);
    _LevelCollections[_CurrentLevelCollectionIndex]._leveldata = leveldata;

    // Add map template to end of selection
    _LevelCollections[_CurrentLevelCollectionIndex]._leveldata[leveldata.Length - 1] = $"{mapdata.Trim().Replace("\n", string.Empty)} +{map_name} \n";

    // Save to file
    SaveLevels();
  }

  public static LevelCollection _LevelPack_Current;
  public static int _LEVELPACK_MAX = 30, _LOADOUT_MAX_POINTS = 35,
    _LevelPackMenu_SaveIndex,
    _LevelEdit_SaveIndex,
    _LevelPack_SaveReorderIndex,
    _LoadoutEdit_SaveIndex,
    _LevelPacks_Play_SaveIndex;
  public static bool _IsReorderingLevel,
    _IsOverwritingLevel,
    _EditingLoadout,
    _LevelPack_Playing,
    _LevelPack_UploadingToWorkshop,
    _LevelPack_SelectingLevelsFromPack,
    _LevelPack_TestingForUpload;
  public static string _UploadingFile;
  public static GameScript.ItemManager.Loadout _HardcodedLoadout;

  // Init level pack folder structure
  public static void LevelPacks_InitFolders()
  {

    // Level pack dir
    if (!System.IO.Directory.Exists("Levelpacks"))
      System.IO.Directory.CreateDirectory("Levelpacks");

    // Local levelpacks dir
    if (!System.IO.Directory.Exists("Levelpacks/Local"))
      System.IO.Directory.CreateDirectory("Levelpacks/Local");

    if (!System.IO.Directory.Exists("Levelpacks/Local/trashed"))
      System.IO.Directory.CreateDirectory("Levelpacks/Local/trashed");

    // Steam Workshop content dir
    if (!System.IO.Directory.Exists("Levelpacks/WorkshopContent"))
      System.IO.Directory.CreateDirectory("Levelpacks/WorkshopContent");
  }

  public static void LevelPacks_NewLocal()
  {

    LevelPacks_InitFolders();

    // Create new level pack on disk
    var filestructure = "Levelpacks/Local/";
    var new_name = "unnamed_levelpack";
    var number = 0;

    while (System.IO.File.Exists($"{filestructure}{new_name}{number}.levelpack"))
      number++;

    WriteToFile($"{filestructure}{new_name}{number}.levelpack", "");

  }

  public static void LevelPack_Save()
  {
    var filestructure = "Levelpacks/Local/";
    WriteToFile($"{filestructure}{_LevelPack_Current._name}", _LevelPack_Current._leveldata);
  }

  public static string[] GetLevelMeta(string leveldata)
  {
    var levelmeta = new string[4];
    levelmeta[0] = leveldata;

    // Get level data and name
    if (leveldata.Contains("+"))
    {
      var splitdat = leveldata.Split('+');

      levelmeta[0] = splitdat[0].Trim();
      levelmeta[1] = splitdat[1].Trim();
    }

    // Get hard loadout
    if (levelmeta[1] != null && levelmeta[1].Contains("!"))
    {
      var splitdat = levelmeta[1].Split('!');

      levelmeta[1] = splitdat[0].Trim();
      levelmeta[2] = splitdat[1].Trim();
    }

    // Get level theme
    if (levelmeta[2] != null && levelmeta[2].Contains("*"))
    {
      var splitdat = levelmeta[2].Split('*');

      levelmeta[2] = splitdat[0].Trim();
      levelmeta[3] = splitdat[1].Trim();
    }
    else if (levelmeta[1] != null && levelmeta[1].Contains("*"))
    {
      var splitdat = levelmeta[1].Split('*');

      levelmeta[1] = splitdat[0].Trim();
      levelmeta[3] = splitdat[1].Trim();
    }

    return levelmeta;
  }
  public static string ParseLevelMeta(params string[] level_meta)
  {

    return $"{level_meta[0].Trim()} " +

      // Add level name
      (level_meta.Length > 1 && level_meta[1] != null ? $"+{level_meta[1]} " : "") +

      // Add loadout
      (level_meta.Length > 2 && level_meta[2] != null ? $"!{level_meta[2]}" : "") +

      // Add theme
      (level_meta.Length > 3 && level_meta[3] != null && int.Parse(level_meta[3]) > -1 ? $"*{level_meta[3]}" : "");

  }

  public static void GetHardcodedLoadout(string loadout_info)
  {
    // Load level loadout equipment
    var equipment = new GameScript.PlayerProfile.Equipment();
    var level_load = loadout_info;

    void UpdateLoadoutUI()
    {
      Levels._HardcodedLoadout._equipment = equipment;

      if (GameScript.PlayerProfile._Profiles != null)
        foreach (var profile in GameScript.PlayerProfile._Profiles)
          profile.UpdateIcons();
    }

    // Empty loadout
    if (level_load == null)
    {
      UpdateLoadoutUI();
      return;
    }

    // Parse string
    var equipment_split = level_load.Split(',');
    if (equipment_split.Length < 7)
    {
      UpdateLoadoutUI();
      return;
    }

    var left_item0 = equipment_split[0];
    var right_item0 = equipment_split[1];
    var left_item1 = equipment_split[2];
    var right_item1 = equipment_split[3];
    var left_util = equipment_split[4];
    var right_util = equipment_split[5];
    var perks = equipment_split[6];

    try
    {

      equipment._item_left0 = (GameScript.ItemManager.Items)System.Enum.Parse(typeof(GameScript.ItemManager.Items), left_item0);
      equipment._item_right0 = (GameScript.ItemManager.Items)System.Enum.Parse(typeof(GameScript.ItemManager.Items), right_item0);
      equipment._item_left1 = (GameScript.ItemManager.Items)System.Enum.Parse(typeof(GameScript.ItemManager.Items), left_item1);
      equipment._item_right1 = (GameScript.ItemManager.Items)System.Enum.Parse(typeof(GameScript.ItemManager.Items), right_item1);

      var utilities_left = new List<UtilityScript.UtilityType>();
      if (left_util.Trim().Length > 0)
      {
        var left_util_split = left_util.Split(':');
        if (left_util_split.Length == 2)
        {
          var util = left_util_split[0];
          var amount = int.Parse(left_util_split[1]);
          for (var i = 0; i < amount; i++)
            utilities_left.Add((UtilityScript.UtilityType)System.Enum.Parse(typeof(UtilityScript.UtilityType), util));
        }
      }
      equipment._utilities_left = utilities_left.ToArray();

      var utilities_right = new List<UtilityScript.UtilityType>();
      if (right_util.Length > 0)
      {
        var right_util_split = right_util.Split(':');
        if (right_util_split.Length == 2)
        {
          var util = right_util_split[0];
          var amount = int.Parse(right_util_split[1]);
          for (var i = 0; i < amount; i++)
            utilities_right.Add((UtilityScript.UtilityType)System.Enum.Parse(typeof(UtilityScript.UtilityType), util));
        }
      }
      equipment._utilities_right = utilities_right.ToArray();

      var perks_list = new List<Shop.Perk.PerkType>();
      var perks_split = perks.Split(':');
      foreach (var perk in perks_split)
      {
        if (perk.Trim().Length == 0) continue;
        perks_list.Add((Shop.Perk.PerkType)System.Enum.Parse(typeof(Shop.Perk.PerkType), perk));
      }
      equipment._perks = perks_list;
    }
    catch (System.Exception e)
    {
      Debug.LogError(e.ToString());
    }

    UpdateLoadoutUI();
  }
}
