using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
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

  // Use this for initialization
  void Start()
  {
    if (_PlayerSpawns == null) _PlayerSpawns = new List<PlayerspawnScript>();
    _PlayerSpawns.Add(this);

    if (!GameScript._EditorEnabled)
      _visual.SetActive(false);
  }

  private void OnDestroy()
  {
    if (_PlayerSpawns == null) return;
    _PlayerSpawns.Remove(this);
  }

  public PlayerScript SpawnPlayer(bool setActive = true)
  {
    // Create a new player
    GameObject player = Instantiate(GameScript.s_CustomNetworkManager._Connected ? GameResources._PlayerNetwork : GameResources._Player);
    player.transform.parent = GameObject.Find("Players").transform;
    player.name = "Player";

    // Spawn them based on the this transform
    player.transform.position = transform.position;
    //Debug.Log($"= Spawning player at position: [{transform.position}]");

    FunctionsC.RotateLocal(ref player, transform.localEulerAngles.y);

    // Activate the player script
    player.SetActive(setActive);

    // Fire level load
    if (!GameScript.IsSurvival())
      GameScript.OnLevelStart();

    // Check network spawn
    var playerId = GameScript.s_CustomNetworkManager._PlayerSpawnId++;
    if (GameScript.s_CustomNetworkManager._Connected)
      NetworkServer.Spawn(player, GameScript.s_CustomNetworkManager.GetPlayer(playerId)._NetworkBehavior.connectionToClient);

    return player.transform.GetChild(0).GetComponent<PlayerScript>();
  }
}