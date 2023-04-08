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
        Transform sound = root.GetChild(u);
        sublib.Add(sound.name, sound.GetComponent<AudioSource>());
      }
      s_soundLibrary.Add(root.name, sublib);
    }

    //
    s_BookManager = new BookManager();

    /*/ Particle lists
    _BloodParticles = new List<ParticleSystem>();

    var particles = GameObject.Find("Particles").transform;
    var particles_blood = particles.GetChild(0);
    for (var i = 0; i < particles_blood.childCount; i++)
      _BloodParticles.Add(particles_blood.GetChild(i).GetComponent<ParticleSystem>());*/
  }

  public static void RotateLocal(ref GameObject gameObject, float newLocalY)
  {
    Quaternion rotation = gameObject.transform.localRotation;
    rotation.eulerAngles = new Vector3(rotation.eulerAngles.x, newLocalY, rotation.eulerAngles.z);
    gameObject.transform.localRotation = rotation;
  }

  public static void ChangePitch(ref AudioSource s, float min = 0.9f, float max = 1.1f)
  {
    if (min == max) { s.pitch = min; return; }
    s.pitch = min + Random.value * (max - min);
  }

  // Data structure returned via Player distance queries
  public class DistanceInfo
  {
    public ActiveRagdoll _ragdoll;
    public float _distance;
  }
  // Return the closest player to a point
  public static DistanceInfo GetClosestPlayerTo(Vector3 pos)
  {
    if (PlayerScript._Players == null) return null;
    DistanceInfo info = new DistanceInfo();
    info._distance = 1000f;
    foreach (var player in PlayerScript._Players)
    {
      if (player._ragdoll._dead || player._ragdoll._hip == null) continue;
      var dist = MathC.Get2DDistance(player._ragdoll._hip.position, pos);
      if (dist < info._distance)
      {
        info._distance = dist;
        info._ragdoll = player._ragdoll;
      }
    }
    return info;
  }
  public static DistanceInfo GetClosestEnemyTo(Vector3 pos)
  {
    if (EnemyScript._Enemies_alive == null) return null;
    var info = new DistanceInfo();
    info._distance = 1000f;
    foreach (var enemy in EnemyScript._Enemies_alive)
    {
      var dist = MathC.Get2DDistance(enemy.GetRagdoll()._hip.position, pos);
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
    if (PlayerScript._Players == null) return null;

    var info = new DistanceInfo();
    info._distance = -1000f;

    foreach (var p in PlayerScript._Players)
    {
      if (p._ragdoll._dead) continue;
      var dist = MathC.Get2DDistance(p._ragdoll._hip.position, pos);
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

  public static void PlayOneShot(AudioSource s, bool addToAudioList = true)
  {
    PlayOneShot(s, s.clip, addToAudioList);
  }
  public static void PlayOneShot(AudioSource s, AudioClip c, bool addToAudioList = true)
  {
    // Check for null
    if (s == null || c == null || Settings._VolumeSFX == 0) return;

    // Add to audio list
    if (addToAudioList)
      AddToAudioList(ref s);

    // Set volume
    s.volume *= Settings._VolumeSFX / 5f;

    // Play
    s.PlayOneShot(c);
  }
  public static void PlayAudioSource(ref AudioSource s, float min = 0.9f, float max = 1.1f, bool always_play = false)
  {
    if (s == null || (!always_play && Settings._VolumeSFX == 0))
      return;

    AddToAudioList(ref s);
    if (!s.isPlaying)
    {
      ChangePitch(ref s, min, max);
      s.volume *= Settings._VolumeSFX / 5f;
      s.Play();
    }
  }

  public enum ParticleSystemType
  {
    BLOOD,
    BULLET_CASING,
    SPARKS,
    EXPLOSION,
    GUN_SMOKE,
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
  }
  static int _ExplosionIter;
  public static ParticleSystem[] GetParticleSystem(ParticleSystemType particleType, int forceParticleIndex = -1)
  {
    var particles = GameObject.Find("Particles").transform;
    int index = -1;
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
      case ParticleSystemType.GUN_SMOKE:
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

  static Dictionary<AudioSource, System.Tuple<float, float>> _PlayingAudio;
  public static void AddToAudioList(ref AudioSource s)
  {
    if (s == null) return;
    if (_PlayingAudio == null) _PlayingAudio = new Dictionary<AudioSource, System.Tuple<float, float>>();
    if (_PlayingAudio.ContainsKey(s)) { _PlayingAudio[s] = System.Tuple.Create<float, float>(s.pitch, _PlayingAudio[s].Item2); return; }
    _PlayingAudio.Add(s, System.Tuple.Create<float, float>(s.pitch, s.volume));
  }
  public static void AddToAudioList_Rain()
  {
    if (_Rain_Pair == null) return;
    var rain_sfx = GameScript._Singleton._Rain_Audio;
    if (_PlayingAudio == null) _PlayingAudio = new Dictionary<AudioSource, System.Tuple<float, float>>();
    if (_PlayingAudio.ContainsKey(rain_sfx)) return;
    _PlayingAudio.Add(rain_sfx, _Rain_Pair);

  }

  // Find folder
  static public AudioSource GetAudioClip(string soundPath)
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

  static public void PlaySound(ref AudioSource speaker, string soundPath, float pitch_min = 1f, float pitch_max = 1f)
  {
    if (speaker == null || Settings._VolumeSFX == 0) return;
    if (_PlayingAudio == null) _PlayingAudio = new Dictionary<AudioSource, System.Tuple<float, float>>();

    // Find folder
    var sfx_source = GetAudioClip(soundPath);
    speaker.volume = sfx_source.volume;
    speaker.pitch = sfx_source.pitch;

    // Change pitch
    if (pitch_min != 1f || pitch_max != 1f) ChangePitch(ref speaker, pitch_min, pitch_max);

    // PlayOneShot
    PlayOneShot(speaker, sfx_source.clip);
  }

  static public void PlaySound(ref AudioSource speaker, AudioSource sfx_source, float min = 1f, float max = 1f)
  {
    if (speaker == null || Settings._VolumeSFX == 0) return;
    if (_PlayingAudio == null) _PlayingAudio = new Dictionary<AudioSource, System.Tuple<float, float>>();

    if (sfx_source == null)
    {
      return;
    }
    speaker.volume = sfx_source.volume;
    speaker.pitch = sfx_source.pitch;

    // Change pitch
    if (min != 1f || max != 1f) ChangePitch(ref speaker, min, max);

    // PlayOneShot
    PlayOneShot(speaker, sfx_source.clip);
  }

  public static void UpdateSFX(float pitch)
  {
    if (_PlayingAudio == null) return;
    foreach (var pair in _PlayingAudio)
    {
      // Check for null audio
      if (pair.Key == null) { _PlayingAudio.Remove(pair.Key); break; }

      // Get audio settings; pitch and volume
      var settings = pair.Value;

      // Remove audio that stopped playing; set defaults back
      if (!pair.Key.isPlaying)
      {
        pair.Key.pitch = settings.Item1;
        pair.Key.volume = settings.Item2;
        _PlayingAudio.Remove(pair.Key); break;
      }

      // Update pitch
      pair.Key.pitch = settings.Item1 * pitch;
      // Update volume
      pair.Key.volume = settings.Item2 * Settings._VolumeSFX / 5f;
    }
  }

  static System.Tuple<float, float> _Rain_Pair;
  public static void Reset()
  {
    if (_PlayingAudio == null) return;
    foreach (var pair in _PlayingAudio)
    {
      if (pair.Key == null) continue;

      if (pair.Key.clip != null && pair.Key.clip.name == "rain") { if (_Rain_Pair == null) _Rain_Pair = pair.Value; continue; }

      pair.Key.pitch = pair.Value.Item1;
      pair.Key.volume = pair.Value.Item2;
      pair.Key.Stop();
    }
    _PlayingAudio = null;
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

  public static IEnumerator MoveCoroutine(Transform transform, Vector3 start, Vector3 end, float time)
  {
    float t = 0f,
      interval = 0.015f;
    while (t < time)
    {
      transform.position = Vector3.Lerp(start, end, t / time);
      t += interval;
      yield return new WaitForSeconds(interval);
    }
    transform.position = end;
  }

  // Return a list a ragdolls if they are a distance (radius) from a point (distance)
  public static ActiveRagdoll[] CheckRadius(Vector3 position, float radius, bool raycast = false)
  {
    var returnList = new List<ActiveRagdoll>();
    foreach (var r in ActiveRagdoll._Ragdolls)
    {
      // Sanitize
      if (r._hip == null) continue;
      // Check radius
      if (MathC.Get2DDistance(position, r._hip.position) > radius) continue;
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
  public static void ApplyExplosionRadius(Vector3 position, float radius, ExplosiveScript.ExplosionType type, ActiveRagdoll source)
  {
    var rags = CheckRadius(position, radius);
    foreach (var r in rags)
      r.ToggleRaycasting(false);
    var exploded = 0;
    PlayComplexParticleSystemAt(ParticleSystemType.EXPLOSION, position + new Vector3(0f, 0.2f, 0f));
    foreach (var r in rags)
    {

      // Check for perk
      if (r._isPlayer && r._id == source._id && r._playerScript.HasPerk(Shop.Perk.PerkType.EXPLOSION_RESISTANCE)) continue;

      // Check dist
      if (MathC.Get2DDistance(position, r._hip.position) < 0.3f) { }

      // Raycast to validate
      else
      {
        r.ToggleRaycasting(true, true);
        var hit = new RaycastHit();
        var start_pos = new Vector3(position.x, -0.1f, position.z);
        var dir = -(r._dead ? start_pos - r._hip.position : MathC.Get2DVector(start_pos - r._hip.position)).normalized;
        if (!Physics.SphereCast(new Ray(start_pos, dir), 0.1f, out hit, radius + 5f, EnemyScript._Layermask_Ragdoll))
        {
          r.ToggleRaycasting(false, true);
          continue;
        }

        r.ToggleRaycasting(false, true);
        if (!r.IsSelf(hit.collider.gameObject)) continue;
      }

      if (!r._dead)
      {
        // Check for 3+ kills for slowmo
        if (exploded++ == 3)
          PlayerScript._SlowmoTimer += 2f;

#if UNITY_STANDALONE
        if (exploded == 5)
          SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DEMO_LEVEL_0);
        else if (exploded == 10)
          SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DEMO_LEVEL_1);
        else if (exploded == 25)
          SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DEMO_LEVEL_2);
#endif
      }

      // Explosion force type
      var explosionForce = Vector3.zero;
      if (type == ExplosiveScript.ExplosionType.UPWARD) explosionForce = new Vector3(0f, 3000f, 0f);
      else if (type == ExplosiveScript.ExplosionType.AWAY)
      {
        var away = -(position - r._hip.position).normalized;
        away.y = 0.2f;
        explosionForce = away * 3000f;
      }

      // Assign damage
      r.TakeDamage(
        new ActiveRagdoll.RagdollDamageSource()
        {
          Source = source,

          HitForce = explosionForce,

          Damage = 3,
          DamageSource = Vector3.zero,
          DamageSourceType = ActiveRagdoll.DamageSourceType.EXPLOSION,

          SpawnBlood = true,
          SpawnGiblets = true
        });
      if (r._dead)
      {
        r.DismemberRandomTimes(explosionForce, Random.Range(1, 5));
        r._hip.AddForce(explosionForce);
      }
    }

    foreach (var r in rags)
      if (!r._dead) r.ToggleRaycasting(true);
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

      var a = c.GetComponent<AudioSource>();
      FunctionsC.PlayAudioSource(ref a);
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
    public static int _CurrentTrack;
    public static string[] _TrackNames = new string[]
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
    static bool _Transitioning;

    public static AudioSource _TrackSource;
    public static int _TrackOffset = 3;

    // Create the AudioSource component and play the menu music
    public static void Init()
    {
      _TrackSource = GameScript._Singleton.gameObject.AddComponent<AudioSource>();
      _TrackSource.playOnAwake = false;
    }

    // Load an audio track async to prevent lag; returns a ResourceRequest that tells when the loading is complete
    static ResourceRequest LoadAudioClipAsync(int trackIndex)
    {
      return Resources.LoadAsync<AudioClip>("Music/" + _TrackNames[trackIndex]);
    }

    public static void PlayTrack(int trackIndex)
    {
      if (trackIndex > _TrackNames.Length - 1) throw new System.IndexOutOfRangeException($"Trying to access _TrackNames[{trackIndex}]");
      _Transitioning = true;
      // Stop track and unload resource
      if (_TrackSource.isPlaying)
        _TrackSource.Stop();
      UnloadCurrentTrack();
      IEnumerator LoadAsync()
      {
        ResourceRequest musicRequest = LoadAudioClipAsync(trackIndex);
        while (!musicRequest.isDone) yield return new WaitForSecondsRealtime(0.06f);
        _TrackSource.clip = musicRequest.asset as AudioClip;
        _TrackSource.Play();
        _CurrentTrack = trackIndex;
        _Transitioning = false;
      }
      GameScript._Singleton.StartCoroutine(LoadAsync());
    }

    // fade out current music and play a new track
    public static void TransitionTo(int trackIndex)
    {
      if (trackIndex > _TrackNames.Length - 1) throw new System.IndexOutOfRangeException($"Trying to access _TrackNames[{trackIndex}]");
      _Transitioning = true;
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
          _TrackSource.volume = t * (Settings._VolumeMusic / 5f);
        }
        UnloadCurrentTrack();

        // Wait for music to load
        while (!musicRequest.isDone) yield return new WaitForSecondsRealtime(0.05f);
        _TrackSource.Stop();

        // Play new song
        _TrackSource.clip = musicRequest.asset as AudioClip;
        _TrackSource.Play();
        _TrackSource.volume = (Settings._VolumeMusic / 5f);
        _CurrentTrack = trackIndex;
        _Transitioning = false;
      }
      GameScript._Singleton.StartCoroutine(TransitionToCo());
    }

    // Unload tracks not being played to reduce memory usage
    static void UnloadCurrentTrack()
    {
      Resources.UnloadAsset(_TrackSource.clip);
      _TrackSource.clip = null;
    }

    // Check if song ended, play next in queue
    public static void Update()
    {
      if (!_TrackSource.isPlaying && !_Transitioning)
      {
        // Check for menu music
        if (Menu2._InMenus)
        {
          if (_CurrentTrack < _TrackOffset)
          {
            // If unlocked difficulty 1, play faster track
            if (Settings._DifficultyUnlocked > 0)
            {
              if (_CurrentTrack != 1)
              {
                TransitionTo(1);
                return;
              }
            }
            // Else, play slower track
            else
            {
              if (_CurrentTrack != 0)
              {
                TransitionTo(0);
                return;
              }
            }
            _TrackSource.Play();
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
      if (Menu2._CurrentMenu._type == Menu2.MenuType.MAIN) return 0;

      // SURVIVAL mode music
      if (GameScript._GameMode == GameScript.GameModes.SURVIVAL)
      {
        if (_CurrentTrack == 0 || _CurrentTrack == 1 || _CurrentTrack == 2)
          _CurrentTrack = 3;
        else
          _CurrentTrack++;
        if (_CurrentTrack >= _TrackNames.Length)
          _CurrentTrack -= 3;
        return _CurrentTrack;
      }
      else
      {

        // Level pack music; random
        if (Levels._LevelPack_Playing || GameScript._EditorTesting)
        {

          var songs_length = _TrackNames.Length - 3;
          var song_index = Random.Range(0, songs_length);

          return song_index + 3;
        }

        // CLASSIC mode music
        var iter_base = Mathf.RoundToInt((Levels._CurrentLevelIndex) / ((float)_TrackLevelRange) / ((float)_TrackNames.Length / 3f - 1));
        iter_base *= 3;
        iter_base += _TrackOffset;
        var iter = 0;
        if (_CurrentTrack <= iter_base + 2 && _CurrentTrack >= iter_base)
          iter = ++_CurrentTrack;
        else
        {
          iter = iter_base + Random.Range(0, 3);
          if (iter == _CurrentTrack)
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

  public class SaveableStat
  {
    public void Reset()
    {

    }
  }

  // Basic int saveable information using PlayerPrefs
  public class SaveableStat_Int : SaveableStat
  {
    // PlayerPref name and initial value (default)
    public string _name;
    int _initialValue;

    // Variable get/set for PlayerPref access
    int value;
    public int _value
    {
      get { return value; }
      set
      {
        if (this.value == value) return;
        this.value = value;

        PlayerPrefs.SetInt(_name, value);
      }
    }

    // Reset saved information to default
    new public void Reset()
    {
      PlayerPrefs.SetInt(_name, _initialValue);
      value = _initialValue;
    }

    // Constructor
    public SaveableStat_Int(string name, int initialValue)
    {
      _name = name;
      _initialValue = initialValue;
      value = PlayerPrefs.GetInt(name, initialValue);
    }

    // Overload the ++ operator to allow interation
    public static SaveableStat_Int operator ++(SaveableStat_Int a)
    {
      a._value++;
      return a;
    }

    // Overload operators
    public static bool operator ==(SaveableStat_Int a, int b)
    {
      return a._value == b;
    }
    public static bool operator !=(SaveableStat_Int a, int b)
    {
      return !(a._value == b);
    }
    public override bool Equals(object obj)
    {
      return _value == ((SaveableStat_Int)obj)._value;
    }
    public override int GetHashCode()
    {
      return _value.GetHashCode();
    }
  }

  // Basic int saveable information using PlayerPrefs
  public class SaveableStat_Float : SaveableStat
  {
    // PlayerPref name and initial value (default)
    string _name;
    float _initialValue;

    // Variable get/set for PlayerPref access
    float value;
    public float _value
    {
      get { return value; }
      set
      {
        if (this.value == value) return;
        this.value = value;

        PlayerPrefs.SetFloat(_name, value);
      }
    }

    // Reset saved information to default
    new public void Reset()
    {
      PlayerPrefs.SetFloat(_name, _initialValue);
      value = _initialValue;
    }

    // Constructor
    public SaveableStat_Float(string name, float initialValue)
    {
      _name = name;
      _initialValue = initialValue;
      value = PlayerPrefs.GetFloat(name, initialValue);
    }

    // Overload the + operator to allow interation
    public static SaveableStat_Float operator +(SaveableStat_Float a, float b)
    {
      a._value += b;
      return a;
    }
  }

  // Basic bool saveable information using PlayerPrefs
  public class SaveableStat_Bool : SaveableStat
  {
    // PlayerPref name and initial value (default)
    public string _name;
    bool _initialValue;

    // Variable get/set for PlayerPref access
    bool value;
    public bool _value
    {
      get { return value; }
      set
      {
        if (this.value == value) return;
        this.value = value;

        PlayerPrefs.SetInt(_name, value ? 1 : 0);
      }
    }

    // Reset saved information to default
    new public void Reset()
    {
      PlayerPrefs.SetInt(_name, _initialValue ? 1 : 0);
      value = _initialValue;
    }

    // Constructor
    public SaveableStat_Bool(string name, bool initialValue)
    {
      _name = name;
      _initialValue = initialValue;
      value = PlayerPrefs.GetInt(name, initialValue ? 1 : 0) == 1;
    }

    // Overload operators
    public static bool operator ==(SaveableStat_Bool a, bool b)
    {
      return a._value == b;
    }
    public static bool operator !=(SaveableStat_Bool a, bool b)
    {
      return !(a._value == b);
    }
    public override bool Equals(object obj)
    {
      return _value == ((SaveableStat_Bool)obj)._value;
    }
    public override int GetHashCode()
    {
      return _value.GetHashCode();
    }

  }
}