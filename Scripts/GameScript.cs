using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;

public class GameScript : MonoBehaviour
{
  public static TextMesh _debugText;

  public static GameScript _Singleton;

  public static bool _Paused;

  public bool _GameEnded,
    _ExitOpen,
    _IsDemo;

  public static int _PlayerIter;

  public static Light _CameraLight;

  static MeshRenderer _BlackFade;

  public static AudioSource _audioListenerSource, _music;

  public bool _UseCamera, _X, _Y, _Z;

  public Material[] _item_materials;

  public static float _LevelStartTime;

  public static bool _EditorEnabled, _EditorTesting;

  public static int _LevelSelectColumns = 12, _LevelSelectRows = 4;
  public static int _LevelSelectionsPerPage
  {
    get
    {
      return _LevelSelectRows * _LevelSelectColumns;
    }
  }

  public void OnApplicationQuit()
  {

    //if (!Application.isEditor) System.Diagnostics.Process.GetCurrentProcess().Kill();
    Application.Quit();
  }
  public static void OnApplicationQuitS()
  {
    _Singleton.OnApplicationQuit();
  }

  static float _levelEndTimer;
  public static bool _inLevelEnd;
  static ParticleSystem _LevelEndParticles;

  public static Color _LightingAmbientColor;

  public enum GameModes
  {
    CLASSIC,
    SURVIVAL,
    CHALLENGE
  }
  public static GameModes _GameMode;

  public Input_action _Controls;

  /// <summary>
  /// Holds are information about tutorial such as how to restart or change weapons.
  /// </summary>
  public static class TutorialInformation
  {
    private static bool HasRestarted;
    public static bool _HasRestarted // Will be true when the player has restarted the game
    {
      set
      {
        HasRestarted = value;
        PlayerPrefs.SetInt("tut_hasRestarted", HasRestarted ? 1 : 0);
      }
      get
      {
        return HasRestarted;
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
      // Load saved info
      HasRestarted = PlayerPrefs.GetInt("tut_hasRestarted") == 1;
    }
  }

  // Use this for initialization
  void Start()
  {
    _Singleton = this;

    // Rain SFX
    var rain_sfx = GameObject.Find("RainAudio").GetComponents<AudioSource>();
    _Rain_Audio = rain_sfx[0];
    _Thunder_Audio = rain_sfx[1];

    //
    _debugText = GameObject.Find("DebugText").GetComponent<TextMesh>();

    //
    GameResources.Init();
    FunctionsC.Init();
    TutorialInformation.Init();
    ControllerManager.Init();
    SceneThemes.Init();
    ProgressBar.Init();
    Shop.Init();
    Stats.Init();

    UpdateLevelVault();

    SteamManager.SteamMenus.Init();
    //SteamManager.Achievements.Init();
#if UNITY_STANDALONE
    SteamManager.Workshop_GetUserItems(true);
#endif

    ActiveRagdoll.BodyPart_Handler.Init();

    GameResources._Camera_Main.farClipPlane = 10f;

    // Init loadouts
    ItemManager.Loadout.Init();

    _CameraLight = GameResources._Camera_Main.transform.GetChild(2).GetComponent<Light>();

    Transform t = GameObject.Find("Map").transform.GetChild(2);
    t.localPosition = new Vector3(t.localPosition.x, TileManager.Tile._StartY + TileManager.Tile._AddY, t.localPosition.z);

    TileManager._Map = GameObject.Find("Map").transform;
    TileManager._navMeshSurface = TileManager._Map.GetComponent<UnityEngine.AI.NavMeshSurface>();
    TileManager._navMeshSurface2 = TileManager._Map.GetComponents<UnityEngine.AI.NavMeshSurface>()[1];
    TileManager.Init();
    TileManager.LoadMap("5 6 1 1 1 0 1 1 1 1 0 0 0 0 0 0 0 0 0 0 1 1 0 0 0 0 1 1 1 0 1 1 playerspawn_-37.5_-55.62_rot_0 e_-42.5_-48.1_li_knife_w_-42.5_-48.1_l_-41.4_-48.1_canmove_true_canhear_true_ e_-32.5_-48.1_li_knife_w_-32.5_-48.1_l_-33.6_-48.1_canmove_true_canhear_true_ e_-40_-50.6_li_knife_w_-40_-50.6_l_-39.7_-49.4_canmove_true_canhear_true_ e_-35_-44.4_li_knife_w_-35_-44.4_l_-36.1_-44.4_canmove_true_canhear_true_ p_-37.5_-44.38_end_ barrel_-40.66_-42.61_rot_0 barrel_-39.79_-42.62_rot_0 barrel_-38.9_-42.59_rot_0 barrel_-37.96_-42.62_rot_0 barrel_-37.02_-42.55_rot_0 barrel_-40.72_-46.53_rot_0 barrel_-40.73_-45.68_rot_0 barrel_-34.44_-46.61_rot_0 bookcasebig_-35.16_-42.79_rot_15 bookcaseopen_-40.47_-51.45_rot_0 bookcaseopen_-34.37_-51.19_rot_90.00001 bookcaseopen_-34.39_-50_rot_90.00001 bookcaseopen_-40.67_-44.71_rot_90.00001 bookcaseopen_-37.5_-46.88_rot_0 bookcaseopen_-37.5_-47.67_rot_0 barrel_-34.39_-45.78_rot_0 barrel_-31.86_-47.48_rot_0 barrel_-31.84_-48.37_rot_0 barrel_-35.15_-51.47_rot_0 tablesmall_-43.08_-48.95_rot_0 chair_-42.68_-49.08_rot_45 chair_-31.83_-49.12_rot_1.692939E-06 chair_-40.71_-43.66_rot_135 bookcasebig_-38.42_-50.79_rot_285 bookcaseopen_-36.42_-47.9_rot_75 barrel_-39.44_-51.42_rot_0 barrel_-39.25_-50.49_rot_0 candelbig_-36.55_-46.97_rot_90.00001 bookcaseopen_-43.24_-47.83_rot_105", false, false, true);

    TileManager.EditorMenus.Init();
    TileManager.EditorMenus.HideMenus();

    _BlackFade = GameObject.Find("BlackFade").GetComponent<MeshRenderer>();
    FadeIn();

    _audioListenerSource = GameObject.Find("AudioListener").GetComponent<AudioSource>();
    _music = GameObject.Find("Music").GetComponent<AudioSource>();

    FunctionsC.MusicManager.Init();

    Settings.Init();

    // Init playerprofile and loadouts
    PlayerScript._Materials_Ring = new Material[4];
    var mat = Instantiate(TileManager._Ring.gameObject).transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;
    for (var i = 0; i < PlayerScript._Materials_Ring.Length; i++)
      PlayerScript._Materials_Ring[i] = new Material(mat);

    new PlayerProfile();
    new PlayerProfile();
    new PlayerProfile();
    new PlayerProfile();

    SceneThemes.ChangeMapTheme("Black and White");

    // Init menus
    Menu2.Init();

    // Play menu music
    FunctionsC.MusicManager.PlayTrack(Settings._DifficultyUnlocked == 0 && !Levels._UnlockAllLevels ? 0 : 1);

    //foreach(var gamepad in ControllerManager._Gamepads){
    //  Debug.Log(gamepad.name);
    //}
  }

  public static Transform _lp0, _lp1, _lp2;

  public static void SpawnPlayers()
  {
    PlayerScript._PLAYERID = 0;
    _PlayerIter = 0;
    if (PlayerScript._Players != null)
    {

      // Count number of players to spawn.. only used if trying to stay persistant or players joined?
      foreach (var p in PlayerScript._Players) if (p != null && !p._ragdoll._dead) _PlayerIter++;

      // Remove null / dead players
      for (var i = PlayerScript._Players.Count - 1; i >= 0; i--)
      {
        var p = PlayerScript._Players[i];
        if (p == null || p._ragdoll == null || !p._ragdoll._dead)
          PlayerScript._Players.RemoveAt(i);
      }
    }

    // Spawn players
    if (_PlayerIter < Settings._NumberPlayers)
      for (; _PlayerIter < Settings._NumberPlayers; _PlayerIter++)
        PlayerspawnScript._PlayerSpawns[0].SpawnPlayer();

    // Save player num for challenges
    PlayerScript._NumPlayers_Start = Settings._NumberPlayers;
  }

  static Coroutine _tutorialCo;
  public static void OnLevelStart()
  {
    IEnumerator tutorialfunction()
    {
      //Debug.Log("Starting tut");
      TutorialInformation._TutorialArrow.position = new Vector3(-100f, 0f, 0f);
      var goal = GameObject.Find("Powerup").GetComponent<Powerup>();
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
        float mod = Mathf.PingPong(Time.time * 1f, 1f);
        Vector3 pos;
        // Spawn tutorial arrow after 5 seconds
        if (Time.time - _LevelStartTime > 5f)
          if (!goal._activated && !goal._activated2)
          {
            pos = goal.transform.position + new Vector3(1.5f + Easings.CircularEaseOut(mod), 0f);
            pos.y = ypos;
            TutorialInformation._TutorialArrow.position = pos;
            m.sharedMaterial.mainTextureOffset = new Vector2(0f, Time.time);
            continue;
          }
          else if (!goal._activated2)
          {
            pos = PlayerspawnScript._PlayerSpawns[0].transform.position + new Vector3(1.5f + Easings.CircularEaseOut(mod), 0f);
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
        _Singleton.StopCoroutine(_tutorialCo);
        _tutorialCo = null;
      }
      _tutorialCo = _Singleton.StartCoroutine(tutorialfunction());
    }
  }

  /*static int selectedlevel = -1;
  public static void DisplayLevelOrganizerLevels()
  {
    // Delete old maps if exists
    if (Menu._Menu_LevelOrganizer._menu.GetChild(0).childCount > 0)
    {
      Transform selections = Menu._Menu_LevelOrganizer._menu.GetChild(0);
      for (int i = selections.childCount - 1; i >= 0; i--)
        GameObject.Destroy(selections.GetChild(i).gameObject);
    }
    // Delete level previews if exist
    if (_lp0 != null && _lp0.gameObject != null) GameObject.Destroy(_lp0.gameObject);
    if (_lp1 != null && _lp1.gameObject != null) GameObject.Destroy(_lp1.gameObject);
    if (_lp2 != null && _lp2.gameObject != null) GameObject.Destroy(_lp2.gameObject);
    // Start display coroutine
    _Instance.StartCoroutine(DisplayLevelOrganizerLevelsCo());
    // Change quality settings to 0; loading >100 map previews is very laggy
    RenderSettings.ambientLight = Color.white;
    QualitySettings.SetQualityLevel(0);
  }
  static IEnumerator DisplayLevelOrganizerLevelsCo()
  {
    // Load all map previews
    int width = 6,
      cwidth = 0, cheight = 0,
      iter = 0;
    Transform holder = Menu._Menu_LevelOrganizer._menu.GetChild(0);
    holder.localScale = new Vector3(1f, 1f, 1f);
    string[] leveldata = Levels._LevelCollections[Levels._CurrentLevelCollectionIndex]._leveldata;
    Menu._Menu_LevelOrganizer._menuActions = new Menu.MenuAction[leveldata.Length];
    foreach (string s in leveldata)
    {
      Transform container = TileManager.GetMapPreview(s, -1),
        supercontainer = new GameObject(iter + "").transform;
      container.name = "map";
      container.parent = supercontainer;
      container.localPosition = Vector3.zero;
      supercontainer.parent = holder;
      supercontainer.localPosition = new Vector3(cwidth++ * 2.5f, -cheight * 1.5f, -3f);
      // Swap levels
      Menu._Menu_LevelOrganizer._menuActions[iter++] = (int iterr) =>
      {
        int selection = Menu._Menu_LevelOrganizer._currentSelection;
        // Select level
        if (selectedlevel == -1)
        {
          selectedlevel = selection;
          Menu.ChangeSelectorColor(Color.green, Menu.SelectorType.ARROW);
          TileManager._Ring.position = Menu._Menu_LevelOrganizer._menu.GetChild(0).GetChild(selectedlevel).position;
          return;
        }
        // Check double selection
        if (selectedlevel == selection)
        {
          selectedlevel = -1;
          Menu.ChangeSelectorColor(Color.white, Menu.SelectorType.ARROW);
          TileManager._Ring.position = new Vector3(100f, 0f, 0f);
          return;
        }
        /// Swap level
        // Swap level display
        Transform map0 = Menu._CurrentMenu._menu.GetChild(0).GetChild(selectedlevel),
          map1 = Menu._CurrentMenu._menu.GetChild(0).GetChild(selection);
        map0.GetChild(0).parent = map1;
        map1.GetChild(0).parent = map0;
        map0.GetChild(0).localPosition = map1.GetChild(0).localPosition = Vector3.zero;
        // Swap actual level data and save
        string savemap = Levels._LevelCollections[Levels._CurrentLevelCollectionIndex]._leveldata[selectedlevel];
        Levels._LevelCollections[Levels._CurrentLevelCollectionIndex]._leveldata[selectedlevel] = Levels._LevelCollections[Levels._CurrentLevelCollectionIndex]._leveldata[selection];
        Levels._LevelCollections[Levels._CurrentLevelCollectionIndex]._leveldata[selection] = savemap;
        Levels.SaveLevels();
        // Set selection to -1
        selectedlevel = -1;
        TileManager._Ring.position = new Vector3(100f, 0f, 0f);
        Menu.ChangeSelectorColor(Color.white, Menu.SelectorType.ARROW);
      };
      if (cwidth > width)
      {
        cwidth = 0;
        cheight++;
      }
      //if(cwidth % 20 == 0)
      //  yield return new WaitForSecondsRealtime(0.005f);
    }
    yield return new WaitForSecondsRealtime(0.005f);
    holder.localScale = new Vector3(0.7f, 0.7f, 1f);
    // Set defaults
    selectedlevel = -1;
    TileManager._Ring.position = new Vector3(100f, 0f, 0f);
    Menu.ChangeSelectorColor(Color.white, Menu.SelectorType.ARROW);
    Menu._Menu_LevelOrganizer._currentSelection = 0;
    // Load level selections
    Menu.SwitchMenu(Menu._Menu_LevelOrganizer);
  }*/

  public static class SurvivalMode
  {
    public static bool _WavePlaying;
    public static int _Wave;

    public static float _Time_wave_intermission,
      _Timer_wave_start;

    public static int _Number_enemies_spawned;

    public static Vector3 _AllSpawn;

    public static ItemManager.Loadout[] _PlayerLoadouts;
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
      _PlayerLoadouts = new ItemManager.Loadout[4];
      for (var i = 0; i < _PlayerLoadouts.Length; i++)
      {
        _PlayerLoadouts[i] = new ItemManager.Loadout();
        _PlayerLoadouts[i]._equipment = new PlayerProfile.Equipment();
        _PlayerLoadouts[i]._two_weapon_pairs = true;
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
      if (FunctionsC.MusicManager._CurrentTrack != 3)
      {
        FunctionsC.MusicManager._CurrentTrack = 0;
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
      _PlayerLoadouts[playerId]._equipment._utilities_left = new UtilityScript.UtilityType[] { UtilityScript.UtilityType.GRENADE, };
      _PlayerLoadouts[playerId]._equipment._utilities_right = new UtilityScript.UtilityType[0];
      _PlayerLoadouts[playerId]._equipment._item_left0 = ItemManager.Items.KNIFE;
      _PlayerLoadouts[playerId]._equipment._item_right0 = ItemManager.Items.NONE;
      _PlayerLoadouts[playerId]._equipment._item_left1 = ItemManager.Items.NONE;
      _PlayerLoadouts[playerId]._equipment._item_right1 = ItemManager.Items.NONE;

      _PlayerLoadouts[playerId]._equipment._perks.Clear();

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
      _PlayerIter = 0;
      if (PlayerScript._Players != null)
      {
        PlayerScript alive_player = null;
        for (var i = 0; i < Settings._NumberPlayers; i++)
        {
          //if (i >= PlayerScript._Players.Count) continue;
          var p = PlayerScript._Players[i];
          if (p == null || p._ragdoll == null || p._ragdoll._dead) continue;
          alive_player = p;
          break;
        }

        // Check for an alive player
        if (alive_player == null) return;

        // Count number of players to spawn.. only used if trying to stay persistant or players joined?
        foreach (var p in PlayerScript._Players) if (p != null && !p._ragdoll._dead) _PlayerIter++;

        // Remove null / dead players
        for (var i = PlayerScript._Players.Count - 1; i >= 0; i--)
        {
          var p = PlayerScript._Players[i];
          if (p == null || p._ragdoll == null || p._ragdoll._dead)
          {
            ActiveRagdoll._Ragdolls.Remove(p._ragdoll);
            GameObject.Destroy(p.transform.parent.gameObject);
            PlayerScript._Players.RemoveAt(i);
            p = PlayerspawnScript._PlayerSpawns[0].SpawnPlayer(false);
            p.transform.position = alive_player.transform.position;
            p.transform.parent.gameObject.SetActive(true);
            FunctionsC.PlaySound(ref _audioListenerSource, "Ragdoll/Pop", 0.9f, 1.05f);
          }
        }

        // Heal and reload all players
        foreach (var p in PlayerScript._Players)
          if (p != null && p._ragdoll != null)
            HealPlayer(p);
      }

      // Increment wave
      _Wave++;
      _Text_Wave.text = $"{_Wave}";

      _AllSpawn = Vector3.zero;

      FunctionsC.PlaySound(ref GameScript._audioListenerSource, "Survival/Wave_Start", 1f, 1f);

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

    public static int GetHighestWave(int levelIndex)
    {
      return PlayerPrefs.GetInt($"SURVIVAL_MAP_{levelIndex}", 0);
    }

    public static void OnWaveEnd()
    {
      _Time_wave_intermission = 4f;

      _WavePlaying = false;

      // Get / save highest wave
      var highest_wave = PlayerPrefs.GetInt($"SURVIVAL_MAP_{Levels._CurrentLevelIndex}", 0);
      PlayerPrefs.SetInt($"SURVIVAL_MAP_{Levels._CurrentLevelIndex}", _Wave);
      // Unlock new map
      if (Levels._CurrentLevelIndex == 0 && highest_wave < 11 && _Wave == 11)
      {
        TogglePause();
        //_audioListenerSource.PlayOneShot(Menu2.GetNoise(Menu2.Noise.PURCHASE).Item1.clip);
        Menu2.PlayNoise(Menu2.Noise.PURCHASE);
        Menu2.GenericMenu(new string[] {
$@"<color={Menu2._COLOR_GRAY}>new survival map unlocked</color>

you survived 10 waves and have unlocked a <color=yellow>new survival map</color>!
"
        }, "nice", Menu2.MenuType.NONE, null, true, null,
        (Menu2.MenuComponent c) =>
        {
          TogglePause();
          Menu2.HideMenus();
        });
      }

      // Set random
      CustomObstacle.Randomize();

      // Record stat
      Stats.OverallStats._Waves_Played++;

      // Respawn players
      _PlayerIter = 0;
      if (PlayerScript._Players != null)
      {
        PlayerScript alive_player = null;
        for (var i = 0; i < Settings._NumberPlayers; i++)
        {
          //if (i >= PlayerScript._Players.Count) continue;
          var p = PlayerScript._Players[i];
          if (p == null || p._ragdoll == null || p._ragdoll._dead) continue;
          alive_player = p;
          break;
        }

        // Check for an alive player
        if (alive_player == null) return;

        // Count number of players to spawn.. only used if trying to stay persistant or players joined?
        foreach (var p in PlayerScript._Players) if (p != null && !p._ragdoll._dead) _PlayerIter++;

        // Remove null / dead players
        for (var i = PlayerScript._Players.Count - 1; i >= 0; i--)
        {
          var p = PlayerScript._Players[i];
          if (p == null || p._ragdoll == null || p._ragdoll._dead)
          {
            ActiveRagdoll._Ragdolls.Remove(p._ragdoll);
            GameObject.Destroy(p.transform.parent.gameObject);
            PlayerScript._Players.RemoveAt(i);
            p = PlayerspawnScript._PlayerSpawns[0].SpawnPlayer(false);
            p.transform.position = alive_player.transform.position;
            p.transform.parent.gameObject.SetActive(true);
            FunctionsC.PlaySound(ref _audioListenerSource, "Ragdoll/Pop", 0.9f, 1.05f);
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
        foreach (var p in PlayerScript._Players)
          if (p != null && p._ragdoll != null)
          {
            HealPlayer(p);
            p.RegisterUtilities();
            p._profile.UpdateIcons();
          }
      }
    }

    static void HealPlayer(PlayerScript p)
    {
      // Heal; check for armor perk
      //var healed = false;
      if (Shop.Perk.HasPerk(p._id, Shop.Perk.PerkType.ARMOR_UP))
      {
        if (p._ragdoll._health != 5)
        {
          p._ragdoll._health = 5;
          p._ragdoll.AddArmor();
          //healed = true;
        }
      }
      else if (p._ragdoll._health != 3)
      {
        p._ragdoll._health = 3;
        //healed = true;
      }

      // Particle system and noise if healed
      /*if (healed)
      {
        var particles = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.HEAL)[0];
        particles.transform.position = p._ragdoll._transform_parts._hip.position;
        particles.Play();
        Debug.Log("Healed");
      }*/

      // Update UI
      p._profile.UpdateHealthUI();
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
      if (PlayerScript._Players == null) return;

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
        var player = PlayerScript._Players[_ClosestSpawns_PlayerIter++ % PlayerScript._Players.Count];
        var spawn = _Spawn_Points[_ClosestSpawns_SpawnIter++ % _Spawn_Points.Count];

        if (player._ragdoll != null && !player._ragdoll._dead)
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
          else if (_ClosestSpawns.Count < Mathf.Clamp(Mathf.RoundToInt(PlayerScript._Players.Count * 1.4f), 2, 5))
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
        var enemy_ragdoll = enemy_next.GetRagdoll();

        enemy_ragdoll.ToggleRaycasting(false);

        if (Random.Range(0, 16) == 0f)
          enemy_next.SetRandomStrafe();

        // Check if others in front
        var hit = new RaycastHit();
        if (Physics.SphereCast(new Ray(enemy_ragdoll._hip.transform.position, MathC.Get2DVector(enemy_ragdoll._hip.transform.forward) * 100f), 0.5f, out hit, 100f, EnemyScript._Layermask_Ragdoll))
        {
          enemy_ragdoll.ToggleRaycasting(true);
          if (hit.distance > 10f) return;
          var ragdoll = ActiveRagdoll.GetRagdoll(hit.collider.gameObject);
          if (ragdoll == null || ragdoll._isPlayer) return;

          enemy_ragdoll._force += (enemy_ragdoll._hip.transform.right * (enemy_next._strafeRight ? 1f : -1f)) * Time.deltaTime * 3f;
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
            can._normalizedEnable = 0f;
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
            rb.drag = 0.1f;
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
      if (!_Paused && !Menu2._InMenus)
        for (var i = 0; i < Settings._NumberPlayers; i++)
          text += $"<color={PlayerProfile._Profiles[i].GetColorName(false)}>p{i + 1}:</color> {_PlayerScores[i]}\n";
      _Text_Scores.text = text;

      // Check if paused or in menu
      if (_Paused || Menu2._InMenus || CustomObstacle._CustomSpawners == null) return;

      IncrementalUpdate();

      CustomObstacle.HandleAll();

      // Center audio listener
      var als_pos = _audioListenerSource.transform.position;
      als_pos.y = 0f;
      _audioListenerSource.transform.position = als_pos;

      // Check if wave in progress
      if (_WavePlaying)
      {
        // Check for all players dead
        if (PlayerScript._All_Dead)
        {
          _WavePlaying = false;

          // Save highest wave
          Debug.Log($"{Levels._CurrentLevelIndex} : {_Wave - 1}");

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
                e?.GetRagdoll().ChangeColor((Color.red + Color.yellow) / 3f);//new Color(0, 30, 0));
                //e.GetRagdoll()._hip.transform.localScale *= 1.6f;
                break;
              case (EnemyType.ARMORED):
                movespeed = 0.25f;
                health = 4;
                e.GetRagdoll().ChangeColor(Color.gray);
                break;
            }

            if (e != null)
            {
              e._moveSpeed = movespeed + (-0.1f + Random.value * 0.2f);
              e.GetRagdoll()._health = health;
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

  // Rain sfx
  [System.NonSerialized]
  public AudioSource _Rain_Audio, _Thunder_Audio;
  [System.NonSerialized]
  public float _Thunder_Last, _Thunder_Samples_Last;
  public Light _Thunder_Light;

  // Update is called once per frame
  void Update()
  {

    if (!IsSurvival())
    {
      // Update level timer
      if (!TileManager._Level_Complete && PlayerScript._TimerStarted)
      {
        TileManager._LevelTimer += (Menu2._InMenus ? Time.unscaledDeltaTime : Time.deltaTime);
        if (!Menu2._InMenus)
          TileManager._Text_LevelTimer.text = string.Format("{0:0.000}", TileManager._LevelTimer);
      }

    }

    else
    {
      TileManager._LevelTimer += Time.deltaTime;
      TileManager._Text_LevelTimer.text = string.Format("{0:0.000}", TileManager._LevelTimer);
    }

    // Update controllers
    ControllerManager.Update();

    // Update enemies
    EnemyScript.UpdateEnemies();

    // Update music
    FunctionsC.MusicManager.Update();

    // Update menus
    Menu2.UpdateMenus();

    // Update playerprofiles
    foreach (var profile in PlayerProfile._Profiles)
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
      _lastInputCheck = Time.time;
      Settings._NumberPlayers = (ControllerManager._NumberGamepads) + (Settings._ForceKeyboard ? 1 : 0);
      if (Settings._NumberPlayers == 0)
        Settings._NumberPlayers = 1;
      var numcontrollers_save = Settings._NumberControllers;
      Settings._NumberControllers = (ControllerManager._NumberGamepads) + (Settings._ForceKeyboard ? 1 : 0);
      if (Settings._NumberControllers == 0 && Menu2._Confirmed_SwitchToKeyboard)
        Settings._NumberControllers = 1;

      if (Settings._NumberControllers != numcontrollers_save)
      {
        var ui = GameResources._UI_Player;
        for (var i = 1; i < 4; i++)
          ui.GetChild(i).gameObject.SetActive(i < Settings._NumberPlayers);

        // Pause if a controller was unplugged and playing
        if (!Menu2._InMenus && (!_EditorTesting) && Settings._NumberControllers != PlayerScript._Players.Count)
          Menu2.OnControllersChanged(Settings._NumberControllers - numcontrollers_save, numcontrollers_save);
      }
    }

    // Update survial mode
    if (IsSurvival() && !_EditorEnabled)
      SurvivalMode.Update();

    // Update progress bars
    ProgressBar.Update();

    // Check play mode
    if (!Menu2._InMenus)
    {
      if (!_Paused)
      {
        // Update ragdoll body sounds
        ActiveRagdoll.BodyPart_Handler.Update();

        // Check thunder sfx
        if (SceneThemes._Theme._rain)
        {
          if (Time.time - _Thunder_Last > 10f && Random.value < 0.001f)
          {
            _Thunder_Last = Time.time;

            FunctionsC.PlayAudioSource(ref _Thunder_Audio, 0.6f, 1.05f);

            var cpos = GameResources._Camera_Main.transform.position;
            cpos.y = 0f;
            _Thunder_Light.transform.position = cpos;
          }

          // Lightning timed with thunder volume
          if (
            Settings._Toggle_Lightning._value &&
            _Thunder_Light != null &&
            Time.time - _Thunder_Samples_Last > 0.05f
          )
          {
            var c = 0f;

            if (_Thunder_Audio.isPlaying)
            {
              _Thunder_Samples_Last = Time.time;

              var sample_length = 1024;
              var samples = new float[sample_length];
              _Thunder_Audio.clip.GetData(samples, _Thunder_Audio.timeSamples);

              var clipLoudness = 0f;
              foreach (var sample in samples)
              {
                clipLoudness += Mathf.Abs(sample);
              }
              clipLoudness /= sample_length;

              c = clipLoudness * 18f;
            }

            //RenderSettings.ambientLight = new Color(c, c, c);
            _Thunder_Light.intensity = c;
          }
        }

        // Check normal mode
        if (!IsSurvival() && !_EditorEnabled)
        {
          // Check endgame
          float timeToEnd = 2.2f;
          if (_inLevelEnd && !_GameEnded)
          {
            _levelEndTimer = Mathf.Clamp(_levelEndTimer + Time.deltaTime, 0f, timeToEnd);

            // Check time
            if (_levelEndTimer >= timeToEnd || (EnemyScript.NumberAlive() == 0 && Time.time - _goalPickupTime > 0.8f))
            {
              _inLevelEnd = false;
              _levelEndTimer = 0f;

              if (PlayerScript._Players != null)
                foreach (var p in PlayerScript._Players)
                {
                  if (p == null) continue;
                  p._hasExit = false;
                }

              OnLevelComplete();
            }
          }
          else _levelEndTimer = Mathf.Clamp(_levelEndTimer - Time.deltaTime, 0f, timeToEnd);
          // Check timer with particles
          {
            if (_LevelEndParticles == null) _LevelEndParticles = PlayerspawnScript._PlayerSpawns[0].GetComponent<ParticleSystem>();
            if (_levelEndTimer > 0f && !_LevelEndParticles.isPlaying)
              _LevelEndParticles.Play();
            else if (_levelEndTimer <= 0f && _LevelEndParticles.isPlaying)
              _LevelEndParticles.Stop();
            if (_LevelEndParticles.isPlaying && _levelEndTimer > 0f)
            {
              // Emit particles for feedback
              var emit = _LevelEndParticles.emission;
              emit.rateOverTime = _levelEndTimer * 60f;
            }
          }
        }
        // Enable editor
        if (!_EditorTesting)
        {
          if (Debug.isDebugBuild)
          {
            if (ControllerManager.GetKey(ControllerManager.Key.SPACE, ControllerManager.InputMode.HOLD) && ControllerManager.GetKey(ControllerManager.Key.E))
            {
              _EditorEnabled = !_EditorEnabled;
              if (_EditorEnabled) StartCoroutine(TileManager.EditorEnabled());
              else
              {
                string mapdata = TileManager.SaveMap();
                TileManager.EditorDisabled(mapdata);
                // Save map to map file and reload map selections
                TileManager.SaveFileOverwrite(mapdata);
              }
            }
            if (!_EditorEnabled)
            {
              if ((ControllerManager.GetKey(ControllerManager.Key.PAGE_UP, ControllerManager.InputMode.DOWN)) && IsSurvival()) SurvivalMode._Wave++;
              else
              {
                if (ControllerManager.GetKey(ControllerManager.Key.MULTIPLY_NUMPAD, ControllerManager.InputMode.HOLD)) OnLevelComplete();
              }
              // Fast skip
            }
          }

          if (!_EditorEnabled)
          {
            if (!TileManager._LoadingMap)
            {
              if (ControllerManager.GetKey(ControllerManager.Key.PAGE_UP, ControllerManager.InputMode.DOWN))
              {
                if (Levels._CurrentLevelIndex < (Levels._CurrentLevelCollection?._leveldata.Length ?? 0) && Levels.LevelCompleted(Levels._CurrentLevelIndex + 1))
                {
                  NextLevel(Levels._CurrentLevelIndex + 1);
                }
              }
              else if (ControllerManager.GetKey(ControllerManager.Key.PAGE_DOWN, ControllerManager.InputMode.DOWN))
              {
                if (Levels._CurrentLevelIndex > 0)
                  NextLevel(Levels._CurrentLevelIndex - 1);
              }
            }
          }
        }
        else
        {

          if (!_EditorEnabled && Time.unscaledTime - TileManager._EditorSwitchTime > 0.5f)
          {
            if (ControllerManager.GetKey(ControllerManager.Key.F1))
            {
              _EditorEnabled = true;
              StartCoroutine(TileManager.EditorEnabled());

              TileManager.EditorMenus._Menu_EditorTesting.gameObject.SetActive(false);
            }
          }

          // Check exit
          if (ControllerManager.GetKey(ControllerManager.Key.F2))
          {

            _EditorTesting = false;

            // If editing, save map
            if (_EditorEnabled)
            {
              _EditorEnabled = false;
              TileManager.SaveFileOverwrite(TileManager.SaveMap());
              TileManager.EditorDisabled(null);
            }

            // Exit to menus
            TogglePause(Menu2.MenuType.EDITOR_LEVELS);
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
        if (ControllerManager.GetKey(ControllerManager.Key.F3))
        {
          Settings._CameraType._value = !Settings._CameraType._value;
          Settings.SetPostProcessing();
        }

        if (_EditorEnabled)
          TileManager.HandleInput();
      }
    }
    else
    {
      foreach (var profile in PlayerProfile._Profiles)
        profile.HandleMenuInput();

      // Check if checking for controllers
      if (Menu2._CurrentMenu._type == Menu2.MenuType.CONTROLLERS_CHANGED)
      {
        var numplayers = (ControllerManager._NumberGamepads) + (Settings._ForceKeyboard ? 1 : 0);
        if (numplayers >= Menu2._Save_NumPlayers)
        {
          Menu2.OnControllersChangedFix();
        }
      }
    }
  }

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

  float[] _controlAxis = new float[4];
  static bool _checkmouse;
  static Vector2 _lastmousepos;

  // Assists with controls
  public class PlayerProfile
  {
    static int CurrentSettingsID;
    public static int _CurrentSettingsProfileID
    {
      get
      {
        return CurrentSettingsID;
      }
      set
      {
        CurrentSettingsID = value % 4;
        if (CurrentSettingsID < 0)
          CurrentSettingsID = 3;
      }
    }
    public static PlayerProfile _CurrentSettingsProfile { get { return _Profiles[_CurrentSettingsProfileID]; } }

    static int _ID;
    public static PlayerProfile[] _Profiles;

    int _id;
    string _playerPrefsPrefix
    {
      get
      {
        return $"PlayerProfile{_id}_";
      }
    }

    // The player this profile is attatched to
    public PlayerScript _player
    {
      get
      {
        if (PlayerScript._Players == null) return null;
        foreach (var player in PlayerScript._Players)
          if (player == null) continue;
          else if (player._id == _id) return player;
        return null;
      }
    }

    public float[] _directionalAxis;

    int loadoutIndex;
    public int _loadoutIndex
    {
      get { return loadoutIndex; }
      set
      {
        if (Levels._HardcodedLoadout != null && !GameScript._EditorTesting) return;
        if (_GameMode != GameModes.CLASSIC) return;
        var iter = ItemManager.Loadout._Loadouts.Length;
        var difference = value - loadoutIndex;
        while (iter >= 0)
        {
          loadoutIndex = (loadoutIndex + difference) % ItemManager.Loadout._Loadouts.Length;
          if (loadoutIndex < 0) loadoutIndex = ItemManager.Loadout._Loadouts.Length + loadoutIndex;
          if (!_equipment.IsEmpty()) break;
          iter--;
        }
        if (iter == -1 && _equipment.IsEmpty()) loadoutIndex = 0;
        PlayerPrefs.SetInt($"{_playerPrefsPrefix}loadoutIndex", loadoutIndex);
        UpdateIcons();
        // If in loadout menu, update colors
        if (Menu2._InMenus && Menu2._CurrentMenu._type == Menu2.MenuType.SELECT_LOADOUT)
        {
          Menu2._CanRender = false;
          Menu2.RenderMenu();
        }
      }
    }
    public Equipment _equipment
    {
      get
      {
        return _loadout._equipment;
      }
    }
    public ItemManager.Loadout _loadout
    {
      get
      {
        // Level packs
        if (Levels._HardcodedLoadout != null && !GameScript._EditorTesting) return Levels._HardcodedLoadout;

        // SURVIVAL mode
        if (_GameMode == GameModes.SURVIVAL && SurvivalMode._PlayerLoadouts != null) return SurvivalMode._PlayerLoadouts[_id];

        // CLASSIC mode
        return ItemManager.Loadout._Loadouts[_loadoutIndex];
      }
    }

    bool reloadSidesSameTime;
    public bool _reloadSidesSameTime
    {
      get { return reloadSidesSameTime; }
      set
      {
        if (reloadSidesSameTime == value) return;
        reloadSidesSameTime = value;
        PlayerPrefs.SetInt($"{_playerPrefsPrefix}reloadSidesSameTime", value ? 1 : 0);
      }
    }

    public ItemManager.Items _item_left { get { return (_equipmentIndex == 0 ? _equipment._item_left0 : _equipment._item_left1); } set { if (_equipmentIndex == 0) _equipment._item_left0 = value; else _equipment._item_left1 = value; } }
    public ItemManager.Items _item_right { get { return (_equipmentIndex == 0 ? _equipment._item_right0 : _equipment._item_right1); } set { if (_equipmentIndex == 0) _equipment._item_right0 = value; else _equipment._item_right1 = value; } }
    public ItemManager.Items _item_left_other { get { return (_equipmentIndex == 1 ? _equipment._item_left0 : _equipment._item_left1); } set { if (_equipmentIndex == 1) _equipment._item_left0 = value; else _equipment._item_left1 = value; } }
    public ItemManager.Items _item_right_other { get { return (_equipmentIndex == 1 ? _equipment._item_right0 : _equipment._item_right1); } set { if (_equipmentIndex == 1) _equipment._item_right0 = value; else _equipment._item_right1 = value; } }

    // Used for two sets of equipment
    int equipmentIndex;
    public int _equipmentIndex
    {
      get { return equipmentIndex; }
      set { equipmentIndex = value % 2; }
    }

    public class Equipment
    {
      public Equipment()
      {
        _utilities_left = new UtilityScript.UtilityType[0];
        _utilities_right = new UtilityScript.UtilityType[0];

        _perks = new List<Shop.Perk.PerkType>();
      }

      public ItemManager.Items _item_left0, _item_right0, _item_left1, _item_right1;
      public UtilityScript.UtilityType[] _utilities_left, _utilities_right;
      public List<Shop.Perk.PerkType> _perks;

      public bool HasWeapons0()
      {
        return _item_left0 != ItemManager.Items.NONE ||
          _item_right0 != ItemManager.Items.NONE;
      }
      public bool HasWeapons1()
      {
        return _item_left1 != ItemManager.Items.NONE ||
          _item_right1 != ItemManager.Items.NONE;
      }

      public bool IsEmpty()
      {
        return !HasWeapons0() && !HasWeapons1() &&
          _utilities_left.Length == 0 &&
          _utilities_right.Length == 0 &&
          _perks.Count == 0;
      }
    }

    public static Color[] _Colors = new Color[] { Color.blue, Color.red, Color.yellow, Color.cyan, Color.white, Color.black, new Color(1f, 0.6f, 0f) };

    bool holdRun = true, faceMovement = false;
    public bool _holdRun
    {
      get
      {
        return holdRun;
      }
      set
      {
        holdRun = value;
        // Save pref
        PlayerPrefs.SetInt($"{_playerPrefsPrefix}holdRun", holdRun ? 1 : 0);
      }
    }
    public bool _faceMovement
    {
      get
      {
        return faceMovement;
      }
      set
      {
        faceMovement = value;
        // Save pref
        PlayerPrefs.SetInt($"{_playerPrefsPrefix}faceDir", faceMovement ? 1 : 0);
      }
    }
    int playerColor;
    public int _playerColor
    {
      get
      {
        return playerColor;
      }
      set
      {
        // Clamp
        playerColor = value % _Colors.Length;
        if (playerColor < 0)
          playerColor = _Colors.Length - 1;
        // Update UI
        Transform ui = GameResources._UI_Player;
        ui.GetChild(_id).GetChild(0).GetComponent<TextMesh>().color = GetColor();
        // Check for alive player
        if (PlayerScript._Players != null)
          foreach (PlayerScript p in PlayerScript._Players)
            if (p._id == _id && !p._ragdoll._dead) p._ragdoll.ChangeColor(GetColor());
        // Save pref
        PlayerPrefs.SetInt($"{_playerPrefsPrefix}color", playerColor);
      }
    }
    Transform _UI { get { return GameResources._UI_Player.GetChild(_id); } }

    MeshRenderer[] _health_UI, _perk_UI;

    public PlayerProfile()
    {
      _id = _ID++;

      if (_Profiles == null) _Profiles = new PlayerProfile[4];
      _Profiles[_id] = this;

      _directionalAxis = new float[3];

      // Load profile settings
      LoadPrefs();

      // Check empty loadout
      ChangeLoadoutIfEmpty();

      // Update profile equipment icons
      UpdateIcons();
      CreateHealthUI(1);
    }

    // Check empty loadout and switch to a new one if empty
    public void ChangeLoadoutIfEmpty(int max_loadout = 4)
    {
      // Bounds-check _loadoutIndex
      if (max_loadout != 4 && _loadoutIndex > max_loadout)
        _loadoutIndex = 0;
      else if (_equipment.IsEmpty())
        _loadoutIndex++;

      // Update UI
      UpdateIcons();
    }

    public void HandleMenuInput()
    {
      // Accommodate for _ForceKeyboard; checking for -1 controllerID and controllerID > Gamepad.all.Count
      if (Settings._ForceKeyboard && _id == 0)
        return;
      if (_id + (Settings._ForceKeyboard ? -1 : 0) >= ControllerManager._NumberGamepads)
        return;
      // Check axis selections
      for (int i = 0; i < 2; i++)
      {
        float y = 0;
        switch (i)
        {
          case (0):
            y = ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.LSTICK_Y);
            break;
          case (1):
            y = ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.DPAD_Y);
            break;
        }
        if (y > 0.75f)
        {
          if (_directionalAxis[i] <= 0f)
          {
            _directionalAxis[i] = 1f;
            Up();
          }
        }
        else if (y < -0.75f)
        {
          if (_directionalAxis[i] >= 0f)
          {
            _directionalAxis[i] = -1f;
            Down();
          }
        }
        else
          _directionalAxis[i] = 0f;
      }

    }

    public void HandleInput()
    {
      // Accommodate for _ForceKeyboard; checking for -1 controllerID and controllerID > Gamepad.all.Count
      if (Settings._ForceKeyboard && _id == 0 || _id + (Settings._ForceKeyboard ? -1 : 0) >= ControllerManager._NumberGamepads)
      {
        // Check loadout change
        if (ControllerManager.GetKey(ControllerManager.Key.Z))
          _loadoutIndex--;
        if (ControllerManager.GetKey(ControllerManager.Key.C))
          _loadoutIndex++;
        return;
      }
      // Check axis selections
      var y = ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.DPAD_X);
      if (y > 0.75f)
      {
        if (_directionalAxis[2] <= 0f)
        {
          _directionalAxis[2] = 1f;
          _loadoutIndex++;
        }
      }
      else if (y < -0.75f)
      {
        if (_directionalAxis[2] >= 0f)
        {
          _directionalAxis[2] = -1f;
          _loadoutIndex--;
        }
      }
      else
        _directionalAxis[2] = 0f;
    }

    void Up()
    {
      Menu2.SendInput(Menu2.Input.UP);
      FunctionsC.OnControllerInput();
    }
    void Down()
    {
      Menu2.SendInput(Menu2.Input.DOWN);
      FunctionsC.OnControllerInput();
    }

    public Color GetColor()
    {
      return _Colors[playerColor];
    }
    public string GetColorName(bool visual = true)
    {
      switch (playerColor)
      {
        case (0):
          return "blue";
        case (1):
          return "red";
        case (2):
          return "yellow";
        case (3):
          return visual ? "cyan" : "#00FFFF";
        case (4):
          return "white";
        case (5):
          return "black";
        case (6):
          return "orange";
      }
      return "";
    }

    public void LoadPrefs()
    {
      _playerColor = PlayerPrefs.GetInt($"{_playerPrefsPrefix}color", _id);
      _holdRun = PlayerPrefs.GetInt($"{_playerPrefsPrefix}holdRun", 1) == 1;
      _faceMovement = PlayerPrefs.GetInt($"{_playerPrefsPrefix}faceDir", 1) == 1;
      _loadoutIndex = PlayerPrefs.GetInt($"{_playerPrefsPrefix}loadoutIndex", 0);
      _reloadSidesSameTime = PlayerPrefs.GetInt($"{_playerPrefsPrefix}reloadSidesSameTime", 1) == 1;
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
      _health_UI = new MeshRenderer[] { _UI.GetChild(3).gameObject.GetComponent<MeshRenderer>(), _UI.GetChild(4).gameObject.GetComponent<MeshRenderer>(), _UI.GetChild(5).gameObject.GetComponent<MeshRenderer>() };
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
      UpdateHealthUI(_player._ragdoll._health);
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

      public void Init(System.Tuple<Transform, int> data, int index, System.Tuple<PlayerScript, bool, ActiveRagdoll.Side> item_data = null)
      {
        _base = data.Item1;
        _base.localPosition += new Vector3(_Offset.x, _Offset.y, 0f);
        _ammoCount = _ammoVisible = data.Item2;

        // Spawn ammo border
        _ammoUI = GameObject.Instantiate(GameObject.Find("AmmoUI") as GameObject).transform;
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
            if (side == ActiveRagdoll.Side.LEFT) ammo = player._ragdoll._itemL != null ? player._ragdoll._itemL.Clip() : ammo;
            else ammo = player._ragdoll._itemR != null ? player._ragdoll._itemR.Clip() : ammo;
          else
            if (side == ActiveRagdoll.Side.LEFT) ammo = player._utilities_left != null ? player._utilities_left.Count : ammo;
          else ammo = player._utilities_right != null ? player._utilities_right.Count : ammo;
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
          _ammo[i].GetComponent<Renderer>().sharedMaterial = _Singleton._item_materials[0];
          if (i >= ammo) _ammo[i].gameObject.SetActive(false);
        }
      }

      // Update bullets
      public void Clip_Deincrement()
      {
        if (_ammoVisible <= 0 || _ammoVisible - 1 >= _ammo.Length) return;
        _ammo[--_ammoVisible].gameObject.SetActive(false);
      }
      public void Clip_Reload(bool one_at_at_time)
      {
        if (one_at_at_time)
        {
          if (_ammoVisible >= _ammo.Length) return;
          _ammo[_ammoVisible++].gameObject.SetActive(true);
          return;
        }
        foreach (var t in _ammo)
          t.gameObject.SetActive(true);
        _ammoVisible = _ammoCount;
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
      if (side == ActiveRagdoll.Side.RIGHT && _item_left != ItemManager.Items.NONE) return 1;
      return 0;
    }

    public void ItemUse(ActiveRagdoll.Side side)
    {
      _weaponIcons[GetItemIcons(side)]?.Clip_Deincrement();
    }
    public void ItemReload(ActiveRagdoll.Side side, bool one_at_a_time)
    {
      _weaponIcons[GetItemIcons(side)]?.Clip_Reload(one_at_a_time);
    }
    public void ItemSetClip(ActiveRagdoll.Side side, int clip)
    {
      _weaponIcons[GetItemIcons(side)]?.UpdateIconManual(clip);
    }

    ItemIcon GetUtility(ActiveRagdoll.Side side)
    {
      var startIter = (_item_left == ItemManager.Items.NONE ? 0 : 1) + (_item_right == ItemManager.Items.NONE ? 0 : 1);
      var addIter = 0;
      if (side == ActiveRagdoll.Side.LEFT)
        addIter = 0;
      else
      {
        if (_equipment._utilities_left.Length == 0)
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
    public void UtilityReload(ActiveRagdoll.Side side, bool one_at_a_time)
    {
      var utility = GetUtility(side);
      utility?.Clip_Reload(one_at_a_time);
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
      var utils = _equipment._utilities_left;
      if (side == ActiveRagdoll.Side.RIGHT)
        utils = _equipment._utilities_right;
      // Check for no utilities
      if (utils.Length == 0)
        return 0;
      // Check for special utils
      if (utils[0] == UtilityScript.UtilityType.SHURIKEN)
        return utils.Length * 2;
      return utils.Length;
    }

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
      var bg = _UI.GetChild(1).transform;

      // Check for empty
      if (_weaponIcons.Length == 2 && _item_left == ItemManager.Items.NONE && _item_right == ItemManager.Items.NONE)
      {
        bg.localPosition = new Vector3(0f, -0.05f, 0f);
        bg.localScale = new Vector3(0.6f, 0.74f, 0.001f);
        return;
      }
      int equipmentIter = 0;
      if (_item_left != ItemManager.Items.NONE)
      {
        _weaponIcons[equipmentIter] = new ItemIcon();
        _weaponIcons[equipmentIter].Init(LoadIcon(_item_left.ToString(), equipmentIter), equipmentIter++, System.Tuple.Create(_player, false, ActiveRagdoll.Side.LEFT));
      }
      if (_item_right != ItemManager.Items.NONE)
      {
        _weaponIcons[equipmentIter] = new ItemIcon();
        _weaponIcons[equipmentIter].Init(LoadIcon(_item_right.ToString(), equipmentIter), equipmentIter++, System.Tuple.Create(_player, false, ActiveRagdoll.Side.RIGHT));
      }
      // Load utilities
      var loaded_utils = new List<System.Tuple<Transform, int, int, ActiveRagdoll.Side>>();
      if (_equipment._utilities_left.Length > 0)
      {
        var util_data = LoadIcon(_equipment._utilities_left[0].ToString(), equipmentIter);
        loaded_utils.Add(System.Tuple.Create(util_data.Item1, utilLength_left, equipmentIter++, ActiveRagdoll.Side.LEFT));
      }
      if (_equipment._utilities_right.Length > 0)
      {
        var util_data = LoadIcon(_equipment._utilities_right[0].ToString(), equipmentIter);
        loaded_utils.Add(System.Tuple.Create(util_data.Item1, utilLength_right, equipmentIter++, ActiveRagdoll.Side.RIGHT));
      }
      foreach (var util in loaded_utils)
      {
        _weaponIcons[util.Item3] = new ItemIcon();
        _weaponIcons[util.Item3].Init(System.Tuple.Create(util.Item1, util.Item2), util.Item3, System.Tuple.Create(_player, true, util.Item4));
      }
      loaded_utils = null;
      // Local parsing function
      System.Tuple<Transform, int> LoadIcon(string name, int iter)
      {
        // Get icon
        var item = ItemManager.GetItemUI(name, _player);
        Transform t = item.Item1;
        t.parent = _UI;
        // Move BG
        bg.localPosition = new Vector3(0.05f + (iter + 1) * 0.4f, -0.05f, 0f);
        bg.localScale = new Vector3(0.6f, 0.74f + (iter + 1) * 0.85f, 0.001f);
        // Set transform
        t.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        t.localPosition = new Vector3(0.7f + iter * 0.8f, 0f, 0f);
        t.localEulerAngles = new Vector3(0f, 90f, 0f);
        switch (t.name)
        {
          case ("KNIFE"):
          case ("ROCKET_FIST"):
            t.localPosition += new Vector3(-0.11f, -0.02f, 0f);
            t.localScale = new Vector3(0.13f, 0.13f, 0.13f);
            t.localEulerAngles += new Vector3(90f, 0f, 0f);
            break;
          case ("AXE"):
            t.localPosition += new Vector3(-0.11f, 0.03f, 0f);
            t.localScale = new Vector3(0.1f, 0.12f, 0.1f);
            t.localEulerAngles += new Vector3(90f, 0f, 0f);
            break;
          case ("BAT"):
            t.localPosition += new Vector3(-0.15f, 0f, 0f);
            t.localScale = new Vector3(0.11f, 0.12f, 0.11f);
            t.localEulerAngles += new Vector3(81f, 0f, 0f);
            break;
          case ("SWORD"):
            t.localPosition += new Vector3(-0.2f, 0f, 0f);
            t.localScale = new Vector3(0.11f, 0.1f, 0.11f);
            t.localEulerAngles = new Vector3(8f, 0f, -75f);
            break;
          case ("PISTOL_SILENCED"):
          case ("PISTOL"):
          case ("DOUBLE_PISTOL"):
            t.localPosition += new Vector3(-0.19f, 0.06f, 0f);
            t.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            break;
          case ("MACHINE_PISTOL"):
            t.localPosition += new Vector3(-0.19f, 0.08f, 0f);
            t.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            break;
          case ("REVOLVER"):
            t.localPosition += new Vector3(-0.22f, -0.01f, 0f);
            t.localScale = new Vector3(0.13f, 0.13f, 0.13f);
            break;
          case ("SHOTGUN_DOUBLE"):
            t.localPosition += new Vector3(0.08f, 0.04f, 0f);
            t.localScale = new Vector3(0.16f, 0.16f, 0.16f);
            break;
          case ("SHOTGUN_PUMP"):
            t.localPosition += new Vector3(0f, 0.02f, 0f);
            t.localScale = new Vector3(0.1f, 0.09f, 0.1f);
            break;
          case ("SHOTGUN_BURST"):
            t.localPosition += new Vector3(0.01f, 0f, 0f);
            t.localScale = new Vector3(0.11f, 0.1f, 0.13f);
            break;
          case ("UZI"):
            t.localPosition += new Vector3(-0.16f, 0.07f, 0f);
            t.localScale = new Vector3(0.14f, 0.14f, 0.14f);
            break;
          case ("AK47"):
          case ("FLAMETHROWER"):
            t.localPosition += new Vector3(-0.11f, 0.03f, 0f);
            t.localScale = new Vector3(0.09f, 0.09f, 0.09f);
            break;
          case ("M16"):
            t.localPosition += new Vector3(-0.14f, 0.03f, 0f);
            t.localScale = new Vector3(0.09f, 0.11f, 0.08f);
            break;
          case ("DMR"):
          case ("RIFLE"):
          case ("RIFLE_LEVER"):
            t.localPosition += new Vector3(-0.14f, 0.03f, 0f);
            t.localScale = new Vector3(0.09f, 0.11f, 0.08f);
            break;
          case ("CROSSBOW"):
            t.localPosition += new Vector3(-0.35f, -0.07f, 0f);
            t.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            t.localEulerAngles = new Vector3(-30f, 90f, 90f);
            break;
          case ("GRENADE_LAUNCHER"):
            t.localPosition += new Vector3(-0.21f, 0.03f, 0f);
            t.localScale = new Vector3(0.11f, 0.11f, 0.11f);
            break;
          case ("SNIPER"):
            t.localPosition += new Vector3(-0.14f, 0.03f, 0f);
            t.localScale = new Vector3(0.09f, 0.11f, 0.08f);
            break;
          case ("GRENADE_IMPACT"):
          case ("GRENADE"):
            t.localPosition += new Vector3(-0.23f, 0.03f, 0f);
            t.localEulerAngles = new Vector3(0f, 0f, 0f);
            t.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            break;
          case ("C4"):
            t.localPosition += new Vector3(-0.17f, 0.0f, 0f);
            t.localEulerAngles = new Vector3(0f, 90f, -90f);
            t.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            break;
          case ("SHURIKEN"):
          case ("SHURIKEN_BIG"):
            t.localPosition += new Vector3(-0.22f, 0.02f, 0f);
            t.localEulerAngles = new Vector3(90f, 0f, 0f);
            t.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            break;
          case ("KUNAI_EXPLOSIVE"):
          case ("KUNAI_STICKY"):
            t.localPosition += new Vector3(-0.14f, -0.03f, 0f);
            t.localEulerAngles = new Vector3(0f, 90f, 90f);
            t.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            break;
          case ("STOP_WATCH"):
          case ("INVISIBILITY"):
            t.localPosition += new Vector3(-0.25f, 0f, 0f);
            t.localEulerAngles = new Vector3(0f, 90f, -90f);
            t.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            break;
        }
        // Color
        var meshes = new List<Renderer>();
        var transform_base = t.Find("Mesh");
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
            if (shared[i].name.Equals("Item")) shared[i] = _Singleton._item_materials[0];
            else shared[i] = _Singleton._item_materials[1];
            mesh.sharedMaterials = shared;
          }
        return item;
      }
    }

    //
    public void UpdatePerkIcons()
    {
      // UI
      if (_perk_UI == null)
        _perk_UI = new MeshRenderer[] { _UI.GetChild(2).GetChild(0).GetComponent<MeshRenderer>(), _UI.GetChild(2).GetChild(1).GetComponent<MeshRenderer>(), _UI.GetChild(2).GetChild(2).GetComponent<MeshRenderer>(), _UI.GetChild(2).GetChild(3).GetComponent<MeshRenderer>() };

      // Current perks
      var perks = Shop.Perk.GetPerks(_id);

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

      CreateHealthUI(_player == null ? 1 : _player._ragdoll._health);
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
      if (!Menu2._InMenus && !_EditorEnabled)
        TogglePause();
  }

  static Coroutine _movecam = null;
  public static void ToggleCameraLight(bool toggle)
  {
    if (toggle && _CameraLight.intensity != 0f) return;
    else if (!toggle && _CameraLight.intensity == 0f) return;
    if (_movecam != null) return;
    _movecam = _Singleton.StartCoroutine(ToggleCameraLightCo(toggle));
  }
  static IEnumerator ToggleCameraLightCo(bool toggle)
  {
    if (toggle) _CameraLight.enabled = true;
    float t = 0f;
    while (t < 1f)
    {
      t = Mathf.Clamp(t + 0.02f, 0f, 1f);
      _CameraLight.intensity = Mathf.Lerp(0f, 1.1f, toggle ? t : 1f - t);
      yield return new WaitForSecondsRealtime(0.01f);
    }
    if (!toggle) _CameraLight.enabled = false;
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
        _Loadouts = new Loadout[10];
        for (int i = 0; i < _Loadouts.Length; i++)
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
            _equipment._item_left0, _equipment._item_right0,
            _equipment._item_left1, _equipment._item_right1,
          } : new Items[] {
            _equipment._item_left0, _equipment._item_right0,
          }))
            total += GetItemValue(item);
          // Utils
          foreach (var utility in _equipment._utilities_left)
            total += GetUtilityValue(utility);
          foreach (var utility in _equipment._utilities_right)
            total += GetUtilityValue(utility);
          // Perks
          foreach (var perk in _equipment._perks)
            total += GetPerkValue(perk);
          return _POINTS_MAX - total;
        }
      }

      bool two_weapon_pairs;
      public bool _two_weapon_pairs
      {
        get { return two_weapon_pairs; }
        set
        {
          two_weapon_pairs = value;
        }
      }

      public PlayerProfile.Equipment _equipment;

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
        var currentEquipValue = GetItemValue(side == ActiveRagdoll.Side.LEFT ? (index == 0 ? _equipment._item_left0 : _equipment._item_left1) : (index == 0 ? _equipment._item_right0 : _equipment._item_right1));
        return (_available_points + currentEquipValue - GetItemValue(item) >= 0);
      }
      public bool CanEquipUtility(ActiveRagdoll.Side side, UtilityScript.UtilityType utility)
      {
        var currentUtilities = side == ActiveRagdoll.Side.LEFT ? _equipment._utilities_left : _equipment._utilities_right;
        var currentEquipValue = 0;
        // Check if adding a new utility or adding additional
        if (currentUtilities.Length > 0 && utility != currentUtilities[0])
        {
          currentEquipValue = GetUtilityValue(currentUtilities[0]) * currentUtilities.Length;
        }
        return (_available_points + currentEquipValue - GetUtilityValue(utility) >= 0);
      }

      // Save the loadout
      public void Save()
      {
        var savestring = "";
        if (_equipment._item_left0 != Items.NONE) savestring += $"item_left0:{_equipment._item_left0.ToString()}|";
        if (_equipment._item_right0 != Items.NONE) savestring += $"item_right0:{_equipment._item_right0.ToString()}|";
        if (_equipment._item_left1 != Items.NONE) savestring += $"item_left1:{_equipment._item_left1.ToString()}|";
        if (_equipment._item_right1 != Items.NONE) savestring += $"item_right1:{_equipment._item_right1.ToString()}|";
        if (_equipment._utilities_left != null && _equipment._utilities_left.Length > 0) savestring += $"utility_left:{_equipment._utilities_left[0].ToString()},{_equipment._utilities_left.Length}|";
        if (_equipment._utilities_right != null && _equipment._utilities_right.Length > 0) savestring += $"utility_right:{_equipment._utilities_right[0].ToString()},{_equipment._utilities_right.Length}|";
        foreach (var perk in _equipment._perks)
          savestring += $"perk:{perk}|";
        var pairs = _two_weapon_pairs ? 1 : 0;
        savestring += $"two_pairs:{pairs}|";
        PlayerPrefs.SetString($"loadout:{_id}", savestring);
      }

      public void Load()
      {
        _equipment = new PlayerProfile.Equipment();

        try
        {
          var loadstring = PlayerPrefs.GetString($"loadout:{_id}", "");
          if (loadstring.Trim().Length == 0) return;
          // Parse load string
          foreach (var split in loadstring.Split('|'))
          {
            if (split.Trim().Length == 0) continue;
            var split0 = split.Split(':');
            var variable = split0[0];
            var val = split0[1];
            switch (variable)
            {
              case "item_left0":
                var item = (Items)System.Enum.Parse(typeof(Items), val, true);
                _equipment._item_left0 = item;
                break;
              case "item_right0":
                item = (Items)System.Enum.Parse(typeof(Items), val, true);
                _equipment._item_right0 = item;
                break;
              case "item_left1":
                item = (Items)System.Enum.Parse(typeof(Items), val, true);
                _equipment._item_left1 = item;
                break;
              case "item_right1":
                item = (Items)System.Enum.Parse(typeof(Items), val, true);
                _equipment._item_right1 = item;
                break;
              case "utility_left":
                var split1 = val.Split(',');
                var util = (UtilityScript.UtilityType)System.Enum.Parse(typeof(UtilityScript.UtilityType), split1[0], true);
                var count = int.Parse(split1[1]);
                var utils = new UtilityScript.UtilityType[count];
                for (var i = 0; i < count; i++)
                  utils[i] = util;
                _equipment._utilities_left = utils;
                break;
              case "utility_right":
                split1 = val.Split(',');
                util = (UtilityScript.UtilityType)System.Enum.Parse(typeof(UtilityScript.UtilityType), split1[0], true);
                count = int.Parse(split1[1]);
                utils = new UtilityScript.UtilityType[count];
                for (var i = 0; i < count; i++)
                  utils[i] = util;
                _equipment._utilities_right = utils;
                break;
              case "two_pairs":
                _two_weapon_pairs = int.Parse(val) == 1;
                break;
              case "perk":
                var perk = (Shop.Perk.PerkType)System.Enum.Parse(typeof(Shop.Perk.PerkType), val, true);
                _equipment._perks.Add(perk);
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
      MACHINE_PISTOL,
      DOUBLE_PISTOL,
      GRENADE_HOLD,
      AK47,
      M16,
      ROCKET_LAUNCHER,
      SWORD,
      AXE,
      CROSSBOW,
      GRENADE_LAUNCHER,
      FLAMETHROWER,
      ROCKET_FIST,
      STICKY_GUN,
    }

    // Spawn a single item
    public static ItemScript SpawnItem(Items item)
    {
      if (item == Items.NONE) return null;
      if (_Items == null) _Items = new List<Item>();
      var name = item.ToString();
      GameObject new_item = Instantiate(Resources.Load($"Items/{name}") as GameObject);
      new_item.name = name;
      var script = new_item.GetComponent<ItemScript>();
      script._type = item;
      if (_Container == null) _Container = GameObject.Find("Items").transform;
      new_item.transform.parent = _Container;
      new_item.transform.position = new Vector3(0f, -10f, 0f);
      _Items.Add(new Item(item, ref new_item));
      return script;
    }

    public static System.Tuple<Transform, int> GetItemUI(string item, PlayerScript player)
    {
      GameObject new_item = Instantiate(Resources.Load($"Items/{item}") as GameObject);
      new_item.layer = 11;
      var script = new_item.GetComponent<ItemScript>();
      script._ragdoll = player != null ? player._ragdoll : null;
      int clip = script.ClipSize();
      GameObject.Destroy(script);
      var collider = new_item.GetComponent<Collider>();
      if (collider) GameObject.Destroy(collider);
      var rigidbody = new_item.GetComponent<Rigidbody>();
      if (rigidbody) GameObject.Destroy(rigidbody);
      new_item.name = item;
      return System.Tuple.Create(new_item.transform, clip);
    }

    public static GameObject GetItem(Items itemType)
    {
      foreach (Item i in _Items)
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
          return 0;
        case Items.REVOLVER:
          return 4;
        case Items.UZI:
          return 4;
        case Items.KNIFE:
          return 1;
        case Items.AXE:
          return 3;
        case Items.BAT:
          return 4;
        case Items.ROCKET_FIST:
          return 3;
        case Items.SHOTGUN_PUMP:
          return 4;
        case Items.SHOTGUN_DOUBLE:
          return 4;
        case Items.SHOTGUN_BURST:
          return 5;
        case Items.PISTOL:
          break;
        case Items.PISTOL_SILENCED:
          return 3;
        case Items.MACHINE_PISTOL:
          return 3;
        case Items.DOUBLE_PISTOL:
          return 3;
        case Items.GRENADE_HOLD:
          break;
        case Items.AK47:
          return 5;
        case Items.FLAMETHROWER:
          return 5;
        case Items.M16:
          return 5;
        case Items.RIFLE:
          return 3;
        case Items.RIFLE_LEVER:
          return 4;
        case Items.DMR:
          return 5;
        case Items.SNIPER:
          return 4;
        case Items.CROSSBOW:
          return 4;
        case Items.GRENADE_LAUNCHER:
          return 4;
        case Items.ROCKET_LAUNCHER:
          break;
        case Items.SWORD:
          return 4;
        case Items.STICKY_GUN:
          return 3;
      }
      return -1;
    }
    public static int GetUtilityValue(UtilityScript.UtilityType utility)
    {
      switch (utility)
      {
        case UtilityScript.UtilityType.NONE:
          return 0;
        case UtilityScript.UtilityType.GRENADE:
          return 2;
        case UtilityScript.UtilityType.GRENADE_IMPACT:
          return 2;
        case UtilityScript.UtilityType.C4:
          return 2;
        case UtilityScript.UtilityType.SHURIKEN:
          return 1;
        case UtilityScript.UtilityType.SHURIKEN_BIG:
          return 1;
        case UtilityScript.UtilityType.KUNAI_EXPLOSIVE:
        case UtilityScript.UtilityType.KUNAI_STICKY:
          return 2;
        case UtilityScript.UtilityType.STOP_WATCH:
          return 2;
        case UtilityScript.UtilityType.INVISIBILITY:
          return 3;
        case UtilityScript.UtilityType.DASH:
          return 1;
      }
      return 100;
    }
    public static int GetPerkValue(Shop.Perk.PerkType perk)
    {
      switch (perk)
      {
        case Shop.Perk.PerkType.NONE:
          return 0;
        case Shop.Perk.PerkType.PENETRATION_UP:
          return 4;
        case Shop.Perk.PerkType.ARMOR_UP:
          return 4;
        case Shop.Perk.PerkType.SPEED_UP:
          break;
        case Shop.Perk.PerkType.EXPLOSION_RESISTANCE:
          return 3;
        case Shop.Perk.PerkType.EXPLOSIONS_UP:
          return 3;
        case Shop.Perk.PerkType.FASTER_RELOAD:
          return 4;
        case Shop.Perk.PerkType.MAX_AMMO_UP:
          return 3;
        case Shop.Perk.PerkType.FIRE_RATE_UP:
          break;
        case Shop.Perk.PerkType.LASER_SIGHTS:
          return 1;
        case Shop.Perk.PerkType.AKIMBO:
          break;
        case Shop.Perk.PerkType.NO_SLOWMO:
          return 0;
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

  // Remove a target from target list, returns true if all targets dead
  public static bool KillTarget(EnemyScript target)
  {
    EnemyScript._Targets.Remove(target);
    if (EnemyScript._Targets.Count == 0)
    {
      ToggleExit();
      return true;
    }
    return false;
  }

  public static float _LastPause;
  public static void TogglePause()
  {
    TogglePause(Menu2.MenuType.PAUSE);
  }
  public static void TogglePause(Menu2.MenuType afterUnlockMenu, bool controllersChanged = false)
  {
    if (Time.unscaledTime < 5f) return;

    if (!Menu2.CanPause()) return;

    if (afterUnlockMenu != Menu2.MenuType.NONE)
    {
      if (TileManager._LoadingMap) return;
      if (Time.unscaledTime - _LastPause < 0.1f) return;
    }
    _LastPause = Time.unscaledTime;
    _Paused = !_Paused;
    if (_Paused)
    {
      Time.timeScale = 0f;
      Menu2.OnPause(afterUnlockMenu);
      System.GC.Collect();
      TileManager._Text_LevelNum.gameObject.SetActive(false);
      TileManager._Text_LevelTimer.gameObject.SetActive(false);
      TileManager._Text_LevelTimer_Best.gameObject.SetActive(false);
    }
    else
    {
      Time.timeScale = 1f;
      // Check player amount change
      if (_GameMode != GameModes.SURVIVAL)
        if (PlayerScript._Players != null && Settings._NumberPlayers != PlayerScript._Players.Count) TileManager.ReloadMap();
      TileManager._Text_LevelNum.gameObject.SetActive(true);
      TileManager._Text_LevelTimer.gameObject.SetActive(true);
      TileManager._Text_LevelTimer_Best.gameObject.SetActive(true);
    }
    // Toggle text bubbles
    TextBubbleScript.ToggleBubbles(!_Paused);
  }

  // Determine unlocks for classic mode via PlayerPrefs
  public static void UpdateLevelVault()
  {

    // Check difficulty 0
    for (var i = 0; i < 12; i++)
      if (PlayerPrefs.GetInt($"levels0_{11 + i * 12}") == 1)
        Shop.AddAvailableUnlockVault($"classic_{i}");

    // Check difficulty 1
    for (var i = 0; i < 12; i++)
      if (PlayerPrefs.GetInt($"levels1_{11 + i * 12}") == 1)
        Shop.AddAvailableUnlockVault($"classic_{12 + i}");
  }

  static void OnLevelComplete()
  {
    // Check survival
    if (IsSurvival()) return;

    //Debug.Log($"Completed level with time: {TileManager._LevelTimer}");

    // Check level pack
    if (Levels._LevelPack_Playing)
    {

      // Check last level
      if (Levels._CurrentLevelIndex + 1 == Levels._CurrentLevelCollection._leveldata.Length)
      {
        Levels._LevelPack_Playing = false;

        TogglePause(Menu2.MenuType.EDITOR_PACKS);
        Menu2.SwitchMenu(Menu2.MenuType.EDITOR_PACKS);
        Menu2._CurrentMenu._selectionIndex = Levels._LevelPacks_Play_SaveIndex;
        Menu2._CanRender = false;
        Menu2.RenderMenu();
        _LastPause = Time.unscaledTime - 0.2f;
        Menu2.SendInput(Menu2.Input.SPACE);
        Menu2.SendInput(Menu2.Input.SPACE);
        Menu2.SendInput(Menu2.Input.SPACE);
        _LastPause = Time.unscaledTime;
        return;
      }

      // Load next level
      NextLevel(Levels._CurrentLevelIndex + 1);
      return;
    }

    // Check level editor levels
    if (_EditorTesting)
    {
      ReloadMap();
      return;
    }

    // Stat
    Stats.OverallStats._Levels_Completed++;

    // Save level status
    if (!Levels.LevelCompleted(Levels._CurrentLevelIndex))
    {
      Settings._LevelsCompleted_Current.Add(Levels._CurrentLevelIndex);
      PlayerPrefs.SetInt($"{Levels._CurrentLevelCollection_Name}_{Levels._CurrentLevelIndex}", 1);
      //Debug.Log($"SET: {Levels._CurrentLevelCollection_Name}_{Levels._CurrentLevelIndex}... 1");
      // Check mode-specific unlocks
      if (_GameMode == GameModes.CLASSIC)
      {
        // Award shop point
        Shop._AvailablePoints++;

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

    // Check for extras challenges
    if (_GameMode == GameModes.CLASSIC)
    {

      //
      bool EquipmentIsEqual(PlayerProfile.Equipment e0, PlayerProfile.Equipment e1)
      {

        // Check items equal
        var equipment0_items = new List<ItemManager.Items>(){
          e0._item_left0,
          e0._item_right0,
          e0._item_left1,
          e0._item_right1
        };
        var equipment1_items = new List<ItemManager.Items>(){
          e1._item_left0,
          e1._item_right0,
          e1._item_left1,
          e1._item_right1
        };
        foreach (var item in equipment0_items)
        {
          if (!equipment1_items.Contains(item)) { return false; }
          equipment1_items.Remove(item);
        }
        if (equipment0_items.Count != equipment1_items.Count) { return false; }

        // Check perks equal
        if (e0._perks.Count != e1._perks.Count) return false;
        foreach (var perk0 in e0._perks)
        {
          if (!e1._perks.Contains(perk0)) return false;
        }

        // Check utilities equal
        if (e0._utilities_left.Length != e1._utilities_left.Length) return false;
        foreach (var utlity0 in e0._utilities_left)
        {
          if (!e1._utilities_left.Contains(utlity0)) return false;
        }
        if (e0._utilities_right.Length != e1._utilities_right.Length) return false;
        foreach (var utlity0 in e0._utilities_right)
        {
          if (!e1._utilities_right.Contains(utlity0)) return false;
        }

        return true;
      }

      // Make sure # of players has not changed
      if (PlayerScript._NumPlayers_Start == 1 && PlayerScript._Players.Count > 0)
      {

        // Make sure equipment has not changed
        var equipment_start = PlayerScript._Players[0]._equipment_start;
        var equipment_changed = PlayerScript._Players[0]._equipment_changed;
        if (EquipmentIsEqual(equipment_start, PlayerScript._Players[0]._equipment) && !equipment_changed)
        {

          // Function to check for equipment matching
          bool HasItemPair(PlayerProfile.Equipment equipment, params ItemManager.Items[] items)
          {
            // Check perks, utils
            if (equipment._perks.Count > 0)
            {
              return false;
            }
            if (equipment._utilities_left.Length > 0)
            {
              return false;
            }
            if (equipment._utilities_right.Length > 0)
            {
              return false;
            }

            // Check item pair
            var equipment_items = new List<ItemManager.Items>(){
              equipment._item_left0,
              equipment._item_right0,
              equipment._item_left1,
              equipment._item_right1
            };
            foreach (var item in items)
            {
              if (item == ItemManager.Items.NONE) { continue; }
              if (!equipment_items.Contains(item)) { return false; }
              equipment_items.Remove(item);
            }

            // Matches
            return true;
          }

          switch (Levels._CurrentLevelIndex + 1)
          {

            // Check time extra
            // "unlock by completing sneaky level 40, solo, with just a knife",
            case 40:

              if (HasItemPair(equipment_start, ItemManager.Items.KNIFE))
              {
                Debug.Log("Unlocked EXTRA_TIME");
                Shop.AddAvailableUnlock(Shop.Unlocks.EXTRA_TIME, true);
              }

              break;

            // Check gravity extra
            // "unlock by completing sneaky level 80, solo, with just a knife and silenced pistol",
            case 80:

              if (HasItemPair(equipment_start, ItemManager.Items.KNIFE, ItemManager.Items.PISTOL_SILENCED))
              {
                Debug.Log("Unlocked EXTRA_GRAVITY");
                Shop.AddAvailableUnlock(Shop.Unlocks.EXTRA_GRAVITY, true);
              }

              break;

          }

        }
      }
    }

    // Check demo
    if (_Singleton._IsDemo && Levels._CurrentLevelIndex + 1 >= 48)
    {
      Menu2._SaveMenuDir = -1;
      Shop._UnlockString += $"- you've reached the end of the <color=cyan>demo</color> for CLASSIC mode\n- purchase the game to access the rest of the game!\n";

#if UNITY_STANDALONE
      // Achievement
      SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DIFFICULTY_1);
      TogglePause(Menu2.MenuType.LEVELS);
#endif

      return;
    }

    // Check last level completed
    else if (Levels._CurrentLevelIndex + 1 == Levels._CurrentLevelCollection._leveldata.Length)
    {
      if (Settings._DIFFICULTY == 0)
      {
        if (Settings._DifficultyUnlocked == 0)
        {
          Settings._DIFFICULTY = 1;
          Levels._CurrentLevelCollectionIndex = 1;
          Menu2._SaveMenuDir = -1;
          Settings._DifficultyUnlocked = 1;
          Shop._UnlockString += $"- new difficulty unlocked: <color=cyan>sneakier</color>\n";

#if UNITY_STANDALONE
          // Achievement
          SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DIFFICULTY_1);
#endif

          TogglePause(Menu2.MenuType.LEVELS);
          return;
        }
        //Menu2.sw
      }
      else if (Settings._DIFFICULTY == 1)
      {
#if UNITY_STANDALONE
        // Achievement
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DIFFICULTY_2);
#endif

        Shop._UnlockString += $"- you have beaten the classic mode!\n- try out the survival mode?\n";

        // Check for shop point bug

      }

      // Display unlock messages
      if (Shop._UnlockString != string.Empty)
      {
        TogglePause(Menu2.MenuType.LEVELS);
      }
      else
      {
        TogglePause();
        Menu2.SwitchMenu(Menu2.MenuType.LEVELS);
      }
      return;
    }
    // Increment menu selector
    Menu2._SaveLevelSelected++;
    if (Menu2._SaveLevelSelected == 12)
    {
      Menu2._SaveLevelSelected = 0;
      Menu2._SaveMenuDir++;
    }
    // Load next level
    NextLevel(Levels._CurrentLevelIndex + 1);
  }

  public static void NextLevel(int levelIndex)
  {
    // Unpause if paused
    if (GameScript._Paused) GameScript.TogglePause();

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
    _Coroutine_load = _Singleton.StartCoroutine(NextLevelCo(levelData));
  }
  static IEnumerator NextLevelCo(string levelData)
  {
    _Singleton._GameEnded = true;
    _inLevelEnd = false;
    // Fix players
    if (PlayerScript._Players != null)
    {
      // Get player with goal
      PlayerScript hasGoal = null;
      foreach (var p in PlayerScript._Players)
      {
        if (p == null || !p._hasExit) continue;
        hasGoal = p;
        // Move player to playerspawn to correct errors
        p.transform.position = PlayerspawnScript._PlayerSpawns[0].transform.position;
        break;
      }
      if (hasGoal != null)
      {
        hasGoal._hasExit = false;
        // Teleport other players to them
        foreach (var p in PlayerScript._Players)
        {
          if (p == null || p._ragdoll._dead || p._id == hasGoal._id) continue;
          p.transform.position = hasGoal.transform.position;
        }
      }

    }
    // Hide exit
    ToggleExit(false);
    ActiveRagdoll.SoftReset();
    yield return new WaitForSeconds(0.05f);
    TileManager.LoadMap(levelData);
    System.GC.Collect();
    yield return new WaitForSeconds(0.05f);
    _Singleton._GameEnded = false;

    // Check for editor enable
    while (TileManager._LoadingMap)
      yield return new WaitForSeconds(0.1f);
    if (TileManager._EnableEditorAfterLoad)
    {
      TileManager._EnableEditorAfterLoad = false;
      _EditorEnabled = true;
      yield return TileManager.EditorEnabled();
    }
  }

  public static bool IsSurvival()
  {
    return _GameMode == GameModes.SURVIVAL;
  }

  static float _lasttick;
  public static bool CanPlayTick()
  {
    if (Time.unscaledTime - _lasttick < 0.05f) return false;
    _lasttick = Time.unscaledTime;
    FunctionsC.PlaySound(ref GameScript._audioListenerSource, "Ragdoll/Footstep");
    return true;
  }

  // When called, the player can exit / complete the level
  public float _goalPickupTime;
  public static void ToggleExit(bool toggle = true)
  {
    if (_Singleton._ExitOpen == toggle) return;
    _Singleton._ExitOpen = toggle;
    if (toggle)
    {
      _Singleton._goalPickupTime = Time.time;
    }
  }

  public static void FadeIn()
  {
    _Singleton.StartCoroutine(FadeScreen(true));
  }
  public static void FadeOut()
  {
    _Singleton.StartCoroutine(FadeScreen(false));
  }
  static IEnumerator FadeScreen(bool toggle)
  {
    float t = 0f;
    float toggleT = toggle ? 1f - t : t;
    _BlackFade.sharedMaterial.color = new Color(_BlackFade.sharedMaterial.color.r, _BlackFade.sharedMaterial.color.g, _BlackFade.sharedMaterial.color.b, toggleT);
    while (t < 1f)
    {
      t += 0.03f;
      toggleT = toggle ? 1f - t : t;
      _BlackFade.sharedMaterial.color = new Color(_BlackFade.sharedMaterial.color.r, _BlackFade.sharedMaterial.color.g, _BlackFade.sharedMaterial.color.b, toggleT);
      yield return new WaitForSeconds(0.03f);
    }
  }

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

    public static FunctionsC.SaveableStat_Bool _CameraType;

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
        return PlayerPrefs.GetInt($"{_GameMode}_DifficultyLevel", 0);
      }
      set
      {
        PlayerPrefs.SetInt($"{_GameMode}_DifficultyLevel", value);
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
        PlayerPrefs.SetInt($"{_GameMode}_SavedDifficulty", DIFFICULTY);
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
        FunctionsC.MusicManager._TrackSource.volume = VolumeMusic / 5f * 0.8f;
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
    public static FunctionsC.SaveableStat_Int _CameraZoom;

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
      _Extra_BodyExplode;
    public static bool _Extras_CanUse { get { return _GameMode == GameModes.CLASSIC && !_LevelEditorEnabled; } }
    public static bool _Extras_UsingAny
    {
      get
      {
        return
      _Extra_Superhot ||
      _Extra_CrazyZombies ||
      _Extra_RemoveBatGuy._value != 0 ||
      _Extra_EnemyMultiplier._value != 0 ||
      _Extra_PlayerAmmo._value != 0 ||
      _Extra_EnemyAmmo._value != 0 ||
      _Extra_BodyExplode._value != 0
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
        for (int i = 0; i < Levels._LevelCollections[u]._leveldata.Length; i++)
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

      //Camera
      _CameraZoom = new FunctionsC.SaveableStat_Int("CameraZoom", 1);
      _CameraType = new FunctionsC.SaveableStat_Bool("CameraType_ortho", false);
      SetPostProcessing();

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
          switch (GameScript.Settings._Extra_Gravity)
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
  }
}