
namespace Assets.Scripts.UI.Menus
{
  // Wrapper class for adding dropdown selections
  class DropdownSelectionComponent
  {
    public string _selectionPrompt,
      _selectionDescription;
    public System.Action<MenuComponent> on_selected;

    public DropdownSelectionComponent(string promp, string desc, System.Action<MenuComponent> onselect)
    {
      _selectionPrompt = promp;
      _selectionDescription = desc;
      on_selected = onselect;
    }
  }
}