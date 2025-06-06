﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveScript : MonoBehaviour
{

  public static List<ExplosiveScript> s_Explosives;

  public ExplosionType _explosionType;
  public float _radius;

  public bool _triggered, _exploded;
  float _delay;

  public AudioSource _audio;

  public System.Action _onExplode;

  public enum ExplosionType
  {
    UPWARD,
    AWAY,

    STUN
  }

  ActiveRagdoll _source;
  bool _disableGameObject, _playSound;

  TVScript _script_tv;

  // Use this for initialization
  public void Start()
  {
    if (s_Explosives == null) s_Explosives = new List<ExplosiveScript>();
    s_Explosives.Add(this);

    _audio = GetComponent<AudioSource>();

    if (gameObject.name == "Television")
    {
      _script_tv = GetComponent<TVScript>();
    }
  }

  void Update()
  {
    if (!_triggered) return;
    _delay -= Time.deltaTime;
    if (_delay <= 0f)
    {

      if (_script_tv != null)
      {
        _script_tv.Explode(_source);
      }
      else
      {
        Explode(_source, _disableGameObject, _playSound);
      }
    }

  }

  public void Trigger(ActiveRagdoll source, float delay = 0f, bool disableGameobject = true, bool playSound = true)
  {
    if (_triggered || (disableGameobject && !transform.GetChild(0).gameObject.activeSelf)) return;
    _delay = delay;
    _triggered = true;
    _source = source;
    _disableGameObject = disableGameobject;
    _playSound = playSound;
  }

  public void Explode(ActiveRagdoll source, bool disableGameobject = true, bool playSound = true)
  {
    if (s_Explosives == null || this == null || (disableGameobject && !transform.GetChild(0).gameObject.activeSelf)) return;

    _onExplode?.Invoke();

    _triggered = false;
    _exploded = true;

    // Remove from explosion list
    s_Explosives.Remove(this);

    // Show explosion scar
    FunctionsC.SpawnExplosionScar(new Vector3(transform.position.x, -1.23f, transform.position.z), _radius);

    // Hide mesh
    if (disableGameobject)
    {
      //transform.GetChild(0).gameObject.SetActive(false);
      GameObject.Destroy(gameObject);
    }

    // Particles
    var particles = FunctionsC.GetParticleSystem(_explosionType != ExplosionType.STUN ? FunctionsC.ParticleSystemType.EXPLOSION : FunctionsC.ParticleSystemType.EXPLOSION_STUN);
    var main = particles[0].main;
    main.startSpeed = new ParticleSystem.MinMaxCurve(2, Mathf.Lerp(5f, 10f, _radius / 6f));
    main = particles[1].main;
    main.startSpeed = new ParticleSystem.MinMaxCurve(2, Mathf.Lerp(5f, 10f, _radius / 6f));
    var shape = particles[2].shape;
    shape.radius = Mathf.Lerp(1f, 3f, _radius / 6f);

    // Check for nearby ragdolls
    FunctionsC.ApplyExplosionRadius(transform.position, _radius, _explosionType, source);

    // Check for books??
    var books = FunctionsC.BookManager._Books;
    for (var i = books.Count - 1; i >= 0; i--)
    {
      var b = books[i];
      if (b == null) continue;
      if (MathC.Get2DDistance(transform.position, b.position) > _radius) continue;
      FunctionsC.BookManager.ExplodeBooks(b.GetComponent<Collider>(), transform.position);
    }

    // Only affect certain other objects if not stun
    if (_explosionType != ExplosionType.STUN)
    {

      // Bullets
      var bullets = ItemScript._BulletPool;
      for (var i = bullets.Length - 1; i >= 0; i--)
      {
        var b = bullets[i];
        if (b == null) continue;
        if (MathC.Get2DDistance(transform.position, b.transform.position) > _radius) continue;
        b.Hide();
      }

      // Check for nearby explosives
      foreach (var e in s_Explosives)
      {
        if (e == null) continue;
        if (e.name.Equals("Missle") || e.name.Equals("Grenade") || MathC.Get2DDistance(transform.position, e.transform.position) > _radius) continue;
        e.Trigger(source, 0.25f);
      }

      // Check for nearby tables
      var mapObjects = GameObject.Find("Map_Objects").transform;
      for (var i = 0; i < mapObjects.childCount; i++)
      {
        var c = mapObjects.GetChild(i);
        if (c.gameObject.name != "Table") continue;
        if (MathC.Get2DDistance(transform.position, c.position) > _radius * 1f) continue;

        // Flip table
        var explosionPosition = transform.position;
        explosionPosition.y = -0.56f;
        PlayerScript.FlipTable(explosionPosition, MathC.Get2DVector((c.position - transform.position).normalized), _radius * 1f);
      }

    }

    // Set sound alert
    EnemyScript.CheckSound(transform.position, EnemyScript.Loudness.LOUD);

    //
    var smokeParts = FunctionsC.GetParticleSystem(FunctionsC.ParticleSystemType.EXPLOSION_SMOKE)[0];
    var spawnPos = transform.position;
    spawnPos.y = 1f;
    smokeParts.transform.position = spawnPos;
    smokeParts.Emit(10);

    // Play sound
    if (playSound)
      SfxManager.PlayAudioSourceSimple(transform.position, "Ragdoll/Explode", 0.825f, 1.175f);

    // Try to remove RB
    if (disableGameobject)
    {
      var rb = GetComponent<Rigidbody>();
      if (rb != null) Destroy(rb);
    }
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
    s_Explosives = null;
  }
}
