using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
  public static CustomNetworkManager s_CustomNetworkManager;

  //
  public static TextMesh s_DebugText;

  public static GameScript s_Singleton;

  public static bool s_Paused;

  public bool _GameEnded,
    _ExitOpen,
    _IsDemo;

  public static int s_PlayerIter;

  public static Light s_CameraLight, s_ExitLight;
  public static bool s_ExitLightShow;

  public static AudioSource s_Music, s_SfxRain;
  static AudioSource s_sfxThunder;

  public bool _UseCamera, _X, _Y, _Z;

  public Material[] _item_materials;

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
    RenderSettings.ambientLight = SettingsModule.Brightness switch
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
    };
  }

  public enum GameModes
  {
    CLASSIC,
    SURVIVAL,

    CHALLENGE,

    VERSUS
  }
  public static GameModes s_GameMode;

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

    //
    GameResources.Init();
    Shop.Init();
    FunctionsC.Init();
    s_Music = GameObject.Find("Music").GetComponent<AudioSource>();
    FunctionsC.MusicManager.Init();

    //
    Settings.Init();
    Settings.LevelSaveData.Save();
    Settings.SettingsSaveData.Save();

    //
    SfxManager.Init();
    ControllerManager.Init();
    TutorialInformation.Init();
    SceneThemes.Init();
    ProgressBar.Init();
    Stats.Init();

    UpdateLevelVault();

    SteamManager.SteamMenus.Init();
    //SteamManager.Achievements.Init();
#if UNITY_STANDALONE
    if (s_UsingSteam)
      SteamManager.Workshop_GetUserItems(true);
#endif

    ActiveRagdoll.Rigidbody_Handler.Init();

    var material = GameResources._CameraFader.sharedMaterial;
    var color = material.color;
    color.a = 0f;
    material.color = color;

    // Network
    s_CustomNetworkManager = GameObject.Find("Networking").GetComponent<CustomNetworkManager>();

    // Init loadouts
    ItemManager.Loadout.Init();

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
    PlayerScript.s_Materials_Ring = new Material[4];
    var mat = Instantiate(TileManager._Ring.gameObject).transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
    for (var i = 0; i < PlayerScript.s_Materials_Ring.Length; i++)
      PlayerScript.s_Materials_Ring[i] = new Material(mat);

    new PlayerProfile();
    new PlayerProfile();
    new PlayerProfile();
    new PlayerProfile();

    SceneThemes.ChangeMapTheme("Black and White");

    // Init menus
    Menu.Init();

    //
    VersusMode.Init();
    SurvivalMode.InitLight();

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

    if ((s_GameMode == GameModes.VERSUS && !VersusMode.s_Settings._FreeForAll ? numTeams : numPlayers) <= numSpawns || s_GameMode != GameModes.VERSUS || (s_GameMode == GameModes.VERSUS && VersusMode.s_Settings._FreeForAll))
    {
      var spawnLocation = new int[numPlayers];
      if (s_GameMode == GameModes.VERSUS && !VersusMode.s_Settings._FreeForAll && numPlayers > numSpawns)
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
      if (!s_CustomNetworkManager._Connected || s_CustomNetworkManager._IsServer)
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

      if (!s_CustomNetworkManager._Connected || s_CustomNetworkManager._IsServer)
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
              Settings.SettingsSaveData.CameraZoomType.FAR => -1.2f
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
              Settings.SettingsSaveData.CameraZoomType.FAR => 1.2f
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

  //
  public static class SurvivalMode
  {
    public static bool _WavePlaying;
    public static int _Wave;

    public static float _Time_wave_intermission,
      _Timer_wave_start;

    public static int _Number_enemies_spawned;

    public static Vector3 _AllSpawn;

    public static ItemManager.Loadout[] s_PlayerLoadouts;
    static int[] _PlayerScores, _PlayerScores_Total;

    static public List<System.Tuple<GameObject, float>> _Money;

    public static TMPro.TextMeshPro _Text_Scores, _Text_Wave;

    // Holds the enemies to spawned this wave. In order
    public static Queue<EnemyScript.SurvivalAttributes> _Wave_enemies;
    public enum EnemyType
    {
      KNIFE_WALK_SLOW,
      KNIFE_WALK,
      KNIFE_JOG,
      KNIFE_RUN,
      KNIFE_BEEFY_SLOW,
      KNIFE_BEEFY_WALK,

      GRENADE_JOG,

      PISTOL_WALK,

      SET_ALLSPAWN,
      REMOVE_ALLSPAWN,

      ARMORED
    }

    public static List<int> _EnabledSpawners;

    public static void InitLight()
    {
      s_PlayerLoadouts = new ItemManager.Loadout[4];
      for (var i = 0; i < s_PlayerLoadouts.Length; i++)
      {
        s_PlayerLoadouts[i] = new ItemManager.Loadout();
        s_PlayerLoadouts[i]._Equipment = new PlayerProfile.Equipment();
        s_PlayerLoadouts[i]._two_weapon_pairs = true;
        OnPlayerDead(i);
      }
    }

    public static void Init()
    {
      _WavePlaying = false;
      _Wave = 0;

      _Time_wave_intermission = 3f;
      _Timer_wave_start = -1f;

      EnemyScript._MAX_RAGDOLLS_ALIVE = 30;

      _Wave_enemies = new Queue<EnemyScript.SurvivalAttributes>();

      // Reset local stats
      Stats.Reset_Local();

      // Assign starting player loadouts
      s_PlayerLoadouts = new ItemManager.Loadout[4];
      for (var i = 0; i < s_PlayerLoadouts.Length; i++)
      {
        s_PlayerLoadouts[i] = new ItemManager.Loadout();
        s_PlayerLoadouts[i]._Equipment = new PlayerProfile.Equipment();
        s_PlayerLoadouts[i]._two_weapon_pairs = true;
        OnPlayerDead(i);
      }

      _EnabledSpawners = new List<int>();
      _PlayerScores = new int[4];
      _PlayerScores_Total = new int[4];

      //for (var i = 0; i < _PlayerScores.Length; i++)
      //  _PlayerScores[i] = 1000;

      _Text_Scores = GameObject.Find("PlayerScores").transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
      _Text_Scores.gameObject.SetActive(true);

      _Text_Wave = GameObject.Find("Wave").transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
      _Text_Wave.gameObject.SetActive(true);
      _Text_Wave.text = "0";

      // Hide candle
      if (CustomObstacle._CustomCandles != null)
        foreach (var candle in CustomObstacle._CustomCandles)
          candle.gameObject.GetComponent<CandleScript>().Off();

      // Open starting room
      var starting_room = 0;
      if (CustomObstacle._PlayerSpawn != null)
        starting_room = CustomObstacle._PlayerSpawn._index;
      OpenRoom(starting_room, starting_room);

      // Reset closest spawns
      _ClosestSpawns = null;

      // Music
      if (FunctionsC.MusicManager.s_CurrentTrack != 3)
      {
        FunctionsC.MusicManager.s_CurrentTrack = 0;
        FunctionsC.MusicManager.TransitionTo(FunctionsC.MusicManager.GetNextTrackIter());
      }
    }

    public static void AddMoney(GameObject g)
    {
      if (_Money == null)
        _Money = new List<System.Tuple<GameObject, float>>();

      _Money.Add(System.Tuple.Create(g, Time.time));
    }

    // Revert player loadout to starting loadout
    public static void OnPlayerDead(int playerId)
    {
      s_PlayerLoadouts[playerId]._Equipment._UtilitiesLeft = new UtilityScript.UtilityType[] { UtilityScript.UtilityType.GRENADE, };
      s_PlayerLoadouts[playerId]._Equipment._UtilitiesRight = new UtilityScript.UtilityType[0];
      s_PlayerLoadouts[playerId]._Equipment._ItemLeft0 = ItemManager.Items.KNIFE;
      s_PlayerLoadouts[playerId]._Equipment._ItemRight0 = ItemManager.Items.NONE;
      s_PlayerLoadouts[playerId]._Equipment._ItemLeft1 = ItemManager.Items.NONE;
      s_PlayerLoadouts[playerId]._Equipment._ItemRight1 = ItemManager.Items.NONE;

      s_PlayerLoadouts[playerId]._Equipment._Perks.Clear();

      // Check for unlocks
      if (PlayerScript._All_Dead)
      {

      }
    }

    public static void OnLeaveMode()
    {
      _Text_Scores?.gameObject.SetActive(false);
      _Text_Wave?.gameObject.SetActive(false);

      if (_Money != null)
      {
        foreach (var pair in _Money)
        {
          var g = pair.Item1;
          if (g != null)
            Destroy(g);
        }
        _Money = null;
      }
    }

    public static void OnWaveStart()
    {
      // Respawn players and heal
      s_PlayerIter = 0;
      if (PlayerScript.s_Players != null)
      {
        PlayerScript alive_player = null;
        for (var i = 0; i < Settings._NumberPlayers; i++)
        {
          //if (i >= PlayerScript._Players.Count) continue;
          var p = PlayerScript.s_Players[i];
          if (p == null || p._Ragdoll == null || p._Ragdoll._IsDead) continue;
          alive_player = p;
          break;
        }

        // Check for an alive player
        if (alive_player == null) return;

        // Count number of players to spawn.. only used if trying to stay persistant or players joined?
        foreach (var p in PlayerScript.s_Players) if (p != null && !p._Ragdoll._IsDead) s_PlayerIter++;

        // Remove null / dead players
        for (var i = PlayerScript.s_Players.Count - 1; i >= 0; i--)
        {
          var p = PlayerScript.s_Players[i];
          if (p == null || p._Ragdoll == null || p._Ragdoll._IsDead)
          {
            ActiveRagdoll.s_Ragdolls.Remove(p._Ragdoll);
            GameObject.Destroy(p.transform.parent.gameObject);
            PlayerScript.s_Players.RemoveAt(i);
            PlayerspawnScript._PlayerSpawns[0].SpawnPlayer((playerScript) =>
            {
              playerScript.transform.position = alive_player.transform.position;
              playerScript.transform.parent.gameObject.SetActive(true);
              SfxManager.PlayAudioSourceSimple(playerScript.transform.position, "Ragdoll/Pop", 0.9f, 1.05f);
            }, false);
          }
        }

        // Heal and reload all players
        foreach (var p in PlayerScript.s_Players)
          if (p != null && p._Ragdoll != null)
            HealPlayer(p);
      }

      // Increment wave
      _Wave++;
      _Text_Wave.text = $"{_Wave}";

      _AllSpawn = Vector3.zero;

      SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Survival/Wave_Start");

      _Number_enemies_spawned = 0;

      var max_zombies = 0;
      void LocalQueue(EnemyType type, int number, int waitForSize = 1)
      {
        if (max_zombies <= 0) return;
        max_zombies -= number;

        QueueEnemyType(type, number, waitForSize);
      }

      /*void RandomSpecialWave()
      {
        var type = Random.value <= 0.5f ? EnemyType.GRENADE_JOG : EnemyType.PISTOL_WALK;
        var modifier = 0.6f;
        var waitFor = (modifier * _Wave) * 0.8f;
        QueueEnemyType(type, Mathf.RoundToInt(modifier * _Wave), Mathf.RoundToInt(waitFor));
      }*/

      // Populate wave with enemies
      switch (_Wave)
      {
        case (1):
          SpawnLogic._SpawnTime = 1.2f;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 7);
          break;
        case (2):
          SpawnLogic._SpawnTime = 1.1f;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 12);
          break;
        case (3):
          SpawnLogic._SpawnTime = 1f;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 6);
          QueueEnemyType(EnemyType.KNIFE_WALK, 4);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 6);
          break;
        case (4):
          SpawnLogic._SpawnTime = 0.9f;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 5, 5);
          QueueEnemyType(EnemyType.KNIFE_WALK, 5, 3);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 5);
          QueueEnemyType(EnemyType.KNIFE_WALK, 3);
          break;
        //case (5):
        //  RandomSpecialWave();
        //  break;
        case (5):
          SpawnLogic._SpawnTime = 0.8f;
          EnemyScript._MAX_RAGDOLLS_ALIVE = 35;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 3);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 5, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK, 5);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 8);
          QueueEnemyType(EnemyType.KNIFE_WALK, 3);
          break;
        case (6):
          SpawnLogic._SpawnTime = 0.7f;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 5);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 8, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK, 5);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 6);
          QueueEnemyType(EnemyType.KNIFE_WALK, 3);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 6);
          QueueEnemyType(EnemyType.KNIFE_WALK, 5);
          break;
        case (7):
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 5);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 6, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 10, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK, 5);
          break;
        case (8):
          SpawnLogic._SpawnTime = 0.65f;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 8, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK, 5);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          break;
        //case (10):
        //  RandomSpecialWave();
        //  break;
        case (9):
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 15, 20);
          QueueEnemyType(EnemyType.KNIFE_WALK, 7);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          break;
        case (10):
          SpawnLogic._SpawnTime = 0.6f;
          EnemyScript._MAX_RAGDOLLS_ALIVE = 50;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 15, 20);
          QueueEnemyType(EnemyType.KNIFE_WALK, 8);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          QueueEnemyType(EnemyType.KNIFE_WALK, 8);
          break;
        case (11):
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20, 15);
          QueueEnemyTypeGroup(EnemyType.KNIFE_WALK_SLOW, 10, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK, 10);
          break;
        case (12):
          SpawnLogic._SpawnTime = 0.5f;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20, 15);
          QueueEnemyTypeGroup(EnemyType.KNIFE_WALK_SLOW, 10, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          QueueEnemyType(EnemyType.KNIFE_WALK, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 15, 10);
          QueueEnemyTypeGroup(EnemyType.KNIFE_WALK_SLOW, 10, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 10);
          break;
        //case (15):
        //RandomSpecialWave();
        //break;
        case (13):
          QueueEnemyType(EnemyType.KNIFE_WALK, 10, 5);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20, 20);
          QueueEnemyType(EnemyType.KNIFE_WALK, 10, 5);
          QueueEnemyTypeGroup(EnemyType.KNIFE_WALK_SLOW, 20, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15);
          break;
        case (14):
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 15, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK, 7);
          QueueEnemyType(EnemyType.KNIFE_JOG, 3);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 25, 20);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15);
          QueueEnemyType(EnemyType.KNIFE_JOG, 3);
          QueueEnemyType(EnemyType.ARMORED, 1, 13);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 15);
          break;
        case (15):
          SpawnLogic._SpawnTime = 0.45f;
          EnemyScript._MAX_RAGDOLLS_ALIVE = 60;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20, 25);
          QueueEnemyType(EnemyType.KNIFE_WALK, 10);
          QueueEnemyType(EnemyType.KNIFE_JOG, 5);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15);
          QueueEnemyType(EnemyType.KNIFE_JOG, 5);
          QueueEnemyType(EnemyType.ARMORED, 3, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 17);
          QueueEnemyType(EnemyType.KNIFE_WALK, 13);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20);
          break;
        case (16):
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30, 35);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15);
          QueueEnemyType(EnemyType.KNIFE_JOG, 7);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15);
          QueueEnemyType(EnemyType.KNIFE_JOG, 7);
          QueueEnemyType(EnemyType.ARMORED, 6, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          break;
        //case (20):
        // RandomSpecialWave();
        // break;
        case (17):
          SpawnLogic._SpawnTime = 0.4f;
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30, 45);
          QueueEnemyType(EnemyType.KNIFE_WALK, 5);
          QueueEnemyType(EnemyType.KNIFE_JOG, 10);
          QueueEnemyType(EnemyType.ARMORED, 3, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15, 20);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20);
          QueueEnemyType(EnemyType.KNIFE_JOG, 5, 15);
          QueueEnemyType(EnemyType.ARMORED, 7, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 35);
          QueueEnemyType(EnemyType.KNIFE_WALK, 20, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 35);
          break;
        case (18):
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 35, 45);
          QueueEnemyType(EnemyType.KNIFE_WALK, 7);
          QueueEnemyType(EnemyType.KNIFE_JOG, 10, 15);
          QueueEnemyType(EnemyType.ARMORED, 3, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 25);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15, 25);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 25);
          QueueEnemyType(EnemyType.KNIFE_JOG, 5, 15);
          QueueEnemyType(EnemyType.ARMORED, 7, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 15, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK, 20);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 50);
          break;
        case (19):
          QueueEnemyType(EnemyType.KNIFE_WALK, 15, 20);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 50);
          QueueEnemyType(EnemyType.ARMORED, 3, 15);
          QueueEnemyType(EnemyType.KNIFE_JOG, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 25);
          QueueEnemyType(EnemyType.KNIFE_JOG, 5, 15);
          QueueEnemyType(EnemyType.ARMORED, 8, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          QueueEnemyType(EnemyType.KNIFE_WALK, 25);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 50);
          break;
        case (20):
          SpawnLogic._SpawnTime = 0.35f;
          EnemyScript._MAX_RAGDOLLS_ALIVE = 75;
          QueueEnemyType(EnemyType.ARMORED, 3, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15, 50);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30, 40);
          QueueEnemyType(EnemyType.KNIFE_JOG, 10, 5);
          QueueEnemyTypeGroup(EnemyType.ARMORED, 6, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
          QueueEnemyType(EnemyType.KNIFE_WALK, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20);
          QueueEnemyType(EnemyType.KNIFE_WALK, 10);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20);
          QueueEnemyType(EnemyType.ARMORED, 8, 15);
          QueueEnemyType(EnemyType.KNIFE_JOG, 5, 15);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 35);
          QueueEnemyType(EnemyType.KNIFE_WALK, 30);
          QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 60);
          break;
        //case (25):
        //  RandomSpecialWave();
        //  break;
        default:
          var mod = Mathf.Clamp((_Wave - 20f) * 0.1f, 1f, 5f);
          max_zombies = (200 * (int)mod);
          while (max_zombies > 0)
          {
            QueueEnemyType(EnemyType.KNIFE_WALK, 15, 50);
            max_zombies -= 50;
            QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30, 40);
            QueueEnemyType(EnemyType.KNIFE_JOG, 10, 5);
            QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20);
            max_zombies -= 60;
            if (max_zombies <= 0) break;
            QueueEnemyType(EnemyType.KNIFE_JOG, 10, 15);
            if (Random.value < 0.2f) if (_AllSpawn != Vector3.zero)
                QueueEnemyType(EnemyType.SET_ALLSPAWN);
              else
                QueueEnemyType(EnemyType.REMOVE_ALLSPAWN);
            QueueEnemyType(EnemyType.ARMORED, 5, 15);
            QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 35);
            max_zombies -= 50;
            QueueEnemyType(EnemyType.KNIFE_WALK, 30);
            max_zombies -= 30;
            QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 60);
            max_zombies -= 60;
            if (_Wave > 12)
            {
              //if (Random.value * 5f < 2f)
              //  LocalQueue(_Wave > 20 && (Random.value * 1f) > 0.5f ? EnemyType.KNIFE_WALK : EnemyType.KNIFE_WALK_SLOW, (int)(Mathf.Clamp((20) * mod, 5f, EnemyScript._MAX_RAGDOLLS_ALIVE - 5)), (int)(Mathf.Clamp((20) * mod, 0f, EnemyScript._MAX_RAGDOLLS_ALIVE - 10)));
              LocalQueue(EnemyType.KNIFE_JOG, 10, 22);
            }
            if (_Wave > 25)
            {
              QueueEnemyType(EnemyType.ARMORED, 5, 15);
              QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30, 35);
              max_zombies -= 50;
              QueueEnemyType(EnemyType.KNIFE_JOG, 15, 20);
              QueueEnemyType(EnemyType.KNIFE_RUN, 5, 10);
              max_zombies -= 30;
              SpawnLogic._SpawnTime = 0.3f;
              EnemyScript._MAX_RAGDOLLS_ALIVE = 100;
            }
            if (_Wave > 30)
            {
              SpawnLogic._SpawnTime = 0.2f;
              EnemyScript._MAX_RAGDOLLS_ALIVE = 120;
            }
            if (_Wave > 35)
            {
              SpawnLogic._SpawnTime = 0.1f;
              EnemyScript._MAX_RAGDOLLS_ALIVE = 150;
            }
          }
          break;
      }

      //
      _WavePlaying = true;

      _Timer_wave_start = Time.time;
    }

    public static void OnWaveEnd()
    {
      _Time_wave_intermission = 4f;

      _WavePlaying = false;

      // Get / save highest wave
      var highestWave = LevelModule.GetHighestSurvivalWave();
      if (_Wave > highestWave)
      {
        LevelModule.SetHighestSurvivalWave(_Wave);
        Settings.LevelSaveData.Save();
      }

      // Check survival achievements
      // Map 1
      if (Levels._CurrentLevelIndex == 0)
      {
        if (_Wave == 10)
        {

#if UNITY_STANDALONE
          SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.SURVIVAL_MAP0_10);
#endif

          if (highestWave < 10)
          {
            // Unlock next map
            TogglePause();
            Menu.PlayNoise(Menu.Noise.PURCHASE);
            Menu.GenericMenu(new string[] {
$@"<color={Menu._COLOR_GRAY}>new survival map unlocked</color>

you survived 10 waves and have unlocked a <color=yellow>new survival map</color>!
"
        }, "nice", Menu.MenuType.NONE, null, true, null,
            (Menu.MenuComponent c) =>
            {
              TogglePause();
              Menu.HideMenus();
            });
          }
        }

        else if (_Wave == 20)
        {
#if UNITY_STANDALONE
          SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.SURVIVAL_MAP0_20);
#endif
        }
      }

      // Map 2
      else if (Levels._CurrentLevelIndex == 1)
      {
        if (_Wave == 10)
        {
#if UNITY_STANDALONE
          SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.SURVIVAL_MAP1_10);
#endif
        }
        else if (_Wave == 20)
        {
#if UNITY_STANDALONE
          SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.SURVIVAL_MAP1_20);
#endif
        }
      }


      // Set random
      CustomObstacle.Randomize();

      // Record stat
      //Stats.OverallStats._Waves_Played++;

      // Respawn players
      s_PlayerIter = 0;
      if (PlayerScript.s_Players != null)
      {
        PlayerScript alive_player = null;
        for (var i = 0; i < Settings._NumberPlayers; i++)
        {
          //if (i >= PlayerScript._Players.Count) continue;
          var p = PlayerScript.s_Players[i];
          if (p == null || p._Ragdoll == null || p._Ragdoll._IsDead) continue;
          alive_player = p;
          break;
        }

        // Check for an alive player
        if (alive_player == null) return;

        // Count number of players to spawn.. only used if trying to stay persistant or players joined?
        foreach (var p in PlayerScript.s_Players) if (p != null && !p._Ragdoll._IsDead) s_PlayerIter++;

        // Remove null / dead players
        for (var i = PlayerScript.s_Players.Count - 1; i >= 0; i--)
        {
          var p = PlayerScript.s_Players[i];
          if (p == null || p._Ragdoll == null || p._Ragdoll._IsDead)
          {
            ActiveRagdoll.s_Ragdolls.Remove(p._Ragdoll);
            GameObject.Destroy(p.transform.parent.gameObject);
            PlayerScript.s_Players.RemoveAt(i);
            PlayerspawnScript._PlayerSpawns[0].SpawnPlayer((playerScript) =>
            {
              playerScript.transform.position = alive_player.transform.position;
              playerScript.transform.parent.gameObject.SetActive(true);
              SfxManager.PlayAudioSourceSimple(playerScript.transform.position, "Ragdoll/Pop", 0.9f, 1.05f);
            }, false);
          }
        }

        // Remove old items / utilities
        var objects = GameResources._Container_Objects;
        for (var i = objects.childCount - 1; i >= 0; i--)
        {
          var obj = objects.GetChild(i);
          if (obj.name == "BookcaseOpen" || obj.name == "Interactable") continue;
          GameObject.Destroy(obj.gameObject);
        }

        // Heal and reload all players
        foreach (var p in PlayerScript.s_Players)
          if (p != null && p._Ragdoll != null)
          {
            HealPlayer(p);
            p.RegisterUtilities();
            p._Profile.UpdateIcons();
          }
      }
    }

    static void HealPlayer(PlayerScript p)
    {
      // Heal; check for armor perk
      //var healed = false;
      if (Shop.Perk.HasPerk(p._Id, Shop.Perk.PerkType.ARMOR_UP))
      {
        if (p._Ragdoll._health != 5)
        {
          p._Ragdoll._health = 5;
          p._Ragdoll.AddArmor();
          //healed = true;
        }
      }
      else if (p._Ragdoll._health != 3)
      {
        p._Ragdoll._health = 3;
        //healed = true;
      }

      // Particle system and noise if healed
      /*if (healed)
      {
        var particles = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.HEAL)[0];
        particles.transform.position = p._ragdoll._transform_parts._hip.position;
        particles.Play();
      }*/

      // Update UI
      p._Profile.UpdateHealthUI();
    }

    public static void SetAllSpawn()
    {
      var room_id = _EnabledSpawners[Random.Range(0, _EnabledSpawners.Count)];
      var spawner_list = CustomObstacle._CustomSpawners[room_id];
      _AllSpawn = spawner_list[Random.Range(0, spawner_list.Count)].transform.position;
    }
    public static void RemoveAllSpawn()
    {
      _AllSpawn = Vector3.zero;
    }

    static List<Vector3> _Spawn_Points;
    static Dictionary<Vector3, float> _ClosestSpawns;
    static int _ClosestSpawns_SpawnIter, _ClosestSpawns_PlayerIter, _Money_Iter;

    // change path
    static int _EnemyIndex;
    static UnityEngine.AI.NavMeshPath _EnemyPath;
    static float _LastIncrementalUpdate;
    static void IncrementalUpdate()
    {
      //
      if (PlayerScript.s_Players == null) return;

      /*#if DEBUG
            if (_ClosestSpawns != null)
            {
              List<Vector3> closest_spawns = System.Linq.Enumerable.ToList(_ClosestSpawns.Keys);
              foreach (var point in closest_spawns)
                Debug.DrawLine(PlayerScript._Players[0].transform.position, point, Color.yellow);
            }
#endif*/

      /// Compile list of possible spawn points
      {
        if (_Spawn_Points == null)
          _Spawn_Points = new List<Vector3>();
        else
          _Spawn_Points.Clear();
        if (_AllSpawn != Vector3.zero && Random.value <= 0.9f)
          _Spawn_Points.Add(_AllSpawn);
        else
        {
          foreach (var i in _EnabledSpawners)
            if (CustomObstacle._CustomSpawners.ContainsKey(i))
              foreach (var spawn in CustomObstacle._CustomSpawners[i])
                _Spawn_Points.Add(spawn.transform.position);
        }
        if (_Spawn_Points.Count == 0) throw new System.NullReferenceException("No valid spawn points for survial enemy");
      }

      /// Check for closest spawns
      {
        // Init array
        if (_ClosestSpawns == null) _ClosestSpawns = new Dictionary<Vector3, float>();

        // Gather player and spawn
        var player = PlayerScript.s_Players[_ClosestSpawns_PlayerIter++ % PlayerScript.s_Players.Count];
        var spawn = _Spawn_Points[_ClosestSpawns_SpawnIter++ % _Spawn_Points.Count];

        if (player._Ragdoll != null && !player._Ragdoll._IsDead)
        {
          // Calculate distance
          UnityEngine.AI.NavMeshQueryFilter filter = new UnityEngine.AI.NavMeshQueryFilter();
          var path = new UnityEngine.AI.NavMeshPath();
          filter.areaMask = 1;
          filter.agentTypeID = TileManager._navMeshSurface2.agentTypeID;

          // Calculate length from spawn to player
          if (player == null) return;
          UnityEngine.AI.NavMesh.CalculatePath(player.transform.position, spawn, filter, path);
          var dist = FunctionsC.GetPathLength(path.corners);

          /// Compare to other spawners
          // Else, check if already in dict
          if (_ClosestSpawns.ContainsKey(spawn))
            _ClosestSpawns[spawn] = dist;
          // Check if dict empty
          else if (_ClosestSpawns.Count < Mathf.Clamp(Mathf.RoundToInt(PlayerScript.s_Players.Count * 1.4f), 2, 5))
            _ClosestSpawns.Add(spawn, dist);
          // Else, compare
          else
          {
            var remove_entry = Vector3.zero;
            foreach (var entry in _ClosestSpawns)
            {
              if (dist > entry.Value) continue;
              remove_entry = entry.Key;
              _ClosestSpawns.Add(spawn, dist);
              break;
            }
            if (remove_entry != Vector3.zero)
              _ClosestSpawns.Remove(remove_entry);
          }
        }
      }

      /// Check for old money
      {
        if (_Money != null && _Money.Count > 0)
        {
          var index = _Money_Iter++ % _Money.Count;
          var pair = _Money[index];
          if (Time.time - pair.Item2 > 45f)
          {
            _Money.RemoveAt(index);
            GameObject.Destroy(pair.Item1);
          }
        }
      }

      /// Check enemy lining up
      // Check for null
      {
        if (EnemyScript._Enemies_alive == null || EnemyScript._Enemies_alive.Count == 0) return;

        _LastIncrementalUpdate = Time.time;

        // Get next enemy
        var enemy_next = EnemyScript._Enemies_alive[_EnemyIndex++ % EnemyScript._Enemies_alive.Count];
        var enemy_ragdoll = enemy_next._Ragdoll;

        enemy_ragdoll.ToggleRaycasting(false);

        if (Random.Range(0, 16) == 0f)
          enemy_next.SetRandomStrafe();

        // Check if others in front
        var hit = new RaycastHit();
        if (Physics.SphereCast(new Ray(enemy_ragdoll._Hip.transform.position, MathC.Get2DVector(enemy_ragdoll._Hip.transform.forward) * 100f), 0.5f, out hit, 100f, GameResources._Layermask_Ragdoll))
        {
          enemy_ragdoll.ToggleRaycasting(true);
          if (hit.distance > 10f) return;
          var ragdoll = ActiveRagdoll.GetRagdoll(hit.collider.gameObject);
          if (ragdoll == null || ragdoll._IsPlayer) return;

          enemy_ragdoll._ForceGlobal += enemy_ragdoll._Hip.transform.right * (enemy_next._strafeRight ? 1f : -1f) * Time.deltaTime * 3f;
          return;
        }

        enemy_ragdoll.ToggleRaycasting(true);
      }
    }

    // Open a room and enable its spawners based on index
    public static void OpenRoom(int index, int index2)
    {
      // Enable spawners
      if (!_EnabledSpawners.Contains(index))
        _EnabledSpawners.Add(index);

      // Enable candle
      if (CustomObstacle._CustomCandles != null)
        foreach (var candle in CustomObstacle._CustomCandles)
          if (candle._index == index || candle._index == index2)
          {
            var can = candle.gameObject.GetComponent<CandleScript>();
            can._NormalizedEnable = 0f;
            can.On();
          }

      // Enable / disable other interactables
      if (CustomObstacle._CustomInteractables != null)
      {
        var remove = new List<CustomObstacle>();
        foreach (var interactable in CustomObstacle._CustomInteractables)
          // Check for buttons to the same door just opened; remove them
          if (interactable._type == CustomObstacle.InteractType.REMOVEBARRIER && interactable._index == index2 && interactable._index2 == index)
          {
            interactable.gameObject.SetActive(false);
            remove.Add(interactable);
          }
          // Set other interactables to visible
          else if (interactable._index2 == index)
            interactable.gameObject.SetActive(true);
        foreach (var interactable in remove)
          CustomObstacle._CustomInteractables.Remove(interactable);
      }

      // Remove barriers
      if (CustomObstacle._CustomBarriers != null)
      {
        foreach (var barrier in CustomObstacle._CustomBarriers)
          if ((barrier._index == index && barrier._index2 == index2) || (barrier._index2 == index && barrier._index == index2))
          {
            barrier.gameObject.GetComponent<UnityEngine.AI.NavMeshObstacle>().enabled = false;
            var collider = barrier.gameObject.GetComponent<BoxCollider>();
            var bounds = collider.bounds;
            bounds.size *= 0.7f;
            bounds.extents *= 0.7f;
            collider.enabled = false;
            var rb = barrier.gameObject.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.useGravity = false;
            rb.linearDamping = 0.1f;
            rb.mass = 0.5f;
            rb.AddForce(new Vector3(0f, 1f, 0f) * (350f + Random.value * 200f));
            rb.AddTorque(new Vector3(1f - (Random.value * 2f), 1f - (Random.value * 2f), 1f - (Random.value * 2f)) * (250f + Random.value * 200f));
            GameObject.Destroy(rb.gameObject, 10f);
          }
      }
    }

    public static void IncrementScore(int playerId)
    {
      _PlayerScores[playerId]++;
      _PlayerScores_Total[playerId]++;
    }
    public static bool HasPoints(int playerId, int points)
    {
      return _PlayerScores[playerId] >= points;
    }
    public static void SpendPoints(int playerId, int points)
    {
      _PlayerScores[playerId] -= points;
    }
    public static void GivePoints(int playerId, int points, bool recordPoints = false)
    {
      if (_PlayerScores == null || playerId > _PlayerScores.Length - 1) return;
      _PlayerScores[playerId] += points;
      if (recordPoints) _PlayerScores_Total[playerId] += points;
    }
    public static int GetTotalPoints(int playerId)
    {
      return _PlayerScores_Total[playerId];
    }

    static class SpawnLogic
    {
      public static float _LastSpawnTime,
        _NextSpawnTime,
        _SpawnTime;
    }

    public static void Update()
    {
      if (!_Text_Scores) return;

#if DEBUG
      if (ControllerManager.GetKey(ControllerManager.Key.COMMA))
        _PlayerScores[0] = 1000;
#endif

      // Update score text
      var text = "";
      if (!s_Paused && !Menu.s_InMenus)
        for (var i = 0; i < Settings._NumberPlayers; i++)
          text += $"<color={PlayerProfile.s_Profiles[i].GetColorName(false)}>p{i + 1}:</color> {_PlayerScores[i]}\n";
      _Text_Scores.text = text;

      // Check if paused or in menu
      if (s_Paused || Menu.s_InMenus || CustomObstacle._CustomSpawners == null) return;

      IncrementalUpdate();

      CustomObstacle.HandleAll();

      // Check if wave in progress
      if (_WavePlaying)
      {
        // Check for all players dead
        if (PlayerScript._All_Dead)
        {
          _WavePlaying = false;

          // Save highest wave
          //Debug.Log($"{Levels._CurrentLevelIndex} : {_Wave - 1}");

          return;
        }
        // If no more enemies to spawn and no more enemies alive, end wave
        if (EnemyScript._Enemies_alive != null && _Wave_enemies.Count == 0 && EnemyScript._Enemies_alive.Count == 0)
        {
          OnWaveEnd();
        }
        // Try to spawn an enemy
        else if (_Wave_enemies.Count > 0 &&
          (EnemyScript._Enemies_alive == null || EnemyScript._Enemies_alive.Count < EnemyScript._MAX_RAGDOLLS_ALIVE))
        {
          if (Time.time > SpawnLogic._NextSpawnTime)
          {
            // Spawn enemy
            EnemyScript e = null;
            var survivalAttributes = _Wave_enemies.Dequeue();
            if (
              survivalAttributes._enemyType != EnemyType.SET_ALLSPAWN &&
              survivalAttributes._enemyType != EnemyType.REMOVE_ALLSPAWN
              )
            {
              survivalAttributes._WaitForEnemyHordeSizeStart = _Number_enemies_spawned;
              {
                var dest = Vector3.zero;
                // Spawn using closest spawns
                if (_ClosestSpawns.Count > 0 && Random.value < 0.6f)
                {
                  var closest_spawns = System.Linq.Enumerable.ToList(_ClosestSpawns.Keys);
                  dest = closest_spawns[Random.Range(0, closest_spawns.Count)];
                }
                // Spawn using random spawn
                else
                  dest = _Spawn_Points[Random.Range(0, _Spawn_Points.Count)];

                var startPos = new Vector2(dest.x - 0.5f + Random.value, dest.z - 0.5f + Random.value);
                e = EnemyScript.SpawnEnemyAt(survivalAttributes, startPos);
              }

              _Number_enemies_spawned++;
            }

            // Change enemy per type
            var movespeed = 0f;
            var health = 1;
            switch (survivalAttributes._enemyType)
            {
              case (EnemyType.KNIFE_WALK_SLOW):
                movespeed = 0.25f;
                break;
              case (EnemyType.KNIFE_WALK):
              case (EnemyType.PISTOL_WALK):
                movespeed = 0.5f;
                break;
              case (EnemyType.SET_ALLSPAWN):
                SetAllSpawn();
                break;
              case (EnemyType.REMOVE_ALLSPAWN):
                RemoveAllSpawn();
                break;
              case (EnemyType.KNIFE_JOG):
              case (EnemyType.GRENADE_JOG):
                movespeed = 0.7f;
                break;
              case (EnemyType.KNIFE_RUN):
                movespeed = 1f;
                break;
              case (EnemyType.KNIFE_BEEFY_SLOW):
                movespeed = 0.15f;
                health = 4;
                e?._Ragdoll.ChangeColor((Color.red + Color.yellow) / 3f);//new Color(0, 30, 0));
                //e.GetRagdoll()._hip.transform.localScale *= 1.6f;
                break;
              case (EnemyType.ARMORED):
                movespeed = 0.25f;
                health = 4;
                e._Ragdoll.ChangeColor(Color.gray);
                break;
            }

            if (e != null)
            {
              e._moveSpeed = movespeed + (-0.1f + Random.value * 0.2f);
              e._Ragdoll._health = health;
            }

            SpawnLogic._NextSpawnTime = Time.time + SpawnLogic._SpawnTime;
          }
        }
        return;
      }
      // Else, check intermission
      // If intermission time is up, start next wave
      if (_Time_wave_intermission < 0f && !PlayerScript._All_Dead)
      {
        OnWaveStart();
      }
      _Time_wave_intermission -= Time.deltaTime;
    }

    static void QueueEnemyTypeGroup(params System.Tuple<EnemyType, int, int>[] enemy_data)
    {
      QueueEnemyType(EnemyType.SET_ALLSPAWN);
      foreach (var data in enemy_data)
        QueueEnemyType(data.Item1, data.Item2, data.Item3);
      QueueEnemyType(EnemyType.REMOVE_ALLSPAWN);
    }

    static void QueueEnemyTypeGroup(params System.Tuple<EnemyType, int>[] enemy_data)
    {
      QueueEnemyType(EnemyType.SET_ALLSPAWN);
      foreach (var data in enemy_data)
        QueueEnemyType(data.Item1, data.Item2);
      QueueEnemyType(EnemyType.REMOVE_ALLSPAWN);
    }

    static void QueueEnemyTypeGroup(EnemyType enemy_type, int group_size, int waitForSize = 1)
    {
      QueueEnemyTypeGroup(System.Tuple.Create(enemy_type, group_size, waitForSize));
    }

    static void QueueEnemyType(EnemyType enemy_type, int number = 1, int waitForSize = 1)
    {
      // Balance...
      number = Mathf.CeilToInt(number * 0.75f);
      waitForSize = Mathf.CeilToInt(number * 0.75f);

      // Clamp
      waitForSize = Mathf.Clamp(waitForSize, 0, EnemyScript._MAX_RAGDOLLS_ALIVE - 2);

      if (Settings._NumberPlayers > 1)
        number = Mathf.Clamp(Mathf.RoundToInt(number * (1f + Settings._NumberPlayers * 0.4f)), 0, EnemyScript._MAX_RAGDOLLS_ALIVE - 1);
      for (var i = 1; i < number; i++)
      {
        _Wave_enemies.Enqueue(new EnemyScript.SurvivalAttributes()
        {
          _enemyType = enemy_type,
          _WaitForEnemyHordeSize = (waitForSize - i) % waitForSize
        });
      }
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
  public static bool s_InteractableObjects { get { return s_GameMode == GameModes.CLASSIC || s_GameMode == GameModes.VERSUS; } }
  public Light _GlobalLight;
  void Update()
  {

    if (s_ExitLightShow)
    {
      if (!s_ExitLight.enabled)
        s_ExitLight.enabled = true;
      s_ExitLight.spotAngle += ((40f - Mathf.Sin(Time.time * 4f) * 2.5f) - s_ExitLight.spotAngle) * Time.deltaTime * 5f;
      s_ExitLight.innerSpotAngle = s_ExitLight.spotAngle - 6;
    }
    else
      s_ExitLight.spotAngle += (0f - s_ExitLight.spotAngle) * Time.deltaTime * 5f;

    //
    if (!IsSurvival())
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

    if (s_CustomNetworkManager._Connected)
    {
      var mapData = s_CustomNetworkManager._CurrentMapData;
      if (mapData != null && mapData.Trim().Length > 0 && !s_Paused && !TileManager._LoadingMap)
      {
        s_CustomNetworkManager._CurrentMapData = null;
        NextLevel(mapData);
      }
    }

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
      Settings._NumberPlayers = (ControllerManager._NumberGamepads) + (Settings._ForceKeyboard ? 1 : 0);
      if (s_CustomNetworkManager._Connected)
        Settings._NumberPlayers += s_CustomNetworkManager._Players.Count - 1;
      if (Settings._NumberPlayers == 0)
        Settings._NumberPlayers = 1;
      var numcontrollers_save = Settings._NumberControllers;
      Settings._NumberControllers = (ControllerManager._NumberGamepads) + (Settings._ForceKeyboard ? 1 : 0);
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
    if (IsSurvival() && !s_EditorEnabled)
      SurvivalMode.Update();

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
        if (!IsSurvival() && !s_EditorEnabled && s_GameMode != GameModes.VERSUS)
        {
          // Check endgame
          var timeToEnd = 2.2f;
          if (_inLevelEnd && !_GameEnded && (SettingsModule.LevelCompletionBehavior != Settings.SettingsSaveData.LevelCompletionBehaviorType.NOTHING))
          {
            _levelEndTimer += Time.deltaTime;

            // Check time
            if (_levelEndTimer > timeToEnd || (EnemyScript.NumberAlive() == 0 && Time.time - _goalPickupTime > 0.8f))
            {

              if (_levelEndTimer > timeToEnd || Levels._CurrentLevelIndex == 0)
              {
                MarkLevelCompleted();
              }

              s_InLevelEndPlayer = null;
              _levelEndTimer = 0f;

              // Check who got the goal
              s_CrownPlayer = -1;
              foreach (var p in PlayerScript.s_Players)
              {
                if (!p._HasExit) { continue; }
                p._HasExit = false;
                s_CrownPlayer = p._Profile._Id;
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
              if (IsSurvival())
              {
                if (ControllerManager.GetKey(ControllerManager.Key.PAGE_UP, ControllerManager.InputMode.DOWN))
                {
                  SurvivalMode._Wave++;
                  Debug.Log($"Survival wave incremented to: {SurvivalMode._Wave}");
                }
                if (ControllerManager.GetKey(ControllerManager.Key.PAGE_DOWN, ControllerManager.InputMode.DOWN))
                {
                  SurvivalMode._Wave--;
                  Debug.Log($"Survival wave incremented to: {SurvivalMode._Wave}");
                }
                if (ControllerManager.GetKey(ControllerManager.Key.INSERT, ControllerManager.InputMode.DOWN))
                {
                  SurvivalMode.GivePoints(0, 100, false);
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
          if (!s_EditorEnabled && !IsSurvival() && (s_GameMode != GameModes.VERSUS || Debug.isDebugBuild))
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
        var numplayers = (ControllerManager._NumberGamepads) + (Settings._ForceKeyboard ? 1 : 0);
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

  // Assists with controls
  public class PlayerProfile
  {
    static int s_currentSettingsID;
    public static int s_CurrentSettingsProfileID
    {
      get
      {
        return s_currentSettingsID;
      }
      set
      {
        s_currentSettingsID = value % 4;
        if (s_currentSettingsID < 0)
          s_currentSettingsID = 3;
      }
    }
    public static PlayerProfile s_CurrentSettingsProfile { get { return s_Profiles[s_CurrentSettingsProfileID]; } }

    static int s_iD;
    public static PlayerProfile[] s_Profiles;

    public int _Id;
    public Settings.SettingsSaveData.PlayerProfile _profileSettings { get { return SettingsModule.PlayerProfiles[_Id]; } }

    // The player this profile is attatched to
    public PlayerScript _Player
    {
      get
      {
        if (PlayerScript.s_Players == null) return null;
        foreach (var player in PlayerScript.s_Players)
          if (player == null) continue;
          else if (player._Id == _Id) return player;
        return null;
      }
    }

    public float[] _directionalAxis;

    public int _LoadoutIndex
    {
      get { return _profileSettings.LoadoutIndex; }
      set
      {
        if (Levels._HardcodedLoadout != null && !s_EditorTesting) return;
        if (s_GameMode != GameModes.CLASSIC) return;
        if (_Player?._Ragdoll?._grappling ?? false) return;

        // Locate valid loadout to equip
        var iter = ItemManager.Loadout._Loadouts.Length;
        var difference = value - _LoadoutIndex;

        var profileSettings = _profileSettings;
        var setIndex = profileSettings.LoadoutIndex;
        while (iter >= 0)
        {
          setIndex = (setIndex + difference) % ItemManager.Loadout._Loadouts.Length;
          if (setIndex < 0) setIndex = ItemManager.Loadout._Loadouts.Length + setIndex;

          var equipment = ItemManager.Loadout._Loadouts[setIndex]._Equipment;
          if (!equipment.IsEmpty()) break;
          iter--;
        }

        {
          var equipment = ItemManager.Loadout._Loadouts[setIndex]._Equipment;
          if (iter == -1 && equipment.IsEmpty())
            setIndex = 0;
        }

        // Set loadout index
        profileSettings.LoadoutIndex = setIndex;
        SettingsModule.UpdatePlayerProfile(_Id, profileSettings);
        UpdateIcons();

        // If in loadout menu, update colors
        if (Menu.s_InMenus)
        {
          Menu.PlayNoise(Menu.Noise.LOADOUT_SWAP);
          if (Menu.s_CurrentMenu._Type == Menu.MenuType.SELECT_LOADOUT)
            Menu.TriggerActionSwapTo(Menu.MenuType.SELECT_LOADOUT);
        }
      }
    }
    public Equipment _Equipment
    {
      get
      {
        return _Loadout._Equipment;
      }
    }
    public ItemManager.Loadout _Loadout
    {
      get
      {
        // Level packs
        if (Levels._HardcodedLoadout != null && !GameScript.s_EditorTesting) return Levels._HardcodedLoadout;

        // VERSUS mode
        if (s_GameMode == GameModes.VERSUS) return VersusMode.s_PlayerLoadouts;

        // SURVIVAL mode
        if (s_GameMode == GameModes.SURVIVAL && SurvivalMode.s_PlayerLoadouts != null) return SurvivalMode.s_PlayerLoadouts[_Id];

        // CLASSIC mode
        if (_LoadoutIndex > ItemManager.Loadout._Loadouts.Length)
          _LoadoutIndex = 0;
        return ItemManager.Loadout._Loadouts[_LoadoutIndex];
      }
    }

    public bool _reloadSidesSameTime
    {
      get { return _profileSettings.ReloadSameTime; }
      set
      {
        var profileSettings = _profileSettings;
        profileSettings.ReloadSameTime = value;
        SettingsModule.UpdatePlayerProfile(_Id, profileSettings);
      }
    }

    ItemManager.Items GetItem(ItemManager.Items item, ItemManager.Items other)
    {
      if (item == ItemManager.Items.NONE && !Shop.IsActuallyTwoHanded(other) && _Equipment._Perks.Contains(Shop.Perk.PerkType.MARTIAL_ARTIST))
        return ItemManager.Items.FIST;
      return item;
    }

    public ItemManager.Items _ItemLeft
    {
      get
      {
        var item = _EquipmentIndex == 0 ? _Equipment._ItemLeft0 : _Equipment._ItemLeft1;
        var itemOther = _EquipmentIndex == 0 ? _Equipment._ItemRight0 : _Equipment._ItemRight1;
        return GetItem(item, itemOther);
      }
      set { if (_EquipmentIndex == 0) _Equipment._ItemLeft0 = value; else _Equipment._ItemLeft1 = value; }
    }
    public ItemManager.Items _ItemRight
    {
      get
      {
        var item = _EquipmentIndex == 0 ? _Equipment._ItemRight0 : _Equipment._ItemRight1;
        var itemOther = _EquipmentIndex == 0 ? _Equipment._ItemLeft0 : _Equipment._ItemLeft1;
        return GetItem(item, itemOther);
      }
      set { if (_EquipmentIndex == 0) _Equipment._ItemRight0 = value; else _Equipment._ItemRight1 = value; }
    }
    public ItemManager.Items _ItemLeft_Other
    {
      get
      {
        var item = _EquipmentIndex == 1 ? _Equipment._ItemLeft0 : _Equipment._ItemLeft1;
        var itemOther = _EquipmentIndex == 1 ? _Equipment._ItemRight0 : _Equipment._ItemRight1;
        return GetItem(item, itemOther);
      }
      set { if (_EquipmentIndex == 1) _Equipment._ItemLeft0 = value; else _Equipment._ItemLeft1 = value; }
    }
    public ItemManager.Items _ItemRight_Other
    {
      get
      {
        var item = _EquipmentIndex == 1 ? _Equipment._ItemRight0 : _Equipment._ItemRight1;
        var itemOther = _EquipmentIndex == 1 ? _Equipment._ItemLeft0 : _Equipment._ItemLeft1;
        return GetItem(item, itemOther);
      }
      set { if (_EquipmentIndex == 1) _Equipment._ItemRight0 = value; else _Equipment._ItemRight1 = value; }
    }

    // Used for two sets of equipment
    int equipmentIndex;
    public int _EquipmentIndex
    {
      get { return equipmentIndex; }
      set { equipmentIndex = value % 2; }
    }

    public class Equipment
    {
      public Equipment()
      {
        _UtilitiesLeft = new UtilityScript.UtilityType[0];
        _UtilitiesRight = new UtilityScript.UtilityType[0];

        _Perks = new List<Shop.Perk.PerkType>();
      }
      public ItemManager.Items _ItemLeft0, _ItemRight0, _ItemLeft1, _ItemRight1;

      public UtilityScript.UtilityType[] _UtilitiesLeft, _UtilitiesRight;
      public List<Shop.Perk.PerkType> _Perks;

      public bool HasWeapons0()
      {
        return _ItemLeft0 != ItemManager.Items.NONE ||
          _ItemRight0 != ItemManager.Items.NONE;
      }
      public bool HasWeapons1()
      {
        return _ItemLeft1 != ItemManager.Items.NONE ||
          _ItemRight1 != ItemManager.Items.NONE;
      }

      public bool IsEmpty()
      {
        return !HasWeapons0() && !HasWeapons1() &&
          _UtilitiesLeft.Length == 0 &&
          _UtilitiesRight.Length == 0 &&
          _Perks.Count == 0;
      }
    }

    public static Color[] s_PlayerColors = new Color[] { Color.blue, Color.red, Color.yellow, Color.cyan, Color.green, Color.magenta, new Color(1f, 0.4f, 0f), Color.white, Color.black };
    public Color GetColor()
    {
      return s_PlayerColors[_profileSettings.Color];
    }
    public string GetColorName(bool visual = true)
    {
      switch (_profileSettings.Color)
      {
        case 0:
          return "blue";
        case 1:
          return "red";
        case 2:
          return "yellow";
        case 3:
          return visual ? "cyan" : "#00FFFF";
        case 4:
          return "green";
        case 5:
          return visual ? "magenta" : "#FF00FF";
        case 6:
          return "orange";
        case 7:
          return "white";
        case 8:
          return "black";
      }
      return "";
    }

    public bool _faceMovement
    {
      get
      {
        return _profileSettings.FaceLookDirection;
      }
      set
      {
        var profileSettings = _profileSettings;
        profileSettings.FaceLookDirection = value;
        SettingsModule.UpdatePlayerProfile(_Id, profileSettings);
      }
    }

    public int _playerColor
    {
      get
      {
        return _profileSettings.Color;
      }
      set
      {

        var profileSettings = _profileSettings;

        // Clamp
        profileSettings.Color = value % s_PlayerColors.Length;
        if (profileSettings.Color < 0)
          profileSettings.Color = s_PlayerColors.Length - 1;

        // Save
        SettingsModule.UpdatePlayerProfile(_Id, profileSettings);

        // Update UI
        var ui = GameResources._UI_Player;
        ui.GetChild(_Id).GetChild(0).GetComponent<TextMesh>().color = GetColor();

        // Check for alive player
        if (PlayerScript.s_Players != null)
          foreach (PlayerScript p in PlayerScript.s_Players)
            if (p._Id == _Id && !p._Ragdoll._IsDead) p._Ragdoll.ChangeColor(GetColor());

        // Update UI
        CreateHealthUI(_Player == null ? 1 : _Player._Ragdoll._health);
      }
    }
    Transform _ui { get { return GameResources._UI_Player.GetChild(_Id); } }
    public Transform _VersusUI;
    TMPro.TextMeshPro _loadoutIndexText;

    MeshRenderer[] _health_UI, _perk_UI;

    public PlayerProfile()
    {
      _Id = s_iD++;
      _VersusUI = _ui.GetChild(6);
      _loadoutIndexText = _ui.GetChild(7).GetComponent<TMPro.TextMeshPro>();

      if (s_Profiles == null) s_Profiles = new PlayerProfile[4];
      s_Profiles[_Id] = this;

      _directionalAxis = new float[3];

      // Check empty loadout
      ChangeLoadoutIfEmpty();

      // Update profile equipment icons
      _playerColor = _playerColor;
      UpdateIcons();
      CreateHealthUI(1);
    }

    // Check empty loadout and switch to a new one if empty
    public void ChangeLoadoutIfEmpty(int max_loadout = 4)
    {
      // Bounds-check _loadoutIndex
      if (max_loadout != 4 && _LoadoutIndex > max_loadout)
        _LoadoutIndex = 0;
      else if (_Equipment.IsEmpty())
        _LoadoutIndex++;

      // Update UI
      UpdateIcons();
    }

    public void HandleMenuInput()
    {

      // Accommodate for _ForceKeyboard; checking for -1 controllerID and controllerID > Gamepad.all.Count
      if (
        Settings._ForceKeyboard && _Id == 0 ||
        _Id + (Settings._ForceKeyboard ? -1 : 0) >= ControllerManager._NumberGamepads
        )
      {
        _menuDownTime_down = _menuDownTime_up = 0f;

        // Check color change
        if (ControllerManager.GetKey(ControllerManager.Key.ONE, ControllerManager.InputMode.DOWN))
        {
          var playerColor = _playerColor;
          playerColor--;
          if (playerColor < 0)
            playerColor = s_PlayerColors.Length - 1;
          _playerColor = playerColor;
        }
        if (ControllerManager.GetKey(ControllerManager.Key.TWO, ControllerManager.InputMode.DOWN))
          _playerColor = ++_playerColor % s_PlayerColors.Length;

        return;
      }

      // Check axis hold
      if (!(_menuDownTime_down != 0f && _menuDownTime_up != 0f))
      {
        if (_menuDownTime_down != 0f && Time.unscaledTime - _menuDownTime_down > 0.09f)
        {
          _menuDownTime_down = Time.unscaledTime;

          Down();
        }
        else if (_menuDownTime_up != 0f && Time.unscaledTime - _menuDownTime_up > 0.09f)
        {
          _menuDownTime_up = Time.unscaledTime;

          Up();
        }
      }

      var gamepad = ControllerManager.GetPlayerGamepad(_Id);
      if (gamepad != null)
      {

        // Check color change
        if (gamepad.leftShoulder.wasPressedThisFrame)
        {
          var playerColor = _playerColor;
          playerColor--;
          if (playerColor < 0)
            playerColor = s_PlayerColors.Length - 1;
          _playerColor = playerColor;
        }
        if (gamepad.rightShoulder.wasPressedThisFrame)
          _playerColor = ++_playerColor % s_PlayerColors.Length;

        // Check axis selections
        for (var i = 0; i < 2; i++)
        {
          var y = 0f;
          switch (i)
          {
            case 0:
              y = ControllerManager.GetControllerAxis(_Id, ControllerManager.Axis.LSTICK_Y);
              break;
            case 1:
              y = ControllerManager.GetControllerAxis(_Id, ControllerManager.Axis.DPAD_Y);
              break;
          }

          if (y > 0.75f)
          {
            if (_directionalAxis[i] <= 0f)
            {
              _directionalAxis[i] = 1f;

              SetDownTime(false);
            }
          }

          else if (y < -0.75f)
          {
            if (_directionalAxis[i] >= 0f)
            {
              _directionalAxis[i] = -1f;

              SetDownTime(true);
            }
          }

          else
          {
            if (_directionalAxis[i] != 0f)
            {
              _directionalAxis[i] = 0f;

              SetUpTime(true);
              SetUpTime(false);
            }
          }
        }
      }

    }

    //
    float _menuDownTime_down, _menuDownTime_up;
    public void SetDownTime(bool down)
    {
      if (down)
      {
        if (_menuDownTime_down == 0f)
          Down();

        _menuDownTime_down = Time.unscaledTime + 0.4f;
      }
      else
      {
        if (_menuDownTime_up == 0f)
          Up();

        _menuDownTime_up = Time.unscaledTime + 0.4f;
      }
    }
    public void SetUpTime(bool down)
    {
      if (down)
        _menuDownTime_down = 0f;
      else
        _menuDownTime_up = 0f;
    }

    //
    public void HandleInput()
    {

      // Accommodate for _ForceKeyboard; checking for -1 controllerID and controllerID > Gamepad.all.Count
      if (Settings._ForceKeyboard && _Id == 0 || _Id + (Settings._ForceKeyboard ? -1 : 0) >= ControllerManager._NumberGamepads)
      {

        // Check loadout change
        if (ControllerManager.GetKey(ControllerManager.Key.Z))
        {
          if (Menu.s_CurrentMenu._Type == Menu.MenuType.VERSUS)
          {
            if (Menu.s_InMenus)
              VersusMode.IncrementPlayerTeam(_Id, -1);
          }
          else
            _LoadoutIndex--;
        }
        if (ControllerManager.GetKey(ControllerManager.Key.C))
        {
          if (Menu.s_CurrentMenu._Type == Menu.MenuType.VERSUS)
          {
            if (Menu.s_InMenus)
              VersusMode.IncrementPlayerTeam(_Id, 1);
          }
          else
            _LoadoutIndex++;
        }

        return;
      }

      // Check in menus
      if (!Menu.s_InMenus)
        return;

      // Check axis selections
      var gamepad = ControllerManager.GetPlayerGamepad(_Id);
      if (gamepad.dpad.left.wasPressedThisFrame)
      {
        if (Menu.s_CurrentMenu._Type == Menu.MenuType.VERSUS)
          VersusMode.IncrementPlayerTeam(_Id, -1);
        else
          _LoadoutIndex--;
      }
      if (gamepad.dpad.right.wasPressedThisFrame)
      {
        if (Menu.s_CurrentMenu._Type == Menu.MenuType.VERSUS)
          VersusMode.IncrementPlayerTeam(_Id, 1);
        else
          _LoadoutIndex++;
      }
    }

    void Up()
    {
      Menu.SendInput(Menu.Input.UP);
      FunctionsC.OnControllerInput();
    }
    void Down()
    {
      Menu.SendInput(Menu.Input.DOWN);
      FunctionsC.OnControllerInput();
    }

    // Create health icons
    static Color _ExtraHealthColor = Color.gray;
    public void CreateHealthUI(int health)
    {
      // Remove health UI if already present
      //if (_health_UI != null && _health_UI.Length > 0)
      //  RemoveHealthUI();

      // Create primitives
      //_health_UI = new GameObject[health];
      _health_UI = new MeshRenderer[] { _ui.GetChild(3).gameObject.GetComponent<MeshRenderer>(), _ui.GetChild(4).gameObject.GetComponent<MeshRenderer>(), _ui.GetChild(5).gameObject.GetComponent<MeshRenderer>() };
      foreach (var renderer in _health_UI)
      {
        var color = GetColor();
        renderer.material.color = color;
        //render.material.SetColor("_EmissionColor", color);
      }
      UpdateHealthUI(health);
      /*var scale = 0.46f / health;
      var x_offset = Mathf.LerpUnclamped(-0.02f, -0.2f, (health - 1f) / 4f);
      for (var i = 0; i < health; i++)
      {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.transform.parent = _UI;
        g.transform.localPosition = new Vector3(x_offset + scale * i, -0.29f, 0f);
        g.transform.localScale = new Vector3(scale * Mathf.LerpUnclamped(1f, 0.8f, (health - 1f) / 4f), 0.08f, 0.001f);
        g.transform.localEulerAngles = Vector3.zero;
        g.layer = 11;
        g.SetActive(true);
        var render = g.GetComponent<MeshRenderer>();
        Resources.UnloadAsset(render.sharedMaterial);
        render.sharedMaterial = PlayerScript._Materials_Ring[_id];
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var color = GetColor();
        render.sharedMaterial.color = color;
        render.sharedMaterial.SetColor("_EmissionColor", color);
        _health_UI[i] = g;
      }*/
    }

    // Remove health icons
    public void RemoveHealthUI()
    {
      if (_health_UI != null && _health_UI.Length > 0)
      {
        for (int i = _health_UI.Length - 1; i >= 0; i--)
          GameObject.Destroy(_health_UI[i]);
        _health_UI = null;
      }
    }

    public void UpdateHealthUI()
    {
      UpdateHealthUI(_Player._Ragdoll._health);
    }
    public void UpdateHealthUI(int health)
    {
      for (int i = 0; i < _health_UI.Length; i++)
      {
        _health_UI[i].gameObject.SetActive(i < health);
        if (health > 3)
        {
          if (i < health - 3)
            _health_UI[i].material.color = _ExtraHealthColor;
          else
            _health_UI[i].material.color = GetColor();
        }
        else
          _health_UI[i].material.color = GetColor();
      }
    }

    ItemIcon[] _weaponIcons;
    class ItemIcon
    {
      public Transform _base, _ammoUI;
      public Transform[] _ammo;
      public int _ammoCount, _ammoVisible;

      static Vector2 _Offset = new Vector2(0.1f, 0.08f);

      public void Destroy()
      {
        GameObject.Destroy(_base.gameObject);
        foreach (var t in _ammo)
          GameObject.Destroy(t.gameObject);
        GameObject.Destroy(_ammoUI.gameObject);
      }

      public void Init(System.Tuple<Transform, int> data, int index, System.Tuple<PlayerScript, bool, ActiveRagdoll.Side> item_data, bool is_utility)
      {
        _base = data.Item1;
        _base.localPosition += new Vector3(_Offset.x, _Offset.y, 0f);
        _ammoCount = _ammoVisible = data.Item2;

        // Spawn ammo border
        GameResources.s_AmmoSideUi.text = !is_utility ? "" : item_data.Item3 == ActiveRagdoll.Side.LEFT ? "L" : "R";
        _ammoUI = GameObject.Instantiate(GameResources.s_AmmoUi).transform;
        _ammoUI.gameObject.layer = 11;
        _ammoUI.parent = _base.parent;
        _ammoUI.localPosition = new Vector3(0.8f + index * 0.8f, -0.15f, 0f);
        _ammoUI.localEulerAngles = new Vector3(90f, 0f, 0f);
        _ammoUI.localScale = new Vector3(0.8f, 0.004f, 0.25f);

        // Create ammo meshes
        _ammo = new Transform[_ammoCount];
        Vector3 localScale = new Vector3(0.8f / _ammoCount * (_ammoCount >= 3 ? (_ammoCount >= 12 ? 0.5f : 0.7f) : 0.82f), 0.18f, 0.001f);
        // Hide ammo if in-game
        var ammo = _ammoCount;
        if (item_data.Item1 != null)
        {
          var player = item_data.Item1;
          var isUtility = item_data.Item2;
          var side = item_data.Item3;
          if (!isUtility)
            if (side == ActiveRagdoll.Side.LEFT) ammo = player._Ragdoll._ItemL != null ? player._Ragdoll._ItemL.Clip() : ammo;
            else ammo = player._Ragdoll._ItemR != null ? player._Ragdoll._ItemR.Clip() : ammo;
          else
            if (side == ActiveRagdoll.Side.LEFT) ammo = player._UtilitiesLeft != null ? player._UtilitiesLeft.Count : ammo;
          else ammo = player._UtilitiesRight != null ? player._UtilitiesRight.Count : ammo;
        }
        if (ammo != _ammoCount) _ammoVisible = ammo;

        // Create meshes
        for (int i = 0; i < _ammoCount; i++)
        {
          var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
          cube.layer = 11;
          _ammo[i] = cube.transform;
          _ammo[i].parent = _base.parent;
          _ammo[i].localScale = localScale;
          _ammo[i].localEulerAngles = Vector3.zero;
          _ammo[i].localPosition = new Vector3(0.36f + index * 0.8f + _Offset.x, -0.23f + _Offset.y, 0f) + new Vector3(Mathf.Lerp(0f, 0.7f, (float)i / _ammoCount), 0f, 0f) + new Vector3(localScale.x / 2f, 0f, 0f);
          _ammo[i].GetComponent<Renderer>().sharedMaterial = s_Singleton._item_materials[0];
          if (i >= ammo) _ammo[i].gameObject.SetActive(false);
        }
      }

      // Update bullets
      public void Clip_Deincrement()
      {
        if (_ammoVisible <= 0 || _ammoVisible - 1 >= _ammo.Length) return;
        _ammo[--_ammoVisible].gameObject.SetActive(false);
      }
      public void Clip_Reload(int amount)
      {

        // Reload all
        if (amount < 1)
        {
          foreach (var t in _ammo)
            t.gameObject.SetActive(true);
          _ammoVisible = _ammoCount;
        }

        // Reload not all
        else
        {
          for (var i = 0; i < amount; i++)
          {
            if (_ammoVisible >= _ammo.Length) return;
            _ammo[_ammoVisible++].gameObject.SetActive(true);
          }
        }
      }

      public void UpdateIconManual(int clip)
      {
        _ammoVisible = clip;
        for (int i = 0; i < _ammoCount; i++)
          _ammo[i].gameObject.SetActive(i < clip);
      }
    }

    int GetItemIcons(ActiveRagdoll.Side side)
    {
      if (side == ActiveRagdoll.Side.RIGHT && _ItemLeft != ItemManager.Items.NONE) return 1;
      return 0;
    }

    public void ItemUse(ActiveRagdoll.Side side)
    {
      _weaponIcons[GetItemIcons(side)]?.Clip_Deincrement();
    }
    public void ItemReload(ActiveRagdoll.Side side, int amount)
    {
      _weaponIcons[GetItemIcons(side)]?.Clip_Reload(amount);
    }
    public void ItemSetClip(ActiveRagdoll.Side side, int clip)
    {
      _weaponIcons[GetItemIcons(side)]?.UpdateIconManual(clip);
    }

    ItemIcon GetUtility(ActiveRagdoll.Side side)
    {
      var startIter = (_ItemLeft == ItemManager.Items.NONE ? 0 : 1) + (_ItemRight == ItemManager.Items.NONE ? 0 : 1);
      var addIter = 0;
      if (side == ActiveRagdoll.Side.LEFT)
        addIter = 0;
      else
      {
        if (_Equipment._UtilitiesLeft.Length == 0)
          addIter = 0;
        else
          addIter = 1;
      }
      var iter = startIter + addIter;
      if (iter < _weaponIcons.Length)
        return _weaponIcons[iter];
      return null;
    }
    public void UtilityUse(ActiveRagdoll.Side side)
    {
      var utility = GetUtility(side);
      utility?.Clip_Deincrement();
    }
    public void UtilityReload(ActiveRagdoll.Side side, int amount)
    {
      var utility = GetUtility(side);
      utility?.Clip_Reload(amount);
    }

    public bool CanUtilityReload(ActiveRagdoll.Side side)
    {
      var util = GetUtility(side);
      if (util._ammo.Length == 0) return false;
      return !util._ammo[util._ammo.Length - 1].gameObject.activeSelf;
    }

    public int GetUtilitiesLength(ActiveRagdoll.Side side)
    {
      // Check which utilities to check
      var utils = _Equipment._UtilitiesLeft;
      if (side == ActiveRagdoll.Side.RIGHT)
        utils = _Equipment._UtilitiesRight;

      // Check for no utilities
      if (utils.Length == 0)
        return 0;

      //
      return utils.Length * Shop.GetUtilityCount(utils[0]);
    }

    //
    public void UpdateVersusUI()
    {
      var bg = _ui.GetChild(1).transform;

      _VersusUI.gameObject.SetActive(s_GameMode == GameModes.VERSUS);
      if (s_GameMode == GameModes.VERSUS)
      {

        var xpos = 0.701f;
        var ypos = -0.113f;
        if (bg.localScale.y >= 4.14f)
        {
          xpos = 3f;
          ypos = 0.35f;
        }
        else if (bg.localScale.y >= 3.29f)
        {
          xpos = 3.17f;
        }
        else if (bg.localScale.y >= 2.44f)
        {
          xpos = 2.38f;
        }
        else if (bg.localScale.y >= 1.59f)
        {
          xpos = 1.55f;
        }

        _VersusUI.localPosition = new Vector3(xpos, ypos, 0f);
        UpdateVersusScore();
      }
    }
    public void UpdateVersusScore()
    {
      var score = VersusMode.GetPlayerScore(_Id);
      var scoreText = _VersusUI.GetChild(1).GetComponent<TMPro.TextMeshPro>();

      scoreText.text = $"{score}";
    }

    // Update loadout index
    public void UpdateLoadoutIndex()
    {
      var bg = _ui.GetChild(1).transform;

      if (s_GameMode == GameModes.CLASSIC && SettingsModule.ShowLoadoutIndexes)
      {
        _loadoutIndexText.enabled = true;
        _loadoutIndexText.text = $"{_LoadoutIndex + 1}";

        var xpos = 0.49f;
        var ypos = -0.24f;
        if (bg.localScale.y >= 4.14f)
        {
          xpos = 3.82f;
        }
        else if (bg.localScale.y >= 3.29f)
        {
          xpos = 3f;
        }
        else if (bg.localScale.y >= 2.44f)
        {
          xpos = 2.17f;
        }
        else if (bg.localScale.y >= 1.59f)
        {
          xpos = 1.36f;
        }

        _loadoutIndexText.transform.localPosition = new Vector3(xpos, ypos, 0f);
      }
      else
      {
        _loadoutIndexText.enabled = false;
      }
    }

    //
    public void UpdateIcons()
    {

      // Perks
      UpdatePerkIcons();

      // Clear weapon icons
      if (_weaponIcons != null)
        foreach (var t in _weaponIcons) { if (t == null) continue; t.Destroy(); }
      _weaponIcons = null;

      // Load icons per player equipment
      var utilLength_left = GetUtilitiesLength(ActiveRagdoll.Side.LEFT);
      var utilLength_right = GetUtilitiesLength(ActiveRagdoll.Side.RIGHT);
      _weaponIcons = new ItemIcon[2 + utilLength_left + utilLength_right];
      var bg = _ui.GetChild(1).transform;

      // Check for empty
      if (_weaponIcons.Length == 2 && _ItemLeft == ItemManager.Items.NONE && _ItemRight == ItemManager.Items.NONE)
      {
        bg.localPosition = new Vector3(0f, -0.05f, 0f);
        bg.localScale = new Vector3(0.6f, 0.74f, 0.001f);

        UpdateVersusUI();
        UpdateLoadoutIndex();
        return;
      }
      var equipmentIter = 0;
      if (_ItemLeft != ItemManager.Items.NONE)
      {
        _weaponIcons[equipmentIter] = new ItemIcon();
        _weaponIcons[equipmentIter].Init(LoadIcon(_ItemLeft.ToString(), equipmentIter), equipmentIter++, System.Tuple.Create(_Player, false, ActiveRagdoll.Side.LEFT), false);
      }
      if (_ItemRight != ItemManager.Items.NONE)
      {
        _weaponIcons[equipmentIter] = new ItemIcon();
        _weaponIcons[equipmentIter].Init(LoadIcon(_ItemRight.ToString(), equipmentIter), equipmentIter++, System.Tuple.Create(_Player, false, ActiveRagdoll.Side.RIGHT), false);
      }
      // Load utilities
      var loaded_utils = new List<System.Tuple<Transform, int, int, ActiveRagdoll.Side>>();
      if (_Equipment._UtilitiesLeft.Length > 0)
      {
        var util_data = LoadIcon(_Equipment._UtilitiesLeft[0].ToString(), equipmentIter);
        loaded_utils.Add(System.Tuple.Create(util_data.Item1, utilLength_left, equipmentIter++, ActiveRagdoll.Side.LEFT));
      }
      if (_Equipment._UtilitiesRight.Length > 0)
      {
        var util_data = LoadIcon(_Equipment._UtilitiesRight[0].ToString(), equipmentIter);
        loaded_utils.Add(System.Tuple.Create(util_data.Item1, utilLength_right, equipmentIter++, ActiveRagdoll.Side.RIGHT));
      }
      foreach (var util in loaded_utils)
      {
        _weaponIcons[util.Item3] = new ItemIcon();
        _weaponIcons[util.Item3].Init(System.Tuple.Create(util.Item1, util.Item2), util.Item3, System.Tuple.Create(_Player, true, util.Item4), true);
      }
      loaded_utils = null;

      // Local parsing function
      System.Tuple<Transform, int> LoadIcon(string name, int iter)
      {
        // Get icon
        var item = ItemManager.GetItemUI(name, _Player, _ui);
        var transform = item.Item1;

        // Move BG
        bg.localPosition = new Vector3(0.05f + (iter + 1) * 0.4f, -0.05f, 0f);
        bg.localScale = new Vector3(0.6f, 0.74f + (iter + 1) * 0.85f, 0.001f);

        // Set transform
        transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        transform.localPosition = new Vector3(0.7f + iter * 0.8f, 0f, 0f);
        transform.localEulerAngles = new Vector3(0f, 90f, 0f);
        switch (transform.name)
        {
          case ("KNIFE"):
          case ("ROCKET_FIST"):
            transform.localPosition += new Vector3(-0.11f, -0.02f, 0f);
            transform.localScale = new Vector3(0.13f, 0.13f, 0.13f);
            transform.localEulerAngles += new Vector3(6.8f, 0f, 0f);
            break;
          case ("AXE"):
            transform.localPosition += new Vector3(-0.11f, 0.03f, 0f);
            transform.localScale = new Vector3(0.1f, 0.12f, 0.1f);
            transform.localEulerAngles += new Vector3(90f, 0f, 0f);
            break;
          case "STUN_BATON":
            transform.localPosition += new Vector3(-0.03f, 0.03f, 0f);
            transform.localScale = new Vector3(0.17f, 0.17f, 0.17f);
            transform.localEulerAngles = new Vector3(75f, 90f, 0f);
            break;
          case ("FRYING_PAN"):
            transform.localPosition += new Vector3(-0.2f, 0.03f, 0f);
            transform.localScale = new Vector3(0.13f, 0.17f, 0.13f);
            transform.localEulerAngles = new Vector3(0f, 0f, 270f);
            break;
          case ("BAT"):
            transform.localPosition += new Vector3(-0.15f, 0f, 0f);
            transform.localScale = new Vector3(0.11f, 0.12f, 0.11f);
            transform.localEulerAngles += new Vector3(81f, 0f, 0f);
            break;
          case "KATANA":
          case "RAPIER":
            transform.localPosition += new Vector3(-0.2f, -0.05f, 0f);
            transform.localScale = new Vector3(0.11f, 0.1f, 0.11f);
            transform.localEulerAngles = new Vector3(8f, 0f, -75f);
            break;
          case "FIST":
            transform.localPosition += new Vector3(-0.2f, 0.03f, 0f);
            transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            break;
          case "PISTOL_SILENCED":
          case "PISTOL":
          case "PISTOL_DOUBLE":
          case "PISTOL_CHARGE":
            transform.localPosition += new Vector3(-0.19f, 0.06f, 0f);
            transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            break;
          case "RIFLE_CHARGE":
            transform.localPosition += new Vector3(0.03f, 0.06f, 0f);
            transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            break;
          case "PISTOL_MACHINE":
            transform.localPosition += new Vector3(-0.19f, 0.08f, 0f);
            transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            break;
          case ("REVOLVER"):
            transform.localPosition += new Vector3(-0.22f, -0.01f, 0f);
            transform.localScale = new Vector3(0.13f, 0.13f, 0.13f);
            break;
          case ("SHOTGUN_DOUBLE"):
            transform.localPosition += new Vector3(0.08f, 0.04f, 0f);
            transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
            break;
          case ("SHOTGUN_PUMP"):
            transform.localPosition += new Vector3(0f, 0.02f, 0f);
            transform.localScale = new Vector3(0.1f, 0.09f, 0.1f);
            break;
          case ("SHOTGUN_BURST"):
            transform.localPosition += new Vector3(0.01f, 0f, 0f);
            transform.localScale = new Vector3(0.11f, 0.1f, 0.13f);
            break;
          case ("UZI"):
            transform.localPosition += new Vector3(-0.16f, 0.07f, 0f);
            transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            break;
          case ("AK47"):
          case ("FLAMETHROWER"):
            transform.localPosition += new Vector3(-0.11f, 0.03f, 0f);
            transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
            break;
          case ("M16"):
            transform.localPosition += new Vector3(-0.14f, 0.03f, 0f);
            transform.localScale = new Vector3(0.09f, 0.11f, 0.08f);
            break;
          case ("DMR"):
          case ("RIFLE"):
          case ("RIFLE_LEVER"):
            transform.localPosition += new Vector3(-0.14f, 0.03f, 0f);
            transform.localScale = new Vector3(0.09f, 0.11f, 0.08f);
            break;
          case ("CROSSBOW"):
            transform.localPosition += new Vector3(-0.35f, -0.07f, 0f);
            transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            transform.localEulerAngles = new Vector3(-30f, 90f, 90f);
            break;
          case ("GRENADE_LAUNCHER"):
            transform.localPosition += new Vector3(-0.21f, 0.05f, 0f);
            transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
            break;
          case ("MORTAR_STRIKE"):
            transform.localPosition += new Vector3(-0.15f, 0.05f, 0f);
            transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
            break;
          case ("SNIPER"):
            transform.localPosition += new Vector3(-0.14f, 0.03f, 0f);
            transform.localScale = new Vector3(0.09f, 0.11f, 0.08f);
            break;
          case "GRENADE_IMPACT":
          case "GRENADE":
          case "MOLOTOV":
            transform.localPosition += new Vector3(-0.23f, 0.03f, 0f);
            transform.localEulerAngles = new Vector3(25f, -90f, 0f);
            transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            break;
          case "TACTICAL_BULLET":
            transform.localPosition += new Vector3(-0.13f, 0.03f, 0f);
            transform.localEulerAngles = new Vector3(17f, -90f, 0f);
            transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            break;
          case "MIRROR":
            transform.localPosition += new Vector3(-0.14f, 0f, 0f);
            transform.localEulerAngles = new Vector3(0f, 180f, -16f);
            transform.localScale = new Vector3(0.8f, 0.8f, 0.2f);
            break;
          case ("C4"):
            transform.localPosition += new Vector3(-0.17f, 0.0f, 0f);
            transform.localEulerAngles = new Vector3(0f, 90f, -90f);
            transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            break;
          case ("SHURIKEN"):
          case ("SHURIKEN_BIG"):
            transform.localPosition += new Vector3(-0.22f, 0.02f, 0f);
            transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            break;
          case ("KUNAI_EXPLOSIVE"):
          case ("KUNAI_STICKY"):
            transform.localPosition += new Vector3(-0.14f, -0.03f, 0f);
            transform.localEulerAngles = new Vector3(0f, 90f, 90f);
            transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            break;
          case ("STOP_WATCH"):
          case ("INVISIBILITY"):
            transform.localPosition += new Vector3(-0.25f, 0f, 0f);
            transform.localEulerAngles = new Vector3(0f, 90f, -90f);
            transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            break;
          case ("STICKY_GUN"):
            transform.localPosition += new Vector3(-0.06f, 0.03f, 0f);
            transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            break;

          case ("GRENADE_STUN"):

            transform.localPosition += new Vector3(-0.13f, -0.01f, 0f);
            transform.localEulerAngles = new Vector3(0f, 0f, -90f);
            transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);

            break;

          case ("TEMP_SHIELD"):

            transform.localPosition += new Vector3(-0.2f, 0.03f, 0f);
            transform.localEulerAngles = new Vector3(90f, 0f, 0f);
            transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            break;

          default:
            Debug.LogWarning($"Unhandled positioning of Utility UI type: {name}");

            transform.localPosition += new Vector3(-0.19f, 0.08f, 0f);
            transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            break;
        }
        // Color
        var meshes = new List<Renderer>();
        var transform_base = transform.Find("Mesh");
        transform_base.gameObject.layer = 11;
        var mesh_base = transform_base.GetComponent<Renderer>();
        if (mesh_base != null) meshes.Add(mesh_base);
        for (int i = 0; i < transform_base.childCount; i++)
        {
          transform_base.GetChild(i).gameObject.layer = 11;
          mesh_base = transform_base.GetChild(i).GetComponent<Renderer>();
          if (mesh_base != null) meshes.Add(mesh_base);
        }
        foreach (var mesh in meshes)
          for (int i = 0; i < mesh.sharedMaterials.Length; i++)
          {
            var shared = mesh.sharedMaterials;
            if (shared[i].name.Equals("Item")) shared[i] = s_Singleton._item_materials[0];
            else shared[i] = s_Singleton._item_materials[1];
            mesh.sharedMaterials = shared;
          }

        return item;
      }

      //
      UpdateVersusUI();
      UpdateLoadoutIndex();
    }

    //
    public void UpdatePerkIcons()
    {
      // UI
      if (_perk_UI == null)
        _perk_UI = new MeshRenderer[] { _ui.GetChild(2).GetChild(0).GetComponent<MeshRenderer>(), _ui.GetChild(2).GetChild(1).GetComponent<MeshRenderer>(), _ui.GetChild(2).GetChild(2).GetComponent<MeshRenderer>(), _ui.GetChild(2).GetChild(3).GetComponent<MeshRenderer>() };

      // Current perks
      var perks = Shop.Perk.GetPerks(_Id);

      // Check empty
      if (perks.Count == 0)
      {
        foreach (var perkRend in _perk_UI)
          perkRend.gameObject.SetActive(false);
        return;
      }

      // Check normal
      for (var i = 0; i < _perk_UI.Length; i++)
      {
        var has_perk = i < perks.Count;
        _perk_UI[i].gameObject.SetActive(has_perk);
        if (!has_perk) continue;
        _perk_UI[i].material.mainTexture = GameResources._PerkTypes.transform.GetChild((int)perks[i]).GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
      }
    }

    //
    public void OnPlayerSpawn()
    {
      UpdateIcons();

      CreateHealthUI(_Player == null ? 1 : _Player._Ragdoll._health);
    }
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

  public static class ItemManager
  {
    class Item
    {
      public Items _type;
      public GameObject _gameObject;

      public Item(Items itemType, ref GameObject gameObject)
      {
        _type = itemType;
        _gameObject = gameObject;
      }
    }

    public class Loadout
    {
      public static Loadout[] _Loadouts;
      public static int _CurrentLoadoutIndex;
      public static Loadout _CurrentLoadout { get { if (Levels._EditingLoadout) return Levels._HardcodedLoadout; if (_Loadouts == null) return null; return _Loadouts[_CurrentLoadoutIndex]; } }

      public static void Init()
      {
        _Loadouts = new Loadout[Shop.s_ShopLoadoutCount];
        for (var i = 0; i < _Loadouts.Length; i++)
          _Loadouts[i] = new Loadout(i);
      }

      public static int _POINTS_MAX
      {
        get
        {
          // Return level editor max points
          if (Levels._EditingLoadout)
            return Levels._LOADOUT_MAX_POINTS;

          // Else, return CLASSIC mode shop points
          return Shop._Max_Equipment_Points;
        }
      }

      public int _id;
      public int _available_points
      {
        get
        {
          var total = 0;
          // Items
          foreach (var item in (_two_weapon_pairs ? new Items[] {
            _Equipment._ItemLeft0, _Equipment._ItemRight0,
            _Equipment._ItemLeft1, _Equipment._ItemRight1,
          } : new Items[] {
            _Equipment._ItemLeft0, _Equipment._ItemRight0,
          }))
            total += GetItemValue(item);
          // Utils
          foreach (var utility in _Equipment._UtilitiesLeft)
            total += GetUtilityValue(utility);
          foreach (var utility in _Equipment._UtilitiesRight)
            total += GetUtilityValue(utility);
          // Perks
          foreach (var perk in _Equipment._Perks)
            total += GetPerkValue(perk);
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
        _id = id;

        // Load saved equipment
        Load();
      }

      public bool CanEquipItem(ActiveRagdoll.Side side, int index, Items item)
      {
        var currentEquipValue = GetItemValue(side == ActiveRagdoll.Side.LEFT ? (index == 0 ? _Equipment._ItemLeft0 : _Equipment._ItemLeft1) : (index == 0 ? _Equipment._ItemRight0 : _Equipment._ItemRight1));
        return _available_points + currentEquipValue - GetItemValue(item) >= 0;
      }
      public bool CanEquipUtility(ActiveRagdoll.Side side, UtilityScript.UtilityType utility)
      {
        var currentUtilities = side == ActiveRagdoll.Side.LEFT ? _Equipment._UtilitiesLeft : _Equipment._UtilitiesRight;
        var currentEquipValue = 0;
        // Check if adding a new utility or adding additional
        if (currentUtilities.Length > 0 && utility != currentUtilities[0])
        {
          currentEquipValue = GetUtilityValue(currentUtilities[0]) * currentUtilities.Length;
        }
        return _available_points + currentEquipValue - GetUtilityValue(utility) >= 0;
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

        LevelModule.SetLoadout(_id, savestring);
      }

      public void Load()
      {
        _Equipment = new PlayerProfile.Equipment();

        try
        {
          var loadstring = LevelModule.GetLoadout(_id);

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
                var perk = (Shop.Perk.PerkType)System.Enum.Parse(typeof(Shop.Perk.PerkType), val, true);
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

    static List<Item> _Items;
    public static Transform _Container;

    public enum Items
    {
      NONE,
      REVOLVER,
      DMR,
      RIFLE,
      RIFLE_LEVER,
      SNIPER,
      UZI,
      KNIFE,
      BAT,
      SHOTGUN_PUMP,
      SHOTGUN_DOUBLE,
      SHOTGUN_BURST,
      PISTOL,
      PISTOL_SILENCED,
      PISTOL_MACHINE,
      PISTOL_DOUBLE,
      GRENADE_HOLD,
      AK47,
      M16,
      ROCKET_LAUNCHER,
      KATANA,
      FRYING_PAN,
      AXE,
      CROSSBOW,
      GRENADE_LAUNCHER,
      FLAMETHROWER,
      ROCKET_FIST,
      STICKY_GUN,
      PISTOL_CHARGE,
      RAPIER,
      RIFLE_CHARGE,
      FIST,
      STUN_BATON,
    }

    // Spawn a single item
    public static ItemScript SpawnItem(Items item)
    {
      if (item == Items.NONE) return null;
      if (_Items == null) _Items = new List<Item>();

      var name = item.ToString();
      if (_Container == null) _Container = GameObject.Find("Items").transform;

      GameObject new_item = Instantiate(Resources.Load($"Items/{name}") as GameObject, _Container);
      new_item.name = name;
      var script = new_item.GetComponent<ItemScript>();
      script._type = item;
      new_item.transform.position = new Vector3(0f, -10f, 0f);
      _Items.Add(new Item(item, ref new_item));
      return script;
    }

    public static System.Tuple<Transform, int> GetItemUI(string item, PlayerScript player, Transform parent)
    {
      GameObject new_item = Instantiate(Resources.Load($"Items/{item}") as GameObject, parent);
      new_item.name = item;
      new_item.layer = 11;

      var script = new_item.GetComponent<ItemScript>();
      script._ragdoll = player != null ? player._Ragdoll : null;
      var clipSize = script.GetClipSize();
      GameObject.DestroyImmediate(script);

      var colliders = new_item.GetComponents<Collider>();
      for (var i = 0; i < colliders.Length; i++)
        GameObject.DestroyImmediate(colliders[i]);

      var rigidbody = new_item.GetComponent<Rigidbody>();
      if (rigidbody) GameObject.DestroyImmediate(rigidbody);

      return System.Tuple.Create(new_item.transform, clipSize);
    }

    public static GameObject GetItem(Items itemType)
    {
      foreach (var i in _Items)
      {
        if (i._type == itemType)
        {
          _Items.Remove(i);
          return i._gameObject;
        }
      }
      return null;
    }

    public static int GetItemValue(Items item)
    {
      switch (item)
      {
        case Items.NONE:
        case Items.FIST:
          return 0;
        case Items.KNIFE:
          return 1;
        case Items.STUN_BATON:
          return 2;
        case Items.FRYING_PAN:
        case Items.AXE:
        case Items.ROCKET_FIST:
        case Items.RAPIER:
        case Items.PISTOL_SILENCED:
        case Items.PISTOL_MACHINE:
        case Items.PISTOL_DOUBLE:
        case Items.PISTOL_CHARGE:
        case Items.STICKY_GUN:
        case Items.RIFLE:
        case Items.GRENADE_LAUNCHER:
          return 3;
        case Items.RIFLE_LEVER:
        case Items.RIFLE_CHARGE:
        case Items.SNIPER:
        case Items.CROSSBOW:
        case Items.KATANA:
        case Items.SHOTGUN_DOUBLE:
        case Items.SHOTGUN_PUMP:
        case Items.BAT:
        case Items.UZI:
        case Items.REVOLVER:
          return 4;
        case Items.FLAMETHROWER:
        case Items.AK47:
        case Items.SHOTGUN_BURST:
        case Items.M16:
        case Items.DMR:
          return 5;
        case Items.ROCKET_LAUNCHER:
        case Items.GRENADE_HOLD:
        case Items.PISTOL:
          break;
      }
      return -1;
    }
    public static int GetUtilityValue(UtilityScript.UtilityType utility)
    {
      switch (utility)
      {
        case UtilityScript.UtilityType.NONE:
          return 0;
        case UtilityScript.UtilityType.DASH:
        case UtilityScript.UtilityType.SHURIKEN:
        case UtilityScript.UtilityType.SHURIKEN_BIG:
        case UtilityScript.UtilityType.GRENADE_STUN:
        case UtilityScript.UtilityType.TACTICAL_BULLET:
        case UtilityScript.UtilityType.MIRROR:
          return 1;
        case UtilityScript.UtilityType.GRENADE:
        case UtilityScript.UtilityType.GRENADE_IMPACT:
        case UtilityScript.UtilityType.C4:
        case UtilityScript.UtilityType.MOLOTOV:
        case UtilityScript.UtilityType.KUNAI_EXPLOSIVE:
        case UtilityScript.UtilityType.KUNAI_STICKY:
        case UtilityScript.UtilityType.STOP_WATCH:
        case UtilityScript.UtilityType.TEMP_SHIELD:
          return 2;
        case UtilityScript.UtilityType.INVISIBILITY:
        case UtilityScript.UtilityType.MORTAR_STRIKE:
          return 3;
      }
      return 100;
    }
    public static int GetPerkValue(Shop.Perk.PerkType perk)
    {
      switch (perk)
      {
        case Shop.Perk.PerkType.FIRE_RATE_UP:
        case Shop.Perk.PerkType.AKIMBO:
          break;
        case Shop.Perk.PerkType.NONE:
        case Shop.Perk.PerkType.NO_SLOWMO:
          return 0;
        case Shop.Perk.PerkType.LASER_SIGHTS:
        case Shop.Perk.PerkType.MARTIAL_ARTIST:
          return 1;
        case Shop.Perk.PerkType.THRUST:
        case Shop.Perk.PerkType.SPEED_UP:
        case Shop.Perk.PerkType.TWIN:
          return 2;
        case Shop.Perk.PerkType.EXPLOSIONS_UP:
        case Shop.Perk.PerkType.GRAPPLE_MASTER:
        case Shop.Perk.PerkType.EXPLOSION_RESISTANCE:
        case Shop.Perk.PerkType.MAX_AMMO_UP:
        case Shop.Perk.PerkType.EXPLOSIVE_PARRY:
          return 3;
        case Shop.Perk.PerkType.PENETRATION_UP:
        case Shop.Perk.PerkType.ARMOR_UP:
        case Shop.Perk.PerkType.FASTER_RELOAD:
          return 4;
        case Shop.Perk.PerkType.SMART_BULLETS:
          return 6;
      }
      return 100;
    }
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
    }
    else
    {
      Time.timeScale = 1f;
      // Check player amount change
      if (s_GameMode == GameModes.CLASSIC)
        if (PlayerScript.s_Players != null && Settings._NumberPlayers != PlayerScript.s_Players.Count) TileManager.ReloadMap();
      TileManager._Text_LevelNum.gameObject.SetActive(true);
      TileManager._Text_LevelTimer.gameObject.SetActive(true);
      if (s_GameMode == GameModes.CLASSIC && !Levels._LevelPack_Playing && !s_EditorTesting)
        TileManager._Text_LevelTimer_Best.gameObject.SetActive(true);
      TileManager._Text_GameOver.gameObject.SetActive(true);
      TileManager._Text_Money.gameObject.SetActive(true);
      TileManager.UnHideMonies();
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
        Shop.AddAvailableUnlockVault($"classic_{(numDirs0) + i}");
  }

  // Fired on last enemy killed
  public static void OnLastEnemyKilled()
  {
    s_Singleton._goalPickupTime = Time.time;

    ToggleExitLight(true);

    // Check achievements
#if UNITY_STANDALONE
    if (LevelModule.ExtraTime == 1 && Settings._Extras_CanUse)
      SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.EXTRA_SUPERH);
#endif
  }

  //
  static public void OnLevelComplete()
  {
    // Check survival
    if (IsSurvival()) return;

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

    // Check level editor levels
    if (s_EditorTesting)
    {
      ReloadMap();
      return;
    }

    // Display unlock messages
    if (Shop.s_UnlockString != string.Empty)
    {
      var nextMenu = Shop.s_UnlockString.Contains("new difficulty unlocked") || Shop.s_UnlockString.Contains("to unlock the optional extra settings") ? Menu.MenuType.LEVELS : Menu.MenuType.NONE;
#if UNITY_WEBGL
      if (Shop.s_UnlockString.Contains("you completed the demo"))
        nextMenu = Menu.MenuType.LEVELS;
#endif
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
      Shop.s_UnlockString += $"- you completed the demo! <color=magenta>check out the full game on Steam!</color>\n";

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
      if (s_GameMode == GameModes.CLASSIC)
      {
        // Award shop point
        //Shop._AvailablePoints++;

        /*/ Check unlocks at end of level packs
        if ((Levels._CurrentLevelIndex + 1) % 12 == 0)
        {
          var iter = ((Levels._CurrentLevelIndex + 1) / 12) - 1;
          if (Settings._DIFFICULTY == 1) iter += 12;
          Shop.AddAvailableUnlockVault($"{_GameMode.ToString().ToLower()}_{iter}");
        }*/

        UpdateLevelVault();

#if UNITY_STANDALONE
        // Check for achievements
        if (Levels._CurrentLevelIndex == 0 && Settings._DIFFICULTY == 0)
        {
          SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.LEVEL_0_COMPLETED);
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
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DIFFICULTY_1);
#endif

        if (Settings._DifficultyUnlocked <= 0)
        {
          Menu.s_SaveMenuDir = -1;
          Settings._DifficultyUnlocked = 1;
          Shop.s_UnlockString += $"- new difficulty unlocked: <color=cyan>sneakier</color>\n";
          Menu.s_SetNextDifficultyOnMenu = true;

          return true;
        }
      }
      else if (Settings._DIFFICULTY == 1)
      {

        // Achievement
#if UNITY_STANDALONE
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DIFFICULTY_2);
#endif

        if (Settings._DifficultyUnlocked <= 1)
        {

          Settings._DifficultyUnlocked = 2;

          Shop.s_UnlockString += $"- you have beaten the classic mode!\n- try out the survival mode or try to unlock the optional extra settings!";
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
      IsSurvival() ||
      TileManager._LoadingMap
    )
      return false;

    //
    if (SettingsModule.LevelCompletionBehavior == Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL)
    {
      LoadRandomLevel();
      return true;
    }

    else if (SettingsModule.LevelCompletionBehavior == Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL_ALL)
    {
      LoadRandomLevel(true);
      return true;
    }

    //
    else if (Levels._CurrentLevelIndex < (Levels._CurrentLevelCollection?._levelData.Length ?? 0) && Levels.LevelCompleted(Levels._CurrentLevelIndex))
    {
      NextLevel(Levels._CurrentLevelIndex + 1);
      IncrementLevelMenu(1);
      return true;
    }

    //
    return false;
  }

  public static bool PreviousLevelSafe()
  {

    if (
      s_EditorEnabled ||
      s_EditorTesting ||
      IsSurvival() ||
      TileManager._LoadingMap
    )
      return false;

    //
    if (SettingsModule.LevelCompletionBehavior == Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL)
    {
      LoadRandomLevel();
      return true;
    }

    else if (SettingsModule.LevelCompletionBehavior == Settings.SettingsSaveData.LevelCompletionBehaviorType.RANDOM_LEVEL_ALL)
    {
      LoadRandomLevel(true);
      return true;
    }

    //
    else if (Levels._CurrentLevelIndex > 0)
    {
      NextLevel(Levels._CurrentLevelIndex - 1);
      IncrementLevelMenu(-1);
      return true;
    }

    //
    return false;
  }

  public static void NextLevel(int levelIndex)
  {
    // Unpause if paused
    if (GameScript.s_Paused) GameScript.TogglePause();

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

    // Network load
    s_CustomNetworkManager.OnLevelLoad(levelData);
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

  public static bool IsSurvival()
  {
    return s_GameMode == GameModes.SURVIVAL;
  }

  // When called, the player can exit / complete the level
  public float _goalPickupTime;
  public static void ToggleExit(bool toggle = true)
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

    if (s_GameMode != GameModes.CLASSIC && s_GameMode != GameModes.CHALLENGE)
      s_ExitLightShow = false;

    else if (!PlayerScript._TimerStarted)
      s_ExitLightShow = true;

    else if (!EnemyScript.AllDead() || !PlayerScript.HasExit())
      s_ExitLightShow = false;
    else
      s_ExitLightShow = toggle;
  }

}