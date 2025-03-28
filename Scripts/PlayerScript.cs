﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Net.Sockets;



#if !DISABLESTEAMWORKS
using Steamworks;
#endif

public class PlayerScript : MonoBehaviour, PlayerScript.IHasRagdoll
{


  public interface IHasRagdoll
  {
    public ActiveRagdoll _Ragdoll { get; set; }
  }

  // Singleton
  public static List<PlayerScript> s_Players;
  static Settings.SettingsSaveData SettingsModule { get { return Settings.s_SaveData.Settings; } }
  static Settings.LevelSaveData LevelModule { get { return Settings.s_SaveData.LevelData; } }

  //
  public static bool _All_Dead;

  public static int _PLAYERID = 0;
  public int _Id;

  // Ragdoll
  ActiveRagdoll _ragdoll;
  public ActiveRagdoll _Ragdoll { get { return _ragdoll; } set { _ragdoll = value; } }

  // Holds values for camera position / rotation to lerp to
  public Vector3 _camPos, _camRot;

  Vector2 _saveInput;
  public Vector2 GetInput()
  {
    return _saveInput;
  }

  bool _spawnRunCheck;

  public GameScript.ItemManager.Items _itemLeft, _itemRight;

  public static readonly float
  MOVESPEED = 4f,
    RUNSPEED = 1.25f,
    ROTATIONSPEED = 2f;

  float _swordRunLerper = 0.7f;

  bool mouseEnabled, _setCamera, _centerCamera;

  public bool _HasExit, _CanDetect;

  float _spawnTimer;

  public NavMeshAgent _agent;

  static float _cY, _cX;

  public List<UtilityScript> _UtilitiesLeft, _UtilitiesRight;

  public GameScript.PlayerProfile _Profile
  {
    get
    {
      return GameScript.PlayerProfile.s_Profiles[_Id];
    }
  }

  public GameScript.PlayerProfile.Equipment _Equipment
  {
    get { return _Profile._Equipment; }
  }

  int _saveLoadoutIndex;
  public void SetNewLoadout()
  {
    _saveLoadoutIndex = -1;
  }

  //
  [System.NonSerialized]
  public GameScript.PlayerProfile.Equipment _EquipmentStart;
  [System.NonSerialized]
  public bool _EquipmentChanged;
  public static int s_NumPlayersStart;
  public static int[] s_ExtrasSnapshot;

  // Colored ring under player
  MeshRenderer[] _ring;
  public static Material[] s_Materials_Ring;

  bool _isauto;
  static float s_autoUseRate;
  static ActiveRagdoll.Side s_autoSide;

  // Last time whistled
  public float _LastWhistle;

  public static int s_PlayerCountSave;

  [System.NonSerialized]
  public int _PlayerSpawnId;
  PlayerspawnScript _playerSpawn { get { return PlayerspawnScript._PlayerSpawns[_PlayerSpawnId]; } }

  //
  PlayerScript _connectedTwin;
  ActiveRagdoll.Side _connectedTwinSide;
  public bool _IsOriginalTwin;
  public bool _IsOriginal { get { return _Id == 0 && (!_HasTwin || _IsOriginalTwin); } }
  public bool _HasTwin { get { return _connectedTwin != null; } }
  bool _isLeftTwin { get { return !_HasTwin || _connectedTwinSide == ActiveRagdoll.Side.LEFT; } }
  bool _isRightTwin { get { return !_HasTwin || _connectedTwinSide == ActiveRagdoll.Side.RIGHT; } }

  // Use this for initialization
  void Start()
  {
    _All_Dead = false;

    _SlowmoTimer = 0f;
    Time.timeScale = 1f;
    _spawnRunCheck = LevelModule.ExtraTime == 0;

    // Setup ragdoll
    var ragdollObj = Instantiate(
      GameResources._Ragdoll,
      transform.position,
      transform.rotation * Quaternion.Euler(0f, 90f, 0f),
      transform.parent
    );
    var health = GameScript.s_GameMode == GameScript.GameModes.VERSUS ? VersusMode.s_Settings._PlayerHealth : (GameScript.IsSurvival() ? 3 : 1);
    _ragdoll = new ActiveRagdoll(ragdollObj, transform)
    {
      _IsPlayer = true,
      _health = health
    };

    // Check armor for editor maps
    if (!GameScript.IsSurvival())
      if (_Equipment._Perks != null && _Equipment._Perks.Contains(Shop.Perk.PerkType.ARMOR_UP))
      {
        _ragdoll.AddArmor();
        _ragdoll._health = 3;
      }

    // Get NavMeshAgent
    _agent = transform.GetComponent<NavMeshAgent>();

    // Add self to list of players
    if (s_Players == null)
      s_Players = new List<PlayerScript>();
    s_Players.Add(this);

    //
    if (_HasTwin)
    {

      _Id = _connectedTwin._Id;

    }
    else
    {

      // Give unique _PlayerID and decide input based upon _PlayerID
      _Id = _PLAYERID++ % Settings._NumberPlayers;

      // Check all players to make sure no duplicate _PlayerID
      var loops = 0;
      while (true && loops++ < 5)
      {
        var breakLoop = true;
        foreach (var p in s_Players)
        {
          if (p == null) continue;

          // If self, skip
          if (p.transform.GetInstanceID() == transform.GetInstanceID() || p._ragdoll._IsDead) continue;

          // If has same _PlayerID, increment id and set do not break while loop
          if (p._Id == _Id)
          {
            _Id = _PLAYERID++ % Settings._NumberPlayers;
            breakLoop = false;
            break;
          }
        }
        // If break loop, break the loop
        if (breakLoop) break;
      }
      if (loops == 5) Debug.LogError("Duplicate _PlayerID, may cause bug " + _Id);

    }

    // Create health UI
    //_profile.CreateHealthUI(health);

    // Assign color by _PlayerID
    _ragdoll.ChangeColor(_Profile.GetColor());

    // Create ring
    var new_ring = Instantiate(TileManager._Ring.gameObject) as GameObject;
    _ring = new MeshRenderer[] { new_ring.transform.GetChild(0).GetComponent<MeshRenderer>(), new_ring.transform.GetChild(1).GetComponent<MeshRenderer>() };
    Resources.UnloadAsset(_ring[0].sharedMaterial);
    Resources.UnloadAsset(_ring[1].sharedMaterial);
    _ring[0].sharedMaterial = s_Materials_Ring[_Id];
    _ring[1].sharedMaterial = s_Materials_Ring[_Id];
    Vector3 localscale = _ring[0].transform.parent.localScale;
    localscale *= 0.8f;
    localscale.y = 2f;
    _ring[0].transform.parent.localScale = localscale;
    ChangeRingColor(GetRingColor());

    // Move ring with hip
    var hippos = transform.position;
    hippos.y = -1.38f;
    if (!_ragdoll._IsDead)
      _ring[0].transform.parent.position = hippos;

    // Equip starting weapons
    _Profile._EquipmentIndex = 0;

    CheckSpawnTwin();

    _itemLeft = _isLeftTwin || Shop.IsActuallyTwoHanded(_Profile._ItemLeft) ? _Profile._ItemLeft : GameScript.ItemManager.Items.NONE;
    _itemRight = _isRightTwin || Shop.IsActuallyTwoHanded(_Profile._ItemRight) ? _Profile._ItemRight : GameScript.ItemManager.Items.NONE;

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
    else if (_ragdoll._ItemL == null || !_ragdoll._ItemL._twoHanded)
    {
      _ragdoll.AddArmJoint(ActiveRagdoll.Side.RIGHT);
      _ragdoll.UnequipItem(ActiveRagdoll.Side.RIGHT);
    }
    _saveLoadoutIndex = _Profile._LoadoutIndex;

    // Set camera position
    _camPos = GameResources._Camera_Main.transform.position;

    _spawnTimer = 0.2f;
    _CanDetect = true;

    _lastInputTriggers = new Vector2(-1f, -1f);

    _ragdoll._rotSpeed = 2.1f * ROTATIONSPEED;
    _agent.stoppingDistance = 0.5f;

    _taunt_times = new float[4];
    _dpadPressed = new float[4];

    GameScript.UpdateAmbientLight();
    if (!TileManager._HasLocalLighting)
    {
      if (!GameScript.s_Backrooms)
        AddLight();
    }

    RegisterUtilities();

    _Profile.OnPlayerSpawn();
    _Profile.UpdateHealthUI();

    ControllerManager.GetPlayerGamepad(_Id)?.SetMotorSpeeds(0f, 0f);

    // Handle rain VFX
    if (_IsOriginal)
    {

      // Clean up old light if exists
      if (GameScript.s_Singleton._Thunder_Light != null)
        GameObject.DestroyImmediate(GameScript.s_Singleton._Thunder_Light.gameObject);

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

        GameScript.s_Singleton._Thunder_Light = thunder_light;
      }

      // Clean up rain SFX
      else
      {

      }
    }

    // Check crown
    if (LevelModule.ExtraCrownMode != 0)
      if (GameScript.s_CrownPlayer == _Profile._Id)
      {
        _ragdoll.AddCrown();
      }
  }

  //
  Color GetRingColor()
  {
    if (GameScript.s_GameMode == GameScript.GameModes.VERSUS && !VersusMode.s_Settings._FreeForAll)
      return VersusMode.GetTeamColorFromPlayerId(_Id);
    return _Profile.GetColor();
  }

  public void RegisterEquipment()
  {
    // Save start loadout for challenges
    _EquipmentStart = _Equipment;
    _EquipmentChanged = false;
  }

  public void RegisterUtilities()
  {
    // Register utilities
    if (_isLeftTwin)
      RegisterUtility(ActiveRagdoll.Side.LEFT);
    if (_isRightTwin)
      RegisterUtility(ActiveRagdoll.Side.RIGHT);
  }

  public void RegisterUtility(ActiveRagdoll.Side side, int amount = -1)
  {
    // Check extra
    if (Settings._Extras_CanUse)
    {
      if (_ragdoll?._IsPlayer ?? false)
        switch (LevelModule.ExtraPlayerAmmo)
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
      _UtilitiesLeft = new List<UtilityScript>();
      foreach (var utility in _Equipment._UtilitiesLeft)
      {
        for (var i = Shop.GetUtilityCount(utility); i > 0 && max-- > 0; i--)
          AddUtility(utility, side);
      }
    }
    else
    {
      _UtilitiesRight = new List<UtilityScript>();
      foreach (var utility in _Equipment._UtilitiesRight)
      {
        var count = Shop.GetUtilityCount(utility);
        for (; count > 0 && max-- > 0; count--)
          AddUtility(utility, side);
      }
    }
  }

  public void AddUtility(UtilityScript.UtilityType utility, ActiveRagdoll.Side side)
  {
    var util = UtilityScript.GetUtility(utility);
    util.SetSide(side);
    (side == ActiveRagdoll.Side.LEFT ? _UtilitiesLeft : _UtilitiesRight).Add(util);
    util.RegisterUtility(_ragdoll);
  }

  // Remove utility
  public void NextUtility(ActiveRagdoll.Side side)
  {
    var utilities = (side == ActiveRagdoll.Side.LEFT ? _UtilitiesLeft : _UtilitiesRight);
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
    var utilities = (side == ActiveRagdoll.Side.LEFT ? _UtilitiesLeft : _UtilitiesRight);
    return (utilities != null && utilities.Count > 0 &&
      utilities[0] == utility);
  }

  public int UtilityCount(ActiveRagdoll.Side side)
  {
    return (side == ActiveRagdoll.Side.LEFT ? _UtilitiesLeft : _UtilitiesRight).Count;
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
    foreach (var p in s_Players)
    {
      if (p == null || p._ragdoll == null || p._ragdoll._IsDead) continue;
      p.AddLight();
    }
  }

  public static void Reset()
  {
    if (s_Players != null)
      foreach (var p in s_Players)
      {
        p._HasExit = false;
        if (p != null && p.gameObject != null) Destroy(p.gameObject);
      }

    GameScript.s_InLevelEndPlayer = null;
    s_Players = null;
  }

  public void ChangeRingColor(Color c)
  {
    if (_ring == null) return;
    _ring[0].sharedMaterial.SetColor("_EmissionColor", c);
    c.a = 0.85f;
    _ring[0].sharedMaterial.color = c;
  }

  public static void StartLevelTimer()
  {
    _TimerStarted = true;
    GameScript.ToggleExitLight(false);

    if (s_Players[0]._level_ratings_shown)
      TileManager._Text_LevelTimer_Best.text = TileManager._Text_LevelTimer_Best.text.Split("\n")[0];
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
    if (!_TimerStarted && _IsOriginal && _spawnTimer <= 0f)
    {

      var player_farthest = FunctionsC.GetFarthestPlayerFrom(_playerSpawn.transform.position);
      if (player_farthest._distance > 0.5f)
      {
        StartLevelTimer();
      }
    }

    // Paused dpad reset
    if (
      GameScript.s_Paused &&
      (_dpadPressed[0] != 0f || _dpadPressed[1] != 0f || _dpadPressed[2] != 0f || _dpadPressed[3] != 0f)
    )
    {
      _dpadPressed = new float[4];
    }

    // Ratings
    if (
      _IsOriginal && !_level_ratings_shown && !TileManager._Level_Complete && !GameScript.s_Paused &&
      (
        (!_TimerStarted && Time.time - GameScript.s_LevelStartTime > 3f) ||
        (_All_Dead && Time.unscaledTime - _ragdoll._time_dead > 3f)
      )
    )
    {
      _level_ratings_shown = true;

      // Rating times
      var best_dev_time = TileManager._LevelTime_Dev;
      var level_time_best = LevelModule.GetLevelBestTime();

      var medal_times = Levels.GetLevelRatingTimings(best_dev_time);
      var ratings = Levels.GetLevelRatings();

      var index = 0;
      var medal_index = -1;
      TileManager._Text_LevelTimer_Best.text += "\n\n";
      var medal_format = "<color={0}>{1,-5}: {2,-6}</color>\n";
      foreach (var time in medal_times)
      {
        var time_ = time.ToStringTimer().ParseFloatInvariant();
        if (level_time_best != -1f && level_time_best <= time_ && medal_index == -1)
        {
          medal_index = index;
        }
        index++;
      }

      // FX
      for (var i = medal_times.Length - 1; i >= 0; i--)
      {
        var time_ = medal_times[i].ToStringTimer().ParseFloatInvariant();
        TileManager._Text_LevelTimer_Best.text += string.Format(medal_format, ratings[i].Item2, ratings[i].Item1, medal_times[i] == -1f ? "-" : time_.ToStringTimer() + (medal_index == i ? "*" : ""));
      }
    }

#if DEBUG
    if (ControllerManager.GetKey(ControllerManager.Key.DELETE))
    {
      _isauto = !_isauto;
    }
    if (_isauto && !_ragdoll._IsDead && !GameScript.s_Paused)
    {
      _rearrangeTime -= Time.deltaTime * (_agent.pathStatus != UnityEngine.AI.NavMeshPathStatus.PathComplete ? 10f : 1f);
      if (_rearrangeTime <= 0f && !s_Players[0]._ragdoll._IsDead)
      {
        var pos = s_Players[0]._ragdoll._Hip.position;
        var dis = MathC.Get2DDistance(pos, transform.position);
        var maxD = 5f;

        if (dis < 2f)
        {
          pos = _ragdoll._Hip.position;
          maxD = 6f;
        }
        if (GameScript.s_GameMode == GameScript.GameModes.CLASSIC)
        {

          if (!HasExit())
          {

            var minDist = 5f;
            var moved = false;
            foreach (var bullet in ItemScript._BulletPool)
            {
              if (!bullet.gameObject.activeSelf || bullet.GetRagdollID() == _ragdoll._Id) continue;
              if (MathC.Get2DDistance(_ragdoll._Hip.position, bullet.transform.position) < minDist)
              {
                //_SlowmoTimer = Mathf.Clamp(_SlowmoTimer + 1f, 0f, 2f);
                var dir = Quaternion.AngleAxis(-90, Vector3.up) * bullet._rb.linearVelocity;
                _agent.SetDestination(_ragdoll.Transform.position + dir * 10f);
                moved = true;
                break;
              }
            }

            if (!moved)
              _agent.SetDestination(Powerup._Powerups[0].transform.position);
          }
          else
          {
            _agent.SetDestination(_playerSpawn.transform.position);
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
        if (_targetRagdoll == null || _targetRagdoll._IsDead) _lastDistance = 10000f;

        for (var i = 0; i < 10; i++)
        {
          var next_enemy = EnemyScript._Enemies_alive[_targetIter++ % EnemyScript._Enemies_alive.Count];
          var next_ragdoll = next_enemy._Ragdoll;

          if (next_ragdoll != null && (_targetRagdoll == null || next_ragdoll._Id != _targetRagdoll._Id))
          {
            var path = new UnityEngine.AI.NavMeshPath();
            if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, next_enemy.transform.position, TileManager._navMeshSurface2.agentTypeID, path))
            {
              var dist = FunctionsC.GetPathLength(path.corners);
              if (dist < _lastDistance)
              {
                _targetRagdoll = next_enemy._Ragdoll;
                _lastDistance = dist;
              }
            }
          }
        }

        if (_targetRagdoll != null)
        {
          transform.LookAt(_targetRagdoll._Hip.transform.position);

          _ragdoll.ToggleRaycasting(false);
          var hit = new RaycastHit();
          if (Physics.SphereCast(new Ray(_ragdoll._transform_parts._head.transform.position, _ragdoll._Hip.transform.forward * 100f + Vector3.up * 0.3f), 0.1f, out hit, GameResources._Layermask_Ragdoll))
          {
            if (_targetRagdoll.IsSelf(hit.collider.gameObject))
            {
              var dist = 15f;
              var switch_sides = false;

              for (var i = 0; i < 2; i++)
              {
                if (switch_sides)
                  s_autoSide = s_autoSide == ActiveRagdoll.Side.LEFT ? ActiveRagdoll.Side.RIGHT : ActiveRagdoll.Side.LEFT;
                switch_sides = false;

                if (Time.time - s_autoUseRate < 0) break;

                var item = (s_autoSide == ActiveRagdoll.Side.LEFT ? _ragdoll._ItemL : _ragdoll._ItemR);

                if (!item)
                {
                  switch_sides = true;
                  continue;
                }

                if (item)
                {
                  // Melee
                  if (item._isMelee)
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
                    if (s_autoSide == ActiveRagdoll.Side.LEFT)
                      _ragdoll.UseLeft();
                    else
                      _ragdoll.UseRight();

                    // Set use rate
                    if (item._isMelee)
                      s_autoUseRate = Time.time + item.UseRate();
                    else if (item._fireMode == ItemScript.FireMode.AUTOMATIC)
                    {
                      if (item.NeedsReload())
                        switch_sides = true;
                    }
                    else if (item._fireMode == ItemScript.FireMode.BURST || item._fireMode == ItemScript.FireMode.SEMI)
                    {
                      switch_sides = true;
                      s_autoUseRate = Time.time + 0.2f;
                    }

                    // Check reload
                    if (item.NeedsReload())
                      item.Reload();

                    if (switch_sides)
                      s_autoSide = s_autoSide == ActiveRagdoll.Side.LEFT ? ActiveRagdoll.Side.RIGHT : ActiveRagdoll.Side.LEFT;
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
              var item = i == 0 ? _ragdoll._ItemL : _ragdoll._ItemR;
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
        if (_isOriginal)
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
    if (!_ragdoll.IsActive() && Time.time - GameScript.s_LevelStartTime > 0.2f)
    {
      // Enable ragdoll
      _ragdoll.SetActive(true);
    }

    float unscaled_dt = Time.unscaledDeltaTime,
      dt = Time.deltaTime;
    _SlowmoTimer = Mathf.Clamp(_SlowmoTimer - unscaled_dt, 0f, 2f);

    if (s_Players == null || this == null) return;

    // Move ring with hip
    if (!_ragdoll._IsDead)
    {
      var hippos = _ragdoll._Hip.position;
      hippos.y = -1.38f;
      //if (Time.timeScale < 0.9f)
      //{

      var ringDis = hippos - _ring[0].transform.parent.position;
      if (ringDis.magnitude > 2f)
      {
        _ring[0].transform.parent.position = hippos;
      }
      else
      {
        if (!_ring[0].transform.parent.gameObject.activeSelf)
          _ring[0].transform.parent.gameObject.SetActive(true);

        _ring[0].transform.parent.position += ringDis * Time.deltaTime * 15f;
      }
      //}
      //else
      //  _ring[0].transform.parent.position = hippos;
      var lookpos = _ragdoll._transform_parts._hip.position + _ragdoll._transform_parts._hip.forward * 10f;
      lookpos.y = _ring[0].transform.parent.position.y;
      _ring[0].transform.parent.LookAt(lookpos);

      // Check back at spawn
      var spawnDist = Vector3.Distance(new Vector3(_playerSpawn.transform.position.x, 0f, _playerSpawn.transform.position.z), new Vector3(_ragdoll._Hip.position.x, 0f, _ragdoll._Hip.position.z));
      if (_HasExit)
      {
        if (GameScript._inLevelEnd)
        {
          if (spawnDist > 0.55f)
            GameScript.s_InLevelEndPlayer = null;
        }
        else
        {
          if (spawnDist <= 0.55f)
            GameScript.s_InLevelEndPlayer = this;
        }
      }

    }

    if (GameScript.s_Paused)
    {
      if (_IsOriginal)
      {
        SfxManager.Update(0f);
      }
      return;
    }

    // Handle camera
    UpdateCamera();

    // Handle input
    if (!_isauto)
      HandleInput();

    if (TileManager._LoadingMap || GameScript.s_Singleton._GameEnded || GameScript.s_Paused)
    {
      Time.timeScale = 1f;
      return;
    }

    // Check for timescale change
    if (true)
    {
      if (_IsOriginal)
      {

        // Check should apply time
        if ((GameScript.s_GameMode == GameScript.GameModes.VERSUS && VersusMode.s_Settings._UseSlowmo) || GameScript.s_GameMode != GameScript.GameModes.VERSUS)
        {

          // Update time via player speed
          var time_move = LevelModule.ExtraTime == 1 && Settings._Extras_CanUse;
          if (time_move)
          {
            if (_spawnTimer <= 0f)
            {

              var dirs = Vector2.zero;
              var num = 0;
              for (var i = 0; i < s_Players.Count; i++)
              {
                if (!s_Players[i]._ragdoll._IsDead)
                {
                  num++;
                  dirs += s_Players[i]._saveInput;
                }
              }
              if (num == 0) num = 1;
              dirs /= num;

              var mag = Mathf.Clamp(dirs.magnitude, 0.03f, 1f);
              //if (mag < 0.1f)
              //  mag = 0.0f;
              Time.timeScale = mag;
            }
          }
          else
          {
            // Check player dist
            float minDist = 1.2f, desiredTimeScale = 1f, slowTime = 0.1f, speedMod = 1f;

            if (_SlowmoTimer > 0f)
            {
              var has_perk = false;
              foreach (var p in s_Players)
              {
                if (p._ragdoll._IsDead) continue;
                if (p.HasPerk(Shop.Perk.PerkType.NO_SLOWMO))
                {
                  has_perk = true;
                  break;
                }
              }

              if (!has_perk)
              {
                desiredTimeScale = slowTime;
              }
            }
            else

              // Loop through players
              foreach (var p in s_Players)
              {
                // Check if player dead
                if (p._ragdoll._IsDead || HasPerk(Shop.Perk.PerkType.NO_SLOWMO)) continue;
                foreach (var bullet in ItemScript._BulletPool)
                {

                  if (!bullet.gameObject.activeSelf) continue;

                  var bulletSourceId = bullet.GetRagdollID();
                  if (
                    bulletSourceId == p._ragdoll._Id ||
                    bulletSourceId == (p._ragdoll._grapplee?._Id ?? -1)
                  )
                    continue;

                  if (MathC.Get2DDistance(p._ragdoll._Hip.position, bullet.transform.position) < minDist)
                  {
                    //_SlowmoTimer = Mathf.Clamp(_SlowmoTimer + 1f, 0f, 2f);
                    desiredTimeScale = slowTime;
                    break;
                  }
                }
              }
            if (Time.timeScale > desiredTimeScale)
              speedMod = 4f;
            else
              speedMod = 2f;

            var onealive = false;
            foreach (var player in s_Players)
            {
              if (player._ragdoll != null && !player._ragdoll._IsDead)
              {
                onealive = true; break;
              }
            }
            if (!onealive && GameScript.s_GameMode != GameScript.GameModes.VERSUS) desiredTimeScale = 0f;

            // Update timescale
            {
              var newScale = Mathf.Clamp(Time.timeScale + (desiredTimeScale - Time.timeScale) * unscaled_dt * 5f * speedMod, slowTime, 1f);
              if (newScale < 0.01f)
                Time.timeScale = 0f;
              else if (newScale > 0.99f)
                Time.timeScale = 1f;
              else
                Time.timeScale = newScale;
            }

            //Debug.Log(Time.timeScale);
          }

          // Update sounds with Time.timescale
          var pitch = 1f + -0.7f * ((1f - Time.timeScale) / (0.8f));
          SfxManager.Update(pitch);
        }
      }
    }

    // Update ragdoll
    _ragdoll.Update();
  }

  //
  public static void ResetCamera()
  {
    if (s_Players.Count > 0)
      s_Players[0]._setCamera = false;
  }

  // Lerp camera - taking into account more than one player
  void UpdateCamera()
  {
    if (this == null) return;

    float unscaled_dt = Time.unscaledDeltaTime,
      dt = Time.deltaTime;

    if (_IsOriginal && GameScript.s_Singleton._UseCamera)
    {
      var camera_height = SettingsModule.CameraZoom == Settings.SettingsSaveData.CameraZoomType.NORMAL ? 14f : SettingsModule.CameraZoom == Settings.SettingsSaveData.CameraZoomType.CLOSE ? 10f : 18f;
      if (SettingsModule.UseOrthographicCamera)
      {
        camera_height = 14f;
      }

      // Center camera on map if it does not need to move
      if (!_setCamera)
      {
        _setCamera = true;

        _centerCamera = SettingsModule.UseOrthographicCamera;
        var map_x = TileManager._Map_Size_X;
        var map_y = TileManager._Map_Size_Y;
        //Debug.Log($"{map_x} {map_y}");

        // Backrooms
        if (GameScript.s_Backrooms)
        {
          _centerCamera = true;

          SettingsModule.CameraZoom = Settings.SettingsSaveData.CameraZoomType.NORMAL;
          Settings.SetPostProcessing();
          SettingsModule.CameraZoom = Settings.SettingsSaveData.CameraZoomType.AUTO;
        }

        else if (_centerCamera)
        {

          var zoom = SettingsModule.CameraZoom;
          if (zoom == Settings.SettingsSaveData.CameraZoomType.CLOSE)
          {
            if (map_x > 7 || map_y > 4)
              _centerCamera = false;
          }
          else if (zoom == Settings.SettingsSaveData.CameraZoomType.NORMAL)
          {
            if (map_x > 10 || map_y > 6)
              _centerCamera = false;
          }
          else if (zoom == Settings.SettingsSaveData.CameraZoomType.FAR)
          {
            if (map_x > 14 || map_y > 8)
              _centerCamera = false;
          }
          else if (zoom == Settings.SettingsSaveData.CameraZoomType.AUTO)
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
              SettingsModule.CameraZoom = (Settings.SettingsSaveData.CameraZoomType)zoom_;
              Settings.SetPostProcessing();
              SettingsModule.CameraZoom = Settings.SettingsSaveData.CameraZoomType.AUTO;
            }
            else
            {
              SettingsModule.CameraZoom = Settings.SettingsSaveData.CameraZoomType.NORMAL;
              Settings.SetPostProcessing();
              SettingsModule.CameraZoom = Settings.SettingsSaveData.CameraZoomType.AUTO;

              _centerCamera = false;
            }
          }
        }
      }

      if (_centerCamera)
      {
        _camPos = TileManager._Floor.position;
        _camPos.z += -0.5f;
      }

      // Move camera normally
      else
      {

        float CamYPos = 16f,
          camSpeed = (Time.time - GameScript.s_LevelStartTime < 1f ? 0.8f : 0.4f) * (22f / CamYPos);//,
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
          foreach (var p in s_Players)
          {
            if (p._ragdoll._IsDead && followAlive) continue;
            sharedPos += new Vector3(p._ragdoll._Hip.position.x, 0f, p._ragdoll._Hip.position.z);
            sharedForward += MathC.Get2DVector(p._ragdoll._Hip.transform.forward).normalized * forwardMagnitude;
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

        if (SettingsModule.CameraZoom == Settings.SettingsSaveData.CameraZoomType.FAR)
          sharedForward = Vector3.zero;
        else if (SettingsModule.CameraZoom == 0)
          sharedForward /= 1.4f;

        var changePos = Vector3.zero;
        if (GameScript.s_Singleton._X) changePos.x = sharedPos.x + sharedForward.x;
        if (GameScript.s_Singleton._Y) changePos.y = sharedPos.y;
        if (GameScript.s_Singleton._Z) changePos.z = sharedPos.z + sharedForward.z;

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
      if (SettingsModule.UseOrthographicCamera)
      {
        //_camPos.z -= 1.9f;
      }
      GameResources._Camera_Main.transform.position = _camPos;

      // Set audio listener y
      var audioListenerPos = GameResources.s_AudioListener.transform.position;
      audioListenerPos.y = PlayerspawnScript._PlayerSpawns[0].transform.position.y;
      GameResources.s_AudioListener.transform.position = audioListenerPos;

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
    return Shop.Perk.HasPerk(_Id, perk);
  }

  Vector2 _lastInputTriggers;
  public static float _SlowmoTimer;
  float[] _dpadPressed;

  float _crazyZombieTimer;

  //
  void CheckSpawnTwin()
  {
    // Check twin mod
    if (HasPerk(Shop.Perk.PerkType.TWIN) && _connectedTwin == null)
    {
      _IsOriginalTwin = true;

      PlayerspawnScript.SpawnPlayerAt(transform.position, transform.localEulerAngles.y, (playerScript) =>
      {
        _connectedTwin = playerScript;
        playerScript._connectedTwin = this;

        _connectedTwinSide = ActiveRagdoll.Side.LEFT;
        playerScript._connectedTwinSide = ActiveRagdoll.Side.RIGHT;
      }, true, _PlayerSpawnId);
    }
  }

  //
  void HandleInput()
  {
    // Check if application is focused
    if (!Application.isFocused) return;

    //
    if (AutoPlayer._Playing && _Id != 0)
      return;

    //
    if (_saveLoadoutIndex != _Profile._LoadoutIndex)
      ResetLoadout();

    // Spawn enemies as player gets closer to goal; 3rd difficulty / game mode ?
    if (!GameScript.IsSurvival())
      if (
        _IsOriginal &&
        LevelModule.ExtraHorde == 1 &&
        Settings._Extras_CanUse &&
        (Powerup._Powerups?.Count ?? 0) > 0 &&
        EnemyScript._Enemies_alive.Count < EnemyScript._MAX_RAGDOLLS_ALIVE
      )
      {

        var minSpawnDistance = 0f;
        foreach (var player in s_Players)
        {
          if ((player?._ragdoll._health ?? -1) <= 0) continue;
          var spawnDistance = MathC.Get2DDistance(player._ragdoll._Hip.position, player._playerSpawn.transform.position);

          if (spawnDistance > minSpawnDistance)
            minSpawnDistance = spawnDistance;
        }

        if (minSpawnDistance > 3f)
        {

          _crazyZombieTimer += Time.deltaTime;
          var czt = 0.2f;
          if (_crazyZombieTimer >= czt)
          {
            //if (EnemyScript._Enemies_alive.Count > 0)
            {
              // Save setting
              var saveEnemyMulti = -1;
              if (LevelModule.ExtraEnemyMultiplier == 2)
              {
                saveEnemyMulti = LevelModule.ExtraEnemyMultiplier;
                LevelModule.ExtraEnemyMultiplier = 0;
              }

              //
              while (
                _crazyZombieTimer >= czt &&
                EnemyScript._Enemies_alive.Count < EnemyScript._MAX_RAGDOLLS_ALIVE
              )
              {

                _crazyZombieTimer -= czt;

                //
                var enemy = EnemyScript.SpawnEnemyAt(
                  new EnemyScript.SurvivalAttributes()
                  {
                    _enemyType = GameScript.SurvivalMode.EnemyType.KNIFE_RUN
                  },
                  new Vector2(PlayerspawnScript._PlayerSpawns[0].transform.position.x + 0.5f * Random.value, PlayerspawnScript._PlayerSpawns[0].transform.position.z + 0.5f * Random.value),
                  false,
                  true
                );
                enemy._moveSpeed = 0.5f + 0.15f * Random.value;
              }

              //
              if (saveEnemyMulti > -1)
              {
                LevelModule.ExtraEnemyMultiplier = saveEnemyMulti;
              }
            }
          }
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

    if ((_Id == 0 && Settings._ForceKeyboard) || ControllerManager._NumberGamepads == 0)
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

    //
    var useSword = false;
    if ((_ragdoll._ItemL?._type ?? GameScript.ItemManager.Items.NONE) == GameScript.ItemManager.Items.KATANA)
    {
      useSword = true;

      var triggerDown = _ragdoll._ItemL._TriggerDown;
      _swordRunLerper += ((triggerDown ? 1.5f : 0.7f) - _swordRunLerper) * Time.deltaTime * 0.8f;
    }

    //
    var movespeed = MOVESPEED * (useSword ? _swordRunLerper : 1f);
    var runKeyDown = false;
    if (Time.timeScale < 0.8f)
      unscaled_dt = unscaled_dt * Time.timeScale * 2f;

    // Check arrow keys
    if (mouseEnabled)
    {

      if (_HasTwin)
      {

        if (_isLeftTwin)
        {
          if (ControllerManager.GetKey(ControllerManager.Key.W, ControllerManager.InputMode.HOLD))
            y += 1f;
          if (ControllerManager.GetKey(ControllerManager.Key.S, ControllerManager.InputMode.HOLD))
            y -= 1f;
          if (ControllerManager.GetKey(ControllerManager.Key.D, ControllerManager.InputMode.HOLD))
            x += 1f;
          if (ControllerManager.GetKey(ControllerManager.Key.A, ControllerManager.InputMode.HOLD))
            x -= 1f;
        }
        else
        {
          if (ControllerManager.GetKey(ControllerManager.Key.ARROW_U, ControllerManager.InputMode.HOLD))
            y += 1f;
          if (ControllerManager.GetKey(ControllerManager.Key.ARROW_D, ControllerManager.InputMode.HOLD))
            y -= 1f;
          if (ControllerManager.GetKey(ControllerManager.Key.ARROW_R, ControllerManager.InputMode.HOLD))
            x += 1f;
          if (ControllerManager.GetKey(ControllerManager.Key.ARROW_L, ControllerManager.InputMode.HOLD))
            x -= 1f;
        }

        // Look
        transform.LookAt(transform.position + new Vector3(x, 0f, y));
      }

      // Normal movement
      else
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
      }

      // Throw money
      if (ControllerManager.GetKey(ControllerManager.Key.V))
        Taunt(1);

      // Check versus start
      if (GameScript.s_GameMode != GameScript.GameModes.VERSUS || (GameScript.s_GameMode == GameScript.GameModes.VERSUS && VersusMode.s_PlayersCanMove))
      {

        /// Use items
        // Left item
        if (_isLeftTwin)
        {
          if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.DOWN))
            if (!_ragdoll._ItemL)
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
            if (!_ragdoll._ItemL)
            {
              _ragdoll.UseRightUp();
              saveInput.y = -1f;
            }
            else
            {
              _ragdoll.UseLeftUp();
              saveInput.x = -1f;
            }
        }

        // Right item
        if (_isRightTwin)
        {
          if (ControllerManager.GetMouseInput(1, ControllerManager.InputMode.DOWN))
            if (!_ragdoll._ItemR)
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
            if (!_ragdoll._ItemR)
            {
              _ragdoll.UseLeftUp();
              saveInput.x = -1f;
            }
            else
            {
              _ragdoll.UseRightUp();
              saveInput.y = -1f;
            }
        }

        // Check grapple
        if (
          !_ragdoll._IsDead &&
          _ragdoll._CanGrapple &&
          ControllerManager.GetMouseInput(2, ControllerManager.InputMode.UP)
          )
        {
          _ragdoll.Grapple(true);
        }
        if (_ragdoll._grappling)
        {
          if (_isLeftTwin)
            if (_ragdoll._ItemL == null && ControllerManager.GetMouseInput(0, ControllerManager.InputMode.UP))
              _ragdoll.Grapple(false);

          if (_isRightTwin)
            if (_ragdoll._ItemR == null && ControllerManager.GetMouseInput(1, ControllerManager.InputMode.UP))
              _ragdoll.Grapple(false);
        }
      }

      // Move arms
      if (!_ragdoll._IsDead && ControllerManager.GetKey(ControllerManager.Key.SPACE, ControllerManager.InputMode.HOLD))
        ExtendArms();
      else
        _ragdoll.ArmsDown();

      // Check runkey
      /*if (_Profile._holdRun)
      {
        runKeyDown = (ControllerManager.ShiftHeld());
      }
      else*/
      {
        runKeyDown = ControllerManager.ShiftHeld();
      }

      // Check utility
      if (GameScript.s_GameMode != GameScript.GameModes.VERSUS || (GameScript.s_GameMode == GameScript.GameModes.VERSUS && VersusMode.s_PlayersCanMove))
      {
        if (ControllerManager.GetKey(ControllerManager.Key.Q))
        {
          /*if (_UtilitiesLeft.Count == 0 && _UtilitiesRight.Count > 0)
            _UtilitiesRight[0].UseDown();
          else */
          if (_UtilitiesLeft.Count > 0)
            _UtilitiesLeft[0].UseDown();
        }
        else if (ControllerManager.GetKey(ControllerManager.Key.Q, ControllerManager.InputMode.UP))
        {
          /*if (_UtilitiesLeft.Count == 0 && _UtilitiesRight.Count > 0)
            _UtilitiesRight[0].UseUp();
          else */
          if (_UtilitiesLeft.Count > 0)
            _UtilitiesLeft[0].UseUp();
        }
        if (ControllerManager.GetKey(ControllerManager.Key.E))
        {
          /*if (_UtilitiesRight.Count == 0 && _UtilitiesLeft.Count > 0)
            _UtilitiesLeft[0].UseDown();
          else */
          if (_UtilitiesRight.Count > 0)
            _UtilitiesRight[0].UseDown();
        }
        else if (ControllerManager.GetKey(ControllerManager.Key.E, ControllerManager.InputMode.UP))
        {
          /*if (_UtilitiesRight.Count == 0 && _UtilitiesLeft.Count > 0)
            _UtilitiesLeft[0].UseUp();
          else */
          if (_UtilitiesRight.Count > 0)
            _UtilitiesRight[0].UseUp();
        }
      }

      // Check weapon swap
      if (ControllerManager.GetKey(ControllerManager.Key.TAB))
        SwapLoadouts();

      // Check weapon swap
      if (ControllerManager.GetKey(ControllerManager.Key.G))
      {
        _ragdoll.SwapItemHands(_Profile._EquipmentIndex);
        _Profile.UpdateIcons();
      }

      // Check interactable
      if (ControllerManager.GetKey(ControllerManager.Key.F))
      {
        if (_currentInteractable != null)
          _currentInteractable.Interact(this, CustomObstacle.InteractSide.DEFAULT);
        else if (FlipTable(_ragdoll._Hip.position, transform.forward))
        {
#if !DISABLESTEAMWORKS
          SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.TABLE_FLIP);
#endif
        }
      }
      // Check reload
      if (ControllerManager.GetKey(ControllerManager.Key.R, _Profile._reloadSidesSameTime ? ControllerManager.InputMode.HOLD : ControllerManager.InputMode.DOWN))
        Reload();
    }

    // Controller
    else
    {
      var gamepadId = _Id - (GameScript.s_CustomNetworkManager._Connected ? GameScript.s_CustomNetworkManager._Self._NetworkBehavior._PlayerId : 0);

      // Check sticks
      Vector2 leftStick = new Vector2(ControllerManager.GetControllerAxis(gamepadId, ControllerManager.Axis.LSTICK_X),
          ControllerManager.GetControllerAxis(gamepadId, ControllerManager.Axis.LSTICK_Y)),
        rightStick = new Vector2(ControllerManager.GetControllerAxis(gamepadId, ControllerManager.Axis.RSTICK_X),
          ControllerManager.GetControllerAxis(gamepadId, ControllerManager.Axis.RSTICK_Y));

      //
      if (_HasTwin)
      {
        if (_isLeftTwin)
          rightStick = leftStick;
        if (_isRightTwin)
          leftStick = rightStick;
      }

      //
      float min = 0.125f, max = 0.85f;

      // Left
      if (Mathf.Abs(leftStick.x) <= min)
        leftStick.x = 0f;
      else if (Mathf.Abs(leftStick.x) >= max)
        leftStick.x = 1f * Mathf.Sign(leftStick.x);

      if (Mathf.Abs(leftStick.y) <= min)
        leftStick.y = 0f;
      else if (Mathf.Abs(leftStick.y) >= max)
        leftStick.y = 1f * Mathf.Sign(leftStick.y);

      // Right
      if (Mathf.Abs(rightStick.x) <= min)
        rightStick.x = 0f;
      else if (Mathf.Abs(rightStick.x) >= max)
        rightStick.x = 1f * Mathf.Sign(rightStick.x);

      if (Mathf.Abs(rightStick.y) <= min)
        rightStick.y = 0f;
      else if (Mathf.Abs(rightStick.y) >= max)
        rightStick.y = 1f * Mathf.Sign(rightStick.y);

      if (leftStick != Vector2.zero)
      {
        x = leftStick.x;
        y = leftStick.y;
      }
      if (rightStick != Vector2.zero && rightStick.magnitude > 0.5f)
      {
        x2 = rightStick.x;
        y2 = rightStick.y;
        _cX = Mathf.Clamp(_cX + rightStick.x / 2f, -1f, 1f);
        _cY = Mathf.Clamp(_cY + rightStick.y / 2f, -1f, 1f);
      }

      // Move arms
      var gamepad = ControllerManager.GetPlayerGamepad(gamepadId);
      if (gamepad != null)
      {
        if (!_ragdoll._IsDead && gamepad.buttonSouth.isPressed)
          ExtendArms();
        else
          _ragdoll.ArmsDown();

        // Use items
        if (GameScript.s_GameMode != GameScript.GameModes.VERSUS || (GameScript.s_GameMode == GameScript.GameModes.VERSUS && VersusMode.s_PlayersCanMove))
        {

          Vector2 input = new Vector2(ControllerManager.GetControllerAxis(gamepadId, ControllerManager.Axis.L2),
            ControllerManager.GetControllerAxis(gamepadId, ControllerManager.Axis.R2));

          //
          if (_HasTwin)
          {
            if (_isLeftTwin)
              input.y = 0f;
            if (_isRightTwin)
              input.x = 0f;
          }

          //
          float bias = 0.4f;
          min = 0f + bias;

          //
          if (input.x >= 1f - bias && _lastInputTriggers.x < 1f - bias)
            if (!_ragdoll._ItemL && !_ragdoll._ItemR)
            {
              SwapLoadouts();
            }
            else if (!_ragdoll._ItemL)
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
            if (!_ragdoll._ItemL)
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
            if (!_ragdoll._ItemL && !_ragdoll._ItemR)
            {
              SwapLoadouts();
            }
            else if (!_ragdoll._ItemR)
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
            if (!_ragdoll._ItemR)
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
        }

        // Save dpad press times
        if (gamepad.dpad.up.wasPressedThisFrame)
          _dpadPressed[0] = Time.unscaledTime;
        else if (gamepad.dpad.down.wasPressedThisFrame)
          _dpadPressed[1] = Time.unscaledTime;

        if (gamepad.dpad.left.wasPressedThisFrame)
          _dpadPressed[2] = Time.unscaledTime;
        else if (gamepad.dpad.right.wasPressedThisFrame)
          _dpadPressed[3] = Time.unscaledTime;

        // Whistle / drop money
        if (gamepad.dpad.down.wasReleasedThisFrame)
          Taunt(1);

        // Swap weapon hands
        else if (gamepad.dpad.up.wasReleasedThisFrame)
          Taunt(0);

        // Buy specific weapon
        var dpadHoldTime = 0.6f;
        if (gamepad.dpad.left.wasReleasedThisFrame)
        {
          Taunt(2);
          _dpadPressed[2] = 0f;
        }
        else if (gamepad.dpad.right.wasReleasedThisFrame)
        {
          Taunt(3);
          _dpadPressed[3] = 0f;
        }

        // Change levels
        else
        {
          if (_dpadPressed[2] != 0f && Time.unscaledTime - _dpadPressed[2] >= dpadHoldTime)
          {
            GameScript.PreviousLevelSafe();
            _dpadPressed[2] = 0f;
          }
          else if (_dpadPressed[3] != 0f && Time.unscaledTime - _dpadPressed[3] >= dpadHoldTime)
          {
            GameScript.NextLevelSafe();
            _dpadPressed[3] = 0f;
          }
        }

        // Check runkey
        /*if (_Profile._holdRun)
          runKeyDown = gamepad.leftStickButton.wasPressedThisFrame;
        else */
        runKeyDown = gamepad.leftStickButton.isPressed;

        // Check grapple
        if (GameScript.s_GameMode != GameScript.GameModes.VERSUS || (GameScript.s_GameMode == GameScript.GameModes.VERSUS && VersusMode.s_PlayersCanMove))
        {
          if (
            !_ragdoll._IsDead &&
            _ragdoll._CanGrapple &&
            gamepad.rightStickButton.wasPressedThisFrame
          )
          {
            _ragdoll.Grapple(true);
          }
          if (_ragdoll._grappling)
          {
            if (_isLeftTwin)
              if (_ragdoll._ItemL == null && gamepad.leftTrigger.wasReleasedThisFrame)
                _ragdoll.Grapple(false);

            if (_isRightTwin)
              if (_ragdoll._ItemR == null && gamepad.rightTrigger.wasReleasedThisFrame)
                _ragdoll.Grapple(false);
          }

          // Check utilities
          if (gamepad.leftShoulder.wasPressedThisFrame)
          {
            /*if (_UtilitiesLeft.Count == 0 && _UtilitiesRight.Count > 0)
              {}_UtilitiesRight[0].UseDown();
            else */
            if (_UtilitiesLeft.Count > 0)
              _UtilitiesLeft[0].UseDown();
          }
          else if (gamepad.leftShoulder.wasReleasedThisFrame)
          {
            /*if (_UtilitiesLeft.Count == 0 && _UtilitiesRight.Count > 0)
              _UtilitiesRight[0].UseUp();
            else */
            if (_UtilitiesLeft.Count > 0)
              _UtilitiesLeft[0].UseUp();
          }
          if (gamepad.rightShoulder.wasPressedThisFrame)
          {
            /*if (_UtilitiesRight.Count == 0 && _UtilitiesLeft.Count > 0)
              _UtilitiesLeft[0].UseDown();
            else */
            if (_UtilitiesRight.Count > 0)
              _UtilitiesRight[0].UseDown();
          }
          else if (gamepad.rightShoulder.wasReleasedThisFrame)
          {
            /*if (_UtilitiesRight.Count == 0 && _UtilitiesLeft.Count > 0)
              _UtilitiesLeft[0].UseUp();
            else */
            if (_UtilitiesRight.Count > 0)
              _UtilitiesRight[0].UseUp();
          }
        }

        // Check weapon swap
        if (gamepad.buttonNorth.wasPressedThisFrame)
          SwapLoadouts();

        // Check interactable
        if (gamepad.buttonEast.wasPressedThisFrame)
        {
          if (_currentInteractable != null)
            _currentInteractable.Interact(this, CustomObstacle.InteractSide.DEFAULT);
          else if (FlipTable(_ragdoll._Hip.position, transform.forward))
          {
#if !DISABLESTEAMWORKS
            SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.TABLE_FLIP);
#endif
          }
        }

        // Check reload
        if (_Profile._reloadSidesSameTime ? gamepad.buttonWest.isPressed : gamepad.buttonWest.wasPressedThisFrame)
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
      /*/ Check run option; option is not toggle run
      if (_Profile._holdRun)
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
      if (_runToggle || _ragdoll._forceRun)*/
      movespeed *= RUNSPEED *
        // Speed perk
        (HasPerk(Shop.Perk.PerkType.SPEED_UP) ? 1.15f : 1f);
    }

    // Move player
    if (!_ragdoll._grappled)
    {
      _saveInput = xy;
      if (GameScript.s_GameMode != GameScript.GameModes.VERSUS || (GameScript.s_GameMode == GameScript.GameModes.VERSUS && VersusMode.s_PlayersCanMove))
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
    if (_ragdoll._grappling) { moveSpeed *= 0.9f; }

    // Move player
    if (_ragdoll.Active() && !_ragdoll._IsStunned && _agent != null)
    {
      /*if (Time.timeScale < 1f)
      {
        moveSpeed *= Mathf.Clamp(Time.timeScale * 1.3f, 0f, 1f);
        Debug.Log($"{Time.timeScale} ... {moveSpeed}");
      }*/
      var dis2 = (input.x * moveSpeed * Vector3.right) + (input.y * moveSpeed * Vector3.forward);
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

      //
      if (_swordRunLerper > 0.9f)
        if (dis2.magnitude < 0.5f)
          _swordRunLerper = 0.7f;
    }
  }

  public void ResetLoadout()
  {
    if (_ragdoll._IsDead) return;
    if (!_IsOriginal)
    {
      if (_HasTwin)
      {
        _connectedTwin._IsOriginalTwin = false;
        _connectedTwin._connectedTwin = null;

        _ragdoll.TakeDamage(
          new ActiveRagdoll.RagdollDamageSource()
          {
            Source = _ragdoll,

            HitForce = Vector3.zero,

            Damage = 100,
            DamageSource = _ragdoll._Hip.position,
            DamageSourceType = ActiveRagdoll.DamageSourceType.MELEE,

            SpawnBlood = false,
            SpawnGiblets = false
          });
      }
      return;
    }
    if (_HasTwin) return;

    // Check changed loadout profile
    if (_Profile._LoadoutIndex != _saveLoadoutIndex)
    {
      if (MathC.Get2DDistance(transform.position, _playerSpawn.transform.position) > 1.2f)
        _Profile._LoadoutIndex = _saveLoadoutIndex;
      else
      {
        EquipLoadout(_Profile._LoadoutIndex);
        if (EnemyScript._Enemies_dead?.Count > 0)
          _EquipmentChanged = true;
      }
    }
  }

  public void EquipLoadout(int loadoutIndex)
  {
    _Profile._LoadoutIndex = loadoutIndex;

    _Profile._EquipmentIndex = 0;
    if (_ragdoll.CanSwapWeapons())
    {

      CheckSpawnTwin();

      var leftItem = _Profile._ItemLeft;
      var rightItem = _Profile._ItemRight;

      _ragdoll.SwapItems(
        new ActiveRagdoll.WeaponSwapData()
        {
          ItemType = leftItem,
          ItemId = -1,
          ItemClip = -1,
          ItemUseItem = -1f
        },
        new ActiveRagdoll.WeaponSwapData()
        {
          ItemType = rightItem,
          ItemId = -1,
          ItemClip = -1,
          ItemUseItem = -1f
        },
        _Profile._EquipmentIndex
      );

      // Despawn utilities
      if (_UtilitiesLeft != null)
        for (int i = _UtilitiesLeft.Count - 1; i >= 0; i--)
          if (_UtilitiesLeft[i] != null)
            GameObject.Destroy(_UtilitiesLeft[i].gameObject);
      if (_UtilitiesRight != null)
        for (int i = _UtilitiesRight.Count - 1; i >= 0; i--)
          if (_UtilitiesRight[i] != null)
            GameObject.Destroy(_UtilitiesRight[i].gameObject);

      RegisterUtilities();
      _Profile.UpdateIcons();
      _saveLoadoutIndex = _Profile._LoadoutIndex;
    }
  }

  void RotatePlayer(Vector2 input, Vector2 input2)
  {
    if (_isauto) return;
    if (Mathf.Abs(input.x) > 0.1f || Mathf.Abs(input.y) > 0.1f)
      transform.LookAt(transform.position + (new Vector3(GameResources._Camera_Main.transform.right.x, 0f, GameResources._Camera_Main.transform.right.z).normalized * input.x + new Vector3(GameResources._Camera_Main.transform.forward.x, 0f, GameResources._Camera_Main.transform.forward.z).normalized * input.y).normalized * 5f);
    else if (_Profile._faceMovement)
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
  int[] _item_info;
  public void SwapLoadouts()
  {
    if (!_ragdoll.CanSwapWeapons()) return;
    if (!_Profile._Loadout._two_weapon_pairs && !HasPerk(Shop.Perk.PerkType.MARTIAL_ARTIST)) return;

    var clip_l = -1;
    var clip_r = -1;

    var useTime_l = -1f;
    var useTime_r = -1f;

    // Save equipment info; save clip and use_time
    if (_loadout_info == null)
      _loadout_info = new float[4];
    else
    {
      clip_l = (int)_loadout_info[0];
      clip_r = (int)_loadout_info[1];

      useTime_l = _loadout_info[2];
      useTime_r = _loadout_info[3];
    }

    // Save item metadata; item id
    var itemId_l = -1;
    var itemId_r = -1;
    if (_item_info == null)
    {
      _item_info = new int[] { -1, -1 };
    }
    else
    {
      itemId_l = _item_info[0];
      itemId_r = _item_info[1];
    }

    // Save other item data for later
    _item_info[0] = _ragdoll._ItemL?._ItemId ?? -1;
    _item_info[1] = _ragdoll._ItemR?._ItemId ?? -1;

    _loadout_info[0] = _ragdoll._ItemL?.Clip() ?? -1;
    _loadout_info[1] = _ragdoll._ItemR?.Clip() ?? -1;

    _loadout_info[2] = _ragdoll._ItemL?._useTime ?? -1f;
    _loadout_info[3] = _ragdoll._ItemR?._useTime ?? -1f;

    _Profile._EquipmentIndex++;
    _ragdoll.SwapItems(
      new ActiveRagdoll.WeaponSwapData()
      {
        ItemType = _Profile._ItemLeft,
        ItemId = itemId_l,
        ItemClip = clip_l,
        ItemUseItem = useTime_l
      },
      new ActiveRagdoll.WeaponSwapData()
      {
        ItemType = _Profile._ItemRight,
        ItemId = itemId_r,
        ItemClip = clip_r,
        ItemUseItem = useTime_r
      },
      _Profile._EquipmentIndex
    );
    _Profile.UpdateIcons();
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
    if (GameScript.IsSurvival() && !_ragdoll._IsDead)
    {

      return;
    }
    // If dead, check if all other players dead
    if (_ragdoll._IsDead)
    {
      foreach (var p in s_Players)
        if (!p._ragdoll._IsDead)
        {
          reset = false;
          break;
        }
    }
    if (reset && !GameScript.s_Singleton._GameEnded)
    {
      if (GameScript.ReloadMap(true, true))
      {
        if (!GameScript.TutorialInformation._HasRestarted)
          GameScript.TutorialInformation._HasRestarted = true;
      }
    }
  }

  // 'Spread' arms for 90 degree angle
  void ExtendArms()
  {
    bool extendleft = true, extendright = true;
    if (_ragdoll._ItemL && _ragdoll._ItemL.IsMelee())
      extendleft = false;
    if (_ragdoll._ItemR && _ragdoll._ItemR.IsMelee())
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
    if (Menu.s_InMenus) return;

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

        if (!_HasTwin)
        {
          _ragdoll.SwapItemHands(_Profile._EquipmentIndex);
          _Profile.UpdateIcons();
        }
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
              if (GameScript.SurvivalMode.HasPoints(_Id, points))
                break;
            }
            if (points == 0) return;

            GameScript.SurvivalMode.SpendPoints(_Id, points);
            var money = GameObject.Instantiate(GameResources._Money) as GameObject;
            GameScript.SurvivalMode.AddMoney(money);
            money.name = $"Credits {points}";
            money.transform.parent = GameScript.s_Singleton.transform;
            money.transform.position = _ragdoll._Hip.position + _ragdoll._Hip.transform.forward * 0.2f;
            var collider = money.GetComponent<Collider>();
            var collider0 = _ragdoll._Hip.GetComponents<Collider>()[1];
            //_ragdoll.IgnoreCollision(collider);
            Physics.IgnoreCollision(collider, collider0);
            collider.enabled = true;
            var rb = money.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.AddForce(_ragdoll._Hip.transform.forward * 100f);
            _ragdoll.PlaySound("Ragdoll/Throw");

            IEnumerator sizeChange()
            {
              yield return new WaitForSeconds(0.2f);
              if (collider != null && collider0 != null)
                _ragdoll.PlaySound("Survival/Points_Land_Floor");

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
          _ragdoll.PlaySound("Ragdoll/Whistle", 0.85f, 1.2f);

          EnemyScript.CheckSound(_ragdoll._Hip.position + _ragdoll._Hip.transform.forward * 3f, _ragdoll._Hip.position, EnemyScript.Loudness.SOFT);
          _ragdoll.DisplayText("!");

          _LastWhistle = Time.time;
        }

        /*if (_tauntIter >= 0)
        {
          _tauntIter = -1;
          break;
        }*/
        break;

      // Left
      case (2):

        if (GameScript.s_GameMode == GameScript.GameModes.CLASSIC)
        {
          if (_IsOriginal)
            _Profile._LoadoutIndex--;
          break;
        }

        // Check interactables
        if (_currentInteractable != null)
          _currentInteractable.Interact(this, CustomObstacle.InteractSide.LEFT);
        break;

      // Right
      case (3):

        if (GameScript.s_GameMode == GameScript.GameModes.CLASSIC)
        {
          if (_IsOriginal)
            _Profile._LoadoutIndex++;
          break;
        }

        // Check interactables
        if (_currentInteractable != null)
          _currentInteractable.Interact(this, CustomObstacle.InteractSide.RIGHT);
        break;
    }
  }

  //
  public static PlayerScript GetClosestPlayerTo(Vector2 position)
  {

    if (s_Players.Count == 0)
      return null;
    if (s_Players.Count == 1 && s_Players[0]._ragdoll != null && !s_Players[0]._ragdoll._IsDead)
      return s_Players[0];

    var distance = 10000f;
    PlayerScript closest_player = null;
    foreach (var player in s_Players)
    {
      if (player?._ragdoll._IsDead ?? true) continue;
      var distance0 = Vector2.Distance(position, new Vector2(player._ragdoll._Controller.position.x, player._ragdoll._Controller.position.z));
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
    if (s_Players != null)
      foreach (PlayerScript p in s_Players)
        if (p._HasExit) return true;
    return false;
  }

  public void OnDamageTaken(ActiveRagdoll.RagdollDamageSource ragdollDamageSource)
  {
    _Profile.UpdateHealthUI();

    //
    if (_HasTwin && _connectedTwin._HasTwin && !(_connectedTwin._ragdoll?._IsDead ?? true))
      _connectedTwin._Ragdoll?.TakeDamage(ragdollDamageSource);

    // Controller rumble
    if (!mouseEnabled && SettingsModule.ControllerRumble)
    {
      var gamepad = ControllerManager.GetPlayerGamepad(_Id);
      if (gamepad != null)
      {
        if (_rumbleCoroutine != null)
          StopCoroutine(_rumbleCoroutine);
        _rumbleCoroutine = StartCoroutine(rumbleCo(gamepad, 0.25f, 0.2f, 0.2f));
      }
    }
  }

  Coroutine _rumbleCoroutine;
  IEnumerator rumbleCo(Gamepad gamepad, float time, float intensityLeft, float intensityRight)
  {
    gamepad.SetMotorSpeeds(intensityLeft, intensityRight);
    yield return new WaitForSecondsRealtime(time);
    gamepad.SetMotorSpeeds(0f, 0f);
  }

  public void OnToggle(ActiveRagdoll source, ActiveRagdoll.DamageSourceType damageSourceType)
  {
    // Record stat
    Stats.RecordDeath(_Id);

    // Controller rumble
    if (!mouseEnabled && SettingsModule.ControllerRumble)
    {
      var gamepad = ControllerManager.GetPlayerGamepad(_Id);
      if (gamepad != null)
      {
        if (_rumbleCoroutine != null)
          StopCoroutine(_rumbleCoroutine);
        _rumbleCoroutine = StartCoroutine(rumbleCo(gamepad, 0.18f, 0.5f, 0.5f));
      }
    }

    // Send dead to other clients
    //MultiplayerManager.SendMessage("dead " + string.Format("{0} {1} {2} {3}", _id, _ragdoll._hip.velocity.x, _ragdoll._hip.velocity.y, _ragdoll._hip.velocity.z));

#if UNITY_STANDALONE
    // Check achievements
    if (source != null)
    {
      SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.DIE);
      if (source._IsPlayer && source._PlayerScript != null && source._PlayerScript._Id != _Id)
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.TEAM_KILL);
      if (damageSourceType == ActiveRagdoll.DamageSourceType.EXPLOSION)
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.EXPLODE);
    }
#endif

    // Survival
    if (GameScript.s_GameMode == GameScript.GameModes.SURVIVAL)
      GameScript.SurvivalMode.OnPlayerDead(_Id);

    // Versus
    if (GameScript.s_GameMode == GameScript.GameModes.VERSUS)
    {
      VersusMode.OnPlayerDeath(this, source._PlayerScript);
    }

    // Remove ring
    IEnumerator fadeRing()
    {
      var baseColor = _ring[0].sharedMaterial.color;
      var emissionColor = _ring[0].sharedMaterial.GetColor("_EmissionColor");
      float t = 1f, startA = baseColor.a;
      while (t >= 0f)
      {
        t -= 0.07f;
        baseColor.a = Mathf.Clamp(startA * t, 0f, 1f);
        emissionColor = Color.Lerp(Color.black, emissionColor, t);
        _ring[0].sharedMaterial.color = baseColor;
        _ring[0].sharedMaterial.SetColor("_EmissionColor", emissionColor);
        yield return new WaitForSeconds(0.05f);
        if (this == null || _ragdoll == null || _ragdoll._Hip == null) break;
      }
      if (_ring != null) _ring[0].transform.parent.gameObject.SetActive(false);
    }
    IEnumerator fadeRing2()
    {
      float t = 1f;
      var ring = _ring[0].transform.parent;
      var startScale = ring.transform.localScale;
      while (t >= 0f)
      {
        t -= 0.02f;
        ring.localScale = Vector3.Lerp(Vector3.one * 0.01f, startScale, t);
        yield return new WaitForSeconds(0.05f);
        if (this == null || _ragdoll == null || _ragdoll._Hip == null) break;
      }
      if (_ring != null) _ring[0].transform.parent.gameObject.SetActive(false);
    }
    if (_IsOriginal)
      GameScript.s_Singleton.StartCoroutine(fadeRing());

    // Switching loadouts with twin
    else if (_HasTwin && !_connectedTwin._HasTwin)
      GameScript.s_Singleton.StartCoroutine(fadeRing2());

    // Slow motion on player death
    var lastplayer = true;
    foreach (var p0 in s_Players)
      if (p0._Id != _Id && !p0._ragdoll._IsDead)
      {
        lastplayer = false;
        break;
      }
    if (!_IsOriginal) lastplayer = false;
    if (Settings._Slowmo_on_death && lastplayer && !HasPerk(Shop.Perk.PerkType.NO_SLOWMO)) _SlowmoTimer += 2f;

    // Check for restart tutorial
    if (lastplayer && GameScript.s_GameMode != GameScript.GameModes.VERSUS)
    {
      _All_Dead = true;

      // Save setting before changing
      var saveinfo = GameScript.TutorialInformation._HasRestarted;

      // Embarass player ultimately leading to the return of the title
      if (SettingsModule.ShowDeathText)
        TileManager.ShowGameOverText("NOT SNEAKY.", "white", "red");

      // Coroutine to show controls
      IEnumerator FlashRestart()
      {
        var offset = GameResources._Camera_Main.transform.up * 4f;
        float pretimer = 0f, timer = 0f, supertimer = 0f, startpos = 5f;
        var flicker = false;

        var tutorial_keyboard = ControllerManager._NumberGamepads == 0;
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
      GameScript.s_Singleton.StartCoroutine(FlashRestart());
    }
    // Play death sound
    //_ragdoll._audioPlayer.volume = 1f;

    // If has the exit and dies, re-drop the exit so someone else can pick it up
    if (!_HasExit || GameScript.s_Singleton._GameEnded) return;
    GameScript.ToggleExit(false);
    _HasExit = false;
    GameScript.s_InLevelEndPlayer = null;
    var p = FunctionsC.SpawnPowerup(Powerup.PowerupType.END);
    p.transform.position = transform.position;
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
    if (_ragdoll == null || _ragdoll._IsDead) return;
    switch (other.name)
    {
      case "SHURIKEN":
        other.GetComponent<UtilityScript>().PickUp(this);
        break;

      case "SHURIKEN_BIG":
        other.transform.parent.GetComponent<UtilityScript>().PickUp(this);
        break;

      default:
        // Pick up points
        if (other.name.StartsWith("Credits "))
        {
          var amount = other.name.Split(' ')[1].ParseIntInvariant();
          GameScript.SurvivalMode.GivePoints(_Id, amount);
          GameObject.Destroy(other.gameObject);
          if (Time.time - _LastMoneyPickupNoiseTime > 0.1f)
          {
            _LastMoneyPickupNoiseTime = Time.time;
            _ragdoll.PlaySound("Survival/Pickup_Points");
          }
          break;
        }
        break;
    }
  }
  public void OnTriggerStay(Collider other)
  {
    /*
    if (!_ragdoll._dead && other.name.Equals("PlayerSpawn") && _HasExit)
      GameScript._inLevelEnd = true;*/

    // Check interactable
    switch (other.name)
    {
      case "Interactable":
        if (_currentInteractable != null) break;
        _currentInteractable = other.gameObject.GetComponent<CustomObstacle>();
        break;
    }
  }
  public void OnTriggerExit(Collider other)
  {
    /*/ Stop being in end of level
    if (!_ragdoll._dead && other.name.Equals("PlayerSpawn") && _HasExit)
      GameScript._inLevelEnd = false;*/

    // Check interactable
    if (other.gameObject.GetInstanceID() == _currentInteractable?.gameObject.GetInstanceID())
    {
      _currentInteractable = null;
    }
  }

  // Destroy dependancies
  private void OnDestroy()
  {
    // Despawn utilities
    if (_UtilitiesLeft != null)
      for (var i = _UtilitiesLeft.Count - 1; i >= 0; i--)
        if (_UtilitiesLeft[i] != null)
          GameObject.Destroy(_UtilitiesLeft[i].gameObject);
    if (_UtilitiesRight != null)
      for (var i = _UtilitiesRight.Count - 1; i >= 0; i--)
        if (_UtilitiesRight[i] != null)
          GameObject.Destroy(_UtilitiesRight[i].gameObject);

    // Despawn ring
    if (_ring == null || _ring[0] == null || _ring[0].transform.parent == null) return;
    GameObject.Destroy(_ring[0].transform.parent.gameObject);
    _ring = null;
  }

  public System.Tuple<bool, ActiveRagdoll.Side> HasUtility(UtilityScript.UtilityType utility)
  {
    if (_Equipment._UtilitiesLeft != null && _Equipment._UtilitiesLeft.Length > 0 && _Equipment._UtilitiesLeft[0] == utility)
      return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT);
    if (_Equipment._UtilitiesRight != null && _Equipment._UtilitiesRight.Length > 0 && _Equipment._UtilitiesRight[0] == utility)
      return System.Tuple.Create(true, ActiveRagdoll.Side.RIGHT);
    return System.Tuple.Create(false, ActiveRagdoll.Side.LEFT);
  }
  public System.Tuple<bool, ActiveRagdoll.Side> HasUtility(UtilityScript.UtilityType utility, ActiveRagdoll.Side side)
  {
    if (_Equipment._UtilitiesLeft != null && _Equipment._UtilitiesLeft.Length > 0 && _Equipment._UtilitiesLeft[0] == utility && side == ActiveRagdoll.Side.LEFT)
      return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT);
    if (_Equipment._UtilitiesRight != null && _Equipment._UtilitiesRight.Length > 0 && _Equipment._UtilitiesRight[0] == utility && side == ActiveRagdoll.Side.RIGHT)
      return System.Tuple.Create(true, ActiveRagdoll.Side.RIGHT);
    return System.Tuple.Create(false, side);
  }

  public static bool FlipTable(Vector3 effectorPos, Vector3 forwardDir, float checkDistance = 0.6f)
  {
    if (!GameScript.s_InteractableObjects) return false;

    // Raycast table
    var raycastinfo = new RaycastHit();
    if (Physics.SphereCast(new Ray(effectorPos + -forwardDir * 0.2f + -Vector3.up * 0.3f, forwardDir), 0.15f, out raycastinfo, checkDistance, LayerMask.GetMask("ParticleCollision")))
    {
      if (raycastinfo.collider.name == "Table")
      {
        var table = raycastinfo.collider.gameObject;

        var startRotation = table.transform.localRotation;
        var startPosition = table.transform.position;

        var distanceToTable = effectorPos - table.transform.position;
        distanceToTable.y = 0f;
        distanceToTable = distanceToTable.normalized;
        distanceToTable = Quaternion.Euler(0f, -table.transform.localEulerAngles.y, 0f) * distanceToTable;

        var applyRotation = Vector3.zero;
        var applyPosition = Vector3.zero;
        if (distanceToTable.x < -0.5f)
        {
          applyRotation = new Vector3(0f, 0f, 1f) * -90f;
          applyPosition = table.transform.right;
        }
        else if (distanceToTable.x > 0.5f)
        {
          applyRotation = new Vector3(0f, 0f, 1f) * 90f;
          applyPosition = -table.transform.right;
        }
        else if (distanceToTable.z > 0.5f)
        {
          applyRotation = new Vector3(1f, 0f, 0f) * -90f;
          applyPosition = -table.transform.forward * 2f;
        }
        else
        {
          applyRotation = new Vector3(1f, 0f, 0f) * 90f;
          applyPosition = table.transform.forward * 2f;
        }

        var applyPositionRotated = Quaternion.Euler(0f, 90f, 0f) * applyPosition.normalized;
        var rotateLongways = applyRotation.x != 0f;

        // Check can flip
        var canFlip = true;

        var tableOrigin = table.transform.position;
        var tableScale = new Vector3(1f, 0f, 2f);

        bool PositionInBounds(Vector3 position)
        {
          var hitOriginDiff = tableOrigin - position;
          var hitOriginRotated = tableOrigin + Quaternion.Euler(0f, -table.transform.eulerAngles.y, 0f) * hitOriginDiff;
          if (
            hitOriginRotated.x < tableOrigin.x - tableScale.x * 0.5f ||
            hitOriginRotated.x > tableOrigin.x + tableScale.x * 0.5f ||
            hitOriginRotated.z < tableOrigin.z - tableScale.z * 0.5f ||
            hitOriginRotated.z > tableOrigin.z + tableScale.z * 0.5f
          )
            return false;
          return true;
        }

        // Check candles
        foreach (var candle in CandleScript.s_Candles)
        {
          if (PositionInBounds(candle.transform.position) && candle.gameObject.name != "CandelBig")
          {
            candle.Flip();
            break;
          }
        }

        // Raycast to get stuff on top of table
        var hitsTableTop = Physics.SphereCastAll(new Ray(table.transform.position, Vector3.up), 1f, 2f, LayerMask.GetMask("ParticleCollision"));
        var books = new List<Collider>();
        var tvs = new List<TVScript>();
        foreach (var hit in hitsTableTop)
        {

          // Check collider is actually on top
          if (!PositionInBounds(hit.transform.position))
            continue;

          //
          switch (hit.collider.name)
          {
            case "Television":
              tvs.Add(hit.collider.GetComponent<TVScript>());
              break;
            case "Books":
              books.Add(hit.collider);
              break;
          }

          if (!canFlip)
            break;
        }

        if (!canFlip)
        {
          //_ragdoll.DisplayText("it's too heavy..");
        }
        else
        {

          // Check space available in front of table
          var hitsTableFront = Physics.SphereCastAll(new Ray(table.transform.position + -applyPositionRotated * (rotateLongways ? 0.5f : 1f) + applyPosition, applyPositionRotated), 0.2f, rotateLongways ? 1f : 2f, LayerMask.GetMask("ParticleCollision"));
          var spaceInFront = hitsTableFront.Length == 0;
          var spaceInFrontDis = 10f;
          if (!spaceInFront)
          {
            foreach (var hit in hitsTableFront)
            {

              switch (hit.collider.gameObject.name)
              {
                case "Books":
                case "Television":
                  continue;
              }
              var dis = MathC.Get2DDistance(hit.point == Vector3.zero ? hit.transform.position : hit.point, table.transform.position);
              if (dis < spaceInFrontDis)
                spaceInFrontDis = dis;

            }
            spaceInFrontDis *= 0.5f;
          }

          var endRotation = startRotation * Quaternion.Euler(applyRotation);
          var endPosition = startPosition + applyPosition.normalized * (spaceInFront ? applyPosition.magnitude : Mathf.Clamp(spaceInFrontDis, 0f, applyPosition.magnitude));

          //
          table.name = "Table_Flipped";
          SfxManager.PlayAudioSourceSimple(effectorPos, "Etc/Table_flip");

          IEnumerator FlipTable()
          {

            var navmeshobs = table.GetComponent<NavMeshObstacle>();
            navmeshobs.carveOnlyStationary = false;

            // Explode books
            foreach (var book in books)
              FunctionsC.BookManager.ExplodeBooks(book, effectorPos);

            // TVs
            foreach (var tv in tvs)
              tv?.Flip();

            var t = 0f;
            var lastTime = Time.time;
            while (t < 1f)
            {
              t += (Time.time - lastTime) * 3.5f;
              lastTime = Time.time;

              if (table != null)
              {
                table.transform.localRotation = Quaternion.Lerp(startRotation, endRotation, t);
                table.transform.position = Vector3.Lerp(startPosition, endPosition, t);
              }

              yield return new WaitForSecondsRealtime(0.01f);

            }

            if (table != null)
            {
              table.transform.localRotation = endRotation;
              table.transform.position = endPosition;

              navmeshobs.carveOnlyStationary = true;

              var parts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.TABLE_FLIP)[0];
              var partsPos = table.transform.position;
              partsPos.y = -1.1f;
              parts.transform.position = partsPos;
              var angles = parts.transform.localEulerAngles;
              angles.y = table.transform.localEulerAngles.y;
              parts.transform.localEulerAngles = angles;

              var partsShapeModule = parts.shape;
              partsShapeModule.scale = new Vector3(0.37f, rotateLongways ? 0.37f : 1f, 1f);

              parts.Play();

              EnemyScript.CheckSound(table.transform.position, EnemyScript.Loudness.NORMAL);
            }
          }
          GameScript.s_Singleton.StartCoroutine(FlipTable());
        }
      }
      return true;

    }

    return false;
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
      _TimeScalesList.Add(Time.time - GameScript.s_LevelStartTime);
    }

    public static bool _Playing;
    static int _PlaybackIndex;
    public static void Playback()
    {
      /*_Playing = !_Playing;
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

      Debug.LogError("Started playback");*/
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

  //
  public static int GetNumberAlivePlayers()
  {
    var numAlive = 0;
    foreach (var player in s_Players)
      if (player._ragdoll._health > 0)
        numAlive++;

    return numAlive;
  }
}