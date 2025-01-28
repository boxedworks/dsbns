using System.Collections.Generic;

using UnityEngine;

// Holds settings you may see in a game menu
public static class Settings
{
  //
  static SettingsSaveData SettingsModule { get { return s_SaveData.Settings; } }
  static LevelSaveData LevelModule { get { return s_SaveData.LevelData; } }

  //
  static Resolution ScreenResolution;

  public static bool _Slowmo_on_death = true,
    _Slowmo_on_lastkill = true,
    _PLAYER_INVINCIBLE = false;

  public static int _DeleteSaveDataIter, _DeleteStatsIter;

  public static float _ForceKeyboardTimer = -0.5f;
  public static bool _ForceKeyboard
  {
    set
    {
      if (Time.unscaledTime - _ForceKeyboardTimer < 0.1f) return;
      _ForceKeyboardTimer = Time.unscaledTime;
      SettingsModule.ForceKeyboard = value;
    }
    get
    {
      return SettingsModule.ForceKeyboard;
    }
  }
  public static bool _Fullscreen
  {
    set
    {
      // Set value
      SettingsModule.Fullscreen = value;

      // Update graphics
      UpdateResolution();
    }
    get
    {
      return SettingsModule.Fullscreen;
    }
  }
  public static Resolution _ScreenResolution
  {
    set
    {
      ScreenResolution = value;

      // Update json
      SettingsModule.ScreenResolution = $"{value}";

      // Update res
      UpdateResolution();
    }
    get
    {
      return ScreenResolution;
    }
  }

  public static bool _CurrentDifficulty_NotTopRated { get { return (_DIFFICULTY == 0 && !LevelModule.IsTopRatedClassic0) || (_DIFFICULTY == 1 && !LevelModule.IsTopRatedClassic1); } }

  public static List<LevelSaveData.LevelStructData> _LevelsCompleted_Current { get { return LevelModule.LevelData[Levels._CurrentLevelCollectionIndex].Data; } }

  public static int _NumberPlayers = -1,
    _NumberControllers = -1;
  public static int _DifficultyUnlocked
  {
    get
    {
      if (GameScript.s_GameMode == GameScript.GameModes.CLASSIC)
        return LevelModule.HighestDifficultyUnlockedClassic;
      else
        return LevelModule.HighestDifficultyUnlockedSurvival;
    }
    set
    {
      if (GameScript.s_GameMode == GameScript.GameModes.CLASSIC)
        LevelModule.HighestDifficultyUnlockedClassic = value;
      else
        LevelModule.HighestDifficultyUnlockedSurvival = value;
    }
  }

  public static int _DIFFICULTY
  {
    get
    {
      if (GameScript.s_GameMode == GameScript.GameModes.CLASSIC)
        return LevelModule.Difficulty;
      else return 0;
    }
    set
    {
      LevelModule.Difficulty = value;
    }
  }

  public static int _VolumeMusic
  {
    get
    {
      return SettingsModule.VolumeMusic;
    }
    set
    {
      SettingsModule.VolumeMusic = value % 6;
      if (SettingsModule.VolumeMusic < 0)
        SettingsModule.VolumeMusic = 5;

      // Update music volume
      FunctionsC.MusicManager.s_TrackSource.volume = SettingsModule.VolumeMusic / 5f * 0.8f;
    }
  }

  public static int _QualityLevel
  {
    get
    {
      return SettingsModule.Quality;
    }
    set
    {
      // Use modulus to get remainder
      SettingsModule.Quality = value % 6;
      if (SettingsModule.Quality < 0)
        SettingsModule.Quality = 6 - SettingsModule.Quality;

      // Update quality settings
      QualitySettings.SetQualityLevel(SettingsModule.Quality);

      var data = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
      data.msaaSampleCount = SettingsModule.Quality >= 4 ? 2 : 1;
      data.supportsHDR = SettingsModule.Quality >= 3;
    }
  }
  public static bool _VSync
  {
    get
    {
      return SettingsModule.UseVsync;
    }
    set
    {
      // Use modulus to get remainder
      SettingsModule.UseVsync = value;

      // Update quality settings
      QualitySettings.vSyncCount = value ? 1 : 0;
    }
  }

  // Extras
  public static bool _Extras_CanUse { get { return GameScript.s_GameMode == GameScript.GameModes.CLASSIC && !_LevelEditorEnabled; } }
  public static bool _Extras_UsingAny
  {
    get
    {
      return
        _Extras_UsingAnyImportant ||
        LevelModule.ExtraBloodType != 0
      ;
    }
  }
  public static bool _Extras_UsingAnyImportant
  {
    get
    {
      return
        LevelModule.ExtraTime != 0 ||
        LevelModule.ExtraHorde != 0 ||
        LevelModule.ExtraRemoveChaser != 0 ||
        LevelModule.ExtraGravity != 0 ||
        LevelModule.ExtraEnemyMultiplier != 0 ||
        LevelModule.ExtraPlayerAmmo != 0 ||
        LevelModule.ExtraEnemyAmmo != 0 ||
        LevelModule.ExtraBodyExplode != 0 ||
        LevelModule.ExtraCrownMode != 0
      ;
    }
  }

  public static float _VERSION = 1.48f;

  // Struct holding info what item pair gets unlocked at what level
  public class WeaponPair
  {
    public int _levelCollectionIter, _levelPackIter;
    public WeaponPair(int lc, int lp)
    {
      _levelCollectionIter = lc;
      _levelPackIter = lp;
    }
  }

  public static WeaponPair[] _WeaponPairInfo;

  public static void Init()
  {

    // Register levels files by name
    Levels._LevelCollections = new();
    var levelsLoad = new string[] { "levels0", "levels1", "levels_survival", "levels_editor_local", "levels_versus" /*"levels_challenge"*/ };
#if UNITY_WEBGL
    levelsLoad = new string[] { "levels_demo", "levels_demo", "levels_survival", "levels_editor_local", "levels_versus" /*"levels_challenge"*/ };
#endif
    foreach (var filename in levelsLoad)
      Levels._LevelCollections.Add(new Levels.LevelCollection()
      {
        _name = filename
      });

    // Load all level collections
    for (var i = 0; i < Levels._LevelCollections.Count; i++)
    {
      Levels._CurrentLevelCollectionIndex = i;
      Levels.LoadLevels();
    }

    // Load json
    s_SaveData = new SaveData();
    SettingsSaveData.Load();
    LevelSaveData.Load();

    // Load last version (old)
    var oldversion_OLD = PlayerPrefs.GetFloat("VERSION", -1f);
    if (oldversion_OLD != -1f)
    {
      // Fix really old saves... by deleting them :)
      if (oldversion_OLD < 1.1f)
        PlayerPrefs.DeleteAll();

      // Fix shop point buy bug
      if (oldversion_OLD < 1.24f)
      {
        if (PlayerPrefs.GetInt($"levels0_0", 0) == 1)
        {
          Shop._AvailablePoints = PlayerPrefs.GetInt("Shop_availablePoints", 3);
          Shop._AvailablePoints++;
        }
      }

      // Check new json; convert
      if (oldversion_OLD < 1.41f)
      {

        // Settings
        SettingsModule.UseBlood = PlayerPrefs.GetInt("show_blood", 1) == 1;
        SettingsModule.ForceKeyboard = PlayerPrefs.GetInt("force_keyboard", 0) == 1;
        SettingsModule.ShowTips = PlayerPrefs.GetInt("show_tips", 1) == 1;
        SettingsModule.ShowDeathText = PlayerPrefs.GetInt("showDeathText", 1) == 1;
        SettingsModule.TextSpeedFast = PlayerPrefs.GetInt("option_fasttext", 0) == 1;

        SettingsModule.ControllerRumble = PlayerPrefs.GetInt("controller_rumble", 1) == 1;
        SettingsModule.IgnoreFirstController = PlayerPrefs.GetInt("_IgnoreFirstController", 0) == 1;

        var defaultQuality = 5;
#if UNITY_WEBGL
        defaultQuality = 2;
#endif
        SettingsModule.Quality = PlayerPrefs.GetInt("quality", defaultQuality);
        SettingsModule.ScreenResolution = PlayerPrefs.GetString("screen_resolution", $"{GetSafeMaxResolution()}");
        SettingsModule.Fullscreen = PlayerPrefs.GetInt("screen_fullscreen", 1) == 1;
        SettingsModule.UseVsync = PlayerPrefs.GetInt("vsync_setting", 0) == 1;
        SettingsModule.UseDefaultTargetFramerate = PlayerPrefs.GetInt("default_targetFramerate", 1) == 1;
        SettingsModule.UseOrthographicCamera = PlayerPrefs.GetInt("CameraType_ortho", 1) == 1;
        SettingsModule.CameraZoom = (SettingsSaveData.CameraZoomType)PlayerPrefs.GetInt("CameraZoom", SettingsModule.UseOrthographicCamera ? 3 : 1);

        SettingsModule.VolumeMusic = PlayerPrefs.GetInt("VolumeMusic", 3);
        SettingsModule.VolumeSFX = PlayerPrefs.GetInt("VolumeSFX", 3);

        SettingsModule.LevelCompletionBehavior = (SettingsSaveData.LevelCompletionBehaviorType)PlayerPrefs.GetInt("LevelCompletion", 0);

        SettingsModule.PlayerProfiles = new();
        for (var i = 0; i < 4; i++)
          SettingsModule.PlayerProfiles.Add(new SettingsSaveData.PlayerProfile()
          {
            Id = i,

            LoadoutIndex = PlayerPrefs.GetInt($"PlayerProfile{i}_loadoutIndex", 0),

            Color = PlayerPrefs.GetInt($"PlayerProfile{i}_color", i),
            ReloadSameTime = PlayerPrefs.GetInt($"PlayerProfile{i}_reloadSidesSameTime", 1) == 1,
            FaceLookDirection = PlayerPrefs.GetInt($"PlayerProfile{i}_faceDir", 1) == 1
          });

        SettingsModule.UseLightning = PlayerPrefs.GetInt("vfx_toggle_lightning", 1) == 1;

        // Level data
        LevelModule.IsTopRatedClassic0 = PlayerPrefs.GetInt("_Classic_0_TopRated", 0) == 1;
        LevelModule.IsTopRatedClassic1 = PlayerPrefs.GetInt("_Classic_1_TopRated", 0) == 1;

        LevelModule.HighestDifficultyUnlockedClassic = PlayerPrefs.GetInt("CLASSIC_DifficultyLevel", 0);
        LevelModule.HighestDifficultyUnlockedSurvival = PlayerPrefs.GetInt("SURVIVAL_DifficultyLevel", 0);

        LevelModule.Difficulty = PlayerPrefs.GetInt("CLASSIC_SavedDifficulty", 0);

        LevelModule.LevelData = new();
        for (var i = 0; i < 2; i++)
        {

          LevelModule.LevelData.Add(new());
          LevelModule.LevelData[i] = new LevelSaveData.LevelStructDataWrapper()
          {
            Data = new()
          };
          for (var u = 0; u < Levels._LevelCollections[i]._levelData.Length; u++)
          {
            LevelModule.LevelData[i].Data.Add(new LevelSaveData.LevelStructData()
            {
              LevelNumber = u,
              Completed = PlayerPrefs.GetInt($"{Levels._LevelCollections[i]._name}_{u}", 0) == 1,
              BestCompletionTime = PlayerPrefs.GetFloat($"{Levels._LevelCollections[i]._name}_{u}_time", -1f).ToStringTimer()
            });
          }
        }

        for (var i = 0; i < Levels._LevelCollections[2]._levelData.Length; i++)
        {
          var highestWave = PlayerPrefs.GetInt($"SURVIVAL_MAP_{i}", 0);
          if (highestWave > 0)
            LevelModule.SurvivalHighestWave.Add(highestWave);
        }

        LevelModule.ShopPoints = PlayerPrefs.GetInt("Shop_availablePoints", 3);
        LevelModule.ShopDisplayMode = PlayerPrefs.GetInt("Shop_DisplayMode", 0);
        LevelModule.ShopLoadoutDisplayMode = PlayerPrefs.GetInt("Shop_LoadoutDisplayMode", 0);
        LevelModule.ShopUnlockString = PlayerPrefs.GetString("UnlockString", "");

        foreach (Shop.Unlocks unlock in System.Enum.GetValues(typeof(Shop.Unlocks)))
        {

          // Speacial case; changed name
          var unlockString = unlock switch
          {
            Shop.Unlocks.ITEM_KATANA => "ITEM_SWORD",
            Shop.Unlocks.ITEM_PISTOL_CHARGE => "ITEM_PISTOL_CHARGE",
            Shop.Unlocks.ITEM_PISTOL_DOUBLE => "ITEM_DOUBLE_PISTOL",
            Shop.Unlocks.ITEM_PISTOL_MACHINE => "ITEM_MACHINE_PISTOL",
            _ => $"{unlock}"
          };

          var unlockUnlocked = PlayerPrefs.GetInt($"Shop_Unlocks_{unlockString}", 0) == 1;
          var unlockAvailable = PlayerPrefs.GetInt($"Shop_UnlocksAvailable_{unlockString}", 0) == 1;

          LevelModule.ShopUnlocks.Add(new LevelSaveData.ShopUnlock()
          {
            Unlock = unlock,
            UnlockValue = unlockUnlocked ? LevelSaveData.ShopUnlock.UnlockValueType.UNLOCKED : (unlockAvailable ? LevelSaveData.ShopUnlock.UnlockValueType.AVAILABLE : LevelSaveData.ShopUnlock.UnlockValueType.LOCKED)
          });
        }

        for (var i = 0; i < 15; i++)
        {
          var saveString = PlayerPrefs.GetString($"loadout:{i}", "");
          if (saveString != "")
            LevelModule.LoadoutData.Add(new LevelSaveData.LoadoutStructData()
            {
              Id = i,

              SaveString = saveString
            });
        }

        LevelModule.ExtraGravity = PlayerPrefs.GetInt("extra_gravity", 0);
        LevelModule.ExtraTime = PlayerPrefs.GetInt("extra_superhot", 0);
        LevelModule.ExtraHorde = PlayerPrefs.GetInt("extra_crazyzombies", 0);
        LevelModule.ExtraRemoveChaser = PlayerPrefs.GetInt("extra_batguy", 0);
        LevelModule.ExtraBloodType = PlayerPrefs.GetInt("extra_bloodtype", 0);
        LevelModule.ExtraEnemyMultiplier = PlayerPrefs.GetInt("extra_emulti", 0);
        LevelModule.ExtraBodyExplode = PlayerPrefs.GetInt("extra_bodyexplode", 0);
        LevelModule.ExtraEnemyAmmo = PlayerPrefs.GetInt("extra_enemyammo", 0);
        LevelModule.ExtraPlayerAmmo = PlayerPrefs.GetInt("extra_playerammo", 0);

        LevelModule.HasRestarted = PlayerPrefs.GetInt("tut_hasRestarted", 0) == 1;
      }

      PlayerPrefs.DeleteAll();
    }

    // Set new version
    var versionLast = SettingsModule.Version.ParseFloatInvariant();
    var versionCurrent = _VERSION;
    if (versionLast < versionCurrent)
    {

    }
    SettingsModule.Version = $"{_VERSION}";

    // Set volumes
    _VolumeMusic = SettingsModule.VolumeMusic;

    // Load graphics settings
    SetResolution(SettingsModule.ScreenResolution);
    UpdateResolution();
    _QualityLevel = SettingsModule.Quality;
    _VSync = SettingsModule.UseVsync;
    SetPostProcessing();

    // Set starting level collection
    Levels._CurrentLevelCollectionIndex = LevelModule.Difficulty;

    // Load missing / new shop items
    Shop.s_ShopLoadoutCount = 3;
    Shop._Max_Equipment_Points = 1;
    LevelModule.OrderShopUnlocks();
    foreach (Shop.Unlocks unlock in System.Enum.GetValues(typeof(Shop.Unlocks)))
    {

      if (!LevelModule.ShopUnlocksOrdered.ContainsKey(unlock))
        LevelModule.ShopUnlocks.Add(new LevelSaveData.ShopUnlock()
        {
          Unlock = unlock,
          UnlockValue = LevelSaveData.ShopUnlock.UnlockValueType.LOCKED
        });
      else
      {
        if (LevelModule.ShopUnlocksOrdered[unlock].UnlockValue == LevelSaveData.ShopUnlock.UnlockValueType.UNLOCKED)
        {
          var unlockDat = LevelModule.ShopUnlocksOrdered[unlock];
          unlockDat.UnlockValue = LevelSaveData.ShopUnlock.UnlockValueType.LOCKED;
          LevelModule.ShopUnlocksOrdered[unlock] = unlockDat;
          Shop.Unlock(unlock);
        }
      }
    }
    LevelModule.OrderShopUnlocks();

    // Set starter unlocks
    Shop.AddAvailableUnlock(Shop.Unlocks.ITEM_KNIFE);
    Shop.AddAvailableUnlock(Shop.Unlocks.UTILITY_SHURIKEN);

    Shop.AddAvailableUnlock(Shop.Unlocks.MAX_EQUIPMENT_POINTS_0);
    Shop.AddAvailableUnlock(Shop.Unlocks.MAX_EQUIPMENT_POINTS_1);
    Shop.AddAvailableUnlock(Shop.Unlocks.ITEM_PISTOL_SILENCED);

    // Extras
    s_Extra_UnlockCriterea = new Dictionary<Shop.Unlocks, UnlockCriteria>();

    // Gravity
    s_Extra_UnlockCriterea.Add(
        Shop.Unlocks.EXTRA_GRAVITY,
        new UnlockCriteria
        {
          level = 63,
          difficulty = 0,
          rating = 0,
          extras = null,
          loadoutDesc = "knife, silenced pistol",
          items = new GameScript.ItemManager.Items[] {
            GameScript.ItemManager.Items.KNIFE,
            GameScript.ItemManager.Items.PISTOL_SILENCED
            },
          utilities = null,
          perks = null
        }
    );

    // Player ammo
    s_Extra_UnlockCriterea.Add(
        Shop.Unlocks.EXTRA_PLAYER_AMMO,
        new UnlockCriteria
        {
          level = 95,
          difficulty = 0,
          rating = 2,
          extras = new Shop.Unlocks[] { Shop.Unlocks.EXTRA_HORDE },
          loadoutDesc = "knife, lever-action rifle",
          items = new GameScript.ItemManager.Items[] {
            GameScript.ItemManager.Items.KNIFE,
            GameScript.ItemManager.Items.RIFLE_LEVER
            },
          utilities = null,
          perks = null
        }
    );

    // Chaser
    s_Extra_UnlockCriterea.Add(
        Shop.Unlocks.EXTRA_CHASE,
        new UnlockCriteria
        {
          level = -1,
          difficulty = 0,
          rating = 0,
          extras = null,
          loadoutDesc = "auto-unlocked",
          items = new GameScript.ItemManager.Items[] {
            GameScript.ItemManager.Items.KNIFE
          },
          utilities = null,
          perks = null
        }
    );

    // Enemy off
    s_Extra_UnlockCriterea.Add(
        Shop.Unlocks.EXTRA_ENEMY_OFF,
        new UnlockCriteria
        {
          level = 48,
          difficulty = 0,
          rating = 2,
          extras = null,
          loadoutDesc = "sticky gun",
          items = new GameScript.ItemManager.Items[] {
            GameScript.ItemManager.Items.STICKY_GUN
            },
          utilities = null,
          perks = null
        }
    );

    // Horde
    s_Extra_UnlockCriterea.Add(
        Shop.Unlocks.EXTRA_HORDE,
        new UnlockCriteria
        {
          level = 50,
          difficulty = 1,
          rating = 1,
          extras = null,
          loadoutDesc = "axe x2",
          items = new GameScript.ItemManager.Items[] {
            GameScript.ItemManager.Items.AXE,
            GameScript.ItemManager.Items.AXE
            },
          utilities = null,
          perks = null
        }
    );

    // Time
    s_Extra_UnlockCriterea.Add(
        Shop.Unlocks.EXTRA_TIME,
        new UnlockCriteria
        {
          level = 75,
          difficulty = 1,
          rating = 0,
          extras = null,
          loadoutDesc = "silenced pistol x2",
          items = new GameScript.ItemManager.Items[] {
            GameScript.ItemManager.Items.PISTOL_SILENCED,
            GameScript.ItemManager.Items.PISTOL_SILENCED
          },
          utilities = null,
          perks = null
        }
    );

    // Blood FX
    s_Extra_UnlockCriterea.Add(
        Shop.Unlocks.EXTRA_BLOOD_FX,
        new UnlockCriteria
        {
          level = 127,
          difficulty = 0,
          rating = 0,
          extras = null,
          loadoutDesc = "katana, shuriken x4",
          items = new GameScript.ItemManager.Items[] {
            GameScript.ItemManager.Items.KATANA,
            },
          utilities = new UtilityScript.UtilityType[]{
            UtilityScript.UtilityType.SHURIKEN,
            UtilityScript.UtilityType.SHURIKEN,
          },
          perks = null
        }
    );

    // Explode on death
    s_Extra_UnlockCriterea.Add(
        Shop.Unlocks.EXTRA_EXPLODED,
        new UnlockCriteria
        {
          level = 109,
          difficulty = 1,
          rating = 0,
          extras = null,
          loadoutDesc = "grenade impact x4, explosion res.",
          items = null,
          utilities = new UtilityScript.UtilityType[]
          {
            UtilityScript.UtilityType.GRENADE_IMPACT,
            UtilityScript.UtilityType.GRENADE_IMPACT,
            UtilityScript.UtilityType.GRENADE_IMPACT,
            UtilityScript.UtilityType.GRENADE_IMPACT,
          },
          perks = new Shop.Perk.PerkType[]
          {
            Shop.Perk.PerkType.EXPLOSION_RESISTANCE
          }
        }
    );

    //
    SettingsSaveData.Save();
    LevelSaveData.Save();
  }

  // Take snapshot of extras
  public static int[] GetExtrasSnapshot()
  {
    return new int[]{
        LevelModule.ExtraHorde,
        LevelModule.ExtraCrownMode,
        LevelModule.ExtraPlayerAmmo,
        LevelModule.ExtraEnemyAmmo,
        LevelModule.ExtraEnemyMultiplier,
        LevelModule.ExtraGravity,
        LevelModule.ExtraRemoveChaser,
        LevelModule.ExtraTime,
        LevelModule.ExtraBloodType,
        LevelModule.ExtraBodyExplode
      };
  }

  public static void AllExtrasOff()
  {
    LevelModule.ExtraHorde = 0;
    LevelModule.ExtraCrownMode = 0;
    LevelModule.ExtraEnemyAmmo = 0;
    LevelModule.ExtraPlayerAmmo = 0;
    LevelModule.ExtraEnemyMultiplier = 0;
    LevelModule.ExtraGravity = 0;
    LevelModule.ExtraRemoveChaser = 0;
    LevelModule.ExtraTime = 0;
    LevelModule.ExtraBloodType = 0;
    LevelModule.ExtraBodyExplode = 0;
  }

  public enum GamemodeChange
  {
    CLASSIC,
    SURVIVAL,

    LEVEL_EDITOR,
    VERSUS,
  }

  public static bool _LevelEditorEnabled;
  public static void OnGamemodeChanged(GamemodeChange gamemode)
  {
    // Update UI
    foreach (var profile in GameScript.PlayerProfile.s_Profiles)
      profile.UpdateIcons();

    //
    switch (gamemode)
    {
      case GamemodeChange.CLASSIC:
        switch (LevelModule.ExtraGravity)
        {
          case 0: Physics.gravity = new Vector3(0f, -9.81f, 0f); break;
          case 1: Physics.gravity = new Vector3(0f, 9.81f, 0f); break;
          case 2: Physics.gravity = new Vector3(0f, 0f, 9.81f); break;
          case 3: Physics.gravity = Vector3.zero; break;
        }
        break;

      case GamemodeChange.LEVEL_EDITOR:
      case GamemodeChange.SURVIVAL:
      case GamemodeChange.VERSUS:

        Physics.gravity = new Vector3(0f, -9.81f, 0f);

        break;
    }

    VersusMode.OnGamemodeSwitched(gamemode == GamemodeChange.VERSUS);

    _LevelEditorEnabled = gamemode == GamemodeChange.LEVEL_EDITOR;
  }

  public static void SetPostProcessing(bool forceOffDOF = false)
  {

    // Camera settings
    if (SettingsModule.UseOrthographicCamera)
    {
      GameResources._Camera_Main.orthographic = GameResources._Camera_IgnorePP.orthographic = true;
      GameResources._Camera_Main.orthographicSize = GameResources._Camera_IgnorePP.orthographicSize =
        SettingsModule.CameraZoom == SettingsSaveData.CameraZoomType.NORMAL ? 7.6f : (SettingsModule.CameraZoom == SettingsSaveData.CameraZoomType.CLOSE ? 5.9f : 10.8f);
      GameResources._Camera_Main.transform.eulerAngles = GameResources._Camera_IgnorePP.transform.eulerAngles = new Vector3(88f, 0f, 0f);
    }
    else
    {
      GameResources._Camera_Main.orthographic = GameResources._Camera_IgnorePP.orthographic = false;
      GameResources._Camera_Main.transform.eulerAngles = GameResources._Camera_IgnorePP.transform.eulerAngles = new Vector3(89.9f, 0f, 0f);
    }

    //
    var profile_ = GameResources._Camera_Main.GetComponent<UnityEngine.Rendering.Volume>().profile;

    // Brightness
    UnityEngine.Rendering.Universal.ColorAdjustments colorAdjustment = null;
    if (profile_.TryGet(out colorAdjustment))
    {
      var b = Mathf.Lerp(0.65f, 2.4f, SettingsModule.Brightness / 9f);
      colorAdjustment.colorFilter.value = new Color(b, b, b);
    }

    // Bloom
    UnityEngine.Rendering.Universal.Bloom bloom = null;
    if (profile_.TryGet(out bloom))
    {
      bloom.intensity.value = SettingsModule.BloomAmount switch
      {
        0 => 0f,
        1 => 1f,
        2 => 1.5f,
        3 => 10f
      };
    }

    // DOF
    UnityEngine.Rendering.Universal.DepthOfField dof = null;
    if (profile_.TryGet(out dof))
    {
      if (SettingsModule.DepthOfFieldAmount > 0 && !forceOffDOF)
      {
        dof.mode.value = UnityEngine.Rendering.Universal.DepthOfFieldMode.Bokeh;

        var apertureMod = SettingsModule.DepthOfFieldAmount == 1 ? 1.6f : 1.2f;

        if (SettingsModule.UseOrthographicCamera)
        {
          dof.focusDistance.value = 5.65f;
          dof.aperture.value = apertureMod;
          dof.focalLength.value = 235f;
        }
        else
        {
          switch (SettingsModule.CameraZoom)
          {
            case SettingsSaveData.CameraZoomType.CLOSE:
              dof.focusDistance.value = 10.2f;
              dof.aperture.value = 1f * apertureMod;
              dof.focalLength.value = 235f;
              break;

            case SettingsSaveData.CameraZoomType.FAR:
              dof.focusDistance.value = 18.2f;
              dof.aperture.value = 0.8f * apertureMod;
              dof.focalLength.value = 300f;
              break;

            default:
              dof.focusDistance.value = 14.3f;
              dof.aperture.value = 0.9f * apertureMod;
              dof.focalLength.value = 275f;
              break;
          }
        }
      }

      else
      {
        dof.mode.value = UnityEngine.Rendering.Universal.DepthOfFieldMode.Off;
      }
    }
  }

  public static Resolution GetSafeMaxResolution()
  {
    var max = Screen.resolutions[Screen.resolutions.Length - 1];
    if (max.width > 1920)
      for (var i = Screen.resolutions.Length - 1; i >= 0; i--)
      {
        var local_res = Screen.resolutions[i];
        if (local_res.width > 1920)
          continue;
        max = local_res;
        break;
      }
    return max;
  }

  public static void SetResolution(string resolution)
  {
    //Debug.LogError($"Attempting to set resolution to: {resolution} with current resolution: {_ScreenResolution}");

    // Check for current resolution
    if ("" + _ScreenResolution == resolution)
    {
      //Debug.LogError($"Attempting to set resolution to same resolution");
      return;
    }

    // Check for new resolution
    //Debug.Log("printing supported resolutions list:");
    foreach (var res in Screen.resolutions)
    {
      //Debug.Log($"{res} vs {resolution}");
      if ("" + res == resolution)
      {
        _ScreenResolution = res;
        Application.targetFrameRate = SettingsModule.UseDefaultTargetFramerate ? -1 : _ScreenResolution.refreshRate;

        //Debug.LogError($"Found supported resolution and set");
        return;
      }
    }

    // Found none
    _ScreenResolution = GetSafeMaxResolution();

    //Debug.LogError($"Could not find resolution, set to safe max: {_ScreenResolution}");
  }

  public static void UpdateResolution()
  {
#if UNITY_WEBGL
    return;
#endif

    Screen.SetResolution(_ScreenResolution.width, _ScreenResolution.height, _Fullscreen);
    // set the desired aspect ratio (the values in this example are
    // hard-coded for 16:9, but you could make them into public
    // variables instead so you can set them at design time)
    var targetaspect = 16.0f / 9.0f;

    // determine the game window's current aspect ratio
    var windowaspect = (float)_ScreenResolution.width / (float)_ScreenResolution.height;

    // current viewport height should be scaled by this amount
    var scaleheight = windowaspect / targetaspect;

    // obtain camera component so we can modify its viewport
    foreach (var camera in new Camera[] { GameResources._Camera_Main, GameResources._Camera_Menu })
    {

      // if scaled height is less than current height, add letterbox
      if (scaleheight < 1.0f)
      {
        var rect = camera.rect;

        rect.width = 1.0f;
        rect.height = scaleheight;
        rect.x = 0;
        rect.y = (1.0f - scaleheight) / 2.0f;

        camera.rect = rect;
      }
      else // add pillarbox
      {
        var scalewidth = 1.0f / scaleheight;
        var rect = camera.rect;

        rect.width = scalewidth;
        rect.height = 1.0f;
        rect.x = (1.0f - scalewidth) / 2.0f;
        rect.y = 0;

        camera.rect = rect;
      }
    }
  }

  public struct UnlockCriteria
  {
    // Level and difficulty
    public int level;
    public int difficulty;
    public int rating;

    // Extras
    public Shop.Unlocks[] extras;

    // Loadout
    public string loadoutDesc;
    public GameScript.ItemManager.Items[] items;
    public UtilityScript.UtilityType[] utilities;
    public Shop.Perk.PerkType[] perks;
  }

  public static Dictionary<Shop.Unlocks, UnlockCriteria> s_Extra_UnlockCriterea;

  // JSON save
  public class SaveData
  {

    //
    public SettingsSaveData Settings;
    public LevelSaveData LevelData;
  }
  public static SaveData s_SaveData;

  //
  [System.Serializable]
  public class SettingsSaveData
  {

    // Meta
    public string Version = $"{_VERSION}";

    // General
    public bool UseBlood = true;
    public bool ForceKeyboard = false;
    public bool ShowTips = true;
    public bool ShowDeathText = true;
    public bool TextSpeedFast = false;

    // Controls
    public bool ControllerRumble = true;
    public bool IgnoreFirstController = false;

    // Graphics
    public int Quality = 5;
    public string ScreenResolution = $"{GetSafeMaxResolution()}";
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
    [System.Serializable]
    public struct PlayerProfile
    {
      public int Id;

      public int LoadoutIndex;

      public int Color;
      public bool FaceLookDirection;
      public bool ReloadSameTime;
    }
    public List<PlayerProfile> PlayerProfiles;
    public bool ShowLoadoutIndexes = true;

    public void UpdatePlayerProfile(int id, PlayerProfile playerProfile)
    {
      PlayerProfiles[id] = playerProfile;
    }

    // Etc
    public bool UseLightning = true;

    //
    public static void Load()
    {
      // Load json
      if (!System.IO.File.Exists("settings.json"))
      {
        s_SaveData.Settings = new SettingsSaveData();

        // Player profiles
        SettingsModule.PlayerProfiles = new();
        for (var i = 0; i < 4; i++)
          SettingsModule.PlayerProfiles.Add(new PlayerProfile()
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
        var jsonData = System.IO.File.ReadAllText("settings.json");
        s_SaveData.Settings = JsonUtility.FromJson<SettingsSaveData>(jsonData);
      }
    }
    public static void Save()
    {
      var json = JsonUtility.ToJson(SettingsModule, Application.isEditor || Debug.isDebugBuild);
      System.IO.File.WriteAllText("settings.json", json);
    }
  }

  //
  [System.Serializable]
  public class LevelSaveData
  {

    // Meta
    public bool IsTopRatedClassic0 = false, IsTopRatedClassic1 = false;
    public int HighestDifficultyUnlockedClassic = 0, HighestDifficultyUnlockedSurvival = 0;

    // Classic
    public int Difficulty = 0;

    [System.Serializable]
    public struct LevelStructData
    {
      public int LevelNumber;
      public bool Completed;
      public string BestCompletionTime;
    }
    [System.Serializable]
    public struct LevelStructDataWrapper
    {
      public List<LevelStructData> Data;
    }
    public List<LevelStructDataWrapper> LevelData;

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

    [System.Serializable]
    public struct ShopUnlock
    {
      public Shop.Unlocks Unlock;
      public enum UnlockValueType
      {
        LOCKED,
        AVAILABLE,
        UNLOCKED
      }
      public UnlockValueType UnlockValue;
    }
    public List<ShopUnlock> ShopUnlocks;
    [System.NonSerialized]
    public Dictionary<Shop.Unlocks, ShopUnlock> ShopUnlocksOrdered;
    public void OrderShopUnlocks()
    {
      ShopUnlocksOrdered = new();
      foreach (var entry in LevelModule.ShopUnlocks)
        ShopUnlocksOrdered.Add(entry.Unlock, entry);
    }
    public void SyncShopUnlocks()
    {
      ShopUnlocks = new List<ShopUnlock>(ShopUnlocksOrdered.Values);
    }

    // Loadouts
    [System.Serializable]
    public struct LoadoutStructData
    {
      public int Id;

      public string SaveString;
    }
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
        s_SaveData.LevelData = new LevelSaveData();

        // Empty level data
        LevelModule.LevelData = new();
        for (var i = 0; i < 2; i++)
        {

          LevelModule.LevelData.Add(new());
          LevelModule.LevelData[i] = new LevelStructDataWrapper()
          {
            Data = new()
          };
          for (var u = 0; u < Levels._LevelCollections[i]._levelData.Length; u++)
          {
            LevelModule.LevelData[i].Data.Add(new LevelStructData()
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
        s_SaveData.LevelData = JsonUtility.FromJson<LevelSaveData>(jsonData);
      }
    }
    public static void Save()
    {
      var json = JsonUtility.ToJson(LevelModule, Application.isEditor || Debug.isDebugBuild);
      System.IO.File.WriteAllText("save.json", json);
    }
  }

}