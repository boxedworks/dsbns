using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomEntityUI : MonoBehaviour {

  public CustomEntity[] _activate;

  public Color[] _colors;
  int _colorIter;

  public Light _light;
  public bool _activated;

  MeshRenderer _renderer;
  static List<Material> _Materials_CE;

  public static int _ID;
  int _id;

	// Use this for initialization
	void Start () {
    _id = _ID++;

    _renderer = GetComponent<MeshRenderer>();
    if (_Materials_CE == null) _Materials_CE = new List<Material>();
    if (_Materials_CE.Count == _id)
      _Materials_CE.Add(new Material(_renderer.sharedMaterial));
    _renderer.sharedMaterial = _Materials_CE[_id];

    if (_Materials_CE == null) _Materials_CE = new List<Material>();

    if (_colors != null && _colors.Length > 0)
    {
      _light = transform.GetChild(0).gameObject.GetComponent<Light>();
      ChangeColor(_colors[0]);
    }
	}

  public void Reset()
  {
    if (_colors != null && _colors.Length > 0)
      ChangeColor(_colors[0]);
    _colorIter = 0;
    _activated = false;
  }

  // Lerp mesh color to new color
  public void ChangeColor(Color c)
  {
    if (Color.Equals(_renderer.sharedMaterial.color, c)) return;
    StopAllCoroutines();
    StartCoroutine(ChangeColorCo(c, 0.3f));
  }
  IEnumerator ChangeColorCo(Color newColor, float time)
  {
    var currentColor = _renderer.sharedMaterial.color;
    var saveTime = time;
    while (time > 0f)
    {
      _renderer.sharedMaterial.color = Color.Lerp(currentColor, newColor, 1f - time / saveTime);
      _light.color = Color.Lerp(currentColor, newColor, 1f - time / saveTime);
      time -= 0.05f;
      yield return new WaitForSeconds(0.05f);
    }
    _renderer.sharedMaterial.color = newColor;
  }

  public void Activate()
  {
    if (_activate == null || _activated) return;
    _activated = true;
    foreach(var c in _activate)
    {
      c.Activate(this);
    }
    if (_colors != null && _colors.Length > 0) {
      var c = _colors[++_colorIter % _colors.Length];
      ChangeColor(c);
    }
  }

  // Adds a new entity to the _activate array
  public void AddToActivateArray(CustomEntity entity)
  {
    foreach (var en in _activate)
    {
      if (entity.GetInstanceID() == en.GetInstanceID())
      {
        Debug.Log("Attempting to add duplicate to array");
        return;
      }
    }
    System.Array.Resize<CustomEntity>(ref _activate, _activate.Length + 1);
    _activate[_activate.Length - 1] = entity;
  }
}
