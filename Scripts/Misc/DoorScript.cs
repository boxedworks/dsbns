using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : CustomEntity
{

  bool _opening;
  float _normalizedTime;

  Transform _door;
  public CustomEntityUI _Button;

  AudioSource _sfx;

  public bool _HasButton
  {
    get { return _Button != null; }
  }

  public bool _XScale, _Opened;
  public float _Speed;

  Light _l;
  Collider _collider0;
  UnityEngine.AI.NavMeshObstacle _obstacle;
  ParticleSystem _psDust;

  public List<EnemyScript> _EnemiesEditor;
  public List<EnemyScript> _EnemiesGame;

  bool _forceExtrasOpen { get { return Settings.s_SaveData.LevelData.ExtraEnemyMultiplier == 2 && GameScript.s_GameMode == GameScript.GameModes.CLASSIC && !GameScript._EditorEnabled; } }

  // Use this for initialization
  void Start()
  {
    _door = transform.GetChild(0).GetChild(0);

    _collider0 = _door.GetComponent<Collider>();
    _obstacle = _door.GetComponent<UnityEngine.AI.NavMeshObstacle>();

    _psDust = transform.GetChild(0).GetChild(2).GetComponent<ParticleSystem>();

    if (!_Opened)
    {
      SetDoorScale(1f);
      _normalizedTime = 1f;
      _obstacle.enabled = false;
    }

    // Special case
    if (_forceExtrasOpen && _Opened)
    {
      _Opened = false;
      Toggle(false);
    }
  }

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
  public void LinkToDoor(DoorScript script)
  {
    if (script._Button == null) return;
    RegisterButton(ref script._Button);
  }

  // Update is called once per frame
  void Update()
  {
    if (!_opening) return;

    //_door.position = Vector3.Lerp(_saveDoorPos, _saveDoorPos - new Vector3(0f, 1f, 0f), _normalizedTime);
    _normalizedTime = Mathf.Clamp(_normalizedTime + Time.deltaTime * _Speed * 0.4f * (_Opened ? -1f : 1f), 0f, 1f);
    SetDoorScale(_normalizedTime);

    if (_normalizedTime == 1f || _normalizedTime == 0f)
    {
      _opening = false;
      if (_sfx != null)
      {
        _sfx.loop = false;
        _sfx?.Stop();
        _sfx = null;
      }
      _psDust.Stop();
    }
  }

  void SetDoorScale(float normalized)
  {
    _door.localPosition = Vector3.Lerp(new Vector3(0f, -0.336f, 0f), new Vector3(0f, -0.75f, 0f), normalized);
  }

  public void Toggle()
  {
    if (_forceExtrasOpen) return;

    if (_Button != null)
    {
      SfxManager.PlayAudioSourceSimple(transform.position, "Ragdoll/Tick");
    }

    if (!GameScript._EditorEnabled)
      EnemyScript.CheckSound(_door.position, EnemyScript.Loudness.SOFT);

    _Opened = !_Opened;
    Toggle(_Opened);
  }

  void Toggle(bool open)
  {
    _collider0.enabled = open;
    //_collider1.enabled = open;
    _obstacle.enabled = open;

    _opening = true;

    if (_sfx == null)
    {
      _sfx = SfxManager.PlayAudioSourceSimple(transform.position, "Etc/Door_open", 1.1f, 1.3f);
      if (_sfx != null)
      {
        _sfx.loop = true;
      }
    }

    _psDust.Play();
  }

  public override void Activate(CustomEntityUI ui)
  {
    Toggle();
  }

  public void OnDestroy()
  {
    if (_sfx != null)
    {
      _sfx.loop = false;
      _sfx.Stop();
      _sfx = null;
    }
  }
}
