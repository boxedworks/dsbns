using UnityEngine;

namespace SneakyGame.Objects.CustomEntities
{
  public abstract class CustomEntity : MonoBehaviour
  {
    abstract public void Activate(CustomEntityUI ui);
  }
}