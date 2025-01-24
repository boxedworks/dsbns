using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserScript : CustomEntity
{

  Transform _machine, _emitter, _laser;

  public float _rotationSpeed;

  int _onIter;
  public bool _on;
  bool _canTrigger;
  public float[] _onPattern;
  float _timer;

  AudioSource _audio_buzz, _audio_other;

  public enum LaserType
  {
    KILL,
    ALARM
  }

  public LaserType _type;

  float _saveRot;
  bool _saveOn, _canRotate;

  public void Init()
  {
    _machine = transform.GetChild(0);
    _laser = transform.GetChild(1);
    _emitter = _machine.GetChild(0);

    if (_type == LaserType.ALARM)
      _laser.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_EmissionColor", Color.blue);
    else
      _laser.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_EmissionColor", Color.red);

    _audio_buzz = _emitter.GetComponents<AudioSource>()[0];
    _audio_other = _emitter.GetComponents<AudioSource>()[1];
    _canTrigger = true;

    _saveRot = _machine.rotation.eulerAngles.y;
    _saveOn = _on;
    if (!GameScript.s_EditorEnabled) _canRotate = true;
  }

  // Update is called once per frame
  void Update()
  {
    if (_audio_buzz == null) Init();
    // Rotate laser
    if (_canRotate && _rotationSpeed != 0f)
    {
      _machine.Rotate(new Vector3(0f, 1f, 0f) * _rotationSpeed * 0.8f * Time.deltaTime);
    }

    if (_on)
    {
      if (!_audio_buzz.isPlaying) _audio_buzz.Play();
      RaycastHit h;
      if (Physics.SphereCast(_emitter.position, 0.2f, _emitter.forward, out h))
      {
        _laser.localScale = new Vector3(0.1f * Random.value, 0.1f * Random.value, h.distance);
        _laser.LookAt(_emitter.position + _emitter.forward * 100f);
        _laser.position = _emitter.position + _emitter.forward * h.distance / 2f;
        // Check if person there
        ActiveRagdoll r = ActiveRagdoll.GetRagdoll(h.collider.gameObject);
        if (r == null || !_canTrigger) { }
        else
        {
          switch (_type)
          {
            // Slice ragdoll in half and kill
            case (LaserType.KILL):
              if (r._IsDead) break;
              // If ragdoll does not take damage, dismember
              if (!r.TakeDamage(
                new ActiveRagdoll.RagdollDamageSource()
                {
                  Source = null,

                  HitForce = Vector3.zero,

                  Damage = 1,
                  DamageSource = new Vector3(0f, 1f, 0f),
                  DamageSourceType = ActiveRagdoll.DamageSourceType.LASER,

                  SpawnBlood = false,
                  SpawnGiblets = false
                }))
                break;
              HingeJoint j = null;// h.collider.GetComponent<HingeJoint>();
              if (j == null) j = r._spine;
              r.Dismember(j);
              r._Hip.linearVelocity = Vector3.zero;
              // Particles
              FunctionsC.PlayComplexParticleSystemAt(FunctionsC.ParticleSystemType.SMOKE, j.transform.position);
              // Sound
              r.PlaySound("Ragdoll/Slice");
              r.PlaySound("Etc/Sizzle");
              break;
            // Alert AIs
            case (LaserType.ALARM):
              _canTrigger = false;
              foreach (EnemyScript e in EnemyScript._Enemies_alive)
              {
                if (e._Ragdoll._Id == r._Id) continue;
                e.SetRagdollTarget(r);
                e.TargetFound();
              }
              SfxManager.PlayAudioSourceSimple(transform.position, "Etc/Buzzer");
              break;
          }

        }
      }
    }
    else if (_audio_buzz.isPlaying) _audio_buzz.Stop();

    _timer -= Time.fixedDeltaTime;
    if (_onPattern == null || _onPattern.Length == 0) return;
    if (_timer < 0f)
    {
      _on = !_on;
      _timer = _onPattern[_onIter++ % _onPattern.Length];
      _laser.gameObject.SetActive(_on);
    }
  }

  public void Reset()
  {
    Quaternion q = _machine.rotation;
    q.eulerAngles = new Vector3(q.eulerAngles.x, _saveRot, q.eulerAngles.z);
    _machine.rotation = q;
    _on = _saveOn;
    _canRotate = true;
  }

  public void OnEditorEnable()
  {
    Reset();
    _canRotate = false;
  }

  public override void Activate(CustomEntityUI ui)
  {
    _rotationSpeed = -_rotationSpeed;
  }
}
