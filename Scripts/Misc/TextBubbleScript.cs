using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextBubbleScript : MonoBehaviour {

  public TMPro.TextMeshPro _textMesh;
  float _timeStart;
  Transform _source;
  MeshRenderer _renderer;

  static List<TextBubbleScript> _bubbles;

	public TMPro.TextMeshPro Init(string text, Transform source, Color color)
  {
    if (_bubbles == null) _bubbles = new List<TextBubbleScript>();
    _bubbles.Add(this);

    _textMesh = transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();
    _textMesh.text = text;
    //_textMesh.color = color;
    _renderer = _textMesh.GetComponent<MeshRenderer>();

    _source = source;

    _timeStart = Time.time;

    return _textMesh;
  }

	// Update is called once per frame
	void Update () {
    // Check for null source
    if(_source == null)
    {
      Destroy(gameObject);
      return;
    }
    // Move text up
    transform.position += Vector3.up * Time.deltaTime * 1f;

    // Move text with _source
    var pos = transform.position;
    pos.x = _source.position.x;
    pos.z = _source.position.z;
    transform.position += (pos - transform.position) * Time.deltaTime * 4f;

    // Scale text size in and out
    float time = Time.time - _timeStart;
    Vector3 scale;
    float length = 2f, middle = 0.5f;
    if (time < middle)
      scale = Vector3.Lerp(Vector3.zero, new Vector3(1f, 1f, 1f), Easings.Interpolate(time / middle, Easings.Functions.BounceEaseInOut));
    else
      scale = Vector3.Lerp(new Vector3(1f, 1f, 1f), Vector3.zero, Easings.Interpolate((time - middle) / (length - middle), Easings.Functions.CircularEaseIn));
    if(scale.x > 0f)
      transform.localScale = scale;
    // Destroy after 2 seconds
    if (time > length)
      Destroy(gameObject);
	}

  private void OnDestroy()
  {
    _bubbles.Remove(this);
  }

  public static void ToggleBubbles(bool toggle)
  {
    if (_bubbles == null) return;
    foreach(TextBubbleScript t in _bubbles)
      t._renderer.enabled = toggle;
  }
}
