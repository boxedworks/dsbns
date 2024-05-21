using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

// Holds functions for versus mode
public static class VersusMode
{

  // Versus settings
  public class VersusSettings
  {
    public int _ScoreToWin;
    public bool _UseSlowmo;
    public bool _FreeForAll;

    public int _PlayerHealth;

    public ModeType _Mode;

    public enum ModeType
    {
      LAST_MAN_STANDING,
      KILLS_FOR_POINTS,
    }

    public VersusSettings()
    {
      _ScoreToWin = 5;
      _UseSlowmo = true;
      _FreeForAll = true;
      _PlayerHealth = 1;

      _Mode = ModeType.LAST_MAN_STANDING;
    }
  }
  public static VersusSettings s_Settings;

  //
  static bool s_gamePlaying;
  public static bool s_PlayersCanMove;
  static TMPro.TextMeshPro s_announcementText;
  public static GameScript.ItemManager.Loadout s_PlayerLoadouts;
  static int[] s_playerTeams;

  static int[] s_playerScores, s_playerScoresRound;
  public static int GetPlayerScore(int playerId)
  {
    return s_playerScores[playerId];
  }

  public static void Init()
  {
    s_Settings = new();

    s_announcementText = GameObject.Find("VersusUI").transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();

    s_playerTeams = new int[4];

    Reset();
  }

  //
  public static void Reset()
  {
    s_playerScores = new int[4];
    s_PlayerLoadouts = new GameScript.ItemManager.Loadout
    {
      _equipment = new GameScript.PlayerProfile.Equipment()
    };

    foreach (var playerProfile in GameScript.PlayerProfile.s_Profiles)
    {
      playerProfile.UpdateIcons();
      playerProfile.UpdateVersusScore();
    }
  }

  //
  public static void OnLevelLoad()
  {
    s_PlayersCanMove = false;
    s_gamePlaying = true;
    ToggleTeammodeSwitchUi(false);

    s_playerScoresRound = new int[4];

    SetRandomLoadout();

    IEnumerator LevelStartCountdownCo()
    {

      var timeStart = Time.time;
      var levelId = TileManager._s_MapIndex;

      bool IsMapSame()
      {
        return levelId == TileManager._s_MapIndex;
      }

      SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);

      var preTimeAmount = 1.5f;
      while (Time.time - timeStart < preTimeAmount)
      {
        yield return new WaitForSeconds(0.01f);
        if (IsMapSame())
          s_announcementText.text = $"{string.Format("{0:0.00}", Mathf.Clamp(-(Time.time - timeStart - preTimeAmount), 0f, 10f))}";
      }

      if (IsMapSame())
      {
        s_PlayersCanMove = true;
        yield return new WaitForSeconds(0f);
        s_announcementText.text = $"go!";

        SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_start", 0.9f, 1.1f);

        yield return new WaitForSeconds(0.3f);
        s_announcementText.text = "";
      }
    }
    GameScript._s_Singleton.StartCoroutine(LevelStartCountdownCo());
  }

  //
  public static void OnGamemodeSwitched(bool switchedTo)
  {
    s_announcementText.enabled = switchedTo;

    if (switchedTo)
    {
      if (s_Settings._FreeForAll)
        ToggleTeammodeSwitchUi(false);
      else
        ToggleTeammodeSwitchUi(true);
    }
    else
    {
      ToggleTeammodeSwitchUi(true);
    }
  }

  //
  public static int GetNumberTeams()
  {
    if (s_Settings._FreeForAll && Settings._NumberPlayers > 1) return -1;

    var teamsCounted = new List<int>();
    for (var i = 0; i < Settings._NumberPlayers; i++)
    {
      var teamId = s_playerTeams[i];

      if (teamsCounted.Contains(teamId)) continue;
      teamsCounted.Add(teamId);
    }

    return teamsCounted.Count;
  }
  public static bool HasMultipleTeams()
  {
    return (s_Settings._FreeForAll && Settings._NumberPlayers > 1) || GetNumberTeams() > 1;
  }

  //
  static int s_deathCheckIndex;
  public static void OnPlayerDeath(PlayerScript playerDied, PlayerScript playerSource)
  {
    if (!s_gamePlaying)
      return;

    // Check all / last dead
    IEnumerator CheckPlayerDeathStatusCo()
    {

      var mapIndex = TileManager._s_MapIndex;
      var checkIndex = ++s_deathCheckIndex;
      bool IsMapSame()
      {
        return mapIndex == TileManager._s_MapIndex;
      }
      bool IsCheckSame()
      {
        return checkIndex == s_deathCheckIndex;
      }

      yield return new WaitForSeconds(0.5f);
      if (IsMapSame())
      {

        var waitTime = 2f;
        var gameOver = false;
        var gameWon = false;

        var gameMode = s_Settings._Mode;

        switch (gameMode)
        {

          // Last one / team standing wins
          case VersusSettings.ModeType.LAST_MAN_STANDING:

            // Free for all
            if (s_Settings._FreeForAll)
            {
              var numAlive = PlayerScript.GetNumberAlivePlayers();

              // Draw
              if (numAlive == 0)
              {
                gameOver = true;
                s_gamePlaying = false;

                s_announcementText.text = $"draw!";

                SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
              }

              // Score
              else if (numAlive == 1)
              {
                gameOver = true;
                s_gamePlaying = false;

                PlayerScript lastAlive = null;
                foreach (var player in PlayerScript.s_Players)
                  if (player._ragdoll._health > 0)
                    lastAlive = player;

                // Check game types
                var playerColor = GameScript.PlayerProfile.s_Profiles[lastAlive._Id].GetColorName();
                s_playerScores[lastAlive._Id]++;
                s_playerScoresRound[lastAlive._Id]++;

                OnScore();

                foreach (var playerProfile in GameScript.PlayerProfile.s_Profiles)
                  playerProfile.UpdateVersusUI();

                if (IsCheckSame())
                {
                  s_announcementText.text = $"<color={playerColor}>P{lastAlive._Id + 1}</color> scored!";

                  // Check win
                  if (s_playerScores[lastAlive._Id] >= s_Settings._ScoreToWin)
                  {
                    s_announcementText.text = $"<color={playerColor}>P{lastAlive._Id + 1}</color> <b>wins</b>!";
                    gameWon = true;

                    waitTime = 4.5f;

                    SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_win", 0.9f, 1.1f);
                  }
                  else
                    SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
                }
              }
            }

            // Team fight
            else
            {

              var teamsAlive = new List<int>();
              foreach (var player in PlayerScript.s_Players)
              {
                if (player._ragdoll._health > 0)
                {
                  var teamId = s_playerTeams[player._Id];
                  if (!teamsAlive.Contains(teamId))
                    teamsAlive.Add(teamId);
                }
              }

              // Draw
              if (teamsAlive.Count == 0)
              {
                gameOver = true;
                s_gamePlaying = false;

                s_announcementText.text = $"Draw!";

                SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
              }

              // Score
              else if (teamsAlive.Count == 1)
              {
                gameOver = true;
                s_gamePlaying = false;

                var teamScore = 0;
                var teamColor = GetTeamColorName(teamsAlive[0]);
                for (var i = 0; i < s_playerScores.Length; i++)
                {
                  var teamId = s_playerTeams[i];
                  if (teamId == teamsAlive[0])
                  {
                    teamScore = s_playerScores[i];

                    s_playerScores[i]++;
                    s_playerScoresRound[i]++;

                    OnScore();
                  }
                }
                foreach (var playerProfile in GameScript.PlayerProfile.s_Profiles)
                  playerProfile.UpdateVersusUI();

                if (IsCheckSame())
                {
                  s_announcementText.text = $"<color={teamColor}>{teamColor}</color> team scored!";

                  // Check win
                  if (teamScore >= s_Settings._ScoreToWin)
                  {
                    s_announcementText.text = $"<color={teamColor}>{teamColor}</color> team <b>wins</b>!";
                    gameWon = true;

                    waitTime = 4.5f;

                    SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_win", 0.9f, 1.1f);
                  }
                  else
                    SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
                }
              }
            }
            break;

          case VersusSettings.ModeType.KILLS_FOR_POINTS:

            // FFA
            if (s_Settings._FreeForAll)
            {

              var numAlive = PlayerScript.GetNumberAlivePlayers();

              var playerId = playerDied._Id;
              var killedId = playerSource._Id;

              // Check kill
              if (playerId != killedId)
              {
                s_playerScores[killedId]++;
                s_playerScoresRound[killedId]++;

                OnScore();
              }

              // Check suicide
              else
              {
                s_playerScores[killedId]--;
                s_playerScoresRound[killedId]--;

                OnScore();
              }
              foreach (var playerProfile in GameScript.PlayerProfile.s_Profiles)
                playerProfile.UpdateVersusUI();

              if (IsCheckSame())
              {

                // Check round over
                if (numAlive < 2)
                {
                  gameOver = true;
                  s_gamePlaying = false;

                  s_announcementText.text = "Round over!";
                  SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
                  yield return new WaitForSeconds(1.5f);

                  s_announcementText.text = "Results:\n";
                  SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
                  yield return new WaitForSeconds(0.75f);

                  var highestScore = -1;
                  for (var i = 0; i < Settings._NumberPlayers; i++)
                  {
                    var playerScore = s_playerScoresRound[i];
                    var playerScoreTotal = s_playerScores[i];
                    var playerColor = GameScript.PlayerProfile.s_Profiles[i].GetColorName();
                    if (playerColor == "cyan") playerColor = "#00FFFF";
                    var scorePreText = playerScore < 0 ? '-' : '+';
                    var scoreColor = playerScore < 0 ? "red" : (playerScore == 0 ? "white" : "green");
                    s_announcementText.text += $"\n<color={playerColor}>P{i + 1}</color>: {playerScoreTotal} (<color={scoreColor}>{scorePreText}{playerScore}</color>)";
                    SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
                    yield return new WaitForSeconds(0.25f);

                    if (playerScoreTotal >= s_Settings._ScoreToWin && playerScoreTotal > highestScore)
                    {
                      highestScore = playerScoreTotal;
                    }
                  }

                  // Check highest score
                  var highestPlayers = new List<int>();
                  for (var i = 0; i < Settings._NumberPlayers; i++)
                  {
                    var playerScore = s_playerScores[i];
                    if (playerScore == highestScore)
                      highestPlayers.Add(i);
                  }

                  // Check win
                  if (highestPlayers.Count == 1)
                  {
                    yield return new WaitForSeconds(1.25f);

                    var playerWinner = highestPlayers[0];
                    var playerColor = GameScript.PlayerProfile.s_Profiles[playerWinner].GetColorName();

                    s_announcementText.text = $"<color={playerColor}>P{playerWinner + 1}</color> <b>wins</b>!";
                    gameWon = true;

                    waitTime = 4.5f;

                    SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_win", 0.9f, 1.1f);
                  }
                  else
                  {

                    // Draw
                    if (highestPlayers.Count > 1)
                    {
                      yield return new WaitForSeconds(1.25f);
                      s_announcementText.text = $"Draw!";

                      SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
                    }

                    //
                    else
                    {
                      waitTime -= 0.25f;
                    }
                  }
                }
              }
            }

            // Teams
            else
            {

              var teamsAlive = new List<int>();
              var teamsAll = new List<int>();
              foreach (var player in PlayerScript.s_Players)
              {
                var teamId = s_playerTeams[player._Id];
                if (!teamsAll.Contains(teamId))
                  teamsAll.Add(teamId);
                if (player._ragdoll._health > 0)
                {
                  if (!teamsAlive.Contains(teamId))
                    teamsAlive.Add(teamId);
                }
              }

              var numAlive = teamsAlive.Count;

              var playerTeamId = GetTeamId(playerDied._Id);
              var killedTeamId = GetTeamId(playerSource._Id);

              // Check kill
              if (playerTeamId != killedTeamId)
              {
                for (var i = 0; i < s_playerScores.Length; i++)
                {
                  var teamId = s_playerTeams[i];
                  if (teamId == teamsAlive[0])
                  {
                    s_playerScores[i]++;
                    s_playerScoresRound[i]++;

                    OnScore();
                  }
                }
              }

              // Check suicide / team-kill
              else
              {
                for (var i = 0; i < s_playerScores.Length; i++)
                {
                  var teamId = s_playerTeams[i];
                  if (teamId == teamsAlive[0])
                  {
                    s_playerScores[i]--;
                    s_playerScoresRound[i]--;

                    OnScore();
                  }
                }
              }
              foreach (var playerProfile in GameScript.PlayerProfile.s_Profiles)
                playerProfile.UpdateVersusUI();

              if (IsCheckSame())
              {

                // Check round over
                if (numAlive < 2)
                {
                  gameOver = true;
                  s_gamePlaying = false;

                  s_announcementText.text = "Round over!";
                  SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
                  yield return new WaitForSeconds(1.5f);

                  s_announcementText.text = "Results:\n";
                  SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
                  yield return new WaitForSeconds(0.75f);

                  var highestScore = -1;
                  var numTeams = GetNumberTeams();
                  for (var i = 0; i < numTeams; i++)
                  {
                    var teamId = teamsAll[i];
                    var teamScore = GetTeamScore(teamId, true);
                    var teamScoreTotal = GetTeamScore(teamId);
                    var teamColor = GetTeamColorName(teamId);
                    if (teamColor == "cyan") teamColor = "#00FFFF";
                    var scorePreText = teamScore < 0 ? "" : "+";
                    var scoreColor = teamScore < 0 ? "red" : (teamScore == 0 ? "white" : "green");
                    s_announcementText.text += $"\n<color={teamColor}>{teamColor} team</color>: {teamScoreTotal} (<color={scoreColor}>{scorePreText}{teamScore}</color>)";
                    SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
                    yield return new WaitForSeconds(0.25f);

                    if (teamScoreTotal >= s_Settings._ScoreToWin && teamScoreTotal > highestScore)
                    {
                      highestScore = teamScoreTotal;
                    }
                  }

                  // Check highest score
                  var highestTeams = new List<int>();
                  for (var i = 0; i < numTeams; i++)
                  {
                    var teamId = teamsAll[i];
                    var teamScore = GetTeamScore(teamId);
                    if (teamScore == highestScore)
                      highestTeams.Add(teamId);
                  }

                  // Check win
                  if (highestTeams.Count == 1)
                  {
                    yield return new WaitForSeconds(1.25f);

                    var teamWinnerId = highestTeams[0];
                    var teamColor = GetTeamColorName(teamWinnerId);

                    s_announcementText.text = $"<color={teamColor}>{teamColor} team</color> <b>wins</b>!";
                    gameWon = true;

                    waitTime = 4.5f;

                    SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_win", 0.9f, 1.1f);
                  }

                  else
                  {

                    // Draw
                    if (highestTeams.Count > 1)
                    {
                      yield return new WaitForSeconds(1.25f);
                      s_announcementText.text = $"Draw!";

                      SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_text", 0.9f, 1.1f);
                    }

                    //
                    else
                    {
                      waitTime -= 0.25f;
                    }
                  }
                }
              }

            }

            break;
        }

        //
        if (IsCheckSame())
          if (gameOver)
          {

            // Load next level
            yield return new WaitForSeconds(waitTime);

            if (IsMapSame())
            {
              if (gameWon)
              {
                GameScript.TogglePause(Menu2.MenuType.VERSUS);
                Menu2.SwitchMenu(Menu2.MenuType.VERSUS);
                GameScript._LastPause = Time.unscaledTime;
              }
              else
              {
                var nextLevelIndex = GetRandomNextLevelIndex();
                GameScript.NextLevel(nextLevelIndex);
              }
            }
          }
      }

    }
    GameScript._s_Singleton.StartCoroutine(CheckPlayerDeathStatusCo());
  }

  //
  static void OnScore()
  {
    SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Vs_score", 0.9f, 1.1f);
  }

  //
  public static void OnTeammmodeChanged()
  {

    // Split players into teams
    if (!s_Settings._FreeForAll)
      switch (Settings._NumberPlayers)
      {
        case 2:
          s_playerTeams = new int[] { 0, 1, 0, 1 };
          break;
        case 3:
          s_playerTeams = new int[] { 0, 1, 1, 0 };
          break;
        default:
          s_playerTeams = new int[] { 0, 0, 1, 1 };
          break;
      }
    else
      s_playerTeams = new int[] { 0, 1, 2, 3 };

    //
    ToggleTeammodeSwitchUi(!s_Settings._FreeForAll);
    UpdateTeammodeUis();
  }
  public static void UpdateTeammodeUis()
  {
    foreach (var playerProf in GameScript.PlayerProfile.s_Profiles)
    {
      var text = playerProf._VersusUI.GetChild(1).GetComponent<TMPro.TextMeshPro>();
      text.color = s_Settings._FreeForAll ? Color.white : GetTeamColorFromPlayerId(playerProf._Id);
    }
  }
  public static Color GetTeamColor(int teamId)
  {
    return teamId switch
    {
      0 => Color.blue,
      1 => Color.red,
      2 => Color.green,
      _ => Color.yellow
    };
  }
  public static string GetTeamColorName(int teamId)
  {
    return teamId switch
    {
      0 => "blue",
      1 => "red",
      2 => "green",
      _ => "yellow"
    };
  }
  public static Color GetTeamColorFromPlayerId(int playerId)
  {
    return GetTeamColor(GetTeamId(playerId));
  }
  public static int GetTeamId(int playerId)
  {
    return s_playerTeams[playerId];
  }
  public static void IncrementPlayerTeam(int playerId, int incrementBy)
  {
    if (!s_Settings._FreeForAll)
    {

      var s = s_playerTeams[playerId] + incrementBy;
      if (s < 0)
        s = 3;
      s_playerTeams[playerId] = s % 4;

      UpdateTeammodeUis();

      Menu2.PlayNoise(Menu2.Noise.TEAM_SWAP);
    }

    Menu2._CanRender = false;
    Menu2.RenderMenu();
  }

  //
  static int GetTeamScore(int teamId, bool roundScoreOnly = false)
  {
    for (var i = 0; i < Settings._NumberPlayers; i++)
    {
      if (s_playerTeams[i] != teamId) continue;
      return roundScoreOnly ? s_playerScoresRound[i] : s_playerScores[i];
    }
    return -1;
  }

  //
  public static string GetModeName(int modeIndex = -1)
  {
    if (modeIndex == -1)
      return GetModeName((int)s_Settings._Mode);
    return (VersusSettings.ModeType)modeIndex switch
    {
      VersusSettings.ModeType.LAST_MAN_STANDING => "endurance",
      VersusSettings.ModeType.KILLS_FOR_POINTS => "kills",
      _ => "?"
    };
  }

  //
  static int s_currentLevelIndex = -1;
  public static int GetRandomNextLevelIndex()
  {
    var levelSize = Levels._CurrentLevelCollection._levelData.Length;
    while (true)
    {
      var levelIndex = Random.Range(0, levelSize);
      if (levelIndex == s_currentLevelIndex)
      {
        levelIndex += 1;
        levelIndex %= levelSize;
      }
      s_currentLevelIndex = levelIndex;

      // Check map size
      var numPlayers = Settings._NumberPlayers;
      var numTeams = GetNumberTeams();

      var numberSpawns = 0;
      var levelData = Levels._CurrentLevelCollection._levelData[s_currentLevelIndex];
      var a = 0;
      var pattern = "playerspawn_";
      while ((a = levelData.IndexOf(pattern, a)) != -1)
      {
        a += pattern.Length;
        numberSpawns++;
      }

      //
      if ((s_Settings._FreeForAll && numberSpawns < numPlayers) || (!s_Settings._FreeForAll && numberSpawns < numTeams))
        continue;

      break;
    }
    return s_currentLevelIndex;
  }

  //
  static GameScript.ItemManager.Items GetRandomMeleeWeapon()
  {
    return Random.Range(0, 4) switch
    {
      1 => GameScript.ItemManager.Items.FRYING_PAN,
      2 => GameScript.ItemManager.Items.RAPIER,
      3 => GameScript.ItemManager.Items.AXE,

      _ => GameScript.ItemManager.Items.KNIFE,
    };
  }
  static GameScript.ItemManager.Items GetRandomGun()
  {
    return Random.Range(0, 20) switch
    {
      1 => GameScript.ItemManager.Items.PISTOL_DOUBLE,
      2 => GameScript.ItemManager.Items.PISTOL_MACHINE,
      3 => GameScript.ItemManager.Items.PISTOL_CHARGE,
      4 => GameScript.ItemManager.Items.REVOLVER,

      5 => GameScript.ItemManager.Items.RIFLE,
      6 => GameScript.ItemManager.Items.RIFLE_LEVER,
      7 => GameScript.ItemManager.Items.RIFLE_CHARGE,

      8 => GameScript.ItemManager.Items.UZI,
      9 => GameScript.ItemManager.Items.AK47,
      10 => GameScript.ItemManager.Items.SNIPER,
      11 => GameScript.ItemManager.Items.FLAMETHROWER,
      12 => GameScript.ItemManager.Items.GRENADE_LAUNCHER,
      13 => GameScript.ItemManager.Items.CROSSBOW,
      14 => GameScript.ItemManager.Items.DMR,
      15 => GameScript.ItemManager.Items.M16,

      16 => GameScript.ItemManager.Items.SHOTGUN_BURST,
      17 => GameScript.ItemManager.Items.SHOTGUN_DOUBLE,
      18 => GameScript.ItemManager.Items.SHOTGUN_PUMP,
      19 => GameScript.ItemManager.Items.STICKY_GUN,

      _ => GameScript.ItemManager.Items.PISTOL_SILENCED,
    };
  }

  static UtilityScript.UtilityType GetRandomUtility()
  {
    return Random.Range(0, 11) switch
    {
      1 => UtilityScript.UtilityType.GRENADE_IMPACT,
      2 => UtilityScript.UtilityType.GRENADE_STUN,

      3 => UtilityScript.UtilityType.KUNAI_STICKY,
      4 => UtilityScript.UtilityType.SHURIKEN,
      5 => UtilityScript.UtilityType.SHURIKEN_BIG,

      6 => UtilityScript.UtilityType.TACTICAL_BULLET,
      7 => UtilityScript.UtilityType.MORTAR_STRIKE,
      8 => UtilityScript.UtilityType.C4,
      9 => UtilityScript.UtilityType.INVISIBILITY,
      10 => UtilityScript.UtilityType.TEMP_SHIELD,

      _ => UtilityScript.UtilityType.GRENADE
    };
  }
  static Shop.Perk.PerkType GetRandomPerk()
  {
    return Random.Range(0, 5) switch
    {
      1 => Shop.Perk.PerkType.MAX_AMMO_UP,
      2 => Shop.Perk.PerkType.EXPLOSION_RESISTANCE,
      3 => Shop.Perk.PerkType.SMART_BULLETS,
      4 => Shop.Perk.PerkType.LASER_SIGHTS,
      5 => Shop.Perk.PerkType.SPEED_UP,

      _ => Shop.Perk.PerkType.FASTER_RELOAD
    };
  }
  static void SetRandomLoadout()
  {

    if (Random.Range(0, 15) == 0)
    {
      s_PlayerLoadouts = new GameScript.ItemManager.Loadout()
      {
        _equipment = new GameScript.PlayerProfile.Equipment()
        {
          _item_left0 = GameScript.ItemManager.Items.KATANA,
        }
      };
    }
    else
      switch (Random.Range(0, 7))
      {

        case 0:
        case 1:
        case 2:
        case 3:
          s_PlayerLoadouts = new GameScript.ItemManager.Loadout()
          {
            _equipment = new GameScript.PlayerProfile.Equipment()
            {
              _item_left0 = GetRandomMeleeWeapon(),
              _item_right0 = Random.Range(0, 4) > 2 ? GetRandomMeleeWeapon() : GetRandomGun(),
            }
          };
          break;
        case 4:
          s_PlayerLoadouts = new GameScript.ItemManager.Loadout()
          {
            _equipment = new GameScript.PlayerProfile.Equipment()
            {
              _item_left0 = GetRandomGun(),
              _item_right0 = GetRandomGun()
            }
          };
          break;
        case 5:
          s_PlayerLoadouts = new GameScript.ItemManager.Loadout()
          {
            _equipment = new GameScript.PlayerProfile.Equipment()
            {
              _item_left0 = GameScript.ItemManager.Items.NONE,
              _item_right0 = GetRandomGun()
            }
          };
          break;
        case 6:
          s_PlayerLoadouts = new GameScript.ItemManager.Loadout()
          {
            _equipment = new GameScript.PlayerProfile.Equipment()
            {
              _item_left0 = GetRandomMeleeWeapon(),
              _item_right0 = GameScript.ItemManager.Items.NONE
            }
          };
          break;
      }

    // Add utilities
    if (Random.Range(0, 5) == 0)
    {
      s_PlayerLoadouts._equipment._utilities_left = new UtilityScript.UtilityType[Random.Range(1, 4)];
      var randomUtil = GetRandomUtility();
      for (var i = 0; i < s_PlayerLoadouts._equipment._utilities_left.Length; i++)
      {
        s_PlayerLoadouts._equipment._utilities_left[i] = randomUtil;
      }

      if (Random.Range(0, 5) == 0)
      {
        s_PlayerLoadouts._equipment._utilities_right = new UtilityScript.UtilityType[Random.Range(1, 4)];
        randomUtil = GetRandomUtility();
        for (var i = 0; i < s_PlayerLoadouts._equipment._utilities_right.Length; i++)
        {
          s_PlayerLoadouts._equipment._utilities_right[i] = randomUtil;
        }
      }
    }

    // Add mods
    if (Random.Range(0, 5) == 0)
    {
      s_PlayerLoadouts._equipment._perks = new();
      for (var i = 0; i < Random.Range(1, 3); i++)
      {

        var randomPerk = GetRandomPerk();
        if (s_PlayerLoadouts._equipment._perks.Contains(randomPerk))
        {
          i--;
          continue;
        }

        s_PlayerLoadouts._equipment._perks.Add(randomPerk);
      }
    }
  }

  //
  public static void ToggleTeammodeSwitchUi(bool toggle)
  {
    foreach (var playerProf in GameScript.PlayerProfile.s_Profiles)
    {
      var controlUi = playerProf._VersusUI.GetChild(1).GetChild(0).gameObject;
      controlUi.SetActive(toggle);
    }
  }
}