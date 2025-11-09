using UnityEngine;

public abstract class CustomEntity : MonoBehaviour
{

  // Use this for initialization
  void Start()
  {
  }

  // Update is called once per frame
  void Update()
  {
  }

  //abstract void Init();
  abstract public void Activate(CustomEntityUI ui);
}
