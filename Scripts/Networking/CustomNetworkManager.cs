using System.Collections.Generic;
using UnityEngine;

public class CustomNetworkManager : MonoBehaviour// : NetworkManager
{

  //
  public bool _Connected { get { return _Players.Count > 0; } }
  public bool _IsServer;

  public PlayerData _Self;
  public Dictionary<uint, PlayerData> _Players;
  public class PlayerData
  {
    public CustomNetworkBehavior _NetworkBehavior;
    public bool _MapLoaded;

    public PlayerData(CustomNetworkBehavior customNetworkBehavior, int playerId)
    {
      _NetworkBehavior = customNetworkBehavior;
      _NetworkBehavior._PlayerId = playerId;
    }
  }

  public string _CurrentMapData;
  public int _PlayerSpawnId;

  //
  public CustomNetworkManager() : base()
  {
    ResetNetwork();
  }
  public void ResetNetwork()
  {
    _Players = new();

    _CurrentMapData = "";
  }

  /*public void Connect()
  {
    _IsServer = true;

    StartHost();
  }
  public void Host()
  {
    networkAddress = "localhost";
    StartClient();
  }

  //
  public override void OnClientConnect()
  {
    base.OnClientConnect();

    Debug.Log("Client connected");
  }
  public override void OnClientDisconnect()
  {
    base.OnClientDisconnect();

    Debug.Log("Client disconnected");
  }*/

  //
  public PlayerData GetPlayer(int playerId)
  {
    var keys = new List<uint>(_Players.Keys);
    return _Players[keys[playerId]];
  }

  //
  public int OnPlayerBehaviourCreated(CustomNetworkBehavior player)
  {
    var playerId = _Players.Count;
    var playerData = new PlayerData(player, playerId);
    _Players.Add(player.netIdentity.netId, playerData);
    if (player.isLocalPlayer)
      _Self = playerData;

    Debug.Log($"Player {playerId} added to list");

    return playerId;
  }

  //
  public void OnLevelLoad(string levelData)
  {
    if (_Connected && _IsServer)
    {
      MarkAllLevelsUnloaded();

      _Self._NetworkBehavior.CmdLoadMap(levelData);
    }
  }
  public void MarkAllLevelsUnloaded()
  {
    foreach (var playerData in _Players)
    {
      var player = playerData.Value;
      player._MapLoaded = false;
    }

    _PlayerSpawnId = 0;
  }
  public void MarkLevelLoaded(uint netId)
  {
    _Players[netId]._MapLoaded = true;
  }
  public bool AllPlayersLoaded()
  {
    foreach (var playerData in _Players)
    {
      if (!playerData.Value._MapLoaded)
        return false;
    }
    return true;
  }

}