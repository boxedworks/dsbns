using System.Collections;
using System.Collections.Generic;
using Localization;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameScript : MonoBehaviour
{
  //
  static Settings.SettingsSaveData SettingsModule { get { return Settings.s_SaveData.Settings; } }
  static Settings.LevelSaveData LevelModule { get { return Settings.s_SaveData.LevelData; } }

  // True if steam, false if EOS
  public static bool s_UsingSteam
  {
    get
    {
#if DISABLESTEAMWORKS
      return false;
#else
      return SteamManager._Enabled;
#endif
    }
  }

  //
  public static TextMesh s_DebugText;

  public static GameScript s_Singleton;

  public static bool s_Paused;

  public bool _GameEnded,
    _ExitOpen,
    _IsDemo;

  public Material[] _ItemMaterials;

  public static int s_PlayerIter;

  public static Light s_CameraLight, s_ExitLight;
  public static bool s_ExitLightShow;

  public static AudioSource s_Music, s_SfxRain;
  static AudioSource s_sfxThunder;

  public bool _UseCamera, _X, _Y, _Z;

  public static float s_LevelStartTime;

  public static bool s_EditorEnabled, s_EditorTesting;

  public static int s_LevelSelectColumns = 12, s_LevelSelectRows = 4;
  public static int s_LevelSelectionsPerPage
  {
    get
    {
      return s_LevelSelectRows * s_LevelSelectColumns;
    }
  }

  public static int s_GameId;

  public void OnApplicationQuit()
  {
    // Check window swap
    if (Screen.fullScreen != SettingsModule.Fullscreen)
    {
      SettingsModule.Fullscreen = Screen.fullScreen;
    }
    Settings.SettingsSaveData.Save();

    //if (!Application.isEditor) System.Diagnostics.Process.GetCurrentProcess().Kill();
    Application.Quit();
  }
  public static void OnApplicationQuitS()
  {
    s_Singleton.OnApplicationQuit();
  }

  static float _levelEndTimer;
  public static PlayerScript s_InLevelEndPlayer;
  public static bool _inLevelEnd { get { return s_InLevelEndPlayer != null; } }

  public static void UpdateAmbientLight()
  {
    /*RenderSettings.ambientLight = SettingsModule.Brightness switch
    {
      1 => new Color(0.025f, 0.025f, 0.025f),
      2 => new Color(0.0549f, 0.0549f, 0.0549f),
      3 => new Color(0.09f, 0.09f, 0.09f),
      4 => new Color(0.17f, 0.17f, 0.17f),
      5 => new Color(0.25f, 0.25f, 0.25f),
      6 => new Color(0.32f, 0.32f, 0.32f),
      7 => new Color(0.45f, 0.45f, 0.45f),
      8 => new Color(0.55f, 0.55f, 0.55f),
      9 => new Color(0.65f, 0.65f, 0.65f),

      _ => Color.black
    };*/

    // Color adjustments
    var profile_ = GameResources._Camera_Main.GetComponent<UnityEngine.Rendering.Volume>().profile;
    UnityEngine.Rendering.Universal.ColorAdjustments colorAdj = null;
    if (profile_.TryGet(out colorAdj))
    {
      colorAdj.postExposure.value = SettingsModule.Brightness switch
      {
        1 => -1.2f,
        2 => -0.6f,
        3 => 0f,
        4 => 0.75f,
        5 => 1.5f,
        6 => 2.25f,
        7 => 3f,
        8 => 3.75f,
        9 => 4.5f,

        _ => -2f
      };

      UnityEngine.Rendering.Universal.Bloom bloom = null;
      if (profile_.TryGet(out bloom))
      {
        bloom.scatter.value = SettingsModule.Brightness switch
        {
          4 => 0.35f,
          5 => 0.1f,
          6 => 0f,
          7 => 0f,
          8 => 0f,
          9 => 0f,

          _ => 0.5f
        };
      }

      // Bullets
      if (ItemScript._BulletPool != null)
        foreach (var bullet in ItemScript._BulletPool)
        {
          bullet.SetLightIntensity(SettingsModule.Brightness switch
          {
            4 => 0.8f,
            5 => 0.6f,
            6 => 0.3f,
            7 => 0.1f,
            8 => 0f,
            9 => 0f,

            _ => 1f
          });
        }
    }
  }

  public enum GameModes
  {
    MISSIONS,
    ZOMBIE,

    CHALLENGE,

    PARTY
  }
  public static GameModes s_GameMode;
  public static bool s_IsMissionsGameMode { get { return s_GameMode == GameModes.MISSIONS; } }
  public static bool s_IsZombieGameMode { get { return s_GameMode == GameModes.ZOMBIE; } }
  public static bool s_IsPartyGameMode { get { return s_GameMode == GameModes.PARTY; } }

  public static int s_CrownPlayer, s_CrownEnemy;

  public Input_action _Controls;

  /// <summary>
  /// Holds are information about tutorial such as how to restart or change weapons.
  /// </summary>
  public static class TutorialInformation
  {
    public static bool _HasRestarted // Will be true when the player has restarted the game
    {
      set
      {
        LevelModule.HasRestarted = value;
      }
      get
      {
        return LevelModule.HasRestarted;
      }
    }

    public static Transform _TutorialArrow,
      _Tutorial_Restart_Controller0, _Tutorial_Restart_Controller1,
      _Tutorial_Restart_Keyboard0, _Tutorial_Restart_Keyboard1;

    public static void Init()
    {
      _TutorialArrow = GameObject.Find("TutorialArrow").transform;
      _Tutorial_Restart_Controller0 = GameObject.Find("RestartTutorial0").transform;
      _Tutorial_Restart_Controller1 = GameObject.Find("RestartTutorial1").transform;
      _Tutorial_Restart_Keyboard0 = GameObject.Find("RestartTutorial2").transform;
      _Tutorial_Restart_Keyboard1 = GameObject.Find("RestartTutorial3").transform;
    }
  }

  // Use this for initialization
  void Start()
  {
    s_Singleton = this;

    //
    s_DebugText = GameObject.Find("DebugText").GetComponent<TextMesh>();

    GameResources.Init();
    s_Music = GameObject.Find("Music").GetComponent<AudioSource>();
    FunctionsC.MusicManager.Init();

    //
    Shop.Init();

    //
    Settings.Init();
    Settings.LevelSaveData.Save();
    Settings.SettingsSaveData.Save();

    //
    new LocalizationController();

    //
    FunctionsC.Init();

    //
    SfxManager.Init();
    ControllerManager.Init();
    TutorialInformation.Init();
    SceneThemes.Init();
    ProgressBar.Init();
    Stats.Init();

    UpdateLevelVault();

    SteamManager.SteamMenus.Init();
    //Achievements.Init();
#if UNITY_STANDALONE
    if (s_UsingSteam)
      SteamManager.Workshop_GetUserItems(true);
#endif

    ActiveRagdoll.Rigidbody_Handler.Init();

    var material = GameResources._CameraFader.sharedMaterial;
    var color = material.color;
    color.a = 0f;
    material.color = color;

    // Init loadouts
    Loadout.Init();

    s_CameraLight = GameResources._Camera_Main.transform.GetChild(2).GetComponent<Light>();
    s_ExitLight = GameObject.Find("Spotlight").GetComponent<Light>();

    Transform t = GameObject.Find("Map").transform.GetChild(2);
    t.localPosition = new Vector3(t.localPosition.x, TileManager.Tile._StartY + TileManager.Tile._AddY, t.localPosition.z);

    TileManager._Map = GameObject.Find("Map").transform;
    var navMeshSurfaces = TileManager._Map.GetComponents<Unity.AI.Navigation.NavMeshSurface>();
    TileManager._navMeshSurface = navMeshSurfaces[0];
    TileManager._navMeshSurface2 = navMeshSurfaces[1];
    TileManager.Init();
    TileManager._LoadingMap = true;
    TileManager.LoadMap("5 6 1 1 1 0 1 1 1 1 0 0 0 0 0 0 0 0 0 0 1 1 0 0 0 0 1 1 1 0 1 1 playerspawn_-37.5_-55.62_rot_0 e_-42.5_-48.1_li_knife_w_-42.5_-48.1_l_-41.4_-48.1_canmove_true_canhear_true_ e_-32.5_-48.1_li_knife_w_-32.5_-48.1_l_-33.6_-48.1_canmove_true_canhear_true_ e_-40_-50.6_li_knife_w_-40_-50.6_l_-39.7_-49.4_canmove_true_canhear_true_ e_-35_-44.4_li_knife_w_-35_-44.4_l_-36.1_-44.4_canmove_true_canhear_true_ p_-37.5_-44.38_end_ barrel_-40.66_-42.61_rot_0 barrel_-39.79_-42.62_rot_0 barrel_-38.9_-42.59_rot_0 barrel_-37.96_-42.62_rot_0 barrel_-37.02_-42.55_rot_0 barrel_-40.72_-46.53_rot_0 barrel_-40.73_-45.68_rot_0 barrel_-34.44_-46.61_rot_0 bookcasebig_-35.16_-42.79_rot_15 bookcaseopen_-40.47_-51.45_rot_0 bookcaseopen_-34.37_-51.19_rot_90.00001 bookcaseopen_-34.39_-50_rot_90.00001 bookcaseopen_-40.67_-44.71_rot_90.00001 bookcaseopen_-37.5_-46.88_rot_0 bookcaseopen_-37.5_-47.67_rot_0 barrel_-34.39_-45.78_rot_0 barrel_-31.86_-47.48_rot_0 barrel_-31.84_-48.37_rot_0 barrel_-35.15_-51.47_rot_0 tablesmall_-43.08_-48.95_rot_0 chair_-42.68_-49.08_rot_45 chair_-31.83_-49.12_rot_1.692939E-06 chair_-40.71_-43.66_rot_135 bookcasebig_-38.42_-50.79_rot_285 bookcaseopen_-36.42_-47.9_rot_75 barrel_-39.44_-51.42_rot_0 barrel_-39.25_-50.49_rot_0 candelbig_-36.55_-46.97_rot_90.00001 bookcaseopen_-43.24_-47.83_rot_105", false, false, true);

    TileManager.EditorMenus.Init();
    TileManager.EditorMenus.HideMenus();

    // Init playerprofile and loadouts
    PlayerScript.s_Materials_Ring = new Material[8];
    var mat = Instantiate(TileManager._Ring.gameObject).transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
    for (var i = 0; i < PlayerScript.s_Materials_Ring.Length; i++)
      PlayerScript.s_Materials_Ring[i] = new Material(mat);

    for (var i = 0; i < 4; i++)
      new PlayerProfile();

    SceneThemes.ChangeMapTheme("Black and White");

    // Init menus
    Menu.Init();

    //
    VersusMode.Init();
    SurvivalManager.InitLight();

    // Play menu music
    FunctionsC.MusicManager.PlayTrack(Settings._DifficultyUnlocked == 0 && !Levels._UnlockAllLevels ? 0 : 1);

    //foreach(var gamepad in ControllerManager._Gamepads){
    //  Debug.Log(gamepad.name);
    //}

    s_CrownPlayer = s_CrownEnemy = -1;
  }

  public static Transform _lp0, _lp1, _lp2;

  public static void SpawnPlayers()
  {
    PlayerScript._PLAYERID = 0;
    s_PlayerIter = 0;
    if (PlayerScript.s_Players != null)
    {

      // Count number of players to spawn.. only used if trying to stay persistant or players joined?
      foreach (var p in PlayerScript.s_Players) if (p != null && !p._Ragdoll._IsDead) s_PlayerIter++;

      // Remove null / dead players
      for (var i = PlayerScript.s_Players.Count - 1; i >= 0; i--)
      {
        var p = PlayerScript.s_Players[i];
        if (p == null || p._Ragdoll == null || !p._Ragdoll._IsDead)
          PlayerScript.s_Players.RemoveAt(i);
      }
    }

    // Spawn players
    var rnd = new System.Random();
    var numSpawns = PlayerspawnScript._PlayerSpawns.Count;
    var spawnList = new int[numSpawns];
    for (var i = 0; i < spawnList.Length; i++)
      spawnList[i] = i;
    rnd.Shuffle(spawnList);

    var numPlayers = Settings._NumberPlayers;
    var numTeams = VersusMode.GetNumberTeams();

    if ((s_IsPartyGameMode && !VersusMode.s_Settings._FreeForAll ? numTeams : numPlayers) <= numSpawns || !s_IsPartyGameMode || (s_IsPartyGameMode && VersusMode.s_Settings._FreeForAll))
    {
      var spawnLocation = new int[numPlayers];
      if (s_IsPartyGameMode && !VersusMode.s_Settings._FreeForAll && numPlayers > numSpawns)
      {
        var teamListSpawn = new Dictionary<int, int>();
        var spawnList_ = new List<int>(spawnList);
        for (var i = 0; i < numPlayers; i++)
        {

          var teamId = VersusMode.GetTeamId(i);
          if (teamListSpawn.ContainsKey(teamId))
          {
            var spawnIndex = teamListSpawn[teamId];
            spawnLocation[i] = spawnIndex;
          }
          else
          {
            var spawnIndex = spawnList_[Random.Range(0, spawnList_.Count)];
            spawnList_.Remove(spawnIndex);
            teamListSpawn.Add(teamId, spawnIndex);

            spawnLocation[i] = spawnIndex;
          }

        }
      }
      else
      {
        for (var i = 0; i < numPlayers; i++)
        {
          spawnLocation[i] = spawnList[i % spawnList.Length];
        }
      }

      var playerSpawnIndex = 0;
      if (s_PlayerIter < Settings._NumberPlayers)
        for (; s_PlayerIter < Settings._NumberPlayers; s_PlayerIter++)
          PlayerspawnScript._PlayerSpawns[spawnLocation[playerSpawnIndex++]].SpawnPlayer();
    }

    else
    {

      // Get level data
      //Debug.Log($"numTeams: {numTeams} .. numSpawn: {numSpawns}");

      var spawnOrder = new int[numTeams];
      rnd.Shuffle(spawnList);
      if (spawnOrder.Length <= spawnList.Length)
        for (var i = 0; i < spawnOrder.Length; i++)
          spawnOrder[i] = spawnList[i];

      if (s_PlayerIter < Settings._NumberPlayers)
        for (; s_PlayerIter < Settings._NumberPlayers; s_PlayerIter++)
          PlayerspawnScript._PlayerSpawns[spawnList[VersusMode.GetTeamId(s_PlayerIter) % spawnList.Length]].SpawnPlayer();
    }
  }

  static Coroutine _tutorialCo;
  public static void OnLevelStart()
  {
    IEnumerator tutorialfunction()
    {
      TutorialInformation._TutorialArrow.position = new Vector3(-100f, 0f, 0f);
      Powerup goal = null;
      do
      {
        goal = GameObject.Find("Powerup")?.GetComponent<Powerup>();
        if (goal == null)
          yield return new WaitForSecondsRealtime(0.1f);
      } while (goal == null);
      yield return new WaitForSecondsRealtime(1f);
      var m = TutorialInformation._TutorialArrow.GetComponent<MeshRenderer>();
      float ypos = 1.75f;
      // Tutorial loop
      while (true)
      {
        yield return new WaitForSecondsRealtime(0.01f);

        // Check goal exists
        if (goal == null)
          break;
        var mod = Mathf.PingPong(Time.time * 1f, 1f);
        Vector3 pos;

        // Spawn tutorial arrow after 5 seconds
        if (Time.time - s_LevelStartTime > 5f)
          if (!goal._activated && !goal._activated2)
          {
            pos = goal.transform.position + new Vector3(1.5f + Easings.CircularEaseOut(mod), 0f) + (SettingsModule.UseOrthographicCamera ? new Vector3(0f, 0f, SettingsModule.CameraZoom switch
            {
              Settings.SettingsSaveData.CameraZoomType.AUTO => 0.8f,
              Settings.SettingsSaveData.CameraZoomType.CLOSE => 0.8f,
              Settings.SettingsSaveData.CameraZoomType.NORMAL => -0.2f,
              _ => -1.2f
            }) : Vector3.zero);
            pos.y = ypos;
            TutorialInformation._TutorialArrow.position = pos;
            m.sharedMaterial.mainTextureOffset = new Vector2(0f, Time.time);
            continue;
          }
          else if (!goal._activated2)
          {
            pos = PlayerspawnScript._PlayerSpawns[0].transform.position + new Vector3(1.5f + Easings.CircularEaseOut(mod), 0f) + (SettingsModule.UseOrthographicCamera ? new Vector3(0f, 0f, SettingsModule.CameraZoom switch
            {
              Settings.SettingsSaveData.CameraZoomType.AUTO => -0.8f,
              Settings.SettingsSaveData.CameraZoomType.CLOSE => -0.8f,
              Settings.SettingsSaveData.CameraZoomType.NORMAL => 0.2f,
              _ => 1.2f
            }) : Vector3.zero);
            pos.y = ypos;
            TutorialInformation._TutorialArrow.position += (pos - TutorialInformation._TutorialArrow.position) * 0.05f;
            m.sharedMaterial.mainTextureOffset = new Vector2(0f, Time.time);
            continue;
          }
          else
            break;
      }
      // Remove arrow
      TutorialInformation._TutorialArrow.position = new Vector3(-100f, 0f, 0f);
    }
    // Load tutorial info for level
    if (Levels._CurrentLevelCollectionIndex == 0 && Levels._CurrentLevelIndex == 0 && Settings._DIFFICULTY == 0)
    {
      if (_tutorialCo != null)
      {
        s_Singleton.StopCoroutine(_tutorialCo);
        _tutorialCo = null;
      }
      _tutorialCo = s_Singleton.StartCoroutine(tutorialfunction());
    }
  }

  static float _lastInputCheck;
  static int _HomePressed; // Used for cheat

  // Thunder lightning FX
  [System.NonSerialized]
  public float _Thunder_Last, _Thunder_Samples_Last;
  public Light _Thunder_Light;

  // Update is called once per frame
  public static bool s_Backrooms;
  public static bool s_InteractableObjects { get { return s_IsMissionsGameMode || s_IsPartyGameMode; } }
  public Light _GlobalLight;
  void Update()
  {

    if (s_ExitLightShow)
    {
      if (!s_ExitLight.enabled)
        s_ExitLight.enabled = true;
      s_ExitLight.spotAngle += (40f - Mathf.Sin(Time.time * 4f) * 2.5f - s_ExitLight.spotAngle) * Time.deltaTime * 5f;
      s_ExitLight.innerSpotAngle = s_ExitLight.spotAngle - 6;
    }
    else
      s_ExitLight.spotAngle += (0f - s_ExitLight.spotAngle) * Time.deltaTime * 5f;

    //
    if (!s_IsZombieGameMode)
    {
      // Update level timer
      if (s_Backrooms)
        TileManager._Text_LevelTimer.text = "";
      if (!TileManager._Level_Complete && PlayerScript._TimerStarted)
      {
        TileManager._LevelTimer += Menu.s_InMenus ? Time.unscaledDeltaTime : Time.deltaTime;
        if (!Menu.s_InMenus)
          TileManager._Text_LevelTimer.text = TileManager._LevelTimer.ToStringTimer();
      }

      // If more candles than can handle, hide some
      if (CustomObstacle._CustomCandles != null && CustomObstacle._CustomCandles.Count > 4)
      {
        CustomObstacle.HandleAll();
      }

      // Check backrooms
      if (Debug.isDebugBuild && ControllerManager.GetKey(ControllerManager.Key.B) && ControllerManager.GetKey(ControllerManager.Key.R, ControllerManager.InputMode.HOLD))
      {
        //Screen.SetResolution(720, 1280, true);

        s_Backrooms = true;
        var clipboardData = ClipboardHelper.clipBoard;
        var levelData = clipboardData.EndsWith("loaded map") ? clipboardData : "11 10 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 playerspawn_-29_-51.4_rot_0_ e_-42.9_-32.9_li_knife_w_-42.9_-32.9_l_-43.4_-32.3_canmove_false_canhear_false_ e_-29_-41.8_li_pistol_w_-29_-41.8_l_-29_-40.9_canmove_true_canhear_true_ p_-29.25_-32.75_end_ +unnamed loaded map";
        NextLevel(levelData);
        GameObject.Find("GlobalLight").GetComponent<Light>().enabled = true;
      }
    }

    else
    {
      TileManager._LevelTimer += Time.deltaTime;
      TileManager._Text_LevelTimer.text = TileManager._LevelTimer.ToStringTimer();
    }

    // Update controllers
    ControllerManager.Update();

    // Update enemies
    EnemyScript.UpdateEnemies();

    // Update aoe effects
    FunctionsC.AoeHandler.Update();

    // Update music
    FunctionsC.MusicManager.Update();

    // Update menus
    Menu.UpdateMenus();

    /*/ Update multiplayer
    if (ControllerManager.GetKey(ControllerManager.Key.H))
    {
      s_CustomNetworkManager.Connect();
    }
    else if (ControllerManager.GetKey(ControllerManager.Key.J))
    {
      s_CustomNetworkManager.Host();
    }
    else if (ControllerManager.GetKey(ControllerManager.Key.K))
    {
      s_CustomNetworkManager.StopHost();
    }
    else if (ControllerManager.GetKey(ControllerManager.Key.L))
    {
      s_CustomNetworkManager.StopClient();
    }*/

    // Update playerprofiles
    foreach (var profile in PlayerProfile.s_Profiles)
      profile.HandleInput();

    // Screenshot
    if (ControllerManager.GetKey(ControllerManager.Key.INSERT))
      ScreenCapture.CaptureScreenshot("Screenshot.png");

    if (Debug.isDebugBuild)
    {
      // Next track
      if (ControllerManager.GetKey(ControllerManager.Key.END))
        FunctionsC.MusicManager.TransitionTo(FunctionsC.MusicManager.GetNextTrackIter());
      // Unlock all levels
      if (ControllerManager.GetKey(ControllerManager.Key.HOME))
      {
        Levels._UnlockAllLevels = !Levels._UnlockAllLevels;
      }
    }

    // Check for controller change
    if (Time.unscaledTime - _lastInputCheck > 0.25f)
    {
      _lastInputCheck = Time.unscaledTime;
      Settings._NumberPlayers = ControllerManager._NumberGamepads + (Settings._ForceKeyboard ? 1 : 0);
      if (Settings._NumberPlayers == 0)
        Settings._NumberPlayers = 1;
      var numcontrollers_save = Settings._NumberControllers;
      Settings._NumberControllers = ControllerManager._NumberGamepads + (Settings._ForceKeyboard ? 1 : 0);
      if (Settings._NumberControllers == 0 && Menu._Confirmed_SwitchToKeyboard)
        Settings._NumberControllers = 1;

      if (Settings._NumberControllers != numcontrollers_save)
      {
        var ui = GameResources._UI_Player;
        for (var i = 1; i < 4; i++)
          ui.GetChild(i).gameObject.SetActive(i < Settings._NumberPlayers);

        // Pause if a controller was unplugged and playing
        if (!Menu.s_InMenus && (!s_EditorTesting) && Settings._NumberControllers != PlayerScript.s_Players.Count)
          Menu.OnControllersChanged(Settings._NumberControllers - numcontrollers_save, numcontrollers_save);

        // Check if menu
        if (Menu.s_InMenus && Menu.s_CurrentMenu._Type == Menu.MenuType.VERSUS)
        {
          Menu._CanRender = false;
          Menu.RenderMenu();
        }
      }
    }

    // Update survial mode
    if (s_IsZombieGameMode && !s_EditorEnabled)
      SurvivalManager.Update();

    // Update progress bars
    ProgressBar.Update();

    // Check play mode
    if (!Menu.s_InMenus)
    {
      if (!s_Paused)
      {
        // Update ragdoll body sounds
        ActiveRagdoll.Rigidbody_Handler.Update();

        // Check thunder sfx
        if (SceneThemes._Theme._rain)
        {
          if (Time.time - _Thunder_Last > 10f && Random.value < 0.001f)
          {
            _Thunder_Last = Time.time;

            s_sfxThunder = SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Thunder", 0.6f, 1.05f);

            var cpos = GameResources._Camera_Main.transform.position;
            cpos.y = 0f;
            _Thunder_Light.transform.position = cpos;
          }

          // Lightning timed with thunder volume
          if (
            SettingsModule.UseLightning &&
            _Thunder_Light != null &&
            Time.time - _Thunder_Samples_Last > 0.05f
          )
          {
            var c = 0f;

            if (s_sfxThunder != null && !s_sfxThunder.isPlaying)
            {
              s_sfxThunder = null;
            }
            if (s_sfxThunder?.isPlaying ?? false)
            {
              _Thunder_Samples_Last = Time.time;

              var sample_length = 1024;
              var samples = new float[sample_length];
              s_sfxThunder.clip.GetData(samples, s_sfxThunder.timeSamples);

              var clipLoudness = 0f;
              foreach (var sample in samples)
              {
                clipLoudness += Mathf.Abs(sample);
              }
              clipLoudness /= sample_length;

              c = clipLoudness * 15f;
            }

            //RenderSettings.ambientLight = new Color(c, c, c);
            _Thunder_Light.intensity = c;
          }
        }

        // Check normal mode
        if (!s_IsZombieGameMode && !s_EditorEnabled && !s_IsPartyGameMode)
        {
          // Check endgame
          var timeToEnd = 2.2f;
          if (_inLevelEnd && !_GameEnded && (SettingsModule.LevelCompletionBehavior != Settings.SettingsSaveData.LevelCompletionBehaviorType.NOTHING))
          {
            _levelEndTimer += Time.deltaTime;

            // Check time
            if (/*_levelEndTimer > timeToEnd || */Time.time - _goalPickupTime > 0.8f && EnemyScript.NumberAlive() == 0 && (LevelModule.ExtraCrownMode == 0 || (PlayerScript.GetNumberAlivePlayers() == 1)))
            {

              if (_levelEndTimer > timeToEnd || Levels._CurrentLevelIndex == 0)
              {
                MarkLevelCompleted();
              }

              s_InLevelEndPlayer = null;
              _levelEndTimer = 0f;

              // Check who got the goal
              foreach (var p in PlayerScript.s_Players)
              {
                if (!p._HasExit) { continue; }
                p._HasExit = false;
                break;

              }
              //Debug.Log($"Goal retrieved by {s_CrownPlayer}!");

              // Complete level
              OnLevelComplete();
            }
          }
          else
            _levelEndTimer = Mathf.Clamp(_levelEndTimer - Time.deltaTime, 0f, timeToEnd);
        }

        // Enable editor
        if (!s_EditorTesting)
        {
          if (Debug.isDebugBuild)
          {
            if (ControllerManager.GetKey(ControllerManager.Key.SPACE, ControllerManager.InputMode.HOLD) && ControllerManager.GetKey(ControllerManager.Key.E))
            {
              s_EditorEnabled = !s_EditorEnabled;
              if (s_EditorEnabled) StartCoroutine(TileManager.EditorEnabled());
              else
              {
                string mapdata = TileManager.SaveMap();
                TileManager.EditorDisabled(mapdata);
                // Save map to map file and reload map selections
                TileManager.SaveFileOverwrite(mapdata);
              }
            }

            if (!s_EditorEnabled)
            {
              // Increment survival wave
              if (s_IsZombieGameMode)
              {
                if (ControllerManager.GetKey(ControllerManager.Key.PAGE_UP, ControllerManager.InputMode.DOWN))
                {
                  SurvivalManager._Wave++;
                  Debug.Log($"Survival wave incremented to: {SurvivalManager._Wave}");
                }
                if (ControllerManager.GetKey(ControllerManager.Key.PAGE_DOWN, ControllerManager.InputMode.DOWN))
                {
                  SurvivalManager._Wave--;
                  Debug.Log($"Survival wave incremented to: {SurvivalManager._Wave}");
                }
                if (ControllerManager.GetKey(ControllerManager.Key.INSERT, ControllerManager.InputMode.DOWN))
                {
                  SurvivalManager.GivePoints(0, 100, false);
                }
              }

              else
              {
                if (ControllerManager.GetKey(ControllerManager.Key.BACKQUOTE))
                {
                  // Reset dev level time
                  if (ControllerManager.GetKey(ControllerManager.Key.SHIFT_L, ControllerManager.InputMode.HOLD))
                  {
                    TileManager._LevelTime_Dev = -1f;
                    Debug.Log("Reset dev level time");
                  }

                  // Reset local time
                  LevelModule.SetLevelBestTime(-1f);
                  Settings.LevelSaveData.Save();

                  Debug.Log("Reset best level time");
                  if (Settings._DIFFICULTY == 0)
                    LevelModule.IsTopRatedClassic0 = false;
                  else
                    LevelModule.IsTopRatedClassic1 = false;

                  Levels.BufferLevelTimeDatas();

                  // Reload map
                  ReloadMap(false, true);
                }
              }
            }

          }

          // Next / previous level
          if (!s_EditorEnabled && !s_IsZombieGameMode && (!s_IsPartyGameMode || Debug.isDebugBuild))
          {
            if (!TileManager._LoadingMap)
            {
              if (ControllerManager.GetKey(ControllerManager.Key.PAGE_UP, ControllerManager.InputMode.DOWN))
              {

                // Editor logic
#if UNITY_EDITOR
                if (SettingsModule.LevelCompletionBehavior == Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL)
                  LoadRandomLevel();
                else if (SettingsModule.LevelCompletionBehavior == Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL_ALL)
                  LoadRandomLevel(true);
                else
                {
                  NextLevel(Levels._CurrentLevelIndex + 1);
                  IncrementLevelMenu(1);
                }
                return;
#endif

                // Game logic
                NextLevelSafe();
              }

              // Previous level
              else if (ControllerManager.GetKey(ControllerManager.Key.PAGE_DOWN, ControllerManager.InputMode.DOWN))
              {
                PreviousLevelSafe();
              }
            }
          }

        }

        else
        {

          if (!s_EditorEnabled && Time.unscaledTime - TileManager._EditorSwitchTime > 0.5f)
          {
            if (ControllerManager.GetKey(ControllerManager.Key.F1))
            {
              s_EditorEnabled = true;
              StartCoroutine(TileManager.EditorEnabled());

              TileManager.EditorMenus._Menu_EditorTesting.gameObject.SetActive(false);
            }
          }

          // Check exit
          if (ControllerManager.GetKey(ControllerManager.Key.F2))
          {

            s_EditorTesting = false;

            // If editing, save map
            if (s_EditorEnabled)
            {
              s_EditorEnabled = false;
              TileManager.SaveFileOverwrite(TileManager.SaveMap());
              TileManager.EditorDisabled(null);
            }

            // Exit to menus
            TogglePause(Menu.MenuType.EDITOR_LEVELS);
            TileManager.EditorMenus.HideMenus();
          }

        }

        // Check for scene reset
        if (ControllerManager.GetKey(ControllerManager.Key.BACKSPACE) || ControllerManager.GetKey(ControllerManager.Key.CONTROL_LEFT) || ControllerManager.GetKey(ControllerManager.Key.CONTROL_RIGHT))
        {
          if (!_GameEnded)
          {
            if (ReloadMap(true, true) && !TutorialInformation._HasRestarted)
              TutorialInformation._HasRestarted = true;
          }
        }

        // Check camera change
        if (ControllerManager.GetKey(ControllerManager.Key.F4))
        {
          SettingsModule.UseOrthographicCamera = !SettingsModule.UseOrthographicCamera;
          if (SettingsModule.CameraZoom == Settings.SettingsSaveData.CameraZoomType.AUTO)
            SettingsModule.CameraZoom = Settings.SettingsSaveData.CameraZoomType.NORMAL;
          Settings.SetPostProcessing();
          PlayerScript.ResetCamera();
        }

        if (s_EditorEnabled)
          TileManager.HandleInput();
      }
    }
    else
    {
      foreach (var profile in PlayerProfile.s_Profiles)
        profile.HandleMenuInput();

      // Check if checking for controllers
      if (Menu.s_CurrentMenu._Type == Menu.MenuType.CONTROLLERS_CHANGED)
      {
        var numplayers = ControllerManager._NumberGamepads + (Settings._ForceKeyboard ? 1 : 0);
        if (numplayers >= Menu._Save_NumPlayers)
        {
          Menu.OnControllersChangedFix();
        }
      }
    }
  }

  //
  static float _LastReloadTime;
  public static bool ReloadMap(bool checkReloadtime = true, bool sendMultiplayerInfo = false)
  {
    // Check last reload time
    if (checkReloadtime && Time.time - _LastReloadTime < 0.3f) return false;
    _LastReloadTime = Time.time;

    // Reload the map
    TileManager.ReloadMap();
    return true;
  }

  private void LateUpdate()
  {
    EnemyScript._RAYCOUNT = 0;
  }

  public static bool _Focused;
  private void OnApplicationFocus(bool focus)
  {
    _Focused = focus;

#if UNITY_EDITOR
    return;
#endif

    if (!focus)
      if (!Menu.s_InMenus && !s_EditorEnabled)
        TogglePause();
  }

  static Coroutine _movecam = null;
  static bool s_cameraLightToggle;
  public static void ToggleCameraLight(bool toggle)
  {
    if (toggle && s_CameraLight.intensity != 0f) return;
    else if (!toggle && s_CameraLight.intensity == 0f) return;
    if (_movecam != null) return;

    s_cameraLightToggle = toggle;
    _movecam = s_Singleton.StartCoroutine(ToggleCameraLightCo(toggle));
  }
  public static void ToggleCameraLight()
  {
    ToggleCameraLight(!s_cameraLightToggle);
  }
  static IEnumerator ToggleCameraLightCo(bool toggle)
  {
    if (toggle) s_CameraLight.enabled = true;
    float t = 0f;
    while (t < 1f)
    {
      t = Mathf.Clamp(t + 0.02f, 0f, 1f);
      s_CameraLight.intensity = Mathf.Lerp(0f, 13f, toggle ? t : 1f - t);
      yield return new WaitForSecondsRealtime(0.01f);
    }
    if (!toggle) s_CameraLight.enabled = false;
    _movecam = null;
  }

  public static void ResetObjects()
  {
    EnemyScript.Reset();
    ExplosiveScript.Reset();
    Powerup.Reset();
    ActiveRagdoll.SoftReset();
  }

  public static float _LastPause;
  public static void TogglePause()
  {
    TogglePause(Menu.MenuType.PAUSE);
  }
  public static void TogglePause(Menu.MenuType afterUnlockMenu, bool controllersChanged = false)
  {
    if (Time.unscaledTime < 5f) return;

    if (!Menu.CanPause()) return;

    if (afterUnlockMenu != Menu.MenuType.NONE)
    {
      if (TileManager._LoadingMap) return;
      if (Time.unscaledTime - _LastPause < 0.1f) return;
    }
    _LastPause = Time.unscaledTime;
    s_Paused = !s_Paused;
    if (s_Paused)
    {
      Time.timeScale = 0f;
      Menu.OnPause(afterUnlockMenu);
      System.GC.Collect();
      TileManager._Text_LevelNum.gameObject.SetActive(false);
      TileManager._Text_LevelTimer.gameObject.SetActive(false);
      TileManager._Text_LevelTimer_Best.gameObject.SetActive(false);
      TileManager._Text_GameOver.gameObject.SetActive(false);
      TileManager._Text_Money.gameObject.SetActive(false);
      TileManager.HideMonies();

      if (SettingsModule.HideUI)
        PlayerProfile.ShowAll();
    }
    else
    {
      Time.timeScale = 1f;
      // Check player amount change
      if (s_IsMissionsGameMode)
        if (PlayerScript.s_Players != null && Settings._NumberPlayers != PlayerScript.s_Players.Count) TileManager.ReloadMap();
      TileManager._Text_LevelNum.gameObject.SetActive(true);
      TileManager._Text_LevelTimer.gameObject.SetActive(true);
      if (s_IsMissionsGameMode && !Levels._LevelPack_Playing && !s_EditorTesting)
        TileManager._Text_LevelTimer_Best.gameObject.SetActive(true);
      TileManager._Text_GameOver.gameObject.SetActive(true);
      TileManager._Text_Money.gameObject.SetActive(true);
      TileManager.UnHideMonies();

      if (SettingsModule.HideUI)
        PlayerProfile.HideAll();
    }
    // Toggle text bubbles
    TextBubbleScript.ToggleBubbles(!s_Paused);
  }

  // Determine unlocks for classic mode via JSON save
  public static void UpdateLevelVault()
  {

    var dirsPerLevel = 12;

    // Check difficulty 0
    var numDirs0 = Levels._LevelCollections[0]._levelData.Length / dirsPerLevel;
    for (var i = 0; i < numDirs0; i++)
      if (LevelModule.LevelData[0].Data[(i * dirsPerLevel) + (dirsPerLevel - 1)].Completed)
        Shop.AddAvailableUnlockVault($"classic_{i}");

    // Check difficulty 1
    var numDirs1 = Levels._LevelCollections[1]._levelData.Length / dirsPerLevel;
    for (var i = 0; i < numDirs1; i++)
      if (LevelModule.LevelData[1].Data[(i * dirsPerLevel) + (dirsPerLevel - 1)].Completed)
        Shop.AddAvailableUnlockVault($"classic_{numDirs0 + i}");
  }

  // Fired on last enemy killed
  public static void OnLastEnemyKilled()
  {
    if (s_IsMissionsGameMode)
    {
      s_Singleton._goalPickupTime = Time.time;

      if (LevelModule.ExtraCrownMode == 0 || PlayerScript.GetNumberAlivePlayers() < 2)
        ToggleExitLight(true);

      if (LevelModule.ExtraCrownMode != 0 && s_CrownPlayer == -1 && PlayerScript.GetNumberAlivePlayers() == 1)
        GiveCrownToAlivePlayer();

      // Check achievements
#if UNITY_STANDALONE
      if (LevelModule.ExtraTime == 1 && Settings._Extras_CanUse)
        Achievements.UnlockAchievement(Achievements.Achievement.EXTRA_SUPERH);
#endif
    }
  }

  //
  public static void GiveCrownToAlivePlayer()
  {
    foreach (var player in PlayerScript.s_Players)
    {
      if (player == null || player._Ragdoll == null || player._Ragdoll._health < 1) continue;

      RemoveCrown();

      s_CrownPlayer = player._Profile._Id;
      player._Ragdoll.AddCrown(true);

      break;
    }
  }

  //
  public static void RemoveCrown()
  {
    if (s_CrownPlayer != -1)
      foreach (var player in PlayerScript.s_Players)
      {
        if (player == null || player._Ragdoll == null || player._Ragdoll._IsDead) continue;
        if (player._Profile._Id != s_CrownPlayer) continue;
        player._Ragdoll.RemoveCrown();
        break;
      }
    if (s_CrownEnemy != -1)
      foreach (var enemy in EnemyScript._Enemies_alive)
      {
        if (enemy == null || enemy._Ragdoll == null || enemy._Ragdoll._IsDead) continue;
        if (enemy._Id != s_CrownEnemy) continue;
        enemy._Ragdoll.RemoveCrown();
        break;
      }
    s_CrownPlayer = s_CrownEnemy = -1;
  }

  //
  static public void OnLevelComplete()
  {
    // Check survival
    if (s_IsZombieGameMode) return;

    // Check level pack
    if (Levels._LevelPack_Playing)
    {

      // Check level completion behavior
      switch (SettingsModule.LevelCompletionBehavior)
      {

        // Next level
        case Settings.SettingsSaveData.LevelCompletionBehaviorType.NEXT_LEVEL:
        case Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL:
        case Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL_ALL:

          // Check last level
          if (Levels._CurrentLevelIndex + 1 == Levels._CurrentLevelCollection._levelData.Length)
          {
            Levels._LevelPack_Playing = false;

            TogglePause(Menu.MenuType.EDITOR_PACKS);
            Menu.SwitchMenu(Menu.MenuType.EDITOR_PACKS);
            Menu.s_CurrentMenu._selectionIndex = Levels._LevelPacks_Play_SaveIndex;
            Menu._CanRender = false;
            Menu.RenderMenu();
            _LastPause = Time.unscaledTime - 0.2f;
            Menu.SendInput(Menu.Input.SPACE);
            Menu.SendInput(Menu.Input.SPACE);
            Menu.SendInput(Menu.Input.SPACE);
            _LastPause = Time.unscaledTime;
            return;
          }

          // Load next level
          NextLevel(Levels._CurrentLevelIndex + 1);
          break;

        // Restart level
        case Settings.SettingsSaveData.LevelCompletionBehaviorType.RELOAD_LEVEL:

          TileManager.ReloadMap();
          break;

        // Previous level
        case Settings.SettingsSaveData.LevelCompletionBehaviorType.PREVIOUS_LEVEL:

          // Check last level
          if (Levels._CurrentLevelIndex == 0)
          {
            Levels._LevelPack_Playing = false;

            TogglePause(Menu.MenuType.EDITOR_PACKS);
            Menu.SwitchMenu(Menu.MenuType.EDITOR_PACKS);
            Menu.s_CurrentMenu._selectionIndex = Levels._LevelPacks_Play_SaveIndex;
            Menu._CanRender = false;
            Menu.RenderMenu();
            _LastPause = Time.unscaledTime - 0.2f;
            Menu.SendInput(Menu.Input.SPACE);
            Menu.SendInput(Menu.Input.SPACE);
            Menu.SendInput(Menu.Input.SPACE);
            _LastPause = Time.unscaledTime;
            return;
          }

          // Load previous level
          NextLevel(Levels._CurrentLevelIndex - 1);
          break;
      }

      return;
    }

    // Check mission editor levels
    if (s_EditorTesting)
    {
      ReloadMap();
      return;
    }

    // Display unlock messages
    if (Shop.s_UnlockString != string.Empty)
    {
      var nextMenu = Shop.s_SetLevelsMenuAfterUnlockString ? Menu.MenuType.LEVELS : Menu.MenuType.NONE;
      TogglePause(nextMenu);
      if (nextMenu != Menu.MenuType.NONE)
        return;
    }

    // Check level completion behavior
    switch (SettingsModule.LevelCompletionBehavior)
    {

      // Next level
      case Settings.SettingsSaveData.LevelCompletionBehaviorType.NEXT_LEVEL:
        if (Levels._CurrentLevelIndex + 1 == Levels._CurrentLevelCollection._levelData.Length)
        {
          TogglePause();
          Menu.SwitchMenu(Menu.MenuType.LEVELS);
          return;
        }
        NextLevel(Levels._CurrentLevelIndex + 1);
        IncrementLevelMenu(1);
        return;

      // Reload level
      case Settings.SettingsSaveData.LevelCompletionBehaviorType.RELOAD_LEVEL:
        TileManager.ReloadMap();
        break;

      // Previous level
      case Settings.SettingsSaveData.LevelCompletionBehaviorType.PREVIOUS_LEVEL:
        if (Levels._CurrentLevelIndex == 0)
        {
          TogglePause();
          Menu.SwitchMenu(Menu.MenuType.LEVELS);
          return;
        }
        NextLevel(Levels._CurrentLevelIndex - 1);
        IncrementLevelMenu(-1);
        break;

      // Random level
      case Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL:

        LoadRandomLevel(false);
        break;

      case Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL_ALL:

        LoadRandomLevel(true);
        break;
    }
  }

  // Load a random level
  static void LoadRandomLevel(bool anyDifficulty = false)
  {

#if UNITY_EDITOR
    /*if (anyDifficulty)
      Settings._DIFFICULTY = Levels._CurrentLevelCollectionIndex = Random.Range(0, 2);
    NextLevel(Random.Range(0, Levels._CurrentLevelCollection._levelData.Length));
    return;*/
#endif

    if (anyDifficulty && Settings._DifficultyUnlocked > 0)
    {
      var highestLevelCompleted = 0;
      var levels1 = LevelModule.LevelData[1].Data;
      for (var i = levels1.Count - 1; i >= 0; i--)
      {
        var levelData = levels1[i];
        if (levelData.Completed)
        {
          highestLevelCompleted = i;
          break;
        }
      }

      var randomLevel = Random.Range(0, 131 + highestLevelCompleted + 1);
      if (randomLevel > 131)
      {
        Settings._DIFFICULTY = 1;
        randomLevel -= 131;

      }
      else
        Settings._DIFFICULTY = 0;
      NextLevel(Mathf.Clamp(randomLevel, 0, Settings._LevelsCompleted_Current.Count));
    }

    else
    {
      var highestLevelCompleted = 0;
      var levels = Settings._LevelsCompleted_Current;
      for (var i = levels.Count - 1; i >= 0; i--)
      {
        var levelData = levels[i];
        if (levelData.Completed)
        {
          highestLevelCompleted = i;
          break;
        }
      }
      NextLevel(Mathf.Clamp(Random.Range(0, highestLevelCompleted + 2), 0, levels.Count));
    }
  }

  // Increment level menu
  static void IncrementLevelMenu(int by)
  {
    // Increment menu selector
    Menu.s_SaveLevelSelected += by;
    if (Menu.s_SaveLevelSelected == 12)
    {
      Menu.s_SaveLevelSelected = 0;
      Menu.s_SaveMenuDir++;
    }
    else if (Menu.s_SaveLevelSelected == -1)
    {
      Menu.s_SaveLevelSelected = 11;
      Menu.s_SaveMenuDir--;
    }
  }

  public static bool MarkLevelCompleted()
  {
    // Stat
    //Stats.OverallStats._Levels_Completed++;


#if UNITY_WEBGL
    if (Levels._CurrentLevelIndex >= 32)
    {
      Shop.s_UnlockString += $"- {LocalizationController.GetString("demo_completed")} <color=magenta>{LocalizationController.GetString("demo_checkOutSteam")}</color>\n";
      Shop.s_SetLevelsMenuAfterUnlockString = true;

      return true;
    }
#endif

    // Save level status
    if (!Levels.LevelCompleted(Levels._CurrentLevelIndex))
    {

      if (Levels._CurrentLevelCollectionIndex < 2)
      {
        var levelDat = LevelModule.LevelData[Levels._CurrentLevelCollectionIndex].Data[Levels._CurrentLevelIndex];
        levelDat.Completed = true;
        LevelModule.LevelData[Levels._CurrentLevelCollectionIndex].Data[Levels._CurrentLevelIndex] = levelDat;
      }

      // Check mode-specific unlocks
      if (s_IsMissionsGameMode)
      {
        UpdateLevelVault();

#if UNITY_STANDALONE
        // Check for achievements
        if (Levels._CurrentLevelIndex == 0 && Settings._DIFFICULTY == 0)
        {
          Achievements.UnlockAchievement(Achievements.Achievement.LEVEL_0_COMPLETED);
        }
#endif
      }
    }

    // Check last level completed
    if (Levels._CurrentLevelIndex + 1 == Levels._CurrentLevelCollection._levelData.Length)
    {
      if (Settings._DIFFICULTY == 0)
      {

        // Achievement
#if UNITY_STANDALONE
        Achievements.UnlockAchievement(Achievements.Achievement.DIFFICULTY_1);
#endif

        if (Settings._DifficultyUnlocked <= 0)
        {
          Menu.s_SaveMenuDir = -1;
          Settings._DifficultyUnlocked = 1;
          Shop.s_UnlockString += $"- {LocalizationController.GetString("difficultyUnlocked")} <color=cyan>{LocalizationController.GetString("sneakier")}</color>\n";
          Shop.s_SetLevelsMenuAfterUnlockString = true;
          Menu.s_SetNextDifficultyOnMenu = true;

          return true;
        }
      }
      else if (Settings._DIFFICULTY == 1)
      {

        // Achievement
#if UNITY_STANDALONE
        Achievements.UnlockAchievement(Achievements.Achievement.DIFFICULTY_2);
#endif

        if (Settings._DifficultyUnlocked <= 1)
        {

          Settings._DifficultyUnlocked = 2;

          Shop.s_UnlockString += $"- {LocalizationController.GetString("missionsMode_completed")}\n- {LocalizationController.GetString("missionsMode_tryOtherMode")}";
          Shop.s_SetLevelsMenuAfterUnlockString = true;
          return true;
        }
      }

    }

    return false;
  }

  public static bool NextLevelSafe()
  {

    if (
      s_EditorEnabled ||
      s_EditorTesting ||
      s_IsZombieGameMode ||
      s_IsPartyGameMode ||
      TileManager._LoadingMap
    )
      return false;

    var returnCode = false;

    //
    if (SettingsModule.LevelCompletionBehavior == Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL)
    {
      LoadRandomLevel();
      returnCode = true;
    }

    else if (SettingsModule.LevelCompletionBehavior == Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL_ALL)
    {
      LoadRandomLevel(true);
      returnCode = true;
    }

    //
    else if (Levels._CurrentLevelIndex < (Levels._CurrentLevelCollection?._levelData.Length ?? 0) && Levels.LevelCompleted(Levels._CurrentLevelIndex))
    {
      NextLevel(Levels._CurrentLevelIndex + 1);
      IncrementLevelMenu(1);
      returnCode = true;
    }

    //
    if (returnCode)
      s_CrownEnemy = -1;

    //
    return returnCode;
  }

  public static bool PreviousLevelSafe()
  {

    if (
      s_EditorEnabled ||
      s_EditorTesting ||
      s_IsZombieGameMode ||
      s_IsPartyGameMode ||
      TileManager._LoadingMap
    )
      return false;

    var returnCode = false;

    //
    if (SettingsModule.LevelCompletionBehavior == Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL)
    {
      LoadRandomLevel();
      returnCode = true;
    }

    else if (SettingsModule.LevelCompletionBehavior == Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL_ALL)
    {
      LoadRandomLevel(true);
      returnCode = true;
    }

    //
    else if (Levels._CurrentLevelIndex > 0)
    {
      NextLevel(Levels._CurrentLevelIndex - 1);
      IncrementLevelMenu(-1);
      returnCode = true;
    }

    //
    if (returnCode)
      s_CrownEnemy = -1;

    //
    return returnCode;
  }

  public static void NextLevel(int levelIndex)
  {
    // Unpause if paused
    if (s_Paused) TogglePause();

    // Check last level
    if (levelIndex >= Levels._CurrentLevelCollection._levelData.Length)
      return;

    // Load
    Levels._CurrentLevelIndex = levelIndex;
    NextLevel(Levels._CurrentLevelData);
  }

  public static Coroutine _Coroutine_load;
  public static void NextLevel(string levelData)
  {
    // Sanitize
    //if (levelData == null || levelData.Trim().Length == 0) return;

    // Make sure isn't already loading
    if (_Coroutine_load != null) return;

    // Begin coroutine
    s_Singleton._GameEnded = true;
    _Coroutine_load = s_Singleton.StartCoroutine(NextLevelCo(levelData));
  }
  static IEnumerator NextLevelCo(string levelData)
  {
    s_InLevelEndPlayer = null;

    // Fix players
    if (PlayerScript.s_Players != null)
    {

      // Get player with goal
      PlayerScript hasGoal = null;
      foreach (var p in PlayerScript.s_Players)
      {
        if (p == null || !p._HasExit) continue;
        hasGoal = p;

        // Move player to playerspawn to correct errors
        p.transform.position = PlayerspawnScript._PlayerSpawns[0].transform.position;
        break;
      }
      if (hasGoal != null)
      {
        hasGoal._HasExit = false;

        // Teleport other players to them
        foreach (var p in PlayerScript.s_Players)
        {
          if (p == null || p._Ragdoll._IsDead || p._Id == hasGoal._Id) continue;
          p.transform.position = hasGoal.transform.position;
        }
      }

    }

    // Hide exit
    ToggleExit(false);
    ActiveRagdoll.SoftReset();
    yield return new WaitForSeconds(0.05f);
    TileManager.LoadMap(levelData);
    yield return new WaitForSeconds(0.05f);
    s_Singleton._GameEnded = false;

    // Check for editor enable
    while (TileManager._LoadingMap)
      yield return new WaitForSeconds(0.1f);
    if (TileManager._EnableEditorAfterLoad)
    {
      TileManager._EnableEditorAfterLoad = false;
      s_EditorEnabled = true;
      yield return TileManager.EditorEnabled();
    }
  }

  // When called, the player can exit / complete the level
  public float _goalPickupTime;
  public static void ToggleExit(bool toggle)
  {
    ToggleExitLight(toggle);

    if (s_Singleton._ExitOpen == toggle) return;
    s_Singleton._ExitOpen = toggle;
    if (toggle)
    {
      s_Singleton._goalPickupTime = Time.time;
    }
  }

  public static void ToggleExitLight(bool toggle)
  {
    s_ExitLight.transform.position = PlayerspawnScript._PlayerSpawns[0].transform.position + new Vector3(0f, 3f, 0f);

    if (!s_IsMissionsGameMode && s_GameMode != GameModes.CHALLENGE)
      s_ExitLightShow = false;

    else if (!PlayerScript._TimerStarted)
      s_ExitLightShow = true;

    else if (!EnemyScript.AllDead() || !PlayerScript.HasExit() || (LevelModule.ExtraCrownMode != 0 && PlayerScript.GetNumberAlivePlayers() > 1))
      s_ExitLightShow = false;
    else
      s_ExitLightShow = toggle;
  }

}