using SneakyEngine.Ragdoll;
using SneakyEngine.Effects;
using UnityEngine;

namespace SneakyEngine.Engine
{

  public class SneakyEngineSystem
  {

    public static SneakyEngineSystem _Instance;

    VfxSystem vfxSystem;
    public static VfxSystem VfxSystem => _Instance.vfxSystem;

    RagdollSystem ragdollSystem;
    public static RagdollSystem RagdollSystem => _Instance.ragdollSystem;

    CoroutineSystem coroutineSystem;
    public static CoroutineSystem CoroutineSystem => _Instance.coroutineSystem;

    public SneakyEngineSystem()
    {
      ragdollSystem = new();
      vfxSystem = new();

      coroutineSystem = new GameObject("CoroutineSystem").AddComponent<CoroutineSystem>();
    }

    public static void Initialize()
    {
      _Instance = new();
    }

  }

}