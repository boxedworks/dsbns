
using System.Collections;
using UnityEngine;

namespace SneakyEngine.Engine
{

  public class CoroutineSystem : MonoBehaviour
  {
    public static CoroutineSystem Singleton => SneakyEngineSystem.CoroutineSystem;

    public static Coroutine InitiateCoroutine(IEnumerator routine)
    {
      return Singleton.StartCoroutine(routine);
    }
    public static void TerminateCoroutine(Coroutine routine)
    {
      if (routine != null)
        Singleton.StopCoroutine(routine);
    }

  }

}