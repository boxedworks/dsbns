using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.CustomEntities;
using Assets.Scripts.Ragdoll;
using Assets.Scripts.Settings;
using Assets.Scripts.Settings.Serialization;
using Assets.Scripts.UI.Menus;
using Assets.Scripts.Game.Items;
using UnityEngine;

//
public static class SurvivalManager
{
  //
  static SettingsSaveData SettingsModule { get { return SettingsHelper.s_SaveData.Settings; } }
  static LevelSaveData LevelModule { get { return SettingsHelper.s_SaveData.LevelData; } }

  //
  public static bool _WavePlaying;
  public static int _Wave;

  public static float _Time_wave_intermission,
    _Timer_wave_start;

  public static int _Number_enemies_spawned;

  public static Vector3 _AllSpawn;

  public static Loadout[] s_PlayerLoadouts;
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
    s_PlayerLoadouts = new Loadout[4];
    for (var i = 0; i < s_PlayerLoadouts.Length; i++)
    {
      s_PlayerLoadouts[i] = new Loadout
      {
        _Equipment = new PlayerProfile.Equipment(),
        _two_weapon_pairs = true
      };
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
    s_PlayerLoadouts = new Loadout[4];
    for (var i = 0; i < s_PlayerLoadouts.Length; i++)
    {
      s_PlayerLoadouts[i] = new Loadout
      {
        _Equipment = new PlayerProfile.Equipment(),
        _two_weapon_pairs = true
      };
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
          Object.Destroy(g);
      }
      _Money = null;
    }
  }

  public static void OnWaveStart()
  {
    // Respawn players and heal
    GameScript.s_PlayerIter = 0;
    if (PlayerScript.s_Players != null)
    {
      PlayerScript alive_player = null;
      for (var i = 0; i < SettingsHelper._NumberPlayers; i++)
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
      foreach (var p in PlayerScript.s_Players) if (p != null && !p._Ragdoll._IsDead) GameScript.s_PlayerIter++;

      // Remove null / dead players
      for (var i = PlayerScript.s_Players.Count - 1; i >= 0; i--)
      {
        var p = PlayerScript.s_Players[i];
        if (p == null || p._Ragdoll == null || p._Ragdoll._IsDead)
        {
          ActiveRagdoll.s_Ragdolls.Remove(p._Ragdoll);
          Object.Destroy(p.transform.parent.gameObject);
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
      case 1:
        SpawnLogic._SpawnTime = 1.2f;
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 7);
        break;
      case 2:
        SpawnLogic._SpawnTime = 1.1f;
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 12);
        break;
      case 3:
        SpawnLogic._SpawnTime = 1f;
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 6);
        QueueEnemyType(EnemyType.KNIFE_WALK, 4);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 6);
        break;
      case 4:
        SpawnLogic._SpawnTime = 0.9f;
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 5, 5);
        QueueEnemyType(EnemyType.KNIFE_WALK, 5, 3);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 5);
        QueueEnemyType(EnemyType.KNIFE_WALK, 3);
        break;
      //case (5):
      //  RandomSpecialWave();
      //  break;
      case 5:
        SpawnLogic._SpawnTime = 0.8f;
        EnemyScript._MAX_RAGDOLLS_ALIVE = 35;
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 3);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 5, 10);
        QueueEnemyType(EnemyType.KNIFE_WALK, 5);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 8);
        QueueEnemyType(EnemyType.KNIFE_WALK, 3);
        break;
      case 6:
        SpawnLogic._SpawnTime = 0.7f;
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 5);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 8, 10);
        QueueEnemyType(EnemyType.KNIFE_WALK, 5);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 6);
        QueueEnemyType(EnemyType.KNIFE_WALK, 3);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 6);
        QueueEnemyType(EnemyType.KNIFE_WALK, 5);
        break;
      case 7:
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 5);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 6, 10);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 10, 10);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 10);
        QueueEnemyType(EnemyType.KNIFE_WALK, 5);
        break;
      case 8:
        SpawnLogic._SpawnTime = 0.65f;
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 8, 10);
        QueueEnemyType(EnemyType.KNIFE_WALK, 5);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
        break;
      //case (10):
      //  RandomSpecialWave();
      //  break;
      case 9:
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 15, 20);
        QueueEnemyType(EnemyType.KNIFE_WALK, 7);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
        break;
      case 10:
        SpawnLogic._SpawnTime = 0.6f;
        EnemyScript._MAX_RAGDOLLS_ALIVE = 50;
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 15, 20);
        QueueEnemyType(EnemyType.KNIFE_WALK, 8);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 30);
        QueueEnemyType(EnemyType.KNIFE_WALK, 8);
        break;
      case 11:
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20, 15);
        QueueEnemyTypeGroup(EnemyType.KNIFE_WALK_SLOW, 10, 15);
        QueueEnemyType(EnemyType.KNIFE_WALK, 15, 10);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20, 15);
        QueueEnemyType(EnemyType.KNIFE_WALK, 10);
        break;
      case 12:
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
      case 13:
        QueueEnemyType(EnemyType.KNIFE_WALK, 10, 5);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20);
        QueueEnemyType(EnemyType.KNIFE_WALK, 15);
        QueueEnemyType(EnemyType.KNIFE_WALK_SLOW, 20, 20);
        QueueEnemyType(EnemyType.KNIFE_WALK, 10, 5);
        QueueEnemyTypeGroup(EnemyType.KNIFE_WALK_SLOW, 20, 15);
        QueueEnemyType(EnemyType.KNIFE_WALK, 15);
        break;
      case 14:
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
      case 15:
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
      case 16:
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
      case 17:
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
      case 18:
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
      case 19:
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
      case 20:
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
        max_zombies = 200 * (int)mod;
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
      LevelSaveData.Save();
    }

    // Check survival achievements
    // Map 1
    if (Levels._CurrentLevelIndex == 0)
    {
      if (_Wave == 10)
      {

#if UNITY_STANDALONE
        Achievements.UnlockAchievement(Achievements.Achievement.SURVIVAL_MAP0_10);
#endif

        if (highestWave < 10)
        {
          // Unlock next map
          GameScript.TogglePause();
          Menu.PlayNoise(Menu.Noise.PURCHASE);
          Menu.GenericMenu(new string[] {
$@"<color={Menu._COLOR_GRAY}>new survival map unlocked</color>

you survived 10 waves and have unlocked a <color=yellow>new survival map</color>!
"
        }, "nice", Menu.MenuType.NONE, null, true, null,
          c =>
          {
            GameScript.TogglePause();
            Menu.HideMenus();
          });
        }
      }

      else if (_Wave == 20)
      {
#if UNITY_STANDALONE
        Achievements.UnlockAchievement(Achievements.Achievement.SURVIVAL_MAP0_20);
#endif
      }
    }

    // Map 2
    else if (Levels._CurrentLevelIndex == 1)
    {
      if (_Wave == 10)
      {
#if UNITY_STANDALONE
        Achievements.UnlockAchievement(Achievements.Achievement.SURVIVAL_MAP1_10);
#endif
      }
      else if (_Wave == 20)
      {
#if UNITY_STANDALONE
        Achievements.UnlockAchievement(Achievements.Achievement.SURVIVAL_MAP1_20);
#endif
      }
    }


    // Set random
    CustomObstacle.Randomize();

    // Record stat
    //Stats.OverallStats._Waves_Played++;

    // Respawn players
    GameScript.s_PlayerIter = 0;
    if (PlayerScript.s_Players != null)
    {
      PlayerScript alive_player = null;
      for (var i = 0; i < SettingsHelper._NumberPlayers; i++)
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
      foreach (var p in PlayerScript.s_Players) if (p != null && !p._Ragdoll._IsDead) GameScript.s_PlayerIter++;

      // Remove null / dead players
      for (var i = PlayerScript.s_Players.Count - 1; i >= 0; i--)
      {
        var p = PlayerScript.s_Players[i];
        if (p == null || p._Ragdoll == null || p._Ragdoll._IsDead)
        {
          ActiveRagdoll.s_Ragdolls.Remove(p._Ragdoll);
          Object.Destroy(p.transform.parent.gameObject);
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
        Object.Destroy(obj.gameObject);
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
    if (Perk.HasPerk(p._Id, Perk.PerkType.ARMOR_UP))
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
          Object.Destroy(pair.Item1);
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

      enemy_ragdoll.ToggleRaycasting(false, true);

      if (Random.Range(0, 16) == 0f)
        enemy_next.SetRandomStrafe();

      // Check if others in front
      var hit = new RaycastHit();
      if (Physics.SphereCast(new Ray(enemy_ragdoll._Hip.transform.position, MathC.Get2DVector(enemy_ragdoll._Hip.transform.forward) * 100f), 0.5f, out hit, 100f, GameResources._Layermask_Ragdoll))
      {
        enemy_ragdoll.ToggleRaycasting(true, true);
        if (hit.distance > 10f) return;
        var ragdoll = ActiveRagdoll.GetRagdoll(hit.collider.gameObject);
        if (ragdoll == null || ragdoll._IsPlayer) return;

        enemy_ragdoll._ForceGlobal += enemy_ragdoll._Hip.transform.right * (enemy_next._strafeRight ? 1f : -1f) * Time.deltaTime * 3f;
        return;
      }

      enemy_ragdoll.ToggleRaycasting(true, true);
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
          rb.AddForce(new Vector3(0f, 1f, 0f) * (2f + Random.value * 1f), ForceMode.Impulse);
          rb.AddTorque(new Vector3(1f - (Random.value * 2f), 1f - (Random.value * 2f), 1f - (Random.value * 2f)) * (3f + Random.value * 1f), ForceMode.Impulse);
          Object.Destroy(rb.gameObject, 10f);
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
    if (!GameScript.s_Paused && !Menu.s_InMenus)
      for (var i = 0; i < SettingsHelper._NumberPlayers; i++)
        text += $"<color={PlayerProfile.s_Profiles[i].GetColorName(false)}>p{i + 1}:</color> {_PlayerScores[i]}\n";
    _Text_Scores.text = text;

    // Check if paused or in menu
    if (GameScript.s_Paused || Menu.s_InMenus || CustomObstacle._CustomSpawners == null) return;

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
            case EnemyType.KNIFE_WALK_SLOW:
              movespeed = 0.25f;
              break;
            case EnemyType.KNIFE_WALK:
            case EnemyType.PISTOL_WALK:
              movespeed = 0.5f;
              break;
            case EnemyType.SET_ALLSPAWN:
              SetAllSpawn();
              break;
            case EnemyType.REMOVE_ALLSPAWN:
              RemoveAllSpawn();
              break;
            case EnemyType.KNIFE_JOG:
            case EnemyType.GRENADE_JOG:
              movespeed = 0.7f;
              break;
            case EnemyType.KNIFE_RUN:
              movespeed = 1f;
              break;
            case EnemyType.KNIFE_BEEFY_SLOW:
              movespeed = 0.15f;
              health = 4;
              e?._Ragdoll.ChangeColor((Color.red + Color.yellow) / 3f);//new Color(0, 30, 0));
                                                                       //e.GetRagdoll()._hip.transform.localScale *= 1.6f;
              break;
            case EnemyType.ARMORED:
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

    if (SettingsHelper._NumberPlayers > 1)
      number = Mathf.Clamp(Mathf.RoundToInt(number * (1f + SettingsHelper._NumberPlayers * 0.4f)), 0, EnemyScript._MAX_RAGDOLLS_ALIVE - 1);
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