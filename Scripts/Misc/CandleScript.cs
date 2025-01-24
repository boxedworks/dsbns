using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CandleScript : MonoBehaviour
{

  public static List<CandleScript> s_Candles;
  public static void Reset()
  {
    s_Candles = new();
  }

  Light _light;
  float _baseIntensity = 1.75f,
    _start_time;

  public bool _enabled;
  ParticleSystem particles;
  ParticleSystem _particles
  {
    get
    {
      if (particles == null) particles = transform.GetChild(1).GetComponent<ParticleSystem>();
      return particles;
    }
    set { particles = value; }
  }

  public float _NormalizedEnable;
  float normalizedEnable;

  // Use this for initialization
  void Start()
  {
    s_Candles.Add(this);

    _light = transform.GetChild(2).GetComponent<Light>();

    if (GameScript.s_GameMode != GameScript.GameModes.SURVIVAL)
      _enabled = true;

    _NormalizedEnable = GameScript.s_GameMode != GameScript.GameModes.SURVIVAL ? 1f : 0f;
    if (GameScript.s_GameMode == GameScript.GameModes.SURVIVAL)
      _light.range = 8.3f;
  }

  // Update is called once per frame
  void Update()
  {
    //float moveAmount = 0.3f;
    //_l.intensity += (_baseIntensity + (-moveAmount + Random.value * moveAmount * 2f) - _l.intensity) * Time.deltaTime * 6f;

    // Lerp brightness
    if (_enabled)
      _light.intensity = Mathf.Clamp((Time.time - 1f) - _start_time, 0f, 1f) * _baseIntensity * normalizedEnable;
    if (Time.time - GameScript.s_LevelStartTime < 0.25f)
      normalizedEnable = _NormalizedEnable;
    else
      normalizedEnable += (_NormalizedEnable - normalizedEnable) * Time.deltaTime * 2f;

    // Particle FX
    if (normalizedEnable < 0.35f && _particles.isPlaying)
      _particles.Stop();
    else if (normalizedEnable >= 0.35f && !_particles.isPlaying)
      _particles.Play();
  }

  public void On()
  {
    if (_enabled) return;
    _enabled = true;
    _particles.Play();
    _start_time = Time.time - 1f;
  }

  public void Off()
  {
    if (!_enabled) return;
    _enabled = false;
    _particles.Stop();
    _light.intensity = 0f;
  }
}
