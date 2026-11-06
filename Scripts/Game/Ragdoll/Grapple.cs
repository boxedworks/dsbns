
namespace SneakyGame.Ragdoll.Actions
{
  public class Grapple
  {
    public ActiveRagdoll
      Grappler,
      Grapplee;

    public bool IsGrappling => Grapplee != null;
    public bool IsGrappled => Grappler != null;

    public float _LastGrappleTime;
  }
}