using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ProgressBar
{
  static instance[] _ProgressBars;

  public class instance
  {
    public static int _ID;

    public int _id;
    public Transform _transform, _slider, _parent;
    public float _timer, _timer_start;
    public System.Action _timerAction;
    public System.Action<instance> _updateAction;
    public bool _enabled;
  }
  static int _PoolIter;

  public static void Init()
  {
    // Populate pool of instances
    _ProgressBars = new instance[10];
    instance._ID = 1;
    for (var i = 0; i < _ProgressBars.Length; i++)
    {
      _ProgressBars[i] = new instance();
      _ProgressBars[i]._transform = (GameObject.Instantiate(Resources.Load("ProgressBar")) as GameObject).transform;
      _ProgressBars[i]._transform.position = new Vector3(1000f, 0f, 0f);
      _ProgressBars[i]._slider = _ProgressBars[i]._transform.GetChild(0);
    }
  }

  // Update is called once per frame
  public static void Update()
  {
    // Update each progress bar with it's parent's position
    foreach(var progress_bar in _ProgressBars)
    {
      var p = progress_bar._parent;

      // If no parent, move offscreen
      if (p == null || !progress_bar._enabled || GameScript._Paused)
      {
        progress_bar._transform.position = new Vector3(1000f, 0f, 1000f);
        continue;
      }
      progress_bar._updateAction?.Invoke(progress_bar);

      // Else, update to parent's pos
      progress_bar._transform.position = progress_bar._parent.position + new Vector3(0f, 0.2f, 0.2f);

      // Increment timer and update slider position
      progress_bar._timer -= Time.deltaTime;
      progress_bar._slider.localPosition = new Vector3(0.47f + (-0.47f * 2f * (progress_bar._timer / progress_bar._timer_start)), 0f, 0f);

      // If timer is up, set parent to null and fire action
      if(progress_bar._timer <= 0f)
      {
        progress_bar._parent = null;
        progress_bar._enabled = false;
        progress_bar._timerAction.Invoke();
      }
    }
  }

  public static void GetProgressBar(Transform parent, float time, System.Action timerAction, System.Action<instance> updateAction = null)
  {
    var set_progressbar = _ProgressBars[_PoolIter++ % _ProgressBars.Length];

    set_progressbar._id = instance._ID++;
    set_progressbar._parent = parent;
    set_progressbar._enabled = true;
    set_progressbar._timer_start = set_progressbar._timer = time;
    set_progressbar._timerAction = timerAction;
    set_progressbar._updateAction = updateAction;
  }
}