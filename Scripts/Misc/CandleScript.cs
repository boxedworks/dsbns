using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CandleScript : MonoBehaviour {

  Light _light;
  float _baseIntensity = 1.6f,
    _start_time;

  public bool _enabled;
  ParticleSystem particles;
  ParticleSystem _particles
  {
    get {
      if(particles == null) particles = transform.GetChild(1).GetComponent<ParticleSystem>();
      return particles;
    }
    set{ particles = value; }
  }

  public float _normalizedEnable;
  float normalizedEnable;

	// Use this for initialization
	void Start () {
    _light = transform.GetChild(2).GetComponent<Light>();

    _baseIntensity = _light.intensity;
    _light.intensity = 0f;

    if (GameScript._GameMode != GameScript.GameModes.SURVIVAL)
      _enabled = true;

    _normalizedEnable = GameScript._GameMode == GameScript.GameModes.CLASSIC ? 1f : 0f;
    if (GameScript._GameMode == GameScript.GameModes.SURVIVAL)
      _light.range = 8.3f;
	}
	
	// Update is called once per frame
	void Update () {
    //float moveAmount = 0.3f;
    //_l.intensity += (_baseIntensity + (-moveAmount + Random.value * moveAmount * 2f) - _l.intensity) * Time.deltaTime * 6f;
    if(_enabled)
      _light.intensity = Mathf.Clamp((Time.time - 1f) - _start_time, 0f, 1f) * _baseIntensity * normalizedEnable;

    normalizedEnable += (_normalizedEnable - normalizedEnable) * Time.deltaTime * 2f;
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
