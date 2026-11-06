
using System.Collections.Generic;
using SneakyEngine.Utility;
using UnityEngine;

namespace SneakyEngine.Physics
{

  // Handle body part noises
  public static class RigidbodyHandler
  {
    static List<RigidbodyData> _Rbs;

    static int _RbsIndex;

    //
    public static void Init()
    {
      _Rbs = new();
    }

    //
    public static void Update()
    {
      // If no rbs, ignore
      if (_Rbs.Count == 0) return;

      for (var u = Mathf.Clamp(3, 1, _Rbs.Count) - 1; u >= 0; u--)
      {

        // Gather data
        var index = _RbsIndex++ % _Rbs.Count;
        var rbData = _Rbs[index];

        // Check for null and remove
        var rb = rbData.Rigidbody;
        if (rb == null)
        {
          _Rbs.RemoveAt(index);
          continue;
        }

        //
        var mag = rb.linearVelocity.magnitude;
        var mag_old = rbData.MagnitudeOld;
        var lastSound = rbData.LastSoundFX;
        var bodyType = rbData.RigidbodyType;

        //
        var min = 3.5f;
        if (mag > mag_old && mag > min)
        {
          rbData.MagnitudeOld = mag;
          rbData.LastSoundFX = lastSound;

          _Rbs[index] = rbData;
          continue;
        }

        //
        if (Time.time - lastSound > 0.15f && mag_old > mag && mag_old > 0.5f)
        {
          rbData.MagnitudeOld = mag;
          rbData.LastSoundFX = Time.time;

          _Rbs[index] = rbData;

          switch (bodyType)
          {

            case RigidbodyType.BODY:
              var soundName = mag_old > 7f ? "Thud_loud" : "Thud";
              SfxManager.PlayAudioSourceSimple(rb.position, $"Ragdoll/{soundName}", 0.7f, 1.25f, SfxManager.AudioClass.FOOTSTEP);
              break;
            case RigidbodyType.WOOD:
              soundName = mag_old > 6f ? "Wood_hard" : "Wood_soft";
              SfxManager.PlayAudioSourceSimple(rb.position, $"Etc/{soundName}", 0.7f, 1.25f, SfxManager.AudioClass.FOOTSTEP);
              break;

          }

        }
      }
    }

    public static void AddListener(Rigidbody rb, RigidbodyType rigidbodyType)
    {
      _Rbs.Add(
        new RigidbodyData()
        {
          Rigidbody = rb,
          RigidbodyType = rigidbodyType,

          MagnitudeOld = 0f,
          LastSoundFX = 0f
        });
    }

    public static void ApplyExplosion(Vector3 position, float radius)
    {
      foreach (var bodyData in _Rbs)
      {
        if (bodyData.RigidbodyType == RigidbodyType.BODY) continue;

        var dir = MathC.Get2DVector(bodyData.Rigidbody.position - position);
        var dist = dir.magnitude;
        if (dist > radius * 1.5f) continue;
        bodyData.Rigidbody.AddForce(dir.normalized * 30f, ForceMode.Impulse);
      }
    }

    public static void Reset()
    {
      _Rbs.Clear();
    }
  }


}