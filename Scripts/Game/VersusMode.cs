using UnityEngine;
using SneakyEngine.Equippable.Utility;
using System.Collections;
using System.Collections.Generic;
using SneakyGame.Settings;
using SneakyGame.UI.Menus;
using SneakyGame.Game.Items;
using SneakyGame.Game.Player;

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
  public static Loadout s_PlayerLoadouts;
  static int[] s_playerTeams;

  static int[] s_playerScores, s_playerScoresRound;
  public static int GetPlayerScore(int playerId)
  {
    return s_playerScores[playerId];
  }

  public static void Init()
  {
    s_Settings = new();

    s_announcementText = GameResources._UI_Versus.GetChild(0).GetComponent<TMPro.TextMeshPro>();

    s_playerTeams = new int[4];

    Reset();
  }

  //
  public static void Reset()
  {
    s_playerScores = new int[4];
    s_PlayerLoadouts = new Loadout
    {
      _Equipment = new PlayerProfile.Equipment()
    };

    foreach (var playerProfile in PlayerProfile.s_Profiles)
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
    GameScript.s_Singleton.StartCoroutine(LevelStartCountdownCo());
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
    if (s_Settings._FreeForAll && SettingsHelper._NumberPlayers > 1) return -1;

    var teamsCounted = new List<int>();
    for (var i = 0; i < SettingsHelper._NumberPlayers; i++)
    {
      var teamId = s_playerTeams[i];

      if (teamsCounted.Contains(teamId)) continue;
      teamsCounted.Add(teamId);
    }

    return teamsCounted.Count;
  }
  public static bool HasMultipleTeams()
  {
    return (s_Settings._FreeForAll && SettingsHelper._NumberPlayers > 1) || GetNumberTeams() > 1;
  }

  //
  static int s_deathCheckIndex;
  public static void OnPlayerDeath(PlayerScript playerDied, PlayerScript playerSource)
  {
    if (!s_gamePlaying)
      return;
    if (!playerDied._IsOriginal)
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
                  if (player._Ragdoll._health > 0)
                    lastAlive = player;

                // Check game types
                var playerColor = PlayerProfile.s_Profiles[lastAlive._Id].GetColorName(false);

                s_playerScores[lastAlive._Id]++;
                s_playerScoresRound[lastAlive._Id]++;

                OnScore();

                foreach (var playerProfile in PlayerProfile.s_Profiles)
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
                if (player._Ragdoll._health > 0)
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
                foreach (var playerProfile in PlayerProfile.s_Profiles)
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
              foreach (var playerProfile in PlayerProfile.s_Profiles)
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
                  for (var i = 0; i < SettingsHelper._NumberPlayers; i++)
                  {
                    var playerScore = s_playerScoresRound[i];
                    var playerScoreTotal = s_playerScores[i];
                    var playerColor = PlayerProfile.s_Profiles[i].GetColorName(false);
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
                  for (var i = 0; i < SettingsHelper._NumberPlayers; i++)
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
                    var playerColor = PlayerProfile.s_Profiles[playerWinner].GetColorName(false);

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
                if (player._Ragdoll._health > 0)
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
              foreach (var playerProfile in PlayerProfile.s_Profiles)
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
                GameScript.TogglePause(Menu.MenuType.VERSUS);
                Menu.SwitchMenu(Menu.MenuType.VERSUS);
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
    GameScript.s_Singleton.StartCoroutine(CheckPlayerDeathStatusCo());
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
      switch (SettingsHelper._NumberPlayers)
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
    foreach (var playerProf in PlayerProfile.s_Profiles)
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

      Menu.PlayNoise(Menu.Noise.TEAM_SWAP);
    }

    Menu._CanRender = false;
    Menu.RenderMenu();
  }

  //
  static int GetTeamScore(int teamId, bool roundScoreOnly = false)
  {
    for (var i = 0; i < SettingsHelper._NumberPlayers; i++)
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
      var numPlayers = SettingsHelper._NumberPlayers;
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
  static ItemManager.Items GetRandomMeleeWeapon()
  {
    return Random.Range(0, 6) switch
    {
      0 => ItemManager.Items.FRYING_PAN,
      1 => ItemManager.Items.RAPIER,
      2 => ItemManager.Items.AXE,
      3 => ItemManager.Items.FIST,
      4 => ItemManager.Items.STUN_BATON,

      _ => ItemManager.Items.KNIFE,
    };
  }
  static ItemManager.Items GetRandomGun()
  {
    return Random.Range(0, 20) switch
    {
      0 => ItemManager.Items.PISTOL_DOUBLE,
      1 => ItemManager.Items.PISTOL_MACHINE,
      2 => ItemManager.Items.PISTOL_CHARGE,
      3 => ItemManager.Items.REVOLVER,

      4 => ItemManager.Items.RIFLE,
      5 => ItemManager.Items.RIFLE_LEVER,
      6 => ItemManager.Items.RIFLE_CHARGE,

      7 => ItemManager.Items.UZI,
      8 => ItemManager.Items.AK47,
      9 => ItemManager.Items.SNIPER,
      10 => ItemManager.Items.FLAMETHROWER,
      11 => ItemManager.Items.GRENADE_LAUNCHER,
      12 => ItemManager.Items.CROSSBOW,
      13 => ItemManager.Items.DMR,
      14 => ItemManager.Items.M16,

      15 => ItemManager.Items.SHOTGUN_BURST,
      16 => ItemManager.Items.SHOTGUN_DOUBLE,
      17 => ItemManager.Items.SHOTGUN_PUMP,
      18 => ItemManager.Items.STICKY_GUN,

      _ => ItemManager.Items.PISTOL_SILENCED,
    };
  }

  static UtilityType GetRandomUtility()
  {
    return Random.Range(0, 15) switch
    {
      1 => UtilityType.GRENADE_IMPACT,
      2 => UtilityType.GRENADE_STUN,

      3 => UtilityType.KUNAI_STICKY,
      4 => UtilityType.SHURIKEN,
      5 => UtilityType.SHURIKEN_BIG,

      6 => UtilityType.TACTICAL_BULLET,
      7 => UtilityType.MIRROR,

      8 => UtilityType.MORTAR_STRIKE,
      9 => UtilityType.C4,
      10 => UtilityType.INVISIBILITY,
      11 => UtilityType.TEMP_SHIELD,

      12 => UtilityType.BEAR_TRAP,
      13 => UtilityType.MINE,

      14 => UtilityType.COIN,

      _ => UtilityType.GRENADE
    };
  }
  static Perk.PerkType GetRandomPerk()
  {
    return Random.Range(0, 10) switch
    {
      0 => Perk.PerkType.MAX_AMMO_UP,
      1 => Perk.PerkType.EXPLOSION_RESISTANCE,
      2 => Perk.PerkType.SMART_BULLETS,
      3 => Perk.PerkType.LASER_SIGHTS,
      4 => Perk.PerkType.SPEED_UP,
      5 => Perk.PerkType.MARTIAL_ARTIST,
      6 => Perk.PerkType.THRUST,
      7 => Perk.PerkType.TWIN,
      8 => Perk.PerkType.BULLET_DESTROYER,

      _ => Perk.PerkType.FASTER_RELOAD
    };
  }
  static void SetRandomLoadout()
  {

    if (Random.Range(0, 15) == 0)
    {
      s_PlayerLoadouts = new Loadout()
      {
        _Equipment = new PlayerProfile.Equipment()
        {
          _ItemLeft0 = ItemManager.Items.KATANA,
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
          s_PlayerLoadouts = new Loadout()
          {
            _Equipment = new PlayerProfile.Equipment()
            {
              _ItemLeft0 = GetRandomMeleeWeapon(),
              _ItemRight0 = Random.Range(0, 4) > 2 ? GetRandomMeleeWeapon() : GetRandomGun(),
            }
          };
          break;
        case 4:
          s_PlayerLoadouts = new Loadout()
          {
            _Equipment = new PlayerProfile.Equipment()
            {
              _ItemLeft0 = GetRandomGun(),
              _ItemRight0 = GetRandomGun()
            }
          };
          break;
        case 5:
          s_PlayerLoadouts = new Loadout()
          {
            _Equipment = new PlayerProfile.Equipment()
            {
              _ItemLeft0 = ItemManager.Items.NONE,
              _ItemRight0 = GetRandomGun()
            }
          };
          break;
        case 6:
          s_PlayerLoadouts = new Loadout()
          {
            _Equipment = new PlayerProfile.Equipment()
            {
              _ItemLeft0 = GetRandomMeleeWeapon(),
              _ItemRight0 = ItemManager.Items.NONE
            }
          };
          break;
      }

    // Add utilities
    if (Random.Range(0, 5) == 0)
    {
      s_PlayerLoadouts._Equipment._UtilitiesLeft = new UtilityType[Random.Range(1, 4)];
      var randomUtil = GetRandomUtility();
      for (var i = 0; i < s_PlayerLoadouts._Equipment._UtilitiesLeft.Length; i++)
      {
        s_PlayerLoadouts._Equipment._UtilitiesLeft[i] = randomUtil;
      }

      if (Random.Range(0, 5) == 0)
      {
        s_PlayerLoadouts._Equipment._UtilitiesRight = new UtilityType[Random.Range(1, 4)];
        randomUtil = GetRandomUtility();
        for (var i = 0; i < s_PlayerLoadouts._Equipment._UtilitiesRight.Length; i++)
        {
          s_PlayerLoadouts._Equipment._UtilitiesRight[i] = randomUtil;
        }
      }
    }

    // Add mods
    if (Random.Range(0, 5) == 0)
    {
      s_PlayerLoadouts._Equipment._Perks = new();
      for (var i = 0; i < Random.Range(1, 3); i++)
      {

        var randomPerk = GetRandomPerk();
        if (s_PlayerLoadouts._Equipment._Perks.Contains(randomPerk))
        {
          i--;
          continue;
        }

        s_PlayerLoadouts._Equipment._Perks.Add(randomPerk);
      }
    }
  }

  //
  public static void ToggleTeammodeSwitchUi(bool toggle)
  {
    foreach (var playerProf in PlayerProfile.s_Profiles)
    {
      var controlUi = playerProf._VersusUI.GetChild(1).GetChild(0).gameObject;
      controlUi.SetActive(toggle);
    }
  }

  //
  public static List<PlayerScript> GetEnemyPlayers(int ofPlayerId)
  {
    var enemyList = new List<PlayerScript>();
    var thisTeamId = GetTeamId(ofPlayerId);

    for (var i = 0; i < SettingsHelper._NumberPlayers; i++)
    {

      if (i >= PlayerScript.s_Players.Count) continue;

      var otherPlayer = PlayerScript.s_Players[i];
      if (otherPlayer._Id == ofPlayerId) continue;

      var otherTeamId = GetTeamId(otherPlayer._Id);
      if (otherTeamId != thisTeamId)
        enemyList.Add(otherPlayer);
    }

    return enemyList;
  }
}