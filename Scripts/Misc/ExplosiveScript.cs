using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveScript : MonoBehaviour {

  public static List<ExplosiveScript> _Explosives;

  public ExplosionType _explosionType;
  public float _radius;

  public bool _triggered, _exploded;
  float _delay;

  public AudioSource _audio;

  public System.Action _onExplode;

  public enum ExplosionType
  {
    UPWARD,
    AWAY
  }

  ActiveRagdoll _source;
  bool _disableGameObject, _playSound;

  // Use this for initialization
  public void Start()
  {
    if (_Explosives == null) _Explosives = new List<ExplosiveScript>();
    _Explosives.Add(this);

    _audio = GetComponent<AudioSource>();
  }

  void Update()
  {
    if (!_triggered) return;
    _delay -= Time.deltaTime;
    if (_delay <= 0f) Explode(_source, _disableGameObject, _playSound);
  }

  public void Trigger(ActiveRagdoll source, float delay = 0f, bool disableGameobject = true, bool playSound = true)
  {
    if (_triggered || !transform.GetChild(0).gameObject.activeSelf) return;
    _delay = delay;
    _triggered = true;
    _source = source;
    _disableGameObject = disableGameobject;
    _playSound = playSound;
  }

  public void Explode(ActiveRagdoll source, bool disableGameobject = true, bool playSound = true)
  {
    if (_Explosives == null || this == null || !transform.GetChild(0).gameObject.activeSelf) return;

    _onExplode?.Invoke();

    _triggered = false;
    _exploded = true;
    // Remove from explosion list
    _Explosives.Remove(this);
    
    // Show explosion scar
    FunctionsC.SpawnExplosionScar(transform.position, _radius);

    // Hide mesh
    if (disableGameobject) transform.GetChild(0).gameObject.SetActive(false);

    // Particles
    var particles = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.EXPLOSION);
    var main = particles[0].main; 
    main.startSpeed = new ParticleSystem.MinMaxCurve(2, Mathf.Lerp(5f, 10f, (_radius / 6f)));
    main = particles[1].main;
    main.startSpeed = new ParticleSystem.MinMaxCurve(2, Mathf.Lerp(5f, 10f, (_radius / 6f))); var shape = particles[2].shape;
    shape.radius = Mathf.Lerp(1f, 3f, (_radius / 6f));
    // Check for nearby ragdolls
    FunctionsC.ApplyExplosionRadius(transform.position, _radius, _explosionType, source);
    // Check for nearby explosives
    foreach(ExplosiveScript e in _Explosives)
    {
      if (e == null) continue;
      if (e.name.Equals("Missle") || e.name.Equals("Grenade") || MathC.Get2DDistance(transform.position, e.transform.position) > _radius) continue;
      e.Trigger(source, 0.25f);
    }
    // Set sound alert
    EnemyScript.CheckSound(transform.position, EnemyScript.Loudness.LOUD);
    // Play sound
    if(playSound)
      FunctionsC.PlaySound(ref _audio, "Ragdoll/Explode", 0.9f, 1.1f);
    // Try to remove RB
    Rigidbody rb = GetComponent<Rigidbody>();
    if (rb != null) Destroy(rb);
  }

  public void Reset2()
  {
    gameObject.SetActive(true);
    transform.GetChild(0).gameObject.SetActive(true);
    Start();
    _triggered = false;
    _exploded = false;
    _onExplode = null;
  }

  public static void Reset()
  {
    _Explosives = null;
  }
}
