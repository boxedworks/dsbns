using System.Collections;
using System.Collections.Generic;

using UnityEngine;

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
        GameObject.DestroyImmediate(playerSpawn.gameObject);
      }
  }
  static int s_playerSpawnIndex;
  public static PlayerspawnScript GetPlayerSpawnScript()
  {
    if (s_playerSpawnIndex == _PlayerSpawns.Count)
    {
      var newSpawn = GameObject.Instantiate(_PlayerSpawns[0].gameObject);
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
    SpawnPlayerAt(transform.position, transform.localEulerAngles.y, onSpawn, setActive, _id);
  }

  public static void SpawnPlayerAt(Vector3 atPosition, float rotateEulerAngle, System.Action<PlayerScript> onSpawn = null, bool setActive = true, int spawnId = 0)
  {
    Debug.Log("Spawning player");

    // Create a new player
    var player = Instantiate(GameScript.s_CustomNetworkManager._Connected ? GameResources._PlayerNetwork : GameResources._Player);
    player.transform.parent = GameObject.Find("Players").transform;
    player.name = "Player";

    var playerScript = player.transform.GetChild(0).GetComponent<PlayerScript>();
    playerScript._PlayerSpawnId = spawnId;

    // Spawn them based on the this transform
    var spawnPosition = atPosition;
    spawnPosition += new Vector3(Random.Range(-1f, 1f) * 0.01f, 0f, Random.Range(-1f, 1f) * 0.01f);
    if ((GameScript.s_GameMode == GameScript.GameModes.VERSUS && !VersusMode.s_Settings._FreeForAll) || (GameScript.s_GameMode == GameScript.GameModes.SURVIVAL))
    {
      var numSpawns = _PlayerSpawns.Count;
      var numPlayers = Settings._NumberPlayers;

      if (numPlayers > numSpawns)
        spawnPosition += new Vector3(Random.Range(-0.35f, 0.35f), 0f, Random.Range(-0.35f, 0.35f));
    }
    player.transform.position = spawnPosition;
    //Debug.Log($"= Spawning player at position: [{transform.position}]");

    FunctionsC.RotateLocal(ref player, rotateEulerAngle);

    // Activate the player script
    player.SetActive(setActive);

    // Fire level load
    if (!GameScript.IsSurvival())
      GameScript.OnLevelStart();

    /*/ Check network spawn
    var playerId = GameScript.s_CustomNetworkManager._PlayerSpawnId++;
    if (GameScript.s_CustomNetworkManager._Connected)
      NetworkServer.Spawn(player, GameScript.s_CustomNetworkManager.GetPlayer(playerId)._NetworkBehavior.connectionToClient);*/

    onSpawn?.Invoke(playerScript);
  }
}