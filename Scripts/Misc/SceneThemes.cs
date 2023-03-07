using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneThemes : MonoBehaviour
{

  public static SceneThemes _Instance
  {
    set
    {
      m_Instance = value;
    }
    get
    {
      if (m_Instance == null) m_Instance = GameObject.Find("PProfiles").GetComponent<SceneThemes>();
      return m_Instance;
    }
  }
  static SceneThemes m_Instance;

  public static AudioSource _footstep;

  [SerializeField]
  public Theme[] _Themes;
  [SerializeField]
  public string[] _ThemeOrder;

  static int _CurrentTheme = -1;
  public static Theme _Theme;

  [System.Serializable]
  public class Theme
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

  public static void Init()
  {
    _Instance._Themes = _Master_Themes;
    _Instance._ThemeOrder = _Master_SceneOrder;
  }

  private void OnApplicationQuit()
  {
    PrintThemes();
  }

  static Theme GetLevelTheme(int themeIter)
  {
    return _Instance._Themes[themeIter];
  }

  [ContextMenu("Refresh Theme")]
  public void RefreshTheme()
  {
    Theme oldtheme = _Theme;
    _Theme = GetLevelTheme(_CurrentTheme);
    _Instance.StartCoroutine(ChangeMapThemeCo(oldtheme));
  }

  public static void ChangeMapTheme(string themeName)
  {
    var iter = -1;
    foreach (var t in _Instance._Themes)
    {
      iter++;
      if (t._name.Equals(themeName))
        break;
    }

    if (_CurrentTheme == iter) return;
    var oldtheme = _Theme;
    _CurrentTheme = iter;
    _Theme = GetLevelTheme(iter);
    _Instance.StartCoroutine(ChangeMapThemeCo(oldtheme));

    // Set footstep FX per theme
    _footstep = GameObject.Find($"Footstep_{_Theme._tile}")?.GetComponent<AudioSource>();
    if (_footstep == null) { _footstep = GameObject.Find("Footstep").GetComponent<AudioSource>(); }
    var color = _Theme._tileColorDown * 1.5f;
    color.a = 1f;
    FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.FOOTPRINT)[0].GetComponent<ParticleSystemRenderer>().sharedMaterial.color = color;

    // Enable / disable rain
    var rain_sfx = GameScript._Singleton._Rain_Audio;
    if (_Theme._rain && !rain_sfx.isPlaying)
    {
      FunctionsC.PlayAudioSource(ref rain_sfx, 1f, 1f, true);
      GameScript._Singleton._Thunder_Last = Time.time;
    }
    else if (!_Theme._rain && rain_sfx.isPlaying)
    {
      rain_sfx.Stop();
    }
  }

  static readonly float FOG_START = 14f, FOG_END = 10f;
  public static bool _ChangingTheme;
  static IEnumerator ChangeMapThemeCo(Theme oldtheme)
  {
    _ChangingTheme = true;

    // Enable / disable fog
    var fog_current = RenderSettings.fog;
    var fog_movement = 0; // -1 = dissappear; 1 = reappear
    if (fog_current && !_Theme._useFog)
      fog_movement = -1;
    else if (!fog_current && _Theme._useFog)
    {
      fog_movement = 1;
      RenderSettings.fogStartDistance = FOG_START;
      RenderSettings.fog = true;
    }

    // Change map colors
    GameObject.Find("Floor").GetComponent<MeshRenderer>().sharedMaterial.color = _Theme._tileColorUp;

    // Replace inner tiles decor
    var inner = GameObject.Find("Tile_" + _Theme._tile).transform.GetChild(0);
    // Change inner color
    //Material m = new Material(inner.GetChild(0).GetComponent<MeshRenderer>().sharedMaterials[0]);
    //m.color = _Theme._innerWallColor;
    //Debug.Log($"setting theme: {_Theme._name}, {_Theme._innerWallColor}");
    for (var u = 0; u < 4; u++)
    {
      var r = inner.GetChild(u).GetComponent<MeshRenderer>();
      for (var x = 0; x < r.sharedMaterials.Length; x++)
        r.sharedMaterials[x].color = _Theme._innerWallColor;
    }
    // Check for new ppprofile
    var newprofile = (oldtheme != null && oldtheme._profile != _Theme._profile);
    var profiles = Camera.main.transform.GetChild(4);
    // Change Tile theme / move fog
    int i = 0, iter_tile = 0;
    foreach (var t in TileManager._Tiles)
    {
      // Check fog movements
      if (fog_movement == 1)
        RenderSettings.fogStartDistance = Mathf.Lerp(FOG_START, FOG_END, (float)iter_tile++ / (float)TileManager._Tiles.Count);
      else if (fog_movement == -1)
        RenderSettings.fogStartDistance = Mathf.Lerp(FOG_END, FOG_START, (float)iter_tile++ / (float)TileManager._Tiles.Count);

      if (t._tile == null) continue;
      var enabled = new int[4];
      if (t._tile.transform.childCount > 0)
      {
        for (var u = 0; u < 4; u++)
          enabled[u] = t._tile.transform.GetChild(0).GetChild(u).gameObject.activeSelf ? 1 : 0;
        GameObject.DestroyImmediate(t._tile.transform.GetChild(0).gameObject);
      }
      var newinner = GameObject.Instantiate(inner.gameObject, t._tile.transform);
      for (var u = 0; u < 4; u++)
        newinner.transform.GetChild(u).gameObject.SetActive(enabled[u] == 1);
      newinner.transform.localPosition = Vector3.zero;
      if (i++ % 50 == 0)
      {
        yield return new WaitForSecondsRealtime(0.005f);
        var normalized = ((float)i) / ((float)TileManager._Tiles.Count);
        profiles.GetChild(_Theme._profile).localPosition = Vector3.Lerp(profiles.GetChild(_Theme._profile).localPosition, new Vector3(0f, 0f, 0f), normalized);
        if (newprofile)
          profiles.GetChild(oldtheme._profile).localPosition = Vector3.Lerp(profiles.GetChild(oldtheme._profile).localPosition, new Vector3(15f, 0f, 0f), normalized);
      }
    }

    // Check fog movements
    if (fog_movement == 1)
      RenderSettings.fogStartDistance = FOG_END;
    else if (fog_movement == -1)
      RenderSettings.fogStartDistance = FOG_START;

    // Set profile
    profiles.GetChild(_Theme._profile).localPosition = Vector3.Lerp(profiles.GetChild(_Theme._profile).localPosition, new Vector3(0f, 0f, 0f), 1f);
    if (newprofile)
      profiles.GetChild(oldtheme._profile).localPosition = Vector3.Lerp(profiles.GetChild(oldtheme._profile).localPosition, new Vector3(15f, 0f, 0f), 1f);

    // Remove fog
    if (fog_movement == -1)
      RenderSettings.fog = false;
    _ChangingTheme = false;
  }

  public static void PrintThemes()
  {
#if !UNITY_EDITOR
    return;
#endif
    // Function to conver Color class to string
    string GetColorString(Color c)
    {
      return $"new Color({c.r}f, {c.g}f, {c.b}f, {c.a}f)";
    }
    // Format all Theme class fields into a string
    string outstring = string.Empty;
    System.Reflection.FieldInfo[] fields = typeof(Theme).GetFields();
    foreach (Theme t in _Instance._Themes)
    {
      outstring += $"new Theme(){{\n";
      foreach (System.Reflection.FieldInfo field in fields)
      {
        // Format fields by type
        outstring += $"\t{field.Name} = ";
        if (field.FieldType == typeof(string)) outstring += $"\"{field.GetValue(t)}\",\n";
        if (field.FieldType == typeof(bool)) outstring += $"{field.GetValue(t).ToString().ToLower()},\n";
        if (field.FieldType == typeof(int)) outstring += $"{field.GetValue(t)},\n";
        if (field.FieldType == typeof(float)) outstring += $"{field.GetValue(t)}f,\n";
        if (field.FieldType == typeof(Color)) outstring += $"{GetColorString((Color)field.GetValue(t))},\n";
      }
      outstring += $"}},\n";
    }
    // Print to console
    Debug.Log(outstring);
  }

  public static string[] _Master_SceneOrder = new string[] {
    "Black and White",
    "Wood",
    "Dungeon",
    "Castle White",
    "Stone",
    "Hedge",
    "Wood",
    "Dungeon",
    "Castle White",
    "Stone",
    "Dungeon",//"Hedge",
    "Wood",
  };
  public static string[] _SceneOrder_LevelEditor = new string[]{
    "Black and White",

    "Wood",
    "Castle White",
    "Dungeon",
    "Stone",
    "Hedge"
  };
  public static Theme[] _Master_Themes = new Theme[]
  {
new Theme(){
  _name = "Black and White",
  _tile = "none",
  _tileColorDown = new Color(0.5f, 0.5f, 0.5f, 0f),
  _tileColorUp = new Color(0f, 0f, 0f, 0f),
  _furnatureColor = new Color(0f, 0f, 0f, 0f),
  _lightColor = new Color(1f, 1f, 1f, 0f),
  _innerWallColor = new Color(0f, 0f, 0f, 1f),
  _shadowStrength = 0.3f,
  _profile = 5,
  _useFog = false,
},
new Theme(){
  _name = "Black and White 2",
  _tile = "none",
  _tileColorDown = new Color(1f, 1f, 1f, 0f),
  _tileColorUp = new Color(0f, 0f, 0f, 0f),
  _furnatureColor = new Color(0f, 0f, 0f, 0f),
  _lightColor = new Color(1f, 1f, 1f, 0f),
  _innerWallColor = new Color(0f, 0f, 0f, 1f),
  _shadowStrength = 0.4f,
  _profile = 4,
  _useFog = false,
},
new Theme(){
  _name = "Hedge",
  _tile = "hedge",
  _tileColorDown = new Color(0.1698113f, 0.06736612f, 0.06327872f, 0f),
  _tileColorUp = new Color(0.3207547f, 0.1951762f, 0.1951762f, 0f),
  _furnatureColor = new Color(0.08245816f, 0.1603774f, 0.08245816f, 0f),
  _lightColor = new Color(1f, 1f, 1f, 0f),
  _innerWallColor = new Color(0.08245816f, 0.1603774f, 0.08245816f, 0f),
  _shadowStrength = 0.6f,
  _profile = 5,
  _useFog = false,
  _rain = true
},
new Theme(){
  _name = "Wood",
  _tile = "wood",
  _tileColorDown = new Color(0.2924528f, 0.2419383f, 0.2414115f, 0f),
  _tileColorUp = new Color(0.3207547f, 0.1951762f, 0.1951762f, 0f),
  _furnatureColor = new Color(0.3490566f, 0.2058117f, 0.2058117f, 0f),
  _lightColor = new Color(1f, 1f, 1f, 0f),
  _innerWallColor = new Color(0.2264151f, 0.1075575f, 0.1035956f, 0f),
  _shadowStrength = 0.7f,
  _profile = 5,
  _useFog = false,
},
new Theme(){
  _name = "Wood_Fog",
  _tile = "wood",
  _tileColorDown = new Color(0.6415094f, 0.5544282f, 0.5295479f, 0f),
  _tileColorUp = new Color(0.3207547f, 0.1951762f, 0.1951762f, 0f),
  _furnatureColor = new Color(0.490566f, 0.2614809f, 0.2614809f, 0f),
  _lightColor = new Color(1f, 0.8822758f, 0.6470588f, 0f),
  _innerWallColor = new Color(0.490566f, 0.2614809f, 0.2614809f, 0f),
  _shadowStrength = 0.3f,
  _profile = 5,
  _useFog = true,
},
new Theme(){
  _name = "Stone",
  _tile = "stone",
  _tileColorDown = new Color(0.2924528f, 0.1779547f, 0.1779547f, 0f),
  _tileColorUp = new Color(0.03773582f, 0.03773582f, 0.03773582f, 0f),
  _furnatureColor = new Color(0.4245283f, 0.2382965f, 0.2382965f, 0f),
  _lightColor = new Color(0.8773585f, 0.7987354f, 0.6414649f, 0f),
  _innerWallColor = new Color(0.09985755f, 0.1132075f, 0.1036718f, 1f),
  _shadowStrength = 0.65f,
  _profile = 5,
  _useFog = false,
},
new Theme(){
  _name = "Stone black",
  _tile = "stone",
  _tileColorDown = new Color(0.2169811f, 0.2169811f, 0.2169811f, 0f),
  _tileColorUp = new Color(0.5471698f, 0.5471698f, 0.5471698f, 0f),
  _furnatureColor = new Color(0.490566f, 0.490566f, 0.490566f, 0f),
  _lightColor = new Color(0.9705523f, 0.9716981f, 0.884612f, 0f),
  _innerWallColor = new Color(0f, 0f, 0f, 1f),
  _shadowStrength = 0.3f,
  _profile = 5,
  _useFog = false,
},
new Theme(){
  _name = "Stone white",
  _tile = "stone",
  _tileColorDown = new Color(0.8207547f, 0.8207547f, 0.8207547f, 1f),
  _tileColorUp = new Color(0.6415094f, 0.6415094f, 0.6415094f, 1f),
  _furnatureColor = new Color(0.3301887f, 0.3301887f, 0.3301887f, 1f),
  _lightColor = new Color(0.8584906f, 0.8584906f, 0.8584906f, 0f),
  _innerWallColor = new Color(0.8867924f, 0.8867924f, 0.8867924f, 1f),
  _shadowStrength = 0.7f,
  _profile = 5,
  _useFog = false,
},
new Theme(){
  _name = "Castle",
  _tile = "castle",
  _tileColorDown = new Color(0.8396226f, 0.8396226f, 0.8396226f, 0f),
  _tileColorUp = new Color(0f, 0f, 0f, 0f),
  _furnatureColor = new Color(0.2830189f, 0.09745461f, 0.09745461f, 0f),
  _lightColor = new Color(0.6981132f, 0.6945931f, 0.5960306f, 0f),
  _innerWallColor = new Color(0.8313726f, 0.8313726f, 0.8313726f, 1f),
  _shadowStrength = 0.4f,
  _profile = 5,
  _useFog = false,
},
new Theme(){
  _name = "Castle White",
  _tile = "castle",
  _tileColorDown = new Color(0.4056604f, 0.4056604f, 0.4056604f, 0f),
  _tileColorUp = new Color(0f, 0f, 0f, 0f),
  _furnatureColor = new Color(0.2264151f, 0.2264151f, 0.2264151f, 0f),
  _lightColor = new Color(0.9528302f, 0.8159035f, 0.8135012f, 0f),
  _innerWallColor = new Color(0.3962264f, 0.3962264f, 0.3962264f, 1f),
  _shadowStrength = 0.7f,
  _profile = 5,
  _useFog = false,
},
new Theme(){
  _name = "Dungeon",
  _tile = "dungeon",
  _tileColorDown = new Color(0.3867925f, 0.3521271f, 0.3521271f, 0f),
  _tileColorUp = new Color(0.09433961f, 0.09433961f, 0.09433961f, 0f),
  _furnatureColor = new Color(0.2735849f, 0.1664738f, 0.1664738f, 0f),
  _lightColor = new Color(0.8773585f, 0.7987354f, 0.6414649f, 0f),
  _innerWallColor = new Color(0.235493f, 0.2641509f, 0.2433088f, 1f),
  _shadowStrength = 0.65f,
  _profile = 5,
  _useFog = false,
},
new Theme(){
  _name = "Dungeon_Fog",
  _tile = "dungeon",
  _tileColorDown = new Color(0.4433962f, 0.2405215f, 0.2405215f, 0f),
  _tileColorUp = new Color(0.2169811f, 0.2169811f, 0.2169811f, 0f),
  _furnatureColor = new Color(0.1509434f, 0f, 0f, 0f),
  _lightColor = new Color(0.8113208f, 0.8113208f, 0.8113208f, 0f),
  _innerWallColor = new Color(0.235493f, 0.2641509f, 0.2433088f, 1f),
  _shadowStrength = 0.6f,
  _profile = 5,
  _useFog = true,
},
new Theme(){
  _name = "Dungeon blue",
  _tile = "dungeon",
  _tileColorDown = new Color(0.629272f, 0.6772987f, 0.745283f, 1f),
  _tileColorUp = new Color(0.003921569f, 0.08627451f, 0.1529412f, 1f),
  _furnatureColor = new Color(0.133366f, 0.2525634f, 0.3490566f, 1f),
  _lightColor = new Color(0.9294118f, 0.9686275f, 0.9647059f, 0f),
  _innerWallColor = new Color(0.1490196f, 0.3764706f, 0.6431373f, 1f),
  _shadowStrength = 0.7f,
  _profile = 5,
  _useFog = false,
},
};
}