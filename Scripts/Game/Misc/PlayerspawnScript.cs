using System.Collections.Generic;
using SneakyGame.Settings;
using UnityEngine;
using SneakyGame.Game.Player;

public class PlayerspawnScript : MonoBehaviour
{

  public int _ItemPair;

  public static List<PlayerspawnScript> _PlayerSpawns;
  public static void ResetPlayerSpawns()
  {
    if (_PlayerSpawns.Count > 1)
      for (var i = _PlayerSpawns.Count - 1; i >= 1; i--)
      {
        var playerSpawn = _PlayerSpawns[i];

        _PlayerSpawns.RemoveAt(i);
        DestroyImmediate(playerSpawn.gameObject);
      }
  }
  static int s_playerSpawnIndex;
  public static PlayerspawnScript GetPlayerSpawnScript()
  {
    if (s_playerSpawnIndex == _PlayerSpawns.Count)
    {
      var newSpawn = Instantiate(_PlayerSpawns[0].gameObject);
      newSpawn.name = _PlayerSpawns[0].name;
      return newSpawn.GetComponent<PlayerspawnScript>();
    }

    return _PlayerSpawns[s_playerSpawnIndex++];
  }
  public static void ResetPlayerSpawnIndex()
  {
    s_playerSpawnIndex = 0;
  }

  public GameObject _visual;

  int _id;
  // Use this for initialization
  void Start()
  {
    if (_PlayerSpawns == null) _PlayerSpawns = new List<PlayerspawnScript>();
    _PlayerSpawns.Add(this);
    _id = _PlayerSpawns.Count - 1;

    if (!GameScript.s_EditorEnabled)
    {
      _visual.SetActive(false);
    }

    if (GameScript.s_ExitLight != null)
      GameScript.s_ExitLight.spotAngle = 0f;
  }

  private void OnDestroy()
  {
    if (_PlayerSpawns == null) return;
    _PlayerSpawns.Remove(this);
  }

  public void SpawnPlayer(System.Action<PlayerScript> onSpawn = null, bool setActive = true)
  {
    SpawnPlayerAt(transform.position, transform.localEulerAngles.y, null, onSpawn, setActive, _id);
  }

  public static void SpawnPlayerAt(Vector3 atPosition, float rotateEulerAngle, PlayerScript connectedTwin, System.Action<PlayerScript> onSpawn = null, bool setActive = true, int spawnId = 0)
  {

    // Create a new player
    var player = Instantiate(GameResources._Player);
    player.transform.parent = GameObject.Find("Players").transform;
    player.name = "Player";

    // Spawn them based on the this transform
    var spawnPosition = atPosition;
    spawnPosition += new Vector3(Random.Range(-1f, 1f) * 0.01f, 0f, Random.Range(-1f, 1f) * 0.01f);
    if ((GameScript.s_IsPartyGameMode && !VersusMode.s_Settings._FreeForAll) || (GameScript.s_IsZombieGameMode))
    {
      var numSpawns = _PlayerSpawns.Count;
      var numPlayers = SettingsHelper._NumberPlayers;

      if (numPlayers > numSpawns)
        spawnPosition += new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.35f, 0.35f));
    }
    player.transform.position = spawnPosition;

    FunctionsC.SetRotationLocalY(ref player, rotateEulerAngle);

    // Activate the player script
    var playerScript = new PlayerScript(player.transform.GetChild(0), spawnId, connectedTwin);
    player.SetActive(setActive);

    // Fire level load
    if (!GameScript.s_IsZombieGameMode)
      GameScript.OnLevelStart();

    /*/ Check network spawn
    var playerId = GameScript.s_CustomNetworkManager._PlayerSpawnId++;
    if (GameScript.s_CustomNetworkManager._Connected)
      NetworkServer.Spawn(player, GameScript.s_CustomNetworkManager.GetPlayer(playerId)._NetworkBehavior.connectionToClient);*/

    onSpawn?.Invoke(playerScript);
  }

  public static int s_PlayerIter;
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
    var numSpawns = _PlayerSpawns.Count;
    var spawnList = new int[numSpawns];
    for (var i = 0; i < spawnList.Length; i++)
      spawnList[i] = i;
    rnd.Shuffle(spawnList);

    var numPlayers = SettingsHelper._NumberPlayers;
    var numTeams = VersusMode.GetNumberTeams();

    if ((GameScript.s_IsPartyGameMode && !VersusMode.s_Settings._FreeForAll ? numTeams : numPlayers) <= numSpawns || !GameScript.s_IsPartyGameMode || (GameScript.s_IsPartyGameMode && VersusMode.s_Settings._FreeForAll))
    {
      var spawnLocation = new int[numPlayers];
      if (GameScript.s_IsPartyGameMode && !VersusMode.s_Settings._FreeForAll && numPlayers > numSpawns)
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
      if (s_PlayerIter < SettingsHelper._NumberPlayers)
        for (; s_PlayerIter < SettingsHelper._NumberPlayers; s_PlayerIter++)
          _PlayerSpawns[spawnLocation[playerSpawnIndex++]].SpawnPlayer();
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

      if (s_PlayerIter < SettingsHelper._NumberPlayers)
        for (; s_PlayerIter < SettingsHelper._NumberPlayers; s_PlayerIter++)
          _PlayerSpawns[spawnOrder[VersusMode.GetTeamId(s_PlayerIter) % spawnOrder.Length]].SpawnPlayer();
    }
  }
}