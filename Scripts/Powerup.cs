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

      if (!GameScript.s_EditorEnabled)
      {
        _rb = _icon.AddComponent<Rigidbody>();
        _rb.angularDamping = 0.4f;
        _rb.constraints = RigidbodyConstraints.FreezePosition;
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.linearDamping = 1.5f;

        _rb.position = new Vector3(_rb.position.x, 0f, _rb.position.z);
        //_rb.isKinematic = true;

        Rotate();
      }
    }
    // Hide editor indicator
    transform.GetComponent<MeshRenderer>().enabled = false;
    transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
  }

  public void Activate(ActiveRagdoll r)
  {
    if (_activated) return;
    if (r == null) return;

    _activator = r;
    _activated = true;
    _Powerups.Remove(this);
    var remove = true;
    GetComponent<CustomEntityUI>().Activate();
    switch (_type)
    {
      case PowerupType.SILENCED_PISTOL:
        r.EquipItem(GameScript.ItemManager.Items.PISTOL_SILENCED, ActiveRagdoll.Side.RIGHT);
        r.PlaySound("Ragdoll/Reload");
        break;

      case PowerupType.KNIFE:
        r.EquipItem(GameScript.ItemManager.Items.KNIFE, ActiveRagdoll.Side.LEFT);
        r.PlaySound("Ragdoll/Slice");
        break;

      case PowerupType.END:
        remove = false;
        if (r._IsPlayer && !GameScript.s_Singleton._ExitOpen)
        {
          SfxManager.PlayAudioSourceSimple(r._Controller.position, "Etc/Ping");

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
              if (e.IsChaser()) e._Ragdoll.TakeDamage(
                new ActiveRagdoll.RagdollDamageSource()
                {
                  Source = r,

                  HitForce = Vector3.zero,

                  Damage = 100,
                  DamageSource = Vector3.zero,
                  DamageSourceType = ActiveRagdoll.DamageSourceType.MELEE,

                  SpawnBlood = false,
                  SpawnGiblets = false
                });
            }


          }

          r._PlayerScript._HasExit = true;
          _rb.constraints = RigidbodyConstraints.None;
          transform.parent = transform.parent.parent;
          //_audio.Stop();
          //GameScript.NextLevel();

          GameScript.ToggleExit();

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
    if (GameScript.s_Paused || Menu.s_InMenus)
    {
      _audio.volume = 0f;
      return;
    }
    if (GameScript.s_EditorEnabled || _rb == null)
    {
      _icon.transform.Rotate(new Vector3(1f, 1f, Mathf.Sin(Time.time)) * Time.deltaTime * 15f);
      return;
    }

    if (Time.time % 3 <= 0.1f) Rotate();
    _l.intensity += (Mathf.Clamp(_rb.angularVelocity.magnitude / 3f, 0.5f, 2f) - _l.intensity) * Time.deltaTime * 2f;

    if (_activated)
    {
      if (_activator == null || _activator._IsDead || _activator._Hip == null)
      {
        Destroy(gameObject);
        return;
      }

      // If game ended, set activated2
      if (GameScript.s_Singleton._GameEnded)
        _activated2 = true;

      // If activated2, move icon into sky
      if (_activated2)
      {
        _rb.MovePosition(_rb.position + ((new Vector3(_icon.transform.position.x, transform.position.y + 21f, _icon.transform.position.z)) - _rb.position) * Time.deltaTime * 3f);
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
        _rb.MovePosition(_rb.position + ((_activator._Controller.transform.position - _activator._Hip.transform.forward + new Vector3(0f, 1f, 0f)) - _rb.position) * Time.deltaTime * 4f);
      }

    }
    else
    {

      var pl_info = FunctionsC.GetClosestTargetTo(-1, transform.position);
      if (pl_info != null && pl_info._ragdoll != null)
      {
        var dist = pl_info._distance;
        _audio.volume += (((1f - dist / 5f) * 0.27f) - _audio.volume) * Time.deltaTime * 2f;
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
