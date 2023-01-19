using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerspawnScript : MonoBehaviour
{

  public int _ItemPair;

  public static List<PlayerspawnScript> _PlayerSpawns;

  public GameObject _visual;

  // Use this for initialization
  void Start()
  {
    if (_PlayerSpawns == null) _PlayerSpawns = new List<PlayerspawnScript>();
    _PlayerSpawns.Add(this);

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
    GameObject player = Instantiate(Resources.Load("Player") as GameObject);
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

    return player.transform.GetChild(0).GetComponent<PlayerScript>();
  }
}