
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Settings.Serialization
{

  //
  [System.Serializable]
  public class SettingsSaveData
  {
    //
    static SettingsSaveData SettingsModule { get { return SettingsHelper.s_SaveData.Settings; } }

    // Meta
    public string Version = $"{SettingsHelper._VERSION}";

    // General
    public bool UseBlood = true;
    public bool ForceKeyboard = false;
    public bool ShowTips = true;
    public bool ShowDeathText = true;
    public bool TextSpeedFast = false;
    public bool HideUI = false;

    // Localization
    public string Language;

    // Controls
    public bool ControllerRumble = true;
    public bool IgnoreFirstController = false;

    // Graphics
    public int Quality = 5;
    public string ScreenResolution = $"{SettingsHelper.GetSafeMaxResolution()}";
    public bool Fullscreen = true;
    public bool UseVsync = false;
    public bool UseDefaultTargetFramerate = true;
    public bool UseOrthographicCamera = true;

    public int Brightness = 3;
    public int BloomAmount = 2;
    public int DepthOfFieldAmount = 2;

    public bool UseSmokeFx = true;

    public enum CameraZoomType
    {
      CLOSE,
      NORMAL,
      FAR,
      AUTO,
    }
    public CameraZoomType CameraZoom = CameraZoomType.AUTO;

    // Audio
    public int VolumeMusic = 3;
    public int VolumeSFX = 3;

    // Level
    public enum LevelCompletionBehaviorType
    {
      NEXT_LEVEL,

      RELOAD_LEVEL,
      NOTHING,
      PREVIOUS_LEVEL,
      RANDOM_LEVEL,
      RANDOM_LEVEL_ALL
    }
    public LevelCompletionBehaviorType LevelCompletionBehavior = LevelCompletionBehaviorType.NEXT_LEVEL;

    public enum LevelEndConditionType
    {
      RETURN_TO_START,
      LAST_ENEMY_KILLED,
    }
    public LevelEndConditionType LevelEndCondition = LevelEndConditionType.RETURN_TO_START;

    // Player profiles
    public List<PlayerProfileData> PlayerProfiles;
    public bool ShowLoadoutIndexes = true;

    public void UpdatePlayerProfile(int id, PlayerProfileData playerProfile)
    {
      PlayerProfiles[id] = playerProfile;
    }

    // Etc
    public bool UseLightning = true;

    //
    public static void Load()
    {
      // Load json
      if (!System.IO.File.Exists("Settings.json"))
      {
        SettingsHelper.s_SaveData.Settings = new SettingsSaveData();

        // Player profiles
        SettingsModule.PlayerProfiles = new();
        for (var i = 0; i < 4; i++)
          SettingsModule.PlayerProfiles.Add(new PlayerProfileData()
          {
            Id = i,

            LoadoutIndex = 0,

            Color = i,
            ReloadSameTime = true,
            FaceLookDirection = true
          });
      }
      else
      {
        var jsonData = System.IO.File.ReadAllText("Settings.json");
        SettingsHelper.s_SaveData.Settings = JsonUtility.FromJson<SettingsSaveData>(jsonData);
      }
    }
    public static void Save()
    {
      var json = JsonUtility.ToJson(SettingsModule, Application.isEditor || Debug.isDebugBuild);
      System.IO.File.WriteAllText("Settings.json", json);
    }
  }

}