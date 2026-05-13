
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace Assets.Scripts.XR
{
  public static class ManualXRControl
  {
    static bool s_xrStarted { get { return XRGeneralSettings.Instance.Manager.activeLoader != null; } }

    public static void StartXr(System.Action onComplete)
    {
      GameScript.s_Singleton.StartCoroutine(StartXRCoroutine(onComplete));
    }

    public static IEnumerator StartXRCoroutine(System.Action onComplete)
    {
      while (!s_xrStarted)
      {
        yield return TryStartXRCoroutine(onComplete);

        if (!s_xrStarted)
          yield return new WaitForSeconds(5);
      }

      //
      onComplete?.Invoke();
    }

    public static IEnumerator TryStartXRCoroutine(System.Action onComplete)
    {
      Debug.Log("Initializing XR...");
      yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

      if (!s_xrStarted)
      {
        Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
      }
      else
      {
        Debug.Log("Starting XR...");
        XRGeneralSettings.Instance.Manager.StartSubsystems();
      }
    }

    public static void StopXR()
    {
      Debug.Log("Stopping XR...");

      XRGeneralSettings.Instance.Manager.StopSubsystems();
      XRGeneralSettings.Instance.Manager.DeinitializeLoader();
      Debug.Log("XR stopped completely.");
    }

    //
    public static void Recenter()
    {
      List<XRInputSubsystem> subsystems = new();
      SubsystemManager.GetSubsystems(subsystems);
      if (subsystems.Count > 0)
      {
        XRInputSubsystem inputSubsystem = subsystems[0];
        Debug.Log($"Found and recentering active XRInputSubsystem: {inputSubsystem.subsystemDescriptor.id}");
        inputSubsystem.TryRecenter();
      }
    }
  }
}