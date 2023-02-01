using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Powerup : MonoBehaviour
{

  public static List<Powerup> _Powerups;

  public enum PowerupType
  {
    SILENCED_PISTOL,
    KNIFE,
    END
  }

  ActiveRagdoll _activator;

  public PowerupType _type;

  public bool _activated, _activated2;

  GameObject _icon, _touch;
  Rigidbody _rb;

  Light _l;
  float _saveIntensity;

  AudioSource _audio;

  private void Start()
  {
    //Init();
  }

  // Use this for initialization
  public void Init()
  {
    if (_Powerups == null) _Powerups = new List<Powerup>();
    _Powerups.Add(this);

    transform.GetChild(0).gameObject.layer = 2;

    _touch = transform.GetChild(0).gameObject;

    _l = transform.GetChild(1).GetComponent<Light>();
    _saveIntensity = _l.intensity;

    _audio = GetComponent<AudioSource>();

    // Create icon for player
    {
      Vector3 pos = Vector3.zero, scale = new Vector3(1f, 1f, 1f);

      if (_type == PowerupType.SILENCED_PISTOL)
      {
        _icon = Instantiate(GameObject.Find("Icons").transform.GetChild(5).gameObject);
        pos = new Vector3(0f, -0.7f, 0f);
        scale = new Vector3(0.4f, 0.4f, 0.4f);
      }
      else if (_type == PowerupType.KNIFE)
      {
        _icon = Instantiate(GameObject.Find("Icons").transform.GetChild(0).gameObject);
        //pos = new Vector3(0f, -0.7f, 0f);
        scale = new Vector3(0.5f, 0.5f, 0.5f);
        _l.intensity = 0.7f;
      }
      else if (_type == PowerupType.END)
      {
        transform.GetChild(0).name = "Goal";
        _icon = Instantiate(transform.GetChild(0).gameObject);
        scale = new Vector3(0.5f, 0.5f, 0.5f);
      }

      _icon.transform.parent = transform;
      _icon.transform.localPosition = pos;
      _icon.transform.localScale = scale;
      _icon.GetComponent<BoxCollider>().enabled = false;

      _icon.name = "Icon";

      _l.transform.parent = _icon.transform;

      _icon.layer = 2;

      _rb = _icon.AddComponent<Rigidbody>();
      _rb.angularDrag = 0.4f;
      _rb.constraints = RigidbodyConstraints.FreezePosition;
      _rb.useGravity = false;
      _rb.interpolation = RigidbodyInterpolation.Interpolate;
      _rb.drag = 1.5f;
    }
    // Hide editor indicator
    transform.GetComponent<MeshRenderer>().enabled = false;
    transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;

    Rotate();
  }

  public void Activate(ActiveRagdoll r)
  {
    if (_activated) return;
    _activator = r;
    _activated = true;
    _Powerups.Remove(this);
    bool remove = true;
    GetComponent<CustomEntityUI>().Activate();
    switch (_type)
    {
      case (Powerup.PowerupType.SILENCED_PISTOL):
        r.EquipItem(GameScript.ItemManager.Items.PISTOL_SILENCED, ActiveRagdoll.Side.RIGHT);
        FunctionsC.PlaySound(ref r._audioPlayer, "Ragdoll/Reload");
        break;
      case (Powerup.PowerupType.KNIFE):
        r.EquipItem(GameScript.ItemManager.Items.KNIFE, ActiveRagdoll.Side.LEFT);
        FunctionsC.PlaySound(ref r._audioPlayer, "Ragdoll/Slice");
        break;
      case (Powerup.PowerupType.END):
        remove = false;
        if (r._isPlayer && !GameScript._Singleton._ExitOpen)
        {
          FunctionsC.PlaySound(ref r._audioPlayer, "Etc/Ping");
          /*if (EnemyScript.AllDead())
          {
            GameScript.NextLevel();
            break;
          }*/
          // Disable chase enemies
          if (EnemyScript._Enemies_alive != null)
          {
            for (int i = EnemyScript._Enemies_alive.Count - 1; i >= 0; i--)
            {
              EnemyScript e = EnemyScript._Enemies_alive[i];
              if (e.IsChaser()) e.GetRagdoll().TakeDamage(r, ActiveRagdoll.DamageSourceType.MELEE, Vector3.zero, 100);
            }


          }

          GameScript.ToggleExit();
          r._playerScript._hasExit = true;
          _rb.constraints = RigidbodyConstraints.None;
          transform.parent = transform.parent.parent;
          //_audio.Stop();
          //GameScript.NextLevel();

          break;
        }
        break;
    }
    _touch.GetComponent<BoxCollider>().enabled = false;
    if (remove) GameObject.Destroy(gameObject);
  }

  // Update is called once per frame
  void Update()
  {
    if (GameScript._Paused || Menu2._InMenus)
    {
      _audio.volume = 0f;
      return;
    }

    if (Time.time % 3 <= 0.1f) Rotate();
    _l.intensity += (Mathf.Clamp(_rb.angularVelocity.magnitude / 3f, 0.5f, 2f) - _l.intensity) * Time.deltaTime * 2f;

    if (_activated)
    {
      if (_activator == null || _activator._dead || _activator._hip == null)
      {
        Destroy(gameObject);
        return;
      }
      // If game ended, set activated2
      if (GameScript._Singleton._GameEnded)
      {
        _activated2 = true;
      }
      // If activated2, move icon into sky
      if (_activated2)
      {
        _icon.transform.position += (((new Vector3(_icon.transform.position.x, transform.position.y + 21f, _icon.transform.position.z)) - _icon.transform.position) * Time.deltaTime * 3f);
        if (_icon.transform.position.y > transform.position.y + 20f)
        {
          Destroy(_icon);
          Destroy(this);
        }
      }
      // Else, follow the player
      else
      {
        //Vector3 newPos = MathC.Get2DVector(_icon.transform.position - GameObject.Find("EndLight").transform.position);
        //if (newPos.magnitude > 1f) newPos.Normalize();
        _icon.transform.position += (((_activator._controller.transform.position - _activator._hip.transform.forward + new Vector3(0f, 1f, 0f)) - _icon.transform.position) * Time.deltaTime * 4f);
      }

    }
    else
    {

      var pl_info = FunctionsC.GetClosestPlayerTo(transform.position);
      if (pl_info != null && pl_info._ragdoll != null)
      {
        var dist = pl_info._distance;
        _audio.volume += (((1f - dist / 5f) * 0.35f) - _audio.volume) * Time.deltaTime * 2f;
      }

    }
  }

  void Rotate()
  {
    _rb.AddTorque(new Vector3(-1f + Random.value * 2f, -1f + Random.value * 2f, -1f + Random.value * 2f) * 600f);
  }

  public static void OnEditorEnable()
  {
    if (_Powerups == null) return;
    foreach (Powerup p in _Powerups)
      p.transform.GetChild(0).gameObject.layer = 0;
  }

  public static void Reset()
  {
    _Powerups = null;
  }
}
