

using SneakyEngine.Ragdoll;

namespace SneakyEngine.Engine
{

  public class SneakyEngineSystem
  {

    public static SneakyEngineSystem _Instance;

    public RagdollSystem _RagdollSystem;
    public static RagdollSystem RagdollSystem => _Instance._RagdollSystem;

    public SneakyEngineSystem()
    {
      _RagdollSystem = new();
    }

    public static void Initialize()
    {
      _Instance = new();
    }

  }

}