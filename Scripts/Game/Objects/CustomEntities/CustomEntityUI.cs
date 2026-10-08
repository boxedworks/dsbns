using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Objects.CustomEntities
{
  public class CustomEntityUI : MonoBehaviour
  {

    public CustomEntity[] _activate;

    public Color[] _colors;
    int _colorIter;

    public Light _light;
    public bool _activated;

    MeshRenderer _renderer;
    static List<Material> s_Materials_CE;

    static int s_Id;
    int _id;

    public static void ResetMaterialIndex()
    {
      s_Id = 0;
    }

    // Use this for initialization
    void Start()
    {
      _id = s_Id++;

      _renderer = GetComponent<MeshRenderer>();
      s_Materials_CE ??= new();
      if (s_Materials_CE.Count == _id)
        s_Materials_CE.Add(new Material(_renderer.sharedMaterial));
      _renderer.sharedMaterial = s_Materials_CE[_id];

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
      _renderer.sharedMaterial.SetColor("_EmissionColor", c);
      _light.color = c;
    }

    public void Activate()
    {
      if (_activate == null || _activated) return;
      _activated = true;
      foreach (var c in _activate)
      {
        c.Activate(this);
      }
      if (_colors != null && _colors.Length > 0)
      {
        var c = _colors[++_colorIter % _colors.Length];
        ChangeColor(c);
      }
    }

    // Adds a new entity to the _activate array
    public void AddToActivateArray(CustomEntity entity)
    {
      foreach (var en in _activate)
      {
        if (entity.GetEntityId() == en.GetEntityId())
        {
          Debug.Log("Attempting to add duplicate to array");
          return;
        }
      }
      System.Array.Resize(ref _activate, _activate.Length + 1);
      _activate[^1] = entity;
    }
  }
}