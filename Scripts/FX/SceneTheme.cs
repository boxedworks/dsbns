using UnityEngine;

namespace Assets.Scripts.FX
{

  [System.Serializable]
  public class SceneTheme
  {
    public string _name,
      _tile;
    public Color _tileColorDown, _tileColorUp;
    public Color _furnatureColor,
      _lightColor,
      _innerWallColor;
    public float _shadowStrength;
    public int _profile;
    public bool _useFog,
      _rain;
  }
}