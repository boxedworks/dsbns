
using System.Collections.Generic;
using Assets.Scripts.Game.Items;
using UnityEngine;

namespace Assets.Scripts.Settings.Serialization
{
  //
  [System.Serializable]
  public class LevelSaveData
  {

    //
    static LevelSaveData LevelModule { get { return SettingsHelper.s_SaveData.LevelData; } }

    // Meta
    public bool IsTopRatedClassic0 = false, IsTopRatedClassic1 = false;
    public int HighestDifficultyUnlockedClassic = 0, HighestDifficultyUnlockedSurvival = 0;

    // Classic
    public int Difficulty = 0;
    public List<LevelDataWrapper> LevelData;

    public float GetLevelBestTime(int levelCollectionIndex = -1, int levelIndex = -1)
    {
      if (levelCollectionIndex == -1)
        levelCollectionIndex = Levels._CurrentLevelCollectionIndex;
      if (levelIndex == -1)
        levelIndex = Levels._CurrentLevelIndex;

      if (levelCollectionIndex > 1)
        return -1f;

      return LevelData[levelCollectionIndex].Data[levelIndex].BestCompletionTime.ParseFloatInvariant();
    }
    public void SetLevelBestTime(float time, int levelCollectionIndex = -1, int levelIndex = -1)
    {
      if (levelCollectionIndex == -1)
        levelCollectionIndex = Levels._CurrentLevelCollectionIndex;
      if (levelIndex == -1)
        levelIndex = Levels._CurrentLevelIndex;

      if (levelCollectionIndex > 1)
        return;

      var levelDat = LevelData[levelCollectionIndex].Data[levelIndex];
      levelDat.BestCompletionTime = time.ToStringTimer();
      LevelData[levelCollectionIndex].Data[levelIndex] = levelDat;
    }

    // Survival
    public List<int> SurvivalHighestWave;

    public int GetHighestSurvivalWave(int levelIndex = -1)
    {
      if (levelIndex == -1)
        levelIndex = Levels._CurrentLevelIndex;

      if (levelIndex >= SurvivalHighestWave.Count) return 0;
      return SurvivalHighestWave[levelIndex];
    }
    public void SetHighestSurvivalWave(int highestWave, int levelIndex = -1)
    {
      if (levelIndex == -1)
        levelIndex = Levels._CurrentLevelIndex;

      if (levelIndex < SurvivalHighestWave.Count)
        SurvivalHighestWave[levelIndex] = highestWave;
      else
        SurvivalHighestWave.Add(highestWave);
    }

    // Shop
    public int ShopPoints = 3;
    public int ShopDisplayMode = 0;
    public int ShopLoadoutDisplayMode = 0;
    public string ShopUnlockString = "";
    public bool SwitchLevelsMenuAfterUnlockString = false;

    public List<ShopUnlockData> ShopUnlocks;
    [System.NonSerialized]
    public Dictionary<ShopHelper.Unlocks, ShopUnlockData> ShopUnlocksOrdered;
    public void OrderShopUnlocks()
    {
      ShopUnlocksOrdered = new();
      foreach (var entry in LevelModule.ShopUnlocks)
        ShopUnlocksOrdered.Add(entry.Unlock, entry);
    }
    public void SyncShopUnlocks()
    {
      ShopUnlocks = new List<ShopUnlockData>(ShopUnlocksOrdered.Values);
    }

    // Loadouts
    public List<LoadoutStructData> LoadoutData;

    public string GetLoadout(int id)
    {
      if (id < LoadoutData.Count)
        return LoadoutData[id].SaveString;
      else
        return "";
    }
    public void SetLoadout(int id, string saveString)
    {
      if (id < LoadoutData.Count)
      {
        var loadoutDat = LoadoutData[id];
        loadoutDat.SaveString = saveString;
        LoadoutData[id] = loadoutDat;
      }
      else
      {
        LoadoutData.Add(new LoadoutStructData()
        {
          Id = id,

          SaveString = saveString
        });
      }
    }

    // Extras
    public int
      ExtraGravity = 0,
      ExtraTime = 0,
      ExtraHorde = 0,
      ExtraRemoveChaser = 0,
      ExtraBloodType = 0,
      ExtraEnemyMultiplier = 0,
      ExtraBodyExplode = 0,
      ExtraEnemyAmmo = 0, ExtraPlayerAmmo = 0,
      ExtraCrownMode = 0;

    // Etc
    public bool HasRestarted = false;

    //
    public static void Load()
    {
      // Load json
      if (!System.IO.File.Exists("save.json"))
      {
        SettingsHelper.s_SaveData.LevelData = new LevelSaveData();

        // Empty level data
        LevelModule.LevelData = new();
        for (var i = 0; i < 2; i++)
        {

          LevelModule.LevelData.Add(new());
          LevelModule.LevelData[i] = new LevelDataWrapper()
          {
            Data = new()
          };
          for (var u = 0; u < Levels._LevelCollections[i]._levelData.Length; u++)
          {
            LevelModule.LevelData[i].Data.Add(new LevelData()
            {
              LevelNumber = u,
              Completed = false,
              BestCompletionTime = "-1.000"
            });
          }
        }

        LevelModule.SurvivalHighestWave = new();

        //
        LevelModule.ShopUnlocks = new();
        LevelModule.LoadoutData = new();
      }
      else
      {
        var jsonData = System.IO.File.ReadAllText("save.json");
        SettingsHelper.s_SaveData.LevelData = JsonUtility.FromJson<LevelSaveData>(jsonData);
      }
    }
    public static void Save()
    {
      var json = JsonUtility.ToJson(LevelModule, Application.isEditor || Debug.isDebugBuild);
      System.IO.File.WriteAllText("save.json", json);
    }
  }
}