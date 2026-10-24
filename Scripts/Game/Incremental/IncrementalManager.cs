
using SneakyGame.Game.Player;
using SneakyGame.Game.Survival;
using SneakyGame.Ragdoll;
using UnityEngine;
using UnityEngine.AI;

namespace SneakyGame.Game.Incremental
{

  public class IncrementalManager
  {
    const int s_startingHealth = 10;
    const float s_spawnInterval = 0.1f;
    const float s_minSpawnRadius = 18f;
    const float s_maxSpawnRadius = 22f;
    const float s_navMeshSearchRadius = 4f;
    const int s_maxAliveZombies = 200;
    const float s_likesRequestInterval = 5f;
    const float s_respawnDelay = 5f;

    public const int s_screenWidth = 42;

    float _nextSpawnTime;
    float _nextLikesRequestTime;
    float _respawnTime = -1f;
    int _health = s_startingHealth;
    int _respawningPlayerId = -1;
    bool _restartRequested;

    public SpawnBehavior _SpawnBehavior = SpawnBehavior.RIGHT;
    public int Health => _health;

    public enum SpawnBehavior
    {
      NONE,

      RANDOM,
      TOP,
      BOTTOM,
      LEFT,
      RIGHT,
    }

    public static void Initialize()
    {
      var instance = new IncrementalManager();
      GameScript.s_Singleton._IncrementalManager = instance;

      GameUIController.SetIncrementalHealth(instance._health);
      IncrementalApiHelper.Init();
    }

    public void Update()
    {
      if (UpdateRespawn())
        return;

      if (UpdateRestart())
        return;

      UpdateLikesRequest();
      UpdateZombieSpawning();
    }

    bool UpdateRespawn()
    {
      if (_respawningPlayerId < 0)
        return false;

      var secondsRemaining = Mathf.Max(0, Mathf.CeilToInt(_respawnTime - Time.time));
      GameUIController.SetIncrementalRespawnCountdown(secondsRemaining);
      if (secondsRemaining > 0)
        return true;

      RespawnPlayer(_respawningPlayerId);
      _respawningPlayerId = -1;
      _respawnTime = -1f;
      GameUIController.HideIncrementalRespawnCountdown();
      return true;
    }

    bool UpdateRestart()
    {
      if (!_restartRequested)
        return false;

      if (!GameScript.ReloadMap())
        return true;

      _health = s_startingHealth;
      _restartRequested = false;
      GameUIController.SetIncrementalHealth(_health);
      return true;
    }

    public void OnZombieReachedExit()
    {
      if (_restartRequested)
        return;

      _health = Mathf.Max(0, _health - 1);
      GameUIController.SetIncrementalHealth(_health);
      _restartRequested = _health == 0;
    }

    public void OnPlayerDead(int playerId)
    {
      if (_respawningPlayerId >= 0)
        return;

      _respawningPlayerId = playerId;
      _respawnTime = Time.time + s_respawnDelay;
      GameUIController.SetIncrementalRespawnCountdown(Mathf.CeilToInt(s_respawnDelay));
    }

    static void RespawnPlayer(int playerId)
    {
      if (PlayerScript.s_Players != null)
      {
        for (var i = PlayerScript.s_Players.Count - 1; i >= 0; i--)
        {
          var player = PlayerScript.s_Players[i];
          if (player == null || player._Id != playerId)
            continue;

          if (player._Ragdoll != null)
            ActiveRagdoll.s_Ragdolls.Remove(player._Ragdoll);

          Object.Destroy(player.transform.parent.gameObject);
          PlayerScript.s_Players.RemoveAt(i);
          break;
        }
      }

      if (PlayerspawnScript._PlayerSpawns == null || PlayerspawnScript._PlayerSpawns.Count == 0)
      {
        Debug.LogError("Cannot respawn incremental player because no player spawn is available.");
        return;
      }

      PlayerScript._PLAYERID = playerId;
      PlayerspawnScript._PlayerSpawns[0].SpawnPlayer();
    }

    // Request total likes at regular intervals.
    void UpdateLikesRequest()
    {
      if (Time.time < _nextLikesRequestTime)
        return;

      _nextLikesRequestTime = Time.time + s_likesRequestInterval;
      IncrementalApiHelper.RequestInteractionData();
    }

    // Spawn zombies at regular intervals if conditions are met.
    void UpdateZombieSpawning()
    {
      if (Time.time < _nextSpawnTime || !CanSpawnZombie())
        return;

      _nextSpawnTime = Time.time + s_spawnInterval;

      if (!TryGetSpawnPosition(out var spawnPosition))
        return;

      var zombieType = Random.value switch
      {
        < 0.5f => SurvivalManager.EnemyType.KNIFE_WALK_SLOW,
        < 0.85f => SurvivalManager.EnemyType.KNIFE_WALK,
        _ => SurvivalManager.EnemyType.KNIFE_JOG
      };

      var zombie = EnemyScript.SpawnEnemyAt(
        new EnemyScript.SurvivalAttributes
        {
          _enemyType = zombieType
        },
        spawnPosition,
        false,
        true
      );

      zombie._moveSpeed = zombieType switch
      {
        SurvivalManager.EnemyType.KNIFE_WALK_SLOW => 0.25f,
        SurvivalManager.EnemyType.KNIFE_WALK => 0.5f,
        _ => 0.7f
      };
    }

    static bool CanSpawnZombie()
    {
      if (GameResources._Camera_Main == null)
        return false;

      if (EnemyScript._Enemies_alive != null && EnemyScript._Enemies_alive.Count >= s_maxAliveZombies)
        return false;

      if (PlayerScript.s_Players == null)
        return false;

      foreach (var player in PlayerScript.s_Players)
        if (player != null && player._Ragdoll != null && !player._Ragdoll._IsDead)
          return true;

      return false;
    }

    bool TryGetSpawnPosition(out Vector2 spawnPosition)
    {
      var cameraPosition = GameResources._Camera_Main.transform.position;
      var radius = Mathf.Sqrt(Random.Range(s_minSpawnRadius * s_minSpawnRadius, s_maxSpawnRadius * s_maxSpawnRadius));
      Vector3 offset;

      var horizontalRadius = 5f;

      switch (_SpawnBehavior)
      {
        case SpawnBehavior.NONE:
          spawnPosition = default;
          return false;

        case SpawnBehavior.TOP:
          offset = new Vector3(Random.Range(-horizontalRadius, horizontalRadius), 0f, radius);
          break;

        case SpawnBehavior.BOTTOM:
          offset = new Vector3(Random.Range(-horizontalRadius, horizontalRadius), 0f, -radius);
          break;

        case SpawnBehavior.LEFT:
          offset = new Vector3(-radius, 0f, Random.Range(-horizontalRadius, horizontalRadius));
          break;

        case SpawnBehavior.RIGHT:
          offset = new Vector3(radius, 0f, Random.Range(-horizontalRadius, horizontalRadius));
          break;

        default:
          var angle = Random.value * Mathf.PI * 2f;
          offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
          break;
      }

      var samplePosition = new Vector3(cameraPosition.x, 0f, cameraPosition.z) + offset;

      if (!NavMesh.SamplePosition(samplePosition, out var hit, s_navMeshSearchRadius, NavMesh.AllAreas))
      {
        spawnPosition = default;
        return false;
      }

      spawnPosition = new Vector2(hit.position.x, hit.position.z);
      return true;
    }

  }


}