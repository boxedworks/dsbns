using UnityEngine;

public class PathScript : MonoBehaviour
{

  public string _waitTimes;
  public string[] _lookTimes;
  public bool _pingPong;

  float[] _waitTimes_parsed;
  float[][] _lookTimes_parsed;

  public int _currentPatrolPoint, _currentLookPoint;

  // Use this for initialization
  public void Init()
  {
    // Parse info
    if (_waitTimes.Trim().Length > 0)
    {
      string[] waitTimes = _waitTimes.Split(',');
      _waitTimes_parsed = new float[waitTimes.Length];
      for (int i = 0; i < waitTimes.Length; i++)
      {
        _waitTimes_parsed[i] = waitTimes[i].ParseFloatInvariant();
      }
    }

    if (_lookTimes.Length > 0)
    {
      _lookTimes_parsed = new float[_lookTimes.Length][];
      for (int u = 0; u < _lookTimes.Length; u++)
      {
        string[] lookTimes = _lookTimes[u].Split(',');
        _lookTimes_parsed[u] = new float[lookTimes.Length];
        for (int i = 0; i < lookTimes.Length; i++)
        {
          _lookTimes_parsed[u][i] = lookTimes[i].ParseFloatInvariant();
        }
      }
    }
    // Hide path markers
    for (int i = 0; i < transform.childCount; i++)
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
    return _pingPong ? Mathf.RoundToInt(Mathf.PingPong(_currentPatrolPoint, transform.childCount - 1)) : _currentPatrolPoint % (transform.childCount);
  }
  public float GetPatrolWait()
  {
    if (_waitTimes_parsed == null) return 2f;
    return _waitTimes_parsed[GetPatrolPointIter()];
  }

  // Look point functions
  public Transform GetLookPoint(bool increment = true)
  {
    Transform p = GetPatrolPoint();
    return p.GetChild((increment ? ++_currentLookPoint : _currentLookPoint) % p.childCount);
  }
  public float GetLookWait()
  {
    if (_lookTimes_parsed == null) return 3f + (Random.value * 3f);
    return _lookTimes_parsed[GetPatrolPointIter()][Mathf.Abs(_currentLookPoint % (_lookTimes_parsed[GetPatrolPointIter()].Length))];
  }

  // Find nearest patrol point to a position
  public Transform GetNearestPatrolPoint(Vector3 currentPos)
  {
    int iter = 0, point = 0;
    float dis = 1000f;
    foreach (Transform t in GetChildren())
    {
      float cDis = Vector3.Distance(t.position, currentPos);
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
    Transform[] children = new Transform[transform.childCount];
    for (int i = 0; i < transform.childCount; i++)
    {
      children[i] = transform.GetChild(i);
    }
    return children;
  }
}
