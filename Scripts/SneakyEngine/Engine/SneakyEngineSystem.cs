using SneakyEngine.Ragdoll;
using SneakyEngine.Effects;

namespace SneakyEngine.Engine
{

  public class SneakyEngineSystem
  {

    public static SneakyEngineSystem _Instance;

    public VfxSystem _VfxSystem;
    public static VfxSystem VfxSystem => _Instance._VfxSystem;

    public RagdollSystem _RagdollSystem;
    public static RagdollSystem RagdollSystem => _Instance._RagdollSystem;

    public SneakyEngineSystem()
    {
      _RagdollSystem = new();
      _VfxSystem = new();
    }

    public static void Initialize()
    {
      _Instance = new();
    }

  }

}