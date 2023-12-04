using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FunctionsC
{
  static Dictionary<string, Dictionary<string, AudioSource>> s_soundLibrary;
  public static ParticleSystem[] s_ParticlesAll;
  //static List<ParticleSystem> _BloodParticles;
  public static Transform s_Sounds;

  public static BookManager s_BookManager;
  public static void Init()
  {
    s_soundLibrary = new Dictionary<string, Dictionary<string, AudioSource>>();
    s_ParticlesAll = Object.FindObjectsOfType<ParticleSystem>();

    // Populate audio library
    s_Sounds = Camera.main.transform.parent.GetChild(2);
    for (var i = 0; i < s_Sounds.childCount; i++)
    {
      var root = s_Sounds.GetChild(i);
      var sublib = new Dictionary<string, AudioSource>();
      for (var u = 0; u < root.childCount; u++)
      {
        var sound = root.GetChild(u);
        sublib.Add(sound.name, sound.GetComponent<AudioSource>());
      }
      s_soundLibrary.Add(root.name, sublib);
    }

    //
    s_BookManager = new BookManager();
  }

  public static void RotateLocal(ref GameObject gameObject, float newLocalY)
  {
    Quaternion rotation = gameObject.transform.localRotation;
    rotation.eulerAngles = new Vector3(rotation.eulerAngles.x, newLocalY, rotation.eulerAngles.z);
    gameObject.transform.localRotation = rotation;
  }

  // Data structure returned via Player distance queries
  public class DistanceInfo
  {
    public ActiveRagdoll _ragdoll;
    public float _distance;
  }
  // Return the closest player to a point
  public static DistanceInfo GetClosestPlayerTo(Vector3 pos, int playerIdFilter = -1)
  {
    if (PlayerScript.s_Players == null) return null;

    var info = new DistanceInfo
    {
      _distance = 1000f
    };
    foreach (var player in PlayerScript.s_Players)
    {
      if (player._Id == playerIdFilter || player._ragdoll._IsDead || player._ragdoll._Hip == null) continue;
      var dist = MathC.Get2DDistance(player._ragdoll._Hip.position, pos);
      if (dist < info._distance)
      {
        info._distance = dist;
        info._ragdoll = player._ragdoll;
      }
    }
    return info;
  }
  public static DistanceInfo GetClosestEnemyTo(Vector3 pos, bool include_chaser = true)
  {
    if (EnemyScript._Enemies_alive == null) return null;
    var info = new DistanceInfo();
    info._distance = 1000f;
    foreach (var enemy in EnemyScript._Enemies_alive)
    {
      if (enemy.IsChaser() && !include_chaser) { continue; }
      var dist = MathC.Get2DDistance(enemy.GetRagdoll()._Hip.position, pos);
      if (dist < info._distance)
      {
        info._distance = dist;
        info._ragdoll = enemy.GetRagdoll();
      }
    }
    return info;
  }
  // Return the farthest player from a point
  public static DistanceInfo GetFarthestPlayerFrom(Vector3 pos)
  {
    if (PlayerScript.s_Players == null) return null;

    var info = new DistanceInfo();
    info._distance = -1000f;

    foreach (var p in PlayerScript.s_Players)
    {
      if (p._ragdoll._IsDead) continue;
      var dist = MathC.Get2DDistance(p._ragdoll._Hip.position, pos);
      if (dist > info._distance)
      {
        info._distance = dist;
        info._ragdoll = p._ragdoll;
      }
    }

    return info;
  }

  public static float GetPathLength(params Vector3[] corners)
  {
    var dist = 0f;
    for (var i = 1; i < corners.Length; i++)
      dist += (corners[i] - corners[i - 1]).magnitude;
    return dist;
  }

  public enum ParticleSystemType
  {
    BLOOD,
    BULLET_CASING,
    SPARKS,
    EXPLOSION,
    GUN_FIRE,
    SMOKE,
    FIREBALL,
    SMOKEBALL,
    FOOTPRINT,
    RAIN,
    RAIN_FAKE,
    MAGAZINE,
    SPARKS_ROBOT,
    PAPER,
    BULLET_COLLIDE,
    EXPLOSION_MARK, EXPLOSION_MARK_SMALL,
    GIBLETS,
    CONFETTI,
    BULLET_CASING_HOT,
    CONFUSED,
    EXPLOSION_STUN,
    SMOKE_RING,
    FOOTPRINT_BLOOD,
    TACTICAL_BULLET,
    TABLE_FLIP
  }
  static int _ExplosionIter;
  public static ParticleSystem[] GetParticleSystem(ParticleSystemType particleType, int forceParticleIndex = -1)
  {
    var particles = GameResources.s_Particles.transform;
    var index = -1;
    bool randomChild = false,
      hasChildren = false; // True if contains multiple particle systems
    switch (particleType)
    {
      case ParticleSystemType.BLOOD:
        index = 0;
        randomChild = true;
        break;
      case ParticleSystemType.BULLET_CASING:
        index = 1;
        break;
      case ParticleSystemType.SPARKS:
        index = 2;
        hasChildren = true;
        break;
      case ParticleSystemType.EXPLOSION:
        index = 3;
        hasChildren = true;
        break;
      case ParticleSystemType.GUN_FIRE:
        index = 4;
        hasChildren = true;
        break;
      case ParticleSystemType.SMOKE:
        index = 5;
        break;
      case ParticleSystemType.FIREBALL:
        index = 6;
        hasChildren = true;
        break;
      case ParticleSystemType.SMOKEBALL:
        index = 7;
        hasChildren = true;
        break;
      case ParticleSystemType.FOOTPRINT:
        index = 8;
        break;
      case ParticleSystemType.RAIN:
        index = 9;
        break;
      case ParticleSystemType.RAIN_FAKE:
        index = 10;
        break;
      case ParticleSystemType.MAGAZINE:
        index = 12;
        break;
      case ParticleSystemType.SPARKS_ROBOT:
        index = 13;
        break;
      case ParticleSystemType.PAPER:
        index = 14;
        randomChild = true;
        break;
      case ParticleSystemType.BULLET_COLLIDE:
        index = 15;
        randomChild = true;
        break;
      case ParticleSystemType.EXPLOSION_MARK:
        index = 16;
        break;
      case ParticleSystemType.EXPLOSION_MARK_SMALL:
        index = 17;
        break;
      case ParticleSystemType.GIBLETS:
        index = 18;
        break;
      case ParticleSystemType.CONFETTI:
        index = 19;
        randomChild = true;
        break;
      case ParticleSystemType.BULLET_CASING_HOT:
        index = 21;
        break;

      case ParticleSystemType.CONFUSED:
        index = 23;
        break;
      case ParticleSystemType.EXPLOSION_STUN:
        index = 24;
        hasChildren = true;
        break;

      case ParticleSystemType.SMOKE_RING:
        index = 25;
        break;

      case ParticleSystemType.FOOTPRINT_BLOOD:
        index = 26;
        break;

      case ParticleSystemType.TACTICAL_BULLET:
        index = 27;
        break;

      case ParticleSystemType.TABLE_FLIP:
        index = 28;
        break;
    }

    if (forceParticleIndex != -1)
    {
      return new ParticleSystem[] { particles.GetChild(index).GetChild(forceParticleIndex).GetComponent<ParticleSystem>() };
    }

    if (randomChild)
    {
      var cn = particles.GetChild(index).childCount;
      if (cn == 0) return null;
      ParticleSystem particle_return = null;
      int loops = cn, startIter = Mathf.RoundToInt(Random.value * (cn - 1));
      while (particle_return == null || particle_return.isPlaying)
      {
        particle_return = particles.GetChild(index).GetChild(startIter++ % cn).GetComponent<ParticleSystem>();
        if (--loops < 0) break;
      }
      return new ParticleSystem[] { particle_return };
    }

    else if (hasChildren)
    {
      var container = particles.GetChild(index);
      if (
        particleType == ParticleSystemType.EXPLOSION ||
        particleType == ParticleSystemType.EXPLOSION_STUN ||
        particleType == ParticleSystemType.FIREBALL ||
        particleType == ParticleSystemType.SMOKEBALL
        )
        container = particles.GetChild(index).GetChild(_ExplosionIter++ % container.childCount);
      var returnList = new List<ParticleSystem>();
      for (var i = 0; i < container.childCount; i++)
      {
        returnList.Add(container.GetChild(i).GetComponent<ParticleSystem>());
      }

      return returnList.ToArray();
    }

    else
      return new ParticleSystem[] { particles.GetChild(index).GetComponent<ParticleSystem>() };
  }

  public static void PlayComplexParticleSystemAt(ParticleSystem[] particles, Vector3 position)
  {

    // Set position
    if (particles.Length > 1)
      particles[0].transform.parent.position = position;
    else
      particles[0].transform.position = position;

    // Play
    for (var i = 0; i < particles.Length; i++)
    {
      var particle = particles[i];
      //particle.transform.Rotate(new Vector3(0f, 1f, 0f) * 360f * Random.value);
      particle.Play();
    }
  }
  public static ParticleSystem[] PlayComplexParticleSystemAt(ParticleSystemType type, Vector3 position)
  {
    var particles = GetParticleSystem(type);
    PlayComplexParticleSystemAt(particles, position);
    return particles;
  }

  // Find folder
  static public AudioSource GetAudioSource(string soundPath)
  {
    var split = soundPath.Split('/');
    string folder = split[0], name = split[1];
    var sfx_source = s_soundLibrary[folder][name];
    if (sfx_source == null)
    {
      Debug.LogError("Need to implement random picker");
      return null;
    }

    return sfx_source;
  }

  public static Powerup SpawnPowerup(Powerup.PowerupType type)
  {
    var powerup = GameObject.Instantiate(Resources.Load("Powerup") as GameObject);
    var s = powerup.GetComponent<Powerup>();
    s._type = type;
    powerup.transform.parent = GameResources._Container_Objects;
    return s;
  }

  public static bool IsMouseOverGameWindow()
  {
    var mousepos = ControllerManager.GetMousePosition();
    return !(0 > mousepos.x || 0 > mousepos.y || Screen.width < mousepos.x || Screen.height < mousepos.y);
  }

  // Return a list a ragdolls if they are a distance (radius) from a point (distance)
  public static ActiveRagdoll[] GetRagdollsInRadius(Vector3 position, float radius, bool raycast = false)
  {
    var returnList = new List<ActiveRagdoll>();
    foreach (var r in ActiveRagdoll.s_Ragdolls)
    {
      // Sanitize
      if (r._Hip == null) continue;

      // Check radius
      if (MathC.Get2DDistance(position, r._Hip.position) > radius) continue;
      returnList.Add(r);
    }
    return returnList.ToArray();
  }

  public static void SpawnExplosionScar(Vector3 position, float scale = 1f)
  {
    var particle_system_type = scale < 2.3f ? ParticleSystemType.EXPLOSION_MARK_SMALL : ParticleSystemType.EXPLOSION_MARK;
    FunctionsC.PlayComplexParticleSystemAt(particle_system_type, position);
  }

  // Apply damage and force in a radius to Ragdolls
  public static void ApplyExplosionRadius(Vector3 posAt, float explosionRadius, ExplosiveScript.ExplosionType explosionType, ActiveRagdoll explosionSource)
  {

    // Gather ragdolls in radius
    var ragdolls = GetRagdollsInRadius(posAt, explosionRadius * 2f);

    // Toggle raycasting for rags
    foreach (var ragdoll in ragdolls)
      ragdoll.ToggleRaycasting(false);

    // SFX
    PlayComplexParticleSystemAt(explosionType != ExplosiveScript.ExplosionType.STUN ? ParticleSystemType.EXPLOSION : ParticleSystemType.EXPLOSION_STUN, posAt + new Vector3(0f, 0.2f, 0f));

    // Send explosion to rigidbody handler
    ActiveRagdoll.Rigidbody_Handler.ApplyExplosion(posAt, explosionRadius);

    // Loop through ragdolls to raycast and affect
    var numKilled = 0;
    var isStunExplosion = explosionType == ExplosiveScript.ExplosionType.STUN;
    var posOrigin = new Vector3(posAt.x, -0.1f, posAt.z);
    foreach (var ragdoll in ragdolls)
    {

      var explosionDist = MathC.Get2DDistance(posAt, ragdoll._Hip.position);

      // Force backward
      if (explosionDist > explosionRadius)
      {
        ragdoll.BounceFromPosition(posAt, (1f - (explosionDist - explosionRadius) / (explosionRadius * 1.0f)) * 2.5f, false);
        continue;
      }

      // Check for perk
      //if (!isStun)
      if (ragdoll._IsPlayer && ragdoll._Id == explosionSource._Id && ragdoll._PlayerScript.HasPerk(Shop.Perk.PerkType.EXPLOSION_RESISTANCE)) continue;

      // Check dist
      if (explosionDist < 0.3f) { }

      // Raycast to validate
      else
      {
        ragdoll.ToggleRaycasting(true, true);
        var raycastHit = new RaycastHit();
        var dir = -(ragdoll._IsDead ? posOrigin - ragdoll._Hip.position : MathC.Get2DVector(posOrigin - ragdoll._Hip.position)).normalized;
        if (!Physics.SphereCast(new Ray(posOrigin, dir), 0.05f, out raycastHit, explosionRadius + 5f, GameResources._Layermask_Ragdoll))
        {
          ragdoll.ToggleRaycasting(false, true);
          continue;
        }

        ragdoll.ToggleRaycasting(false, true);
        if (!ragdoll.IsSelf(raycastHit.collider.gameObject)) continue;
      }

      // Increment exploded counter and award achievements
      if (!isStunExplosion)
      {
        if (!ragdoll._IsDead && ragdoll._health <= 3)
        {
          // Check for 3+ kills for slowmo
          if (numKilled++ == 3)
            PlayerScript._SlowmoTimer += 2f;

#if UNITY_STANDALONE
          if (numKilled == 5)
            SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DEMO_LEVEL_0);
          else if (numKilled == 10)
            SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DEMO_LEVEL_1);
          else if (numKilled == 25)
            SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DEMO_LEVEL_2);
#endif
        }
      }

      // Check explosion effects
      {

        // Stun
        if (isStunExplosion)
        {
          ragdoll.Stun();
        }

        // Explosion
        else
        {
          // Explosion force type
          var explosionForce = Vector3.zero;
          if (explosionType == ExplosiveScript.ExplosionType.UPWARD) explosionForce = new Vector3(0f, 3000f, 0f);
          else if (explosionType == ExplosiveScript.ExplosionType.AWAY)
          {
            var dirAway = -(posAt - ragdoll._Hip.position).normalized;
            dirAway.y = 0.2f;
            explosionForce = dirAway * 3000f;
          }

          //
          var disintigrateCompletely = false;//explosionDist < 0.6f;
          var spawnBlood = !disintigrateCompletely;

          // Assign damage
          ragdoll.TakeDamage(
            new ActiveRagdoll.RagdollDamageSource()
            {
              Source = explosionSource,

              HitForce = explosionForce,

              Damage = 3,
              DamageSource = Vector3.zero,
              DamageSourceType = disintigrateCompletely ? ActiveRagdoll.DamageSourceType.FIRE : ActiveRagdoll.DamageSourceType.EXPLOSION,

              SpawnBlood = spawnBlood,
              SpawnGiblets = spawnBlood
            });

          if (ragdoll._IsDead)
          {

            if (disintigrateCompletely)
            {
              ragdoll.HideCompletely();
            }
            else
            {
              ragdoll.DismemberRandomTimes(explosionForce, Random.Range(1, 5));
              ragdoll._Hip.AddForce(explosionForce);
            }
          }
        }

      }

    }

    // Retoggle raycasting
    foreach (var r in ragdolls)
      if (!r._IsDead) r.ToggleRaycasting(true);
  }

  static Dictionary<string, Mesh> _Meshes;
  public static GameObject CombineMeshes(string key, GameObject[] gameObjects, bool disableRenderer = true)
  {
    void store(Mesh mesh)
    {
      if (_Meshes == null) _Meshes = new Dictionary<string, Mesh>();
      if (_Meshes.ContainsKey(key))
      {
        GameObject.Destroy(_Meshes[key]);
        _Meshes.Remove(key);
      }
      _Meshes.Add(key, mesh);
    }
    // Check null or empty
    if (gameObjects == null || gameObjects.Length == 0)
      return null;
    // Create material list for final combine
    var master_materials = new List<Material>();
    // Create mesh list to hold final meshes
    var master_filters = new List<MeshFilter>();
    // Create a mesh for each subMesh in the gameObjects
    CombineInstance[] combine;
    GameObject master_gameObject = null;
    MeshRenderer master_renderer = null;
    MeshFilter master_filter = null;
    Mesh master_mesh = null;
    int iter;
    for (var u = 0; u < gameObjects[0].GetComponent<MeshFilter>().sharedMesh.subMeshCount; u++)
    {
      combine = new CombineInstance[gameObjects.Length];
      master_gameObject = new GameObject("MeshHolder");
      master_renderer = master_gameObject.AddComponent<MeshRenderer>();
      master_filter = master_gameObject.AddComponent<MeshFilter>();
      master_mesh = new Mesh();
      master_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      iter = 0;
      foreach (var g in gameObjects)
      {
        var filter = g.GetComponent<MeshFilter>();
        combine[iter].mesh = filter.sharedMesh;
        combine[iter].transform = filter.transform.localToWorldMatrix;
        combine[iter++].subMeshIndex = u;
        if (disableRenderer) g.GetComponent<MeshRenderer>().enabled = false;
      }
      master_mesh.CombineMeshes(combine, true, true);
      master_filter.sharedMesh = master_mesh;
      master_materials.Add(gameObjects[0].GetComponent<MeshRenderer>().sharedMaterials[u]);
      master_filters.Add(master_filter);
    }
    // If only 1 mesh; return
    if (master_filters.Count == 1)
    {
      // Set materials
      master_renderer.sharedMaterials = master_materials.ToArray();
      // Store in dictionary
      store(master_mesh);
      // Return
      return master_gameObject;
    }
    // Else, combine remaining meshes into 1
    combine = new CombineInstance[master_filters.Count];
    master_gameObject = new GameObject("MeshHolder");
    master_renderer = master_gameObject.AddComponent<MeshRenderer>();
    master_filter = master_gameObject.AddComponent<MeshFilter>();
    master_mesh = new Mesh();
    iter = 0;
    foreach (var filter in master_filters)
    {
      combine[iter].mesh = filter.sharedMesh;
      combine[iter++].transform = filter.transform.localToWorldMatrix;
    }
    master_mesh.CombineMeshes(combine, false, true);
    master_filter.sharedMesh = master_mesh;
    master_renderer.sharedMaterials = master_materials.ToArray();

    // Clean up submeshes
    for (var i = master_filters.Count - 1; i >= 0; i--)
    {
      GameObject.Destroy(master_filters[i].sharedMesh);
      GameObject.Destroy(master_filters[i].gameObject);
    }
    // Store in dictionary
    store(master_mesh);
    // Return
    return master_gameObject;
  }

  // BOOKS
  public class BookManager
  {

    // Hold list of books in scene
    List<Transform> _books;

    // Reset list of books
    public void Init()
    {
      _books = new List<Transform>();
    }

    // Apply book FX
    public static void ExplodeBooks(Collider c, Vector3 source_pos)
    {
      if (!c.enabled) return;
      _Books.Remove(c.transform);

      c.enabled = false;
      c.transform.GetChild(0).gameObject.SetActive(false);

      var particles = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.PAPER);
      if (particles == null || particles.Length == 0) return;
      var paper = particles[0];
      paper.transform.position = c.transform.position + new Vector3(0f, 0.3f, 0f);// + _hip.transform.forward * 0.2f;//point;
      paper.transform.LookAt(source_pos);
      paper.transform.Rotate(new Vector3(0f, 1f, 0f) * 180f);
      var q = paper.transform.localRotation;
      q.eulerAngles = new Vector3(0f, q.eulerAngles.y, q.eulerAngles.z);
      paper.transform.localRotation = q;
      paper.transform.Rotate(new Vector3(1f, 0f, 0f), UnityEngine.Random.value * -20f);
      paper.Play();

      SfxManager.PlayAudioSourceSimple(source_pos, "Etc/Papers");
    }

    public static void RegisterBooks(Transform books)
    {
      FunctionsC.s_BookManager._books.Add(books);
    }

    public static List<Transform> _Books { get { return FunctionsC.s_BookManager._books; } }

  }


  // MUSIC
  public static class MusicManager
  {
    public static int s_CurrentTrack;
    public static string[] s_TrackNames = new string[]
    {
      // Menu music
      "Local Forecast - Elevator",
      "Local Forecast",
      "Deuces",

      "I Knew a Guy",
      "Intractable",
      "Backbay Lounge",

      "Long Stroll",
      "Apero Hour",
      "No Good Layabout",

      "Nouvelle Noel",
      "Long Stroll",
      "Poppers and Prosecco",

      "Backed Vibes Clean",
      "Off to Osaka",
      "Sidewalk Shade",

      "Shades of Spring",
      "Bossa Antigua",
      "George Street Shuffle",

      "Samba Isobel",
      "Opportunity Walks",
      "Rollin at 5 - 210 - full",

      "Mining by Moonlight",
      "AcidJazz",
      "Faster Does It",
    };
    static bool s_transitioning;

    public static AudioSource s_TrackSource;
    public static int s_TrackOffset = 3;

    // Create the AudioSource component and play the menu music
    public static void Init()
    {
      s_TrackSource = GameScript._s_Singleton.gameObject.AddComponent<AudioSource>();
      s_TrackSource.playOnAwake = false;
    }

    // Load an audio track async to prevent lag; returns a ResourceRequest that tells when the loading is complete
    static ResourceRequest LoadAudioClipAsync(int trackIndex)
    {
      return Resources.LoadAsync<AudioClip>("Music/" + s_TrackNames[trackIndex]);
    }

    public static void PlayTrack(int trackIndex)
    {
      if (trackIndex > s_TrackNames.Length - 1) throw new System.IndexOutOfRangeException($"Trying to access _TrackNames[{trackIndex}]");
      s_transitioning = true;
      // Stop track and unload resource
      if (s_TrackSource.isPlaying)
        s_TrackSource.Stop();
      UnloadCurrentTrack();
      IEnumerator LoadAsync()
      {
        ResourceRequest musicRequest = LoadAudioClipAsync(trackIndex);
        while (!musicRequest.isDone) yield return new WaitForSecondsRealtime(0.06f);
        s_TrackSource.clip = musicRequest.asset as AudioClip;
        s_TrackSource.Play();
        s_CurrentTrack = trackIndex;
        s_transitioning = false;
      }
      GameScript._s_Singleton.StartCoroutine(LoadAsync());
    }

    // fade out current music and play a new track
    public static void TransitionTo(int trackIndex)
    {
      if (trackIndex > s_TrackNames.Length - 1) throw new System.IndexOutOfRangeException($"Trying to access _TrackNames[{trackIndex}]");
      s_transitioning = true;
      IEnumerator TransitionToCo()
      {

        // Load next track
        ResourceRequest musicRequest = LoadAudioClipAsync(trackIndex);

        // Fade out
        float t = 1f;
        while (t > 0f)
        {
          t = Mathf.Clamp(t - 0.05f, 0f, 1f);
          yield return new WaitForSecondsRealtime(0.05f);
          s_TrackSource.volume = t * (Settings._VolumeMusic / 5f);
        }
        UnloadCurrentTrack();

        // Wait for music to load
        while (!musicRequest.isDone) yield return new WaitForSecondsRealtime(0.05f);
        s_TrackSource.Stop();

        // Play new song
        s_TrackSource.clip = musicRequest.asset as AudioClip;
        s_TrackSource.Play();
        s_TrackSource.volume = (Settings._VolumeMusic / 5f);
        s_CurrentTrack = trackIndex;
        s_transitioning = false;
      }
      GameScript._s_Singleton.StartCoroutine(TransitionToCo());
    }

    // Unload tracks not being played to reduce memory usage
    static void UnloadCurrentTrack()
    {
      Resources.UnloadAsset(s_TrackSource.clip);
      s_TrackSource.clip = null;
    }

    // Check if song ended, play next in queue
    public static void Update()
    {
      if (!s_TrackSource.isPlaying && !s_transitioning)
      {
        // Check for menu music
        if (Menu2._InMenus)
        {
          if (s_CurrentTrack < s_TrackOffset)
          {
            // If unlocked difficulty 1, play faster track
            if (Settings._DifficultyUnlocked > 0)
            {
              if (s_CurrentTrack != 1)
              {
                TransitionTo(1);
                return;
              }
            }
            // Else, play slower track
            else
            {
              if (s_CurrentTrack != 0)
              {
                TransitionTo(0);
                return;
              }
            }
            s_TrackSource.Play();
            return;
          }
        }

        // Else, play next track
        PlayNextTrack();
      }
    }

    public static void PlayNextTrack()
    {
      PlayTrack(GetNextTrackIter());
    }

    // How many levels a track playlist will span
    static int _TrackLevelRange = 12;
    // Returns the track iter based on current level pack
    public static int GetNextTrackIter()
    {
      // If main menu is showing, play main menu music
      if (Menu2._CurrentMenu._Type == Menu2.MenuType.MAIN) return 0;

      // SURVIVAL mode music
      if (GameScript.s_GameMode == GameScript.GameModes.SURVIVAL)
      {
        if (s_CurrentTrack == 0 || s_CurrentTrack == 1 || s_CurrentTrack == 2)
          s_CurrentTrack = 3;
        else
          s_CurrentTrack++;
        if (s_CurrentTrack >= s_TrackNames.Length)
          s_CurrentTrack -= 3;
        return s_CurrentTrack;
      }
      else
      {

        // Level pack music; random
        if (Levels._LevelPack_Playing || GameScript._EditorTesting)
        {

          var songs_length = s_TrackNames.Length - 3;
          var song_index = Random.Range(0, songs_length);

          return song_index + 3;
        }

        // CLASSIC mode music
        var iter_base = Mathf.RoundToInt((Levels._CurrentLevelIndex) / ((float)_TrackLevelRange) / ((float)s_TrackNames.Length / 3f - 1));
        iter_base *= 3;
        iter_base += s_TrackOffset;
        var iter = 0;
        if (s_CurrentTrack <= iter_base + 2 && s_CurrentTrack >= iter_base)
          iter = ++s_CurrentTrack;
        else
        {
          iter = iter_base + Random.Range(0, 3);
          if (iter == s_CurrentTrack)
            iter++;
        }
        if (iter > iter_base + 2)
          iter = iter_base;

        return iter;
      }
    }
  }

  // Generate garbage text
  public static string GenerateGarbageText(string input)
  {
    var b = string.Empty;
    for (int i = 0; i < input.Length; i++)
    {
      char nextchar = input[i];
      if (nextchar == ' ' ||
        nextchar == '\n')
      {
        b += nextchar;
        continue;
      }
      /*var r = Mathf.RoundToInt(Random.value * 12f);
      if (r == 0) b += '!';
      else if (r == 1) b += '#';
      else if (r == 2) b += '@';
      else if (r == 3) b += '/';
      else if (r == 4) b += '+';
      else if (r == 5) b += '$';
      else if (r == 6) b += '%';
      else if (r == 7) b += '*';
      else if (r == 8) b += '^';
      else if (r == 9) b += '.';
      else if (r == 10) b += '?';
      else b += nextchar;*/
      b += "*";
    }
    return b;
  }

  public static void OnControllerInput()
  {
    if (Settings._ForceKeyboard || ControllerManager._NumberGamepads == 0) return;
    Cursor.visible = false;
    Menu2._CheckMouse = false;
  }

  public enum Control
  {
    A,
    B,
    X,
    Y,
    DPAD,
    DPAD_UP,
    DPAD_DOWN,
    DPAD_LEFT,
    DPAD_RIGHT,
    L_TRIGGER,
    R_TRIGGER,
    L_BUMPER,
    R_BUMPER,
    L_STICK,
    R_STICK,
    START,
    SELECT,
  }

  public static Transform GetControl(Control control)
  {
    Transform t = null;
    var index = 0;
    if (
      control == Control.DPAD ||
      control == Control.DPAD_UP ||
      control == Control.DPAD_DOWN ||
      control == Control.DPAD_LEFT ||
      control == Control.DPAD_RIGHT ||
      control == Control.L_TRIGGER ||
      control == Control.R_TRIGGER ||
      control == Control.L_BUMPER ||
      control == Control.R_BUMPER ||
      control == Control.L_STICK ||
      control == Control.R_STICK
      )
    {
      t = GameObject.Instantiate(Resources.Load(@"UI\Controller_other") as GameObject).transform;
      switch (control)
      {
        case Control.DPAD:
          index = 0;
          break;
        case Control.DPAD_UP:
          index = 1;
          break;
        case Control.DPAD_DOWN:
          index = 2;
          break;
        case Control.DPAD_LEFT:
          index = 3;
          break;
        case Control.DPAD_RIGHT:
          index = 4;
          break;
        case Control.L_TRIGGER:
          index = 5;
          break;
        case Control.R_TRIGGER:
          index = 6;
          break;
        case Control.L_BUMPER:
          index = 7;
          break;
        case Control.R_BUMPER:
          index = 8;
          break;
        case Control.L_STICK:
          index = 9;
          break;
        case Control.R_STICK:
          index = 10;
          break;
      }
      // Remove other buttons
      for (var i = t.childCount - 1; i >= 0; i--)
        if (i != index)
          GameObject.Destroy(t.GetChild(i).gameObject);
    }
    else if (
        control == Control.A ||
        control == Control.B ||
        control == Control.X ||
        control == Control.Y
      )
    {
      t = GameObject.Instantiate(Resources.Load(@"UI\Controller_buttons") as GameObject).transform;
      switch (control)
      {
        case Control.A:
          index = 0;
          break;
        case Control.B:
          index = 1;
          break;
        case Control.X:
          index = 2;
          break;
        case Control.Y:
          index = 3;
          break;
      }
      // Dim other buttons
      for (var i = t.childCount - 1; i >= 0; i--)
        if (i != index)
          t.GetChild(i).GetComponent<MeshRenderer>().material.color *= Color.black;
    }
    else if (
         control == Control.START ||
         control == Control.SELECT
       )
    {
      t = GameObject.Instantiate(Resources.Load(@"UI\Controller_special") as GameObject).transform;
      switch (control)
      {
        case Control.SELECT:
          index = 0;
          break;
        case Control.START:
          index = 1;
          break;
      }
      // Dim other buttons
      for (var i = t.childCount - 1; i >= 0; i--)
        if (i != index)
          t.GetChild(i).GetComponent<MeshRenderer>().material.color *= Color.black;
    }

    // Return transform
    return t;
  }

  //
  public static float ParseFloatInvariant(this string data)
  {
    return float.Parse(data, System.Globalization.CultureInfo.InvariantCulture);
  }
  public static int ParseIntInvariant(this string data)
  {
    return int.Parse(data, System.Globalization.CultureInfo.InvariantCulture);
  }

  public static string ToStringTimer(this float data)
  {
    return data.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
  }

  // Handle simple radial AOE effects
  public static class AoeHandler
  {

    // Data types
    public enum AoeType
    {
      NONE,

      BLOOD,
      FIRE
    }

    struct AoeEffect
    {
      public AoeType Type;
      public Vector3 Position;

      public float Radius;
      public float Duration;

      public ActiveRagdoll Source;
    }

    // Containers
    static List<AoeEffect> s_aoeEffects;

    // Reset
    public static void Reset()
    {
      s_aoeEffects = new();
    }

    // Register a new AOE
    public static void RegisterAoeEffect(ActiveRagdoll source, AoeType type, Vector3 position, float radius, float duration)
    {
      s_aoeEffects.Add(new AoeEffect()
      {
        Source = source,

        Type = type,
        Position = position,
        Radius = radius,
        Duration = duration == -1f ? -1f : Time.time + duration
      });
    }

    // Check ragdolls in AOE
    static int s_i, s_u;
    const int _MAX_ITERATIONS = 6;
    public static void Update()
    {
      var iterations = 0;

      var ragdolls = ActiveRagdoll.s_Ragdolls;
      if ((s_aoeEffects?.Count ?? 0) == 0 || (ragdolls?.Count ?? 0) == 0) return;
      var maxIterations = Mathf.Clamp(_MAX_ITERATIONS, 1, ragdolls.Count);
      while (true)
      {

        s_i %= s_aoeEffects.Count;
        var aoe = s_aoeEffects[s_i];

        // Check aoe expire
        if (aoe.Duration != -1f && Time.time >= aoe.Duration)
        {
          s_aoeEffects.RemoveAt(s_i);

          if (s_aoeEffects.Count == 0) break;
          continue;
        }

        //Debug.DrawRay(aoe._Position, Vector3.up * 100f, Color.yellow);

        // Check aoe effect
        while (true)
        {
          s_u %= ragdolls.Count;
          var ragdoll = ragdolls[s_u];

          // Check distance in AOE
          if (ragdoll._IsDead || ragdoll._Hip == null) { }
          else
          {
            var hipPosition = ragdoll._Hip.position;
            if (MathC.Get2DDistance(hipPosition, aoe.Position) <= aoe.Radius)
            {

              // Set effect
              switch (aoe.Type)
              {
                case AoeType.BLOOD:
                  ragdoll.SetBloodTimer();
                  break;

                case AoeType.FIRE:
                  ragdoll.TakeDamage(new ActiveRagdoll.RagdollDamageSource()
                  {
                    Damage = 1,
                    Source = aoe.Source,
                    DamageSourceType = ActiveRagdoll.DamageSourceType.FIRE,
                  });
                  break;
              }
            }
          }

          //
          s_u++;
          iterations++;

          if (s_u >= ragdolls.Count)
          {
            s_u = 0;
            s_i++;
            break;
          }
          if (iterations > maxIterations) break;
        }

        //
        if (iterations > maxIterations) break;
      }
    }

  }

  //
  public static class TrashCollector
  {

    //
    static List<System.Tuple<GameObject, float>> s_trash;
    public static void Reset()
    {
      s_trash = new();
    }

    //
    static int s_incrementalIter;
    public static void Update()
    {

      // Make sure there is trash
      if ((s_trash?.Count ?? 0) == 0) return;

      // Remove trash after time
      var trashIndex = s_incrementalIter++ % s_trash.Count;
      var trash = s_trash[trashIndex];
      if (Time.time - trash.Item2 > 10f)
      {
        GameObject.Destroy(trash.Item1);
        s_trash.RemoveAt(trashIndex);
      }

    }

    //
    public static void RegisterTrash(GameObject g)
    {
      s_trash.Add(System.Tuple.Create(g, Time.time));
    }
  }

}