using System.Collections;
using System.Collections.Generic;

using System.IO;

using UnityEngine;

public class Levels : MonoBehaviour
{

  public static List<LevelCollection> _LevelCollections;
  public class LevelCollection
  {
    public string _name;
    public string[] _levelData;
  }
  public static LevelCollection _CurrentLevelCollection { get { if (Levels._LevelPack_Playing) return Levels._LevelPack_Current; return _LevelCollections[_CurrentLevelCollectionIndex]; } }
  public static int _CurrentLevelCollectionIndex;
  public static string _CurrentLevelCollection_Name { get { return _CurrentLevelCollection._name; } }

  public static int _CurrentLevelIndex;
  public static string _CurrentLevelData { get { return _CurrentLevelCollection._levelData[_CurrentLevelIndex]; } }

  // If true, will unlock all levels in the level selector
  public static bool _UnlockAllLevels;

  // Write to file
  public static void WriteToFile(string path, params string[] data)
  {
    // Open writer stream
    using (var writer = new StreamWriter(path))
    {
      // Write data line-by-line
      foreach (var s in data)
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
    using (var reader = new StreamReader(path))
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
        if (!File.Exists("levels_editor_local.txt"))
          File.CreateText("levels_editor_local.txt");
        else
          leveldata = ReadFromFile("levels_editor_local.txt");
#endif
      }
      else
      {
        var loadPath = "Maps/" + _CurrentLevelCollection._name;

        // Check if developmental build; load from editor
        //if (Debug.isDebugBuild && Directory.Exists("C:/Users/thoma/Desktop/Projects/Unity/PolySneak/Assets/Resources/"))
        //  loadPath = "C:/Users/thoma/Desktop/Projects/Unity/PolySneak/Assets/Resources/" + loadPath + ".txt";

        // Load map data
        leveldata = /*Debug.isDebugBuild && Directory.Exists("C:/Users/thoma/Desktop/Projects/Unity/PolySneak/Assets/Resources/") ?
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
    _LevelCollections[_CurrentLevelCollectionIndex]._levelData = levels.ToArray();
  }

  public static Dictionary<int, Dictionary<int, System.Tuple<float, float>>> _CurrentLevel_LevelTimesData;
  public static Dictionary<int, Dictionary<int, bool>> _Levels_All_TopRatings;
  public static Dictionary<int, string> _Ranks_Lowest;
  public static void BufferLevelTimeDatas()
  {

    // Init new dictionaries
    _CurrentLevel_LevelTimesData = new Dictionary<int, Dictionary<int, System.Tuple<float, float>>>();
    _CurrentLevel_LevelTimesData.Add(0, new Dictionary<int, System.Tuple<float, float>>());
    _CurrentLevel_LevelTimesData.Add(1, new Dictionary<int, System.Tuple<float, float>>());

    if (_Ranks_Lowest == null)
    {
      _Ranks_Lowest = new Dictionary<int, string>();
      _Ranks_Lowest.Add(0, "****");
      _Ranks_Lowest.Add(1, "****");
    }

    // Loop through level collections
    var difficulty_save = Settings._DIFFICULTY;
    for (var difficulty = 0; difficulty < 2; difficulty++)
    {
      Levels._CurrentLevelIndex = Settings._DIFFICULTY = difficulty;

      // Get directory to loop through
      var level_datas = _CurrentLevelCollection._levelData;

      // Init per-difficulty dicts
      if (Settings._CurrentDifficulty_NotTopRated)
      {
        if (_Levels_All_TopRatings == null)
          _Levels_All_TopRatings = new Dictionary<int, Dictionary<int, bool>>();
        if (_Levels_All_TopRatings.ContainsKey(difficulty))
          _Levels_All_TopRatings[difficulty] = new Dictionary<int, bool>();
        else
          _Levels_All_TopRatings.Add(difficulty, new Dictionary<int, bool>());
      }

      // Gather data
      for (var i = 0; i < level_datas.Length; i++)
      {

        var dev_time = -1f;
        foreach (var d in level_datas[i].Split(" "))
        {
          if (d.StartsWith("bdt_"))
          {
            dev_time = float.Parse(d.Split("_")[1]);
            break;
          }
        }
        var level_time_best = float.Parse(PlayerPrefs.GetFloat($"{_CurrentLevelCollection_Name}_{i}_time", -1f).ToString("0.000"));

        // Save data
        _CurrentLevel_LevelTimesData[difficulty].Add(i, System.Tuple.Create(dev_time, level_time_best));
        if (Settings._CurrentDifficulty_NotTopRated)
          _Levels_All_TopRatings[difficulty].Add(i, LevelHasTopRating(i));

        if (Settings._CurrentDifficulty_NotTopRated)
        {
          if (dev_time == -1f || level_time_best < 0f)
          {
            _Ranks_Lowest[difficulty] = "";
          }
          else
          {
            var rating = GetLevelRating(dev_time, level_time_best);
            var rating_stars = rating == null ? "" : rating.Item1;
            if (_Ranks_Lowest[difficulty].Length > rating_stars.Length)
            {
              _Ranks_Lowest[difficulty] = rating_stars;
            }
          }
        }
      }

    }
    Levels._CurrentLevelIndex = Settings._DIFFICULTY = difficulty_save;
  }

  public static bool LevelCompleted(int leveliter)
  {
    return Settings._LevelsCompleted[_CurrentLevelCollection_Name].Contains(leveliter);
  }

  public static System.Tuple<string, string>[] GetLevelRatings()
  {
    return new System.Tuple<string, string>[]{
      System.Tuple.Create("****", "#65E7E5"),
      System.Tuple.Create("***", "#DCE461"),
      System.Tuple.Create("**", "#8F8686"),
      System.Tuple.Create("*", "#6F4646"),
    };
  }
  public static float[] GetLevelRatingTimings(float dev_time)
  {
    var medal_diamond = dev_time + 0.4f;
    return new float[]{
      medal_diamond,  // Diamond
      medal_diamond * 1.25f,  // Gold
      medal_diamond * 2f,   // Silver
      medal_diamond * 5f,   // Bronze
    };
  }
  public static System.Tuple<string, string> GetLevelRating(float dev_time, float player_time)
  {
    var ratings = GetLevelRatings();
    if (player_time == -1f || dev_time < 0f)
    {
      return null;
    }

    var rating_times = GetLevelRatingTimings(dev_time);

    var index = 0;
    var medal_index = -1;
    foreach (var time in rating_times)
    {
      var time_ = float.Parse(string.Format("{0:0.000}", time));
      if (player_time <= time_ && medal_index == -1)
      {
        return ratings[index];

      }
      index++;
    }

    return null;
  }
  public static System.Tuple<string, string> GetLevelRating(int level_index)
  {
    var time_data = Levels._CurrentLevel_LevelTimesData[Settings._DIFFICULTY][level_index];
    return GetLevelRating(time_data.Item1, time_data.Item2);
  }
  public static bool LevelHasTopRating(int level_index)
  {
    var level_rating = GetLevelRating(level_index);
    return level_rating == null ? false : level_rating.Item1.Length == 4;
  }

  // Save levels to file
  public static void SaveLevels()
  {

    if (_CurrentLevelCollection._name == "levels_editor_local")
    {
#if UNITY_STANDALONE
      WriteToFile("levels_editor_local.txt", _CurrentLevelCollection._levelData);
#endif
    }
    else if (Debug.isDebugBuild)
    {
      // Save each map as a new line in file
      var write_path = "Assets/Resources/Maps/";
      var file_name = write_path + _CurrentLevelCollection._name + ".txt";
      var save_data = _CurrentLevelCollection._levelData;

      // Check if developmental build; write to editor and return
      if (Debug.isDebugBuild && Directory.Exists("C:/Users/thoma/Desktop/Projects/Unity/PolySneak/Assets/Resources/"))
      {
        WriteToFile("C:/Users/thoma/Desktop/Projects/Unity/PolySneak/" + file_name, save_data);
        return;
      }

      WriteToFile(file_name, save_data);
      //Debug.Log($"Saved levels to {"Assets/Resources/Maps/" + _LevelCollections[_CurrentLevelCollection]._name + ".txt"}");
    }
  }

  // Delete a level from level list and save
  public static void DeleteLevel(int mapIter)
  {
    // Resize array to size - 1 and remove current map
    var levelData = _CurrentLevelCollection._levelData;
    var newMaps = new string[levelData.Length - 1];

    System.Array.Copy(levelData, 0, newMaps, 0, mapIter);
    System.Array.Copy(levelData, mapIter + 1, newMaps, mapIter, levelData.Length - mapIter - 1);

    _LevelCollections[Levels._CurrentLevelCollectionIndex]._levelData = newMaps;

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
    string mapdata = _LevelCollections[_CurrentLevelCollectionIndex]._levelData[mapIter];
    // Create a list of rearranged levels
    List<string> leveldata_new = new List<string>();
    for (int i = 0; i < _LevelCollections[_CurrentLevelCollectionIndex]._levelData.Length; i++)
    {
      if (i == mapIter) continue;
      if (i == posIter) leveldata_new.Add(mapdata);
      string data = _LevelCollections[_CurrentLevelCollectionIndex]._levelData[i];
      leveldata_new.Add(data);
    }
    // Overwrite and save new map data
    _LevelCollections[_CurrentLevelCollectionIndex]._levelData = leveldata_new.ToArray();
    SaveLevels();
  }

  /// <summary>
  /// Copies map of index mapIter to end of leveldata array
  /// </summary>
  /// <param name="mapIter">The index of the map to copy</param>
  public static void CopyMap(int mapIter)
  {
    string mapdata = _LevelCollections[_CurrentLevelCollectionIndex]._levelData[mapIter];
    // Resize levels
    string[] leveldata = _LevelCollections[_CurrentLevelCollectionIndex]._levelData;
    System.Array.Resize<string>(ref leveldata, leveldata.Length + 1);
    _LevelCollections[_CurrentLevelCollectionIndex]._levelData = leveldata;
    // Add map to end of selection
    _LevelCollections[_CurrentLevelCollectionIndex]._levelData[leveldata.Length - 1] = mapdata;
    // Save to file
    SaveLevels();
  }

  public static int _Delete_Iter;
  public static void LevelEditor_NewMap(string map_name)
  {
    string mapdata = _LevelCollections[0]._levelData[0];

    // Resize levels
    string[] leveldata = _LevelCollections[_CurrentLevelCollectionIndex]._levelData;
    System.Array.Resize<string>(ref leveldata, leveldata.Length + 1);
    _LevelCollections[_CurrentLevelCollectionIndex]._levelData = leveldata;

    // Add map template to end of selection
    _LevelCollections[_CurrentLevelCollectionIndex]._levelData[leveldata.Length - 1] = $"{mapdata.Trim().Replace("\n", string.Empty)} +{map_name} \n";

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
    if (!Directory.Exists("Levelpacks"))
      Directory.CreateDirectory("Levelpacks");

    // Local levelpacks dir
    if (!Directory.Exists("Levelpacks/Local"))
      Directory.CreateDirectory("Levelpacks/Local");

    if (!Directory.Exists("Levelpacks/Local/trashed"))
      Directory.CreateDirectory("Levelpacks/Local/trashed");

    // Steam Workshop content dir
    if (!Directory.Exists("Levelpacks/WorkshopContent"))
      Directory.CreateDirectory("Levelpacks/WorkshopContent");
  }

  public static void LevelPacks_NewLocal()
  {

    LevelPacks_InitFolders();

    // Create new level pack on disk
    var filestructure = "Levelpacks/Local/";
    var new_name = "unnamed_levelpack";
    var number = 0;

    while (File.Exists($"{filestructure}{new_name}{number}.levelpack"))
      number++;

    WriteToFile($"{filestructure}{new_name}{number}.levelpack", "");

  }

  public static void LevelPack_Save()
  {
    var filestructure = "Levelpacks/Local/";
    WriteToFile($"{filestructure}{_LevelPack_Current._name}", _LevelPack_Current._levelData);
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

      // Add level best dev time
      (TileManager._LevelTime_Dev != -1f ? $"bdt_{TileManager._LevelTime_Dev} " : "") +

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
