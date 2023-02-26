using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TVScript : MonoBehaviour
{

  // Get components
  public Light _light;
  public MeshRenderer _screen;
  public ExplosiveScript _explosion;
  ParticleSystem _ps_smoke, _ps_fire;
  GameObject _m0, _m1, _m2;
  AudioSource _audio_sfx;
  BoxCollider _collider;

  float _lastUpdate;

  // Start is called before the first frame update
  void Start()
  {
    _m0 = transform.GetChild(1).gameObject;
    _m1 = transform.GetChild(2).gameObject;
    _m2 = transform.GetChild(3).gameObject;

    _ps_smoke = transform.GetChild(4).GetComponent<ParticleSystem>();
    _ps_fire = transform.GetChild(5).GetComponent<ParticleSystem>();

    _audio_sfx = GetComponents<AudioSource>()[1];

    _audio_sfx.clip = FunctionsC.GetAudioClip("Etc/TV_static").clip;
    _audio_sfx.volume = 0.06f;
    FunctionsC.PlayAudioSource(ref _audio_sfx);

    _collider = GetComponent<BoxCollider>();
  }

  // Update is called once per frame
  Color[] colors = new Color[] { Color.blue, Color.green, Color.yellow };
  void Update()
  {

    _lastUpdate -= Time.deltaTime;
    if (_lastUpdate <= 0f)
    {
      if (_explosion._exploded)
      {
        _lastUpdate = 0.02f + Random.value * 0.02f;
        _light.range = 2.7f + Random.value * 0.6f;
        _light.intensity = 2.3f + Random.value * 1.5f;
      }
      else
      {
        _lastUpdate = 0.2f + Random.value * 0.1f;

        //var color = colors[Random.Range(0, colors.Count)];
        var color = Color.white * (Random.Range(0.7f, 1.1f));
        _light.color = color;
        _screen.sharedMaterials[1].color = color;
      }
    }
  }

  // Esplode
  public void Explode(ActiveRagdoll source)
  {
    if (_explosion._exploded) return;

    _explosion.Explode(source, false, false);
    FunctionsC.PlayOneShot(_explosion._audio);

    _m0.SetActive(false);
    _m1.SetActive(false);
    _m2.SetActive(true);

    _ps_smoke.Play();
    _ps_fire.Play();

    _audio_sfx.clip = FunctionsC.GetAudioClip("Etc/Fire_loop").clip;
    _audio_sfx.volume = 0.15f;
    FunctionsC.PlayAudioSource(ref _audio_sfx);

    _light.range = 3f;
    _light.type = LightType.Point;
    _light.color = new Color(1f, 0.4087965f, 0.01568627f);

    _collider.enabled = false;
  }
}
