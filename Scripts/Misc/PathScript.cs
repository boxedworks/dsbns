using UnityEngine;

public class PathScript
{

  public Transform transform;
  public GameObject gameObject { get { return transform.gameObject; } }

  public string _WaitTimes;
  public string[] _LookTimes;
  bool _pingPong;

  float[] _waitTimes_parsed;
  float[][] _lookTimes_parsed;

  public int _currentPatrolPoint, _currentLookPoint;

  // Use this for initialization
  public void Init(Transform pathTransform)
  {
    transform = pathTransform;

    //_pingPong = true;

    // Parse info
    if (_WaitTimes != null && _WaitTimes.Trim().Length > 0)
    {
      var waitTimes = _WaitTimes.Split(',');
      _waitTimes_parsed = new float[waitTimes.Length];
      for (var i = 0; i < waitTimes.Length; i++)
      {
        _waitTimes_parsed[i] = waitTimes[i].ParseFloatInvariant();
      }
    }

    if (_LookTimes != null && _LookTimes.Length > 0)
    {
      _lookTimes_parsed = new float[_LookTimes.Length][];
      for (int u = 0; u < _LookTimes.Length; u++)
      {
        var lookTimes = _LookTimes[u].Split(',');
        _lookTimes_parsed[u] = new float[lookTimes.Length];
        for (var i = 0; i < lookTimes.Length; i++)
        {
          _lookTimes_parsed[u][i] = lookTimes[i].ParseFloatInvariant();
        }
      }
    }

    // Hide path markers
    for (var i = 0; i < transform.childCount; i++)
    {
      transform.GetChild(i).gameObject.SetActive(false);
    }
  }

  public int GetPathLength()
  {
    return transform.childCount;
  }
  // Patrol point functions
  public Transform GetPatrolPoint()
  {
    if (transform.childCount == 1)
      return transform.GetChild(0);
    return transform.GetChild(GetPatrolPointIter());
  }
  public Transform GetNextPatrolPoint()
  {
    _currentPatrolPoint++;
    _currentLookPoint = -1;
    return GetPatrolPoint();
  }
  int GetPatrolPointIter()
  {
    return _pingPong ? Mathf.RoundToInt(Mathf.PingPong(_currentPatrolPoint, transform.childCount - 1)) : _currentPatrolPoint % transform.childCount;
  }
  public float GetPatrolWait()
  {
    if (_waitTimes_parsed == null) return 2f;
    return _waitTimes_parsed[GetPatrolPointIter()];
  }

  // Look point functions
  public Transform GetLookPoint(bool increment = true)
  {
    var p = GetPatrolPoint();
    return p.GetChild((increment ? ++_currentLookPoint : _currentLookPoint) % p.childCount);
  }
  public float GetLookWait()
  {
    if (_lookTimes_parsed == null) return 3f + (Random.value * 3f);
    return _lookTimes_parsed[GetPatrolPointIter()][Mathf.Abs(_currentLookPoint % _lookTimes_parsed[GetPatrolPointIter()].Length)];
  }

  // Find nearest patrol point to a position
  public Transform GetNearestPatrolPoint(Vector3 currentPos)
  {
    int iter = 0, point = 0;
    var dis = 1000f;
    foreach (var t in GetChildren())
    {
      var cDis = Vector3.Distance(t.position, currentPos);
      if (cDis < dis)
      {
        dis = cDis;
        point = iter;
      }
      iter++;
    }
    _currentPatrolPoint = point + 1;
    return GetPatrolPoint();
  }

  Transform[] GetChildren()
  {
    if (transform.childCount == 0) return null;
    var children = new Transform[transform.childCount];
    for (var i = 0; i < transform.childCount; i++)
    {
      children[i] = transform.GetChild(i);
    }
    return children;
  }
}
