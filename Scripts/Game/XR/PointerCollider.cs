
using UnityEngine;

namespace SneakyGame.XR
{
  public class PointerCollider : MonoBehaviour
  {
    void OnTriggerEnter(Collider other)
    {
      if (other.name == "Books")
      {
        FunctionsC.BookManager.ExplodeBooks(other, transform.position);
      }
    }
  }
}