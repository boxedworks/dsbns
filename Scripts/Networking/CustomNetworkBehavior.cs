using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;

public class CustomNetworkBehavior : NetworkBehaviour
{

  public int _PlayerId;

  public void Start()
  {
    RegisterPlayer();
  }

  // Register new connection
  void RegisterPlayer()
  {
    var playerId = GameScript.s_CustomNetworkManager.OnPlayerBehaviourCreated(this);

    if (GameScript.s_CustomNetworkManager._IsServer)
      SendPlayerId(playerId);
  }
  [Server]
  void SendPlayerId(int playerId)
  {
    SetPlayerId(connectionToClient, playerId);
  }
  [TargetRpc]
  public void SetPlayerId(NetworkConnectionToClient target, int playerId)
  {
    Debug.Log($"Set playerId {playerId} ... {isLocalPlayer}");
    _PlayerId = playerId;
  }

  // Load a map
  [Command]
  public void CmdLoadMap(string mapData)
  {
    var keys = new List<uint>(GameScript.s_CustomNetworkManager._Players.Keys);
    for (var i = 1; i < GameScript.s_CustomNetworkManager._Players.Count; i++)
    {
      var player = GameScript.s_CustomNetworkManager._Players[keys[i]];
      TargetLoadMap(player._NetworkBehavior.connectionToClient, mapData);
    }
  }
  [TargetRpc]
  public void TargetLoadMap(NetworkConnectionToClient target, string mapData)
  {
    GameScript.s_CustomNetworkManager._CurrentMapData = mapData;
  }

  //
  [Command]
  public void CmdMarkMapLoadComplete()
  {
    GameScript.s_CustomNetworkManager.MarkLevelLoaded(netIdentity.netId);
  }
}
