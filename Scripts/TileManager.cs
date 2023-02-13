using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileManager
{
  public static List<Tile> _Tiles;
  static int _MouseID, _Width = 50, _Height = 50;
  static float _Tile_spacing = 2.5f;
  static Vector2 _offset;

  static Transform _Tile_begin, _Tile_end;
  public static Transform _Map, _Tile;
  public static Transform _Floor { get { return _Map.transform.GetChild(0); } }
  public static UnityEngine.AI.NavMeshSurface _navMeshSurface, _navMeshSurface2;
  static List<string> _LevelObjects;
  public static string _CurrentLevel_Name,
    _CurrentLevel_Loadout;
  public static int _CurrentLevel_Theme = -1;

  static Vector3 _saveTilePos;

  static System.Tuple<Material, Material> _Materials_Tiles;

  public static TextMesh _Text_LevelNum;

  public static void Init()
  {
    if (_Text_LevelNum == null)
      _Text_LevelNum = GameObject.Find("LevelNum").GetComponent<TextMesh>();

    if (_Tiles != null)
      foreach (Tile t in _Tiles)
        t._tile.transform.parent = _Map;
    GetBaseTile();
    _Tile.gameObject.SetActive(true);
    if (_saveTilePos == Vector3.zero) _saveTilePos = _Tile.position;
    _Tiles = new List<Tile>();
    _Tiles.Add(new Tile(_Tile.gameObject));
    _Tile.GetComponent<Collider>().enabled = true;
    // Check mats
    if (_Materials_Tiles == null)
    {
      _Materials_Tiles = new System.Tuple<Material, Material>(_Tiles[0]._tile.GetComponent<MeshRenderer>().sharedMaterial,
        new Material(_Tiles[0]._tile.GetComponent<MeshRenderer>().sharedMaterial));
      _Materials_Tiles.Item1.name = "TileUp";
      _Materials_Tiles.Item2.name = "TileDown";
    }
    if (_Ring == null) _Ring = GameObject.Find("ring").transform;
    if (_Tile == null || _Tile.gameObject == null) throw new System.NullReferenceException("_Tile is null!");
    // Check if map already loaded
    if (_Map.childCount > 3)
    {
      // Add all tiles
      int w2 = 0, h2 = 1;
      for (; w2 < _Width; w2++)
      {
        for (; h2 < _Height; h2++)
        {
          GameObject tile = _Map.GetChild(2 + h2 + w2 * _Height).gameObject;
          if (tile.GetInstanceID() == _Tile.gameObject.GetInstanceID()) continue;
          Tile t = new Tile(tile);
          _Tiles.Add(t);
          tile.GetComponent<Collider>().enabled = true;
        }
        h2 = 0;
      }
      // Re-order _Tiles based on positions
      //_Tiles.Sort();
      return;
    }
    // Add all tiles
    int w = 0, h = 1;
    for (; w < _Width; w++)
    {
      for (; h < _Height; h++)
      {
        Tile t = SpawnTile(w, h);
        _Tiles.Add(t);
      }
      h = 0;
    }
  }

  // Used to resize the map
  static void ResizeInit(int width, int height)
  {
    // Destroy all tiles but one
    if (_Tiles != null)
    {
      for (var i = _Map.childCount - 1; i > 2; i--)
        GameObject.DestroyImmediate(_Map.GetChild(i).gameObject);
      _Tiles = null;
    }
    if (_Tile != null)
    {
      _Tile.parent = _Map;
      _Tile.localPosition = new Vector3(0f, 0.9100001f, 0f);
    }
    _Width = width;
    _Height = height;
    // Normal init
    Init();
  }

  public static void Reset()
  {
    _Tiles = null;
    _Selected_Tiles = null;
  }

  public static void GetBaseTile()
  {
    float x = Mathf.Infinity, z = Mathf.Infinity;
    int leastIter = 2;
    for (int i = 2; i < _Map.childCount; i++)
    {
      Transform tile = _Map.GetChild(i);
      if (tile.position.x <= x && tile.position.z <= z)
      {
        x = tile.position.x;
        z = tile.position.z;
        leastIter = i;
      }
    }
    _Tile = _Map.GetChild(leastIter);
  }

  static void RemoveGameObjects()
  {
    GameScript.ResetObjects();
    CustomObstacle.Reset();
    GameScript game = GameObject.Find("Game").GetComponent<GameScript>();
    Transform enemies = game.transform.GetChild(0),
      objects = GameResources._Container_Objects,
      navmesh_mods = _Map.GetChild(1),
      items = game.transform.GetChild(1);
    for (int i = enemies.childCount - 1; i >= 0; i--)
      GameObject.Destroy(enemies.GetChild(i).gameObject);
    for (int i = objects.childCount - 1; i >= 0; i--)
      GameObject.Destroy(objects.GetChild(i).gameObject);
    for (int i = navmesh_mods.childCount - 1; i >= 0; i--)
      GameObject.Destroy(navmesh_mods.GetChild(i).gameObject);
    for (int i = items.childCount - 1; i >= 0; i--)
      GameObject.Destroy(items.GetChild(i).gameObject);
  }

  public static string SaveMap(bool saveToClipboard = true)
  {
    Init();
    var returnString = "";
    // Record min / max positions of tiles to calculate dimension
    float min_x = Mathf.Infinity, min_z = min_x, max_x = -min_x, max_z = max_x;
    foreach (var t in _Tiles)
    {
      if (!t._toggled) continue;
      var tile = t._tile.transform;
      if (tile.position.x < min_x) min_x = tile.position.x;
      if (tile.position.x > max_x) max_x = tile.position.x;
      if (tile.position.z < min_z) min_z = tile.position.z;
      if (tile.position.z > max_z) max_z = tile.position.z;
    }
    // Calculate dimensions of the map
    float dis_x = (max_x - min_x),
      dis_z = (max_z - min_z);
    int width = (int)(dis_x / _Tile_spacing) + 1,
      height = (int)(dis_z / _Tile_spacing) + 1;
    returnString = string.Format("{0} {1} ", width, height);

    // Save only tiles that are in the map range
    foreach (var t in _Tiles)
    {
      var tile_pos = t._tile.transform.position;
      if (tile_pos.x < min_x || tile_pos.x > max_x || tile_pos.z < min_z || tile_pos.z > max_z) continue;
      returnString += (t._toggled ? "0" : "1") + " ";
    }

    // Get offset of map to save objects properly
    var offset = new Vector3(min_x - _Tile.position.x, 0f, min_z - _Tile.position.z) + (_Tile.position - _saveTilePos);

    // Add PlayerSpawn information
    foreach (var p in PlayerspawnScript._PlayerSpawns)
    {
      var pos_use = p.transform.position - offset;
      returnString += "playerspawn_" + System.Math.Round(pos_use.x, 2) + "_" + System.Math.Round(pos_use.z, 2) + "_";
      // Add rotation
      returnString += "rot_" + p.transform.localRotation.eulerAngles.y + "_";
      var co = p.GetComponent<CustomObstacle>();
      if (co != null) returnString += $"co_{co._type}.{co._index}.{co._index2}";
      returnString += " ";
    }

    // Add enemy information
    if (!GameScript.IsSurvival())
    {
      var enemies = GameObject.Find("Game").transform.GetChild(0);
      for (int i = 0; i < enemies.childCount; i++)
      {
        var e = enemies.GetChild(i).GetChild(0).GetComponent<EnemyScript>();
        // Don't save bat enemy if not level editor
        /*if (GameScript.Settings._DIFFICULTY > 0)*/
        if (!GameScript._EditorTesting)
          if (e._itemLeft == GameScript.ItemManager.Items.BAT || e._itemRight == GameScript.ItemManager.Items.BAT) continue;
        // Basic position info
        var pos_save = e.transform.position - offset;
        returnString += "e_" + System.Math.Round(pos_save.x, 1) + "_" + System.Math.Round(pos_save.z, 1) + "_";
        // Item info
        string check_item(GameScript.ItemManager.Items item)
        {
          switch (item)
          {
            case (GameScript.ItemManager.Items.KNIFE):
              return "knife";
            case (GameScript.ItemManager.Items.PISTOL):
              return "pistol";
            case (GameScript.ItemManager.Items.PISTOL_SILENCED):
              return "pistolsilenced";
            case (GameScript.ItemManager.Items.REVOLVER):
              return "revolver";
            case (GameScript.ItemManager.Items.GRENADE_HOLD):
              return "grenade";
            case (GameScript.ItemManager.Items.SHOTGUN_PUMP):
              return "shotgun";
            case (GameScript.ItemManager.Items.ROCKET_LAUNCHER):
              return "rocketlauncher";
            case (GameScript.ItemManager.Items.BAT):
              return "bat";
          }
          //throw new System.Exception("No weapon set for " + item);
          Debug.LogWarning($"No weapon set for {item} - returning knife");
          return "knife";
        }
        if (e._itemLeft != GameScript.ItemManager.Items.NONE)
        {
          returnString += "li_";
          var addWeapon = check_item(e._itemLeft);
          returnString += addWeapon + "_";
        }
        if (e._itemRight != GameScript.ItemManager.Items.NONE)
        {
          returnString += "ri_";
          var addWeapon = check_item(e._itemRight);
          returnString += addWeapon + "_";
        }

        // Path information
        var path = e.transform.parent.GetChild(1).GetComponent<PathScript>();
        for (var u = 0; u < path.transform.childCount; u++)
        {
          var waypoint = path.transform.GetChild(u);
          var pos_use = waypoint.position - offset;
          returnString += "w_" + System.Math.Round(pos_use.x, 1) + "_" + System.Math.Round(pos_use.z, 1) + "_";
          for (int x = 0; x < waypoint.childCount; x++)
          {
            var lookPos = waypoint.GetChild(x);
            pos_use = lookPos.position - offset;
            returnString += "l_" + System.Math.Round(pos_use.x, 1) + "_" + System.Math.Round(pos_use.z, 1) + "_";
          }
        }

        // Move information
        returnString += "canmove_" + (e._canMove ? "true" : "false") + "_";
        returnString += "canhear_" + (e._reactToSound ? "true" : "false") + "_";
        returnString += " ";
      }
    }

    // Add object information
    var objects = new List<Transform>();
    foreach (var t in new Transform[] { GameResources._Container_Objects, _Map.GetChild(1) })
      for (var i = 0; i < t.childCount; i++)
        objects.Add(t.GetChild(i));
    for (var i = 0; i < objects.Count; i++)
    {
      var obj = objects[i];
      var pos_use = obj.position - offset;
      // Check each gameobject in Objects against level editor objects to see if it can be saved
      foreach (var leo in LevelEditorObject._Objects)
      {
        // Check if has a save function
        if (leo._saveFunction == null) continue;
        // Execute the save function to see if can be saved
        var s = leo._saveFunction(leo, obj.gameObject, offset, pos_use);
        if (s == null) continue;
        // If returned data, append to the final save data
        returnString += s;
      }
    }

    // Add meta if exists
    returnString = Levels.ParseLevelMeta(returnString, _CurrentLevel_Name != null && _CurrentLevel_Name.Trim().Length != 0 ? _CurrentLevel_Name : null, _CurrentLevel_Loadout, _CurrentLevel_Theme + "");

    //if (_CurrentLevel_Loadout != null && _CurrentLevel_Loadout.Trim().Length != 0)
    //  returnString += $"!{_CurrentLevel_Loadout.Trim()} ";

    // Trim
    returnString = returnString.Trim();

    // Log map data
    Debug.Log(returnString);

    // Save to clipboard
    if (saveToClipboard && Debug.isDebugBuild) ClipboardHelper.clipBoard = string.Format("\"{0}\",\n", returnString);

    // Return to caller
    return returnString;
  }

  public static bool _LoadingMap, _HasLocalLighting, _EnableEditorAfterLoad;
  static string _CurrentData;
  public static Coroutine LoadMap(string data, bool doubleSizeInit = false, bool appendToEditMaps = false)
  {
    _LoadingMap = true;
    _CurrentData = data;
    _LevelObjects = new List<string>();
    var game = GameObject.Find("Game").GetComponent<GameScript>();
    if (!GameResources._Loaded) GameResources.Init();
    return game.StartCoroutine(LoadMapCo(data, doubleSizeInit, appendToEditMaps));
  }

  static Vector2 _objectoffset = Vector2.zero, _last_objectoffset = Vector2.zero, _lastSpawnPos;
  static Vector3 _lastUsePos;
  static IEnumerator LoadMapCo(string data, bool doubleSizeInit = false, bool appendToEditMaps = false)
  {

    //Debug.Log("Loading map with data:\n== BEGIN ==\n" + data + "\n== END ==");

    var level_meta = Levels.GetLevelMeta(data);

    // Check for editor maps
    _CurrentLevel_Name = level_meta[1] == null ? "unnamed loaded map" : level_meta[1];
    _CurrentLevel_Loadout = level_meta[2];
    _CurrentLevel_Theme = level_meta[3] != null ? int.Parse(level_meta[3]) : -1;

    if (_CurrentLevel_Loadout != null)
    {
      Levels._HardcodedLoadout = new GameScript.ItemManager.Loadout()
      {
        _equipment = new GameScript.PlayerProfile.Equipment()
      };
      Levels.GetHardcodedLoadout(_CurrentLevel_Loadout);
    }
    else
      Levels._HardcodedLoadout = null;

    // Split data by space
    var data_split = level_meta[0].Split(' ');
    var data_iter = 0;
    // Load map
    int width = 0,
      height = 0;
    try
    {
      width = int.Parse(data_split[data_iter++]);
      height = int.Parse(data_split[data_iter++]);
    }
    // If fails to load map, just load the first level
    catch (System.FormatException e)
    {
      Menu2.QuickEnableMenus();

      GameScript._Coroutine_load = null;
      _LoadingMap = false;

      throw new System.FormatException("Cannot load level with data: " + data + "\n" + e.StackTrace);
    }
    // Lerp camera
    var time = 1f;
    var main_camera = GameResources._Camera_Main;
    var start_clip = 15.5f;
    while (time > 0f)
    {
      main_camera.farClipPlane = Mathf.Lerp(10f, start_clip, time);
      yield return new WaitForSecondsRealtime(0.005f);
      time -= 0.06f;
    }
    main_camera.farClipPlane = 10f;
    // Show unlocks
    if (!Menu2._InMenus && Shop._UnlockString != string.Empty)
      GameScript.TogglePause(Menu2.MenuType.NONE);

    // Remove prior enemies / objects
    RemoveGameObjects();
    ResetParticles();

    // Set tilemap size
    ResizeInit((int)((width + 2) * (doubleSizeInit ? 1.4f : 1f)), (int)((height + 2) * (doubleSizeInit ? 1.4f : 1f)));

    // Remove stuff
    ActiveRagdoll.Reset();
    EnemyScript.Reset();
    Powerup.Reset();
    CustomEntityUI._ID = 0;
    EnemyScript._ID = 0;

    // Set theme
    if (Levels._LevelPack_Playing)
    {
      if (_CurrentLevel_Theme != -1)
        SceneThemes.ChangeMapTheme(SceneThemes._SceneOrder_LevelEditor[_CurrentLevel_Theme % SceneThemes._SceneOrder_LevelEditor.Length]);
      else
        SceneThemes.ChangeMapTheme(SceneThemes._Instance._ThemeOrder[3]);
    }
    else if (GameScript.IsSurvival())
      SceneThemes.ChangeMapTheme(SceneThemes._Instance._ThemeOrder[3]);
    else if (GameScript._EditorTesting)
      SceneThemes.ChangeMapTheme(SceneThemes._Instance._ThemeOrder[3]);
    else
    {
      if (Levels._CurrentLevelIndex < 12 && Levels._CurrentLevelIndex > (GameScript.Settings._DIFFICULTY == 1 ? 8 : 7))
        SceneThemes.ChangeMapTheme(SceneThemes._Instance._ThemeOrder[1]);
      else
        SceneThemes.ChangeMapTheme(SceneThemes._Instance._ThemeOrder[(Levels._CurrentLevelIndex / 12) % 12]);
    }
    data_iter = width * height + 2;

    // Find the desired offset of the map based off of the player spawn point and the desiredStartPos
    string spawnpoint_data = null;
    _HasLocalLighting = false;
    bool endofobjects = false;
    for (; data_iter < data_split.Length;)
    {

      for (int i = 0; i < 30 && data_iter < data_split.Length; i++)
      {
        // Check if data is valid
        var data0 = data_split[data_iter++].Trim();
        if (data0.Length <= 1) continue;

        // Check for name of map
        if (data0.Contains("+"))
        {
          endofobjects = true;
          break;
        }

        // Add to level data for reloading
        _LevelObjects.Add(data0);
        // Check if data is the spawnpoint data; save it for later
        if (data0.Contains("playerspawn_"))
          spawnpoint_data = data0;
        else if (data0.Contains("candel"))
          _HasLocalLighting = true;
      }

      if (endofobjects)
        break;
    }
    /*/ Turn on light if has no local lighting
    if(!hasLocalLighting && GameScript._CameraLight.intensity == 0f)
      GameScript.ToggleCameraLight(true);
    else if(hasLocalLighting && GameScript._CameraLight.intensity != 0f)
      GameScript.ToggleCameraLight(false);    */

    Vector2 offset = new Vector2(0, 0),
      difference = Vector2.zero;
    if (spawnpoint_data != null)
    {
      // Find the local start position of the new map
      string[] splitData = spawnpoint_data.Split('_');
      Vector2 spawnpos_tilepos = TransformPositionOntoTiles(float.Parse(splitData[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(splitData[2], System.Globalization.CultureInfo.InvariantCulture));
      // Check for null players
      if (PlayerScript._Players != null)
        for (int i = PlayerScript._Players.Count - 1; i >= 0; i--)
          if (PlayerScript._Players[i] == null) PlayerScript._Players.Remove(PlayerScript._Players[i]);
      // Find the local end position of the new map
      Vector3 usePos = Vector3.zero;
      if (PlayerScript._Players != null && PlayerScript._Players.Count > 0)
      {
        foreach (PlayerScript p in PlayerScript._Players)
        {
          if (p._ragdoll._dead) continue;
          usePos = p.transform.position;
          _lastUsePos = usePos;
        }
      }
      if (usePos.Equals(Vector3.zero))
      {
        if (_lastUsePos.Equals(Vector3.zero))
          _lastUsePos = new Vector3(_lastSpawnPos.x, 0f, _lastSpawnPos.y);
        usePos = _lastUsePos;
      }
      _lastSpawnPos = spawnpos_tilepos;
      Vector2 endpoint_tilepos = TransformPositionOntoTiles(usePos.x, usePos.z);
      endpoint_tilepos = new Vector2(Mathf.Abs(endpoint_tilepos.x), Mathf.Abs(endpoint_tilepos.y));

      Vector2 newStartPos = (endpoint_tilepos + new Vector2(1, 1));
      difference = newStartPos - new Vector2(_Width / 2, _Height / 2);

      // Offset the map so the start position is always in the center
      //if (difference != Vector2.zero)
      //  yield return OffsetMap(difference);
      _last_objectoffset = _objectoffset;
    }

    // Clean up old meshes
    string[] names = { "Meshes_Tiles_Up", "Meshes_Tiles_Down", "Meshes_Tiles_Sides" };
    foreach (var name in names)
    {
      GameObject old = GameObject.Find(name);
      if (old != null) GameObject.Destroy(old);
    }

    // Get List of tiles to toggle
    List<Tile> tiles_down = new List<Tile>(),
      tiles_up = new List<Tile>();
    data_iter = 2;
    _offset = new Vector2(1, 1);

    // Color tiles
    _Materials_Tiles.Item1.color = SceneThemes._Theme._tileColorUp;
    _Materials_Tiles.Item2.color = SceneThemes._Theme._tileColorDown;

    // Holds offsets for checking i a 3x3 around a tile
    Vector2Int[] tile_offsets = new Vector2Int[]
    {
      new Vector2Int(0, 1),
      new Vector2Int(0, -1),
      new Vector2Int(-1, 0),
      new Vector2Int(1, 0),
      new Vector2Int(1, 1),
      new Vector2Int(-1, -1),
      new Vector2Int(-1, 1),
      new Vector2Int(1, -1),
    };
    // Holds positions for down and up
    Vector3 pos_up = new Vector3(0f, Tile._StartY + Tile._AddY, 0f),
     pos_down = new Vector3(0f, Tile._StartY, 0f);
    // Check if
    var waititer = 0;
    for (var w = 0; w < _Width; w++)
    {
      for (var h = 0; h < _Height; h++)
      {
        var useData = 1;
        if (w < _offset.x || w >= width + _offset.x || h < _offset.y || h >= height + _offset.y) { }
        else
          useData = int.Parse(data_split[data_iter++]);
        var tile = _Tiles[w * _Height + h];
        // Down
        if (useData == 0)
        {
          var r = tile._tile.GetComponent<MeshRenderer>();
          r.sharedMaterial = _Materials_Tiles.Item2;
          tile._tile.transform.localPosition = new Vector3(tile._tile.transform.localPosition.x, pos_down.y, tile._tile.transform.localPosition.z);
          tiles_down.Add(tile);
        }
        // Up
        else
        {
          var r = tile._tile.GetComponent<MeshRenderer>();
          r.sharedMaterial = _Materials_Tiles.Item1;
          tile._tile.transform.localPosition = new Vector3(tile._tile.transform.localPosition.x, pos_up.y, tile._tile.transform.localPosition.z);
          tiles_up.Add(tile);
        }
      }
      if (waititer++ % 20 == 0) yield return new WaitForSeconds(0.01f);
    }
    while (SceneThemes._ChangingTheme) yield return new WaitForSeconds(0.1f);
    yield return new WaitForSeconds(0.1f);
    // Set inner walls and tile visibility
    foreach (var tile in tiles_up)
    {
      // Gather tiles around tile; see if it is near a down tile
      var next_to_tiles = new List<Vector2Int>();
      foreach (var pos in tile_offsets)
      {
        var tile_next = Tile.GetTile((int)tile._pos.x + pos.x, (int)tile._pos.y + pos.y);
        if (tile_next == null) continue;

        if (tiles_down.Contains(tile_next))
          next_to_tiles.Add(pos);
      }

      // Check visibility
      if (next_to_tiles.Count == 0)
        tile._tile.gameObject.SetActive(false);
      // Check inner walls
      else
      {
        for (int i = 0; i < 4; i++)
          tile._tile.transform.GetChild(0).GetChild(i).gameObject.SetActive(next_to_tiles.Contains(tile_offsets[i]));
      }
    }

    // Load objects
    for (var i = 0; i < _LevelObjects.Count;)
    {
      for (var u = 0; i < _LevelObjects.Count && u < 15; u++)
        if (LoadObject(_LevelObjects[i++]) == null)
        {
          GameScript._Coroutine_load = null;
          _LoadingMap = false;

          throw new System.NullReferenceException("Error loading object: " + _LevelObjects[i - 1]);
        }
      yield return new WaitForSeconds(0.01f);
    }

    // Make sure ragdolls are not null
    if (ActiveRagdoll._Ragdolls != null)
      for (var i = ActiveRagdoll._Ragdolls.Count - 1; i >= 0; i--)
      {
        var r = ActiveRagdoll._Ragdolls[i];
        if (r == null || r._hip == null)
        {
          if (r._controller != null) GameObject.Destroy(r._controller);
          ActiveRagdoll._Ragdolls.Remove(r);
        }
      }

    // Set floor pos to center of map
    var floorPos = _Floor.position;
    var center = Tile.GetTile((int)(_Width * 0.4f), (int)(_Height * 0.4f))._tile.transform.position;
    floorPos.x = center.x;
    floorPos.z = center.z;
    _Floor.position = floorPos;
    _Floor.localScale = new Vector3(_Width * 0.5f, 1f, _Height * 0.5f);

    // Set camera pos to playerspawn
    var campos = GameResources._Camera_Main.transform.position;
    var playerspawnpos = PlayerspawnScript._PlayerSpawns[0].transform.position;
    campos.x = playerspawnpos.x;
    campos.z = playerspawnpos.z + 3.6f;
    GameResources._Camera_Main.transform.position = campos;

    // Hide playerspawn
    if (GameScript._GameMode == GameScript.GameModes.SURVIVAL && !GameScript._EditorEnabled)
      PlayerspawnScript._PlayerSpawns[0].transform.GetChild(1).gameObject.SetActive(false);
    else
      PlayerspawnScript._PlayerSpawns[0].transform.GetChild(1).gameObject.SetActive(true);

    // Bake navmesh
    _navMeshSurface.BuildNavMesh();
    if (GameScript.IsSurvival())
      _navMeshSurface2.BuildNavMesh();

    // Combine meshes
    foreach (var t in tiles_down)
      t._tile.transform.parent = _Map;
    CombineMeshes(true);

    // Set level time
    GameScript._LevelStartTime = Time.time;

    // Lerp cam distance
    GameScript._Singleton.StartCoroutine(LerpCam());

    // Init all enemies
    yield return EnemyScript.HardInitAllCo();

    // Spawn player
    if (!Menu2._InMenus)
      GameScript.SpawnPlayers();

    // Refill player ammo
    if (PlayerScript._Players != null)
      foreach (PlayerScript p in PlayerScript._Players) p._ragdoll.RefillAmmo();
    OnMapLoad();

    // Append to maps
    if (appendToEditMaps)
      Menu2.AppendToEditMaps($"{level_meta[0]} +{_CurrentLevel_Name}" + (level_meta[2] == null ? "" : $"!{level_meta[2]}") + (level_meta[3] == null ? "" : $"*{level_meta[3]}"));

    GameScript._Coroutine_load = null;
    _LoadingMap = false;
  }

  static IEnumerator LerpCam()
  {
    // Lerp camera
    var time = 1f;
    var main_camera = GameResources._Camera_Main;
    while (time > 0f)
    {
      main_camera.farClipPlane = Mathf.Lerp(35f, 10f, time / 1f);
      yield return new WaitForSecondsRealtime(0.005f);
      time -= 0.02f;
    }
    main_camera.farClipPlane = 35f;
  }

  static void CombineMeshes(bool combine_tiles)
  {
    var objects = _Map.GetChild(1);

    // Gather objects to combine
    var meshes = new Dictionary<string, System.Tuple<List<GameObject>, bool>>();
    meshes.Add("Barrels", System.Tuple.Create(new List<GameObject>(), false));
    meshes.Add("Books", System.Tuple.Create(new List<GameObject>(), true));
    meshes.Add("Bookcases", System.Tuple.Create(new List<GameObject>(), true));
    meshes.Add("Tables", System.Tuple.Create(new List<GameObject>(), false));
    meshes.Add("Chairs", System.Tuple.Create(new List<GameObject>(), false));
    meshes.Add("Arches", System.Tuple.Create(new List<GameObject>(), false));

    meshes.Add("Bushes", System.Tuple.Create(new List<GameObject>(), false));
    meshes.Add("Rocks", System.Tuple.Create(new List<GameObject>(), false));

    meshes.Add("Walls", System.Tuple.Create(new List<GameObject>(), false));
    meshes.Add("Outers", System.Tuple.Create(new List<GameObject>(), true));
    meshes.Add("NavmeshBarriers", System.Tuple.Create(new List<GameObject>(), true));

    for (var i = 0; i < objects.childCount; i++)
    {
      var obj = objects.GetChild(i);
      switch (obj.name)
      {
        case ("Barrel"):
          meshes["Barrels"].Item1.Add(obj.GetChild(0).gameObject);
          break;
        case ("BookcaseOpen"):
          meshes["Bookcases"].Item1.Add(obj.GetChild(0).GetChild(0).gameObject);
          for (var u = 0; u < 3; u++)
            meshes["Books"].Item1.Add(obj.GetChild(u + 1).GetChild(0).gameObject);
          break;
        case ("BookcaseBig"):
          meshes["Bookcases"].Item1.Add(obj.GetChild(0).GetChild(0).gameObject);
          for (var u = 0; u < 3; u++)
            meshes["Books"].Item1.Add(obj.GetChild(u + 1).GetChild(0).gameObject);
          break;
        case ("Table"):
          meshes["Tables"].Item1.Add(obj.GetChild(0).gameObject);
          break;
        case ("TableSmall"):
          meshes["Tables"].Item1.Add(obj.GetChild(0).gameObject);
          break;
        case ("Chair"):
          meshes["Chairs"].Item1.Add(obj.GetChild(0).gameObject);
          break;
        case ("Arch"):
          meshes["Arches"].Item1.Add(obj.GetChild(0).gameObject);
          break;
        case ("NavMeshBarrier"):
          var t = obj.GetChild(0);
          for (var u = 0; u < t.childCount; u++)
            meshes["NavmeshBarriers"].Item1.Add(t.GetChild(u).gameObject);
          break;
        case ("TileWall"):
          meshes["Walls"].Item1.Add(obj.GetChild(0).gameObject);
          t = obj.GetChild(1);
          for (int u = 0; u < t.childCount; u++)
            meshes["Outers"].Item1.Add(t.GetChild(u).gameObject);
          break;

        // Forest theme
        case ("Barrel_Rock"):
          meshes["Rocks"].Item1.Add(obj.GetChild(0).gameObject);
          break;

        case ("BookcaseOpen_Bush"):
          meshes["Bushes"].Item1.Add(obj.GetChild(0).gameObject);
          break;
        case ("BookcaseBig_Bush"):
          meshes["Bushes"].Item1.Add(obj.GetChild(0).gameObject);
          break;
        case ("Table_Bush"):
          meshes["Bushes"].Item1.Add(obj.GetChild(0).gameObject);
          break;
      }
    }
    // Combine objects
    GameObject CombineAndPlace(string name, GameObject[] gameObjects)
    {
      var combine = FunctionsC.CombineMeshes(name, gameObjects);
      if (combine == null) return null;
      combine.name = name;
      combine.transform.parent = objects;
      return combine;
    }
    foreach (var key in meshes.Keys)
    {
      var pair = meshes[key];
      // Combine meshes
      var list = pair.Item1;
      var g = CombineAndPlace($"Meshes_{key}", list.ToArray());
      if (g != null && key == "NavmeshBarriers") g.GetComponent<MeshRenderer>().enabled = false;
      // Clean up
      var delete_self = !pair.Item2;
      for (var i = list.Count - 1; i >= 0; i--)
        GameObject.Destroy(delete_self ? list[i] : list[i].transform.parent.gameObject);
    }
    // Handle tiles
    if (combine_tiles)
    {
      // Clean up old
      string[] names = { "Meshes_Tiles_Up", "Meshes_Tiles_Down", "Meshes_Tiles_Sides" };
      foreach (var name in names)
      {
        var old = GameObject.Find(name);
        if (old != null) GameObject.Destroy(old);
      }
      // Gather tiles
      var map = GameObject.Find("Map").transform;
      List<GameObject> tiles_up = new List<GameObject>(),
        tiles_down = new List<GameObject>(),
        tile_sides = new List<GameObject>();
      for (var i = 2; i < map.childCount; i++)
      {
        var tile = map.GetChild(i);
        if (!tile.gameObject.activeSelf) continue;
        if (tile.GetComponent<MeshRenderer>().sharedMaterial.name.Equals("TileUp"))
          tiles_up.Add(tile.gameObject);
        else
          tiles_down.Add(tile.gameObject);
        for (var u = 0; u < 4; u++)
        {
          var side = tile.GetChild(0).GetChild(u);
          if (side.gameObject.activeSelf)
          {
            tile_sides.Add(side.gameObject);
            side.gameObject.SetActive(false);
          }
        }
      }
      // Combine
      GameObject combine_tiles_up = FunctionsC.CombineMeshes(names[0], tiles_up.ToArray()),
        combine_tiles_down = FunctionsC.CombineMeshes(names[1], tiles_down.ToArray()),
        combine_tile_sides = FunctionsC.CombineMeshes(names[2], tile_sides.ToArray(), false);
      combine_tiles_up.name = names[0];
      combine_tiles_down.name = names[1];
      if (combine_tile_sides != null)
        combine_tile_sides.name = names[2];
    }
  }

  static void OnMapLoad()
  {
    _Text_LevelNum.text = "";

    // Check survival
    if (GameScript.IsSurvival()) GameScript.SurvivalMode.Init();

    // Check if there is bat enemy to spawn
    if (!GameScript._EditorTesting)
    {
      var hasBat = false;
      if (EnemyScript._Enemies_alive != null)
        foreach (var e in EnemyScript._Enemies_alive)
          if (e._itemLeft == GameScript.ItemManager.Items.BAT || e._itemRight == GameScript.ItemManager.Items.BAT)
          {
            hasBat = true;
            break;
          }

      // If difficulty is 0, never check for bat person
      var chaser_extra = GameScript.Settings._Extra_RemoveBatGuy._value;
      if (GameScript.Settings._DIFFICULTY == 0 && chaser_extra != 1)
        hasBat = true;
      if (!hasBat && chaser_extra != 2)
      {
        var e = EnemyScript.LoadEnemy(PlayerspawnScript._PlayerSpawns[0].transform.position - PlayerspawnScript._PlayerSpawns[0].transform.forward);
        e._itemLeft = GameScript.ItemManager.Items.BAT;
        e.Init(null);
        e.EquipStart();
      }
    }

    // Display level #
    if (GameScript._GameMode == GameScript.GameModes.CLASSIC && !GameScript._EditorTesting)
    {
      var hardmodeadd = GameScript.Settings._DIFFICULTY == 1 ? "*" : "";
      _Text_LevelNum.text = $"{Levels._CurrentLevelIndex + 1}{hardmodeadd}";
      if (Menu2._InMenus)
      {
        _Text_LevelNum.gameObject.SetActive(false);
      }
    }

    // Jobs
    ActiveRagdoll.Jobs_Init();
  }

  public static Vector2 TransformPositionOntoTiles(float x, float z)
  {
    Vector3 endpoint_pos = new Vector3(x, 0f, z),
      distance = endpoint_pos - _Tile.position;
    float width = _Tile.lossyScale.x;
    return new Vector2((int)Mathf.Floor(distance.x / _Tile_spacing + width / 1.5f), (int)Mathf.Floor(distance.z / _Tile_spacing + width / 1.5f));
  }

  public static GameObject LoadObject(string object_data)
  {
    // Load enemy / object data
    if (object_data.Trim().Equals("")) return null;
    var object_data_split = object_data.Split('_');
    var object_data_iter = 0;
    // Get universal object data
    if (object_data_split.Length < 3) return null;
    var object_base = new ObjectData(object_data_split[object_data_iter++], object_data_split[object_data_iter++], object_data_split[object_data_iter++]);
    Transform container_enemies = GameObject.Find("Game").transform.GetChild(0),
      container_objects = GameResources._Container_Objects;
    GameObject loadedObject = null;

    //Debug.Log($"= Loading object data => [{object_data}]\n=Parsed name: [{object_base._type}], pos: [{object_base._x}, {object_base._z}]");

    // Load specific object data
    switch (object_base._type)
    {
      // Load enemy
      case ("e"):
        loadedObject = object_base.LoadResource("Enemy", container_enemies, new Vector3(0.1f, 0.1f, 1f), -1.32f);
        Transform controller = loadedObject.transform.GetChild(0);
        controller.position = new Vector3(object_base._x, controller.position.y, object_base._z);
        EnemyScript script = controller.GetComponent<EnemyScript>();
        script._itemLeft = script._itemRight = GameScript.ItemManager.Items.NONE;
        // Load path info
        Transform path = loadedObject.transform.GetChild(1),
          default_waypoint = path.GetChild(0),
          default_lookpoint = default_waypoint.GetChild(0);
        default_lookpoint.parent = default_waypoint.parent;
        Transform current_waypoint = null;
        for (; object_data_iter < object_data_split.Length;)
        {
          string object_type = object_data_split[object_data_iter++];
          if (object_type.Equals("")) continue;
          ObjectData subobject_base;
          if (object_type.Equals("w") || object_type.Equals("l"))
            subobject_base = new ObjectData(object_type, object_data_split[object_data_iter++], object_data_split[object_data_iter++]);
          else
            subobject_base = new ObjectData(object_type, object_data_split[object_data_iter++]);
          // Check for (way/look)points
          switch (object_type)
          {
            // Waypoint
            case ("w"):
              current_waypoint = GameObject.Instantiate(default_waypoint.gameObject as GameObject).transform;
              current_waypoint.name = "Waypoint";
              current_waypoint.parent = default_waypoint.parent;
              current_waypoint.localScale = new Vector3(0.3f, 0.3f, 0.2f);
              Quaternion rotation = current_waypoint.localRotation;
              rotation.eulerAngles = new Vector3(0f, 0f, 90f);
              current_waypoint.localRotation = rotation;
              current_waypoint.position = new Vector3(subobject_base._x, current_waypoint.position.y, subobject_base._z);
              current_waypoint.localPosition = new Vector3(current_waypoint.localPosition.x, current_waypoint.localPosition.y, 0f);
              break;
            // Lookpoint
            case ("l"):
              Transform lookpoint = GameObject.Instantiate(default_lookpoint.gameObject as GameObject).transform;
              lookpoint.name = "Lookpoint";
              lookpoint.parent = current_waypoint;
              lookpoint.localScale = new Vector3(0.5f, 0.5f, 0.5f);
              rotation = lookpoint.localRotation;
              rotation.eulerAngles = new Vector3(0f, 0f, 0f);
              lookpoint.localRotation = rotation;
              lookpoint.position = new Vector3(subobject_base._x, lookpoint.position.y, subobject_base._z);
              lookpoint.localPosition = new Vector3(lookpoint.localPosition.x, lookpoint.localPosition.y, 0f);
              break;
            // Left / right item
            case ("li"):
            case ("ri"):
              GameScript.ItemManager.Items item;
              switch (subobject_base._modifier0)
              {
                case ("pistol"):
                  item = GameScript.ItemManager.Items.PISTOL;
                  break;
                case ("pistolsilenced"):
                  item = GameScript.ItemManager.Items.PISTOL_SILENCED;
                  break;
                case ("revolver"):
                  item = GameScript.ItemManager.Items.REVOLVER;
                  break;
                case ("grenade"):
                  item = GameScript.ItemManager.Items.GRENADE_HOLD;
                  break;
                case ("shotgun"):
                  item = GameScript.ItemManager.Items.SHOTGUN_PUMP;
                  break;
                case ("rocketlauncher"):
                  item = GameScript.ItemManager.Items.ROCKET_LAUNCHER;
                  break;
                case ("bat"):
                  item = GameScript.ItemManager.Items.BAT;
                  break;
                default:
                  item = GameScript.ItemManager.Items.KNIFE;
                  break;
              }
              if (object_type.Equals("ri"))
                script._itemRight = item;
              else script._itemLeft = item;
              break;
            // Move mode; if difficulty 2; always can move
            case ("canmove"):
              if (GameScript.Settings._DIFFICULTY > 1)
                script._canMove = true;
              else
                script._canMove = (subobject_base._modifier0.Equals("true") ? true : false);
              break;
            // Hear mode
            case ("canhear"):
              script._reactToSound = (subobject_base._modifier0.Equals("true") ? true : false);
              break;
            // Default
            default:
              Debug.Log("Unhandled data " + subobject_base._type + " : " + subobject_base._modifier0);
              break;
          }
        }
        // Check if no waypoints found
        if (default_waypoint.parent.childCount == 2)
        {
          default_lookpoint.parent = default_waypoint;
        }
        else
        {
          GameObject.DestroyImmediate(default_waypoint.gameObject);
          GameObject.DestroyImmediate(default_lookpoint.gameObject);
        }
        break;
      // Load powerup
      case ("p"):
        // Check survival mode
        if (GameScript.IsSurvival())
        {
          break;
        }
        // Base mode
        loadedObject = object_base.LoadResource("Powerup", container_objects, -0.86f);
        var powerup_script = loadedObject.GetComponent<Powerup>();
        string type = object_data_split[object_data_iter];
        if (type.Equals("end")) powerup_script._type = Powerup.PowerupType.END;
        else Debug.LogError("Unhandled powerup type " + type);
        // Load connected UI
        CustomEntityUI ui_script = loadedObject.GetComponent<CustomEntityUI>();
        while (object_data_split.Length - 1 > object_data_iter)
          switch (object_data_split[++object_data_iter])
          {
            // Check for doors
            case ("door"):

              var loadstring = string.Format("{0}_{1}_{2}_rot_{3}",
                object_data_split[object_data_iter++],
                object_data_split[object_data_iter++],
                object_data_split[object_data_iter++],
                object_data_split[++object_data_iter]
                );
              if (object_data_split.Length - 1 > ++object_data_iter)
              {

                if (object_data_split[object_data_iter] == "enemypos")
                {
                  loadstring += $"_enemypos_{object_data_split[++object_data_iter]}";
                  object_data_iter++;
                }
                if (object_data_split[object_data_iter] == "open")
                {
                  loadstring += $"_open_{object_data_split[++object_data_iter]}";
                  object_data_iter++;
                }

              }

              GameObject door_new = LoadObject(loadstring);
              object_data_iter++;
              DoorScript door_script = door_new.GetComponent<DoorScript>();
              door_script.RegisterButton(ref ui_script);
              break;
          }
        powerup_script.Init();
        break;
      // Load button
      case ("button"):
        loadedObject = object_base.LoadResource("Button", container_objects, new Vector3(0.6f, 0.1f, 0.6f), -1.28f);
        CustomEntityUI button_script = loadedObject.GetComponent<CustomEntityUI>();
        button_script.gameObject.layer = GameScript._EditorEnabled ? 0 : 2;
        // Check for attached objects
        while (object_data_iter < object_data_split.Length)
          // Check object type
          switch (object_data_split[object_data_iter])
          {
            // Check for doors
            case ("door"):
              GameObject door_new = LoadObject(string.Format("{0}_{1}_{2}_rot_{3}_open_{4}", object_data_split[object_data_iter++], object_data_split[object_data_iter++], object_data_split[object_data_iter++], object_data_split[++object_data_iter], object_data_split[++object_data_iter + 1]));
              object_data_iter++;
              object_data_iter++;
              DoorScript door_script = door_new.GetComponent<DoorScript>();
              door_script.RegisterButton(ref button_script);
              door_script.transform.GetChild(0).GetChild(1).GetComponent<BoxCollider>().enabled = GameScript._EditorEnabled;
              break;
          }
        break;
      // Check for doors
      case ("door"):
        loadedObject = object_base.LoadResource("Door", container_objects, 0.15f);
        DoorScript door_script0 = loadedObject.GetComponent<DoorScript>();
        door_script0.enabled = false;
        door_script0.enabled = true;
        door_script0.transform.GetChild(0).GetChild(1).GetComponent<BoxCollider>().enabled = GameScript._EditorEnabled;
        var properties = GetProperties(ref object_data_split, ref object_data_iter);
        foreach (var pair in properties)
        {
          switch (pair.Key)
          {
            // Rotation
            case ("rot"):
              Quaternion rot = loadedObject.transform.localRotation;
              rot.eulerAngles = new Vector3(rot.eulerAngles.x, float.Parse(pair.Value, System.Globalization.CultureInfo.InvariantCulture), rot.eulerAngles.z);
              loadedObject.transform.rotation = rot;
              break;
            // Set the open status
            case ("open"):
              int open = int.Parse(pair.Value);
              door_script0._opened = open == 1;
              door_script0.enabled = true;
              break;
            case ("enemypos"):
              if (pair.Value.Trim() == "") break;
              foreach (var id in pair.Value.Split('|'))
              {
                if (id.Trim() == "") continue;
                var idP = int.Parse(id);
                door_script0.RegisterEnemy(idP);
                door_script0.RegisterEnemyReal(GameScript._Singleton.transform.GetChild(0).GetChild(idP).GetChild(0).GetComponent<EnemyScript>());
              }
              break;
            default:
              Debug.LogError(string.Format("LoadObject() => Unhandled object: ( {0}: {1} )", pair.Key, pair.Value));
              break;
          }
        }
        break;
      // Check for laser
      case ("laser"):
        loadedObject = object_base.LoadResource("Laser", container_objects);
        // Get properties
        {
          LaserScript laser_script = loadedObject.GetComponent<LaserScript>();
          properties = GetProperties(ref object_data_split, ref object_data_iter);
          foreach (var pair in properties)
          {
            switch (pair.Key)
            {
              // Rotation
              case ("rot"):
                FunctionsC.RotateLocal(ref loadedObject, float.Parse(pair.Value, System.Globalization.CultureInfo.InvariantCulture));
                break;
              // Set the rotation speed
              case ("rotspeed"):
                laser_script._rotationSpeed = float.Parse(pair.Value, System.Globalization.CultureInfo.InvariantCulture);
                break;
              // Set the type
              case ("type"):
                laser_script._type = pair.Value.Equals("alarm") ? LaserScript.LaserType.ALARM : LaserScript.LaserType.KILL;
                break;
              default:
                Debug.LogError(string.Format("LoadObject() => Unhandled object: ( {0}: {1} )", pair.Key, pair.Value));
                break;
            }
          }
          laser_script.Init();
        }
        break;
      // Check for explosive barrels
      case ("expbarrel"):
        loadedObject = object_base.LoadResource("ExplosiveBarrel", container_objects);
        loadedObject.transform.localPosition = new Vector3(loadedObject.transform.localPosition.x, _LEO_ExplosiveBarrel._movementSettings._localPos, loadedObject.transform.localPosition.z);
        break;
      // Check for player spawn
      case ("playerspawn"):
        loadedObject = PlayerspawnScript._PlayerSpawns[0].gameObject;
        loadedObject.transform.position = new Vector3(object_base._x, loadedObject.transform.position.y, object_base._z);
        // Move barricade
        //GameScript._EndLight.transform.position = new Vector3(object_base._x, 26f, object_base._z);
        // Get properties
        foreach (var pair in GetProperties(ref object_data_split, ref object_data_iter))
          switch (pair.Key)
          {
            // Rotation
            case ("rot"):
              FunctionsC.RotateLocal(ref loadedObject, float.Parse(pair.Value, System.Globalization.CultureInfo.InvariantCulture));
              break;
            case ("co"):
              var co = loadedObject.AddComponent<CustomObstacle>();
              var split = pair.Value.Split('.');
              var index = int.Parse(split[1], System.Globalization.CultureInfo.InvariantCulture);
              co._index = index;
              CustomObstacle._PlayerSpawn = co;
              break;
            default:
              Debug.LogError("LoadObject() => Unhandled property " + pair.Key + " with value " + pair.Value);
              break;
          }
        break;
    }
    // If not loaded, try loading furniture
    if (loadedObject == null)
    {
      foreach (var leo in _Furniture)
      {
        if (leo == null) Debug.LogError("LEO");
        if (!object_base._type.Equals(leo._name.ToLower())) continue;
        var isNavMesh = (leo._name.Equals(_LEO_NavMeshBarrier._name));
        loadedObject = object_base.LoadResource(leo._name, _Map.GetChild(1));
        if ((isNavMesh || leo._name.Equals(_LEO_RugRectangle._name)) && !GameScript._EditorEnabled) { loadedObject.GetComponent<BoxCollider>().enabled = false; }
        loadedObject.transform.localPosition = new Vector3(loadedObject.transform.localPosition.x, leo._movementSettings._localPos, loadedObject.transform.localPosition.z);

        // Check fake wall
        if (leo._name == _LEO_TileWall._name)
        {
          //Debug.Log("Getting walls for fake wall");

          // Create outer walls
          var inner_container = loadedObject.transform.GetChild(1);
          var inner_mesh = GameObject.Find("Tile_" + SceneThemes._Theme._tile).transform.GetChild(0).GetChild(0);
          for (var i = 0; i < 2; i++)
          {
            var inner0 = GameObject.Instantiate(inner_mesh.gameObject) as GameObject;
            inner0.transform.parent = inner_container;
            inner0.transform.localScale = inner_mesh.localScale;
            inner0.transform.localPosition = new Vector3(0f, inner_mesh.localPosition.y, (inner_mesh.localPosition.z - 0.35f) * (i == 0 ? -1f : 1f));

            if (i == 0)
              inner0.transform.Rotate(new Vector3(0f, 0f, 180f));
          }
        }

        // Check books
        else if (leo._name == _LEO_Books._name)
        {
          FunctionsC.BookManager.RegisterBooks(loadedObject.transform);
        }

        // Change layer per editor mode
        if (loadedObject.name == _LEO_Interactable._name || loadedObject.name == _LEO_NavMeshBarrier._name)
          loadedObject.layer = GameScript._EditorEnabled ? 0 : 2;

        // Get properties
        foreach (var pair in GetProperties(ref object_data_split, ref object_data_iter))
          switch (pair.Key)
          {
            // Rotation
            case ("rot"):
              FunctionsC.RotateLocal(ref loadedObject, float.Parse(pair.Value, System.Globalization.CultureInfo.InvariantCulture));
              break;
            case ("co"):
              var co = loadedObject.GetComponent<CustomObstacle>();
              var split = pair.Value.Split('.');
              var type = (CustomObstacle.InteractType)System.Enum.Parse(typeof(CustomObstacle.InteractType), split[0]);
              var index = int.Parse(split[1], System.Globalization.CultureInfo.InvariantCulture);
              var index2 = 0;
              if (split.Length > 2)
                index2 = int.Parse(split[2], System.Globalization.CultureInfo.InvariantCulture);
              if (co == null)
                co = loadedObject.AddComponent<CustomObstacle>();
              co._type = type;
              co._index = index;
              co._index2 = index2;
              if (loadedObject.name == _LEO_BookcaseOpen._name)
                co.InitMoveableBarrier();
              else if (loadedObject.name == _LEO_RugRectangle._name)
                co.InitZombieSpawn();
              else if (loadedObject.name == _LEO_Interactable._name)
                co.Init();
              else if (loadedObject.name == _LEO_CandelBarrel._name || loadedObject.name == _LEO_CandelBig._name || loadedObject.name == _LEO_CandelTable._name)
                co.InitCandle();
              else Debug.LogError("Unhandled co val in " + loadedObject.name);
              break;
            default:
              Debug.LogError($"LoadObject() => Unhandled property <color=red>{pair.Key}</color> with value <color=red>{pair.Value}</color>\n{object_data}");
              break;
          }
        // Change furnature color
        ChangeColorRecursive(loadedObject.transform, SceneThemes._Theme._furnatureColor);
        // Check for light color change
        if (loadedObject.transform.childCount > 2)
        {
          Light l = loadedObject.transform.GetChild(2).GetComponent<Light>();
          if (l != null)
          {
            l.color = SceneThemes._Theme._lightColor;
            l.shadowStrength = SceneThemes._Theme._shadowStrength;
            // Ignore raycasts on lamps
            if (!GameScript._EditorEnabled)
              loadedObject.layer = 2;
          }
        }
        break;
      }
      // Finally if not loaded, throw error
      if (loadedObject == null)
        Debug.LogWarning("Error loading object data " + object_data);
    }
    // Return the loaded object
    return loadedObject;
  }

  static void ChangeColorRecursive(Transform t, Color c)
  {
    foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
      if (r != null)
        for (int i = 0; i < r.sharedMaterials.Length; i++)
          if (r.sharedMaterials[i] != null && r.sharedMaterials[i].name.Split(' ')[0].Equals("Table"))
            r.sharedMaterials[i].color = c;
  }

  /// Create a dictionary for more efficient parsing of object data
  static Dictionary<string, string> GetProperties(ref string[] object_data_split, ref int object_data_iter)
  {
    Dictionary<string, string> properties = new Dictionary<string, string>();
    for (; object_data_iter < object_data_split.Length;)
    {
      if (object_data_split[object_data_iter].Trim().Length == 0)
      {
        object_data_iter++;
        continue;
      }
      // Check if only one property left
      if (object_data_iter == object_data_split.Length - 1)
      {
        properties.Add(object_data_split[object_data_iter++], "NULL");
        break;
      }
      try
      {
        properties.Add(object_data_split[object_data_iter++], object_data_split[object_data_iter++]);
      }
      catch (System.Exception e)
      {
        Debug.LogWarning("Caught exception at TileManager.GetProperties() => " + e.ToString());
      }
    }
    return properties;
  }

  class ObjectData
  {
    public string _type, _modifier0;
    public float _x, _z;

    public ObjectData(string type, string x, string y)
    {
      _type = type;
      _x = float.Parse(x, System.Globalization.CultureInfo.InvariantCulture) + (_offset.x + _objectoffset.x) * _Tile_spacing;
      _z = float.Parse(y, System.Globalization.CultureInfo.InvariantCulture) + (_offset.y + _objectoffset.y) * _Tile_spacing;
    }
    public ObjectData(string type, string modifier0)
    {
      _type = type;
      _modifier0 = modifier0;
    }
    public ObjectData(string type, string x, string y, string modifier0)
    {
      _type = type;
      _x = float.Parse(x, System.Globalization.CultureInfo.InvariantCulture) + (_offset.x + _objectoffset.x) * _Tile_spacing;
      _z = float.Parse(y, System.Globalization.CultureInfo.InvariantCulture) + (_offset.y + _objectoffset.y) * _Tile_spacing;
      _modifier0 = modifier0;
    }

    public GameObject LoadResource(string resourceName, Transform parent, float localYPosition = 0f)
    {
      return LoadResource(resourceName, parent, new Vector3(1f, 1f, 1f), localYPosition);
    }
    public GameObject LoadResource(string resourceName, Transform parent, Vector3 localScale, float localYPosition = 0f)
    {
      GameObject resource;
      switch (resourceName)
      {
        case ("Enemy"):
          resource = GameResources._Enemy;
          break;
        case ("Door"):
          resource = GameResources._Door;
          break;
        case ("Button"):
          resource = GameResources._Button;
          break;
        case ("Playerspawn"):
          resource = GameResources._Playerspawn;
          break;
        case ("ExplosiveBarrel"):
          resource = GameResources._Barrel_Explosive;
          break;
        case ("Table"):
          resource = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? GameResources._Table_Bush : GameResources._Table;
          resourceName = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? resourceName + "_Bush" : resourceName;
          break;
        case ("TableSmall"):
          resource = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? GameResources._TableSmall_Bush : GameResources._TableSmall;
          resourceName = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? resourceName + "_Bush" : resourceName;
          break;
        case ("Chair"):
          resource = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? GameResources._Chair_Stump : GameResources._Chair;
          resourceName = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? resourceName + "_Stump" : resourceName;
          break;
        case ("BookcaseClosed"):
          resource = GameResources._BookcaseClosed;
          break;
        case ("BookcaseOpen"):
          resource = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? GameResources._BookcaseOpen_Bush : GameResources._BookcaseOpen;
          resourceName = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? resourceName + "_Bush" : resourceName;
          break;
        case ("BookcaseBig"):
          resource = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? GameResources._BookcaseBig_Bush : GameResources._BookcaseBig;
          resourceName = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? resourceName + "_Bush" : resourceName;
          break;
        case ("RugRectangle"):
          resource = GameResources._RugRectangle;
          break;
        case ("Barrel"):
          resource = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? GameResources._Barrel_Rock : GameResources._Barrel;
          resourceName = SceneThemes._Theme._name == "Hedge" && !GameScript._EditorEnabled ? resourceName + "_Rock" : resourceName;
          localScale.y *= 0.8f;
          break;
        case ("Column"):
          resource = GameResources._ColumnNormal;
          break;
        case ("ColumnBroken"):
          resource = GameResources._ColumnBroken;
          break;
        case ("Rock0"):
          resource = GameResources._Rock0;
          break;
        case ("Rock1"):
          resource = GameResources._Rock1;
          break;
        case ("CandelBig"):
          resource = GameResources._CandelBig;
          break;
        case ("CandelTable"):
          resource = GameResources._CandelTable;
          break;
        case ("CandelBarrel"):
          resource = GameResources._CandelBarrel;
          break;
        case ("Powerup"):
          resource = GameResources._Powerup;
          break;
        case ("NavMeshBarrier"):
          resource = GameResources._NavmeshBarrier;
          break;
        case ("Interactable"):
          resource = GameResources._Interactable;
          break;
        case ("FakeTile"):
          resource = GameResources._Fake_Tile;
          break;
        case ("TileWall"):
          resource = GameResources._Tile_Wall;
          break;
        case ("Arch"):
          resource = GameResources._Arch;
          break;
        case ("Television"):
          resource = GameResources._Television;
          break;
        case ("Books"):
          resource = GameResources._Books;
          break;
        default:
          Debug.Log("Loading resource <color=blue>" + resourceName + "</color> using Resources.Load()");
          resource = Resources.Load(resourceName) as GameObject;
          break;
      }
      var new_gameobject = GameObject.Instantiate(resource);
      new_gameobject.transform.parent = parent;
      new_gameobject.name = resourceName;

      new_gameobject.transform.localScale = localScale;
      new_gameobject.transform.position = new Vector3(_x, 0f, _z);
      new_gameobject.transform.localPosition = new Vector3(new_gameobject.transform.localPosition.x, localYPosition, new_gameobject.transform.localPosition.z);

      return new_gameobject;
    }
  }

  static float _LastReloadTime;
  public static bool CanReloadMap()
  {
    return (!GameScript._EditorEnabled && !_LoadingMap && Time.unscaledTime - _LastReloadTime >= 0.3f);
  }
  public static void ReloadMap()
  {
    // Check button spam
    if (!CanReloadMap()) return;
    _LastReloadTime = Time.unscaledTime;

    // Check AutoPlayer
    if (PlayerScript.AutoPlayer._Capturing)
      PlayerScript.AutoPlayer.Capture();
    if (PlayerScript.AutoPlayer._Playing)
      PlayerScript.AutoPlayer.Playback();

    // Check if map reloading
    if (_LoadingMap) return;

    // Reload assets
    GameScript.ToggleExit(false);
    foreach (var r in ActiveRagdoll._Ragdolls)
      GameObject.DestroyImmediate(r._controller.parent.gameObject);
    ActiveRagdoll.Reset();
    EnemyScript.Reset();
    PlayerScript.Reset();
    Powerup.Reset();
    CustomObstacle.Reset();
    CustomEntityUI._ID = 0;
    ExplosiveScript.Reset();
    ResetParticles();

    var objects = GameResources._Container_Objects;
    for (var i = objects.childCount - 1; i >= 0; i--)
    {
      GameObject child = objects.GetChild(i).gameObject;
      // Reset lasers
      LaserScript l = child.GetComponent<LaserScript>();
      if (l != null)
      {
        l.Reset();
        continue;
      }
      // Reset barrels
      var e = child.GetComponent<ExplosiveScript>();
      if (e != null && e.name.Equals("ExplosiveBarrel"))
      {
        e.Reset2();
        continue;
      }
      GameObject.Destroy(child);
    }
    var map_objects = _Map.GetChild(1);
    for (var i = map_objects.childCount - 1; i >= 0; i--)
    {
      var g = map_objects.GetChild(i).gameObject;
      if (GameScript._GameMode != GameScript.GameModes.SURVIVAL && g.name.Contains("Candel")) continue;
      GameObject.Destroy(g);
    }
    // Reload certain objects
    var reloadObjects = new string[] { "e_", "door_", "p_", "button_", "playerspawn_", (GameScript._GameMode == GameScript.GameModes.SURVIVAL ? "candel" : "__") };
    foreach (var data in _LevelObjects)
    {
      var loaded = false;
      foreach (string s in reloadObjects)
      {
        if (data.Length <= s.Length || !data.Substring(0, s.Length).Equals(s)) continue;
        LoadObject(data);
        loaded = true;
        break;
      }
      if (loaded) continue;
      // Load all furnature
      foreach (var leo in _Furniture)
      {
        var s = leo._addSettings._data.Split('_')[0] + "_";
        if (s.Contains("candel")) continue;
        if (data.Length <= s.Length || !data.Substring(0, s.Length).Equals(s)) continue;
        LoadObject(data);
        break;
      }
    }
    // Combine meshes
    IEnumerator co()
    {
      yield return new WaitForSecondsRealtime(0.1f);
      CombineMeshes(false);
    }
    GameScript._Singleton.StartCoroutine(co());
    // Set level time
    GameScript._LevelStartTime = Time.time;
    // Init enemies
    EnemyScript.HardInitAll();
    // Spawn player
    GameScript.SpawnPlayers();
    OnMapLoad();
  }

  public static void ResetParticles()
  {
    // Misc
    FunctionsC._BookManager.Init();

    /// Gather particle systems to hide
    var particles = new List<ParticleSystem>();

    // Player spawn
    if (PlayerspawnScript._PlayerSpawns != null)
      foreach (var playerspawn in PlayerspawnScript._PlayerSpawns)
        particles.Add(playerspawn.gameObject.GetComponent<ParticleSystem>());

    // Blood
    for (var i = 0; i < 7; i++)
      particles.Add(GameObject.Find("Blood" + i).GetComponent<ParticleSystem>());

    // All other particles
    foreach (var t in GetChildrenRecursive(GameObject.Find("Particles").transform))
    {
      particles.Add(t.GetComponent<ParticleSystem>());
    }

    foreach (var p in particles)
    {
      if (p == null) continue;
      p.Stop();
      p.Clear();
    }

    // Remove bullets and their particles
    BulletScript.HideAll();
  }
  static Transform[] GetChildrenRecursive(Transform start)
  {
    List<Transform> returns = new List<Transform>();
    for (int i = 0; i < start.childCount; i++)
    {
      Transform child = start.GetChild(i);
      if (child.childCount > 0)
      {
        Transform[] childReturns = GetChildrenRecursive(child);
        foreach (Transform t in childReturns) returns.Add(t);
        continue;
      }
      returns.Add(child);
    }
    return returns.ToArray();
  }

  static Tile SpawnTile(int width, int height)
  {
    Transform t = GameObject.Instantiate(_Tile.gameObject as GameObject).transform;
    t.gameObject.name = "Tile";
    t.parent = _Map;
    t.localPosition = new Vector3(width * _Tile_spacing, _Tile.localPosition.y, height * _Tile_spacing);
    return new Tile(t.gameObject);
  }

  enum EditorMode
  {
    NONE,
    MOVE
  }
  static EditorMode _CurrentMode;
  static Transform _Pointer;
  public static Transform _Ring;
  static LineRenderer[] _LineRenderers;
  static TextMesh _TextMesh;

  static void SpawnObjectSimple(LevelEditorObject leoobj)
  {
    LevelEditorObject.SetIterOnName(leoobj._name);
    var addSettings = leoobj._addSettings;
    var loaded = LoadObject(addSettings._data);
    // Fire onAdd function
    addSettings._onAdd?.Invoke(loaded);
    // Set move mode
    _SelectedObject = loaded.transform;
    _CurrentMode = EditorMode.MOVE;
  }

  public static class EditorMenus
  {

    public static RectTransform
      _Menu_Editor,
      _Menu_Object_Select,
      _Menu_Infos,
      _Menu_Infos_Enemy,
      _Menu_Infos_Door,
      _Menu_Infos_Button,
      _Menu_Infos_Goal,
      _Menu_Map_Rename,
      _Menu_Infos_Tile,
      _Menu_Workshop_Infos,


      _Menu_EditorTesting;

    // Init menus and buttons
    public static void Init()
    {

      _Menu_Editor = GameObject.Find("Editor_UI").transform as RectTransform;
      _Menu_Object_Select = _Menu_Editor.transform.GetChild(0).transform as RectTransform;
      _Menu_Infos = _Menu_Editor.transform.GetChild(1).transform as RectTransform;
      _Menu_Infos_Enemy = _Menu_Editor.transform.GetChild(2).transform as RectTransform;
      _Menu_Infos_Door = _Menu_Editor.transform.GetChild(4).transform as RectTransform;
      _Menu_Infos_Button = _Menu_Editor.transform.GetChild(5).transform as RectTransform;
      _Menu_Infos_Goal = _Menu_Editor.transform.GetChild(6).transform as RectTransform;
      _Menu_Infos_Tile = _Menu_Editor.transform.GetChild(7).transform as RectTransform;

      _Menu_Map_Rename = _Menu_Editor.transform.GetChild(3).transform as RectTransform;
      _Menu_Workshop_Infos = _Menu_Editor.transform.GetChild(8).transform as RectTransform;

      _Menu_EditorTesting = GameObject.Find("Editor_Testing_UI").transform as RectTransform;

      HideMenus();
    }

    public static void HideMenus()
    {
      _Menu_Editor.gameObject.SetActive(false);
      _Menu_EditorTesting.gameObject.SetActive(false);

      _Menu_Map_Rename.gameObject.SetActive(false);
      _Menu_Workshop_Infos.gameObject.SetActive(false);
    }
    public static void ShowMenus()
    {
      _Menu_Editor.gameObject.SetActive(true);

      _Menu_Object_Select.gameObject.SetActive(true);
      _Menu_Infos.gameObject.SetActive(true);

      _Menu_Infos_Enemy.gameObject.SetActive(false);
      _Menu_Infos_Door.gameObject.SetActive(false);
      _Menu_Infos_Button.gameObject.SetActive(false);
      _Menu_Infos_Goal.gameObject.SetActive(false);
      _Menu_Infos_Tile.gameObject.SetActive(false);
      _Menu_Map_Rename.gameObject.SetActive(false);
      _Menu_Workshop_Infos.gameObject.SetActive(false);
    }

    public static RectTransform ShowRenameMenuMenu()
    {
      _Menu_Editor.gameObject.SetActive(true);

      _Menu_Object_Select.gameObject.SetActive(false);
      _Menu_Infos.gameObject.SetActive(false);
      _Menu_Infos_Enemy.gameObject.SetActive(false);
      _Menu_Infos_Door.gameObject.SetActive(false);
      _Menu_Infos_Button.gameObject.SetActive(false);
      _Menu_Infos_Goal.gameObject.SetActive(false);
      _Menu_Infos_Tile.gameObject.SetActive(false);

      _Menu_Workshop_Infos.gameObject.SetActive(false);

      _Menu_Map_Rename.gameObject.SetActive(true);

      return _Menu_Map_Rename;
    }
    public static RectTransform ShowWorkshopMenu()
    {
      _Menu_Editor.gameObject.SetActive(true);

      _Menu_Object_Select.gameObject.SetActive(false);
      _Menu_Infos.gameObject.SetActive(false);
      _Menu_Infos_Enemy.gameObject.SetActive(false);
      _Menu_Infos_Door.gameObject.SetActive(false);
      _Menu_Infos_Button.gameObject.SetActive(false);
      _Menu_Infos_Goal.gameObject.SetActive(false);
      _Menu_Infos_Tile.gameObject.SetActive(false);

      _Menu_Map_Rename.gameObject.SetActive(false);

      _Menu_Workshop_Infos.gameObject.SetActive(true);


      return _Menu_Map_Rename;
    }


    public static void SetTextQuick(Transform t, string text)
    {
      t.GetComponent<TMPro.TextMeshProUGUI>().text = text;
    }
  }

  public static IEnumerator EditorEnabled()
  {
    _EditorSwitchTime = Time.unscaledTime;

    // Reload current map
    LoadMap(_CurrentData, true);

    // Show menus
    EditorMenus.ShowMenus();

    // Set editor button functions
    for (var i = 1; i < EditorMenus._Menu_Object_Select.childCount; i++)
    {
      var button = EditorMenus._Menu_Object_Select.GetChild(i).GetComponent<UnityEngine.UI.Button>();
      button.onClick.RemoveAllListeners();

      switch (i)
      {

        // Spawn enemy
        case 1:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_Enemy);
          });
          break;

        // Spawn barrel
        case 2:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_Barrel);
          });
          break;

        // Spawn Table
        case 3:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_Table);
          });
          break;

        // Spawn Bookcase
        case 4:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_BookcaseBig);
          });
          break;

        // Spawn Bookcase small
        case 5:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_BookcaseOpen);
          });
          break;

        // Spawn Chair
        case 6:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_Chair);
          });
          break;

        // Spawn Candle
        case 7:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_CandelBig);
          });
          break;

        // Spawn Candle smaller
        case 8:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_CandelTable);
          });
          break;

        // Spawn Candle smallest
        case 9:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_CandelBarrel);
          });
          break;

        // Spawn Door
        case 10:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_Books);
          });
          break;

        // Spawn Fake wall
        case 11:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_TileWall);
          });
          break;

        /*/ Spawn Button
        case 11:

          button.onClick.AddListener(() =>
          {
            SpawnObjectSimple(_LEO_Button);
          });
          break;*/

        // Edit tiles
        case 12:

          button.onClick.AddListener(() =>
          {
            _CurrentMode = EditorMode.NONE;
            LevelEditorObject.SetIterOnName(_LEO_Tile._name);
          });
          break;

        // Move spawn
        case 13:

          button.onClick.AddListener(() =>
          {
            LevelEditorObject.SetIterOnName(_LEO_Playerspawn._name);
            _SelectedObject = GameObject.Find("PlayerSpawn").transform;
            _CurrentMode = EditorMode.MOVE;
          });
          break;

        // Move goal
        case 14:

          button.onClick.AddListener(() =>
          {
            LevelEditorObject.SetIterOnName(_LEO_Goal._name);
            _SelectedObject = GameObject.Find("Powerup").transform.GetChild(0);
            _CurrentMode = EditorMode.MOVE;
          });
          break;

      }

      /*/ Set mouse to center of screen
      button.onClick.AddListener(() =>
      {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.lockState = CursorLockMode.None;
      });*/
    }

    // Reset all map particles
    ResetParticles();

    // Get text
    if (_TextMesh == null)
    {
      _TextMesh = GameObject.Find("Editor_text").GetComponent<TextMesh>();
      _TextMesh.gameObject.SetActive(false);
    }
    _text = new List<string>();
    _TextMesh.GetComponent<MeshRenderer>().enabled = true;
    SetText("Object: Tile");

    while (_LoadingMap) yield return new WaitForSecondsRealtime(0.05f);

    // Remove player
    foreach (PlayerScript p in PlayerScript._Players)
      GameObject.Destroy(p.transform.parent.gameObject);
    PlayerScript.Reset();
    // Reload map objects
    Transform objects = GameResources._Container_Objects,
      navmesh_mods = _Map.GetChild(1);
    for (int i = objects.childCount - 1; i >= 0; i--)
      GameObject.Destroy(objects.GetChild(i).gameObject);
    for (int i = navmesh_mods.childCount - 1; i >= 0; i--)
      GameObject.Destroy(navmesh_mods.GetChild(i).gameObject);
    string[] notloadObjects = new string[] { "e_", "playerspawn_" };
    foreach (string data in _LevelObjects)
    {
      bool found = false;
      foreach (string data_type in notloadObjects)
        if (data.Length > data_type.Length && data.Substring(0, data_type.Length).Equals(data_type))
        {
          found = true;
          break;
        }
      if (found) continue;
      GameObject new_object = LoadObject(data);
      if (new_object == null) continue;
      // Laser script
      LaserScript ls = new_object.GetComponent<LaserScript>();
      ls?.OnEditorEnable();
      // Button
      if (new_object.name == "Button") new_object.layer = 5;
      Powerup.OnEditorEnable();
    }
    // Get camera zoom
    _CameraZoom = GameResources._Camera_Main.transform.position.y;
    // Remove enemies and show paths
    if (!GameScript.IsSurvival())
    {
      if (EnemyScript._Enemies_alive != null)
        foreach (EnemyScript e in EnemyScript._Enemies_alive)
        {
          e.enabled = false;
          Transform controller = e.transform.parent.GetChild(0);
          controller.position = e._startPosition;
          GameObject controller_visual = GiveEnemyVisual(ref controller);
          ChangeEnemyType(e, controller_visual.GetComponent<MeshRenderer>());
          controller.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
          GameObject.Destroy(e.GetRagdoll().transform.gameObject);
        }
      if (EnemyScript._Enemies_dead != null)
        foreach (EnemyScript e in EnemyScript._Enemies_dead)
        {
          e.enabled = false;
          Transform controller = e.transform.parent.GetChild(0);
          controller.position = e._startPosition;
          GameObject controller_visual = GiveEnemyVisual(ref controller);
          ChangeEnemyType(e, controller_visual.GetComponent<MeshRenderer>());
          controller.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
          GameObject.Destroy(e.GetRagdoll().transform.gameObject);
        }
    }
    // If survival, don't save enemy positions
    else
    {
      if (EnemyScript._Enemies_alive != null)
        foreach (EnemyScript e in EnemyScript._Enemies_alive)
          GameObject.Destroy(e.transform.parent.gameObject);
      if (EnemyScript._Enemies_dead != null)
        foreach (EnemyScript e in EnemyScript._Enemies_dead)
          GameObject.Destroy(e.transform.parent.gameObject);
    }
    // Clear ragdolls
    ActiveRagdoll.Reset();
    // Show player spawn
    foreach (PlayerspawnScript s in PlayerspawnScript._PlayerSpawns)
      s._visual.SetActive(true);
    // Enable pointer and ring
    if (_Pointer == null)
    {
      _Pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
      GameObject.Destroy(_Pointer.GetComponent<Collider>());
      _Pointer.gameObject.name = "Pointer";
      _Pointer.parent = GameScript._Singleton.transform;
      _Pointer.gameObject.layer = 2;
      MeshRenderer m = _Pointer.GetComponent<MeshRenderer>();
      m.sharedMaterial = GameObject.Find("BlackFade").GetComponent<MeshRenderer>().sharedMaterial;
      Color c = Color.red;
      c.a = 0.5f;
      m.sharedMaterial.color = c;
      m.enabled = false;
      _Pointer.localScale = new Vector3(0.3f, 0.3f, 0.3f);

      _Ring = GameObject.Find("ring").transform;
      _Ring.gameObject.layer = 2;
    }
    else
    {
      _Pointer.gameObject.SetActive(true);
      _Ring.gameObject.SetActive(true);
    }
    // Create line renderers for editor visual
    if (_LineRenderers == null)
    {
      _LineRenderers = new LineRenderer[4];
      for (int i = 0; i < _LineRenderers.Length; i++)
      {
        GameObject lr = new GameObject("lr" + i);
        _LineRenderers[i] = lr.AddComponent<LineRenderer>();
        _LineRenderers[i].alignment = LineAlignment.TransformZ;
        _LineRenderers[i].startWidth = 0.2f;
        _LineRenderers[i].endWidth = 0.1f;
        lr.transform.Rotate(new Vector3(1f, 0f, 0f) * 90f);
      }
    }
    // Turn on camera light
    GameScript.ToggleCameraLight(true);
    // Set time to normal
    Time.timeScale = 1f;
    // Remove combined meshes and enable tile renderers
    var meshes = GameObject.Find("Meshes_Tiles_Up");
    if (meshes) GameObject.Destroy(meshes);
    meshes = GameObject.Find("Meshes_Tiles_Down");
    if (meshes) GameObject.Destroy(meshes);
    meshes = GameObject.Find("Meshes_Tiles_Sides");
    if (meshes) GameObject.Destroy(meshes);
    foreach (var t in _Tiles)
    {
      if (t == null) continue;
      t._tile.gameObject.SetActive(true);
      t._tile.gameObject.GetComponent<Renderer>().enabled = true;
      t._tile.gameObject.GetComponent<Collider>().enabled = true;
    }

    // Set default start object
    LevelEditorObject._Objects_iter = 1;

    // Toggle exit
    GameScript.ToggleExit(false);
    //Debug.Log("Map editor enabled.");
    //Time.timeScale = 1f;

    // Set player spawn layer
    PlayerspawnScript._PlayerSpawns[0].gameObject.layer = 0;
  }
  public static void EditorDisabled(string mapData)
  {
    _EditorSwitchTime = Time.unscaledTime;

    _SelectedObject = null;

    _TextMesh.GetComponent<MeshRenderer>().enabled = false;
    _text = null;

    // Forget tiles
    _Selected_Tiles = null;
    // Remove enemies
    EnemyScript.Reset();
    CustomObstacle.Reset();
    // Load new map
    if (mapData != null)
      LoadMap(mapData);

    // Disable display ring
    _Pointer.gameObject.SetActive(false);
    _Ring.gameObject.SetActive(false);
    _Ring.position = new Vector3(0f, -100f, 0f);

    // Remove line renders
    LineRenderer_Clear();

    // Set player spawn layer
    PlayerspawnScript._PlayerSpawns[0].gameObject.layer = 2;

    // Disable light
    GameScript.ToggleCameraLight(false);
    // Hide player spawn(s)
    foreach (PlayerspawnScript s in PlayerspawnScript._PlayerSpawns)
      s._visual.SetActive(false);
    //Debug.Log("Map editor disabled.");

    // Hide menus
    EditorMenus.HideMenus();
  }

  public static void SaveFileOverwrite(string mapdata, int mapindex)
  {
    Levels._LevelCollections[Levels._CurrentLevelCollectionIndex]._leveldata[mapindex] = mapdata;
    Levels.SaveLevels();
  }
  public static void SaveFileOverwrite(string mapdata)
  {
    SaveFileOverwrite(mapdata, Levels._CurrentLevelIndex);
  }

  static List<string> _text;
  static void SetText(string text)
  {
    _TextMesh.text = text;
  }
  static void UpdateText()
  {
    _TextMesh.text = "";
    _text.Insert(0, $"Object: {LevelEditorObject.GetCurrentObject()._name}");
    foreach (string s in _text)
      _TextMesh.text += $"{s}\n";
    // Add last line
    if (_SelectedObject != null)
      if (_SelectedObject != null && CanCustomObject())
      {
        var co = _SelectedObject.GetComponent<CustomObstacle>();
        var hasObstacle = co != null ? true : false;
        var add = "";
        var type = "";
        if (hasObstacle)
        {
          if (co._type == CustomObstacle.InteractType.BUYITEM)
            add = $"weapon tier: {co._index} (N) | current roomID: {co._index2} (M) |";
          else if (co._type == CustomObstacle.InteractType.BUYUTILITY)
            add = $"utility tier: {co._index} (N) | current roomID: {co._index2} (M) |";
          else if (co._type == CustomObstacle.InteractType.BUYPERK)
            add = $"perk tier: {co._index} (N) | current roomID: {co._index2} (M) |";
          else
            add = $"roomID: {co._index} (N) | current roomID: {co._index2} (M) |";
          type = co._type.ToString();
        }
        _TextMesh.text += $"custom obstacle: {hasObstacle} (B) | {add} mode: {type} (,)\n";
      }
  }
  static void ClearText()
  {
    _text = new List<string>();
  }

  static bool CanCustomObject()
  {
    var cu = LevelEditorObject.GetCurrentObject();
    foreach (var ob in new LevelEditorObject[] {
      _LEO_BookcaseOpen,
      _LEO_RugRectangle,
      _LEO_Interactable,
      _LEO_CandelBarrel, _LEO_CandelBig, _LEO_CandelTable,
      _LEO_Playerspawn,
    })
      if (_SelectedObject.name == ob._name && cu._name == ob._name)
        return true;
    return false;
  }

  static bool _IsLinking;

  #region LEOs
  static LevelEditorObject
    _LEO_Tile = new LevelEditorObject("Tile", LevelEditorObject._UpdateSelectFunction_Tile,
      null, null, null, null, null, null,
      (GameObject g) =>
      {
        ClearText();
        _text.Add("Select tiles (Left mouse)");
        _text.Add("Toggle tiles (Right mouse)");
        _text.Add("Deselect tiles (Mouse 4)");
        UpdateText();
      }),

    _LEO_Goal = new LevelEditorObject("Goal",
      (GameObject g) =>
      {
        LevelEditorObject._UpdateFunction_Object(g);
        if (_SelectedObject != null)
          LevelEditorObject._UpdateFunction_CustomEntityUI(_SelectedObject.parent.gameObject);
      },
      new LevelEditorObject.MovementSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT,
        _localPos = -0.86f
      }, null, null, null, null,
      (LevelEditorObject leo, GameObject g, Vector3 offset, Vector3 pos_use) =>
      {
        var returnString = "";
        // Check for powerup
        var p = g.GetComponent<Powerup>();
        if (p != null)
        {
          returnString += "p_" + System.Math.Round(pos_use.x, 2) + "_" + System.Math.Round(pos_use.z, 2) + "_";
          // Add powerup type
          switch (p._type)
          {
            case (Powerup.PowerupType.END):
              returnString += "end_";
              break;
          }
          // Add connected entities
          var b = p.GetComponent<CustomEntityUI>();
          foreach (CustomEntity entity in b._activate)
          {
            if (entity == null) continue;
            // Check for doors
            DoorScript ds = entity.gameObject.GetComponent<DoorScript>();
            if (ds != null)
            {
              var pos_use_local = ds.transform.position - offset;

              var append = "";

              {
                var savebutton = ds._button;
                ds._button = null;
                append = _LEO_Door._saveFunction?.Invoke(_LEO_Door, ds.gameObject, offset, pos_use_local);
                ds._button = savebutton;
              }

              //Debug.Log("saving door, got: " + append);
              returnString += $"{append.Trim()}";
            }
          }
          returnString += " ";
          return returnString;
        }
        return null;
      },
      (GameObject g) =>
      {
        if (g == null) return;
        ClearText();
        _text.Add("Link to door (L)");
        _text.Add("Unlink all links (U)");
        UpdateText();
      }),

    _LEO_Enemy = new LevelEditorObject("Enemy", (GameObject g) =>
      {
        // Normal select / deselect
        LevelEditorObject._UpdateFunction_Object(g);
        if (_SelectedObject == null) return;
        var script_enemy = (LevelEditorObject.GetCurrentObject()._name.Equals("Enemy") ? _SelectedObject.GetChild(0) : _SelectedObject.parent).GetComponent<EnemyScript>();

        // Check for change type
        if (ControllerManager.GetKey(ControllerManager.Key.T))
          ChangeEnemyType(script_enemy, script_enemy.transform.GetChild(0).GetComponent<MeshRenderer>(), 1);

        // Change movement mode
        if (ControllerManager.GetKey(ControllerManager.Key.M))
          script_enemy._canMove = !script_enemy._canMove;

        // Change hearing mode
        if (ControllerManager.GetKey(ControllerManager.Key.H))
          script_enemy._reactToSound = !script_enemy._reactToSound;

        // Create new waypoint
        if (ControllerManager.GetKey(ControllerManager.Key.W))
        {
          _Ring.position = new Vector3(0f, -100f, 0f);
          LevelEditorObject.Select(_SelectedObject.GetChild(1).GetChild(0).gameObject);
          LevelEditor_Copy(LevelEditorObject.GetCurrentObject()._copySettings);
        }

        // Move spawn point
        if (ControllerManager.GetKey(ControllerManager.Key.V))
        {
          LevelEditorObject.Select(_SelectedObject.GetChild(0).GetChild(0).gameObject);
          LevelEditorObject.SetIterOnName(_SelectedObject.name);
          _Ring.position = _SelectedObject.position;
          _CurrentMode = EditorMode.MOVE;
        }

        // Link EnemyScript with DoorScript
        if (ControllerManager.GetKey(ControllerManager.Key.L))
        {
          _IsLinking = !_IsLinking;

          _CurrentMode = EditorMode.NONE;
        }
        if (ControllerManager.GetKey(ControllerManager.Key.U))
        {
          _IsLinking = false;

          if (script_enemy._linkedDoor != null)
          {
            script_enemy._linkedDoor._trigger_enemies.Remove(script_enemy._id);
            script_enemy._linkedDoor = null;
          }
        }

        if (_IsLinking)
        {
          // Get mouse pos
          RaycastHit h;
          Physics.SphereCast(GameResources._Camera_Main.ScreenPointToRay(ControllerManager.GetMousePosition()), 0.25f, out h);
          Vector3 mousePos = h.point;
          mousePos.y = -1f;
          _LineRenderers[1].positionCount = 2;
          _LineRenderers[1].SetPositions(new Vector3[] { script_enemy.transform.position, mousePos });
          // Check for selection
          if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.DOWN))
          {
            var d = h.collider.transform.parent.parent.GetComponent<DoorScript>();
            if (d != null)
            {

              if (script_enemy._linkedDoor != null)
              {
                script_enemy._linkedDoor._trigger_enemies.Remove(script_enemy._id);
                script_enemy._linkedDoor = null;
              }

              d.RegisterEnemy(script_enemy._id);
              script_enemy._linkedDoor = d;
            }
            _IsLinking = false;
          }
        }

        // Display line renderer
        else
        {
          if (script_enemy._linkedDoor == null)
            _LineRenderers[1].positionCount = 0;
          else
          {
            _LineRenderers[1].positionCount = 2;
            _LineRenderers[1].SetPositions(new Vector3[] { script_enemy.transform.position, script_enemy._linkedDoor.transform.position });
          }
        }
      },
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.32f
      },
      new LevelEditorObject.RotationSettings()
      {
        _axis = LevelEditorObject.Axis.Z
      },
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "e_0_0_li_knife",
        _onAdd = (GameObject g) =>
        {
          // Set enemy color and give visual for editor
          Transform controller = g.transform.GetChild(0),
            visual = GiveEnemyVisual(ref controller).transform;
          ChangeEnemyType(controller.GetComponent<EnemyScript>(), visual.GetComponent<MeshRenderer>());
        }
      },
      new LevelEditorObject.DeleteSettings(),
      null,
      (GameObject g) =>
      {
        if (g == null || _SelectedObject == null) return;
        EnemyScript s = (LevelEditorObject.GetCurrentObject()._name.Equals("Enemy") ? _SelectedObject.GetChild(0) : _SelectedObject.parent).GetComponent<EnemyScript>();
        if (s == null) return;
        ClearText();
        _text.Add(string.Format("Type (T): {0}", s._itemLeft));
        _text.Add(string.Format("Can move (M): {0}", s._canMove));
        _text.Add(string.Format("Can hear (H): {0}", s._reactToSound));
        UpdateText();

        // Update menus
        var enemy_type = "";
        switch (s._itemLeft)
        {
          case GameScript.ItemManager.Items.KNIFE:

            enemy_type = "Knife";
            break;
          case GameScript.ItemManager.Items.PISTOL:

            enemy_type = "Pistol";
            break;
          case GameScript.ItemManager.Items.PISTOL_SILENCED:

            enemy_type = "Silenced pistol";
            break;
          case GameScript.ItemManager.Items.REVOLVER:

            enemy_type = "Revolvers";
            break;
          case GameScript.ItemManager.Items.GRENADE_HOLD:

            enemy_type = "Grenade";
            break;
          case GameScript.ItemManager.Items.ROCKET_LAUNCHER:

            enemy_type = "Grenade launcher";
            break;

          case GameScript.ItemManager.Items.BAT:

            enemy_type = "Chase robot";
            break;
        }
        EditorMenus.SetTextQuick(EditorMenus._Menu_Infos_Enemy.GetChild(1).GetChild(0), $"[T] Enemy type: {enemy_type}");

        var movement_string = s._canMove ? "Normal" : "Can't move";
        EditorMenus.SetTextQuick(EditorMenus._Menu_Infos_Enemy.GetChild(2).GetChild(0), $"[M] Enemy movement: {movement_string}");

        var hearing_string = s._reactToSound ? "Normal" : "Can't hear";
        EditorMenus.SetTextQuick(EditorMenus._Menu_Infos_Enemy.GetChild(3).GetChild(0), $"[H] Enemy hearing: {hearing_string}");
      })
    {
      _hide = true
    },

    _LEO_EnemyVisual = new LevelEditorObject("Enemy_visual",
      _LEO_Enemy._update,
      new LevelEditorObject.MovementSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT,
        _axis = LevelEditorObject.Axis.Z,
      }, null,
      new LevelEditorObject.CopySettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT_PARENT,
        _onCopy = (GameObject copy) =>
        {
          LevelEditorObject.SetIterOnName(copy.name);
        }
      },
      new LevelEditorObject.AddSettings()
      {
        _data = "e_0_0_li_knife",
        _onAdd = _LEO_Enemy._addSettings._onAdd
      },
      new LevelEditorObject.DeleteSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT_PARENT
      },
      null,
      _LEO_Enemy._textDisplayFunction),

    _LEO_EnemyWaypoint = new LevelEditorObject("Waypoint",
      (GameObject g) =>
      {
        LevelEditorObject._UpdateFunction_Object(g);
        /*/ Teleport controller to position
        if (ControllerManager.GetKey(ControllerManager.Key.L))
        {
          _SelectedObject = _SelectedObject.parent.parent.GetChild(0).GetChild(0);
          _CurrentMode = EditorMode.MOVE;
          LevelEditorObject.SetIterOnName(_SelectedObject.name);
        }*/
      },
      new LevelEditorObject.MovementSettings()
      {
        _axis = LevelEditorObject.Axis.Z
      },
      new LevelEditorObject.RotationSettings()
      {
        _axis = LevelEditorObject.Axis.Z
      },
      new LevelEditorObject.CopySettings(),
      null,
      new LevelEditorObject.DeleteSettings(),
      null,
      (GameObject g) =>
      {
        ClearText();
        _text.Add("Teleport enemy controller to mouse (L)");
        UpdateText();
      }),

    _LEO_EnemyLookpoint = new LevelEditorObject("Lookpoint", LevelEditorObject._UpdateFunction_Object, new LevelEditorObject.MovementSettings()
    {
      _axis = LevelEditorObject.Axis.Z
    },
      new LevelEditorObject.RotationSettings()
      {
        _axis = LevelEditorObject.Axis.Z
      },
      new LevelEditorObject.CopySettings(),
      null,
      new LevelEditorObject.DeleteSettings(),
      null,
      null),

    _LEO_ExplosiveBarrel = new LevelEditorObject("ExplosiveBarrel", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -0.9f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "expbarrel_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      (LevelEditorObject leo, GameObject g, Vector3 offset, Vector3 pos_use) =>
      {
        var returnString = "";

        // Check for barrels
        var exp = g.GetComponent<ExplosiveScript>();
        if (exp != null && g.name == "ExplosiveBarrel")
        {
          returnString += LevelEditorObject._SaveFunction_Pos(leo, pos_use) + " ";
          return returnString;
        }
        return null;
      },
      null),

    _LEO_ExplosiveBarrelMesh = new LevelEditorObject("ExplosiveBarrel_Mesh", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT,
        _localPos = _LEO_ExplosiveBarrel._movementSettings._localPos
      },
      null,
      new LevelEditorObject.CopySettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT
      },
      new LevelEditorObject.AddSettings()
      {
        _data = _LEO_ExplosiveBarrel._addSettings._data
      },
      new LevelEditorObject.DeleteSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT
      },
      null,
      null)
    {
      _hide = true
    },

    _LEO_Button = new LevelEditorObject("Button", (GameObject g) =>
      {
        LevelEditorObject._UpdateFunction_Object(g);
        if (_SelectedObject != null)
          LevelEditorObject._UpdateFunction_CustomEntityUI(_SelectedObject.gameObject);
      },
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.2f
      },
      null,
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "button_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      (LevelEditorObject leo, GameObject g, Vector3 offset, Vector3 pos_use) =>
      {
        string returnString = "";
        // Check for buttons
        if (g.name.Equals("Button"))
        {
          CustomEntityUI b = g.GetComponent<CustomEntityUI>();
          returnString += "button_" + System.Math.Round(pos_use.x, 2) + "_" + System.Math.Round(pos_use.z, 2) + "_";
          // Check connected entities
          foreach (CustomEntity entity in b._activate)
          {
            if (entity == null) continue;
            // Check for doors
            DoorScript ds = entity.gameObject.GetComponent<DoorScript>();
            if (ds != null)
            {
              Vector3 pos_use2 = ds.transform.position - offset;
              returnString += "door_" + System.Math.Round(pos_use2.x, 2) + "_" + System.Math.Round(pos_use2.z, 2) + "_";
              // Add y rotation
              returnString += "rot_" + ds.transform.localRotation.eulerAngles.y + "_";
              // Add open status
              returnString += "open_" + (ds._opened ? "1" : "0") + "_";
              continue;
            }
          }
          returnString = returnString.Substring(0, returnString.Length - 1) + " ";
          return returnString;
        }
        return null;
      },
      (GameObject g) =>
      {
        ClearText();
        _text.Add(string.Format("Link to door (L)"));
        _text.Add(string.Format("Unlink all links (U)"));
        UpdateText();
      }),

    _LEO_Door = new LevelEditorObject("Door",
      (GameObject g) =>
      {
        LevelEditorObject._UpdateFunction_Object(g);
        // Check toggle
        if (ControllerManager.GetKey(ControllerManager.Key.T))
        {
          DoorScript script = null;
          if (_SelectedObject.name.Equals("Door")) script = _SelectedObject.GetComponent<DoorScript>();
          if (_SelectedObject.name.Equals("Door_Obstacle") || _SelectedObject.name.Equals("Door_Under")) script = _SelectedObject.parent.parent.GetComponent<DoorScript>();
          script.Toggle();
        }
      },
      new LevelEditorObject.MovementSettings()
      {
        _localPos = 0.15f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings()
      {
        _onCopy = (GameObject g) =>
        {
          // Link new door to old
          g.GetComponent<DoorScript>().LinkToDoor(_SelectedObject.GetComponent<DoorScript>());
        }
      },
      new LevelEditorObject.AddSettings()
      {
        _data = "door_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_Door,
      (GameObject g) =>
      {
        if (g == null) return;
        ClearText();
        _text.Add("Toggle (T)");
        UpdateText();
      }),

    _LEO_DoorDoor = new LevelEditorObject("Door_Obstacle", _LEO_Door._update,
      new LevelEditorObject.MovementSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT_PARENT,
        _localPos = _LEO_Door._movementSettings._localPos
      },
      new LevelEditorObject.RotationSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT_PARENT
      },
      new LevelEditorObject.CopySettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT_PARENT,
        _onCopy = _LEO_Door._copySettings._onCopy
      },
      null,
      new LevelEditorObject.DeleteSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT_PARENT
      },
      null,
      _LEO_Door._textDisplayFunction)
    {
      _hide = true
    },

    _LEO_DoorBottom = new LevelEditorObject("Door_Under", _LEO_Door._update,
      new LevelEditorObject.MovementSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT_PARENT,
        _localPos = _LEO_Door._movementSettings._localPos
      },
      new LevelEditorObject.RotationSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT_PARENT
      },
      new LevelEditorObject.CopySettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT_PARENT,
        _onCopy = _LEO_Door._copySettings._onCopy
      },
      null,
      new LevelEditorObject.DeleteSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT_PARENT
      },
      null,
      _LEO_Door._textDisplayFunction)
    {
      _hide = true
    },

    _LEO_Laser = new LevelEditorObject("Laser",
      (GameObject g) =>
      {
        LevelEditorObject._UpdateFunction_Object(g);
        if (_SelectedObject == null) return;
        // Get script
        Transform target = null;
        if (_SelectedObject.name.Equals(_LEO_Laser._name)) target = _SelectedObject;
        else if (_SelectedObject.name.Equals(_LEO_LaserMachine._name)) target = _SelectedObject.parent;
        if (target == null) return;
        LaserScript ls = target.GetComponent<LaserScript>();
        // Change laser type
        if (ControllerManager.GetKey(ControllerManager.Key.T))
        {
          ls._type = ls._type == LaserScript.LaserType.ALARM ? LaserScript.LaserType.KILL : LaserScript.LaserType.ALARM;
          ls.Init();
        }
        // Change laser rotation speed
        if (ControllerManager.GetKey(ControllerManager.Key.M))
        {
          float rotAdd = 25f;
          if (ControllerManager.GetKey(ControllerManager.Key.SPACE, ControllerManager.InputMode.HOLD)) rotAdd = 10f;
          if (ControllerManager.GetKey(ControllerManager.Key.SHIFT_L, ControllerManager.InputMode.HOLD) ||
          ControllerManager.GetKey(ControllerManager.Key.SHIFT_R, ControllerManager.InputMode.HOLD)) rotAdd *= -1f;
          ls._rotationSpeed += rotAdd;
        }
      },
      new LevelEditorObject.MovementSettings(),
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "laser_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      (LevelEditorObject leo, GameObject g, Vector3 offset, Vector3 pos_use) =>
      {
        var returnString = "";
        // Check for lasers
        var ls = g.GetComponent<LaserScript>();
        if (ls != null)
        {
          returnString += "laser_" + System.Math.Round(pos_use.x, 2) + "_" + System.Math.Round(pos_use.z, 2) + "_";
          // Add rotation speed
          returnString += "rotspeed_" + ls._rotationSpeed + "_";
          // Add laser type
          returnString += "type_" + (ls._type == LaserScript.LaserType.ALARM ? "alarm" : "kill") + "_";
          // Add rotation
          returnString += "rot_" + ls.transform.localRotation.eulerAngles.y + " ";
          return returnString;
        }
        return null;
      },
      (GameObject g) =>
      {
        if (_SelectedObject == null) return;
        Transform target = null;
        if (_SelectedObject.name.Equals(_LEO_Laser._name)) target = _SelectedObject;
        else if (_SelectedObject.name.Equals(_LEO_LaserMachine._name)) target = _SelectedObject.parent;
        if (target == null) return;
        var ls = target.GetComponent<LaserScript>();
        ClearText();
        _text.Add(string.Format("Type (T): {0}", ls._type));
        _text.Add(string.Format("Rotation speed (M/Shift+M/Space+M): {0}", ls._rotationSpeed));
        UpdateText();
      }),

    _LEO_LaserMachine = new LevelEditorObject("Machine", _LEO_Laser._update,
      new LevelEditorObject.MovementSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT
      },
      new LevelEditorObject.RotationSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT
      },
      new LevelEditorObject.CopySettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT
      },
      new LevelEditorObject.AddSettings()
      {
        _data = "laser_0_0_rotspeed_0_type_kill_rot_0"
      },
      new LevelEditorObject.DeleteSettings()
      {
        _target = LevelEditorObject.TransformTarget.PARENT
      },
      null,
      _LEO_Laser._textDisplayFunction)
    {
      _hide = true
    },

    _LEO_Playerspawn = new LevelEditorObject("PlayerSpawn",
      (GameObject g) =>
      {
        LevelEditorObject._UpdateFunction_Object(g);
      },
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.191999f
      },
      new LevelEditorObject.RotationSettings(),
      null, null, null, null, null),

    _LEO_Table = new LevelEditorObject("Table", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -0.6f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "table_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_TableSmall = new LevelEditorObject("TableSmall", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "tablesmall_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_Chair = new LevelEditorObject("Chair", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -0.96f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "chair_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_BookcaseClosed = new LevelEditorObject("BookcaseClosed", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.34f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "bookcaseclosed_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_RugRectangle = new LevelEditorObject("RugRectangle", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.2f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "rugrectangle_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRotCustom,
      null),

    _LEO_BookcaseOpen = new LevelEditorObject("BookcaseOpen", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = 0f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "bookcaseopen_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRotCustom,
      null),

    _LEO_BookcaseBig = new LevelEditorObject("BookcaseBig", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.32f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "bookcasebig_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_Barrel = new LevelEditorObject("Barrel", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.35f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "barrel_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_Column = new LevelEditorObject("Column", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.37f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "column_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_ColumnBroken = new LevelEditorObject("ColumnBroken", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.35f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "columnbroken_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_Rock0 = new LevelEditorObject("Rock0", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.19f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "rock0_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_Rock1 = new LevelEditorObject("Rock1", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.3f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "rock1_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_CandelBig = new LevelEditorObject("CandelBig", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.1f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "candelbig_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRotCustom,
      null),

    _LEO_CandelTable = new LevelEditorObject("CandelTable", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -0.491f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "candeltable_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRotCustom,
      null),

    _LEO_CandelBarrel = new LevelEditorObject("CandelBarrel", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -0.5f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "candelbarrel_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRotCustom,
      null),

    _LEO_TV = new LevelEditorObject("Television", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -0.1f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "television_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRotCustom,
      null
    ),

    _LEO_Books = new LevelEditorObject("Books", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -0.5f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "books_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRotCustom,
      null
    ),

    _LEO_NavMeshBarrier = new LevelEditorObject("NavMeshBarrier", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.34f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "navmeshbarrier_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

    _LEO_Interactable = new LevelEditorObject("Interactable", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -0.5f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "interactable_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRotCustom,
      null),

    _LEO_FakeTile = new LevelEditorObject("FakeTile", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = 1.601f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "faketile_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),

     _LEO_TileWall = new LevelEditorObject("TileWall", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -3.3f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "tilewall_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null),


    _LEO_Arch = new LevelEditorObject("Arch", LevelEditorObject._UpdateFunction_Object,
      new LevelEditorObject.MovementSettings()
      {
        _localPos = -1.35f
      },
      new LevelEditorObject.RotationSettings(),
      new LevelEditorObject.CopySettings(),
      new LevelEditorObject.AddSettings()
      {
        _data = "arch_0_0"
      },
      new LevelEditorObject.DeleteSettings(),
      LevelEditorObject._SaveFunction_PosRot,
      null);
  #endregion

  static LevelEditorObject[] _Furniture = new LevelEditorObject[]
  {
    _LEO_Table,
    _LEO_TableSmall,
    _LEO_Chair,
    _LEO_RugRectangle,
    _LEO_BookcaseClosed,
    _LEO_BookcaseOpen,
    _LEO_BookcaseBig,
    _LEO_Barrel,
    _LEO_Column,
    _LEO_ColumnBroken,
    _LEO_Rock0,
    _LEO_Rock1,
    _LEO_CandelBig,
    _LEO_CandelTable,
    _LEO_CandelBarrel,
    _LEO_NavMeshBarrier,
    _LEO_Interactable,
    _LEO_FakeTile,
    _LEO_TileWall,
    _LEO_Arch,

    _LEO_TV,
    _LEO_Books,
  };

  class LevelEditorObject
  {
    public static List<LevelEditorObject> _Objects;
    public static int _Objects_iter;

    public enum Axis
    {
      Y,
      X,
      Z
    }
    public enum TransformTarget
    {
      SELF,
      PARENT,
      PARENT_PARENT,
      PARENT_PARENT_PARENT
    }
    public delegate void UpdateFunction(GameObject selection);
    public delegate string SaveFunction(LevelEditorObject leo, GameObject reference, Vector3 offset, Vector3 pos_use);

    public class MovementSettings
    {
      public TransformTarget _target;
      public Axis _axis;
      public float _localPos;
    }
    public class RotationSettings
    {
      public Axis _axis;
      public TransformTarget _target;
    }
    public class CopySettings
    {
      public TransformTarget _target;
      public UpdateFunction _onCopy;
    }
    public class DeleteSettings
    {
      public TransformTarget _target;
    }
    public class AddSettings
    {
      public string _data;
      public UpdateFunction _onAdd;
    }

    public string _name;
    public UpdateFunction _update;
    public MovementSettings _movementSettings;
    public RotationSettings _rotationSettings;
    public CopySettings _copySettings;
    public AddSettings _addSettings;
    public DeleteSettings _deleteSettings;
    public SaveFunction _saveFunction;
    public UpdateFunction _textDisplayFunction;
    public System.Action _onLoadGame;
    public bool _hide; // If true, when cycling through objects, will be skipped

    public LevelEditorObject(string name, UpdateFunction onSelect, MovementSettings movementSettings, RotationSettings rotationSettings, CopySettings copySettings, AddSettings addSettings, DeleteSettings deleteSettings, SaveFunction saveFunction, UpdateFunction textDisplayFunction)
    {
      // Add to objects list
      if (_Objects == null) _Objects = new List<LevelEditorObject>();
      _Objects.Add(this);
      // Initialize varialbes
      _name = name;
      _update = onSelect;
      _movementSettings = movementSettings;
      _rotationSettings = rotationSettings;
      _copySettings = copySettings;
      _addSettings = addSettings;
      _deleteSettings = deleteSettings;
      _saveFunction = saveFunction;
      _textDisplayFunction = textDisplayFunction;
    }

    /// <summary>
    /// Returns the current level editor object via LevelEditorObject._Object_iter
    /// </summary>
    /// <returns></returns>
    public static LevelEditorObject GetCurrentObject()
    {
      return _Objects[_Objects_iter];
    }

    /// <summary>
    /// Increments the current level editor object by amount
    /// </summary>
    /// <param name="amount"></param>
    public static void IncrementIter(int amount, bool skip = false)
    {
      _IsLinking = false;

      // If old mode is tile, deselesct tiles
      if (GetCurrentObject()._name.Equals(_LEO_Tile._name)) DeselectTiles();
      // If old mode is enemy, hide menu
      if (GetCurrentObject()._name.Equals(_LEO_Enemy._name)) EditorMenus._Menu_Infos_Enemy.gameObject.SetActive(false);
      if (GetCurrentObject()._name.Equals(_LEO_Door._name)) EditorMenus._Menu_Infos_Door.gameObject.SetActive(false);
      if (GetCurrentObject()._name.Equals(_LEO_Button._name)) EditorMenus._Menu_Infos_Button.gameObject.SetActive(false);
      if (GetCurrentObject()._name.Equals(_LEO_Goal._name)) EditorMenus._Menu_Infos_Goal.gameObject.SetActive(false);
      if (GetCurrentObject()._name.Equals(_LEO_Tile._name)) EditorMenus._Menu_Infos_Tile.gameObject.SetActive(false);

      // Else, move ring
      else _Ring.position = new Vector3(0f, -100f, 0f);
      // Iterate into a range
      _Objects_iter += amount;
      if (_Objects_iter < 0) _Objects_iter = _Objects.Count - 1;
      else _Objects_iter %= _Objects.Count;
      // Set editor UI text
      ClearText();
      UpdateText();
      // Check skip
      if (skip && GetCurrentObject()._hide) IncrementIter(amount, true);

      // Check enemy selected
      if (GetCurrentObject()._name.Equals(_LEO_Enemy._name)) EditorMenus._Menu_Infos_Enemy.gameObject.SetActive(true);
      if (GetCurrentObject()._name.Equals(_LEO_Door._name)) EditorMenus._Menu_Infos_Door.gameObject.SetActive(true);
      if (GetCurrentObject()._name.Equals(_LEO_Button._name)) EditorMenus._Menu_Infos_Button.gameObject.SetActive(true);
      if (GetCurrentObject()._name.Equals(_LEO_Goal._name)) EditorMenus._Menu_Infos_Goal.gameObject.SetActive(true);
      if (GetCurrentObject()._name.Equals(_LEO_Tile._name)) EditorMenus._Menu_Infos_Tile.gameObject.SetActive(true);
    }

    public static void SetIterOnName(string name)
    {
      int loops = 0;
      while (!GetCurrentObject()._name.Equals(name) && loops++ < 100)
        IncrementIter(1);
    }

    // OnMove delegate Functions to use; lambdas
    public static UpdateFunction _UpdateSelectFunction_Tile = (GameObject selection) =>
    {
      // Select
      if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.HOLD))
      {
        Select(selection);
      }
      // Deselect
      if (ControllerManager.GetMouseInput(2, ControllerManager.InputMode.DOWN))
      {
        DeselectTiles();
      }
      // Toggle
      if (ControllerManager.GetMouseInput(1, ControllerManager.InputMode.DOWN) && _Selected_Tiles != null)
        GameScript._Singleton.StartCoroutine(Tile.LerpPositions(_Selected_Tiles.ToArray(), 0f, false));
    }
    ,
    _UpdateFunction_Object = (GameObject selection) =>
    {
      // Select
      //if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.DOWN) && selection.name.Equals(GetCurrentObject()._name))
      //{
      //  Select(selection);
      //}
      // Deselect
      if (ControllerManager.GetMouseInput(1, ControllerManager.InputMode.DOWN))
      {
        _SelectedObject = null;
        _Ring.position = new Vector3(0f, -100f, 0f);
      }
    }
    ,
    _UpdateFunction_CustomEntityUI = (GameObject selection) =>
    {
      if (_SelectedObject == null) return;
      // Remove connections
      if (ControllerManager.GetKey(ControllerManager.Key.U))
      {
        var cui = selection.GetComponent<CustomEntityUI>();
        foreach (var activate in cui._activate)
          if (activate != null && activate.gameObject.name == "Door")
            ((DoorScript)activate)._button = null;
        cui._activate = new CustomEntity[0];
      }
      // Link CustomEntityUI with CustomEntity
      if (ControllerManager.GetKey(ControllerManager.Key.L))
      {
        _IsLinking = !_IsLinking;

        _CurrentMode = EditorMode.NONE;
      }
      if (_IsLinking)
      {
        // Get mouse pos
        RaycastHit h;
        Physics.SphereCast(GameResources._Camera_Main.ScreenPointToRay(ControllerManager.GetMousePosition()), 0.25f, out h);
        Vector3 mousePos = h.point;
        mousePos.y = -1f;
        _LineRenderers[1].positionCount = 2;
        _LineRenderers[1].SetPositions(new Vector3[] { selection.transform.position, mousePos });
        // Check for selection
        if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.DOWN))
        {
          CustomEntity ce = h.collider.transform.parent.parent.GetComponent<CustomEntity>();
          if (ce != null)
          {
            var custom_entity_ui = selection.GetComponent<CustomEntityUI>();
            custom_entity_ui.AddToActivateArray(ce);

            if (ce.name == "Door")
            {
              ((DoorScript)ce)._button = custom_entity_ui;
            }
          }
          _IsLinking = false;
        }
      }
    };

    public static string _SaveFunction_Pos(LevelEditorObject self, Vector3 pos_use)
    {
      return self._addSettings._data.Split('_')[0] + "_" + System.Math.Round(pos_use.x, 2) + "_" + System.Math.Round(pos_use.z, 2);
    }
    public static string _SaveFunction_Rot(GameObject g)
    {
      return "rot_" + g.transform.localRotation.eulerAngles.y;
    }
    public static SaveFunction _SaveFunction_PosRot = (LevelEditorObject leo, GameObject g, Vector3 offset, Vector3 pos_use) =>
    {
      if (!g.name.Equals(leo._name)) return null;
      string returnString = "";
      returnString += _SaveFunction_Pos(leo, pos_use) + "_";
      returnString += _SaveFunction_Rot(g) + " ";
      return returnString;
    };
    public static SaveFunction _SaveFunction_PosRotCustom = (LevelEditorObject leo, GameObject g, Vector3 offset, Vector3 pos_use) =>
    {
      if (!g.name.Equals(leo._name)) return null;
      var returnString = "";
      returnString += LevelEditorObject._SaveFunction_Pos(leo, pos_use) + "_" +
        LevelEditorObject._SaveFunction_Rot(g);
      var co = g.GetComponent<CustomObstacle>();
      if (co != null)
        returnString += $"_co_{co._type}.{co._index}.{co._index2}";
      returnString += " ";
      return returnString;
    };

    public static SaveFunction _SaveFunction_Door = (LevelEditorObject leo, GameObject g, Vector3 offset, Vector3 pos_use) =>
    {
      if (!g.name.Equals(leo._name)) return null;
      var returnString = "";
      returnString += LevelEditorObject._SaveFunction_Pos(leo, pos_use) + "_" +
        LevelEditorObject._SaveFunction_Rot(g);
      var doorScript = g.GetComponent<DoorScript>();
      if (doorScript != null)
      {
        if (doorScript._hasButton) return null;
        if (doorScript._trigger_enemies == null || doorScript._trigger_enemies.Count == 0)
        {

        }
        else
        {
          returnString += $"_enemypos_{doorScript.GetRegisteredEnemies()}";
        }
        returnString += "_open_" + (doorScript._opened ? "1" : "0") + "_";

      }
      returnString += " ";
      return returnString;
    };

    public static void Select(GameObject selection)
    {
      if (_CurrentMode == EditorMode.MOVE || _IsLinking)
        return;

      // Check for tile selection
      if (selection.name.Equals(_LEO_Tile._name))
      {
        SelectTile(Tile.GetTile(selection));
        return;
      }
      if (GetCurrentObject()._name.Equals(_LEO_Tile._name)) return;
      // Normal selection
      _SelectedObject = selection.transform;
      ClearText();
      UpdateText();
      _Ring.position = _SelectedObject.position;
    }

    public static Transform GetTransformTarget(TransformTarget target)
    {
      Transform return_target = null;
      if (target == TransformTarget.SELF)
        return_target = _SelectedObject;
      else if (target == TransformTarget.PARENT)
        return_target = _SelectedObject.parent;
      else if (target == TransformTarget.PARENT_PARENT)
        return_target = _SelectedObject.parent.parent;
      else if (target == TransformTarget.PARENT_PARENT_PARENT)
        return_target = _SelectedObject.parent.parent.parent;
      return return_target;
    }
  }

  public static float _EditorSwitchTime;

  static Transform _SelectedObject;
  static SnapToGrid _SnapToGrid = SnapToGrid.COMPLEX;
  enum SnapToGrid
  {
    NONE,
    SIMPLE,
    COMPLEX
  }
  static float _CameraZoom;
  public static void HandleInput()
  {
    if (_LoadingMap) return;

    if (!GameScript._Focused) return;

    // Use line renderers to visualize connections
    try
    {
      LineRenderer_Update();
    }
    catch (System.Exception e)
    {
      Debug.LogError("Cause exception at LineRenderer_Update() => " + e.ToString());
    }
    // Set text based on object
    if (LevelEditorObject.GetCurrentObject()._textDisplayFunction != null)
      LevelEditorObject.GetCurrentObject()._textDisplayFunction(_SelectedObject != null ? _SelectedObject.gameObject : null);
    // Move Camera with mouse
    Vector2 mousepos = ControllerManager.GetMousePosition();
    /*if (mousepos.x > 0f && mousepos.x <= Screen.safeArea.width && mousepos.y > 0f && mousepos.y <= Screen.safeArea.height)
    {
      Vector3 newPos = GameResources._Camera_Main.ScreenPointToRay(mousepos).GetPoint(10f);
      newPos.y = 16f;// _CameraZoom;

      Vector3 dis = (newPos - GameResources._Camera_Main.transform.position);
      Vector2 minDis = new Vector2(5f, 3f),
        intensity = new Vector2(0f, 0f);
      if (Mathf.Abs(dis.x) > minDis.x)
        intensity.x = Mathf.LerpUnclamped(0f, 0.6f, Mathf.Abs(dis.x) - minDis.x);
      if (Mathf.Abs(dis.z) > minDis.y)
        intensity.y = Mathf.LerpUnclamped(0f, 0.5f, Mathf.Abs(dis.z) - minDis.y);
      float maxDistance = _Width;
      // Get middle tile and check distances
      Vector3 middlePosition = Tile.GetTile(_Width / 2, _Height / 2)._tile.transform.position;
      middlePosition.y = GameResources._Camera_Main.transform.position.y;
      //if (Mathf.Abs(middlePosition.x - newPos.x) > maxDistance)
      //  newPos.x = middlePosition.x + maxDistance * Mathf.Sign(middlePosition.x + newPos.x);
      //if (Mathf.Abs(middlePosition.z - newPos.z) > maxDistance)
      //  newPos.z = middlePosition.z + maxDistance * Mathf.Sign(middlePosition.z + newPos.z);
      Vector3 movePos = (newPos - GameResources._Camera_Main.transform.position) * Time.deltaTime * 3f;
      movePos.x *= intensity.x;
      movePos.z *= intensity.y;
      GameResources._Camera_Main.transform.position += movePos;
    }*/

    // Move camera with arrows
    if (ControllerManager.GetKey(ControllerManager.Key.ARROW_U, ControllerManager.InputMode.HOLD))
    {
      var movePos = new Vector3(0f, 0f, 1f) * Time.deltaTime * 10f;
      GameResources._Camera_Main.transform.position += movePos;
    }
    if (ControllerManager.GetKey(ControllerManager.Key.ARROW_D, ControllerManager.InputMode.HOLD))
    {
      var movePos = -new Vector3(0f, 0f, 1f) * Time.deltaTime * 10f;
      GameResources._Camera_Main.transform.position += movePos;
    }
    if (ControllerManager.GetKey(ControllerManager.Key.ARROW_L, ControllerManager.InputMode.HOLD))
    {
      var movePos = -new Vector3(1f, 0f, 0f) * Time.deltaTime * 10f;
      GameResources._Camera_Main.transform.position += movePos;
    }
    if (ControllerManager.GetKey(ControllerManager.Key.ARROW_R, ControllerManager.InputMode.HOLD))
    {
      var movePos = new Vector3(1f, 0f, 0f) * Time.deltaTime * 10f;
      GameResources._Camera_Main.transform.position += movePos;
    }

    // Get raycast info
    RaycastHit raycast_info;
    Ray r = GameResources._Camera_Main.ScreenPointToRay(mousepos);
    Physics.SphereCast(r, 0.2f, out raycast_info);
    if (raycast_info.collider == null) return;

    // Save map
    if (ControllerManager.GetKey(ControllerManager.Key.SPACE, ControllerManager.InputMode.HOLD))
      if (ControllerManager.GetKey(ControllerManager.Key.S))
        SaveFileOverwrite(SaveMap());

    // Test and save map
    if (ControllerManager.GetKey(ControllerManager.Key.F1) && Time.unscaledTime - _EditorSwitchTime > 0.5f)
    {
      GameScript._EditorEnabled = false;
      //if (_EditorEnabled) StartCoroutine(TileManager.EditorEnabled());
      string mapdata = TileManager.SaveMap();
      TileManager.EditorDisabled(mapdata);
      TileManager.SaveFileOverwrite(mapdata);

      // Show menu
      EditorMenus._Menu_EditorTesting.gameObject.SetActive(true);
    }

    // Basic editor shortcuts
    {
      // Switch mode
      //if (ControllerManager.GetKey(ControllerManager.Key.Q)) LevelEditorObject.IncrementIter(-1, true);
      //if (ControllerManager.GetKey(ControllerManager.Key.W)) LevelEditorObject.IncrementIter(1, true);
      /*if (ControllerManager.GetKey(ControllerManager.Key.SPACE, ControllerManager.InputMode.HOLD))
      {
      // Save
      if (ControllerManager.GetKey(ControllerManager.Key.S)) SaveFileOverwrite(SaveMap());
      // Save as new map
      if (ControllerManager.GetKey(ControllerManager.Key.INSERT))
      {
        string mapdata = SaveMap();
        // Copy the map to the end of the maps array
        Levels.CopyMap(Levels._CurrentLevelIndex);
        // Set current map to the last map
        Levels._CurrentLevelIndex++;
        // Disable editor
        EditorDisabled(mapdata);
        GameScript._EditorEnabled = false;
      }
      // Delete map
      if (ControllerManager.GetKey(ControllerManager.Key.DELETE))
      {
        // Save to clipboard in case
        string olddata = SaveMap();
        // Delete level
        Levels.DeleteLevel(Levels._CurrentLevelIndex);
        // Set current map
        //Levels._CurrentLevelIndex--;
        // Disable editor
        EditorDisabled(olddata);
        GameScript._EditorEnabled = false;
        // Return to level select menu
        //GameScript.TogglePause();
        //Menu._CurrentMenu._menuActions[3]();
      }
      }*/
      // Move camera to playerspawn
      if (ControllerManager.GetKey(ControllerManager.Key.PERIOD_NUMPAD) || ControllerManager.GetKey(ControllerManager.Key.PERIOD))
        GameResources._Camera_Main.transform.position = new Vector3(PlayerspawnScript._PlayerSpawns[0].transform.position.x, GameResources._Camera_Main.transform.position.y, PlayerspawnScript._PlayerSpawns[0].transform.position.z);
      // Snap to grid
      if (ControllerManager.GetKey(ControllerManager.Key.COMMA))
      {
        var textmesh = GameObject.Find("Snap_Precision").transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();

        if (_SnapToGrid == SnapToGrid.COMPLEX)
        {
          _SnapToGrid = SnapToGrid.SIMPLE;
          textmesh.text = "[,] Snap precison: Hard";
        }
        else if (_SnapToGrid == SnapToGrid.NONE)
        {
          _SnapToGrid = SnapToGrid.COMPLEX;
          textmesh.text = "[,] Snap precison: Minimal";

        }
        else if (_SnapToGrid == SnapToGrid.SIMPLE)
        {
          _SnapToGrid = SnapToGrid.NONE;
          textmesh.text = "[,] Snap precison: Free";
        }
      }
      // Increment mouseID
      //if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.UP))
      // Change mode to current click
      if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.DOWN))
      {

        _MouseID++;

        // Make sure not in move mode when selecting a new object
        //_CurrentMode = EditorMode.NONE;
        //
        LevelEditorObject currentObj = LevelEditorObject.GetCurrentObject();
        bool found = false;
        List<KeyValuePair<GameObject, int>> hiddenLayer = new List<KeyValuePair<GameObject, int>>();
        // Loop until hits the floor or finds a new mode
        int loops = 0;
        GameObject got_obj = null;
        while (true)
        {
          if (++loops > 50) break;
          // Try to find objects
          var save_collider = raycast_info.collider;
          Physics.SphereCast(r, 0.2f, out raycast_info);
          got_obj = raycast_info.collider.gameObject;

          // If did not find collider or finds the floor, end
          if (raycast_info.collider == null || raycast_info.collider.name.Equals("Floor")) break;

          // Check for til
          if (raycast_info.collider.name == "Tile")
          {
            got_obj = save_collider.gameObject;
            break;
          }

          if (_LEO_Tile._name.Equals(raycast_info.collider.name))
            continue;
          // If finds same mode object, hide it and try again
          if (currentObj._name.Equals(raycast_info.collider.name))
          {
            hiddenLayer.Add(new KeyValuePair<GameObject, int>(raycast_info.collider.gameObject, raycast_info.collider.gameObject.layer));
            raycast_info.collider.gameObject.layer = 2;

            // take anyways
            found = true;
            continue;
          }
          // Else, found a new mode
          found = true;
          break;
        }
        // Unhide objects
        foreach (var pair in hiddenLayer)
          pair.Key.layer = pair.Value;
        if (found && _CurrentMode == EditorMode.NONE && !_IsLinking)
        {
          // Change mode to new object
          LevelEditorObject.SetIterOnName(got_obj.name);

          // Select
          _Ring.position = new Vector3(0f, -100f, 0f);
          LevelEditorObject.Select(got_obj);
          _Ring.position = _SelectedObject.position;

          // Check for enemy visual
          if (_SelectedObject.name == _LEO_EnemyVisual._name)
          {
            LevelEditorObject.Select(_SelectedObject.parent.parent.gameObject);
            LevelEditorObject.SetIterOnName(_SelectedObject.name);
            _Ring.position = _SelectedObject.position;
          }

          // Check for door under
          if (_SelectedObject.name == _LEO_DoorBottom._name || _SelectedObject.name == _LEO_DoorDoor._name)
          {
            LevelEditorObject.Select(_SelectedObject.parent.parent.gameObject);
            LevelEditorObject.SetIterOnName(_SelectedObject.name);
            _Ring.position = _SelectedObject.position;
          }
        }
      }
    }
    // Move pointer
    if (_Pointer != null)
    {
      Vector3 pos = raycast_info.point;
      _Pointer.position = pos;
    }

    var obj = LevelEditorObject.GetCurrentObject();
    // Check if object selected
    if (raycast_info.collider != null)
      obj?._update(raycast_info.collider.gameObject);
    obj = LevelEditorObject.GetCurrentObject();

    if (_SelectedObject != null)
    {
      LevelEditorObject.MovementSettings movementSettings = obj._movementSettings;

      // Check move
      if (_CurrentMode == EditorMode.MOVE)
      {

        // Check leaving move mode
        if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.DOWN))
        {
          _CurrentMode = EditorMode.NONE;
          return;
        }

        // Select the transform to move
        var move_target = LevelEditorObject.GetTransformTarget(movementSettings._target);

        // Set the position to move the object to the Pointer's position
        var movePos = _Pointer.position;

        // Snap position to grid
        var gridSize = 2.5f / (_SnapToGrid == SnapToGrid.SIMPLE ? 4f : _SnapToGrid == SnapToGrid.COMPLEX ? 20f : 50f);
        movePos.x = Mathf.RoundToInt(movePos.x / gridSize) * gridSize;
        movePos.z = Mathf.RoundToInt(movePos.z / gridSize) * gridSize;// + (Tile.GetTile(_Width / 2, _Height / 2 + 5)._tile.transform.position.z) + gridSize;

        // Set obj position
        move_target.position = movePos;

        // Set obj local pos per object's _axis and _localPos
        var localPos = move_target.localPosition;
        if (movementSettings._axis == LevelEditorObject.Axis.X)
          localPos.x = movementSettings._localPos;
        else if (movementSettings._axis == LevelEditorObject.Axis.Y)
          localPos.y = movementSettings._localPos;
        else if (movementSettings._axis == LevelEditorObject.Axis.Z)
          localPos.z = movementSettings._localPos;
        move_target.localPosition = localPos;

        // Move ring for visual
        _Ring.position = move_target.position;
      }
      // Check set to move mode
      else
      {
        // Check for key and if current object can move
        if (ControllerManager.GetKey(ControllerManager.Key.G) && movementSettings != null)
          _CurrentMode = EditorMode.MOVE;
        /*/ Check for custom obstacles
        if (CanCustomObject())
        {
          var co = _SelectedObject.GetComponent<CustomObstacle>();
          // Add / remove
          if (ControllerManager.GetKey(ControllerManager.Key.B))
          {
            if (co == null)
              _SelectedObject.gameObject.AddComponent<CustomObstacle>();
            else
              GameObject.DestroyImmediate(_SelectedObject.gameObject.GetComponent<CustomObstacle>());
            ClearText();
            UpdateText();
          }
          var mod = 1;
          if (ControllerManager.GetKey(ControllerManager.Key.CONTROL_LEFT, ControllerManager.InputMode.HOLD)) mod = -1;
          // Change index
          if (ControllerManager.GetKey(ControllerManager.Key.N) && co != null)
          {
            co._index += mod;
            ClearText();
            UpdateText();
          }
          // Change index2
          if (ControllerManager.GetKey(ControllerManager.Key.M) && co != null)
          {
            co._index2 += mod;
            ClearText();
            UpdateText();
          }
          // Change type
          if (ControllerManager.GetKey(ControllerManager.Key.COMMA) && co != null)
          {
            co._type = (CustomObstacle.InteractType)((((int)co._type) + 1) % 5);
            co._index = co._index;
            ClearText();
            UpdateText();
          }
        }*/
      }
      LevelEditorObject.RotationSettings rotationSettings = obj._rotationSettings;
      // Check rotate
      if (rotationSettings != null)
      {
        // Set amount to rotate
        float rotate_degree = Mathf.RoundToInt(UnityEngine.InputSystem.Mouse.current.scroll.y.ReadValue() * 0.085f);
        if (ControllerManager.GetKey(ControllerManager.Key.R))
          rotate_degree = 45f;
        if (ControllerManager.GetKey(ControllerManager.Key.SHIFT_L, ControllerManager.InputMode.HOLD)) rotate_degree /= 3f;
        // Select the transform to rotate
        Transform rotate_target = LevelEditorObject.GetTransformTarget(rotationSettings._target);
        // Rotate the appropriate axis
        Vector3 rotate_axis = Vector3.zero;
        if (rotationSettings._axis == LevelEditorObject.Axis.X)
          rotate_axis.x = 1f;
        else if (rotationSettings._axis == LevelEditorObject.Axis.Y)
          rotate_axis.y = 1f;
        else if (rotationSettings._axis == LevelEditorObject.Axis.Z)
          rotate_axis.z = 1f;
        // Rotate
        rotate_target.Rotate(rotate_axis * rotate_degree);
      }

      // Check copy
      var copySettings = obj._copySettings;
      if (copySettings != null && ControllerManager.GetKey(ControllerManager.Key.C))
        LevelEditor_Copy(copySettings);

      // Check delete
      LevelEditorObject.DeleteSettings deleteSettings = obj._deleteSettings;
      if (deleteSettings != null && ControllerManager.GetKey(ControllerManager.Key.DELETE))
      {
        // Select the transform to delete
        Transform delete_target = LevelEditorObject.GetTransformTarget(deleteSettings._target);
        // Delete it
        GameObject.Destroy(delete_target.gameObject);
        _CurrentMode = EditorMode.NONE;
        _SelectedObject = null;
        _Ring.position = new Vector3(0f, -100f, 0f);
      }
    }
    // Check add
    /*LevelEditorObject.AddSettings addSettings = obj._addSettings;
    if (addSettings != null && ControllerManager.GetKey(ControllerManager.Key.A))
    {
      GameObject loaded = LoadObject(addSettings._data);
      // Fire onAdd function
      addSettings._onAdd?.Invoke(loaded);
      // Set move mode
      _SelectedObject = loaded.transform;
      _CurrentMode = EditorMode.MOVE;
      LevelEditorObject.SetIterOnName(_SelectedObject.name);
    }*/
    /*/ Handle numpad shortcuts
    for (int i = 0; i < _Shortcurts.Length; i++)
    {
      switch (i + 1)
      {
        case (1):
          if (ControllerManager.GetKey(ControllerManager.Key.ONE))
            LevelEditorObject.SetIterOnName(_Shortcurts[i]._name);
          break;
        case (2):
          if (ControllerManager.GetKey(ControllerManager.Key.TWO))
            LevelEditorObject.SetIterOnName(_Shortcurts[i]._name);
          break;
        case (3):
          if (ControllerManager.GetKey(ControllerManager.Key.THREE))
            LevelEditorObject.SetIterOnName(_Shortcurts[i]._name);
          break;
        case (4):
          if (ControllerManager.GetKey(ControllerManager.Key.FOUR))
            LevelEditorObject.SetIterOnName(_Shortcurts[i]._name);
          break;
        case (5):
          if (ControllerManager.GetKey(ControllerManager.Key.FIVE))
            LevelEditorObject.SetIterOnName(_Shortcurts[i]._name);
          break;
        case (6):
          if (ControllerManager.GetKey(ControllerManager.Key.SIX))
            LevelEditorObject.SetIterOnName(_Shortcurts[i]._name);
          break;
        case (7):
          if (ControllerManager.GetKey(ControllerManager.Key.SEVEN))
            LevelEditorObject.SetIterOnName(_Shortcurts[i]._name);
          break;
        case (8):
          if (ControllerManager.GetKey(ControllerManager.Key.EIGHT))
            LevelEditorObject.SetIterOnName(_Shortcurts[i]._name);
          break;
        case (9):
          if (ControllerManager.GetKey(ControllerManager.Key.NINE))
            LevelEditorObject.SetIterOnName(_Shortcurts[i]._name);
          break;
      }

    }*/

    if (ControllerManager.GetKey(ControllerManager.Key.ONE))
      EditorMenus._Menu_Object_Select.GetChild(1).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.TWO))
      EditorMenus._Menu_Object_Select.GetChild(2).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.THREE))
      EditorMenus._Menu_Object_Select.GetChild(3).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.FOUR))
      EditorMenus._Menu_Object_Select.GetChild(4).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.FIVE))
      EditorMenus._Menu_Object_Select.GetChild(5).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.SIX))
      EditorMenus._Menu_Object_Select.GetChild(6).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.SEVEN))
      EditorMenus._Menu_Object_Select.GetChild(7).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.EIGHT))
      EditorMenus._Menu_Object_Select.GetChild(8).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.NINE))
      EditorMenus._Menu_Object_Select.GetChild(9).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.ZERO))
      EditorMenus._Menu_Object_Select.GetChild(10).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.O))
      EditorMenus._Menu_Object_Select.GetChild(11).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.MINUS))
      EditorMenus._Menu_Object_Select.GetChild(12).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.EQUALS))
      EditorMenus._Menu_Object_Select.GetChild(13).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    if (ControllerManager.GetKey(ControllerManager.Key.BACKSLASH))
      EditorMenus._Menu_Object_Select.GetChild(14).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
    //if (ControllerManager.GetKey(ControllerManager.Key.BACKSPACE))
    //  EditorMenus._Menu_Object_Select.GetChild(14).GetComponent<UnityEngine.UI.Button>().onClick?.Invoke();
  }

  static void LevelEditor_Copy(LevelEditorObject.CopySettings copySettings)
  {
    // Select the transform to copy
    var copy_target = LevelEditorObject.GetTransformTarget(copySettings._target);

    // Copy the item
    var copy = GameObject.Instantiate(copy_target.gameObject).transform;

    // Initialize local variables
    copy.name = copy_target.name;
    copy.parent = copy_target.parent;
    copy.rotation = copy_target.rotation;
    copy.localScale = copy_target.localScale;
    copy.position = copy_target.position;

    // Fire onCopy function
    copySettings._onCopy?.Invoke(copy.gameObject);

    // Select and move the object
    _SelectedObject = copy;
    LevelEditorObject.SetIterOnName(_SelectedObject.name);
    _CurrentMode = EditorMode.MOVE;
  }

  static LevelEditorObject[] _Shortcurts = new LevelEditorObject[]
  {
    _LEO_Enemy,
    _LEO_Barrel,
    _LEO_BookcaseBig,
    _LEO_Table,
    _LEO_Door,
    _LEO_CandelBig
  };

  static string[] _ObjectNames;

  static List<Tile> _Selected_Tiles;

  static bool TileIsSelected(Tile tile)
  {
    if (_Selected_Tiles == null) return false;
    foreach (var selected_tile in _Selected_Tiles)
    {
      if (selected_tile._id != tile._id) continue;
      return true;
    }
    return false;
  }

  static void ChangeEnemyType(EnemyScript script, MeshRenderer changeColor, int change = 0)
  {
    // Change enemy type
    int currentType = 0;
    if (script._itemLeft == GameScript.ItemManager.Items.KNIFE) currentType = 1;
    else if (script._itemLeft == GameScript.ItemManager.Items.PISTOL) currentType = 2;
    else if (script._itemLeft == GameScript.ItemManager.Items.PISTOL_SILENCED) currentType = 3;
    else if (script._itemLeft == GameScript.ItemManager.Items.REVOLVER) currentType = 4;
    else if (script._itemLeft == GameScript.ItemManager.Items.SHOTGUN_PUMP) currentType = 5;
    else if (script._itemLeft == GameScript.ItemManager.Items.GRENADE_HOLD) currentType = 6;
    else if (script._itemLeft == GameScript.ItemManager.Items.ROCKET_LAUNCHER) currentType = 7;
    else if (script._itemLeft == GameScript.ItemManager.Items.BAT) currentType = 8;
    currentType += change;

    currentType %= 9;

    // Exclude weird types
    if (currentType == 0) currentType++;
    if (currentType == 5) currentType++;
    //if (currentType == 6) currentType++;

    script._itemLeft = script._itemRight = GameScript.ItemManager.Items.NONE;
    switch (currentType)
    {
      case (0):
        script._itemLeft = GameScript.ItemManager.Items.NONE;
        changeColor.material.color = Color.white;
        break;
      case (1):
        script._itemLeft = GameScript.ItemManager.Items.KNIFE;
        changeColor.material.color = Color.green;
        break;
      case (2):
        script._itemLeft = GameScript.ItemManager.Items.PISTOL;
        changeColor.material.color = Color.magenta;
        break;
      case (3):
        script._itemLeft = GameScript.ItemManager.Items.PISTOL_SILENCED;
        changeColor.material.color = Color.magenta / 3f;
        break;
      case (4):
        script._itemLeft = GameScript.ItemManager.Items.REVOLVER;
        script._itemRight = GameScript.ItemManager.Items.REVOLVER;
        changeColor.material.color = (Color.red + Color.yellow) / 3f;
        break;
      case (5):
        script._itemLeft = GameScript.ItemManager.Items.SHOTGUN_PUMP;
        changeColor.material.color = Color.gray * 1.5f;
        break;
      case (6):
        script._itemLeft = GameScript.ItemManager.Items.GRENADE_HOLD;
        changeColor.material.color = Color.red + Color.yellow;
        break;
      case (7):
        script._itemLeft = GameScript.ItemManager.Items.ROCKET_LAUNCHER;
        changeColor.material.color = Color.yellow;
        break;
      case (8):
        script._itemLeft = GameScript.ItemManager.Items.BAT;
        changeColor.material.color = Color.gray * 1.5f;
        break;
    }
  }

  static GameObject GiveEnemyVisual(ref Transform controller)
  {
    GameObject controller_visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    controller_visual.GetComponent<Collider>().isTrigger = true;
    controller_visual.name = "Enemy_visual";
    controller_visual.transform.parent = controller;
    controller_visual.GetComponent<MeshRenderer>().sharedMaterial.color = Color.red;
    controller_visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
    controller_visual.transform.localPosition = Vector3.zero + new Vector3(0f, 1.5f, 0f);
    // Enable waypoint renderers
    Transform path = controller.parent.GetChild(1);
    for (int i = 0; i < path.childCount; i++)
    {
      Transform waypoint = path.GetChild(i);
      waypoint.gameObject.SetActive(true);
      waypoint.gameObject.GetComponent<MeshRenderer>().enabled = true;
      // Enable lookpoint renderers
      for (int u = 0; u < waypoint.childCount; u++)
      {
        Transform lookPoint = waypoint.GetChild(u);
        lookPoint.gameObject.SetActive(true);
        lookPoint.gameObject.GetComponent<MeshRenderer>().enabled = true;
      }
    }
    return controller_visual;
  }

  static void LineRenderer_Update()
  {
    // Clear lines
    LineRenderer_Clear();
    // Check for selected object
    if (_SelectedObject == null) return;
    // Switch per object name
    switch (_SelectedObject.name)
    {
      case ("Enemy"):
        var path = _SelectedObject.GetChild(1);
        var positions = new List<Vector3>();
        positions.Add(_SelectedObject.position + new Vector3(0.01f, 0f, 0f));
        for (var i = 0; i < path.childCount; i++)
        {
          var path_point = path.GetChild(i);
          positions.Add(path_point.position + new Vector3(0f, 0.5f, 0f));

          // Add waypoint
          for (var u = 0; u < path_point.childCount; u++)
          {
            var path_lookpoint = path_point.GetChild(u);
            positions.Add(path_lookpoint.position + new Vector3(0f, 0.5f, 0f));
            positions.Add(path_point.position + new Vector3(0f, 0.5f, 0f));
          }

        }
        _LineRenderers[0].positionCount = positions.Count;
        _LineRenderers[0].SetPositions(positions.ToArray());
        break;
      case ("Waypoint"):

        for (int i = 0; i < _SelectedObject.childCount; i++)
        {
          _LineRenderers[i].positionCount = 2;
          _LineRenderers[i].SetPositions(new Vector3[] { _SelectedObject.position + new Vector3(0f, 0.5f, 0f), _SelectedObject.GetChild(i).position + new Vector3(0f, 0.5f, 0f) });
        }
        break;
      case ("Lookpoint"):

        _LineRenderers[0].positionCount = 2;
        _LineRenderers[0].SetPositions(new Vector3[] { _SelectedObject.position + new Vector3(0f, 0.5f, 0f), _SelectedObject.parent.position + new Vector3(0f, 0.5f, 0f) });
        break;
      case ("Button"):
        int iter = 0;
        foreach (var c in _SelectedObject.GetComponent<CustomEntityUI>()._activate)
        {
          if (c == null || iter > _LineRenderers.Length - 1) continue;
          positions = new List<Vector3>();
          positions.Add(_SelectedObject.position);
          positions.Add(c.transform.position);
          _LineRenderers[iter].positionCount = positions.Count;
          _LineRenderers[iter++].SetPositions(positions.ToArray());
        }
        break;
      case ("Goal"):
        iter = 0;
        foreach (var c in _SelectedObject.parent.GetComponent<CustomEntityUI>()._activate)
        {
          if (c == null || iter > _LineRenderers.Length - 1) continue;
          positions = new List<Vector3>();
          positions.Add(_SelectedObject.parent.position);
          positions.Add(c.transform.position);
          _LineRenderers[iter].positionCount = positions.Count;
          _LineRenderers[iter++].SetPositions(positions.ToArray());
        }
        break;
    }
  }

  static void LineRenderer_Clear()
  {
    if (_LineRenderers == null) return;
    foreach (LineRenderer r in _LineRenderers)
    {
      r.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });
      r.positionCount = 0;
    }
  }

  static void DeselectTiles(bool changeColor = true)
  {
    if (_Selected_Tiles == null) return;
    if (changeColor)
      foreach (Tile t in _Selected_Tiles)
      {
        if (!t._toggled) t.ChangeColor(SceneThemes._Theme._tileColorUp);
        else t.ChangeColor(SceneThemes._Theme._tileColorDown);
        t._lastMouseID = -1;
      }
    _Selected_Tiles = null;
  }

  static void SelectTile(Tile selection)
  {
    if (selection != null)
    {
      if (selection._lastMouseID == _MouseID)
        return;
      // Init array if null
      if (_Selected_Tiles == null) _Selected_Tiles = new List<Tile>();
      // Check if already selected or not
      if (TileIsSelected(selection))
      {
        _Selected_Tiles.Remove(selection);
        if (!selection._toggled) selection.ChangeColor(SceneThemes._Theme._tileColorUp);
        else selection.ChangeColor(SceneThemes._Theme._tileColorDown);
      }
      else
      {
        _Selected_Tiles.Add(selection);
        selection.ChangeColor(Color.green);
      }
      selection._lastMouseID = _MouseID;
    }
  }

  // Get gameobject for map preview
  public static Transform GetMapPreview(string mapData)
  {
    // Create base objects
    Transform container = new GameObject("MapContainer").transform,
      background = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
    background.name = "Background";
    background.parent = container;
    background.transform.localPosition = Vector3.zero;
    background.transform.localScale = new Vector3(1f, 1f, 0.1f);
    GameScript._Singleton.StartCoroutine(GetMapPreviewCo(container, mapData));
    return container;
  }

  static Material[] _SceneMaterials;
  static IEnumerator GetMapPreviewCo(Transform container, string mapData)
  {
    // Color
    if (_SceneMaterials == null)
    {
      _SceneMaterials = new Material[13];
      for (int i = 0; i < _SceneMaterials.Length; i++)
        _SceneMaterials[i] = new Material(Menu2._Menu.GetChild(1).gameObject.GetComponent<MeshRenderer>().sharedMaterial);
    }
    void ChangeColorAndDelete(Transform t0, int index, Color c0)
    {
      //return;
      var m0 = t0.gameObject.GetComponent<MeshRenderer>();
      //Resources.UnloadAsset(m0.sharedMaterial);
      _SceneMaterials[index].color = c0;
      m0.sharedMaterial = _SceneMaterials[index];
    }
    var background = container.GetChild(0);
    // Get split map data
    string[] data_split = mapData.Split(' ');
    int data_iter = 0;
    // Get metadata
    int width = int.Parse(data_split[data_iter++]),
      height = int.Parse(data_split[data_iter++]);
    // Loop through tiles
    for (; data_iter - 2 < width * height; data_iter++)
    {
      var up = int.Parse(data_split[data_iter]) == 1;

      int current_column = ((data_iter - 2) % height),
        current_row = (int)((data_iter - 2) / height);
      var tile_new = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
      tile_new.gameObject.layer = 11;
      tile_new.parent = background;
      tile_new.localScale = new Vector3(2.5f, 2.5f, up ? 20f : 0.1f);
      tile_new.localPosition = new Vector3(current_column * tile_new.localScale.x - (background.localScale.x / 2f) + tile_new.localScale.x / 2f, current_row * tile_new.localScale.y - (background.localScale.y / 2f) + tile_new.localScale.y / 2f, 0.5f);
      if (!up)
      {
        var r = tile_new.GetComponent<MeshRenderer>();
        ChangeColorAndDelete(tile_new.transform, 12, Color.white);
        r.receiveShadows = false;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
      }
      else
        tile_new.GetComponent<Renderer>().enabled = false;
      //if (data_iter % 40 == 0) yield return new WaitForSecondsRealtime(0.005f);
    }

    // Add border to map to make 16:9 aspect
    int widthBorder = 0, heightBorder = 0;
    if (width < height * (16f / 9f)) widthBorder = ((int)((height + 2) * (16f / 9f)) - width);
    else if (height < width) heightBorder = (width - height);
    if (widthBorder % 2 != 0) widthBorder++;
    if (widthBorder == 0) widthBorder = 2;
    if (heightBorder == 0) heightBorder = 2;
    if (heightBorder % 2 != 0) heightBorder++;
    for (int i = 0; i < widthBorder; i++)
      for (int u = 0; u < height; u++)
      {
        int current_column = u,
          current_row = 0;
        if (i >= widthBorder / 2)
          current_row = width + (i - widthBorder / 2);
        else
          current_row = -1 - i;
        bool spawnTile = (current_row == -1 || current_row == width);
        var tile_new = (spawnTile ? GameObject.CreatePrimitive(PrimitiveType.Cube).transform : new GameObject().transform);
        tile_new.gameObject.layer = 11;
        tile_new.parent = background;
        tile_new.localScale = new Vector3(2.5f, 2.5f, 20f);
        tile_new.localPosition = new Vector3(current_column * tile_new.localScale.x - (background.localScale.x / 2f) + tile_new.localScale.x / 2f, current_row * tile_new.localScale.y - (background.localScale.y / 2f) + tile_new.localScale.y / 2f, 0.5f);
        if (spawnTile) tile_new.GetComponent<Renderer>().enabled = false;
      }
    for (var i = 0; i < heightBorder; i++)
      for (var u = 0; u < width + widthBorder; u++)
      {
        int current_column = 0,
          current_row = u - (widthBorder / 2);
        if (i >= heightBorder / 2)
          current_column = height + (i - heightBorder / 2);
        else
          current_column = -1 - i;
        var spawnTile = (current_column == -1 || current_column == height);
        var tile_new = (spawnTile ? GameObject.CreatePrimitive(PrimitiveType.Cube).transform : new GameObject().transform);
        tile_new.gameObject.layer = 11;
        tile_new.parent = background;
        tile_new.localScale = new Vector3(2.5f, 2.5f, 20f);
        tile_new.localPosition = new Vector3(current_column * tile_new.localScale.x - (background.localScale.x / 2f) + tile_new.localScale.x / 2f, current_row * tile_new.localScale.y - (background.localScale.y / 2f) + tile_new.localScale.y / 2f, 0.5f);
        if (spawnTile) tile_new.GetComponent<Renderer>().enabled = false;
      }

    // Loop through objects
    var container_objects = new GameObject("ObjectContainer").transform;
    container_objects.parent = container;
    container_objects.localPosition = Vector3.zero;
    container_objects.localScale = new Vector3(1f, 1f, 1f);
    for (; data_iter < data_split.Length; data_iter++)
    {
      if (data_split[data_iter].Contains("+")) break;

      var split = data_split[data_iter].Split('_');
      var type = split[0];
      if (type.Trim().Length == 0)
        continue;

      Vector3 position = new Vector3(float.Parse(split[2], System.Globalization.CultureInfo.InvariantCulture), float.Parse(split[1], System.Globalization.CultureInfo.InvariantCulture), 0f),
        scale = new Vector3(2.5f, 2.5f, 0.1f) / 3f;
      Transform object_new = null;
      switch (type)
      {
        // Check enemy
        case ("e"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
          int color_iter = 0;
          var ec = Color.green;
          if (data_split[data_iter].ToLower().Contains("pistol_silenced"))
          {
            ec = Color.magenta / 3f;
            color_iter = 1;
          }
          else if (data_split[data_iter].ToLower().Contains("pistol"))
          {
            ec = Color.magenta;
            color_iter = 2;
          }
          else if (data_split[data_iter].ToLower().Contains("rocket"))
          {
            ec = Color.yellow;
            color_iter = 3;
          }
          else if (data_split[data_iter].ToLower().Contains("grenade"))
          {
            ec = Color.red + Color.yellow / 1.2f;
            color_iter = 4;
          }
          else if (data_split[data_iter].ToLower().Contains("revolver"))
          {
            ec = (Color.red + Color.yellow) / 2f;
            color_iter = 5;
          }
          else if (data_split[data_iter].ToLower().Contains("bat") || data_split[data_iter].ToLower().Contains("shotgun"))
          {
            ec = Color.grey;
            color_iter = 6;
          }
          ChangeColorAndDelete(object_new, 5 + color_iter, ec);
          object_new.gameObject.layer = 2;
          break;
        // Check goal
        case ("p"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
          ChangeColorAndDelete(object_new, 0, Color.yellow * 2f);
          object_new.gameObject.layer = 2;
          // Door
          if (split.Length < 7) break;
          var position2 = new Vector3(float.Parse(split[6], System.Globalization.CultureInfo.InvariantCulture), float.Parse(split[5], System.Globalization.CultureInfo.InvariantCulture), 0f);
          var object_new2 = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
          ChangeColorAndDelete(object_new2, 1, GameResources._Door.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().sharedMaterial.color);
          object_new2.parent = container_objects;
          object_new2.localScale = new Vector3(scale.x * 4f, scale.y * 0.8f, scale.z * 0.9f);
          object_new2.position = position2;
          var rot = object_new2.localRotation;
          rot.eulerAngles = new Vector3(0f, 0f, 1f) * (float.Parse(split[8], System.Globalization.CultureInfo.InvariantCulture) + 90f);
          object_new2.localRotation = rot;
          object_new2.gameObject.layer = 11;
          break;
        // Check spawn
        case ("playerspawn"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
          ChangeColorAndDelete(object_new, 2, Color.blue);
          object_new.gameObject.layer = 2;
          break;
        case ("button"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
          ChangeColorAndDelete(object_new, 3, Color.red);
          object_new.gameObject.layer = 2;
          scale *= 0.7f;
          // Door
          position2 = new Vector3(float.Parse(split[5], System.Globalization.CultureInfo.InvariantCulture), float.Parse(split[4], System.Globalization.CultureInfo.InvariantCulture), 0f);
          object_new2 = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
          ChangeColorAndDelete(object_new2, 1, GameResources._Door.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().sharedMaterial.color);
          object_new2.parent = container_objects;
          object_new2.localScale = new Vector3(scale.x * 5f, scale.y * 0.8f, scale.z * 0.9f);
          object_new2.position = position2;
          rot = object_new2.localRotation;
          rot.eulerAngles = new Vector3(0f, 0f, 1f) * (float.Parse(split[7], System.Globalization.CultureInfo.InvariantCulture) + 90f);
          object_new2.localRotation = rot;
          object_new2.gameObject.layer = 11;
          break;
        case ("barrel"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
          ChangeColorAndDelete(object_new, 4, new Color(139f / 255f, 69f / 255f, 19f / 255f, 1f));
          scale *= 0.8f;
          break;
        case ("expbarrel"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
          ChangeColorAndDelete(object_new, 3, Color.red);
          scale *= 0.8f;
          break;
        case ("bookcaseopen"):
        case ("bookcaseclosed"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
          ChangeColorAndDelete(object_new, 4, new Color(139f / 255f, 69f / 255f, 19f / 255f, 1f));
          scale = new Vector3(scale.x * 1.5f, scale.y * 0.8f, scale.z);
          break;
        case ("bookcasebig"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
          ChangeColorAndDelete(object_new, 4, new Color(139f / 255f, 69f / 255f, 19f / 255f, 1f));
          scale = new Vector3(scale.x * 3f, scale.y * 0.8f, scale.z);
          break;
        case ("chair"):
        case ("tablesmall"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
          ChangeColorAndDelete(object_new, 4, new Color(139f / 255f, 69f / 255f, 19f / 255f, 1f));
          scale *= 0.6f;
          break;
        case ("table"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
          ChangeColorAndDelete(object_new, 4, new Color(139f / 255f, 69f / 255f, 19f / 255f, 1f));
          scale.y *= 2f;
          break;
        case ("laser"):
          object_new = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
          ChangeColorAndDelete(object_new, 4, Color.black);
          // Laser beam
          Transform object_laser = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
          object_laser.localScale = new Vector3(0.3f, 6f, 0.2f);
          ChangeColorAndDelete(object_laser, 3, Color.red * 2f);
          object_laser.parent = object_new;
          object_laser.localPosition = new Vector3(0f, -object_laser.localScale.y / 2f, 0f);
          object_laser.gameObject.layer = 2;
          break;
        default:
          object_new = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
          ChangeColorAndDelete(object_new, 4, new Color(139f / 255f, 69f / 255f, 19f / 255f, 1f));
          break;
      }
      if (object_new != null)
      {
        object_new.gameObject.layer = 11;
        object_new.name = type;
        // Check additional properies
        if (split.Length > 3 && !type.Equals("e") && !type.Equals("button"))
        {
          int iter = 3;
          foreach (var property in GetProperties(ref split, ref iter))
          {
            switch (property.Key)
            {
              case ("rot"):
                Quaternion rot = object_new.localRotation;
                rot.eulerAngles = new Vector3(0f, 0f, 1f) * (float.Parse(property.Value, System.Globalization.CultureInfo.InvariantCulture) + 90f);
                object_new.localRotation = rot;
                break;
              case ("rotspeed"):
                float rotspeed = float.Parse(property.Value, System.Globalization.CultureInfo.InvariantCulture);
                MapPreviewActor script = object_new.gameObject.AddComponent<MapPreviewActor>();
                script._properties = new float[] { rotspeed };
                script.Init(MapPreviewActor.ActorType.LASER);
                break;
            }
          }
        }
        MeshRenderer r = object_new.GetComponent<MeshRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        // Scale / set position
        object_new.parent = container_objects;
        object_new.localScale = scale;
        object_new.position = position;
      }
      // Check for wait
      //if (data_iter % 25 == 0) yield return new WaitForSecondsRealtime(0.01f);
    }
    // Center all objects
    container_objects.localPosition = new Vector3(56.4f, 43.3f, 1f);
    // Center map in Transform
    List<Transform> objects = new List<Transform>();
    for (int i = background.childCount - 1; i >= 0; i--)
    {
      Transform child = background.GetChild(i);
      objects.Add(child);
    }
    for (int i = container_objects.childCount - 1; i >= 0; i--)
    {
      Transform child = container_objects.GetChild(i);
      objects.Add(child);
    }
    var leastPosition = new Vector2(1000f, 1000f);
    foreach (var child in objects)
    {
      child.parent = container;
      Vector3 pos = child.position;
      if (pos.x < leastPosition.x) leastPosition.x = pos.x;
      if (pos.y < leastPosition.y) leastPosition.y = pos.y;
    }
    //leastPosition.x += ((height + heightBorder)) * 2.5f - (2.5f / 2f);
    //GameObject.Destroy(background.gameObject);
    //GameObject.Destroy(container_objects.gameObject);
    foreach (var child in objects)
      child.localPosition += new Vector3(-(height) / 2f * 2.5f - (2.5f * 0.5f), -(width) / 2f * 2.5f, 0f);

    // Rotate / scale map
    container.Rotate(new Vector3(1f, 0f, 1f) * -90f);
    float mod = 12f / (width + widthBorder);
    container.localScale = new Vector3(0.17f * mod, 0.17f * mod, 0.01f);

    // Set position
    container.parent = Menu2._Text.transform;
    if (GameScript._lp0 != null && GameScript._lp0.gameObject != null)
    {
      GameObject.Destroy(GameScript._lp0.gameObject);
      GameScript._lp0 = null;
    }

    // Set preview pos
    if (Menu2._CurrentMenu._type == Menu2.MenuType.EDITOR_LEVELS)
    {
      container.parent = Camera.main.transform;
      container.localPosition = new Vector3(2.8f, 1.5f, 6f);
    }
    else if (Menu2._CurrentMenu._type == Menu2.MenuType.EDITOR_PACKS_EDIT)
    {
      container.parent = Camera.main.transform;
      container.localPosition = new Vector3(2.8f, 1.5f, 6f);
    }
    else if (GameScript._GameMode == GameScript.GameModes.CLASSIC)
      container.localPosition = new Vector3(8.88f, -7.26f, 0f);
    else
      container.localPosition = new Vector3(8.88f, -3.78f, 0f);

    GameScript._lp0 = container;

    // Wait for spawn and check if should delete
    yield return new WaitForSecondsRealtime(0.005f);
    float timer = 0.9f;
    while (timer < 1f && container != null)
    {
      timer = Mathf.Clamp(timer + 0.05f, 0f, 1f);
      yield return new WaitForSecondsRealtime(0.005f);
      // Check no longer in menu
      bool getout = false;
      if (
        !Menu2._InMenus ||
        (
          ((Menu2._CurrentMenu._type != Menu2.MenuType.LEVELS) || (Menu2._CurrentMenu._type == Menu2.MenuType.LEVELS && Menu2._CurrentMenu._dropdownCount == 0)) &&
          (Menu2._CurrentMenu._type != Menu2.MenuType.EDITOR_LEVELS && Menu2._CurrentMenu._type != Menu2.MenuType.EDITOR_PACKS_EDIT)
        )
        )
      {
        if (GameScript._lp0 != null)
          GameObject.Destroy(GameScript._lp0.gameObject);
        getout = true;
      }
      if (getout) break;
    }
  }

  public class Tile : System.IComparable<Tile>
  {
    static int _ID;
    public int _id, _lastMouseID;

    public bool _moving;

    public bool _toggled
    {
      get
      {
        return _tile.transform.localPosition.y <= _StartY + _AddY - 0.001f;
      }
    }

    public GameObject _tile, _tileFake;
    public Vector2 _pos;

    public static float _StartY = -2.88f, _AddY = 3f;

    public static bool _Moving = false;

    public Tile(GameObject tile)
    {
      _id = _ID++;

      _tile = tile;

      if (_Tile != null)
      {
        Vector3 dis = _tile.transform.position - _Tile.transform.position;
        _pos = new Vector2((int)(dis.x / _Tile_spacing), (int)(dis.z / _Tile_spacing));
      }
      else
      {
        _pos = Vector3.zero;
        throw new System.NullReferenceException("_Tile is null while setting tile _pos");
      }
      _lastMouseID = -1;
    }

    public static bool colliders_toggled;
    public static IEnumerator LerpPositions(Tile[] tiles, float waitBetweenMoving = 0f, bool changeColor = true)
    {
      _Moving = true;
      // Seperate tiles into arrays based on if it's moving up or down; Also turn off BoxColliders so the NavMesh can be generated faster
      List<Tile> tiles_up = new List<Tile>(), tiles_down = new List<Tile>();
      foreach (Tile t in tiles)
      {
        t._tile.transform.parent = _Map;
        if (!t._toggled)
        {
          tiles_down.Add(t);
          continue;
        }
        tiles_up.Add(t);
      }
      // Get positions for down and up
      Vector3 pos_up = new Vector3(0f, _StartY + _AddY, 0f),
       pos_down = new Vector3(0f, _StartY, 0f);

      if (!GameScript._EditorEnabled)
      {
        if (tiles_up.Count > 0)
          tiles_up[0].ChangeColor(SceneThemes._Theme._tileColorUp);
        if (tiles_down.Count > 0)
          tiles_down[0].ChangeColor(SceneThemes._Theme._tileColorDown);
      }
      foreach (var t in tiles_up)
        t._tile.transform.localPosition = new Vector3(t._tile.transform.localPosition.x, pos_up.y, t._tile.transform.localPosition.z);
      foreach (var t in tiles_down)
        t._tile.transform.localPosition = new Vector3(t._tile.transform.localPosition.x, pos_down.y, t._tile.transform.localPosition.z);

      colliders_toggled = true;
      yield return new WaitForSecondsRealtime(0.005f);
      _Moving = false;
    }

    public void ChangeColor(Color c)
    {
      GameScript._Singleton.StartCoroutine(ChangeColorCo(c, 0.2f));
    }

    IEnumerator ChangeColorCo(Color c, float time = 1f)
    {
      _moving = true;
      float t = 0f;
      MeshRenderer m = _tile.GetComponent<MeshRenderer>();
      Color startColor = !GameScript._EditorEnabled ? m.sharedMaterial.color : m.material.color;
      if (!Color.Equals(startColor, c))
        while (t < time)
        {
          yield return new WaitForSecondsRealtime(0.01f);
          t += 0.01f;
          if (!GameScript._EditorEnabled)
            m.sharedMaterial.color = Color.Lerp(startColor, c, t / time);
          else
            m.material.color = Color.Lerp(startColor, c, t / time);
        }
      _moving = false;
    }

    public static Tile GetTile(int x, int y)
    {
      Tile[] til = _Tiles.ToArray();
      int iter = x * _Height + y;
      if (x < 0 || y < 0 || iter >= _Tiles.Count) return null;

      Tile t = _Tiles[iter];
      return _Tiles[iter];
    }
    public static Tile GetTile(GameObject g)
    {
      foreach (Tile t in _Tiles)
        if (t._tile.GetInstanceID() == g.GetInstanceID()) return t;
      return null;
    }

    public int CompareTo(Tile other)
    {
      float tile_iter = _pos.x * _Width + _pos.y,
        other_tile_iter = other._pos.x * _Width + other._pos.y;
      if (tile_iter > other_tile_iter) return 1;
      if (tile_iter < other_tile_iter) return -1;
      return 0;
    }
  }
}