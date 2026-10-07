using UnityEngine;

namespace Assets.Scripts.Objects.CustomEntities
{
  public abstract class CustomEntity : MonoBehaviour
  {
    abstract public void Activate(CustomEntityUI ui);
  }
}