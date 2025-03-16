using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

using System.Linq;

public class EnemyScript : MonoBehaviour, PlayerScript.IHasRagdoll
{
  //
  static Settings.SettingsSaveData SettingsModule { get { return Settings.s_SaveData.Settings; } }
  static Settings.LevelSaveData LevelModule { get { return Settings.s_SaveData.LevelData; } }

  //
  static class SpherecastHandler
  {

    static NativeArray<RaycastHit> _Results;
    static NativeArray<SpherecastCommand> _Commands;
    static int[] _EnemyOrder;
    public static int _EnemyOrderIter;
    public static readonly int _NumSpherecasts = 4;
    static int _Commands_Iter;

    public static void Init(int count)
    {
      // Create arrays to hold job data
      _Results = new NativeArray<RaycastHit>(count, Allocator.TempJob);
      _Commands = new NativeArray<SpherecastCommand>(count, Allocator.TempJob);
      _EnemyOrder = new int[count];
      _EnemyOrderIter = 0;
      _Commands_Iter = 0;
    }

    public static void Clean()
    {
      // Check if should dispose data from prior casts
      _Results.Dispose();
      _Commands.Dispose();
    }

    public static void QueueSpherecast(Vector3 origin, Vector3 direction, float radius)
    {
      _Commands[_Commands_Iter++] = new SpherecastCommand(origin, radius, direction, 100f, GameResources._Layermask_Ragdoll);
    }

    public static void ScheduleAllSpherecasts()
    {
      // Schedule the batch of raycasts
      JobHandle handle = SpherecastCommand.ScheduleBatch(_Commands, _Results, 1, default(JobHandle));

      // Wait for the batch processing job to complete
      handle.Complete();

      // Reset enemyorderiter so jobs can be retrieved in order
      _EnemyOrderIter = 0;
    }

    public static void Register(EnemyScript e)
    {
      _EnemyOrder[_EnemyOrderIter++] = e._Id;
    }

    public static RaycastHit GetSpherecastHit(int iter)
    {
      var index = _EnemyOrderIter * _NumSpherecasts + iter;
      if (index < 0) return new RaycastHit();
      if (index < _Results.Length)
        return _Results[index];
      return new RaycastHit();
    }
  }

  void ScheduleSpherecasts()
  {
    if (_IsZombieReal) return;
    if (_ragdoll._IsDead) throw new System.Exception("Ragdoll dead in ScehduleSpherecast");

    // Register to handler to index this._id
    SpherecastHandler.Register(this);

    var origin = _ragdoll._head.transform.position;// + new Vector3(0f, -0.3f, 0f);
    var radius = 0.2f;

    // 0; Cast at ragdolltarget
    var target = _ragdollTarget != null ? _ragdollTarget._Hip.transform.position : transform.forward;
    var dir = -MathC.Get2DVector(_ragdoll._Hip.transform.position - target).normalized;
    SpherecastHandler.QueueSpherecast(origin, dir, radius);

    // Cast two directions
    var forward = MathC.Get2DVector(_ragdoll._head.transform.forward).normalized;
    var right = MathC.Get2DVector(_ragdoll._head.transform.right).normalized;

    // 1; left
    dir = (forward + right * (0.025f + Mathf.PingPong(Time.time * 4f, 0.9f))).normalized;
    radius = 0.25f;
    SpherecastHandler.QueueSpherecast(origin, dir, radius);

    // 2; right
    dir = (forward - right * (1f - (0.025f + Mathf.PingPong(Time.time * 4f, 0.9f)))).normalized;
    SpherecastHandler.QueueSpherecast(origin, dir, radius);

    // 3; Cast towards lastknownpos
    dir = -MathC.Get2DVector(_ragdoll._head.transform.position - _lastKnownPos).normalized + right * (0.1f + (Mathf.PingPong(Time.time * 1.5f + _Id, 2f) - 1f));
    SpherecastHandler.QueueSpherecast(origin, dir, radius);

    // Disable self casting collide
    _ragdoll.ToggleRaycasting(false);
  }

  static public List<EnemyScript> _Enemies_alive, _Enemies_dead;
  public static int _ID;
  public static int _MAX_RAGDOLLS_DEAD = 15, _MAX_RAGDOLLS_ALIVE = 40;

  public int _Id;

  ActiveRagdoll _ragdoll;
  public ActiveRagdoll _Ragdoll { get { return _ragdoll; } set { _ragdoll = value; } }

  public UnityEngine.AI.NavMeshAgent _agent;

  float _lastPosSetTime;

  public PathScript _path;

  bool _beganPatrolling;

  public bool _patroling, // Is enemy patrolling
    _targetFound, // Is the player found
    _targetInLOS, // Is the target (player) in LOS
    _playerKnownWeapon, _playerKnownWeaponValue, // Is it known that the player has a weapon?
    _targetDirectlyInFront,
    _targetInFront,
    _canMove,
    _unlimitedAmmo,
    _reactToSound;
  public bool _strafeRight;

  public bool _canAttack;
  public static List<EnemyScript> _Targets;

  public Vector3 _waitLookPos;
  public Vector3 _startPosition;

  public EnemyType _enemyType;

  public enum EnemyType
  {
    NORMAL,
    ROBOT
  }

  int _stuckIter = 0;

  ActiveRagdoll _ragdollTarget;
  Transform _panicTarget; // Used for panicking

  static EnemyScript _Chaser;

  public DoorScript2 _linkedDoor;

  public float _moveSpeed, _moveSpeed_lerped,
   _waitTimer,
   _waitAmount = 4f,
   _lookAroundT,
   _rayTimer = 0f,
    _time_lost, _time_seen,
    _attackTime, _atd,
    _suspiciousTimer,
    _lastSeenTime,
    _lastHeardTimer;

  public Vector3 _lastKnownPos,
    _searchDir;

  public GameScript.ItemManager.Items _itemLeft, _itemRight;

  public enum State
  {
    NEUTRAL,
    PANICKED,
    SEARCHING,
    SUSPICIOUS,
    PURSUIT
  }

  public State _state;

  public bool _IsZombie { get { return _survivalAttributes != null; } }
  public bool _IsZombieReal { get { return _IsZombie && (GameScript.s_GameMode == GameScript.GameModes.SURVIVAL || _isZombieRealOverride); } }
  bool _isZombieRealOverride;

  public class SurvivalAttributes
  {
    public GameScript.SurvivalMode.EnemyType _enemyType;

    public int _WaitForEnemyHordeSize, _WaitForEnemyHordeSizeStart;
  }
  public SurvivalAttributes _survivalAttributes;

  // Use this for initialization
  public void Init(SurvivalAttributes survivalAttributes, bool grappled = false)
  {

    // Set unique ID
    if (_Id != 0) return;
    _Id = _ID++;

    // Add to list of enemies
    _Enemies_alive.Add(this);

    _canAttack = true;

    _startPosition = transform.position;

    lastBulletID = -1;

    _path.Init();
    // Set nearest patrol point
    _path.GetNearestPatrolPoint(transform.position);

    _survivalAttributes = survivalAttributes;

    if (!_IsZombie)
    {
      if (_itemLeft == GameScript.ItemManager.Items.BAT)
        _waitLookPos = PlayerspawnScript._PlayerSpawns[0].transform.position;
      else if (_path.GetPathLength() > 0)
      {
        Transform lp = _path.GetLookPoint(false),
         p = _path.GetPatrolPoint();
        _waitLookPos = MathC.Get2DVector(lp.position) + new Vector3(0f, transform.position.y, 0f);
      }
      //else Debug.LogError("No path");

      transform.LookAt(_waitLookPos);
    }

    // Get NavAgent
    _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    if (_IsZombie && GameScript.IsSurvival())
      _agent.agentTypeID = TileManager._navMeshSurface2.agentTypeID;
    else
      _agent.agentTypeID = TileManager._navMeshSurface.agentTypeID;

    // Setup ragdoll
    var ragdollObj = Instantiate(
      GameResources._Ragdoll,
      transform.position,
      Quaternion.LookRotation(new Vector3(_waitLookPos.x, transform.position.y, _waitLookPos.z) - transform.position) * Quaternion.Euler(0f, 90f, 0f),
      transform.parent
    );
    _ragdoll = new ActiveRagdoll(ragdollObj, transform);
    if (_enemyType == EnemyType.NORMAL)
      switch (_itemLeft)
      {
        case (GameScript.ItemManager.Items.KNIFE):
          if (Random.value < 0.5f)
          {
            _itemLeft = GameScript.ItemManager.Items.NONE;
            _itemRight = GameScript.ItemManager.Items.KNIFE;
          }
          _ragdoll.ChangeColor(Color.green);
          break;
        case (GameScript.ItemManager.Items.PISTOL):
          _ragdoll.ChangeColor(Color.magenta);
          break;
        case (GameScript.ItemManager.Items.PISTOL_SILENCED):
          _ragdoll.ChangeColor(Color.magenta / 3f);
          _itemRight = GameScript.ItemManager.Items.KNIFE;
          break;
        case (GameScript.ItemManager.Items.REVOLVER):
          _ragdoll.ChangeColor((Color.red + Color.yellow) / 3f);
          break;
        case (GameScript.ItemManager.Items.GRENADE_HOLD):
          _ragdoll.ChangeColor(Color.red + Color.yellow);
          break;
        case (GameScript.ItemManager.Items.SHOTGUN_PUMP):
          _ragdoll.ChangeColor(Color.white);
          _enemyType = EnemyType.ROBOT;
          break;
        case (GameScript.ItemManager.Items.BAT):
          //_itemLeft = GameScript.ItemManager.Items.SHOTGUN_DOUBLE;
          _ragdoll.ChangeColor(Color.white);
          _enemyType = EnemyType.ROBOT;
          _canAttack = false;
          _canMove = false;
          _Chaser = this;
          break;
        case (GameScript.ItemManager.Items.GRENADE_LAUNCHER):
          _itemLeft = GameScript.ItemManager.Items.GRENADE_LAUNCHER;
          _ragdoll.ChangeColor(Color.yellow);
          break;
      }

    // Check zombie configs
    if (_IsZombie)
    {

      //
      if (_IsZombieReal)
        _itemLeft = _itemRight = GameScript.ItemManager.Items.AXE;

      // Agent stuff
      _beganPatrolling = true;
      _ragdoll.SetActive(true);
      _patroling = false;

      // Set target to nearest player
      var distanceData = FunctionsC.GetClosestTargetTo(_ragdoll, transform.position);
      if (distanceData == null || distanceData._ragdoll == null) return;
      var closestPlayer = distanceData._ragdoll;
      SetRagdollTarget(closestPlayer);

      SetRandomStrafe();
      if (!grappled)
        _agent.enabled = true;
      _ragdoll._rotSpeed = PlayerScript.ROTATIONSPEED * (0.8f + Random.value * 0.3f);
      TargetFound(false, false);

      // Check armor
      if (_survivalAttributes._enemyType == GameScript.SurvivalMode.EnemyType.ARMORED)
      {
        _ragdoll.AddArmor();
        _ragdoll._health = 3;
      }
    }

    // Check crown
    if (LevelModule.ExtraCrownMode != 0)
      if (GameScript.s_CrownEnemy == _Id)
      {
        _ragdoll.AddCrown();
      }

    // Start cycle
    Walk();
  }

  public static void UpdateEnemies()
  {
    // Check null
    if (_Enemies_alive == null || _Enemies_alive.Count == 0 || GameScript.s_EditorEnabled) return;
    if (Menu.s_InMenus || TileManager._LoadingMap || PlayerScript.s_Players == null || PlayerScript.s_Players.Count == 0) return;
    if (GameScript.s_GameMode != GameScript.GameModes.SURVIVAL)
    {
      // Set up handler
      var count = (_Enemies_alive.Count) * SpherecastHandler._NumSpherecasts;
      SpherecastHandler.Init(count);

      // Queury spherecasts
      foreach (var e in _Enemies_alive)
        e.ScheduleSpherecasts();

      // Complete all spherecasts
      SpherecastHandler.ScheduleAllSpherecasts();

      // Update with spherecast data
      SpherecastHandler._EnemyOrderIter = _Enemies_alive.Count - 1;
      for (var i = _Enemies_alive.Count - 1; i >= 0; i--)
      {
        var e = _Enemies_alive[i];
        e.Handle();
        SpherecastHandler._EnemyOrderIter--;
      }

      // Clean up data
      SpherecastHandler.Clean();
    }
    else
      foreach (var e in _Enemies_alive)
        e.Handle();
  }

  public bool waiting = false;
  bool setstate = false, noPatrolWaitTurn,
    _sawWithGoal;
  State _setstate;
  float noPartrolWaitTime;

  // Update is called once per frame
  void Handle()
  {
    if (_ragdoll == null || !_ragdoll._CanReceiveInput) return;
    if (!_beganPatrolling)
    {
      _ragdoll.SetActive(true);
      /*/ Set hip rot
      Quaternion rot = _ragdoll._hip.transform.parent.rotation;
      float f = transform.rotation.eulerAngles.y;
      if (f > 180.0f)
        f -= 360.0f;
      rot.eulerAngles = new Vector3(rot.eulerAngles.x, rot.eulerAngles.y, f + 180f);
      _ragdoll._hip.transform.parent.rotation = rot;*/

      _beganPatrolling = true;
      if (!_ragdoll._grappled)
      {
        _agent.enabled = true;
      }

      // Set state and begin patrolling
      _state = State.NEUTRAL;
      _lookAroundT = Time.time;// - 20f;
      _waitTimer = Time.time - 20f;
      BeginPatrol();
    }

    // Update line renderer under enemy
    if (!_ragdoll._IsDead)
    {
      if (!_IsZombieReal)
        _ragdoll.ToggleRaycasting(true);
    }

    if (!_agent.enabled)
    {

      // Check for attacking
      if (_ragdoll._grappled && _ragdoll._grappler._IsPlayer)
      {
        DrawBackMelee();

        if (Time.time - _attackTime > 0f && _canAttack)
        {

          // Only attack if is alive, the target is alive, and (the target is in front, or has a machine gun, or has a melee weapon)
          if (!_ragdoll._IsDead)
          {
            var useItem = _leftweaponuse ? _ragdoll._ItemL : (_ragdoll._ItemR != null ? _ragdoll._ItemR : _ragdoll._ItemL);

            // Check for reload
            if (_ragdoll.HasGun() && useItem.NeedsReload())
            {
              _ragdoll.Reload();
              SetAttackTime(true);
            }

            // Attack if close enough or pointed at target
            else if (_ragdoll.HasGun() || _ragdoll.HasMelee())
            {
              UseItem(true);
              if (HasMachineGun())
                _attackTime = Time.time + useItem.UseRate();
              else
                _attackTime = Time.time + (/*0.2f + Random.value */ (_ragdoll.HasSilencedWeapon() || _itemLeft == GameScript.ItemManager.Items.REVOLVER ? 0.25f : 0.55f));
            }
          }

        }
      }

    }

    else
    {

      var lookAtPos = Vector3.zero;

      var saveWait = waiting;

      if (_ragdoll.Active() && !_ragdoll._IsStunned)
      {
        if (!_IsZombie)
          _ragdoll._rotSpeed = (_state == State.PURSUIT ? _targetDirectlyInFront ? 1.15f : 0.8f : 0.6f) * PlayerScript.ROTATIONSPEED;

        // If chaser, check if enabled
        if (!_canAttack && IsChaser() && !_IsZombie)
        {
          if (Time.time - GameScript.s_LevelStartTime > 0.5f)
          {
            var info = FunctionsC.GetFarthestPlayerFrom(transform.position);
            if (info != null && info._distance > 3f + (Settings._NumberPlayers > 1 ? (1.5f * Mathf.Clamp(Settings._NumberPlayers, 0, 2) - 1) : 0f))
            {
              _canAttack = true;
              _canMove = true;
              SetRagdollTarget(FunctionsC.GetClosestTargetTo(_ragdoll, transform.position)._ragdoll);
              TargetFound();
            }
            else
            {
              var close_data = FunctionsC.GetClosestTargetTo(_ragdoll, transform.position);
              if (close_data._ragdoll != null)
              {
                lookAtPos = (close_data._ragdoll.Transform.position);
                LookAt(lookAtPos);
              }
            }
          }
        }

        // Waiting
        else if (Time.time - _waitTimer < _waitAmount && (_state != State.PURSUIT))
        {
          _atd = _waitAmount - (Time.time - _waitTimer);
          waiting = true;

          // Just started waiting
          if (!saveWait && _state == State.SUSPICIOUS)
            _waitLookPos = _lastKnownPos;

          // Look around
          if (!_patroling)
          {
            if (setstate)
            {
              if (noPatrolWaitTurn)
              {
                //_waitLookPos = transform.position + (_state == State.SEARCHING ? -(transform.position - _lastKnownPos).normalized : transform.forward) * 2f + transform.right * 0.07f * (_strafeRight ? 1f : -1f);
              }
              else if (Time.time - _lookAroundT > noPartrolWaitTime)
              {
                _lookAroundT = Time.time;
                SetRandomStrafe();
                noPartrolWaitTime = 1f + Random.value * 4f;
              }
            }
          }
          else if (Time.time - _lookAroundT > _path.GetLookWait())
          {
            _lookAroundT = Time.time;
            // Look in a direction
            Turn();
          }

          lookAtPos = _waitLookPos;
          LookAt(lookAtPos);
        }
        // Check states
        else
        {
          waiting = false;
          if (_state == State.PURSUIT)
          {
            setstate = false;
          }
          else
          {
            // Just left waiting
            if (saveWait)
            {
              // Check for change state after wait
              if (setstate)
              {
                setstate = false;
                if (_setstate == State.NEUTRAL)
                {
                  ChangeState(State.NEUTRAL);
                  _patroling = true;
                }
              }
              // Check for move back to path
              if (_state == State.NEUTRAL)
                if (_path.GetPathLength() > 1)
                  SetNextPatrolPoint();
                else
                  SetCurrentPatrolPoint();
            }
          }

          // Doing its job
          if (_state == State.NEUTRAL)
          {
            if (!_IsZombie)
            {
              // Update patrol
              if (_patroling && !IsChaser())
              {
                var dis = MathC.Get2DDistance(transform.position, _path.GetPatrolPoint().position);
                _atd = dis;
                if (dis < 0.1f)
                {
                  if (_path.GetPathLength() > 1)
                  {

                    // Look at direction
                    Transform lp = _path.GetLookPoint(),
                     p = _path.GetPatrolPoint();
                    _waitLookPos = MathC.Get2DVector(transform.position + (lp.position - p.position).normalized) + new Vector3(0f, transform.position.y, 0f);

                  }
                  _agent.Move(_path.GetPatrolPoint().position - transform.position);

                  // Wait until next movement
                  Wait(_path.GetPatrolWait(), (_path.GetPathLength() > 1));
                }
              }
              else
                Wait();
            }
          }

          // Checking something out
          else if (_state == State.SUSPICIOUS)
          {
            if (!_canMove)
            {
              _waitTimer = Time.time;
              setstate = true;
              _setstate = State.NEUTRAL;
              _waitAmount = 1f + Random.value * 3f;
              return;
            }
            var dis = MathC.Get2DDistance(transform.position, _lastKnownPos);
            _atd = dis;
            if (dis < 0.8f)
            {
              _waitTimer = Time.time;
              setstate = true;
              _setstate = State.NEUTRAL;
              noPatrolWaitTurn = true;
              _waitAmount = 6f + Random.value * 3f;
            }
            else
            {
              _waitAmount = 0f;
              _agent.SetDestination(_lastKnownPos);
            }
          }

          // Pursuing something
          else if (_state == State.PURSUIT)
          {
            _atd = Time.time - _attackTime;

            // Check if game ended
            if (GameScript.s_Singleton._GameEnded)
            {
            }

            // Check target turned invisible
            else if ((_ragdollTarget?._invisible ?? false) && !IsChaser() && _targetInLOS)
            {
              ChangeState(State.SEARCHING);
              _lastKnownPos = _ragdollTarget._Hip.position;
              _agent.SetDestination(_lastKnownPos);

              _time_lost = 0f;
              _searchDir = -MathC.Get2DVector((transform.position - _lastKnownPos)).normalized * 3f;
            }

            //
            else if (_ragdollTarget != null)
            {

              // Check for weapons
              if (CheckShouldPanic())
              {
                Panic();
              }

              // Chase player
              else
              {

                // Check if close to steering pos; try to look around corner before going around corner
                if (_canMove)
                {
                  if (MathC.Get2DDistance(_ragdoll._Hip.position, _agent.steeringTarget) < 3f)
                  {
                    var iter = 0;
                    if (_agent.path.corners.Length > 0 && !_agent.path.corners[_agent.path.corners.Length - 1].Equals(_agent.steeringTarget))
                      foreach (var p in _agent.path.corners)
                      {
                        if (p.Equals(_agent.steeringTarget))
                        {
                          var nextPos = _agent.path.corners[iter + 1];
                          if (Vector3.Distance(nextPos, _agent.steeringTarget) < 2.5f && iter + 2 < _agent.path.corners.Length - 1)
                            nextPos = _agent.path.corners[iter + 2];
                          lookAtPos = nextPos;
                        }
                        iter++;
                      }
                    //Debug.DrawLine(transform.position, _agent.steeringTarget, Color.red);
                    //Debug.DrawLine(transform.position, _lastKnownPos, Color.blue);
                  }
                }

                if (_targetInLOS)
                {
                  var dir = MathC.Get2DVector(_ragdollTarget._Hip.position - _ragdoll._Hip.position);
                  var dis = dir.magnitude;
                  dir = dir.normalized;

                  // If can't move, has gun, and player gets too close, start to chase
                  if (!_canMove)
                  {
                    if (_ragdoll.HasGun() && dis < 1.5f)
                    {
                      _canMove = true;
                      if (!_agent.hasPath)
                        _agent.SetDestination(_lastKnownPos);
                    }
                  }

                  // Check to chase player if has exit
                  if (!_canMove && _ragdollTarget._PlayerScript._HasExit)
                  {
                    _sawWithGoal = true;
                    if (dis > 13f)
                    {
                      _canMove = true;
                    }
                  }

                  // If has gun, keep distance to shoot
                  if (_ragdoll.HasGun())
                  {
                    if (_targetInFront)
                      lookAtPos = new Vector3(_ragdollTarget._Hip.position.x, transform.position.y, _ragdollTarget._Hip.position.z) + dir;
                    else
                      lookAtPos = _lastKnownPos;

                    // Check if the enemy is at the right distance to shoot
                    if (_canMove)
                    {

                      // If player has the exit, chase closer
                      float close = 3.5f, far = 10f;
                      if (_ragdollTarget._IsPlayer && _ragdollTarget._PlayerScript._HasExit)
                      {
                        close = 3f;
                        far = 4f;
                      }

                      // Move further back if reloading
                      if (_ragdoll._IsReloading)
                      {
                        close += 1.5f;
                        far += 1.5f;
                      }

                      // Keep distance per close and far values
                      if (dis > close && dis < far && Time.time - _lastPosSetTime > 0.1f)
                      {
                        _agent.SetDestination(transform.position);
                        _lastPosSetTime = Time.time;
                      }

                      // If not in distance, move into distance
                      else if (dis >= far)
                        ChaseTarget(false);
                      else
                      {
                        UnityEngine.AI.NavMeshHit hit;
                        var samplePos = transform.position + (_ragdoll._Hip.position - _ragdollTarget._Hip.position).normalized * 5f;
                        if (UnityEngine.AI.NavMesh.SamplePosition(samplePos, out hit, 4f, UnityEngine.AI.NavMesh.AllAreas))
                        {
                          var save_distance = FunctionsC.GetPathLength(_agent.path.corners);
                          var path = new UnityEngine.AI.NavMeshPath();

                          if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, samplePos, UnityEngine.AI.NavMesh.AllAreas, path))
                          {
                            var new_distance = FunctionsC.GetPathLength(path.corners);
                            var diff = new_distance - save_distance;
                            if (diff > 0.5f && diff < 2.5f)
                              _agent.SetPath(path);
                          }
                        }

                      }
                    }
                  }

                  // If has no gun, chase player
                  else
                    ChaseTarget();

                  // Check melee
                  if (!_IsZombieReal || dis < 4f)
                    DrawBackMelee();

                  // Try attacking
                  if (Time.time - _attackTime > 0f && _canAttack)
                  {
                    // If has a melee weapon and sees the target, run at them
                    if (!_ragdoll.HasGun() && _time_seen > 0.05f && _targetInLOS && _enemyType != EnemyType.ROBOT && !_IsZombie)
                      _moveSpeed = PlayerScript.RUNSPEED;

                    // If grappling, slower
                    if (_ragdoll._grappling) { _moveSpeed *= 0.9f; }

                    // Only attack if is alive, the target is alive, and (the target is in front, or has a machine gun, or has a melee weapon)
                    if (!_ragdoll._IsDead && !_ragdollTarget._IsDead && (_targetDirectlyInFront || HasMachineGun() || !_ragdoll.HasGun()))
                    {
                      var useitem = _leftweaponuse ? _ragdoll._ItemL : (_ragdoll._ItemR != null ? _ragdoll._ItemR : _ragdoll._ItemL);

                      // Check for reload
                      if (_ragdoll.HasGun() && useitem.NeedsReload())
                      {
                        _ragdoll.Reload();
                        SetAttackTime(true);
                      }

                      // Attack if close enough or pointed at target
                      else if (
                        _ragdoll.HasGun() ||
                        (!_ragdoll.HasGun() && dis < (_itemLeft == GameScript.ItemManager.Items.GRENADE_HOLD ? 1f : (_itemLeft == GameScript.ItemManager.Items.BAT ? 1.2f : (_IsZombieReal ? 0.85f : 1.8f))))
                        )
                      {
                        UseItem(dis < 1.4f);
                        if (HasMachineGun())
                          _attackTime = Time.time + useitem.UseRate();
                        else
                        {
                          _attackTime = Time.time + (/*0.2f + Random.value */ (_ragdoll.HasSilencedWeapon() || _itemLeft == GameScript.ItemManager.Items.REVOLVER ? 0.25f : 0.55f));
                        }
                      }
                    }
                  }
                }
                else if (_targetFound)
                {
                  if (!_canMove && _sawWithGoal && _time_lost > 1f)
                    _canMove = true;
                  ChaseTarget();

                  if (!_canMove)
                    lookAtPos = _lastKnownPos;

                  // Reload if chasing and not
                  var useitem = _leftweaponuse ? _ragdoll._ItemL : (_ragdoll._ItemR != null ? _ragdoll._ItemR : _ragdoll._ItemL);
                  if (useitem?.NeedsReload() ?? false)
                  {
                    _ragdoll.Reload();
                  }
                }
              }
            }
            else
            {
              ChangeState(State.SEARCHING);
              Walk();
            }
          }

          // Searching
          else if (_state == State.SEARCHING)
          {
            _lastKnownPos = transform.position + _searchDir;
            lookAtPos = _lastKnownPos;
            if (_agent.remainingDistance < 1f || !_canMove)
            {
              _patroling = true;
              SetNextPatrolPoint();
              ChangeState(State.NEUTRAL);
              _waitTimer = Time.time;
              _waitAmount = 0.2f + Random.value * 2f;
              _targetFound = false;
              _suspiciousTimer = 20f;
            }
          }

          // Run away!
          else if (_state == State.PANICKED)
          {
            Vector3 dest = _panicTarget.position;
            _moveSpeed = PlayerScript.RUNSPEED;
            if (!_agent.destination.Equals(dest)) _agent.SetDestination(dest);
            // If at exit, stop panicking
            if (_agent.remainingDistance < 1f)
            {
              Wait(100f);
            }
          }

          // No idea what to do
          else
          {
            Wait();
          }

          // Update controller
          MoveRotateTransform(lookAtPos);

          // Check if stuck and attempt to fix
          if (_ragdoll.HasGun() && _targetInLOS) { }
          else if (!_agent.hasPath && _state != State.NEUTRAL && _canMove)
          {
            _stuckIter++;
            if (_stuckIter > 20)
            {
              if (_IsZombie) { /*Debug.Log("stuck");*/ }
              else if (IsChaser() && Time.time - _lastPosSetTime > 0.1f)
              {
                _agent.SetDestination(_ragdollTarget._Hip.position);
                _lastPosSetTime = Time.time;
              }
              else
              {
                ChangeState(State.SEARCHING);
                Wait();
                Walk();
                //Debug.Log("Stuck");
              }
              _stuckIter = 0;
            }
          }
        }
        if (Time.time - GameScript.s_LevelStartTime > 0.25f && !TileManager.Tile._Moving)
          Raycast();
      }
    }

    _ragdoll.Update();
  }

  //
  void DrawBackMelee()
  {
    // Check knife
    if (_ragdoll._ItemL?._useOnRelease ?? false)
    {
      if (!_ragdoll._ItemL._TriggerDown)
        _ragdoll._ItemL.UseDown();
    }
    if (_ragdoll._ItemR?._useOnRelease ?? false)
    {
      if (!_ragdoll._ItemR._TriggerDown)
        _ragdoll._ItemR.UseDown();
    }
  }

  bool _leftweaponuse = true;
  // Alternate weapons if has multiple
  void UseItem(bool close_to_targ)
  {

    // Local
    void UseLeft()
    {
      if (_ragdoll._ItemL.IsMelee() && _ragdoll._grapplee != null) return;
      if (_ragdoll._ItemL._useOnRelease)
        _ragdoll._ItemL.UseUp();
      else
        _ragdoll.UseLeft();
    }
    void UseRight()
    {
      if (_ragdoll._ItemR.IsMelee() && _ragdoll._grapplee != null) return;
      if (_ragdoll._ItemR._useOnRelease)
        _ragdoll._ItemR.UseUp();
      else
        _ragdoll.UseRight();
    }

    //
    if (_IsZombieReal)
    {
      UseLeft();
      UseRight();
      return;
    }

    //
    _leftweaponuse = !_leftweaponuse;

    if (_ragdoll._ItemL == null && _ragdoll._ItemR == null) { return; }

    else if (_ragdoll._ItemL == null && _ragdoll._ItemR != null) { UseRight(); }
    else if (_ragdoll._ItemL != null && _ragdoll._ItemR == null) { UseLeft(); }

    else
    {

      // If same type of weapon or close to, alternate
      if ((_ragdoll._ItemL.IsGun() && _ragdoll._ItemR.IsGun()) || (!_ragdoll._ItemL.IsGun() && !_ragdoll._ItemR.IsGun()) || close_to_targ)
      {
        if (_leftweaponuse)
        {
          UseLeft();
        }
        else
        {
          UseRight();
        }
      }

      // Else, check how close
      else
      {
        //var melee = _ragdoll._itemL.IsGun() ? _ragdoll._itemR : _ragdoll._itemL;
        var gun = !_ragdoll._ItemL.IsGun() ? _ragdoll._ItemR : _ragdoll._ItemL;
        gun.UseDown();
      }
    }
  }

  void MoveRotateTransform(Vector3 lookAtPos)
  {
    _moveSpeed_lerped += (_moveSpeed - _moveSpeed_lerped) * Time.deltaTime * 3f;
    Move();
    LookAt(lookAtPos);
  }

  public bool IsChaser()
  {
    return (_enemyType == EnemyType.ROBOT);
  }

  //
  bool _linkedDoorTriggered;
  public void OnGrappled()
  {
    if (!_linkedDoorTriggered)
    {
      _linkedDoorTriggered = true;
      _linkedDoor?.OnEnemyDie(this);
    }
  }

  public void OnGrapplerRemoved()
  {
    _survivalAttributes = null;

    // Set target to nearest player
    var distanceData = FunctionsC.GetClosestTargetTo(_ragdoll, transform.position);
    if (distanceData == null || distanceData._ragdoll == null) return;
    var closestPlayer = distanceData._ragdoll;
    SetRagdollTarget(closestPlayer);

    SetRandomStrafe();
    _ragdoll._rotSpeed = PlayerScript.ROTATIONSPEED * (0.8f + Random.value * 0.3f);
    TargetFound(true, false);

    SetAttackTime(true);
  }

  //
  void SetAttackTime(bool reset = false)
  {
    if (_itemLeft == GameScript.ItemManager.Items.GRENADE_HOLD)
    {
      _attackTime = Time.time;
      return;
    }

    _attackTime = Time.time + (0.5f + (!reset ? 0f : Random.value * 0.5f));
  }

  //
  void Move()
  {

    if (!_canMove || _ragdoll._grappled) return;

    // Check survial
    if (GameScript.IsSurvival())
    {
      if (GameScript.SurvivalMode._Wave_enemies == null) return;
      if (GameScript.SurvivalMode._Wave_enemies.Count <= 1 ||
        (GameScript.SurvivalMode._Number_enemies_spawned - _survivalAttributes._WaitForEnemyHordeSizeStart) >= _survivalAttributes._WaitForEnemyHordeSize)
      { }
      else return;
    }

    var moveDir = (_agent.steeringTarget - transform.position);
    var movePos = moveDir.normalized * PlayerScript.MOVESPEED * Time.deltaTime * _moveSpeed_lerped;
    if (!Vector3.Equals(_agent.destination, transform.position))
    {
      _agent.Move(movePos);
    }
  }
  void LookAt(Vector3 lookAtPos)
  {
    if (_ragdoll?._grappled ?? true) return;

    var forward = transform.forward;
    if ((_ragdoll.HasMelee() && !_ragdoll._IsSwinging) || !_ragdoll.HasMelee() || IsChaser())
      transform.LookAt(lookAtPos == Vector3.zero ? new Vector3(_agent.steeringTarget.x, transform.position.y, _agent.steeringTarget.z) : lookAtPos);
    if (_IsZombie) return;
    var val = Mathf.Clamp(Mathf.Abs((transform.forward - forward).magnitude), 0f, 10f);
    if (val < 0.05f || val > 1f) val = 0f;
    _moveSpeed_lerped = Mathf.Clamp(_moveSpeed_lerped - val * Time.deltaTime * 15f, 0f, 10f);
  }

  // Go through "Enemies" Transform children and init
  public static void HardInitAll()
  {
    var enemies = GameScript.s_Singleton.transform.GetChild(0);
    for (var i = 0; i < enemies.childCount; i++)
    {
      var e = enemies.GetChild(i).GetChild(0).GetComponent<EnemyScript>();
      e.Init(null);
      e.EquipStart();
    }
  }

  public static int NumberAlive()
  {
    if (_Enemies_alive == null) return 0;
    return _Enemies_alive.Count;
  }

  float _DIS;
  public static int _RAYCOUNT;
  void Raycast()
  {
    if (_IsZombie)
    {
      var olddis = _DIS;
      var targ = _ragdollTarget;
      var closestPlayer = FunctionsC.GetClosestTargetTo(_ragdoll, _ragdoll._Hip.position);
      if (closestPlayer._distance < olddis && targ._Id != closestPlayer._ragdoll._Id)
        SetRagdollTarget(closestPlayer._ragdoll);
      _DIS = closestPlayer._distance;
    }
    else _DIS = FunctionsC.GetClosestTargetTo(_ragdoll, _ragdoll._Hip.position)._distance;
    /*/ Try to limit Raycast calls via enemy number, distance, and Raycast function call number
    if (!_targetInLOS && !_targetInFront && (_Enemies.Count - _NumDead) > 8)
    {
        if (_DIS > 10f) return;
        if (_RAYCOUNT++ > 15) return;
    }*/
    {
      _rayTimer = Time.time;
      RaycastHit h;
      Vector3 f = MathC.Get2DVector(_ragdoll._head.transform.forward).normalized,
         r = _ragdoll._head.transform.up;

      // Check if lost object perusing
      if (_targetFound && _state != State.PANICKED && _ragdollTarget != null)
      {
        if (!IsChaser() && !_IsZombie)
          _time_lost += Time.deltaTime;
        else
          _time_lost = 0f;

        if (_time_lost > 12f)
          Walk();

        else if (_state != State.SEARCHING && _time_lost > 11f)
        {
          ChangeState(State.SEARCHING);
          if (!Vector3.Equals(_agent.destination, _lastKnownPos))
            _agent.SetDestination(_lastKnownPos);
        }

        // Reset attack time
        else if (_time_lost > 2f)
          SetAttackTime();

        // Check if target is in LOS
        bool found = false;
        Vector3 dirToTarget = -MathC.Get2DVector(_ragdoll._Hip.transform.position - _ragdollTarget._Hip.transform.position).normalized;
        if (_IsZombie && _survivalAttributes._enemyType != GameScript.SurvivalMode.EnemyType.PISTOL_WALK)
        {
          found = true;
        }
        else
        {
          if (_DIS < 0.25f)
            found = true;
          else
          {
            h = SpherecastHandler.GetSpherecastHit(_targetInLOS || Time.time - _lastSeenTime < 1.5f ? 0 : 3);
            if (h.collider != null)
              found = CheckRay(h);
          }
        }

        if (_ragdollTarget == null || _ragdollTarget._IsDead)
        {
          _targetInLOS = false;
          _targetDirectlyInFront = false;
          _targetInFront = false;

          // If ischaser and there are more players, chase them
          if (IsChaser() || _IsZombie)
          {
            var info = FunctionsC.GetClosestTargetTo(_ragdoll, transform.position);
            if (info != null && info._ragdoll != null)
            {
              SetRagdollTarget(info._ragdoll);
              TargetFound();
            }
            else
            {
              _time_seen = 0f;
              SetRagdollTarget(null);
              _targetFound = false;
              _patroling = true;
              _waitAmount = 2f + Random.value * 3f;
              _waitTimer = Time.time;
              setstate = true;
              _setstate = State.NEUTRAL;
            }
          }
          else
          {
            _time_seen = 0f;
            SetRagdollTarget(null);
            _targetFound = false;
            _patroling = true;
            _waitAmount = 2f + Random.value * 3f;
            _waitTimer = Time.time;
            setstate = true;
            _setstate = State.NEUTRAL;
          }
          return;
        }
        else if (!found)
        {
          _time_lost += Time.deltaTime;
          _time_seen = 0f;
          _targetInLOS = false;
          _targetDirectlyInFront = false;
          _targetInFront = false;
        }
        else if (found)
        {
          _lastSeenTime = Time.time;
          _lastKnownPos = _ragdollTarget._Controller.position;
          _time_seen += Time.deltaTime;
          _time_lost = 0f;
          _targetInLOS = true;
          float lookMagnitude = (dirToTarget - MathC.Get2DVector(_ragdoll._head.transform.forward)).magnitude;
          if (lookMagnitude < 1.4f)
          {
            _targetInFront = true;
            if (lookMagnitude < (_ragdoll.HasGun() ? 0.3f : ((_ragdoll._ItemL != null && _ragdoll._ItemL.IsThrowable()) ? 0.4f : 0.2f)))
              _targetDirectlyInFront = true;
            else
              _targetDirectlyInFront = false;
          }
          else _targetInFront = false;
        }
      }
      // Check for things in LOS
      if (_IsZombie && _survivalAttributes._enemyType != GameScript.SurvivalMode.EnemyType.PISTOL_WALK) return;
      {
        bool hit = false;
        h = SpherecastHandler.GetSpherecastHit(1);
        //Debug.DrawLine(transform.position, h.point);
        if (!hit && h.collider != null)
          hit = CheckRay(h);
        h = SpherecastHandler.GetSpherecastHit(2);
        //Debug.DrawLine(transform.position, h.point);
        if (!hit && h.collider != null)
          CheckRay(h);
      }
      // Ray towards lastknownpos
      if (_canMove)
        if (_state == State.SEARCHING || _state == State.SUSPICIOUS || _suspiciousTimer > 0f)
        {
          h = SpherecastHandler.GetSpherecastHit(3);
          if (h.collider != null)
          {
            CheckRay(h, true);
            if ((_state == State.SEARCHING || (_state == State.SUSPICIOUS && _patroling)) && _canMove) LookAt(_canMove ? transform.position + f : transform.position + -MathC.Get2DVector(_ragdoll._head.transform.position - _lastKnownPos).normalized);
          }
        }
    }
    _suspiciousTimer -= Time.deltaTime;
  }

  public void Turn()
  {
    if (Time.time - GameScript.s_LevelStartTime < 1f) return;
    Transform lp = _path.GetLookPoint(),
      p = _path.GetPatrolPoint();
    _waitLookPos = MathC.Get2DVector(transform.position + (lp.position - p.position).normalized) + new Vector3(0f, transform.position.y, 0f);
  }

  bool CheckRay(RaycastHit h, bool frontMagnitudeCheck = false)
  {
    if (h.distance > 20f) return false;
    if (h.collider.name.Equals("Tile")) return false;

    // Check if found player
    var ragdoll = IsTarget(h.collider.gameObject);
    if (ragdoll != null)
    {

      // Check invisible
      if (ragdoll._invisible && !IsChaser())
        return false;

      // Set target
      SetRagdollTarget(ragdoll);

      // Do front magnitude test
      var c = true;
      if (frontMagnitudeCheck)
      {
        c = false;
        var minVal = 0.5f;
        var dirToTarget = -MathC.Get2DVector(_ragdoll._head.transform.position - _ragdollTarget._Hip.transform.position).normalized;
        var lookMagnitude = (dirToTarget - MathC.Get2DVector(_ragdoll._head.transform.forward)).magnitude;
        if (lookMagnitude < minVal)
          c = true;
      }
      // Check if should alert self
      if (!frontMagnitudeCheck || (frontMagnitudeCheck && c))
      {
        if (_ragdollTarget._IsDead)
        {
          SetRagdollTarget(null);
          return false;
        }
        _patroling = false;
        Run();
        TargetFound();
        return true;
      }
      else return false;
    }
    // Check if found another enemy
    var rag = ActiveRagdoll.GetRagdoll(h.collider.gameObject);
    if (rag != null && !rag.IsSelf(_ragdoll._Hip.gameObject) && !rag._IsPlayer)
    {
      var e = rag._Controller.GetComponent<EnemyScript>();
      // Delayed absorb
      if (Settings._DIFFICULTY > 0)
      {
        /*if (_delayedAbsorb == null && (e._state != State.NEUTRAL || e.GetRagdoll()._dead))
          _delayedAbsorb = StartCoroutine(DelayedAbsorb(e, 0.4f + Random.value * 0.9f, h));
        else if (_delayedAbsorb != null)
        {
          StopCoroutine(_delayedAbsorb);
          StartCoroutine(DelayedAbsorb(e, 0.4f + Random.value * 0.9f, h));
        }*/
      }
      // Move around enemy
      if (_state != State.NEUTRAL && h.distance < 10f && _canMove && !e._ragdoll._IsDead)
      {
        //rag._enemyScript.AbsorbInfo(this);
        if (h.distance < 2f && !rag._IsDead)
          _agent.Move((_strafeRight ? transform.right : -transform.right) * 0.6f * Time.deltaTime * _moveSpeed_lerped);
      }
    }
    return false;
  }

  #region Patrol Functions
  void SetCurrentPatrolPoint()
  {
    var p = _path.GetPatrolPoint().position;
    if (MathC.Get2DDistance(p, transform.position) == 0f) return;
    if (_survivalAttributes != null)
      Debug.Log(_agent.enabled);
    _agent.SetDestination(p);
  }
  void SetNextPatrolPoint()
  {
    Vector3 newPos = _path.GetNextPatrolPoint().position;
    _agent.SetDestination(newPos);
  }

  void ChaseTarget(bool checkTurnaround = true)
  {
    //if (_isZombie) return;
    if (_chasingTarget || _ragdollTarget._IsDead || !_canMove) return;
    StartCoroutine(ChaseTargetCo(checkTurnaround));
  }

  float _zombie_chaseTimer;
  bool _chasingTarget = false;
  IEnumerator ChaseTargetCo(bool checkTurnaround)
  {
    _chasingTarget = true;
    //if(_targetInLOS && _time_lost < 10f)
    _lastKnownPos = _ragdollTarget._Controller.position;
    // Check chaser
    if (!checkTurnaround)
      _agent.SetDestination(_lastKnownPos);
    else
    {
      // Normal calculation
      var path = _agent.path;
      var path_new = _agent.path;
      if (_agent.destination != _lastKnownPos)
      {
        var filter = new UnityEngine.AI.NavMeshQueryFilter
        {
          areaMask = 1,
          agentTypeID = _IsZombie && GameScript.IsSurvival() ? TileManager._navMeshSurface2.agentTypeID : TileManager._navMeshSurface.agentTypeID
        };
        if (!UnityEngine.AI.NavMesh.CalculatePath(transform.position, _lastKnownPos, filter, path_new)) Debug.LogError("Failed to find path. (agent enabled): " + _agent.enabled);
        int useIter = 2;
        // Check if the enemy is about to redirect
        if (!_targetInFront)
          if (path_new.corners.Length > useIter && path.corners.Length > useIter && !Vector3.Equals(path.corners[useIter], path_new.corners[useIter]))
          {
            Vector3 ang0 = (MathC.Get2DVector(path_new.corners[1]) - MathC.Get2DVector(path_new.corners[0])).normalized,
             ang1 = (MathC.Get2DVector(path.corners[1]) - MathC.Get2DVector(path.corners[0])).normalized;
            Vector3 diff = ang1 - ang0;
            // New path is about to change directions; give them their old path back
            if (diff.magnitude > 1.3f)
              _agent.SetPath(path);
            // New path is not changing directions, give them the new path
            else
              _agent.SetPath(path_new);
          }
          else
            _agent.SetPath(path_new);
        else
          _agent.SetPath(path_new);
      }
    }
    yield return new WaitForSeconds(0.15f + Random.value * 0.15f);
    _chasingTarget = false;
  }
  #endregion

  public void SetRagdollTarget(ActiveRagdoll ragdoll)
  {
    // Check for null
    if (ragdoll == null)
    {
      _ragdollTarget = null;
      return;
    }

    // Make sure is not targeting self
    if (_ragdoll._Id == ragdoll._Id) return;

    // Check for same setting
    if (_ragdollTarget != null && _ragdollTarget._Id == ragdoll._Id) return;
    _ragdollTarget = ragdoll;
  }

  #region State Change Functions
  // Fired when player is first found
  public void TargetFound(bool run = true, bool check_panic = true)
  {
    _lastKnownPos = _ragdollTarget._Controller.position;
    _lastSeenTime = Time.time;

    // Make sure not already pursuing
    if (_state == State.PURSUIT || _state == State.PANICKED) return;
    _targetFound = true;

    // Stop waiting
    _waitAmount = 0f;
    _waitTimer = Time.time;

    // Reset lost timer
    _time_lost = 0f;
    _time_seen = 0f;

    // Chase!
    if (run) Run();

    // Set variable to move around other enemies
    SetRandomStrafe();

    // Check if should run away
    if (check_panic && CheckShouldPanic())
    {
      Panic();
      return;
    }
    ChangeState(State.PURSUIT);

    // Set next attack time
    SetAttackTime();
  }

  public void SetRandomStrafe()
  {
    _strafeRight = (Random.Range(0, 2) == 0 ? true : false);
  }

  bool CheckShouldPanic()
  {
    _playerKnownWeapon = true;
    if (_ragdollTarget != null) _playerKnownWeaponValue = _ragdollTarget.HasWeapon();
    if (_playerKnownWeapon) return (!HasWeapon());
    return false;
  }
  void Panic()
  {
    ChangeState(State.PANICKED);
    // Run away!
    Run();
    // Stop waiting
    _waitAmount = 0f;
    _waitTimer = Time.time;
    // Set targ
    _panicTarget = GameObject.Find("Powerup").transform;
  }

  Loudness _lastSuspiciousLoudness = Loudness.SUPERSOFT;
  // Fired when enemy hears something from far away
  Coroutine _suspiciousCoroutine;
  public void Suspicious(Vector3 source, Loudness loudness)
  {
    Suspicious(source, loudness, Random.value * 0.5f);
  }
  public void Suspicious(Vector3 source, Loudness loudness, float waittime)
  {
    if (Time.time - GameScript.s_LevelStartTime < 0.2f || TileManager.Tile._Moving) return;
    if (_suspiciousCoroutine != null) StopCoroutine(_suspiciousCoroutine);
    if (!_agent.enabled) return;
    // Return if already chasing or running away
    if (_state == State.PANICKED || _state == State.PURSUIT) return;
    if (_lastSuspiciousLoudness != Loudness.LOUD && loudness == Loudness.LOUD && _state == State.SUSPICIOUS) return;
    _suspiciousCoroutine = StartCoroutine(SuspiciousCo(source, loudness, waittime));
  }
  IEnumerator SuspiciousCo(Vector3 source, Loudness loudness, float wait)
  {
    yield return new WaitForSeconds(wait);
    if (!_agent.enabled || _ragdoll._IsDead) { }
    else
    {
      _waitAmount = 0f;
      _waitTimer = Time.time - 2f;
      SetRandomStrafe();
      setstate = false;
      // Stop patrolling
      _patroling = false;
      // Update last known position and move towards
      _lastKnownPos = source;
      if (!Vector3.Equals(_lastKnownPos, _agent.destination))
      {
        _agent.SetDestination(_lastKnownPos);
      }
      // If searching already, run, reset the time the target is lost, and update the search dest. (dir) to near the noise;
      if (_state == State.SEARCHING)
      {
        Run();
        _time_lost = 0f;
        _searchDir = -MathC.Get2DVector((transform.position - _lastKnownPos)).normalized * 3f;
        _ragdoll.DisplayText("?");
        _ragdoll.PlaySound("Enemies/Suspicious", 0.9f, 1.1f);
      }
      else if (_state != State.SUSPICIOUS)
      {
        ChangeState(State.SUSPICIOUS);
        if (loudness == Loudness.LOUD)
        {
          if (CheckShouldPanic()) Panic();
          else Run();
        }
        else
          Run();
      }
      _lastSuspiciousLoudness = loudness;
    }
    _suspiciousCoroutine = null;
  }

  // Change movement speed
  void Walk()
  {
    if (_IsZombie)
    {
      return;
    }
    _moveSpeed = 0.5f;
  }
  void Run()
  {
    if (_IsZombie) return;
    if (IsChaser())
    {
      _moveSpeed = 0.7f;
      return;
    }
    _moveSpeed = 0.8f;
  }
  void Wait(float waitAmount = 4f, bool setLookAroundT = true)
  {
    _waitTimer = Time.time;
    _waitAmount = waitAmount;
    if (setLookAroundT) _lookAroundT = Time.time;
  }

  float _lastPersuitTimer;
  void ChangeState(State newState)
  {
    if (_state == newState) return;
    if (newState == State.PURSUIT)
    {
      if (Time.time - _lastPersuitTimer < 0.25f) return;
      _lastPersuitTimer = Time.time;
      if (!_IsZombie)
      {
        _ragdoll.DisplayText("!");
        if (_state != State.SUSPICIOUS && !IsChaser())
          _ragdoll.PlaySound("Enemies/Suspicious", 0.9f, 1.1f);
      }
    }
    else if (newState == State.SUSPICIOUS)
    {
      _ragdoll.PlaySound("Enemies/Suspicious", 0.9f, 1.1f);
      _ragdoll.DisplayText("?");

    }

    _state = newState;
  }

  public ActiveRagdoll IsTarget(GameObject obj)
  {
    if (PlayerScript.s_Players == null) return null;
    foreach (PlayerScript p in PlayerScript.s_Players)
    {
      if (!p._CanDetect || p._Ragdoll._IsDead) continue;
      if (p._Ragdoll.IsSelf(obj)) return p._Ragdoll;
    }
    return null;
  }

  // Called when first starts patroling at begining of scene to check path
  void BeginPatrol()
  {
    // Check if already patrolling
    if (_patroling)
    {
      //Debug.LogError("Trying to patrol when patrolling");
      return;
    }
    // Check if has no path
    else if (_path == null || _path.GetPathLength() == 0)
    {
      //.LogError("Beginning to patrol with no path");
      return;
    }

    // Patrol
    _patroling = true;
    SetCurrentPatrolPoint();

    // Look at new pos
    var p = _path.GetLookPoint(false);
    _waitLookPos = MathC.Get2DVector(p.position) + new Vector3(0f, transform.position.y, 0f);
  }
  #endregion

  public static EnemyScript LoadEnemy(Vector3 position)
  {
    GameObject new_gameobject = GameObject.Instantiate(GameResources._Enemy);
    new_gameobject.transform.parent = GameResources.s_Game.transform.GetChild(0);
    new_gameobject.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
    new_gameobject.transform.position = position;
    new_gameobject.transform.localPosition = new Vector3(new_gameobject.transform.localPosition.x, -1.32f, new_gameobject.transform.localPosition.z);
    new_gameobject.name = "Enemy";
    return new_gameobject.transform.GetChild(0).GetComponent<EnemyScript>();
  }

  /*public EnemyScript _absorbee;
  public List<EnemyScript> _bodies;
  void AbsorbInfo(EnemyScript other)
  {
    if (MathC.Get2DDistance(other.transform.position, transform.position) > 25f) return;
    _absorbee = other;
    var otherNewer = _lastSeenTime < other._lastSeenTime;
    // If other has newer info, take info
    if (otherNewer && other._playerKnownWeapon)
    {
      _playerKnownWeapon = other._playerKnownWeapon;
      _playerKnownWeaponValue = other._playerKnownWeaponValue;
    }
    // Check for dead bodies
    if (other._ragdoll._dead)
    {
      // Check if seen this body before
      if (_bodies == null) _bodies = new List<EnemyScript>();
      var found = false;
      foreach (var e in _bodies)
      {
        if (e._id == other._id)
        {
          found = true;
          break;
        }
      }
      if (!found)
      {
        if (CheckShouldPanic()) Panic();
        else if (_state == State.NEUTRAL) Suspicious(other._ragdoll._hip.position, Loudness.SOFT, 0f);
        LookAt(other._ragdoll.transform.position);
        _bodies.Add(other);
      }
      return;
    }
    // Return if data is older than self or already panicked
    if (!otherNewer && other._state != State.PANICKED) return;
    _lastSeenTime = other._lastSeenTime;
    switch (other._state)
    {
      case (State.PURSUIT):
        if (_state == State.PANICKED)
          break;
        Alert(other._lastKnownPos);
        if (_state != State.PURSUIT) ChangeState(State.SUSPICIOUS);
        _agent.SetDestination(_lastKnownPos);
        if (other._time_lost <= _time_lost || other._time_seen >= _time_seen)
        {
          _time_lost = other._time_lost;
          _time_seen = other._time_seen;
          if (other._targetInLOS)
            _targetInLOS = other._targetInLOS;
        }
        break;
      case (State.PANICKED):
        if (_state == State.SUSPICIOUS || _state == State.PURSUIT) break;
        // If the player is known to have a weapon and self is unarmed, panic
        if (!HasWeapon())
        {
          _lastKnownPos = other._lastKnownPos;
          Panic();
          break;
        }
        // Else, if has weapon, go towards
        Alert(other._lastKnownPos);
        ChangeState(State.SUSPICIOUS);
        _agent.SetDestination(_lastKnownPos);
        Run();
        break;
      case (State.SUSPICIOUS):
        if (_state == State.SUSPICIOUS || _state == State.PANICKED || _state == State.PURSUIT) break;
        ChangeState(other._state);
        _lastKnownPos = other._lastKnownPos;
        _patroling = false;
        _waitAmount = 0f;
        _agent.SetDestination(_lastKnownPos);
        Run();
        break;
    }
  }*/

  public void EquipStart()
  {
    // Equip start items
    if (_itemLeft != GameScript.ItemManager.Items.NONE)
    {
      GameScript.ItemManager.SpawnItem(_itemLeft);
      _ragdoll.EquipItem(_itemLeft, ActiveRagdoll.Side.LEFT);
    }
    else
      _ragdoll.AddArmJoint(ActiveRagdoll.Side.LEFT);
    if (_itemRight != GameScript.ItemManager.Items.NONE)
    {
      GameScript.ItemManager.SpawnItem(_itemRight);
      _ragdoll.EquipItem(_itemRight, ActiveRagdoll.Side.RIGHT);
    }
    else
      _ragdoll.AddArmJoint(ActiveRagdoll.Side.RIGHT);

    // Special
    if (_itemLeft == GameScript.ItemManager.Items.PISTOL_SILENCED)
    {
      IEnumerator delay_grappler()
      {
        yield return new WaitForSeconds(0.1f);
        if (!(_ragdoll?._IsDead ?? true))
          _ragdoll.AddGrappler(false);
      }
      StartCoroutine(delay_grappler());
    }

    // Zombie
    if (_IsZombieReal && _itemLeft == GameScript.ItemManager.Items.AXE)
    {
      _ragdoll._ItemL.transform.GetChild(0).GetComponent<Renderer>().enabled = false;
      _ragdoll._ItemR.transform.GetChild(0).GetComponent<Renderer>().enabled = false;
    }
  }

  public static bool AllDead()
  {
    if (_Enemies_alive == null) return true;
    return _Enemies_alive.Count == 0;
  }

  public void OnToggle(ActiveRagdoll source, ActiveRagdoll.DamageSourceType damageSourceType)
  {

    // Check achievement
    if (source != null && source._IsPlayer)
    {

#if UNITY_STANDALONE

      // Get a kill
      if (_Enemies_dead == null || _Enemies_dead.Count == 0)
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.KILL);
#endif

      /*/ Save stats
      if (GameScript._GameMode == GameScript.GameModes.CLASSIC)
      {
        var item_type = _ragdoll._itemL != null ? _ragdoll._itemL._type : _ragdoll._itemR._type;
        switch (item_type)
        {
          case (GameScript.ItemManager.Items.KNIFE):
            Stats.OverallStats._Enemies_Killed_Knife++;
            break;
          case (GameScript.ItemManager.Items.PISTOL):
            Stats.OverallStats._Enemies_Killed_Pistol++;
            break;
          case (GameScript.ItemManager.Items.PISTOL_SILENCED):
            Stats.OverallStats._Enemies_Killed_PistolSilenced++;
            break;
          case (GameScript.ItemManager.Items.GRENADE_HOLD):
            Stats.OverallStats._Enemies_Killed_Grenade++;
            break;
          case (GameScript.ItemManager.Items.REVOLVER):
            Stats.OverallStats._Enemies_Killed_Revolver++;
            break;
        }
      }
      else if (GameScript._GameMode == GameScript.GameModes.SURVIVAL)
      {
        if (_survivalAttributes._enemyType == GameScript.SurvivalMode.EnemyType.ARMORED)
          Stats.OverallStats._Enemies_Killed_Survival_Armored++;
        else
          Stats.OverallStats._Enemies_Killed_Survival_Basic++;
      }*/
    }

#if UNITY_STANDALONE

    // Grapple achievement
    if (!(source?._IsPlayer ?? true) && source._grappled && (source._grappler?._IsPlayer ?? false))
      SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.GRAPPLE_KILL);
#endif

    if (!_linkedDoorTriggered)
    {
      _linkedDoorTriggered = true;
      _linkedDoor?.OnEnemyDie(this);
    }

    if (Time.time - _ragdoll._lastBubbleScriptTime < 0.4f) _ragdoll.DisplayText("");

    // Swap containers
    _Enemies_alive.Remove(this);
    if (_Enemies_dead == null) _Enemies_dead = new List<EnemyScript>();
    _Enemies_dead.Add(this);
    if (_Enemies_dead.Count > _MAX_RAGDOLLS_DEAD)
    {
      var e = _Enemies_dead[0];
      ActiveRagdoll.s_Ragdolls.Remove(e._ragdoll);
      _Enemies_dead.Remove(e);
      Destroy(e.transform.parent.gameObject);
    }

    // Check to see if #bodies is too much
    if (_Enemies_dead.Count > 8)
      foreach (EnemyScript e in _Enemies_dead)
        if (e._ragdoll._IsDead && !e._ragdoll._IsDisabled)
        {
          e._ragdoll.Disable();
          break;
        }

    // Check for last enemy killed
    var last_killed = false;

    // First enemy killed
    if (_Enemies_dead.Count == 1)
    {

      // Take snapshot of extras
      PlayerScript.s_ExtrasSnapshot = Settings.GetExtrasSnapshot();

      // Save player num
      PlayerScript.s_NumPlayersStart = Settings._NumberPlayers;

      // Register equip
      if (PlayerScript.s_Players != null)
        foreach (var p in PlayerScript.s_Players)
          p?.RegisterEquipment();
    }

    // Sneaky difficulty
    if (Settings._DIFFICULTY == 0)
    {
      if (_Enemies_alive.Count == 0)
        last_killed = true;
    }

    // Sneakier difficulty
    else if (!IsChaser())
    {
      if (
        (_Enemies_alive.Count == 1 && _Enemies_alive[0].IsChaser()) ||
        _Enemies_alive.Count == 0
      )
        last_killed = true;
    }

    // Slowmo
    if (last_killed)
    {

      GameScript.OnLastEnemyKilled();

      // Check for slowmo setting
      if (Settings._Slowmo_on_lastkill)
        PlayerScript._SlowmoTimer += 1.3f;

      // Check mode
      if (GameScript.s_GameMode == GameScript.GameModes.SURVIVAL)
      {
        if (source._IsPlayer)
          GameScript.SurvivalMode.GivePoints(source._PlayerScript._Id, 5 * GameScript.SurvivalMode._Wave, true);
      }

      else
      {

        // Level timer
        TileManager._Level_Complete = true;

        if (GameScript.s_GameMode == GameScript.GameModes.CLASSIC && !GameScript.s_EditorTesting && !Levels._LevelPack_Playing)
        {

          var levelComplete = Levels._CurrentLevelCollectionIndex > 1 ? false : LevelModule.LevelData[Levels._CurrentLevelCollectionIndex].Data[Levels._CurrentLevelIndex].Completed;
          if (!levelComplete)
          {
            GameScript.MarkLevelCompleted();

            Settings.LevelSaveData.Save();
          }

          // Check timers
          var can_save_timers = !Settings._Extras_UsingAnyImportant;
          var level_time = TileManager._LevelTimer.ToStringTimer().ParseFloatInvariant();
          var level_time_best = LevelModule.GetLevelBestTime();

          // Give player time and awards if alive after 0.5 seconds
          IEnumerator AwardPlayer()
          {

            var gameId = GameScript.s_GameId;
            yield return new WaitForSecondsRealtime(0.5f);

            var saveDat = false;

            if (PlayerScript._All_Dead || gameId != GameScript.s_GameId)
            {
              TileManager._Text_LevelTimer_Best.text += string.Format(" -> <s>{0}</s> (dead)", level_time.ToStringTimer());
            }
            else
            {

              // Check best player time
              if (can_save_timers)
              {
                if (level_time_best == -1 || level_time < level_time_best)
                {
                  LevelModule.SetLevelBestTime(level_time);
                  TileManager._Text_LevelTimer_Best.text += string.Format(" -> {0}", level_time.ToStringTimer());

                  saveDat = true;
                }
              }

              // Cannot save score
              else
              {
                TileManager._Text_LevelTimer_Best.text += string.Format(" -> <s>{0}</s> (extras on)", level_time.ToStringTimer());
              }

              // Show time difference between best time
              if (level_time_best != -1f && level_time != level_time_best)
              {
                if (level_time < level_time_best)
                  TileManager._Text_LevelTimer.text = string.Format($"{{0}} (<color=green>-{{1}}</color>)", level_time.ToStringTimer(), (level_time_best - level_time).ToStringTimer());
                else
                  TileManager._Text_LevelTimer.text = string.Format($"{{0}} (<color=red>+{{1}}</color>)", level_time.ToStringTimer(), (level_time - level_time_best).ToStringTimer());
              }

              // Time ratings
              var ratingIndex = -1;
              var best_dev_time = TileManager._LevelTime_Dev;

              var medal_times = Levels.GetLevelRatingTimings(best_dev_time);
              var ratings = Levels.GetLevelRatings();

              var index = 0;
              var points_awarded = 0;
              var points_awarded_table = new int[] { -1, -1, -1, -1 };
              foreach (var time in medal_times)
              {
                var time_ = time.ToStringTimer().ParseFloatInvariant();
                if (level_time <= time_)
                {

                  if (ratingIndex == -1)
                    ratingIndex = index;

                  // Check new medal
                  if (level_time_best > time || level_time_best == -1f)
                  {
                    points_awarded++;
                    points_awarded_table[index] = index;
                  }

                }

                index++;
              }

              // FX
              TileManager._Text_LevelTimer_Best.text += "\n\n";
              var medal_format = "<color={0}>{1,-5}: {2,-6}</color>\n";
              var played_wrong = false;
              var points_awarded_counter = points_awarded;
              if (can_save_timers && Shop._AvailablePoints != 999)
                TileManager._Text_Money.text = $"$${Shop._AvailablePoints}";
              for (var i = medal_times.Length - 1; i >= 0; i--)
              {
                var time = medal_times[i];
                var time_ = string.Format("{0}", time.ToStringTimer()).ParseFloatInvariant();
                var timeText = string.Format("{0}", time_.ToStringTimer());

                TileManager._Text_LevelTimer_Best.text += string.Format(medal_format, ratings[i].Item2, ratings[i].Item1, time == -1f ? "-" : timeText + (ratingIndex == i ? "*" : ""));

                // Show $$
                if (can_save_timers && Shop._AvailablePoints != 999 && points_awarded_table.Contains(i))
                {
                  TileManager.MoveMonie(3 - i, points_awarded - points_awarded_counter--, timeText.Length < 6 ? 0 : 1);
                }

                // FX
                if (i < ratingIndex)
                {
                  if (!played_wrong)
                  {
                    played_wrong = true;
                    SfxManager.PlayAudioSourceSimple(GameResources.s_AudioListener.transform.position, "Etc/Wrong", 0.95f, 1f, SfxManager.AudioClass.NONE, false, false);
                  }
                }
                else if (i > ratingIndex)
                {
                  var mod = i * 0.15f;
                  SfxManager.PlayAudioSourceSimple(GameResources.s_AudioListener.transform.position, "Etc/Best_rank", 0.95f - mod, 1f - mod, SfxManager.AudioClass.NONE, false, false);
                }
                else
                {
                  if (ratingIndex == 0)
                    SfxManager.PlayAudioSourceSimple(GameResources.s_AudioListener.transform.position, "Etc/Best_rank", 0.95f, 1f, SfxManager.AudioClass.NONE, false, false);
                  else
                  {

                    var mod = i * 0.15f;
                    SfxManager.PlayAudioSourceSimple(GameResources.s_AudioListener.transform.position, "Etc/Best_rank", 0.95f - mod, 1f - mod, SfxManager.AudioClass.NONE, false, false);
                  }
                }

                if (!played_wrong)
                  yield return new WaitForSecondsRealtime(0.1f);
              }

              // Save stuff
              if (can_save_timers)
              {

                // Save best dev time
                if (/*false && */Debug.isDebugBuild)
                {

                  if (TileManager._LevelTime_Dev == -1 /*|| level_time < TileManager._LevelTime_Dev*/)
                  {
                    TileManager._LevelTime_Dev = level_time;

                    // Set level data
                    var level_data_split = Levels._CurrentLevelData.Split(' ');
                    var level_data_new = new List<string>();
                    index = -1;
                    var levelname_index = -1;
                    foreach (var d in level_data_split)
                    {

                      index++;

                      if (d.StartsWith("bdt_"))
                      {
                        index--;
                        continue;
                      }

                      level_data_new.Add(d);

                      if (d.StartsWith("+"))
                      {
                        levelname_index = index;
                      }
                    }

                    var add_data = $"bdt_{level_time}";

                    if (levelname_index == -1)
                    {
                      level_data_new.Add(add_data);
                    }
                    else
                    {
                      level_data_new.Insert(levelname_index - 1, add_data);
                    }

                    Levels._CurrentLevelCollection._levelData[Levels._CurrentLevelIndex] = TileManager._CurrentMapData = string.Join(" ", level_data_new);
                    Levels.SaveLevels();

                    LevelModule.SetLevelBestTime(-1f);
                    saveDat = true;

                    Debug.Log($"Set best dev time: {level_time}");
                  }

                }

                // Give points based on medals
                {

                  if (points_awarded > 0)
                  {
                    if (can_save_timers)
                    {
                      Shop._AvailablePoints += points_awarded;
                      saveDat = true;
                      //Debug.Log($"Awarded {points_awarded} points");

                      // Check all levels in difficulty completed
                      if (Settings._CurrentDifficulty_NotTopRated)
                      {
                        var levelratings_difficulty = Levels._Levels_All_TopRatings[Settings._DIFFICULTY];
                        levelratings_difficulty[Levels._CurrentLevelIndex] = ratingIndex == 0;

                        var all_top_rated = true;
                        for (var i = Levels._CurrentLevelCollection._levelData.Length - 1; i > 0; i--)
                        {
                          var top_rated = levelratings_difficulty[i];
                          if (!top_rated)
                          {
                            all_top_rated = false;
                            break;
                          }
                        }
                        //Debug.Log($"All top rated: {all_top_rated}: {Settings._DIFFICULTY}");
                        if (all_top_rated)
                        {
                          if (Settings._DIFFICULTY == 0)
                            LevelModule.IsTopRatedClassic0 = true;
                          else
                            LevelModule.IsTopRatedClassic1 = true;
                        }
                      }
                    }
                    //else
                    //Debug.Log($"Fake awarded {points_awarded} points");
                  }
                }

              }

              // Check extra unlocks
              {
                var prereqsSatisfied = true;

                // Check extras menu
                if (!Shop.Unlocked(Shop.Unlocks.MODE_EXTRAS))
                {
                  //Debug.LogWarning($"No extras; extras menu not unlocked");
                  prereqsSatisfied = false;
                }

                // Make sure player count not changed
                if (PlayerScript.s_NumPlayersStart != 1 || Settings._NumberPlayers != 1)
                {
                  //Debug.LogWarning($"No extras; player count: {PlayerScript.s_NumPlayersStart} - {PlayerScript.s_Players.Count}");
                  prereqsSatisfied = false;
                }

                // Make sure extras not changed
                var extrasSnapshot = Settings.GetExtrasSnapshot();
                if (!extrasSnapshot.SequenceEqual(PlayerScript.s_ExtrasSnapshot))
                {
                  //Debug.LogWarning("No extras; extras changed");
                  prereqsSatisfied = false;
                }

                // Make sure loadout not changed
                bool EquipmentIsEqual(GameScript.PlayerProfile.Equipment e0, GameScript.PlayerProfile.Equipment e1)
                {

                  // Check items equal
                  var equipment0_items = new List<GameScript.ItemManager.Items>(){
                  e0._ItemLeft0,
                  e0._ItemRight0,
                  e0._ItemLeft1,
                  e0._ItemRight1
                };
                  var equipment1_items = new List<GameScript.ItemManager.Items>(){
                  e1._ItemLeft0,
                  e1._ItemRight0,
                  e1._ItemLeft1,
                  e1._ItemRight1
                };
                  foreach (var item in equipment0_items)
                  {
                    if (equipment1_items.Contains(item))
                    {
                      equipment1_items.Remove(item);
                    }
                  }
                  if (equipment1_items.Count > 0)
                    return false;

                  // Check perks equal
                  if (e0._Perks.Count != e1._Perks.Count)
                  {
                    return false;
                  }
                  var perkList = new List<Shop.Perk.PerkType>(e1._Perks);
                  foreach (var perk0 in e0._Perks)
                  {
                    if (perkList.Contains(perk0))
                    {
                      perkList.Remove(perk0);
                    }
                  }
                  if (perkList.Count > 0)
                    return false;

                  // Check utilities equal
                  var utilsTotal = new List<UtilityScript.UtilityType>();
                  foreach (var util in e0._UtilitiesLeft)
                    utilsTotal.Add(util);
                  foreach (var util in e0._UtilitiesRight)
                    utilsTotal.Add(util);

                  foreach (var util in e1._UtilitiesLeft)
                  {
                    if (utilsTotal.Contains(util))
                    {
                      utilsTotal.Remove(util);
                    }
                  }
                  foreach (var util in e1._UtilitiesRight)
                  {
                    if (utilsTotal.Contains(util))
                    {
                      utilsTotal.Remove(util);
                    }
                  }
                  if (utilsTotal.Count > 0)
                    return false;

                  //
                  return true;
                }

                var equipmentStart = PlayerScript.s_Players[0]._EquipmentStart;
                var equipment_changed = PlayerScript.s_Players[0]._EquipmentChanged;
                if (equipment_changed || !EquipmentIsEqual(equipmentStart, PlayerScript.s_Players[0]._Equipment))
                {
                  //Debug.LogWarning($"No extras; equipment changed ({equipment_changed})");
                  prereqsSatisfied = false;
                }

                if (prereqsSatisfied)
                  foreach (var extraMeta in Settings.s_Extra_UnlockCriterea)
                  {

                    var extraUnlock = extraMeta.Key;
                    var extraInfo = extraMeta.Value;

                    // Check level and difficulty
                    var level = extraInfo.level;
                    var diff = extraInfo.difficulty;

                    if (Levels._CurrentLevelIndex + 1 != level || Settings._DIFFICULTY != diff)
                    {
                      continue;
                    }

                    // Check extras
                    if (extraInfo.extras != null)
                    {

                      // Horde
                      if (extraInfo.extras.Contains(Shop.Unlocks.EXTRA_HORDE))
                      {
                        if (LevelModule.ExtraHorde == 0)
                          continue;
                      }

                      // Time
                      if (extraInfo.extras.Contains(Shop.Unlocks.EXTRA_TIME))
                      {
                        if (LevelModule.ExtraTime == 0)
                          continue;
                      }

                    }

                    // Check ranking
                    if (ratingIndex == -1 || extraMeta.Value.rating < ratingIndex)
                    {
                      continue;
                    }

                    // Check loadout
                    var equipmentFake = new GameScript.PlayerProfile.Equipment();
                    {
                      if (extraInfo.items?.Length > 0)
                        equipmentFake._ItemLeft0 = extraInfo.items[0];
                      if (extraInfo.items?.Length > 1)
                        equipmentFake._ItemRight0 = extraInfo.items[1];
                      if (extraInfo.items?.Length > 2)
                        equipmentFake._ItemLeft1 = extraInfo.items[2];
                      if (extraInfo.items?.Length > 3)
                        equipmentFake._ItemRight1 = extraInfo.items[3];

                      equipmentFake._UtilitiesLeft = extraInfo.utilities == null ? new UtilityScript.UtilityType[0] : extraInfo.utilities;

                      if (extraInfo.perks != null)
                        equipmentFake._Perks = new List<Shop.Perk.PerkType>(extraInfo.perks);
                    }

                    if (!EquipmentIsEqual(equipmentStart, equipmentFake))
                    {
                      continue;
                    }

                    // Award extra in shop
                    //Debug.Log($"Unlocked {extraUnlock}");
                    Shop.AddAvailableUnlock(extraUnlock, true);
                    Shop.Unlock(extraUnlock);
                    saveDat = true;

                    // Achievements
#if UNITY_STANDALONE

                    // Unlock one achievement
                    SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.EXTRA_UNLOCK1);

                    // Unlocked all achievements
                    if (Shop.AllExtrasUnlocked())
                      SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.EXTRA_UNLOCK_ALL);
#endif
                  }
              }
            }

            // Check all times beaten
            if (LevelModule.IsTopRatedClassic0)
            {
              // Achievement
#if UNITY_STANDALONE
              SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.TIME_BEAT_SNEAKY);
#endif

              if (LevelModule.IsTopRatedClassic1)
              {
                // Achievement
#if UNITY_STANDALONE
                SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.TIME_BEAT_ALL);
#endif
              }
            }

            // Last killed settings
            if (last_killed && SettingsModule.LevelEndCondition == Settings.SettingsSaveData.LevelEndConditionType.LAST_ENEMY_KILLED)
            {
              if (LevelModule.ExtraHorde == 1 && !PlayerScript.HasExit())
              { }
              else
              {
                yield return new WaitForSecondsRealtime(0.5f);
                GameScript.OnLevelComplete();
              }
            }

            if (saveDat)
              Settings.LevelSaveData.Save();

          }
          StartCoroutine(AwardPlayer());

          // Teleport exit to player
          if (level_time_best != -1f)
          {
            if (!PlayerScript.HasExit())
            {
              if (source._IsPlayer)
              {
                Powerup._Powerups[0].Activate(source);
              }
              else
              {
                var closest_player = PlayerScript.GetClosestPlayerTo(new Vector2(_ragdoll._Controller.position.x, _ragdoll._Controller.position.z));
                if (closest_player != null)
                {
                  Powerup._Powerups[0].Activate(closest_player._Ragdoll);
                }
              }
            }
          }
          // Make goal bigger
          else if (!PlayerScript.HasExit() && Powerup._Powerups != null && Powerup._Powerups.Count > 0)
          {
            Powerup._Powerups[0].transform.GetChild(0).GetComponent<BoxCollider>().size *= 3f;
          }
        }
      }
    }

    else
    {

      // If timer hasn't started, start if killed
      if (!PlayerScript._TimerStarted)
      {
        PlayerScript.StartLevelTimer();
      }
    }

    // Check for ischaser
    if (_Chaser != null && !_Chaser._canMove)
    {
      //Debug.Log("Chaser activated: kill");
      _Chaser._canAttack = true;
      _Chaser._canMove = true;
      var playerInfo = FunctionsC.GetClosestTargetTo(_ragdoll, transform.position);
      if (playerInfo._ragdoll == null || playerInfo._ragdoll == null) return;
      _Chaser.SetRagdollTarget(playerInfo._ragdoll);
      _Chaser.TargetFound();
      _Chaser = null;
    }

    // Increment survival score
    if (GameScript.s_GameMode == GameScript.GameModes.SURVIVAL && source._IsPlayer)
      GameScript.SurvivalMode.IncrementScore(source._PlayerScript._Id);
  }

  //
  // Check medal
  public static string GetFormattedMedalColor(float medalTime)
  {

    var best_dev_time = TileManager._LevelTime_Dev;

    var medal_diamond = best_dev_time + 0.5f;
    var medal_times = new float[]{
      medal_diamond,  // Diamond
      medal_diamond * 1.1f,  // Gold
      medal_diamond * 1.5f,   // Silver
      medal_diamond * 2f,   // Bronze
    };
    var medals = new System.Tuple<string, string>[]{
      System.Tuple.Create("diamond", "lightblue"),
      System.Tuple.Create("gold", "yellow"),
      System.Tuple.Create("silver", "grey"),
      System.Tuple.Create("bronze", "brown"),
    };

    var index = 0;
    var medal_index = 0;
    //TileManager._Text_LevelTimer_Best.text += "\n\n";
    foreach (var time in medal_times)
    {

      var time_ = time.ToStringTimer().ParseFloatInvariant();
      //Debug.Log($"{medal_index} ... {time_}");

      if (medalTime <= time_)
      {
        medal_index = index;
      }

      //TileManager._Text_LevelTimer_Best.text += $"{medals[index].Item1} - {time_}\n";

      index++;
    }

    // Award medal
    if (medal_index < medal_times.Length)
    {
      //TileManager._Text_LevelTimer.text += $" {medals[medal_index]}";
      //return $"<color={medals[medal_index].Item2}>{medalTime}</color>";
      return medals[medal_index].Item2;
    }
    //return medalTime + "";
    return "white";
  }


  // Wrapper to see if has weapon
  bool HasWeapon()
  {
    return _ragdoll.HasWeapon();
  }
  bool HasMachineGun()
  {
    return _ragdoll.HasAutomatic();
  }

  public static EnemyScript SpawnEnemyAt(SurvivalAttributes survivalAttributes, Vector2 spawnAtPos, bool grappled = false, bool isZombieRealOverride = false)
  {

    var weapon = "knife";
    if (survivalAttributes._enemyType == GameScript.SurvivalMode.EnemyType.GRENADE_JOG)
      weapon = "grenade";
    else if (survivalAttributes._enemyType == GameScript.SurvivalMode.EnemyType.PISTOL_WALK)
      weapon = "pistol";

    var enemy = TileManager.LoadObject($"e_0_0_li_{weapon}_");
    var e = enemy.transform.GetChild(0).GetComponent<EnemyScript>();
    e.transform.position = new Vector3(spawnAtPos.x, enemy.transform.position.y, spawnAtPos.y);
    if (PlayerScript.s_Players != null && PlayerScript.s_Players.Count > 0 && PlayerScript.s_Players[0] != null)
      e.LookAt(PlayerScript.s_Players[0].transform.position);
    e._isZombieRealOverride = isZombieRealOverride;
    e.Init(survivalAttributes, grappled);
    e.EquipStart();

    return e;
  }

  #region Sound Functions
  // Check if enemy hears a noise
  public enum Loudness
  {
    SUPERSOFT,
    SOFT,
    NORMAL,
    LOUD
  }
  Loudness _lastLoudness = Loudness.SUPERSOFT;
  int lastBulletID;
  public static void CheckSound(Vector3 position, Loudness loudness, int bulletID = -1, bool slowReaction = true)
  {
    CheckSound(position, position, loudness, bulletID, slowReaction);
  }
  /// <summary>
  /// Noise of volume loudness is produced a position noisePosition.
  /// If an enemy is within range of noisePosition, they are suspicious at position sourcePosition
  /// </summary>
  public static void CheckSound(Vector3 noisePosition, Vector3 sourcePosition, Loudness loudness, int bulletID = -1, bool slowReaction = true)
  {
    if (_Enemies_alive == null || GameScript.IsSurvival()) return;

    // Decide distance
    var minDistance = loudness == Loudness.SUPERSOFT ? 1.5f : (loudness == Loudness.SOFT ? 3f : (loudness == Loudness.NORMAL ? 6f : 9f));

    // Check each enemy
    foreach (var e in _Enemies_alive)
    {
      // If the ragdoll is dead continue
      if (e._ragdoll._IsDead || !e._reactToSound || e._IsZombie) continue;

      // If ragdoll is chasing, continue
      if (e._state == State.PURSUIT)
      {

        if (Time.time - e._lastSeenTime > 1f || !e._canMove)
        {

          // Check distance
          var dis0 = MathC.Get2DDistance(noisePosition, e.transform.position);
          if (dis0 < minDistance)
          {
            if (Time.time - e._lastHeardTimer > 0.1f)
            {

              // Make sure wasn't too soon or same bullet
              if (bulletID != -1)
              {
                if (e.lastBulletID == bulletID) continue;
                e.lastBulletID = bulletID;
                e._waitTimer = 0f;
              }

              e._lastHeardTimer = Time.time;
              if (!e._targetInFront && !e._canMove)
                e._lastKnownPos = noisePosition;
            }
          }
        }

        continue;
      }

      // Check distance
      var dis = MathC.Get2DDistance(noisePosition, e.transform.position);
      if (dis < minDistance)
      {
        if (Time.time - e._lastHeardTimer > 0.1f || IsLouder(loudness, e._lastLoudness))
        {
          // Make sure wasn't too soon or same bullet
          if (bulletID != -1)
          {
            if (e.lastBulletID == bulletID) continue;
            e.lastBulletID = bulletID;
            e._waitTimer = 0f;
          }

          e._lastLoudness = loudness;
          e._lastHeardTimer = Time.time;
          if (slowReaction)
            e.Suspicious(sourcePosition, loudness);
          else
            e.Suspicious(sourcePosition, loudness, 0.2f * Random.value);
        }
      }
    }
    return;
  }
  static bool IsLouder(Loudness current, Loudness past)
  {
    if (current == Loudness.LOUD)
    {
      switch (past)
      {
        case (Loudness.LOUD):
          return false;
        case (Loudness.NORMAL):
        case (Loudness.SOFT):
        case (Loudness.SUPERSOFT):
          return true;
      }
    }
    else if (current == Loudness.NORMAL)
    {
      switch (past)
      {
        case (Loudness.LOUD):
        case (Loudness.NORMAL):
          return false;
        case (Loudness.SOFT):
        case (Loudness.SUPERSOFT):
          return true;
      }
    }
    else if (current == Loudness.SOFT)
    {
      switch (past)
      {
        case (Loudness.LOUD):
        case (Loudness.NORMAL):
        case (Loudness.SOFT):
          return false;
        case (Loudness.SUPERSOFT):
          return true;
      }
    }
    else if (current == Loudness.SUPERSOFT)
    {
      return false;
    }
    return false;
  }
  #endregion

  public static void Reset()
  {
    _ID = 0;
    _Enemies_alive = new List<EnemyScript>();
    _Enemies_dead = new List<EnemyScript>();
    _Targets = null;
  }
}