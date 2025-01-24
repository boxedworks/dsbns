using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreviewActor : MonoBehaviour {

  public enum ActorType
  {
    LASER,
    BOMB,
    HOUSE
  }
  ActorType _type;
  public float[] _properties;

  public void Init(ActorType type)
  {
    _type = type;
  }

  private void Update()
  {
    if (_properties == null) return;
    switch (_type)
    {
      // Rotate and scale
      case (ActorType.LASER):
        transform.Rotate(new Vector3(0f, 0f, 1f) * _properties[0] * Time.unscaledDeltaTime);

        RaycastHit h;
        if (Physics.Raycast(new Ray(transform.position, transform.up * 100f), out h))
        {
          Transform laser = transform.GetChild(0);
          laser.parent = GameScript.s_Singleton.transform;
          laser.localScale = new Vector3(0.02f, h.distance, 0.02f);
          laser.position = transform.position + transform.up * laser.lossyScale.y / 2f;
          laser.parent = transform;
        }
        break;
    }
  }

}
