using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public class EnemyScript : MonoBehaviour
{

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
      _Commands[_Commands_Iter++] = new SpherecastCommand(origin, radius, direction);
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
      _EnemyOrder[_EnemyOrderIter++] = e._id;
    }

    public static RaycastHit GetSpherecastHit(EnemyScript e, int iter)
    {
      var index = _EnemyOrderIter * _NumSpherecasts + iter;
      if (index < _Results.Length)
        return _Results[index];
      return new RaycastHit();
    }
  }

  void ScheduleSpherecasts()
  {
    if (_ragdoll._dead) throw new System.Exception("Ragdoll dead in ScehduleSpherecast");

    // Register to handler to index this._id
    SpherecastHandler.Register(this);

    // Cast at ragdolltarget
    Vector3 target = transform.forward;
    if (_ragdollTarget != null) target = _ragdollTarget._hip.transform.position;
    Vector3 origin = _ragdoll._head.transform.position,
      dir = -MathC.Get2DVector(_ragdoll._hip.transform.position - target).normalized;
    float radius = 0.2f;
    SpherecastHandler.QueueSpherecast(origin, dir, radius);

    // Cast two directions
    Vector3 forward = MathC.Get2DVector(_ragdoll._head.transform.forward).normalized,
      right = MathC.Get2DVector(_ragdoll._head.transform.right).normalized;

    // 1; left
    dir = (forward + right * (0.025f + Mathf.PingPong(Time.time * 4f, 0.9f))).normalized;
    radius = 0.25f;
    SpherecastHandler.QueueSpherecast(origin, dir, radius);

    // 2; right
    dir = (forward - right * (1f - (0.025f + Mathf.PingPong(Time.time * 4f, 0.9f)))).normalized;
    SpherecastHandler.QueueSpherecast(origin, dir, radius);

    // Cast towards lastknownpos
    dir = -MathC.Get2DVector(_ragdoll._head.transform.position - _lastKnownPos).normalized + right * (0.1f + (Mathf.PingPong(Time.time * 1.5f + _id, 2f) - 1f));
    SpherecastHandler.QueueSpherecast(origin, dir, radius);

    // Disable self casting collide
    _ragdoll.ToggleRaycasting(false);
  }

  static public List<EnemyScript> _Enemies_alive, _Enemies_dead;
  public static int _ID;
  public static int _MAX_RAGDOLLS_DEAD = 15, _MAX_RAGDOLLS_ALIVE = 40;

  public int _id;

  ActiveRagdoll _ragdoll;

  public UnityEngine.AI.NavMeshAgent _agent;

  float timer;

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

  public bool _isTarget, // Used for kill the target game mode
    _canAttack;
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

  public DoorScript _linkedDoor;

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

  LineRenderer _lr;
  Vector3 _lr_pos0, _lr_pos1;

  public bool _isZombie { get { return _survivalAttributes != null; } }
  public class SurvivalAttributes
  {
    public GameScript.SurvivalMode.EnemyType _enemyType;

    public int _WaitForEnemyHordeSize, _WaitForEnemyHordeSizeStart;
  }
  public SurvivalAttributes _survivalAttributes;

  // Use this for initialization
  public void Init(SurvivalAttributes survivalAttributes)
  {
    // Set unique ID
    _id = _ID++;
    // Add to list of enemies
    if (_Enemies_alive == null) _Enemies_alive = new List<EnemyScript>();
    _Enemies_alive.Add(this);

    _canAttack = true;

    _startPosition = transform.position;

    lastBulletID = -1;

    _path.Init();
    // Set nearest patrol point
    _path.GetNearestPatrolPoint(transform.position);

    _survivalAttributes = survivalAttributes;
    var isZombie = survivalAttributes != null;

    if (!isZombie)
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
    if (isZombie && GameScript.IsSurvival())
      _agent.agentTypeID = TileManager._navMeshSurface2.agentTypeID;
    else
      _agent.agentTypeID = TileManager._navMeshSurface.agentTypeID;

    // Setup ragdoll
    GameObject ragdollObj = Instantiate(GameResources._Ragdoll);
    ragdollObj.transform.parent = transform.parent;
    ragdollObj.transform.position = transform.position;
    ragdollObj.transform.LookAt(new Vector3(_waitLookPos.x, ragdollObj.transform.position.y, _waitLookPos.z));
    ragdollObj.transform.Rotate(new Vector3(0f, 1f, 0f) * 90f);
    _ragdoll = new ActiveRagdoll(ragdollObj, transform);
    if (_enemyType == EnemyType.NORMAL)
      switch (_itemLeft)
      {
        case (GameScript.ItemManager.Items.KNIFE):
          if (UnityEngine.Random.value < 0.5f)
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
        case (GameScript.ItemManager.Items.ROCKET_LAUNCHER):
          _itemLeft = GameScript.ItemManager.Items.GRENADE_LAUNCHER;
          _ragdoll.ChangeColor(Color.yellow);
          break;
      }

    // Add to targets if is a target
    if (_isTarget)
    {
      if (_Targets == null)
        _Targets = new List<EnemyScript>();
      _Targets.Add(this);
      _ragdoll.ChangeColor(Color.yellow);
    }

    _lr = _ragdoll._head.gameObject.AddComponent<LineRenderer>();
    _lr.startWidth = 0.3f;
    _lr.endWidth = 0f;
    Resources.UnloadAsset(_lr.sharedMaterial);
    _lr.sharedMaterial = GameObject.Find("Blood0").GetComponent<Renderer>().sharedMaterial;
    _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    Gradient gradient = new Gradient();
    gradient.SetKeys(
        new GradientColorKey[] { new GradientColorKey(_ragdoll._color, 0.0f), new GradientColorKey(Color.black, 1.0f) },
        new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 1.0f) }
        );
    _lr.colorGradient = gradient;
    _lr_pos0 = _lr_pos1 = transform.position;
    _lr.positionCount = 2;
    _lr.SetPositions(new Vector3[] { _lr_pos0, _lr_pos1 });

    // Check zombie configs
    if (isZombie)
    {
      // Set target to nearest player
      var data = FunctionsC.GetClosestPlayerTo(transform.position);
      if (data == null || data._ragdoll == null) return;
      ActiveRagdoll closestPlayer = data._ragdoll;
      SetRagdollTarget(closestPlayer);
      SetRandomStrafe();
      _patroling = false;
      _ragdoll.SetActive(true);
      _agent.enabled = true;
      _ragdoll._rotSpeed = PlayerScript.ROTATIONSPEED * (0.8f + Random.value * 0.3f);
      _beganPatrolling = true;
      TargetFound(false, false);
      // Disable line renderer
      _lr.enabled = false;

      // Check armor
      if (_survivalAttributes._enemyType == GameScript.SurvivalMode.EnemyType.ARMORED)
      {
        _ragdoll.GiveArmor();
        _ragdoll._health = 3;
      }
    }

    Walk();
  }

  public ActiveRagdoll GetRagdoll()
  {
    return _ragdoll;
  }

  public static void UpdateEnemies()
  {
    // Check null
    if (_Enemies_alive == null || _Enemies_alive.Count == 0 || GameScript._EditorEnabled) return;
    if (Menu2._InMenus || TileManager._LoadingMap || PlayerScript._Players == null || PlayerScript._Players.Count == 0) return;
    if (GameScript._GameMode != GameScript.GameModes.SURVIVAL || (GameScript._GameMode == GameScript.GameModes.SURVIVAL && GameScript.SurvivalMode._Wave % 5 == 0))
    {
      // Set up handler
      int count = (_Enemies_alive.Count) * SpherecastHandler._NumSpherecasts;
      SpherecastHandler.Init(count);
      // Queury spherecasts
      foreach (EnemyScript e in _Enemies_alive)
        e.ScheduleSpherecasts();
      // Complete all spherecasts
      SpherecastHandler.ScheduleAllSpherecasts();
      // Update with spherecast data
      foreach (EnemyScript e in _Enemies_alive)
      {
        e.Handle();
        SpherecastHandler._EnemyOrderIter++;
      }
      // Clean up data
      SpherecastHandler.Clean();
    }
    else foreach (EnemyScript e in _Enemies_alive)
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
    if (_ragdoll == null || !_ragdoll._canReceiveInput) return;
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
      _agent.enabled = true;
      // Set state and begin patrolling
      _state = State.NEUTRAL;
      _lookAroundT = Time.time;// - 20f;
      _waitTimer = Time.time - 20f;
      BeginPatrol();
    }
    if (!_agent.enabled) return;
    // Update line renderer under enemy
    if (!_ragdoll._dead)
    {
      _ragdoll.ToggleRaycasting(true);

      _lr_pos0 = _ragdoll._head.gameObject.transform.position;
      _lr_pos1 = _lr_pos0 + _ragdoll._hip.transform.forward * 3f;
      Vector3 cPos1 = _lr.GetPosition(1);
      cPos1 += (_lr_pos1 - cPos1) * Time.deltaTime * 5f;
      Vector3 offset = -new Vector3(0f, 1f, 0f);
      cPos1 = new Vector3(cPos1.x, _lr_pos0.y, cPos1.z);
      if (Mathf.Abs((_lr_pos1 - _lr_pos0).magnitude) > 2f)
      {
        Vector3 dir = (cPos1 - _lr_pos0);
        cPos1 = _lr_pos0 + dir.normalized * 2f;
      }
      _lr.SetPositions(new Vector3[] { _lr_pos0 + offset, cPos1 + offset });
    }
    var _lookAtPos = Vector3.zero;

    bool saveWait = waiting;

    if (_ragdoll.Active())
    {
      if (!_isZombie)
        _ragdoll._rotSpeed = (_state == State.PURSUIT ? _targetDirectlyInFront ? 1.3f : 1f : 0.8f) * PlayerScript.ROTATIONSPEED;
      // If chaser, check if enabled
      if (!_canAttack && IsChaser() && !_isZombie)
      {
        if (Time.time - GameScript._LevelStartTime > 0.5f)
        {
          var info = FunctionsC.GetFatherstPlayerFrom(transform.position);
          if (info != null && info._distance > 3f + (GameScript.Settings._NumberPlayers > 1 ? (1.5f * Mathf.Clamp(GameScript.Settings._NumberPlayers, 0, 2) - 1) : 0f))
          {
            _canAttack = true;
            _canMove = true;
            SetRagdollTarget(FunctionsC.GetClosestPlayerTo(transform.position)._ragdoll);
            TargetFound();
          }
          else
          {
            var close_data = FunctionsC.GetClosestPlayerTo(transform.position);
            if (close_data._ragdoll != null)
            {
              _lookAtPos = (close_data._ragdoll.transform.position);
              LookAt(_lookAtPos);
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
        _lookAtPos = _waitLookPos;
        LookAt(_lookAtPos);
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
          if (!_isZombie)
          {
            // Update patrol
            if (_patroling && !IsChaser())
            {
              float dis = MathC.Get2DDistance(transform.position, _path.GetPatrolPoint().position);
              _atd = dis;
              if (dis < 0.5f)
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
          float dis = MathC.Get2DDistance(transform.position, _lastKnownPos);
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
          if (GameScript._Singleton._GameEnded)
          {
          }
          else if (_ragdollTarget != null && _ragdollTarget._invisible && !IsChaser() && _targetInLOS)
          {
            ChangeState(State.SEARCHING);
            _lastKnownPos = _ragdollTarget._hip.position;
            _agent.SetDestination(_lastKnownPos);

            _time_lost = 0f;
            _searchDir = -MathC.Get2DVector((transform.position - _lastKnownPos)).normalized * 3f;
          }
          // Check for weapons
          else if (_ragdollTarget != null)
          {
            if (CheckShouldPanic())
              Panic();
            // Chase player
            else
            {
              // Check if close to steering pos; try to look around corner before going around corner
              if (_canMove)
              {
                if (MathC.Get2DDistance(_ragdoll._hip.position, _agent.steeringTarget) < 3f)
                {
                  int iter = 0;
                  if (_agent.path.corners.Length > 0 && !_agent.path.corners[_agent.path.corners.Length - 1].Equals(_agent.steeringTarget))
                    foreach (Vector3 p in _agent.path.corners)
                    {
                      if (p.Equals(_agent.steeringTarget))
                      {
                        Vector3 nextPos = _agent.path.corners[iter + 1];
                        if (Vector3.Distance(nextPos, _agent.steeringTarget) < 2.5f && iter + 2 < _agent.path.corners.Length - 1)
                          nextPos = _agent.path.corners[iter + 2];
                        _lookAtPos = nextPos;
                      }
                      iter++;
                    }
                  //Debug.DrawLine(transform.position, _agent.steeringTarget, Color.red);
                  //Debug.DrawLine(transform.position, _lastKnownPos, Color.blue);
                }
              }
              if (_targetInLOS)
              {
                float dis = MathC.Get2DDistance(_ragdoll._hip.position, _ragdollTarget._hip.position);
                // If can't move, has gun, and player gets too close, start to chase
                if (!_canMove)
                {
                  if (_ragdoll.HasGun() && _targetInLOS && dis < 1.5f)
                    _canMove = true;
                }
                // Check to chase player if has exit
                if (!_canMove && _ragdollTarget._playerScript._hasExit)
                {
                  _sawWithGoal = true;
                  if (dis > 13f)
                    _canMove = true;
                }
                // If has gun, keep distance to shoot
                if (_ragdoll.HasGun())
                {
                  if (_targetInFront)
                    _lookAtPos = new Vector3(_ragdollTarget._hip.position.x, transform.position.y, _ragdollTarget._hip.position.z);
                  // Check if the enemy is at the right distance to shoot
                  if (_canMove)
                  {
                    float close = 3.5f, far = 10f;
                    // If player has the exit, chase closer
                    if (_ragdollTarget._isPlayer && _ragdollTarget._playerScript._hasExit)
                    {
                      close = 3f;
                      far = 4f;
                    }
                    // Move further back if reloading
                    if (_ragdoll._reloading)
                    {
                      close += 1.5f;
                      far += 1.5f;
                    }
                    // Keep distance per close and far values
                    if (dis > close && dis < far)
                      _agent.SetDestination(transform.position);
                    // If not in distance, move into distance
                    else if (dis >= far)
                      ChaseTarget(false);
                    else
                    {
                      UnityEngine.AI.NavMeshHit hit;
                      Vector3 pos = transform.position + (_ragdoll._hip.position - _ragdollTarget._hip.position).normalized * 5f;
                      if (UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 4f, UnityEngine.AI.NavMesh.AllAreas))
                      {
                        var save_destinatioin = _agent.destination;
                        var save_distance = FunctionsC.GetPathLength(_agent.path.corners);

                        var path = new UnityEngine.AI.NavMeshPath();

                        if (UnityEngine.AI.NavMesh.CalculatePath(transform.position, pos, UnityEngine.AI.NavMesh.AllAreas, path))
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
                else ChaseTarget();
                // Try attacking
                if (Time.time - _attackTime > 0f && _canAttack)
                {
                  // If has a melee weapon and sees the target, run at them
                  if (!_ragdoll.HasGun() && _time_seen > 0.05f && _targetInLOS && _enemyType != EnemyType.ROBOT && !_isZombie)
                    _moveSpeed = PlayerScript.RUNSPEED;
                  // Only attack if is alive, the target is alive, and (the target is in front, or has a machine gun, or has a melee weapon)
                  if (!_ragdoll._dead && !_ragdollTarget._dead && (_targetDirectlyInFront || HasMachineGun() || !_ragdoll.HasGun()))
                  {
                    ItemScript useitem = (_leftweaponuse ? _ragdoll._itemL : (_ragdoll._itemR != null ? _ragdoll._itemR : _ragdoll._itemL));
                    // Check for reload
                    if (_ragdoll.HasGun() && useitem.NeedsReload())
                    {
                      _ragdoll.Reload();
                      _attackTime = Time.time + (0.5f + Random.value * 0.5f);
                    }
                    // Attack if close enough or pointed at target
                    else if ((_ragdoll.HasGun()) || (!_ragdoll.HasGun() && dis < (_itemLeft == GameScript.ItemManager.Items.GRENADE_HOLD ? 1f : _itemLeft == GameScript.ItemManager.Items.BAT ? 1.2f : 1.4f)))
                    {
                      UseItem();
                      if (HasMachineGun())
                        _attackTime = Time.time + useitem.UseRate();
                      else
                        _attackTime = Time.time + (0.2f + Random.value * (_ragdoll.HasSilencedWeapon() || _itemLeft == GameScript.ItemManager.Items.REVOLVER ? 0.1f : 0.5f));
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
                  _lookAtPos = _lastKnownPos;
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
          _lookAtPos = _lastKnownPos;
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
        MoveRotateTransform(_lookAtPos);
        // Check if stuck and attempt to fix
        if (_ragdoll.HasGun() && _targetInLOS) { }
        else if (!_agent.hasPath && _state != State.NEUTRAL && _canMove)
        {
          _stuckIter++;
          if (_stuckIter > 20)
          {
            if (_isZombie) { /*Debug.Log("stuck");*/ }
            else if (IsChaser())
              _agent.SetDestination(_ragdollTarget._hip.position);
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
      if (Time.time - GameScript._LevelStartTime > 0.25f && !TileManager.Tile._Moving)
        Raycast();
    }
    _ragdoll.Update();
  }

  bool _leftweaponuse = true;
  // Alternate weapons if has multiple
  void UseItem()
  {
    _leftweaponuse = !_leftweaponuse;
    if (_ragdoll._itemL == null && _ragdoll._itemR == null) { return; }
    else if (_ragdoll._itemL == null && _ragdoll._itemR != null) { _ragdoll.UseRight(); }
    else if (_ragdoll._itemL != null && _ragdoll._itemR == null) { _ragdoll.UseLeft(); }
    else if (_leftweaponuse)
    {
      _ragdoll.UseLeft();
    }
    else
    {
      _ragdoll.UseRight();
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
    var movePos = (_agent.steeringTarget - transform.position).normalized * PlayerScript.MOVESPEED * Time.deltaTime * _moveSpeed_lerped;
    //Debug.Log(MathC.Get2DVector(movePos).magnitude + " : " + (Vector3.Equals(_agent.destination, transform.position)));

    if (Vector3.Equals(_agent.destination, transform.position))
      movePos = Vector3.zero;
    _agent.Move(movePos);
  }
  void LookAt(Vector3 lookAtPos)
  {
    if (_ragdoll._grappled) return;

    var forward = transform.forward;
    transform.LookAt(lookAtPos == Vector3.zero ? new Vector3(_agent.steeringTarget.x, transform.position.y, _agent.steeringTarget.z) : lookAtPos);
    if (_isZombie) return;
    var val = Mathf.Clamp(Mathf.Abs((transform.forward - forward).magnitude), 0f, 10f);
    if (val < 0.05f || val > 1f) val = 0f;
    _moveSpeed_lerped = Mathf.Clamp(_moveSpeed_lerped - val * Time.deltaTime * 15f, 0f, 10f);
  }

  // Go through "Enemies" Transform children and init
  public static void HardInitAll()
  {
    var enemies = GameScript._Singleton.transform.GetChild(0);
    for (var i = 0; i < enemies.childCount; i++)
    {
      var e = enemies.GetChild(i).GetChild(0).GetComponent<EnemyScript>();
      e.Init(null);
      e.EquipStart();
    }
  }
  public static IEnumerator HardInitAllCo()
  {
    var enemies = GameScript._Singleton.transform.GetChild(0);
    for (var i = 0; i < enemies.childCount; i++)
    {
      var e = enemies.GetChild(i).GetChild(0).GetComponent<EnemyScript>();
      e.Init(null);
      e.EquipStart();
      if (i % 3 == 0) yield return new WaitForSeconds(0.01f);
    }
  }

  public static int NumberAlive()
  {
    if (_Enemies_alive == null) return 0;
    return Mathf.Abs(_Enemies_alive.Count);
  }

  float _DIS;
  public static int _RAYCOUNT;
  void Raycast()
  {
    if (_isZombie)
    {
      var olddis = _DIS;
      var targ = _ragdollTarget;
      var closestPlayer = FunctionsC.GetClosestPlayerTo(_ragdoll._hip.position);
      if (closestPlayer._distance < olddis && targ._id != closestPlayer._ragdoll._id)
        SetRagdollTarget(closestPlayer._ragdoll);
      _DIS = closestPlayer._distance;
    }
    else _DIS = FunctionsC.GetClosestPlayerTo(_ragdoll._hip.position)._distance;
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
        if (!IsChaser() && !_isZombie) _time_lost += Time.deltaTime;
        else _time_lost = 0f;
        if (_time_lost > 12f)
          Walk();
        else if (_state != State.SEARCHING && _time_lost > 11f)
        {
          ChangeState(State.SEARCHING);
          _agent.SetDestination(_lastKnownPos);
        }
        // Reset attack time
        else if (_time_lost > 2f)
          _attackTime = Time.time + 0.2f;
        // Check if target is in LOS
        bool found = false;
        Vector3 dirToTarget = -MathC.Get2DVector(_ragdoll._hip.transform.position - _ragdollTarget._hip.transform.position).normalized;
        if (_isZombie && _survivalAttributes._enemyType != GameScript.SurvivalMode.EnemyType.PISTOL_WALK)
        {
          found = true;
        }
        else
        {
          if (_DIS < 0.25f)
            found = true;
          else
          {
            h = SpherecastHandler.GetSpherecastHit(this, 0);
            if (h.collider != null)
              found = CheckRay(h);
          }
        }
        if (_ragdollTarget == null || _ragdollTarget._dead)
        {
          _targetInLOS = false;
          _targetDirectlyInFront = false;
          _targetInFront = false;
          // If ischaser and there are more players, chase them
          if (IsChaser() || _isZombie)
          {
            var info = FunctionsC.GetClosestPlayerTo(transform.position);
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
          _time_seen += Time.deltaTime;
          _time_lost = 0f;
          _targetInLOS = true;
          float lookMagnitude = (dirToTarget - MathC.Get2DVector(_ragdoll._head.transform.forward)).magnitude;
          if (lookMagnitude < 1.4f)
          {
            _targetInFront = true;
            if (lookMagnitude < (_ragdoll.HasGun() ? 0.3f : ((_ragdoll._itemL != null && _ragdoll._itemL.IsThrowable()) ? 0.4f : 0.2f)))
              _targetDirectlyInFront = true;
            else
              _targetDirectlyInFront = false;
          }
          else _targetInFront = false;
        }
      }
      // Check for things in LOS
      if (_isZombie && _survivalAttributes._enemyType != GameScript.SurvivalMode.EnemyType.PISTOL_WALK) return;
      {
        bool hit = false;
        h = SpherecastHandler.GetSpherecastHit(this, 1);
        //Debug.DrawLine(transform.position, h.point);
        if (!hit && h.collider != null)
          hit = CheckRay(h);
        h = SpherecastHandler.GetSpherecastHit(this, 2);
        //Debug.DrawLine(transform.position, h.point);
        if (!hit && h.collider != null)
          CheckRay(h);
      }
      // Ray towards lastknownpos
      if (_canMove)
        if (_state == State.SEARCHING || _state == State.SUSPICIOUS || _suspiciousTimer > 0f)
        {
          h = SpherecastHandler.GetSpherecastHit(this, 3);
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
    if (Time.time - GameScript._LevelStartTime < 1f) return;
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
        var dirToTarget = -MathC.Get2DVector(_ragdoll._head.transform.position - _ragdollTarget._hip.transform.position).normalized;
        var lookMagnitude = (dirToTarget - MathC.Get2DVector(_ragdoll._head.transform.forward)).magnitude;
        if (lookMagnitude < minVal)
          c = true;
      }
      // Check if should alert self
      if (!frontMagnitudeCheck || (frontMagnitudeCheck && c))
      {
        if (_ragdollTarget._dead)
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
    if (rag != null && !rag.IsSelf(_ragdoll._hip.gameObject) && !rag._isPlayer)
    {
      var e = rag._controller.GetComponent<EnemyScript>();
      // Delayed absorb
      if (GameScript.Settings._DIFFICULTY > 0)
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
      if (_state != State.NEUTRAL && h.distance < 10f && _canMove && !e._ragdoll._dead)
      {
        //rag._enemyScript.AbsorbInfo(this);
        if (h.distance < 2f && !rag._dead)
          _agent.Move((_strafeRight ? transform.right : -transform.right) * 0.6f * Time.deltaTime * _moveSpeed_lerped);
      }
    }
    return false;
  }

  /*Coroutine _delayedAbsorb;
  IEnumerator DelayedAbsorb(EnemyScript s, float waitTime, RaycastHit h)
  {
    yield return new WaitForSeconds(waitTime);
    if (!_ragdoll._dead)
      AbsorbInfo(s);
    _delayedAbsorb = null;
  }*/

  #region Patrol Functions
  void SetCurrentPatrolPoint()
  {
    Vector3 p = _path.GetPatrolPoint().position;
    if (MathC.Get2DDistance(p, transform.position) == 0f) return;
    _agent.SetDestination(p);
  }
  void SetNextPatrolPoint()
  {
    Vector3 newPos = _path.GetNextPatrolPoint().position;
    _agent.SetDestination(newPos);
  }

  void SetAgentDestination(Vector3 newDest)
  {
    _agent.SetDestination(newDest);
  }

  void ChaseTarget(bool checkTurnaround = true)
  {
    //if (_isZombie) return;
    if (_chasingTarget || _ragdollTarget._dead || !_canMove) return;
    StartCoroutine(ChaseTargetCo(checkTurnaround));
  }

  float _zombie_chaseTimer;
  bool _chasingTarget = false;
  IEnumerator ChaseTargetCo(bool checkTurnaround)
  {
    _chasingTarget = true;
    //if(_targetInLOS && _time_lost < 10f)
    _lastKnownPos = _ragdollTarget._controller.position;
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
        var filter = new UnityEngine.AI.NavMeshQueryFilter();
        filter.areaMask = 1;
        filter.agentTypeID = (_isZombie && GameScript.IsSurvival() ? TileManager._navMeshSurface2.agentTypeID : TileManager._navMeshSurface.agentTypeID);
        if (!UnityEngine.AI.NavMesh.CalculatePath(transform.position, _lastKnownPos, filter, path_new)) Debug.LogError("Failed to find path");
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
    yield return new WaitForSeconds(0.1f + Random.value * 0.05f);
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
    if (_ragdoll._id == ragdoll._id) return;
    // Check for same setting
    if (_ragdollTarget != null && _ragdollTarget._id == ragdoll._id) return;
    _ragdollTarget = ragdoll;
  }

  #region State Change Functions
  // Fired when player is first found
  public void TargetFound(bool run = true, bool check_panic = true)
  {
    _lastKnownPos = _ragdollTarget._controller.position;
    _lastSeenTime = Time.time;
    // Make sure not already pursuing
    if (_state == State.PURSUIT || _state == State.PANICKED) return;
    if (_ragdoll._itemL != null && (_ragdoll._itemL.IsThrowable() && _canMove)) Scream();
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
    _attackTime = Time.time + 0.2f;
  }

  public void SetRandomStrafe()
  {
    _strafeRight = (Mathf.RoundToInt(Random.value) == 0 ? true : false);
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
    if (Time.time - GameScript._LevelStartTime < 0.5f || TileManager.Tile._Moving) return;
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
    if (!_agent.enabled || _ragdoll._dead) { }
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

  void Alert(Vector3 lastKnownPos, float lostTime = 0f)
  {
    _lastKnownPos = lastKnownPos;
    _time_lost = lostTime;
    _waitAmount = 0f;
    _waitTimer = Time.time - 2f;
    _patroling = false;
  }

  // Change movement speed
  void Walk()
  {
    if (_isZombie)
    {
      return;
    }
    _moveSpeed = 0.5f;
  }
  void Run()
  {
    if (_isZombie) return;
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
      if (!_isZombie)
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
    if (newState == State.PANICKED && !_ragdoll._dead)
      Scream();

    _state = newState;
  }

  float _lastScreamTime;
  void Scream()
  {
    return;
    if (Time.time - _lastScreamTime < 1f) return;
    _lastScreamTime = Time.time;
    int r = Mathf.RoundToInt(Random.value * 4f);
    _ragdoll.PlaySound("Enemies/Scream" + (r), 0.9f, 1.1f);
  }

  public ActiveRagdoll IsTarget(GameObject obj)
  {
    if (PlayerScript._Players == null) return null;
    foreach (PlayerScript p in PlayerScript._Players)
    {
      if (!p._canDetect || p._ragdoll._dead) continue;
      if (p._ragdoll.IsSelf(obj)) return p._ragdoll;
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
    Transform p = _path.GetLookPoint(false);
    _waitLookPos = MathC.Get2DVector(p.position) + new Vector3(0f, transform.position.y, 0f);
  }
  #endregion

  public static EnemyScript LoadEnemy(Vector3 position)
  {
    GameObject new_gameobject = GameObject.Instantiate(GameResources._Enemy);
    new_gameobject.transform.parent = GameObject.Find("Game").transform.GetChild(0);
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
  }

  public static bool AllDead()
  {
    if (_Enemies_alive == null) return true;
    return _Enemies_alive.Count == 0;
  }

  public void OnToggle(ActiveRagdoll source, ActiveRagdoll.DamageSourceType damageSourceType)
  {
    // Check achievement
    if (source != null && source._isPlayer)
    {

#if UNITY_STANDALONE
      if (_Enemies_dead == null || _Enemies_dead.Count == 0)
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.KILL);
#endif

      // Save stats
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
      }
    }

    _linkedDoor?.OnEnemyDie(this);

    _ragdoll.StopSound();
    if (Time.time - _ragdoll._lastBubbleScriptTime < 0.4f) _ragdoll.DisplayText("");

    // Check is target
    if (_isTarget)
      GameScript.KillTarget(this);

    // Remove line renderers
    _lr.positionCount = 0;
    _lr.SetPositions(new Vector3[] { });

    // Swap contianers
    _Enemies_alive.Remove(this);
    if (_Enemies_dead == null) _Enemies_dead = new List<EnemyScript>();
    _Enemies_dead.Add(this);
    if (_Enemies_dead.Count > _MAX_RAGDOLLS_DEAD)
    {
      EnemyScript e = _Enemies_dead[0];
      ActiveRagdoll._Ragdolls.Remove(e._ragdoll);
      _Enemies_dead.Remove(e);
      GameObject.Destroy(e.transform.parent.gameObject);
    }

    // Check to see if #bodies is too much
    if (_Enemies_dead.Count > 8)
      foreach (EnemyScript e in _Enemies_dead)
        if (e._ragdoll._dead && !e._ragdoll._disabled)
        {
          e._ragdoll.Disable();
          break;
        }

    // Check for last enemy killed
    var last_killed = false;
    if (_Enemies_alive.Count == 0)
      GameScript._Singleton._goalPickupTime = Time.time;

    // Sneaky difficulty
    if (GameScript.Settings._DIFFICULTY == 0)
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
      {
        last_killed = true;
      }
    }

    // Slowmo
    if (last_killed)
    {
      // Check for slowmo setting
      if (GameScript.Settings._Slowmo_on_lastkill)
        PlayerScript._SlowmoTimer += 1.3f;

      // Check mode
      if (GameScript._GameMode == GameScript.GameModes.SURVIVAL)
      {
        if (source._isPlayer)
          GameScript.SurvivalMode.GivePoints(source._playerScript._id, 5 * (GameScript.SurvivalMode._Wave), true);
      }
      else
      {
        // Make goal bigger
        if (!PlayerScript.HasExit() && Powerup._Powerups != null && Powerup._Powerups.Count > 0)
          Powerup._Powerups[0].transform.GetChild(0).GetComponent<BoxCollider>().size *= 3f;
      }
    }

    // Check for ischaser
    if (_Chaser != null && !_Chaser._canMove)
    {
      //Debug.Log("Chaser activated: kill");
      _Chaser._canAttack = true;
      _Chaser._canMove = true;
      var playerInfo = FunctionsC.GetClosestPlayerTo(transform.position);
      if (playerInfo._ragdoll == null || playerInfo._ragdoll == null) return;
      _Chaser.SetRagdollTarget(playerInfo._ragdoll);
      _Chaser.TargetFound();
      _Chaser = null;
    }

    // Increment survival score
    if (GameScript._GameMode == GameScript.GameModes.SURVIVAL && source._isPlayer)
      GameScript.SurvivalMode.IncrementScore(source._playerScript._id);
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

  public static EnemyScript SpawnEnemyAt(SurvivalAttributes survivalAttributes, Vector2 location)
  {
    var weapon = "knife";
    if (survivalAttributes._enemyType == GameScript.SurvivalMode.EnemyType.GRENADE_JOG)
      weapon = "grenade";
    else if (survivalAttributes._enemyType == GameScript.SurvivalMode.EnemyType.PISTOL_WALK)
      weapon = "pistol";
    GameObject enemy = TileManager.LoadObject($"e_0_0_li_{weapon}_");
    EnemyScript e = enemy.transform.GetChild(0).GetComponent<EnemyScript>();
    e.transform.position = new Vector3(location.x, enemy.transform.position.y, location.y);
    e.LookAt(PlayerScript._Players[0].transform.position);
    e.Init(survivalAttributes);
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
  public static EnemyScript[] CheckSound(Vector3 position, Loudness loudness, int bulletID = -1, bool setSuspicious = true)
  {
    return CheckSound(position, position, loudness, bulletID, setSuspicious);
  }
  /// <summary>
  /// Noise of volume loudness is produced a position noisePosition.
  /// If an enemy is within range of noisePosition, they are suspicious at position sourcePosition
  /// </summary>
  public static EnemyScript[] CheckSound(Vector3 noisePosition, Vector3 sourcePosition, Loudness loudness, int bulletID = -1, bool setSuspicious = true)
  {
    if (_Enemies_alive == null || GameScript.IsSurvival()) return new EnemyScript[0];
    // Decide distance
    float minDistance = (loudness == Loudness.SUPERSOFT ? 1.5f : (loudness == Loudness.SOFT ? 3.5f : (loudness == Loudness.NORMAL ? 7f : 10f)));
    // Check each enemy
    var enemies = new List<EnemyScript>();
    foreach (var e in _Enemies_alive)
    {
      // If the ragdoll is dead continue
      if (e._ragdoll._dead || !e._reactToSound || e._isZombie) continue;

      // If ragdoll is chasing, continue
      if (e._state == State.PURSUIT) continue;

      // Check distance
      float dis = MathC.Get2DDistance(noisePosition, e.transform.position);
      if (dis < minDistance)
      {
        // Make sure wasn't too soon or same bullet
        if (bulletID != -1)
        {
          if (e.lastBulletID == bulletID) continue;
          e.lastBulletID = bulletID;
          e._waitTimer = 0f;
        }
        if (Time.time - e._lastHeardTimer > 0.1f || EnemyScript.IsLouder(loudness, e._lastLoudness))
        {
          e._lastLoudness = loudness;
          e._lastHeardTimer = Time.time;
          if (setSuspicious)
          {
            e.Suspicious(sourcePosition, loudness);
          }
          enemies.Add(e);
        }
      }
    }
    return enemies.ToArray();
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
    _Enemies_alive = null;
    _Enemies_dead = null;
    _Targets = null;
  }
}