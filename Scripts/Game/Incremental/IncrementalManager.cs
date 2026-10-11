
using SneakyGame.Game.Player;
using SneakyGame.Game.Survival;
using UnityEngine;
using UnityEngine.AI;

namespace SneakyGame.Game.Incremental
{

  public class IncrementalManager
  {
    const float s_spawnInterval = 0.1f;
    const float s_minSpawnRadius = 18f;
    const float s_maxSpawnRadius = 22f;
    const float s_navMeshSearchRadius = 4f;
    const int s_maxAliveZombies = 200;

    float _nextSpawnTime;

    public SpawnBehavior _SpawnBehavior = SpawnBehavior.RANDOM;

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
    }

    public void Update()
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

      switch (_SpawnBehavior)
      {
        case SpawnBehavior.NONE:
          spawnPosition = default;
          return false;

        case SpawnBehavior.TOP:
          offset = new Vector3(Random.Range(-radius, radius), 0f, radius);
          break;

        case SpawnBehavior.BOTTOM:
          offset = new Vector3(Random.Range(-radius, radius), 0f, -radius);
          break;

        case SpawnBehavior.LEFT:
          offset = new Vector3(-radius, 0f, Random.Range(-radius, radius));
          break;

        case SpawnBehavior.RIGHT:
          offset = new Vector3(radius, 0f, Random.Range(-radius, radius));
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