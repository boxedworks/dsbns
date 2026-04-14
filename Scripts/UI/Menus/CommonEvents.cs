
using UnityEngine;

namespace Assets.Scripts.UI.Menus
{
  public static class CommonEvents
  {
    // Switch to a menu
    public static System.Action<Menu.MenuType> _SwitchMenu = (Menu.MenuType type) =>
    {
      // Save last menu
      Menu.s_PreviousMenuType = Menu.s_CurrentMenuType;
      Menu.s_CurrentMenu.ToggleColliders(false);
      Menu.s_CurrentMenu._TimeDisplayed = -1f;
      Menu.s_MaxHeight = Menu.s_StartMaxHeight;
      Menu.s_CurrentMenu._OnSwitched?.Invoke();

      // Switch menu type
      if (type == Menu.MenuType.NONE)
      {
        GameScript.TogglePause(type);
        Menu.s_InPause = false;
        Menu.s_InMenus = false;
        Menu.s_Menu.gameObject.SetActive(false);
      }
      else
      {
        Menu.s_CurrentMenuType = type;
        Menu.s_CurrentMenu.ToggleColliders(true);
        Menu.s_CurrentMenu._TimeDisplayed = 0f;
        if (Menu.s_CurrentMenu._Type != Menu.MenuType.CREDITS)
          Menu.s_CurrentMenu._SelectedComponent._focused = true;
        Menu.s_CurrentMenu._OnSwitchTo?.Invoke();
        // Render new menu
        if (Menu.s_CurrentMenu._Type == Menu.MenuType.CREDITS) return;
        Menu._CanRender = true;
        Menu.RenderMenu();
      }
    };

    // Switch to the previous menu
    public static System.Action _SwitchMenuPrevious = () =>
    {
      _SwitchMenu(Menu.s_PreviousMenuType);
    };

    //
    public static System.Action<MenuComponent> _OnRender_XSelector = (MenuComponent component) =>
    {
      // Set hint text when focused
      component._selectorType = MenuComponent.SelectorType.X;
      component._useEmptySelector = true;
      if (component._focused)
      {
        component._selectorColor = "red";
      }
      else
      {
        component._selectorColor = Menu._COLOR_GRAY;
      }
    };

    //
    public static System.Action<MenuComponent> _RemoveDropdownSelections = (MenuComponent component) =>
    {
      Menu menu = component._menu;

      if (menu._DropdownCount == 0)
        return;

      // Remove dropdown selections
      for (int i = 0; i < menu._DropdownCount; i++)
      {
        var component0 = menu._MenuComponentsSelectable[menu._MenuComponentsSelectable.Count - 1];
        menu._MenuComponents.Remove(component0);
        menu._MenuComponentsSelectable.Remove(component0);
        Object.Destroy(component0._collider.gameObject);
      }

      // Remove prompt
      var component1 = menu._MenuComponents[menu._MenuComponents.Count - 1];
      menu._MenuComponents.Remove(component1);
      Object.Destroy(component1._collider.gameObject);

      // Reset dropdown count
      menu._DropdownCount = 0;
      menu._DropdownParentIndex = -1;

      // Fire action
      menu._OnDropdownRemoved?.Invoke();

      // Re-render menu
      Menu._CanRender = false;
      Menu.s_MaxHeight = Menu.s_StartMaxHeight;
      Menu.RenderMenu();
    }
    ,

      _DropdownSelect = (MenuComponent component) =>
      {
        if (component == null || component._menu == null || component._menu._MenuComponentsSelectable == null) return;

        // Set other dropdown menu components to dark grey
        var color = Menu._COLOR_GRAY; // dark grey;
        for (int i = component._menu._MenuComponentsSelectable.Count - component._menu._DropdownCount; i < component._menu._MenuComponentsSelectable.Count; i++)
          component._menu._MenuComponentsSelectable[i]._textColor = color;

        // Set this color to white
        component._textColor = "white";

        // Render menu
        Menu._CanRender = false;
        Menu.RenderMenu();
      };
  }
}