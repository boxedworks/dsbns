using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
  // Singleton
  public static List<PlayerScript> _Players;

  public static bool _All_Dead;

  public static int _PLAYERID = 0;
  public int _id;
  // Ragdoll
  public ActiveRagdoll _ragdoll;
  // Holds values for camera position / rotation to lerp to
  public Vector3 _camPos, _camRot;
  float _camRotX;

  Vector2 _saveInput;
  bool _spawnRunCheck;

  public GameScript.ItemManager.Items _itemLeft, _itemRight;

  public static readonly float MOD =
#if UNITY_EDITOR
  1.2f
#else
  1f
#endif
    ,
  MOVESPEED = 4.5f * MOD,
    RUNSPEED = 1.15f,
    ROTATIONSPEED = 2f / MOD;

  bool mouseEnabled, _runToggle;

  public bool _hasExit, _canDetect;

  float _spawnTimer, _invisible;

  public UnityEngine.AI.NavMeshAgent _agent;

  static float _cY, _cX;

  public List<UtilityScript> _utilities_left, _utilities_right;

  public GameScript.PlayerProfile _profile
  {
    get
    {
      return GameScript.PlayerProfile._Profiles[_id];
    }
  }

  public GameScript.PlayerProfile.Equipment _equipment
  {
    get { return _profile._equipment; }
  }

  int _saveLoadoutIndex;

  //
  [System.NonSerialized]
  public GameScript.PlayerProfile.Equipment _equipment_start;
  [System.NonSerialized]
  public bool _equipment_changed;
  public static int _NumPlayers_Start;

  // Colored ring under player
  MeshRenderer[] _ring;
  public static Material[] _Materials_Ring;

  bool _isauto;
  static float _autouserate;
  static ActiveRagdoll.Side _autoside;

  // Last time whistled
  public float _last_whistle;

  // Use this for initialization
  void Start()
  {
    _All_Dead = false;

    _SlowmoTimer = 0f;
    Time.timeScale = 1f;
    _spawnRunCheck = true;
    //_camHeight = Vector3.Distance(GameResources._Camera_Main.transform.position, transform.position);

    // Setup ragdoll
    var ragdollObj = Instantiate(GameResources._Ragdoll);
    ragdollObj.transform.parent = transform.parent;
    ragdollObj.transform.Rotate(new Vector3(0f, 1f, 0f) * 90f);
    ragdollObj.transform.position = transform.position;
    var health = (GameScript.IsSurvival() ? 3 : 1);
    _ragdoll = new ActiveRagdoll(ragdollObj, transform)
    {
      _isPlayer = true,
      _health = health
    };
    //_ragdoll._hip.gameObject.AddComponent<RagdollTriggerScript>();

    // Check armor for editor maps
    if (!GameScript.IsSurvival())
      if (_equipment._perks != null && _equipment._perks.Contains(Shop.Perk.PerkType.ARMOR_UP))
      {
        _ragdoll.AddArmor();
        _ragdoll._health = 3;
      }

    // Get NavMeshAgent
    _agent = transform.GetComponent<UnityEngine.AI.NavMeshAgent>();
    // Add self to list of players
    if (_Players == null)
      _Players = new List<PlayerScript>();
    _Players.Add(this);
    // Give unique _PlayerID and decide input based upon _PlayerID
    _id = _PLAYERID++ % Settings._NumberPlayers;
    // Check all players to make sure no duplicate _PlayerID
    var loops = 0;
    while (true && loops++ < 5)
    {
      var breakLoop = true;
      foreach (var p in _Players)
      {
        if (p == null) continue;
        // If self, skip
        if (p.transform.GetInstanceID() == transform.GetInstanceID() || p._ragdoll._dead) continue;
        // If has same _PlayerID, increment id and set do not break while loop
        if (p._id == _id)
        {
          _id = _PLAYERID++ % Settings._NumberPlayers;
          breakLoop = false;
          break;
        }
      }
      // If break loop, break the loop
      if (breakLoop) break;
    }
    if (loops == 5) Debug.LogError("Duplicate _PlayerID, may cause bug " + _id);

    // Create health UI
    //_profile.CreateHealthUI(health);

    // Assign color by _PlayerID
    var color = _profile.GetColor();
    _ragdoll.ChangeColor(color);
    // Create ring
    var new_ring = Instantiate(TileManager._Ring.gameObject) as GameObject;
    _ring = new MeshRenderer[] { new_ring.transform.GetChild(0).GetComponent<MeshRenderer>(), new_ring.transform.GetChild(1).GetComponent<MeshRenderer>() };
    Resources.UnloadAsset(_ring[0].sharedMaterial);
    Resources.UnloadAsset(_ring[1].sharedMaterial);
    _ring[0].sharedMaterial = _Materials_Ring[_id];
    _ring[1].sharedMaterial = _Materials_Ring[_id];
    Vector3 localscale = _ring[0].transform.parent.localScale;
    localscale *= 0.8f;
    localscale.y = 2f;
    _ring[0].transform.parent.localScale = localscale;
    ChangeRingColor(color);

    // Move ring with hip
    var hippos = transform.position;
    hippos.y = -1.38f;
    if (!_ragdoll._dead)
      _ring[0].transform.parent.position = hippos;
    _ring[0].transform.parent.gameObject.SetActive(true);

    // Equip starting weapons
    _profile._equipmentIndex = 0;
    _itemLeft = _equipment._item_left0;
    _itemRight = _equipment._item_right0;
    GameScript.ItemManager.SpawnItem(_itemLeft);
    GameScript.ItemManager.SpawnItem(_itemRight);
    GameScript.ItemManager.SpawnItem(_equipment._item_left1);
    GameScript.ItemManager.SpawnItem(_equipment._item_right1);
    EquipStart();

    _saveLoadoutIndex = _profile._loadoutIndex;

    // Save start loadout for challenges
    _equipment_start = _equipment;
    _equipment_changed = false;

    // Set camera position
    _camPos = GameResources._Camera_Main.transform.position;

    _spawnTimer = 0.25f;
    _canDetect = true;

    _lastInputTriggers = new Vector2(-1f, -1f);

    _ragdoll._rotSpeed = 2.1f * ROTATIONSPEED;
    _agent.stoppingDistance = 0.5f;

    _taunt_times = new float[4];

    if (!TileManager._HasLocalLighting)
    {
      AddLight();

      RenderSettings.ambientLight = Color.black;
    }
    else
    {
      RenderSettings.ambientLight = GameScript._LightingAmbientColor;
    }

    RegisterUtilities();

    _profile.OnPlayerSpawn();
    _profile.UpdateHealthUI();

    ControllerManager.GetPlayerGamepad(_id)?.SetMotorSpeeds(0f, 0f);

    // Handle rain VFX
    if (_id == 0)
    {

      // Clean up old light if exists
      if (GameScript._Singleton._Thunder_Light != null)
        GameObject.DestroyImmediate(GameScript._Singleton._Thunder_Light.gameObject);

      // Handle creating rain SFX
      if (SceneThemes._Theme._rain)
      {
        var rain_prt = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.RAIN)[0];
        var rainFake_prt = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.RAIN_FAKE)[0];
        rain_prt.Play();
        rainFake_prt.Play();

        var thunder_light = new GameObject("ThunderLight").AddComponent<Light>();

        thunder_light.bounceIntensity = 0f;
        thunder_light.color = new Color(0.7423906f, 0.7423906f, 0.7830189f);
        thunder_light.range = 15f;
        thunder_light.intensity = 0f;

        thunder_light.shadowNearPlane = 7f;
        //thunder_light.shadows = LightShadows.Soft;

        GameScript._Singleton._Thunder_Light = thunder_light;

        // Add to audio list
        FunctionsC.AddToAudioList_Rain();
        Debug.Log("add");
      }

      // Clean up rain SFX
      else
      {

      }
    }
  }

  public void RegisterUtilities()
  {
    // Register utilities
    RegisterUtility(ActiveRagdoll.Side.LEFT);
    RegisterUtility(ActiveRagdoll.Side.RIGHT);
  }

  public void RegisterUtility(ActiveRagdoll.Side side, int amount = -1)
  {
    // Check extra
    if (Settings._Extras_CanUse)
    {
      if (_ragdoll?._isPlayer ?? false)
        switch (Settings._Extra_PlayerAmmo._value)
        {
          case 1:
            amount = Mathf.CeilToInt(amount * 2f);
            break;
          case 2:
            amount = Mathf.CeilToInt(amount * 0.5f);
            break;
        }
    }

    // Equip
    var max = amount == -1 ? 100 : amount;
    if (side == ActiveRagdoll.Side.LEFT)
    {
      _utilities_left = new List<UtilityScript>();
      foreach (var utility in _equipment._utilities_left)
      {
        var count = 1;
        if (utility == UtilityScript.UtilityType.SHURIKEN)
          count = 2;
        for (; count > 0 && max-- > 0; count--)
          AddUtility(utility, side);
      }
    }
    else
    {
      _utilities_right = new List<UtilityScript>();
      foreach (var utility in _equipment._utilities_right)
      {
        var count = 1;
        if (utility == UtilityScript.UtilityType.SHURIKEN)
          count = 2;
        for (; count > 0 && max-- > 0; count--)
          AddUtility(utility, side);
      }
    }
  }

  public void AddUtility(UtilityScript.UtilityType utility, ActiveRagdoll.Side side)
  {
    var util = UtilityScript.GetUtility(utility);
    util._side = side;
    (side == ActiveRagdoll.Side.LEFT ? _utilities_left : _utilities_right).Add(util);
    util.RegisterUtility(_ragdoll);
  }

  // Remove utility
  public void NextUtility(ActiveRagdoll.Side side)
  {
    var utilities = (side == ActiveRagdoll.Side.LEFT ? _utilities_left : _utilities_right);
    if (utilities.Count == 0) return;
    utilities.RemoveAt(0);
  }
  public void NextUtility(ActiveRagdoll.Side side, UtilityScript utility)
  {
    if (IsUtility(side, utility))
    {
      NextUtility(side);
    }
    else
    {
      var otherside = side == ActiveRagdoll.Side.LEFT ? ActiveRagdoll.Side.RIGHT : ActiveRagdoll.Side.LEFT;
      if (IsUtility(otherside, utility))
      {
        NextUtility(otherside);
        //Debug.Log(side);
      }
    }
  }

  public bool IsUtility(ActiveRagdoll.Side side, UtilityScript utility)
  {
    var utilities = (side == ActiveRagdoll.Side.LEFT ? _utilities_left : _utilities_right);
    return (utilities != null && utilities.Count > 0 &&
      utilities[0] == utility);
  }

  public int UtilityCount(ActiveRagdoll.Side side)
  {
    return (side == ActiveRagdoll.Side.LEFT ? _utilities_left : _utilities_right).Count;
  }

  public void AddLight()
  {
    var light = _ragdoll._head.gameObject.AddComponent<Light>();
    light.type = LightType.Point;
    light.shadows = LightShadows.Hard;
    light.color = Color.white;// _ragdoll._color;
    light.shadowStrength = 0.9f;
    light.intensity = 2f;
    light.range = 7.5f;
    light.shadowNearPlane = 0.5f;
  }
  static public void AddLights()
  {
    foreach (var p in _Players)
    {
      if (p == null || p._ragdoll == null || p._ragdoll._dead) continue;
      p.AddLight();
    }
  }

  public static void Reset()
  {
    if (_Players != null)
      foreach (var p in _Players)
      {
        p._hasExit = false;
        if (p != null && p.gameObject != null) Destroy(p.gameObject);
      }
    GameScript._inLevelEnd = false;
    _Players = null;
  }

  public void EquipStart()
  {
    // Equip start items
    if (_itemLeft != GameScript.ItemManager.Items.NONE)
      _ragdoll.EquipItem(_itemLeft, ActiveRagdoll.Side.LEFT);
    else
    {
      _ragdoll.AddArmJoint(ActiveRagdoll.Side.LEFT);
      _ragdoll.UnequipItem(ActiveRagdoll.Side.LEFT);
    }
    if (_itemRight != GameScript.ItemManager.Items.NONE)
      _ragdoll.EquipItem(_itemRight, ActiveRagdoll.Side.RIGHT);
    else if (_ragdoll._itemL == null || !_ragdoll._itemL._twoHanded)
    {
      _ragdoll.AddArmJoint(ActiveRagdoll.Side.RIGHT);
      _ragdoll.UnequipItem(ActiveRagdoll.Side.RIGHT);
    }
  }

  public void ChangeRingColor(Color c)
  {
    if (_ring == null) return;
    _ring[0].sharedMaterial.SetColor("_EmissionColor", c);
    c.a = 0.6f;
    _ring[0].sharedMaterial.color = c;
  }

  public static void StartLevelTimer()
  {
    _TimerStarted = true;

    if (PlayerScript._Players[0]._level_ratings_shown)
    {
      TileManager._Text_LevelTimer_Best.text = TileManager._Text_LevelTimer_Best.text.Split("\n")[0];
    }
  }

  float _lastDistance;
  ActiveRagdoll _targetRagdoll;
  int _targetIter;

  public static bool _TimerStarted;
  bool _level_ratings_shown;

  // Update is called once per frame
  float _rearrangeTime;
  void Update()
  {

    // Timer
    if (!_TimerStarted && _id == 0)
    {

      var player_farthest = FunctionsC.GetFarthestPlayerFrom(PlayerspawnScript._PlayerSpawns[0].transform.position);
      if (player_farthest._distance > 0.5f)
      {
        StartLevelTimer();
      }
    }

    // Ratings
    if (
      _id == 0 && !_level_ratings_shown && !TileManager._Level_Complete && !GameScript._Paused &&
      (
        (!_TimerStarted && Time.time - GameScript._LevelStartTime > 3f) ||
        (_ragdoll._dead && Time.unscaledTime - _ragdoll._time_dead > 3f)
      )
    )
    {
      _level_ratings_shown = true;

      // Rating times
      var best_dev_time = TileManager._LevelTime_Dev;
      var level_time_best = float.Parse(PlayerPrefs.GetFloat($"{Levels._CurrentLevelCollection_Name}_{Levels._CurrentLevelIndex}_time", -1f).ToString("0.000"));

      var medal_times = Levels.GetLevelRatingTimings(best_dev_time);
      var ratings = Levels.GetLevelRatings();

      var index = 0;
      var medal_index = -1;
      TileManager._Text_LevelTimer_Best.text += "\n\n";
      var medal_format = "<color={0}>{1,-5}: {2,-6}</color>\n";
      foreach (var time in medal_times)
      {

        var time_ = float.Parse(string.Format("{0:0.000}", time));
        if (level_time_best != -1f && level_time_best <= time_ && medal_index == -1)
        {
          medal_index = index;
        }
        TileManager._Text_LevelTimer_Best.text += string.Format(medal_format, ratings[index].Item2, ratings[index].Item1, string.Format("{0:0.000}", time_) + (medal_index == index ? "*" : ""));
        index++;
      }

    }

#if DEBUG
    if (ControllerManager.GetKey(ControllerManager.Key.DELETE))
    {
      _isauto = !_isauto;
    }
    if (_isauto && !_ragdoll._dead && !GameScript._Paused)
    {
      _rearrangeTime -= Time.deltaTime * (_agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete ? 10f : 1f);
      if (_rearrangeTime <= 0f && !_Players[0]._ragdoll._dead)
      {
        var pos = _Players[0]._ragdoll._hip.position;
        var dis = MathC.Get2DDistance(pos, transform.position);
        var maxD = 5f;

        if (dis < 2f)
        {
          pos = _ragdoll._hip.position;
          maxD = 6f;
        }
        if (GameScript._GameMode == GameScript.GameModes.CLASSIC)
        {

          if (!PlayerScript.HasExit())
          {

            var minDist = 5f;
            var moved = false;
            foreach (var bullet in ItemScript._BulletPool)
            {
              if (!bullet.gameObject.activeSelf || bullet.GetRagdollID() == _ragdoll._id) continue;
              if (MathC.Get2DDistance(_ragdoll._hip.position, bullet.transform.position) < minDist)
              {
                //_SlowmoTimer = Mathf.Clamp(_SlowmoTimer + 1f, 0f, 2f);
                var dir = Quaternion.AngleAxis(-90, Vector3.up) * bullet._rb.velocity;
                _agent.SetDestination(_ragdoll.transform.position + dir * 10f);
                moved = true;
                break;
              }
            }

            if (!moved)
              _agent.SetDestination(Powerup._Powerups[0].transform.position);
          }
          else
          {
            _agent.SetDestination(PlayerspawnScript._PlayerSpawns[0].transform.position);
          }

          _rearrangeTime = 0.2f;

        }
        else
        {
          _agent.SetDestination(pos + new Vector3(-maxD + Random.value * maxD * 2f, 0f, -maxD + Random.value * maxD * 2f));
          _rearrangeTime = 1.5f + Random.value * 4f;
        }
      }
      if (_agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete)
      {
        var dis2 = MathC.Get2DVector(_agent.steeringTarget - transform.position);
        if (dis2.magnitude > 1f)
          dis2 = dis2.normalized;
        MovePlayer(Time.unscaledDeltaTime, MOVESPEED * 0.6f, new Vector2(dis2.x, dis2.z));
      }

      if (EnemyScript._Enemies_alive != null && EnemyScript._Enemies_alive.Count > 0)
      {
        if (_targetRagdoll == null || _targetRagdoll._dead) _lastDistance = 10000f;

        for (var i = 0; i < 10; i++)
        {
          var next_enemy = EnemyScript._Enemies_alive[_targetIter++ % EnemyScript._Enemies_alive.Count];
          var next_ragdoll = next_enemy.GetRagdoll();

          if (next_ragdoll != null && (_targetRagdoll == null || next_ragdoll._id != _targetRagdoll._id))
          {
            var path = new UnityEngine.AI.NavMeshPath();
            if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, next_enemy.transform.position, TileManager._navMeshSurface2.agentTypeID, path))
            {
              var dist = FunctionsC.GetPathLength(path.corners);
              if (dist < _lastDistance)
              {
                _targetRagdoll = next_enemy.GetRagdoll();
                _lastDistance = dist;
              }
            }
          }
        }

        if (_targetRagdoll != null)
        {
          transform.LookAt(_targetRagdoll._hip.transform.position);

          _ragdoll.ToggleRaycasting(false);
          var hit = new RaycastHit();
          if (Physics.SphereCast(new Ray(_ragdoll._transform_parts._head.transform.position, _ragdoll._hip.transform.forward * 100f + Vector3.up * 0.3f), 0.1f, out hit, EnemyScript._Layermask_Ragdoll))
          {
            if (_targetRagdoll.IsSelf(hit.collider.gameObject))
            {
              var dist = 15f;
              var switch_sides = false;

              for (var i = 0; i < 2; i++)
              {
                if (switch_sides)
                  _autoside = _autoside == ActiveRagdoll.Side.LEFT ? ActiveRagdoll.Side.RIGHT : ActiveRagdoll.Side.LEFT;
                switch_sides = false;

                if (Time.time - _autouserate < 0) break;

                var item = (_autoside == ActiveRagdoll.Side.LEFT ? _ragdoll._itemL : _ragdoll._itemR);

                if (!item)
                {
                  switch_sides = true;
                  continue;
                }

                if (item)
                {
                  // Melee
                  if (item._melee)
                  {
                    dist = 0.7f;
                    switch_sides = true;
                  }

                  if (item._reloading)
                  {
                    switch_sides = true;
                    continue;
                  }

                  // Check use distance
                  if (hit.distance < dist)
                  {
                    // Use item
                    if (_autoside == ActiveRagdoll.Side.LEFT)
                      _ragdoll.UseLeft();
                    else
                      _ragdoll.UseRight();

                    // Set use rate
                    if (item._melee)
                      _autouserate = Time.time + item.UseRate();
                    else if (item._fireMode == ItemScript.FireMode.AUTOMATIC)
                    {
                      if (item.NeedsReload())
                        switch_sides = true;
                    }
                    else if (item._fireMode == ItemScript.FireMode.BURST || item._fireMode == ItemScript.FireMode.SEMI)
                    {
                      switch_sides = true;
                      _autouserate = Time.time + 0.2f;
                    }

                    // Check reload
                    if (item.NeedsReload())
                      item.Reload();

                    if (switch_sides)
                      _autoside = _autoside == ActiveRagdoll.Side.LEFT ? ActiveRagdoll.Side.RIGHT : ActiveRagdoll.Side.LEFT;
                    switch_sides = false;

                    break;
                  }
                }

              }
            }
          }
          // Try to reload
          else
          {
            for (var i = 0; i < 2; i++)
            {
              var item = i == 0 ? _ragdoll._itemL : _ragdoll._itemR;
              if (item == null || item._type == GameScript.ItemManager.Items.NONE) continue;
              if (item.IsMelee()) continue;
              if (item.CanReload())
                item.Reload();
            }
          }

          _ragdoll.ToggleRaycasting(true);
        }
      }
      /*/if (Debug.isDebugBuild)
      {
        if (_id == 0)
        {
          if (ControllerManager.GetKey(ControllerManager.Key.C))
            AutoPlayer.Capture();
          if (ControllerManager.GetKey(ControllerManager.Key.G))
            AutoPlayer.Playback();
          if (ControllerManager.GetKey(ControllerManager.Key.DELETE))
            AutoPlayer.Erase();
          if (ControllerManager.GetKey(ControllerManager.Key.TWO))
            GameScript.PlayerProfile._Profiles[1].CycleWeapon(1);
          if (ControllerManager.GetKey(ControllerManager.Key.THREE))
            GameScript.PlayerProfile._Profiles[2].CycleWeapon(1);
          if (ControllerManager.GetKey(ControllerManager.Key.FOUR))
            GameScript.PlayerProfile._Profiles[3].CycleWeapon(1);
        }
        if (AutoPlayer._Playing)
          AutoPlayer.ControlPlayer(this, _id);
      }*/
    }
#endif
    // Try to spawn ragdoll
    if (!_ragdoll.IsActive() && Time.time - GameScript._LevelStartTime > 0.2f)
    {
      // Enable ragdoll
      _ragdoll.SetActive(true);
    }

    float unscaled_dt = Time.unscaledDeltaTime,
      dt = Time.deltaTime;
    _SlowmoTimer = Mathf.Clamp(_SlowmoTimer - unscaled_dt, 0f, 2f);

    if (_Players == null || this == null) return;

    // Move ring with hip
    if (!_ragdoll._dead)
    {
      var hippos = _ragdoll._hip.position;
      hippos.y = -1.38f;
      _ring[0].transform.parent.position = hippos;
      var lookpos = _ragdoll._transform_parts._hip.position + _ragdoll._transform_parts._hip.forward * 10f;
      lookpos.y = _ring[0].transform.parent.position.y;
      _ring[0].transform.parent.LookAt(lookpos);
    }

    if (GameScript._Paused)
    {
      if (_id == 0) FunctionsC.UpdateSFX(0f);
      return;
    }

    // Handle camera
    UpdateCamera();

    // Handle input
    if (!_isauto)
      HandleInput();

    if (GameScript._Paused)
      return;

    if (TileManager._LoadingMap)
    {
      Time.timeScale = 1f;
      return;
    }

    // Check for timescale change
    if (true)
    {
      if (_id == 0)
      {

        // Update time via player speed
        var time_move = Settings._Extra_Superhot && Settings._Extras_CanUse;
        if (time_move)
        {
          if (_spawnTimer <= 0f)
          {

            var dirs = Vector2.zero;
            var num = 0;
            for (var i = 0; i < _Players.Count; i++)
            {
              if (!_Players[i]._ragdoll._dead)
              {
                num++;
                dirs += _Players[i]._saveInput;
              }
            }
            if (num == 0) num = 1;
            dirs /= num;

            float mag = Mathf.Clamp(dirs.magnitude, 0.03f, 1f);
            //if (mag < 0.1f)
            //  mag = 0.0f;
            Time.timeScale = mag;
          }
        }
        else
        {
          // Check player dist
          float minDist = 1.5f, desiredTimeScale = 1f, slowTime = 0.05f, speedMod = 1f;

          if (_SlowmoTimer > 0f)
          {
            var has_perk = false;
            foreach (var p in _Players)
            {
              if (p._ragdoll._dead) continue;
              if (p.HasPerk(Shop.Perk.PerkType.NO_SLOWMO))
              {
                has_perk = true;
                break;
              }
            }

            if (!has_perk)
            {
              desiredTimeScale = slowTime;
              speedMod = 4f;
            }
          }
          else
            // Loop through players
            foreach (var p in _Players)
            {
              // Check if player dead
              if (p._ragdoll._dead || HasPerk(Shop.Perk.PerkType.NO_SLOWMO)) continue;
              foreach (var bullet in ItemScript._BulletPool)
              {
                if (!bullet.gameObject.activeSelf || bullet.GetRagdollID() == p._ragdoll._id) continue;
                if (MathC.Get2DDistance(p._ragdoll._hip.position, bullet.transform.position) < minDist)
                {
                  //_SlowmoTimer = Mathf.Clamp(_SlowmoTimer + 1f, 0f, 2f);
                  desiredTimeScale = slowTime;
                  speedMod = 6f;
                  break;
                }
              }
              if (speedMod != 4f && MissleScript._Missles != null)
              {
                foreach (var m in MissleScript._Missles)
                {
                  if (!m.gameObject.activeSelf || m._source._ragdoll._id == p._ragdoll._id) continue;
                  if (MathC.Get2DDistance(p._ragdoll._hip.position, m.transform.position) < minDist * 1.5f)
                  {
                    //_SlowmoTimer = Mathf.Clamp(_SlowmoTimer + 1f, 0f, 2f);
                    desiredTimeScale = slowTime;
                    speedMod = 4f;
                    break;
                  }
                }
              }
            }
          if (Time.timeScale > desiredTimeScale) speedMod *= 0.5f;

          var onealive = false;
          foreach (var player in _Players)
            if (player._ragdoll != null && !player._ragdoll._dead) { onealive = true; break; }
          if (!onealive) desiredTimeScale = 0f;

          // Update timescale
          Time.timeScale = Mathf.Clamp(Time.timeScale + (desiredTimeScale - Time.timeScale) * dt * 5f * speedMod, desiredTimeScale, 1f);
          if (Time.timeScale < 0.01f) Time.timeScale = 0f;
        }
        // Update sounds with Time.timescale
        FunctionsC.UpdateSFX(1f + -0.7f * ((1f - Time.timeScale) / (0.8f)));
      }
    }
    // Update ragdoll
    _ragdoll.Update();
  }

  // Lerp camera - taking into account more than one player
  void UpdateCamera()
  {
    if (this == null) return;

    float unscaled_dt = Time.unscaledDeltaTime,
      dt = Time.deltaTime;

    if (_id == 0 && GameScript._Singleton._UseCamera)
    {
      var camera_height = Settings._CameraZoom == 1 ? 14f : Settings._CameraZoom == 0 ? 10f : 18f;
      if (Settings._CameraType._value)
      {
        camera_height = 14f;
      }

      // Center camera on map if it does not need to move
      var center_camera = Settings._CameraType._value;
      var map_x = TileManager._Map_Size_X;
      var map_y = TileManager._Map_Size_Y;
      //Debug.Log($"{map_x} {map_y}");
      if (center_camera)
      {
        var zoom = Settings._CameraZoom;
        if (zoom == 0)
        {
          if (map_x > 7 || map_y > 4)
            center_camera = false;
        }
        else if (zoom == 1)
        {
          if (map_x > 9 || map_y > 6)
            center_camera = false;
        }
        else if (zoom == 2)
        {
          if (map_x > 14 || map_y > 8)
            center_camera = false;
        }
        else if (zoom == 3)
        {
          var zoom_ = -1;
          if (map_x <= 7 && map_y <= 4)
          {
            zoom_ = 0;
          }
          else if (map_x <= 9 && map_y <= 6)
          {
            zoom_ = 1;
          }
          else if (map_x <= 14 && map_y <= 8)
          {
            zoom_ = 2;
          }
          if (zoom_ > -1)
          {
            Settings._CameraZoom._value = zoom_;
            Settings.SetPostProcessing();
            Settings._CameraZoom._value = 3;
          }
          else
          {
            Settings._CameraZoom._value = 2;
            Settings.SetPostProcessing();
            Settings._CameraZoom._value = 3;

            center_camera = false;
          }
        }
      }
      if (center_camera)
      {
        _camPos = TileManager._Floor.position;
        _camPos.z += -0.5f;
      }

      // Move camera normally
      else
      {

        float CamYPos = 16f,
          camSpeed = (Time.time - GameScript._LevelStartTime < 1f ? 0.8f : 0.4f) * (22f / CamYPos);//,
                                                                                                   //biggestDist = 0f;
                                                                                                   //GameResources._Camera_Main.transform.GetChild(2).GetComponent<Light>().spotAngle = Mathf.LerpUnclamped(131f, 86f, (CamYPos / 10f) - 1f);
        Vector3 sharedPos = Vector3.zero,
         sharedForward = Vector3.zero;
        var forwardMagnitude = 3.5f;
        PlayerScript lastPlayer = null;
        var followAlive = true; // Variable to follow any player. Will only follow alive players if false
        var counter = 0;
        while (true)
        {
          foreach (var p in _Players)
          {
            if (p._ragdoll._dead && followAlive) continue;
            sharedPos += new Vector3(p._ragdoll._hip.position.x, 0f, p._ragdoll._hip.position.z);
            sharedForward += MathC.Get2DVector(p._ragdoll._hip.transform.forward).normalized * forwardMagnitude;
            if (lastPlayer == null) lastPlayer = p;
            var distance = Vector3.Distance(p.transform.position, lastPlayer.transform.position);
            //if (distance > biggestDist) biggestDist = distance;
            lastPlayer = this;
            counter++;
          }
          if (counter == 0) followAlive = false;
          else break;
        }
        sharedPos /= Mathf.Clamp(counter, 1, counter);
        sharedPos.y = transform.position.y + camera_height;
        sharedForward /= Mathf.Clamp(counter, 1, counter);

        if (Settings._CameraZoom == 2)
          sharedForward = Vector3.zero;
        else if (Settings._CameraZoom == 0)
          sharedForward /= 1.4f;

        var changePos = Vector3.zero;
        if (GameScript._Singleton._X) changePos.x = sharedPos.x + sharedForward.x;
        if (GameScript._Singleton._Y) changePos.y = sharedPos.y;
        if (GameScript._Singleton._Z) changePos.z = sharedPos.z + sharedForward.z;

        /*if (_cycle_objects)
        {
          var map_objects = GameObject.Find("Map_Objects").transform;
          var map_child = map_objects.GetChild((int)(Time.time * 2.5f) % map_objects.childCount);
          _camPos = map_child.position;
          GameScript.ToggleCameraLight(true);
        }
        else*/
        {
          _camPos += (changePos - _camPos) * Mathf.Clamp(unscaled_dt, 0f, 1f) * 1.5f * camSpeed;
          //GameScript.ToggleCameraLight(false);
        }

      }

      _camPos.y = camera_height;
      if (Settings._CameraType._value)
      {
        //_camPos.z -= 1.9f;
      }
      GameResources._Camera_Main.transform.position = _camPos;

      // Update rain
      if (SceneThemes._Theme._rain)
      {
        var rain_prt = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.RAIN)[0];
        var rainFake_prt = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.RAIN_FAKE)[0];

        rain_prt.transform.position = new Vector3(_camPos.x, _camPos.y + 10f, _camPos.z);
        rainFake_prt.transform.position = new Vector3(_camPos.x, _camPos.y + 5f, _camPos.z);
        //GameScript._Singleton._Thunder_Light.transform.position = new Vector3(_camPos.x, 0f, _camPos.z);
      }
    }

    //if (ControllerManager.GetKey(ControllerManager.Key.SPACE))
    {
      //_cycle_objects = !_cycle_objects;
    }

  }

  bool _cycle_objects;

  public bool HasPerk(Shop.Perk.PerkType perk)
  {
    return Shop.Perk.HasPerk(_id, perk);
  }

  Vector2 _lastInputTriggers, _lastInputDPad;
  public static float _SlowmoTimer;
  float _lastWeaponUse;

  float _lastGoalLength = -1f, _goalTotal;

  void HandleInput()
  {
    // Check if application is focused
    if (!Application.isFocused) return;

    //
    if (AutoPlayer._Playing && _id != 0)
      return;

    //
    if (_saveLoadoutIndex != _profile._loadoutIndex)
      ResetLoadout();

    // Spawn enemies as player gets closer to goal; 3rd difficulty / game mode ?
    if (!GameScript.IsSurvival())
      if (_id == 0 && (Settings._Extra_CrazyZombies && Settings._Extras_CanUse) && Powerup._Powerups.Count > 0)
      {
        var dis_spawn = MathC.Get2DDistance(transform.position, PlayerspawnScript._PlayerSpawns[0].transform.position);
        //Debug.Log("| " + dis_spawn + " " + (EnemyScript._Enemies_alive.Count < EnemyScript._MAX_RAGDOLLS_ALIVE));
        if (dis_spawn > 3f && EnemyScript._Enemies_alive.Count < EnemyScript._MAX_RAGDOLLS_ALIVE)
        {
          var path = new UnityEngine.AI.NavMeshPath();
          UnityEngine.AI.NavMesh.CalculatePath(transform.position, Powerup._Powerups[0].transform.position, TileManager._navMeshSurface.layerMask, path);
          var length = 0f;
          if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
          {
            if (path.corners.Length != 0)
            {
              if (path.corners.Length == 1)
                length = MathC.Get2DDistance(transform.position, path.corners[0]);
              else
                for (var i = 1; i < path.corners.Length; i++)
                  length += (path.corners[i] - path.corners[i - 1]).magnitude;
            }
            if (_lastGoalLength != -1f)
              _goalTotal += Mathf.Abs(_lastGoalLength - length);
            _lastGoalLength = length;

            if (_goalTotal > 1f)
            {
              _goalTotal = 0f;
              if (EnemyScript._Enemies_alive.Count > 0)
              {
                for (var i = 0; i < 1; i++)
                {
                  var enemy = EnemyScript.SpawnEnemyAt(new EnemyScript.SurvivalAttributes()
                  {
                    _enemyType = GameScript.SurvivalMode.EnemyType.KNIFE_RUN
                  },
                    new Vector2(PlayerspawnScript._PlayerSpawns[0].transform.position.x + 0.5f * Random.value, PlayerspawnScript._PlayerSpawns[0].transform.position.z + 0.5f * Random.value));
                  enemy._moveSpeed = 0.5f + 0.15f * Random.value;
                }
              }
            }
          }

          //Debug.Log(length + " " + _goalTotal + " " + path.status.ToString());
        }
      }

#if UNITY_EDITOR
    if (ControllerManager.GetKey(ControllerManager.Key.K))
    {
      //_ragdoll.AddCrown();
      FunctionsC.MusicManager.PlayNextTrack();
    }

#endif

    var saveInput = Vector2.zero;

    if ((_id == 0 && Settings._ForceKeyboard) || ControllerManager._NumberGamepads == 0)
      mouseEnabled = true;
    else if (ControllerManager._NumberGamepads > 0)
      mouseEnabled = false;

    float unscaled_dt = Time.unscaledDeltaTime,
      dt = Time.deltaTime;

    if (_spawnTimer > 0f)
    {
      _spawnTimer -= unscaled_dt;
      return;
    }
    float x = 0f,
      y = 0f,
      x2 = 0f,
      y2 = 0f;
    var movespeed = MOVESPEED;
    var runKeyDown = false;
    if (Time.timeScale < 0.8f)
      unscaled_dt = unscaled_dt * Time.timeScale * 2f;

    // Check arrow keys
    if (mouseEnabled)
    {
      if (ControllerManager.GetKey(ControllerManager.Key.W, ControllerManager.InputMode.HOLD) ||
        ControllerManager.GetKey(ControllerManager.Key.ARROW_U, ControllerManager.InputMode.HOLD))
        y += 1f;
      if (ControllerManager.GetKey(ControllerManager.Key.S, ControllerManager.InputMode.HOLD) ||
        ControllerManager.GetKey(ControllerManager.Key.ARROW_D, ControllerManager.InputMode.HOLD))
        y += -1f;
      if (ControllerManager.GetKey(ControllerManager.Key.A, ControllerManager.InputMode.HOLD) ||
        ControllerManager.GetKey(ControllerManager.Key.ARROW_L, ControllerManager.InputMode.HOLD))
        x += -1f;
      if (ControllerManager.GetKey(ControllerManager.Key.D, ControllerManager.InputMode.HOLD) ||
        ControllerManager.GetKey(ControllerManager.Key.ARROW_R, ControllerManager.InputMode.HOLD))
        x += 1f;
      // Mouse look
      var p = GameResources._Camera_Main.ScreenPointToRay(ControllerManager.GetMousePosition()).GetPoint(Vector3.Distance(GameResources._Camera_Main.transform.position, transform.position));
      transform.LookAt(new Vector3(p.x, transform.position.y, p.z));
      // Throw money
      if (ControllerManager.GetKey(ControllerManager.Key.V))
        Taunt(1);
      // Fire modes
      if (ControllerManager.GetKey(ControllerManager.Key.X))
        ToggleFireModes();
      /// Use items
      // Left item
      if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.DOWN))
        if (!_ragdoll._itemL)
        {
          _ragdoll.UseRightDown();
          saveInput.y = 1f;
        }
        else
        {
          _ragdoll.UseLeftDown();
          saveInput.x = 1f;
        }
      if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.UP))
        if (!_ragdoll._itemL)
        {
          _ragdoll.UseRightUp();
          saveInput.y = -1f;
        }
        else
        {
          _ragdoll.UseLeftUp();
          saveInput.x = -1f;
        }
      // Right item
      if (ControllerManager.GetMouseInput(1, ControllerManager.InputMode.DOWN))
        if (!_ragdoll._itemR)
        {
          _ragdoll.UseLeftDown();
          saveInput.x = 1f;
        }
        else
        {
          _ragdoll.UseRightDown();
          saveInput.y = 1f;
        }
      if (ControllerManager.GetMouseInput(1, ControllerManager.InputMode.UP))
        if (!_ragdoll._itemR)
        {
          _ragdoll.UseLeftUp();
          saveInput.x = -1f;
        }
        else
        {
          _ragdoll.UseRightUp();
          saveInput.y = -1f;
        }

      // Check grapple
      if (
        !_ragdoll._dead &&
        _ragdoll.HasMelee() && !_ragdoll.HasTwohandedWeapon() &&
        ControllerManager.GetMouseInput(2, ControllerManager.InputMode.UP)
        )
      {
        _ragdoll.Grapple(true);
      }

      // Move arms
      if (!_ragdoll._dead && ControllerManager.GetKey(ControllerManager.Key.SPACE, ControllerManager.InputMode.HOLD))
        ExtendArms();
      else
        _ragdoll.ArmsDown();
      // Check runkey
      if (_profile._holdRun)
      {
        runKeyDown = (ControllerManager.ShiftHeld());
      }
      else
      {
        runKeyDown = ControllerManager.ShiftHeld();
      }
      // Check utility
      if (ControllerManager.GetKey(ControllerManager.Key.Q))
      {
        if (_utilities_left.Count == 0 && _utilities_right.Count > 0)
          _utilities_right[0].UseDown();
        else if (_utilities_left.Count > 0)
          _utilities_left[0].UseDown();
      }
      else if (ControllerManager.GetKey(ControllerManager.Key.Q, ControllerManager.InputMode.UP))
      {
        if (_utilities_left.Count == 0 && _utilities_right.Count > 0)
          _utilities_right[0].UseUp();
        else if (_utilities_left.Count > 0)
          _utilities_left[0].UseUp();
      }
      if (ControllerManager.GetKey(ControllerManager.Key.E))
      {
        if (_utilities_right.Count == 0 && _utilities_left.Count > 0)
          _utilities_left[0].UseDown();
        else if (_utilities_right.Count > 0)
          _utilities_right[0].UseDown();
      }
      else if (ControllerManager.GetKey(ControllerManager.Key.E, ControllerManager.InputMode.UP))
      {
        if (_utilities_right.Count == 0 && _utilities_left.Count > 0)
          _utilities_left[0].UseUp();
        else if (_utilities_right.Count > 0)
          _utilities_right[0].UseUp();
      }

      // Check weapon swap
      if (ControllerManager.GetKey(ControllerManager.Key.TAB))
        SwapLoadouts();
      // Check weapon swap
      if (ControllerManager.GetKey(ControllerManager.Key.G))
      {
        _ragdoll.SwapItemHands(_profile._equipmentIndex);
        _profile.UpdateIcons();
      }

      // Check interactable
      if (ControllerManager.GetKey(ControllerManager.Key.F))
        if (_currentInteractable != null)
          _currentInteractable.Interact(this, CustomObstacle.InteractSide.DEFAULT);

      // Check reload
      if (ControllerManager.GetKey(ControllerManager.Key.R, _profile._reloadSidesSameTime ? ControllerManager.InputMode.HOLD : ControllerManager.InputMode.DOWN))
        Reload();
    }
    else
    // Controller
    {
      // Check sticks
      Vector2 controllerInput0 = new Vector2(ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.LSTICK_X),
          ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.LSTICK_Y)),
        controllerInput1 = new Vector2(ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.RSTICK_X),
          ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.RSTICK_Y));
      float min = 0.125f, max = 0.85f;
      if (Mathf.Abs(controllerInput0.x) <= min) controllerInput0.x = 0f;
      else if (Mathf.Abs(controllerInput0.x) >= max) controllerInput0.x = 1f * Mathf.Sign(controllerInput0.x);
      if (Mathf.Abs(controllerInput0.y) <= min) controllerInput0.y = 0f;
      else if (Mathf.Abs(controllerInput0.y) >= max) controllerInput0.y = 1f * Mathf.Sign(controllerInput0.y);
      if (Mathf.Abs(controllerInput1.x) <= min) controllerInput1.x = 0f;
      else if (Mathf.Abs(controllerInput1.x) >= max) controllerInput1.x = 1f * Mathf.Sign(controllerInput1.x);
      if (Mathf.Abs(controllerInput1.y) <= min) controllerInput1.y = 0f;
      else if (Mathf.Abs(controllerInput1.y) >= max) controllerInput1.y = 1f * Mathf.Sign(controllerInput1.y);
      if (controllerInput0 != Vector2.zero)
      {
        x = controllerInput0.x;
        y = controllerInput0.y;
      }
      if (controllerInput1 != Vector2.zero && controllerInput1.magnitude > 0.5f)
      {
        x2 = controllerInput1.x;
        y2 = controllerInput1.y;
        _cX = Mathf.Clamp(_cX + controllerInput1.x / 2f, -1f, 1f);
        _cY = Mathf.Clamp(_cY + controllerInput1.y / 2f, -1f, 1f);
      }

      // Move arms
      var gamepad = ControllerManager.GetPlayerGamepad(_id);
      if (gamepad != null)
      {
        if (!_ragdoll._dead && gamepad.buttonSouth.isPressed)
          ExtendArms();
        else
          _ragdoll.ArmsDown();

        // Use items
        Vector2 input = new Vector2(ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.L2),
          ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.R2));
        float bias = 0.4f;
        min = 0f + bias;
        if (input.x >= 1f - bias && _lastInputTriggers.x < 1f - bias)
          if (!_ragdoll._itemL && !_ragdoll._itemR)
          {
            SwapLoadouts();
          }
          else if (!_ragdoll._itemL)
          {
            _ragdoll.UseRightDown();
            saveInput.y = 1f;
          }
          else
          {
            _ragdoll.UseLeftDown();
            saveInput.x = 1f;
          }
        else if (input.x <= min && _lastInputTriggers.x > min)
          if (!_ragdoll._itemL)
          {
            _ragdoll.UseRightUp();
            saveInput.y = -1f;
          }
          else
          {
            _ragdoll.UseLeftUp();
            saveInput.x = -1f;
          }
        if (input.y >= 1f - bias && _lastInputTriggers.y < 1f - bias)
          if (!_ragdoll._itemL && !_ragdoll._itemR)
          {
            SwapLoadouts();
          }
          else if (!_ragdoll._itemR)
          {
            _ragdoll.UseLeftDown();
            saveInput.x = 1f;
          }
          else
          {
            _ragdoll.UseRightDown();
            saveInput.y = 1f;
          }
        else if (input.y <= min && _lastInputTriggers.y > min)
          if (!_ragdoll._itemR)
          {
            _ragdoll.UseLeftUp();
            saveInput.x = -1f;
          }
          else
          {
            _ragdoll.UseRightUp();
            saveInput.y = -1f;
          }
        _lastInputTriggers = input;

        // DPad - taunts
        input = new Vector2(ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.DPAD_X),
          ControllerManager.GetControllerAxis(_id, ControllerManager.Axis.DPAD_Y));
        if (input.y == -1f && _lastInputDPad.y != -1f)
          Taunt(1);

        // Change fire mode
        else if (input.y == 1f && _lastInputDPad.y != 1f)
          Taunt(0);
        if (input.x == -1f && _lastInputDPad.x != -1f)
          Taunt(2);
        else if (input.x == 1f && _lastInputDPad.x != 1f)
          Taunt(3);
        _lastInputDPad = input;

        // Check runkey
        if (_profile._holdRun)
          runKeyDown = gamepad.leftStickButton.wasPressedThisFrame;
        else runKeyDown = gamepad.leftStickButton.isPressed;

        // Check grapple
        if (
          !_ragdoll._dead &&
          _ragdoll.HasMelee() && !_ragdoll.HasTwohandedWeapon() &&
          gamepad.rightStickButton.wasPressedThisFrame
          )
        {
          _ragdoll.Grapple(true);
        }

        // Check utilities
        if (gamepad.leftShoulder.wasPressedThisFrame)
        {
          if (_utilities_left.Count == 0 && _utilities_right.Count > 0)
            _utilities_right[0].UseDown();
          else if (_utilities_left.Count > 0)
            _utilities_left[0].UseDown();
        }
        else if (gamepad.leftShoulder.wasReleasedThisFrame)
        {
          if (_utilities_left.Count == 0 && _utilities_right.Count > 0)
            _utilities_right[0].UseUp();
          else if (_utilities_left.Count > 0)
            _utilities_left[0].UseUp();
        }
        if (gamepad.rightShoulder.wasPressedThisFrame)
        {
          if (_utilities_right.Count == 0 && _utilities_left.Count > 0)
            _utilities_left[0].UseDown();
          else if (_utilities_right.Count > 0)
            _utilities_right[0].UseDown();
        }
        else if (gamepad.rightShoulder.wasReleasedThisFrame)
        {
          if (_utilities_right.Count == 0 && _utilities_left.Count > 0)
            _utilities_left[0].UseUp();
          else if (_utilities_right.Count > 0)
            _utilities_right[0].UseUp();
        }

        // Check weapon swap
        if (gamepad.buttonNorth.wasPressedThisFrame)
          SwapLoadouts();

        // Check interactable
        if (gamepad.buttonEast.wasPressedThisFrame)
          if (_currentInteractable != null)
            _currentInteractable.Interact(this, CustomObstacle.InteractSide.DEFAULT);

        // Check reload
        if (_profile._reloadSidesSameTime ? gamepad.buttonWest.isPressed : gamepad.buttonWest.wasPressedThisFrame)
          Reload();
      }
    }

    // Clamp input
    x = Mathf.Clamp(x, -1f, 1f);
    y = Mathf.Clamp(y, -1f, 1f);
    x2 = Mathf.Clamp(x2, -1f, 1f);
    y2 = Mathf.Clamp(y2, -1f, 1f);
    if (_spawnTimer > 0f)
    {
      x = y = x2 = y2 = 0f;
    }
    var xy = new Vector2(x, y);

    /// Run
    {
      // Check run option; option is not toggle run
      if (_profile._holdRun)
      {
        // If moving and run key is down and not running, run
        if (runKeyDown)
        {
          if (!_runToggle && (Mathf.Abs(x) == 1f || Mathf.Abs(y) == 1f))
            _runToggle = true;
        }
        // If run key is not down and is not moving, stop running
        else if (_runToggle && xy.magnitude < 0.5f)
          _runToggle = false;
      }

      // Check toggle run
      else
        if (runKeyDown)
        _runToggle = !_runToggle;

      // Check initial spawn run
      if (!_spawnRunCheck)
      {
        if (xy.magnitude > 0.75f)
        {
          _runToggle = true;
          _spawnRunCheck = true;
        }

        else if (Time.time - GameScript._LevelStartTime > 0.4f)
        {
          _spawnRunCheck = true;
        }
      }

      // Apply run speed
      if (_runToggle || _ragdoll._forceRun)
        movespeed *= RUNSPEED;
    }

    // Move player
    if (!_ragdoll._grappled)
    {
      _saveInput = xy;
      MovePlayer(unscaled_dt, movespeed, _saveInput);
      // Rotate player
      if (!mouseEnabled)
        RotatePlayer(new Vector2(x2, y2), xy);
    }

    // Check for player capture
    if (AutoPlayer._Capturing)
    {
      var actions = new List<KeyValuePair<string, float>>();
      if (saveInput.x != 0f)
        actions.Add(new KeyValuePair<string, float>("left", saveInput.x));
      if (saveInput.y != 0f)
        actions.Add(new KeyValuePair<string, float>("right", saveInput.y));

      var data = new PlayerData()
      {
        _position = transform.position,
        _forward = transform.forward,
        _actions = actions.ToArray()
      };

      AutoPlayer.UpdateCapture(data);
    }

  }

  public void MovePlayer(float deltaTime, float moveSpeed, Vector2 input)
  {
    // Decrease movespead
    if (_ragdoll._grappling) { moveSpeed *= 0.7f; }

    // Move player
    if (_ragdoll.Active() && _agent != null)
    {
      var dis2 = (Vector3.right * input.x * moveSpeed) + (Vector3.forward * input.y * moveSpeed);
      var savepos = _agent.transform.position;

      // Try to move agent
      var movepos = dis2 * deltaTime;
      _agent.Move(movepos);
      savepos = _agent.transform.position - savepos;

      // If not moved, maybe stuck on furnature, try to move around
      if (savepos.Equals(Vector3.zero) && !dis2.Equals(Vector3.zero))
      {
        savepos = _agent.transform.position;
        _agent.Move(movepos * 0.5f + transform.right * movepos.magnitude * 0.5f * deltaTime * 2f);//new Vector3(movepos.x * 0.5f + movepos.z * 0.5f, 0f, movepos.z * 0.5f + movepos.x * 0.5f));
        savepos = _agent.transform.position - savepos;
        if (savepos.Equals(Vector3.zero))
          _agent.Move(movepos * 0.5f - transform.right * movepos.magnitude * 0.5f * deltaTime * 2f);//new Vector3(movepos.x * 0.5f - movepos.z * 0.5f, 0f, movepos.z * 0.5f - movepos.x * 0.5f));
      }

      if (movepos.magnitude > 0.01f)
        FunctionsC.OnControllerInput();
    }
  }

  public void ResetLoadout()
  {
    if (_ragdoll._dead) return;
    // Check changed loadout profile
    if (_profile._loadoutIndex != _saveLoadoutIndex)
    {
      if (MathC.Get2DDistance(transform.position, PlayerspawnScript._PlayerSpawns[0].transform.position) > 1.2f)
        _profile._loadoutIndex = _saveLoadoutIndex;
      else
      {
        EquipLoadout(_profile._loadoutIndex);
        _equipment_changed = true;
      }
    }
  }

  public void EquipLoadout(int loadoutIndex, bool checkCanSwap = true)
  {
    _profile._loadoutIndex = loadoutIndex;

    _profile._equipmentIndex = 0;
    if (!checkCanSwap || _ragdoll.CanSwapWeapons())
    {
      _ragdoll.SwapItems(
        System.Tuple.Create(_profile._item_left, -1, -1f),
        System.Tuple.Create(_profile._item_right, -1, -1f),
        _profile._equipmentIndex,
        checkCanSwap
      );
      // Despawn utilities
      if (_utilities_left != null)
        for (int i = _utilities_left.Count - 1; i > 0; i--)
        {
          if (_utilities_left[i] != null)
            GameObject.Destroy(_utilities_left[i].gameObject);
        }
      if (_utilities_right != null)
        for (int i = _utilities_right.Count - 1; i > 0; i--)
        {
          if (_utilities_right[i] != null)
            GameObject.Destroy(_utilities_right[i].gameObject);
        }
      RegisterUtilities();
      _profile.UpdateIcons();
      _saveLoadoutIndex = _profile._loadoutIndex;
    }
  }

  void RotatePlayer(Vector2 input, Vector2 input2)
  {
    if (_isauto) return;
    if (Mathf.Abs(input.x) > 0.1f || Mathf.Abs(input.y) > 0.1f)
      transform.LookAt(transform.position + (new Vector3(GameResources._Camera_Main.transform.right.x, 0f, GameResources._Camera_Main.transform.right.z).normalized * input.x + new Vector3(GameResources._Camera_Main.transform.forward.x, 0f, GameResources._Camera_Main.transform.forward.z).normalized * input.y).normalized * 5f);
    else if (_profile._faceMovement)
      transform.LookAt(transform.position + (new Vector3(GameResources._Camera_Main.transform.right.x, 0f, GameResources._Camera_Main.transform.right.z).normalized * input2.x + new Vector3(GameResources._Camera_Main.transform.forward.x, 0f, GameResources._Camera_Main.transform.forward.z).normalized * input2.y).normalized * 5f);
  }

  public void ToggleLaser()
  {
    /*if (_ragdoll._dead) return;
    if (_ragdoll._itemL != null && _ragdoll._itemL._type == ItemScript.ItemType.GUN)
      _ragdoll._itemL.ToggleLaser();
    if (_ragdoll._itemR != null && _ragdoll._itemR._type == ItemScript.ItemType.GUN)
      _ragdoll._itemR.ToggleLaser();*/
  }

  float[] _loadout_info;
  public void SwapLoadouts()
  {
    if (!_ragdoll.CanSwapWeapons()) return;
    if (!_profile._loadout._two_weapon_pairs) return;

    var clip_l = -1;
    var clip_r = -1;

    var useTime_l = -1f;
    var useTime_r = -1f;

    // Save equipment info
    if (_loadout_info == null)
      _loadout_info = new float[4];
    else
    {
      clip_l = (int)_loadout_info[0];
      clip_r = (int)_loadout_info[1];

      useTime_l = _loadout_info[2];
      useTime_r = _loadout_info[3];
    }
    _loadout_info[0] = _ragdoll._itemL ? _ragdoll._itemL.Clip() : -1;
    _loadout_info[1] = _ragdoll._itemR ? _ragdoll._itemR.Clip() : -1;

    _loadout_info[2] = _ragdoll._itemL ? _ragdoll._itemL._useTime : -1f;
    _loadout_info[3] = _ragdoll._itemR ? _ragdoll._itemR._useTime : -1f;

    _profile._equipmentIndex++;
    _ragdoll.SwapItems(
      System.Tuple.Create(_profile._item_left, clip_l, useTime_l),
      System.Tuple.Create(_profile._item_right, clip_r, useTime_r),
      _profile._equipmentIndex
    );
    _profile.UpdateIcons();
  }

  public void OnRefill()
  {
    _loadout_info = new float[4] { -1f, -1f, -1f, -1f };
  }

  public void Reload()
  {
    if (AutoPlayer._Capturing)
      AutoPlayer.UpdateCapture(new PlayerData()
      {
        _position = transform.position,
        _forward = transform.forward,
        _actions = new KeyValuePair<string, float>[] { new KeyValuePair<string, float>("reload", 0f) }
      });

    _ragdoll.Reload();
  }
  public void ReloadMap()
  {
    var reset = true;
    if (GameScript.IsSurvival() && !_ragdoll._dead)
    {

      return;
    }
    // If dead, check if all other players dead
    if (_ragdoll._dead)
    {
      foreach (var p in _Players)
        if (!p._ragdoll._dead)
        {
          reset = false;
          break;
        }
    }
    if (reset && !GameScript._Singleton._GameEnded)
    {
      if (GameScript.ReloadMap(true, true))
      {
        // Save playerpref
        if (!GameScript.TutorialInformation._HasRestarted) GameScript.TutorialInformation._HasRestarted = true;
      }
    }
  }

  void ToggleFireModes()
  {
    /*float useMult = 4f;
    void changeAuto(params ItemScript[] items)
    {
      foreach (ItemScript item in items)
      {
        if (item == null || item._type != ItemScript.ItemType.GUN) continue;
        item._auto = !item._auto;
        if (item._auto)
          item._useRate /= useMult;
        else
          item._useRate *= useMult;
        FunctionsC.PlaySound(ref _ragdoll._audioPlayer_steps, "Ragdoll/Footstep", 1.15f, 1.2f);
      }
    }
    changeAuto(_ragdoll._itemL, _ragdoll._itemR);*/
  }

  // 'Spread' arms for 90 degree angle
  void ExtendArms()
  {
    bool extendleft = true, extendright = true;
    if (_ragdoll._itemL && _ragdoll._itemL.IsMelee())
      extendleft = false;
    if (_ragdoll._itemR && _ragdoll._itemR.IsMelee())
      extendright = false;
    if (extendleft)
      _ragdoll.LeftArmOut();
    if (extendright)
      _ragdoll.RightArmOut();
    if (!extendleft && !extendright)
      _ragdoll.ArmsDown();
  }

  float[] _taunt_times;
  void Taunt(int iter)
  {
    if (Menu2._InMenus) return;
    switch (iter)
    {
      case (1):
        if (!GameScript.IsSurvival() && Time.time - _taunt_times[iter] < 0.75f) return;
        break;
    }
    _taunt_times[iter] = Time.time;
    switch (iter)
    {
      // Up
      case (0):
        _ragdoll.SwapItemHands(_profile._equipmentIndex);
        _profile.UpdateIcons();
        break;

      // Down
      case (1):
        // If survival, throw money
        if (GameScript.IsSurvival())
        {
          void ThrowMoney()
          {
            //if (!GameScript.SurvivalMode.HasPoints(_id, 10)) return;
            var points = 10;
            for (; points > 0; points--)
            {
              if (GameScript.SurvivalMode.HasPoints(_id, points))
                break;
            }
            if (points == 0) return;

            GameScript.SurvivalMode.SpendPoints(_id, points);
            var money = GameObject.Instantiate(GameResources._Money) as GameObject;
            GameScript.SurvivalMode.AddMoney(money);
            money.name = $"Money {points}";
            money.transform.parent = GameScript._Singleton.transform;
            money.transform.position = _ragdoll._hip.position + _ragdoll._hip.transform.forward * 0.2f;
            var collider = money.GetComponent<Collider>();
            var collider0 = _ragdoll._hip.GetComponents<Collider>()[1];
            //_ragdoll.IgnoreCollision(collider);
            Physics.IgnoreCollision(collider, collider0);
            collider.enabled = true;
            var rb = money.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.AddForce(_ragdoll._hip.transform.forward * 100f);
            FunctionsC.PlaySound(ref _ragdoll._audioPlayer_extra, "Ragdoll/Throw", 0.9f, 1.1f);

            IEnumerator sizeChange()
            {
              yield return new WaitForSeconds(0.2f);
              if (collider != null && collider0 != null)
                FunctionsC.PlaySound(ref _ragdoll._audioPlayer_extra, "Survival/Points_Land_Floor", 0.9f, 1.1f);
              yield return new WaitForSeconds(0.3f);
              if (collider != null && collider0 != null)
                Physics.IgnoreCollision(collider, collider0, false);
            }
            StartCoroutine(sizeChange());
          }
          ThrowMoney();
        }

        // If classic, whistle
        else
        {
          FunctionsC.PlaySound(ref _ragdoll._audioPlayer_steps, "Ragdoll/Whistle", 0.85f, 1.2f);
          EnemyScript.CheckSound(_ragdoll._hip.position + _ragdoll._hip.transform.forward * 3f, _ragdoll._hip.position, EnemyScript.Loudness.SOFT);
          _ragdoll.DisplayText("!");

          _last_whistle = Time.time;
        }

        /*if (_tauntIter >= 0)
        {
          _tauntIter = -1;
          break;
        }*/



        break;
      // Left
      case (2):
        //_profile._loadoutIter--;
        // Check interactables
        if (_currentInteractable != null)
          _currentInteractable.Interact(this, CustomObstacle.InteractSide.LEFT);
        break;
      // Right
      case (3):
        //_profile._loadoutIter++;
        // Check interactables
        //if (gamepad.buttonNorth.wasPressedThisFrame)
        //  if (_currentInteractable != null)
        //    _currentInteractable.Interact(this, ActiveRagdoll.Side.LEFT);
        if (_currentInteractable != null)
          _currentInteractable.Interact(this, CustomObstacle.InteractSide.RIGHT);
        break;
    }
  }

  //
  public static PlayerScript GetClosestPlayerTo(Vector2 position)
  {

    if (_Players.Count == 0)
    {
      return null;
    }
    if (_Players.Count == 1 && _Players[0]._ragdoll != null && !_Players[0]._ragdoll._dead)
    {
      return _Players[0];
    }

    var distance = 10000f;
    PlayerScript closest_player = null;
    foreach (var player in _Players)
    {
      if (player?._ragdoll._dead ?? true) continue;
      var distance0 = Vector2.Distance(position, new Vector2(player._ragdoll._controller.position.x, player._ragdoll._controller.position.z));
      if (distance0 < distance)
      {
        distance = distance0;
        closest_player = player;
      }
    }

    return closest_player;

  }

  // Cycles through all players to see if one has the exit
  public static bool HasExit()
  {
    foreach (PlayerScript p in _Players)
    {
      if (p._hasExit) return true;
    }
    return false;
  }

  public void OnDamageTaken()
  {
    _profile.UpdateHealthUI();

    // Controller rumble
    if (!mouseEnabled && Settings._ControllerRumble)
    {
      IEnumerator rumble()
      {
        var gamepad = ControllerManager.GetPlayerGamepad(_id);
        gamepad.SetMotorSpeeds(0.2f, 0.2f);

        yield return new WaitForSecondsRealtime(1f);
        if (_ragdoll != null && _ragdoll._dead) { }
        else
          gamepad.SetMotorSpeeds(0f, 0f);
      }
      StartCoroutine(rumble());
    }
  }

  public void OnToggle(ActiveRagdoll source, ActiveRagdoll.DamageSourceType damageSourceType)
  {
    // Record stat
    Stats.RecordDeath(_id);

    // Controller rumble
    if (!mouseEnabled && Settings._ControllerRumble)
    {
      IEnumerator rumble()
      {
        var gamepad = ControllerManager.GetPlayerGamepad(_id);
        gamepad.SetMotorSpeeds(0.8f, 0.8f);

        yield return new WaitForSecondsRealtime(1.5f);

        gamepad.SetMotorSpeeds(0f, 0f);
      }
      StartCoroutine(rumble());
    }

    // Send dead to other clients
    //MultiplayerManager.SendMessage("dead " + string.Format("{0} {1} {2} {3}", _id, _ragdoll._hip.velocity.x, _ragdoll._hip.velocity.y, _ragdoll._hip.velocity.z));

#if UNITY_STANDALONE
    // Check achievements
    if (source != null)
    {
      SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DIE);
      if (source._isPlayer && source._playerScript != null && source._playerScript._id != _id)
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.TEAM_KILL);
      if (damageSourceType == ActiveRagdoll.DamageSourceType.EXPLOSION)
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.EXPLODE);
    }
#endif

    // Survival
    if (GameScript._GameMode == GameScript.GameModes.SURVIVAL)
      GameScript.SurvivalMode.OnPlayerDead(_id);

    // Remove ring
    IEnumerator fadeRing()
    {
      Color c = _profile.GetColor();
      float t = 1f, startA = c.a;
      while (t >= 0f)
      {
        t -= 0.07f;
        c.a = Mathf.Clamp(startA * t, 0f, 1f);
        _ring[0].sharedMaterial.color = c;
        _ring[1].sharedMaterial.color = c;
        yield return new WaitForSeconds(0.05f);
        if (this == null || _ragdoll == null || _ragdoll._hip == null) break;
      }
      if (_ring != null) _ring[0].transform.parent.gameObject.SetActive(false);
    }
    GameScript._Singleton.StartCoroutine(fadeRing());

    // Slow motion on player death
    var lastplayer = true;
    foreach (var p0 in _Players)
      if (p0._id != _id && !p0._ragdoll._dead)
      {
        lastplayer = false;
        break;
      }
    if (Settings._Slowmo_on_death && lastplayer && !HasPerk(Shop.Perk.PerkType.NO_SLOWMO)) _SlowmoTimer += 2f;

    // Check for restart tutorial
    if (lastplayer)
    {
      _All_Dead = true;

      // Save setting before changing
      var saveinfo = GameScript.TutorialInformation._HasRestarted;

      // Embarass player ultimately leading to the return of the title
      TileManager.ShowGameOverText("NOT SNEAKY.", "white", "red");

      // Coroutine to show controls
      IEnumerator FlashRestart()
      {
        var offset = GameResources._Camera_Main.transform.up * 4f;
        float pretimer = 0f, timer = 0f, supertimer = 0f, startpos = 5f;
        var flicker = false;

        var tutorial_keyboard = ControllerManager._Gamepads.Count == 0;
        var tutorial_ui0 = tutorial_keyboard ? GameScript.TutorialInformation._Tutorial_Restart_Keyboard0 : GameScript.TutorialInformation._Tutorial_Restart_Controller0;
        var tutorial_ui1 = tutorial_keyboard ? GameScript.TutorialInformation._Tutorial_Restart_Keyboard1 : GameScript.TutorialInformation._Tutorial_Restart_Controller1;

        while (this != null && _ragdoll != null && _ragdoll._head != null)
        {
          yield return new WaitForSecondsRealtime(0.01f);
          // Wait before showing tutorial; wait longer if already seen tutorial
          pretimer += 0.05f;
          if (pretimer < (saveinfo ? 38f : 5f)) continue;
          // Ease controls into scene
          supertimer += 0.05f;
          tutorial_ui0.position =
            tutorial_ui1.position =
            GameResources._Camera_Main.transform.position + GameResources._Camera_Main.transform.forward * 10f + offset +
            GameResources._Camera_Main.transform.up * (Easings.ElasticEaseIn(1f - (Mathf.Clamp(supertimer, 0f, startpos) / startpos)) * startpos);
          tutorial_ui1.position += GameResources._Camera_Main.transform.forward * 0.01f;
          tutorial_ui0.LookAt(GameResources._Camera_Main.transform, Vector3.forward);
          tutorial_ui0.Rotate(90f, 0f, 0f);
          tutorial_ui1.LookAt(GameResources._Camera_Main.transform, Vector3.forward);
          tutorial_ui1.Rotate(90f, 0f, 0f);
          // Flash controls
          timer = Mathf.Clamp(timer += 0.05f, 0f, 1f);
          if (timer == 1f)
          {
            timer = 0f;
            flicker = !flicker;
            tutorial_ui0.gameObject.SetActive(flicker);
            tutorial_ui1.gameObject.SetActive(!flicker);
          }
        }
        // Hide tutorialinfo
        tutorial_ui0.position =
          tutorial_ui1.position =
          new Vector3(1000f, 0f, 0f);
      }
      GameScript._Singleton.StartCoroutine(FlashRestart());
    }
    // Play death sound
    //_ragdoll._audioPlayer.volume = 1f;

    // If has the exit and dies, re-drop the exit so someone else can pick it up
    if (!_hasExit || GameScript._Singleton._GameEnded) return;
    GameScript.ToggleExit(false);
    _hasExit = false;
    GameScript._inLevelEnd = false;
    Powerup p = FunctionsC.SpawnPowerup(Powerup.PowerupType.END);
    p.transform.position = _ragdoll._hip.transform.position;
    p.Init();
  }

  CustomObstacle _currentInteractable;
  public void RemoveInteractable()
  {
    _currentInteractable = null;
  }

  static float _LastMoneyPickupNoiseTime;
  public void OnTriggerEnter(Collider other)
  {
    if (_ragdoll == null || _ragdoll._dead) return;
    switch (other.name)
    {
      case ("SHURIKEN"):
      case ("SHURIKEN_BIG"):
        other.GetComponent<UtilityScript>().PickUp(this);
        break;
      default:
        // Pick up points
        if (other.name.StartsWith("Money"))
        {
          var amount = int.Parse(other.name.Split(' ')[1]);
          GameScript.SurvivalMode.GivePoints(_id, amount);
          GameObject.Destroy(other.gameObject);
          if (Time.time - _LastMoneyPickupNoiseTime > 0.1f)
          {
            _LastMoneyPickupNoiseTime = Time.time;
            FunctionsC.PlaySound(ref _ragdoll._audioPlayer_extra, "Survival/Pickup_Points", 0.9f, 1.1f);
          }
          break;
        }
        break;
    }
  }
  public void OnTriggerStay(Collider other)
  {
    if (!_ragdoll._dead && other.name.Equals("PlayerSpawn") && _hasExit)
      GameScript._inLevelEnd = true;
    switch (other.name)
    {
      case ("Interactable"):
        if (_currentInteractable != null) break;
        _currentInteractable = other.gameObject.GetComponent<CustomObstacle>();
        break;
    }
  }
  public void OnTriggerExit(Collider other)
  {
    // Stop being in end of level
    if (!_ragdoll._dead && other.name.Equals("PlayerSpawn") && _hasExit)
      GameScript._inLevelEnd = false;
    // Check interactable
    if (_currentInteractable != null)
      if (other.gameObject.GetInstanceID() == _currentInteractable.gameObject.GetInstanceID())
      {
        _currentInteractable = null;
      }
  }

  // Destroy dependancies
  private void OnDestroy()
  {
    // Despawn utilities
    if (_utilities_left != null)
      for (int i = _utilities_left.Count - 1; i > 0; i--)
      {
        if (_utilities_left[i] != null)
          GameObject.Destroy(_utilities_left[i].gameObject);
      }
    if (_utilities_right != null)
      for (int i = _utilities_right.Count - 1; i > 0; i--)
      {
        if (_utilities_right[i] != null)
          GameObject.Destroy(_utilities_right[i].gameObject);
      }

    // Despawn ring
    if (_ring == null || _ring[0] == null || _ring[0].transform.parent == null) return;
    GameObject.Destroy(_ring[0].transform.parent.gameObject);
    _ring = null;
  }

  public System.Tuple<bool, ActiveRagdoll.Side> HasUtility(UtilityScript.UtilityType utility)
  {
    if (_equipment._utilities_left != null && _equipment._utilities_left.Length > 0 && _equipment._utilities_left[0] == utility)
      return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT);
    if (_equipment._utilities_right != null && _equipment._utilities_right.Length > 0 && _equipment._utilities_right[0] == utility)
      return System.Tuple.Create(true, ActiveRagdoll.Side.RIGHT);
    return System.Tuple.Create(false, ActiveRagdoll.Side.LEFT);
  }
  public System.Tuple<bool, ActiveRagdoll.Side> HasUtility(UtilityScript.UtilityType utility, ActiveRagdoll.Side side)
  {
    if (_equipment._utilities_left != null && _equipment._utilities_left.Length > 0 && _equipment._utilities_left[0] == utility && side == ActiveRagdoll.Side.LEFT)
      return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT);
    if (_equipment._utilities_right != null && _equipment._utilities_right.Length > 0 && _equipment._utilities_right[0] == utility && side == ActiveRagdoll.Side.RIGHT)
      return System.Tuple.Create(true, ActiveRagdoll.Side.RIGHT);
    return System.Tuple.Create(false, side);
  }

  public static class AutoPlayer
  {
    public class CapturedData
    {
      public PlayerData[] _playerData;
      public float[] _TimeScales;
    }

    public static List<CapturedData> _Data;

    static List<PlayerData> _CapturedDataList;
    static List<float> _TimeScalesList;

    public static void Erase()
    {
      _Data = new List<CapturedData>();
    }

    static float _Time;

    public static bool _Capturing;
    // Start / stop capturing
    public static void Capture()
    {
      _Capturing = !_Capturing;
      // Stopped; handle captured data
      if (!_Capturing)
      {

        var data = new CapturedData()
        {
          _playerData = _CapturedDataList.ToArray(),
          _TimeScales = _TimeScalesList.ToArray()
        };
        if (_Data == null) _Data = new List<CapturedData>();
        _Data.Add(data);

        var localtest = _CapturedDataList.ToArray();
        var localtest2 = _TimeScalesList.ToArray();
        Debug.LogError("Stopped capture");
        return;
      }
      // Started; begin capturing
      _Time = Time.time;
      _CapturedDataList = new List<PlayerData>();
      _TimeScalesList = new List<float>();
      Debug.LogError("Started capture");
    }

    // Capture player data
    public static void UpdateCapture(PlayerData data)
    {
      _CapturedDataList.Add(data);
      _TimeScalesList.Add(Time.time - GameScript._LevelStartTime);
    }

    public static bool _Playing;
    static int _PlaybackIndex;
    public static void Playback()
    {
      _Playing = !_Playing;
      if (!_Playing) // Stopped playback
      {
        Debug.LogError("Stopped playback");
        return;
      }
      // Started playback
      _Time = Time.time;
      _PlaybackIndex = 0;

      if (_Data.Count > 1)
      {
        Settings._NumberPlayers = 2;
        PlayerspawnScript._PlayerSpawns[0].SpawnPlayer();
      }
      if (_Data.Count > 2)
      {
        Settings._NumberPlayers = 3;
        PlayerspawnScript._PlayerSpawns[0].SpawnPlayer();
      }
      if (_Data.Count > 3)
      {
        Settings._NumberPlayers = 4;
        PlayerspawnScript._PlayerSpawns[0].SpawnPlayer();
      }

      Debug.LogError("Started playback");
    }

    public static void ControlPlayer(PlayerScript p, int data_iter = 0)
    {
      int index = _PlaybackIndex;
      if (data_iter == 0) _PlaybackIndex++;
      CapturedData capturedData = _Data[data_iter];
      if (index >= capturedData._playerData.Length)
        return;
      // Apply to player
      PlayerData data = capturedData._playerData[index];
      p.transform.position = data._position;
      p.transform.forward = data._forward;
      // Actions
      if (data._actions != null)
        foreach (var action in data._actions)
          switch (action.Key)
          {
            // Use left item
            case ("left"):
              if (action.Value == 1f)
                p._ragdoll.UseLeftDown();
              else if (action.Value == -1f)
                p._ragdoll.UseLeftUp();
              break;
            // Use right item
            case ("right"):
              if (action.Value == 1f)
                p._ragdoll.UseRightDown();
              else if (action.Value == -1f)
                p._ragdoll.UseRightUp();
              break;
            // Reload
            case ("reload"):
              p._ragdoll.Reload();
              break;
          }
    }
  }
  public class PlayerData
  {
    public Vector3 _position, _forward;
    public KeyValuePair<string, float>[] _actions;
  }
}