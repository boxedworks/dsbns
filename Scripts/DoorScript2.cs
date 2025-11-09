using System.Collections.Generic;
using UnityEngine;

public class DoorScript2 : CustomEntity
{

  Transform _door;
  public CustomEntityUI _Button;

  AudioSource _sfx_sustain;
  ParticleSystem _particles_laserSparks;

  public bool _HasButton
  {
    get { return _Button != null; }
  }

  public bool _XScale, _Opened;
  public float _Speed;

  Light _l;
  Collider _collider0;
  UnityEngine.AI.NavMeshObstacle _obstacle;

  public List<EnemyScript> _EnemiesEditor;
  public List<EnemyScript> _EnemiesGame;

  bool _forceExtrasOpen { get { return Settings.s_SaveData.LevelData.ExtraEnemyMultiplier == 2 && GameScript.s_GameMode == GameScript.GameModes.MISSIONS && !GameScript.s_EditorEnabled; } }

  // Use this for initialization
  void Start()
  {
    _door = transform.GetChild(0).GetChild(0);

    _collider0 = _door.GetComponent<Collider>();
    _obstacle = _door.GetComponent<UnityEngine.AI.NavMeshObstacle>();

    _particles_laserSparks = transform.GetChild(0).GetChild(2).GetComponent<ParticleSystem>();

    if (!_Opened)
    {
      Toggle(false);
      _obstacle.enabled = false;
    }

    // Special case
    if (_forceExtrasOpen && _Opened)
    {
      _Opened = false;
      Toggle(false);
    }

    //
    if (_Opened)
    {
      ToggleAudio(true);
      _particles_laserSparks.Play();
    }
  }

  //
  void ToggleAudio(bool toggle)
  {
    if (toggle)
    {
      if (_sfx_sustain != null) return;
      _sfx_sustain = SfxManager.PlayAudioSourceSimple(GameResources._Camera_Main.transform.position, "Etc/Door_laser", 0.9f, 1.1f);
      _sfx_sustain.loop = true;
    }
    else
    {
      if (_sfx_sustain == null) return;
      _sfx_sustain.loop = false;
      _sfx_sustain.Stop();
      _sfx_sustain = null;
    }
  }

  //
  public void RegisterEnemyEditor(EnemyScript e)
  {
    if (_EnemiesEditor == null) _EnemiesEditor = new List<EnemyScript>();
    if (_EnemiesEditor.Contains(e)) return;
    _EnemiesEditor.Add(e);
  }
  public string GetRegisteredEnemiesEditor()
  {
    // Get translated enemy table
    var t = new Dictionary<int, int>();
    var idIndex = 0;
    foreach (var e in EnemyScript._Enemies_alive)
    {
      t.Add(e._Id, idIndex++);
    }

    var returnString = "";
    foreach (var e in _EnemiesEditor)
      returnString += $"{t[e._Id]}|";
    return returnString;
  }

  public void RegisterEnemyGame(EnemyScript e)
  {
    if (_EnemiesGame == null) _EnemiesGame = new List<EnemyScript>();
    if (_EnemiesGame.Contains(e)) return;
    e._linkedDoor = this;
    _EnemiesGame.Add(e);
  }

  public void UnregisterEnemy(EnemyScript e)
  {
    _EnemiesEditor.Remove(e);
    e._linkedDoor = null;
  }

  public void OnEnemyDie(EnemyScript e)
  {
    _EnemiesGame.Remove(e);
    if (_EnemiesGame.Count == 0)
      Toggle();
  }

  public void RegisterButton(ref CustomEntityUI button)
  {
    _Button = button;
    AddToButtonList();
  }
  public void AddToButtonList()
  {
    _Button.AddToActivateArray(this);
  }
  public void LinkToDoor(DoorScript2 script)
  {
    if (script._Button == null) return;
    RegisterButton(ref script._Button);
  }

  public void Toggle()
  {
    if (_forceExtrasOpen) return;

    if (_Button != null)
    {
      SfxManager.PlayAudioSourceSimple(transform.position, "Ragdoll/Tick");
    }

    if (!GameScript.s_EditorEnabled)
      EnemyScript.CheckSound(_door.position, EnemyScript.Loudness.SOFT);

    _Opened = !_Opened;
    Toggle(_Opened);

    if (Time.time - GameScript.s_LevelStartTime > 0.2f)
      SfxManager.PlayAudioSourceSimple(transform.position, "Etc/Door_open", _Opened ? 0.9f : 0.6f, _Opened ? 1.1f : 0.8f);
  }

  void Toggle(bool open)
  {
    _collider0.enabled = open;
    _obstacle.enabled = open;
    transform.GetChild(0).GetChild(0).gameObject.SetActive(open);

    ToggleAudio(open);
    if (open)
      _particles_laserSparks.Play();
    else
      _particles_laserSparks.Stop();
  }

  public override void Activate(CustomEntityUI ui)
  {
    Toggle();
  }

  public void OnDestroy()
  {
    ToggleAudio(false);
  }
}
