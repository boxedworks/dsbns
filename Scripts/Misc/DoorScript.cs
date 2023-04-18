using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : CustomEntity
{

  bool _opening;
  float _normalizedTime;

  Transform _door;
  public CustomEntityUI _button;

  AudioSource _sfx;

  public bool _hasButton
  {
    get { return _button != null; }
  }

  Vector3 _saveDoorPos;

  public bool _XScale, _opened;
  bool _original;
  public float _Speed;

  Light _l;
  Collider _collider0, _collider1;
  UnityEngine.AI.NavMeshObstacle _obstacle;
  ParticleSystem _ps_dust;

  public List<int> _trigger_enemies;
  public List<EnemyScript> _registered_enemies;

  // Use this for initialization
  void Start()
  {
    _door = transform.GetChild(0).GetChild(0);

    _saveDoorPos = _door.position;

    _collider0 = _door.GetComponent<Collider>();
    _collider1 = _door.GetComponents<Collider>()[1];
    _obstacle = _door.GetComponent<UnityEngine.AI.NavMeshObstacle>();

    _ps_dust = transform.GetChild(0).GetChild(2).GetComponent<ParticleSystem>();

    _original = _opened;
    if (!_opened)
    {
      SetDoorScale(1f);
      _normalizedTime = 1f;
      _obstacle.enabled = false;
    }

  }

  public void RegisterEnemy(int enemyPos)
  {
    if (_trigger_enemies == null) _trigger_enemies = new List<int>();
    if (_trigger_enemies.Contains(enemyPos)) return;
    _trigger_enemies.Add(enemyPos);
  }
  public string GetRegisteredEnemies()
  {
    var returnString = "";
    foreach (var enemyId in _trigger_enemies)
      returnString += $"{enemyId}|";
    return returnString;
  }

  public void RegisterEnemyReal(EnemyScript e)
  {
    if (_registered_enemies == null) _registered_enemies = new List<EnemyScript>();
    if (_registered_enemies.Contains(e)) return;
    e._linkedDoor = this;
    _registered_enemies.Add(e);
  }

  public void OnEnemyDie(EnemyScript e)
  {
    _registered_enemies.Remove(e);
    if (_registered_enemies.Count == 0)
      Toggle();
  }

  public void RegisterButton(ref CustomEntityUI button)
  {
    _button = button;
    AddToButtonList();
  }
  public void AddToButtonList()
  {
    _button.AddToActivateArray(this);
  }
  public void LinkToDoor(DoorScript script)
  {
    if (script._button == null) return;
    RegisterButton(ref script._button);
  }

  // Update is called once per frame
  void Update()
  {
    if (!_opening) return;

    //_door.position = Vector3.Lerp(_saveDoorPos, _saveDoorPos - new Vector3(0f, 1f, 0f), _normalizedTime);
    _normalizedTime = Mathf.Clamp(_normalizedTime + Time.deltaTime * _Speed * 0.4f * (_opened ? -1f : 1f), 0f, 1f);
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
      _ps_dust.Stop();

      if (!_opened)
        _obstacle.gameObject.isStatic = true;
    }
  }

  void SetDoorScale(float normalized)
  {
    _door.position = Vector3.Lerp(_saveDoorPos, _saveDoorPos + new Vector3(0f, -2.1f, 0f), normalized);
  }

  public void Toggle()
  {
    if (_button != null)
    {
      SfxManager.PlayAudioSourceSimple(transform.position, "Ragdoll/Tick");
    }

    if (!GameScript._EditorEnabled)
      EnemyScript.CheckSound(_door.position, EnemyScript.Loudness.SOFT);

    _opened = !_opened;
    Toggle(_opened);
  }

  void Toggle(bool open)
  {
    _collider0.enabled = open;
    //_collider1.enabled = open;
    _obstacle.enabled = open;

    _opening = true;

    if (_opened)
      _obstacle.gameObject.isStatic = false;

    if (_sfx == null)
    {
      _sfx = SfxManager.PlayAudioSourceSimple(transform.position, "Etc/Door_open", 1.1f, 1.3f);
      if (_sfx != null)
      {
        _sfx.loop = true;
      }
    }

    _ps_dust.Play();
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
