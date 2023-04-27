using System.Collections.Generic;

using UnityEngine;

// Holds settings you may see in a game menu
public static class Settings
{
  static Resolution ScreenResolution;

  public static bool _Slowmo_on_death = true,
    _Slowmo_on_lastkill = true,
    _PLAYER_INVINCIBLE = false;
  static bool Blood = true,
    ForceKeyboard = false,
    ShowTips = true,
    Fullscreen;


  public static int _DeleteSaveDataIter, _DeleteStatsIter;

  public static bool _Blood
  {
    set
    {
      if (_Blood == value) return;
      Blood = value;
      // Save pref
      PlayerPrefs.SetInt("show_blood", _Blood ? 1 : 0);
    }
    get
    {
      return Blood;
    }
  }

  public static float _ForceKeyboardTimer = -0.5f;
  public static bool _ForceKeyboard
  {
    set
    {
      // Check same value
      if (ForceKeyboard == value) return;

      if (Time.unscaledTime - _ForceKeyboardTimer < 0.1f) return;
      _ForceKeyboardTimer = Time.unscaledTime;
      ForceKeyboard = value;
      // Save pref
      PlayerPrefs.SetInt("force_keyboard", value ? 1 : 0);
    }
    get
    {
      return ForceKeyboard;
    }
  }
  public static bool _ShowTips
  {
    set
    {
      // Check same value
      if (ShowTips == value) return;

      ShowTips = value;
      // Save pref
      PlayerPrefs.SetInt("show_tips", value ? 1 : 0);
    }
    get
    {
      return ShowTips;
    }
  }
  public static bool _Fullscreen
  {
    set
    {
      // Don't update if value has not changed
      if (Fullscreen == value) return;
      // Set value
      Fullscreen = value;
      // Save pref
      PlayerPrefs.SetInt("screen_fullscreen", value ? 1 : 0);
      // Update graphics
      UpdateResolution();
    }
    get
    {
      return Fullscreen;
    }
  }
  public static Resolution _ScreenResolution
  {
    set
    {
      ScreenResolution = value;
      // Update pref
      PlayerPrefs.SetString("screen_resolution", "" + value);
      // Update res
      UpdateResolution();
    }
    get
    {
      return ScreenResolution;
    }
  }
  static bool UseDefaultTargetFramerate;
  public static bool _UseDefaultTargetFramerate
  {
    set
    {
      UseDefaultTargetFramerate = value;
      PlayerPrefs.SetInt("default_targetFramerate", UseDefaultTargetFramerate ? 1 : 0);
    }
    get
    {
      return UseDefaultTargetFramerate;
    }
  }

  public static FunctionsC.SaveableStat_Bool _CameraType, _Classic_0_TopRated, _Classic_1_TopRated;
  public static bool _CurrentDifficulty_NotTopRated { get { return (_DIFFICULTY == 0 && !_Classic_0_TopRated._value) || (_DIFFICULTY == 1 && !_Classic_1_TopRated._value); } }

  static bool ControllerRumble;
  public static bool _ControllerRumble
  {
    set
    {
      ControllerRumble = value;
      PlayerPrefs.SetInt("controller_rumble", ControllerRumble ? 1 : 0);
    }
    get
    {
      return ControllerRumble;
    }
  }

  public static Dictionary<string, List<int>> _LevelsCompleted;
  public static List<int> _LevelsCompleted_Current { get { return _LevelsCompleted[Levels._CurrentLevelCollection_Name]; } }

  public static int _NumberPlayers = -1,
    _NumberControllers = -1,
    _WeaponUnlocked = 0;
  static int VolumeMusic = 5,
    VolumeSFX = 5,
    QualityLevel = -1,
    DIFFICULTY;
  public static int _DifficultyUnlocked
  {
    get
    {
      return PlayerPrefs.GetInt($"{GameScript._GameMode}_DifficultyLevel", 0);
    }
    set
    {
      PlayerPrefs.SetInt($"{GameScript._GameMode}_DifficultyLevel", value);
    }
  }
  public static int _DIFFICULTY
  {
    get
    {
      return DIFFICULTY;
    }
    set
    {
      DIFFICULTY = value;
      PlayerPrefs.SetInt($"{GameScript._GameMode}_SavedDifficulty", DIFFICULTY);
    }
  }
  public static int _VolumeMusic
  {
    get
    {
      return VolumeMusic;
    }
    set
    {
      VolumeMusic = value % 6;
      if (VolumeMusic < 0)
        VolumeMusic = 5;
      // Update music volume
      FunctionsC.MusicManager.s_TrackSource.volume = VolumeMusic / 5f * 0.8f;
      // Save value
      PlayerPrefs.SetInt("VolumeMusic", VolumeMusic);
    }
  }
  public static int _VolumeSFX
  {
    get
    {
      return VolumeSFX;
    }
    set
    {
      VolumeSFX = value % 6;
      if (VolumeSFX < 0)
        VolumeSFX = 5;
      // Save value
      PlayerPrefs.SetInt("VolumeSFX", VolumeSFX);
    }
  }


  public static FunctionsC.SaveableStat_Bool _Toggle_Lightning;
  public static FunctionsC.SaveableStat_Int _CameraZoom, _LevelCompletion;

  public static int _QualityLevel
  {
    get
    {
      return QualityLevel;
    }
    set
    {
      if (QualityLevel == value) return;
      // Use modulus to get remainder
      QualityLevel = value % 6;
      if (QualityLevel < 0)
        QualityLevel = 6 - QualityLevel;
      // Save pref
      PlayerPrefs.SetInt("quality", QualityLevel);
      // Update quality settings
      QualitySettings.SetQualityLevel(QualityLevel);
    }
  }
  static bool VSync;
  public static bool _VSync
  {
    get
    {
      return VSync;
    }
    set
    {
      if (VSync == value) return;
      // Use modulus to get remainder
      VSync = value;
      // Save pref
      PlayerPrefs.SetInt("vsync_setting", value ? 1 : 0);
      // Update quality settings
      QualitySettings.vSyncCount = value ? 1 : 0;
    }
  }

  // Extras
  static int Extra_Gravity;
  public static int _Extra_Gravity
  {
    get
    {
      return Extra_Gravity;
    }
    set
    {
      if (Extra_Gravity == value) return;
      Extra_Gravity = value;

      // Save pref
      PlayerPrefs.SetInt("extra_gravity", value);
    }
  }
  static bool Extra_Superhot;
  public static bool _Extra_Superhot
  {
    get
    {
      return Extra_Superhot;
    }
    set
    {
      if (Extra_Superhot == value) return;
      Extra_Superhot = value;

      // Save pref
      PlayerPrefs.SetInt("extra_superhot", value ? 1 : 0);
    }
  }
  static bool Extra_CrazyZombies;
  public static bool _Extra_CrazyZombies
  {
    get
    {
      return Extra_CrazyZombies;
    }
    set
    {
      if (Extra_CrazyZombies == value) return;
      Extra_CrazyZombies = value;

      // Save pref
      PlayerPrefs.SetInt("extra_crazyzombies", value ? 1 : 0);
    }
  }

  public static FunctionsC.SaveableStat_Int
    _Extra_RemoveBatGuy,
    _Extra_EnemyMultiplier,
    _Extra_PlayerAmmo, _Extra_EnemyAmmo,
    _Extra_BodyExplode,
    _Extra_BloodType,
    _Extra_CrownMode;
  public static bool _Extras_CanUse { get { return GameScript._GameMode == GameScript.GameModes.CLASSIC && !_LevelEditorEnabled; } }
  public static bool _Extras_UsingAny
  {
    get
    {
      return
        _Extras_UsingAnyImportant ||
        _Extra_BloodType._value != 0
      ;
    }
  }
  public static bool _Extras_UsingAnyImportant
  {
    get
    {
      return
        _Extra_Superhot ||
        _Extra_CrazyZombies ||
        _Extra_RemoveBatGuy._value != 0 ||
        Extra_Gravity != 0 ||
        _Extra_EnemyMultiplier._value != 0 ||
        _Extra_PlayerAmmo._value != 0 ||
        _Extra_EnemyAmmo._value != 0 ||
        _Extra_BodyExplode._value != 0 ||
        _Extra_CrownMode._value != 0
      ;
    }
  }

  public static float _VERSION = 1.25f;

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

    // Load last version
    var oldversion = PlayerPrefs.GetFloat("VERSION", -1f);
    if (oldversion != -1f)
    {
      // Fix really old saves... by deleting them :)
      if (oldversion < 1.1f)
        PlayerPrefs.DeleteAll();

      // Fix shop point buy bug
      if (oldversion < 1.24f)
      {
        if (PlayerPrefs.GetInt($"levels0_0", 0) == 1)
        {
          Shop._AvailablePoints = PlayerPrefs.GetInt("Shop_availablePoints", 3);
          Shop._AvailablePoints++;
        }
      }
    }
    PlayerPrefs.SetFloat("VERSION", _VERSION);

    // Register levels files by name
    Levels._LevelCollections = new List<Levels.LevelCollection>();
    var levelfilenames = new string[] { "levels0", "levels1", "levels_survival", "levels_editor_local", /*"levels_challenge"*/ };
    foreach (var filename in levelfilenames)
      Levels._LevelCollections.Add(new Levels.LevelCollection()
      {
        _name = filename
      });
    // Load all level collections
    for (int i = 0; i < Levels._LevelCollections.Count; i++)
    {
      Levels._CurrentLevelCollectionIndex = i;
      Levels.LoadLevels();
    }
    // Set starting level collection
    Levels._CurrentLevelCollectionIndex = DIFFICULTY;

    // Load level packs
    //Levels.InitLevelPacks();

    // Weapon unlocks
    _WeaponUnlocked = PlayerPrefs.GetInt("WeaponUnlocks", 0);
    // What level collection and level pack weapons get unlocked
    _WeaponPairInfo = new WeaponPair[]
    {
        new WeaponPair(0, 10),
        new WeaponPair(0, 24),
        new WeaponPair(0, 36),
        new WeaponPair(1, 24),
        new WeaponPair(1, 36),
    };
    // Load completed levels
    _LevelsCompleted = new Dictionary<string, List<int>>();
    for (var u = 0; u < Levels._LevelCollections.Count; u++)
    {
      var levels_loaded = new List<int>();
      int threes = 0;
      for (int i = 0; i < Levels._LevelCollections[u]._levelData.Length; i++)
      {
        threes++;
        var data = PlayerPrefs.GetInt(Levels._LevelCollections[u]._name + "_" + i, 0);
        if (data == 1)
        {
          levels_loaded.Add(i);
          //Debug.Log($"{Levels._LevelCollections[u]._name} {i}");
          if (threes == 3)
          {
            var iter = 0;
            foreach (var p in _WeaponPairInfo)
            {
              if (u == p._levelCollectionIter && (i / 3) + 1 == p._levelPackIter)
                while (iter >= _WeaponUnlocked)
                  _WeaponUnlocked++;
              iter++;
            }
          }
        }
        if (threes == 3) threes = 0;
      }
      _LevelsCompleted.Add(Levels._LevelCollections[u]._name, levels_loaded);
    }
    // Load general settings
    _Blood = PlayerPrefs.GetInt("show_blood", 1) == 1;
    _ForceKeyboard = PlayerPrefs.GetInt("force_keyboard", 0) == 1;
    _ShowTips = PlayerPrefs.GetInt("show_tips", 1) == 1;

    // Load graphics settings
    _UseDefaultTargetFramerate = PlayerPrefs.GetInt("default_targetFramerate", 1) == 1;
    var max = GetSafeMaxResolution();
    //Debug.Log("got safe max resolution: " + max);

    var got_resolution = PlayerPrefs.GetString("screen_resolution", "" + max);
    //Debug.Log("got loaded resolution: " + got_resolution);

    SetResolution(got_resolution);
    _Fullscreen = PlayerPrefs.GetInt("screen_fullscreen", 1) == 1;
    UpdateResolution();
    _QualityLevel = PlayerPrefs.GetInt("quality", 5);
    _VSync = PlayerPrefs.GetInt("vsync_setting", 0) == 1;

    // Load audio settings
    _VolumeMusic = PlayerPrefs.GetInt("VolumeMusic", 3);
    _VolumeSFX = PlayerPrefs.GetInt("VolumeSFX", 3);
    _Toggle_Lightning = new FunctionsC.SaveableStat_Bool("vfx_toggle_lightning", true);

    // Level settings
    _LevelCompletion = new FunctionsC.SaveableStat_Int("LevelCompletion", 0);

    //Camera
    _CameraZoom = new FunctionsC.SaveableStat_Int("CameraZoom", 1);
    _CameraType = new FunctionsC.SaveableStat_Bool("CameraType_ortho", false);
    SetPostProcessing();

    // Top ratings
    _Classic_0_TopRated = new FunctionsC.SaveableStat_Bool("_Classic_0_TopRated", false);
    _Classic_1_TopRated = new FunctionsC.SaveableStat_Bool("_Classic_1_TopRated", false);

    // Controller
    _ControllerRumble = PlayerPrefs.GetInt("controller_rumble", 1) == 1;

    // Extras
    _Extra_Gravity = PlayerPrefs.GetInt("extra_gravity", 0);
    _Extra_Superhot = PlayerPrefs.GetInt("extra_superhot", 0) == 1;
    _Extra_CrazyZombies = PlayerPrefs.GetInt("extra_crazyzombies", 0) == 1;
    _Extra_RemoveBatGuy = new FunctionsC.SaveableStat_Int("extra_batguy", 0);
    _Extra_EnemyMultiplier = new FunctionsC.SaveableStat_Int("extra_emulti", 0);
    _Extra_PlayerAmmo = new FunctionsC.SaveableStat_Int("extra_playerammo", 0);
    _Extra_EnemyAmmo = new FunctionsC.SaveableStat_Int("extra_enemyammo", 0);
    _Extra_BodyExplode = new FunctionsC.SaveableStat_Int("extra_bodyexplode", 0);
    _Extra_BloodType = new FunctionsC.SaveableStat_Int("extra_bloodtype", 0);
    _Extra_CrownMode = new FunctionsC.SaveableStat_Int("extra_crownmode", 0);

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
          rating = 1,
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
          level = 110,
          difficulty = 0,
          rating = 1,
          extras = new Shop.Unlocks[] { Shop.Unlocks.EXTRA_HORDE },
          loadoutDesc = "knife, double-barrel shotgun",
          items = new GameScript.ItemManager.Items[] {
            GameScript.ItemManager.Items.KNIFE,
            GameScript.ItemManager.Items.SHOTGUN_DOUBLE
            },
          utilities = null,
          perks = null
        }
    );

    // Enemy multi
    s_Extra_UnlockCriterea.Add(
        Shop.Unlocks.EXTRA_ENEMY_OFF,
        new UnlockCriteria
        {
          level = 12,
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
          loadoutDesc = "sword, shuriken x2",
          items = new GameScript.ItemManager.Items[] {
            GameScript.ItemManager.Items.SWORD,
            },
          utilities = new UtilityScript.UtilityType[]{
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

  }

  // Take snapshot of extras
  public static int[] GetExtrasSnapshot()
  {
    return new int[]{
        Settings._Extra_CrazyZombies ? 1 : 0,
        Settings._Extra_CrownMode._value,
        Settings._Extra_EnemyAmmo._value,
        Settings._Extra_EnemyMultiplier._value,
        Settings._Extra_Gravity,
        Settings._Extra_PlayerAmmo._value,
        Settings._Extra_RemoveBatGuy._value,
        Settings._Extra_Superhot ? 1 : 0,
        Settings._Extra_BloodType._value,
        Settings._Extra_BodyExplode._value
      };
  }

  public enum GamemodeChange
  {
    CLASSIC,
    SURVIVAL,

    LEVEL_EDITOR
  }

  public static bool _LevelEditorEnabled;
  public static void OnGamemodeChanged(GamemodeChange gamemode)
  {
    switch (gamemode)
    {
      case GamemodeChange.CLASSIC:
        switch (_Extra_Gravity)
        {
          case 0: Physics.gravity = new Vector3(0f, -9.81f, 0f); break;
          case 1: Physics.gravity = new Vector3(0f, 9.81f, 0f); break;
          case 2: Physics.gravity = new Vector3(0f, 0f, 9.81f); break;
          case 3: Physics.gravity = Vector3.zero; break;
        }
        break;

      case GamemodeChange.SURVIVAL:
      case GamemodeChange.LEVEL_EDITOR:

        Physics.gravity = new Vector3(0f, -9.81f, 0f);
        break;
    }

    _LevelEditorEnabled = gamemode == GamemodeChange.LEVEL_EDITOR;
  }

  public static void SetPostProcessing()
  {
    // Camera settings
    if (_CameraType._value)
    {
      GameResources._Camera_Main.orthographic = true;
      GameResources._Camera_Main.orthographicSize = _CameraZoom == 1 ? 7.6f : _CameraZoom == 0 ? 5.9f : 10.8f;
      GameResources._Camera_Main.transform.eulerAngles = new Vector3(88f, 0f, 0f);
    }
    else
    {
      GameResources._Camera_Main.orthographic = false;
      GameResources._Camera_Main.transform.eulerAngles = new Vector3(89.9f, 0f, 0f);
    }

    // PP
    var profiles = GameObject.Find("PProfiles").transform;
    for (var u = 0; u < 7; u++)
    {
      var profile = profiles.GetChild(u).GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessVolume>();
      UnityEngine.Rendering.PostProcessing.DepthOfField depthOfField = null;
      profile.profile.TryGetSettings(out depthOfField);
      if (depthOfField != null)
      {
        if (_CameraType._value)
        {
          depthOfField.focusDistance.value = 6f;
        }
        else
        {
          depthOfField.focusDistance.value = _CameraZoom == 1 ? 16f : _CameraZoom == 0 ? 10.35f : 22.4f;
        }
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
        Application.targetFrameRate = _UseDefaultTargetFramerate ? -1 : _ScreenResolution.refreshRate;

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
    /*#if UNITY_EDITOR
          return;
    #endif*/
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
}