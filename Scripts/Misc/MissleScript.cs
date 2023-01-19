using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissleScript : MonoBehaviour {

  int _id;

  Rigidbody _rb;
  ParticleSystem _ps;
  public ItemScript _source;

  bool _triggered;
  Vector3 _startPos;
  Quaternion _startRot;

  ExplosiveScript _exp;
  AudioSource _audio;

  Vector3 _lastPos;

  Light _light;

  public static List<MissleScript> _Missles;

  public void Activate(ItemScript source)
  {
    if(_Missles == null) _Missles = new List<MissleScript>();
    _Missles.Add(this);

    _id = BulletScript._ID++;

    _source = source;
    _startPos = transform.localPosition;
    _startRot = transform.localRotation;
    _audio = GetComponent<AudioSource>();

    _lastPos = transform.position;
    // Set parent to Objects
    transform.parent = GameScript.GameResources._Container_Objects;
    // Create a RigidBody so the missle can fly
    if (_rb == null)
    {
      _rb = gameObject.AddComponent<Rigidbody>();
      _rb.interpolation = RigidbodyInterpolation.Interpolate;
      _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
      _rb.constraints = RigidbodyConstraints.FreezePositionY;
    }
    // Create an explosion script so the missle can explode
    if(_exp == null)
    {
      _exp = gameObject.AddComponent<ExplosiveScript>();
      _exp._explosionType = ExplosiveScript.ExplosionType.AWAY;
      _exp._radius = 2.5f;
    }
    if(_light == null)
      _light = GetComponent<Light>();
    _light.enabled = true;
    // Start the smoke particle system behind the rocket
    if (_ps == null) _ps = transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>();
    _ps.Play();
    // Shoot the missle forward
    _rb.AddForce(-transform.forward * 3000f);
    // Rocket sound
    FunctionsC.PlayAudioSource(ref _audio);
  }
	
	// Update is called once per frame
	void FixedUpdate () {
    if (_source == null || _triggered)
    {
      return;
    }
    float dis = (_lastPos - transform.position).magnitude;
    if (dis > 0.1f)
    {
      //Debug.Log((_lastPos - transform.position).magnitude);
      _lastPos = transform.position;
      if (dis > 1.5f) return;
      foreach (EnemyScript e in EnemyScript.CheckSound(transform.position, EnemyScript.Loudness.NORMAL, _id, false))
        e.Suspicious(_source.transform.position, EnemyScript.Loudness.NORMAL, Random.value * 0.4f);
    }
  }

  ActiveRagdoll _redirector;
  private void OnTriggerEnter(Collider other)
  {
    if (_source == null || _triggered || _rb == null) return;
    ActiveRagdoll r = ActiveRagdoll.GetRagdoll(other.gameObject);
    if(r != null)
    {
      // Make sure ragdoll is not the one who shot and is not dead
      if (r._id == _source._ragdoll._id || r._dead) return;
      // If bullet hit is swinging and has a two-handed weapon (bat), reflect bullet
      if (r._swinging && r.HasTwohandedWeapon())
      {
        _rb.velocity = -_rb.velocity * 1.1f;
        FunctionsC.PlaySound(ref GameScript._audioListenerSource, "Ragdoll/Bat", 0.6f, 0.8f);
        PlayerScript._SlowmoTimer += 2.5f;
        _source = r._itemL;
        _redirector = r;
        _exp._radius *= 1.5f;
        // Refresh weapon timer if reflected bullets; use again instantly
        if (r._itemL != null) r._itemL._useTime = (Time.time - 10f);
        else if (r._itemR != null) r._itemR._useTime = (Time.time - 10f);
        return;
      }
      // If the enemy took damage, dismember them
      if (r.TakeDamage(_redirector != null ? _redirector : _source._ragdoll, ActiveRagdoll.DamageSourceType.LARGE_FAST_OBJECT, MathC.Get2DVector(-(_source.transform.position - other.transform.position).normalized * (4000f + (Random.value * 2000f)) * _source._hit_force), _source.transform.position, 1))
      {
        r.Dismember(r._spine);
        return;
      }
    }
    // If hits wall, blow up
    {
      _triggered = true;
      // Blow up
      _audio.Stop();
      transform.position -= _rb.velocity.normalized * 1.5f;
      _exp.Explode(_source._ragdoll, false);
      _light.enabled = false;
      // Move particles so they do not dissapear
      GameScript._Singleton.StartCoroutine(DelayParticles());
      // Reset
      Destroy(_rb);
      GetComponent<Renderer>().enabled = false;
      transform.parent = _source.transform;
      transform.localPosition = _startPos;
      transform.localRotation = _startRot;
      // Remove from list
      _Missles.Remove(this);
    }
  }

  private void OnDestroy()
  {
    if (_Missles == null) return;
    _Missles.Remove(this);
  }

  IEnumerator DelayParticles(float time = 1f)
  {
    _ps.transform.parent = GameScript.GameResources._Container_Objects;
    yield return new WaitForSeconds(time);
    if (_ps != null)
    {
      _ps.Stop();
      _ps.transform.parent = transform.GetChild(0);
      _ps.transform.localPosition = new Vector3(0f, 0f, 5.6f);
      _triggered = false;
    }
  }
}
