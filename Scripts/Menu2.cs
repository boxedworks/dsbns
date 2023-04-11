using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Menu2
{
  // Menu types
  public enum MenuType
  {
    MAIN,
    SPLASH,

    OPTIONS,
    OPTIONS_GAME,
    OPTIONS_CONTROLS,
    OPTIONS_SETTINGS,

    MODE_SELECTION,
    LEVELS,
    PAUSE,
    SELECT_LOADOUT,
    EDIT_LOADOUT,
    SHOP,
    HOW_TO_PLAY,
    CONTROLS,
    DIFFICULTY_COMPLETE,
    STATS,
    SOCIAL,

    EXTRAS,

    EDITOR_MAIN,
    EDITOR_LEVELS,
    EDITOR_LEVELS_EXTERNAL,
    EDITOR_PACKS,
    EDITOR_PACKS_EDIT,

    GAMETYPE_CLASSIC,
    GAMETYPE_SURVIVAL,

    HOWTOPLAY_CLASSIC,
    HOWTOPLAY_SURVIVAL,
    HOWTOPLAY_EDITOR,

    CREDITS,

    MODE_EXIT_CONFIRM,

    CONTROLLERS_CHANGED,

    GENERIC_MENU,

    NONE
  }

  static int _SaveIndex;

  // Static container for all menus
  static Dictionary<MenuType, Menu2> _Menus;
  // Static iters for menu
  static MenuType _CurrentMenuType;
  public static Menu2 _CurrentMenu { get { return _Menus[_CurrentMenuType]; } }
  static MenuType _PreviousMenuType;
  public static Transform _Menu { get { return _Text.transform.parent; } }
  public static TextMesh _Text;
  public static string _TextBuffer;

  public static bool _InMenus;

  static float _WaitTime, _PlayTime;

  static public readonly string _COLOR_GRAY = "#202020";

  // Menu-specific variables
  public static bool _InPause;
  public static int _SaveMenuDir = -1, _SaveLevelSelected = -1;


  // Max height dictatinng number of components to show + height of text
  static int _Start_Max_Height = 23, Max_Height = _Start_Max_Height;
  public static int _Max_Height
  {
    get { return Max_Height; }
    set
    {
      Max_Height = value;
      float height = _Menu.GetChild(2).GetComponent<BoxCollider>().size.y;
      var distance = new Vector3(-6f, Mathf.Clamp(3.4f + (height * (Max_Height - _Start_Max_Height)), 3.4f, 1000f), -3.03f) - _Text.transform.localPosition;
      // Move text and boxcolliders
      if (distance.magnitude == 0f) return;
      _Text.transform.localPosition += distance;
      foreach (var component in _CurrentMenu._menuComponents)
        if (component._collider)
          component._collider.transform.localPosition += distance;
    }
  }

  public void Select(int index)
  {
    _menuComponentsSelectable[index]._onSelected?.Invoke(_menuComponentsSelectable[index]);
  }
  public void SetCurrentSelection(int index)
  {
    if (index == _selectionIndex) return;
    _menuComponentsSelectable[_selectionIndex]._onUnfocus?.Invoke(_menuComponentsSelectable[_selectionIndex]);
    _selectionIndex = index;
    _menuComponentsSelectable[index]._onFocus?.Invoke(_menuComponentsSelectable[index]);
  }

  public class MenuComponent
  {
    public Menu2 _menu;

    public enum ComponentType
    {
      DISPLAY,
      BUTTON_SIMPLE,
      BUTTON_TOGGLE,
      BUTTON_DROPDOWN
    }

    public ComponentType _type;
    string _displayText;

    public int _height,
      _index,
      _buttonIndex,
      _dropdownIndex;

    public System.Action<MenuComponent> _onSelected,
      _onRender,
      _onCreated,
      _onFocus, _onUnfocus,
      _onDoubleSelect;

    public BoxCollider _collider;

    public string _dropdownPrompt;
    public string[] _dropdownSelections;
    public System.Action<MenuComponent>[] _dropdownActions,
      _dropdownOnCreated,
      _dropdownOnDoubleSelected,
      _dropdownOnFocus,
      _dropdownOnUnfocus;

    public string _textColor;

    // Visibility toggle for component
    bool visible;
    public bool _visible
    {
      get { return visible; }
      set
      {
        visible = value;
        if (_collider) _collider.enabled = value;
      }
    }

    public bool _obscured,
      _focused;

    public enum SelectorType
    {
      NORMAL,
      QUESTION,
      X,
    }
    public SelectorType _selectorType;
    public string _selectorColor;

    public string GetSelectorTypeString()
    {
      switch (_selectorType)
      {
        case SelectorType.QUESTION:
          return "?";
        case SelectorType.X:
          return "x";
        default:
          return "*";
      }
    }

    public bool _useEmptySelector;
    public string GetEmptySelector()
    {
      if (_selectorType == SelectorType.NORMAL)
      {
        return "[ ]";
      }
      var selector_string = GetSelectorTypeString();
      return '[' + (_selectorColor != null ? $"<color={_selectorColor}>{selector_string}</color>" : selector_string) + ']';
    }

    public MenuComponent(Menu2 menu, string text, ComponentType componentType = ComponentType.DISPLAY)
    {
      _menu = menu;

      _textColor = string.Empty;

      _visible = true;
      _obscured = false;

      _dropdownIndex = -1;

      // Add prior component listener
      _onUnfocus += (MenuComponent component) =>
      {
        _menu._menuComponent_lastFocused = component;
      };

      // Add button text for button types
      if (componentType != ComponentType.DISPLAY)
      {
        text = $"{GetEmptySelector()} {text}";

        _onSelected += (MenuComponent component) =>
        {
          _menu._menuComponent_lastSelected = component;
        };
      }

      // Add scroll feature
      _onFocus += (MenuComponent component) =>
      {
        _focused = true;
        if (_menu._menuComponent_lastFocused == null) return;
        if (_Max_Height > _Start_Max_Height && component._height <= (_Max_Height - _Start_Max_Height) + 5)
          _Max_Height = Mathf.Clamp(_Max_Height - Mathf.Clamp(_menu._menuComponent_lastFocused._height - component._height, 0, 1000), _Start_Max_Height, _menu._maxHeight);
        else if (component._height >= _Max_Height - 5)
          _Max_Height = Mathf.Clamp(_Max_Height - Mathf.Clamp(_menu._menuComponent_lastFocused._height - component._height, -1000, 0), _Start_Max_Height, _menu._maxHeight);
      };
      _onUnfocus += (MenuComponent component) =>
      {
        _focused = false;
      };

      // Set dropdown events
      _onSelected += (MenuComponent component) =>
      {
        if (_menu == null || _menu._menuComponentsSelectable == null)
          return;
        if (_buttonIndex >= _menu._menuComponentsSelectable.Count - _menu._dropdownCount)
          return;
        CommonEvents._RemoveDropdownSelections(component);
      };
      if (componentType == ComponentType.BUTTON_DROPDOWN)
      {
        _onSelected += (MenuComponent component) =>
        {
          if (_dropdownSelections == null) return;
          _menu._dropdownCount = _dropdownSelections.Length;
          _menu._dropdownParentIndex = _menu._selectionIndex;
          _menu._selectedComponent._onUnfocus?.Invoke(_menu._selectedComponent);
          _menu._selectionIndex = -1;
          _Text.text = _menu.GetDisplayText();
          _menu._onRendered?.Invoke();
          // Create prompt
          var prompt = $"\n===========\n{_dropdownPrompt}";
          _menu.AddComponent(prompt);
          _TextBuffer = string.Empty;
          _TextBuffer += prompt;
          // Create dropdown selections
          var iter = 0;
          foreach (var selection in _dropdownSelections)
          {
            // Create a simple button for each selection
            _menu.AddComponent($"{selection}\n", ComponentType.BUTTON_SIMPLE)
              // Register selection action
              .AddEvent(_dropdownActions[iter])
              // Set text
              .AddEvent((MenuComponent component0) =>
              {
                if (component0._obscured) return;
                CommonEvents._DropdownSelect(component0);
              })
              // Before selection renders, update text
              .AddEvent(EventType.ON_RENDER, (MenuComponent component0) =>
              {
                component0.SetDisplayText($"{_dropdownSelections[component0._dropdownIndex]}\n");
              })
              // Add focus event
              .AddEvent(EventType.ON_FOCUS, (MenuComponent component0) =>
              {
                if (_dropdownOnFocus == null || component0._dropdownIndex > _dropdownOnFocus.Length - 1) return;
                _dropdownOnFocus[component0._dropdownIndex]?.Invoke(component0);
              })
              .AddEvent(EventType.ON_UNFOCUS, (MenuComponent component0) =>
              {
                if (_dropdownOnUnfocus == null || component0._dropdownIndex > _dropdownOnFocus.Length - 1) return;
                _dropdownOnUnfocus[component0._dropdownIndex]?.Invoke(component0);
              });
            // Add double selected event
            if (_dropdownOnDoubleSelected != null && iter < _dropdownOnDoubleSelected.Length)
              _menu.AddEvent(EventType.ON_SELECTED_DOUBLE, _dropdownOnDoubleSelected[iter]);
            // Fire on created event
            if (_dropdownOnCreated != null && iter < _dropdownOnCreated.Length)
              _dropdownOnCreated[iter]?.Invoke(_menu._menuComponent_last);
            // Set selection index to first dropdown selection
            if (_menu._menuComponent_last._textColor == "white")
              _menu._selectionIndex = _menu._menuComponent_last._buttonIndex;
            // Register dropdown index
            _menu._menuComponent_last._dropdownIndex = iter;
            // Update the text buffer with selection
            var displayText = _menu._menuComponent_last._obscured ? FunctionsC.GenerateGarbageText(selection) : selection;
            displayText = (_menu._menuComponent_last._buttonIndex == _menu._selectionIndex ? "[<color=yellow>*</color>" : "[ ") + $"] <color={_menu._menuComponent_last._textColor}>{displayText}</color>";
            _TextBuffer += $"{displayText}\n";
            iter++;
          }
          if (_menu._selectionIndex > -1)
            _menu._selectedComponent._onFocus?.Invoke(_menu._selectedComponent);

          if (_TextBuffer.Contains("..") || _Text.text.Contains(".."))
          {
            _CanRender = false;
            RenderMenu();
          }
          else
            _CanRender = true;
        };
      }

      _displayText = text;
      _type = componentType;
      // Get start text height
      var startHeight = 0;
      // Check height of current text
      var h0 = 0;
      if (_Text.text.Contains("\n"))
        h0 = _Text.text.Split('\n').Length - 1;
      // Check height of components
      foreach (var component in menu._menuComponents)
      {
        int h = 0;
        if (component._displayText.Contains("\n"))
          h = component._displayText.Split('\n').Length - 1;
        startHeight += h;
      }
      // Set indexes
      foreach (var component in menu._menuComponents)
      {
        _index++;
        if (component._type == ComponentType.DISPLAY) continue;
        _buttonIndex++;
      }
      // Create colliders
      // Get maximum length
      var width = 0;
      foreach (var line in _displayText.Split('\n'))
        if (line.Length > width) width = line.Length;
      // Get height
      var height = 1;
      if (_displayText.Contains("\n"))
        height = _displayText.Split('\n').Length - 1;
      _height = startHeight + height - 1;
      // Set collider
      var gameObject = new GameObject();
      gameObject.name = text;
      gameObject.transform.parent = _Menu;
      gameObject.transform.localEulerAngles = Vector3.zero;
      gameObject.transform.localPosition = new Vector3(-5.87f, 3.2f, -3.1f);
      _collider = gameObject.AddComponent<BoxCollider>();
      var size = new Vector2(0.15f, 0.247f);
      _collider.size = new Vector3(size.x * width, size.y * height, 0.02f);
      gameObject.transform.localPosition += new Vector3(size.x * (width - 1) / 2f, -size.y * (height - 1) / 2f, 0f) + new Vector3(0f, -size.y * startHeight, 0f);

      _onCreated?.Invoke(this);
    }

    // Get the component's _displayText; fires the onRender action before
    public string GetDisplayText(bool fireAction = true)
    {
      if (fireAction) _onRender?.Invoke(this);

      //
      return _obscured ? FunctionsC.GenerateGarbageText(_displayText) : _displayText;
    }
    // Sets the component's _displayText
    public void SetDisplayText(string text, bool instant = false)
    {
      // Append selection box for selectables
      if (_type != ComponentType.DISPLAY)
        text = $"{GetEmptySelector()} {text}";

      _displayText = text;
    }

    // Register dropdown data
    public void SetDropdownData(string prompt, List<string> selections, List<System.Action<MenuComponent>> actions_selected, string selection_match,
      List<System.Action<MenuComponent>> actions_onCreated = null,
      List<System.Action<MenuComponent>> action_onDoubleSelect = null,
      List<System.Action<MenuComponent>> action_onFocus = null,
      List<System.Action<MenuComponent>> action_onUnfocus = null)
    {
      var iter = 0;
      var selections_final = new string[selections.Count + 1];
      if (actions_onCreated == null)
      {
        actions_onCreated = new List<System.Action<MenuComponent>>();
        for (int i = 0; i < actions_selected.Count; i++)
          actions_onCreated.Add((MenuComponent component) => { });
      }
      var match_found = false;
      foreach (var selection in selections)
      {
        var color = _COLOR_GRAY; // dark grey;
        if (!match_found &&
          (selection_match != string.Empty && selection.Contains(selection_match) ||
          selection_match == "" && iter == 0))
        {
          match_found = true;
          color = "white";
        }
        actions_onCreated[iter] += (MenuComponent component) =>
        {
          component._textColor = color;
        };
        var last_char = iter == selections.Count - 1 ? "\n" : "";
        selections_final[iter++] = $"{selection}{last_char}";
      }
      // Add a back button
      selections_final[iter] = "back";
      actions_selected.Add((MenuComponent component0) =>
      {
        component0._menu._selectionIndex = _buttonIndex;
        CommonEvents._RemoveDropdownSelections(component0);
      });
      actions_onCreated.Add((MenuComponent component) =>
      {
        component._textColor = _COLOR_GRAY;
      });

      _dropdownPrompt = prompt;
      _dropdownSelections = selections_final;
      _dropdownActions = actions_selected.ToArray();
      _dropdownOnCreated = actions_onCreated.ToArray();
      _dropdownOnDoubleSelected = action_onDoubleSelect?.ToArray();
      _dropdownOnFocus = action_onFocus?.ToArray();
      _dropdownOnUnfocus = action_onUnfocus?.ToArray();
    }
  }

  // Menu actions
  System.Action _onUpdate,
    _onRendered,
    _onSpace,
    _onBack,
    _onSwitchTo,
    _onSwitched,
    _onDropdownRemoved;

  // Array of menu components
  List<MenuComponent> _menuComponents;
  List<MenuComponent> _menuComponentsSelectable;

  // The prior menu component
  MenuComponent _menuComponent_lastFocused, _menuComponent_lastSelected;
  MenuComponent _menuComponent_last { get { return _menuComponents[_menuComponents.Count - 1]; } }
  MenuComponent _menuComponentSelectable_last { get { return _menuComponentsSelectable.Count == 0 ? null : _menuComponentsSelectable[_menuComponentsSelectable.Count - 1]; } }

  MenuComponent _selectedComponent
  {
    get
    {
      // Check for null
      if (_menuComponentsSelectable == null || _menuComponentsSelectable.Count == 0) return null;
      // Check range
      if (_selectionIndex >= _menuComponentsSelectable.Count) _selectionIndex = _menuComponentsSelectable.Count - 1;
      return _menuComponentsSelectable[_selectionIndex];
    }
  }

  // Currently selected component in _menuComponentsSelectable
  public int _selectionIndex;

  // Total height of the menu
  int _maxHeight { get { return _menuComponents[_menuComponents.Count - 1]._height; } }

  // Number of dropdown items currently displayed
  public int _dropdownCount,
    _dropdownParentIndex;

  // Time the menu has been on screen
  float _timeDisplayed;

  bool _canSkip, _canSlowLoad;

  public bool _hasDropdown
  {
    get { return _dropdownCount > 0; }
  }

  // Primary key of menu via Enum
  public MenuType _type;

  public Menu2(MenuType type)
  {
    _type = type;

    _canSkip = true;
    _canSlowLoad = true;

    // Add to menu list
    if (_Menus == null) _Menus = new Dictionary<MenuType, Menu2>();
    // Remove menu if exists already
    if (_Menus.ContainsKey(_type))
    {
      _Max_Height = _Start_Max_Height;
      _Menus[_type].Destroy();
    }

    _Menus.Add(type, this);

    _menuComponents = new List<MenuComponent>();
    _menuComponentsSelectable = new List<MenuComponent>();

    // Add update action
    _onUpdate += () =>
    {
      // Increment time displayed if is current menu
      if (_CurrentMenuType == _type)
        _timeDisplayed += Time.unscaledDeltaTime;
    };

    // Add back action
    _onBack += () =>
    {
      // If main menu, highlight last selection. Do not select
      if (_type == MenuType.MAIN && _dropdownCount == 0)
      {
        var component_last = _menuComponentsSelectable[_menuComponentsSelectable.Count - 1];

        _selectionIndex = component_last._buttonIndex;
        component_last._onFocus?.Invoke(component_last);

        _CanRender = false;
        RenderMenu();

        return;
      }
      // Select last option
      _menuComponentSelectable_last?._onSelected?.Invoke(_menuComponent_last);
    };
  }

  public void Destroy()
  {
    for (var i = _menuComponents.Count - 1; i >= 0; i--)
    {
      var component = _menuComponents[i];
      if (_menuComponentsSelectable.Contains(component)) _menuComponentsSelectable.Remove(component);
      _menuComponents.Remove(component);
      GameObject.Destroy(component._collider.gameObject);
    }
    _menuComponents = null;
    _menuComponentsSelectable = null;
    _Menus.Remove(_type);
  }

  // Concat all menu components display texts
  string GetDisplayText()
  {
    var text = string.Empty;

    foreach (var component in _menuComponents)
    {
      var displayText = component.GetDisplayText();
      var visible = component._visible;

      // Check for visibility changes
      if (component._height > _Max_Height)
        component._visible = false;
      else
        component._visible = true;

      if (!component._visible)
      {
        var matches = System.Text.RegularExpressions.Regex.Matches(displayText, "\n");
        displayText = "";
        foreach (var match in matches)
          displayText += '\n';
        text += displayText;
        continue;
      }
      else
      {
        // If near bottom, show ... to indicate a list
        if (component._height > _Max_Height - 1 &&
          _maxHeight - component._height > 0)
          displayText = "[ ] ...\n";
      }

      // Check selection
      if (component._type != MenuComponent.ComponentType.DISPLAY)
      {
        var selector = "";

        if (component._useEmptySelector)
        {
          selector = component.GetEmptySelector();
        }
        else
          selector = component._buttonIndex == _selectionIndex ||
            (_dropdownCount > 0 && component._buttonIndex == _dropdownParentIndex) ?
            $"[<color=yellow>*</color>]" : component.GetEmptySelector();
        if (component._textColor != "white" && component._textColor != "")
          displayText = $"{selector} <color={component._textColor}>{displayText.Substring(4)}</color>";
        else
          displayText = $"{selector} <color=white>{displayText.Substring(4)}</color>";
      }

      text += displayText;
    }

    return text;
  }

  public void QuickRemoveEdit()
  {
    if (!_CurrentMenu._hasDropdown)
    {
      var text = _CurrentMenu._menuComponentsSelectable[_CurrentMenu._selectionIndex].GetDisplayText(false).Trim();
      if ((text.Contains("left") || text.Contains("right")) && !text.Contains(" - "))
      {
        var util = text.Contains("utility");
        SendInput(Input.SPACE);
        _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable[_CurrentMenu._menuComponentsSelectable.Count - _CurrentMenu._dropdownCount]._buttonIndex;
        SendInput(Input.SPACE);
        SendInput(Input.SPACE);
        if (util) SendInput(Input.BACK);
      }
      else if (text.Contains("mods") && !text.Split('\n')[0].Contains(" - "))
      {
        SendInput(Input.SPACE);
        SendInput(Input.SPACE);
        SendInput(Input.SPACE);
        SendInput(Input.BACK);
      }
    }
    else
    {
      _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable[_CurrentMenu._menuComponentsSelectable.Count - _CurrentMenu._dropdownCount]._buttonIndex;
      SendInput(Input.SPACE);
      SendInput(Input.SPACE);
      if (_CurrentMenu._hasDropdown) SendInput(Input.BACK);
    }
  }

  string _text_buffer;
  // Prepare for rendering the menu
  void Render()
  {
    string text = GetDisplayText();

    if (_CanRender && _canSlowLoad)
    {
      _Text.text = "";
      _TextBuffer = text;
      return;
    }

    RenderFast(true);
  }
  // Quickly render the menu
  void RenderFast(bool bypass = false)
  {
    if (!_canSkip && !bypass) return;
    _Text.text = System.Text.RegularExpressions.Regex.Replace(GetDisplayText(), "~[0-9]", "");
    _TextBuffer = "";
    if (_WaitTime == 0f) _WaitTime = 1.5f;
    _onRendered?.Invoke();
  }

  // LINQ function to add menu components
  Menu2 AddComponent(string text, MenuComponent.ComponentType type = MenuComponent.ComponentType.DISPLAY, string textColor = "")
  {
    var component = new MenuComponent(this, text, type);
    component._textColor = textColor;

    // Add to components lists
    _menuComponents.Add(component);
    if (type != MenuComponent.ComponentType.DISPLAY)
      _menuComponentsSelectable.Add(component);

    return this;
  }

  // LINQ function to add an event to the last added component
  enum EventType
  {
    ON_SELECTED,
    ON_FOCUS,
    ON_UNFOCUS,
    ON_RENDER,
    ON_SELECTED_DOUBLE,
    ON_CREATED
  }
  Menu2 AddEvent(EventType type, System.Action<MenuComponent> actions)
  {
    var component = _menuComponent_last;
    switch (type)
    {
      case EventType.ON_SELECTED:
        component._onSelected += actions;
        break;
      case EventType.ON_SELECTED_DOUBLE:
        component._onDoubleSelect += actions;
        break;
      case EventType.ON_FOCUS:
        component._onFocus += actions;
        break;
      case EventType.ON_UNFOCUS:
        component._onUnfocus += actions;
        break;
      case EventType.ON_RENDER:
        component._onRender += actions;
        break;
      case EventType.ON_CREATED:
        component._onCreated += actions;
        break;
      default:
        throw new System.NullReferenceException();
    }
    return this;
  }
  Menu2 AddEvent(System.Action<MenuComponent> actions)
  {
    return AddEvent(EventType.ON_SELECTED, actions);
  }
  Menu2 AddEventFront(System.Action<MenuComponent> actions)
  {
    _menuComponent_last._onSelected = actions + _menuComponent_last._onSelected;
    return this;
  }

  Menu2 SetSelectorType(MenuComponent.SelectorType selectorType)
  {
    _menuComponent_last._selectorType = selectorType;
    return this;
  }

  // Add a back button component
  Menu2 AddBackButton(MenuType menuType, string text = "")
  {
    return AddComponent(text == "" ? "back\n" : text, MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
        component._menu._selectionIndex = 0;
        CommonEvents._SwitchMenu(menuType);
      });
  }
  Menu2 AddBackButton(System.Action<MenuComponent> action)
  {
    return AddComponent("back\n", MenuComponent.ComponentType.BUTTON_SIMPLE).
      AddEvent(EventType.ON_SELECTED, action);
  }

  public static class CommonEvents
  {
    // Switch to a menu
    public static System.Action<MenuType> _SwitchMenu = (MenuType type) =>
    {
      // Save last menu
      _PreviousMenuType = _CurrentMenuType;
      _CurrentMenu.ToggleColliders(false);
      _CurrentMenu._timeDisplayed = -1f;
      _Max_Height = _Start_Max_Height;
      _CurrentMenu._onSwitched?.Invoke();
      // Switch menu type
      if (type == MenuType.NONE)
      {
        GameScript.TogglePause(type);
        _InPause = false;
        _InMenus = false;
        _Menu.gameObject.SetActive(false);
      }
      else
      {
        _CurrentMenuType = type;
        _CurrentMenu.ToggleColliders(true);
        _CurrentMenu._timeDisplayed = 0f;
        if (_CurrentMenu._type != MenuType.CREDITS)
          _CurrentMenu._selectedComponent._focused = true;
        _CurrentMenu._onSwitchTo?.Invoke();
        // Render new menu
        if (_CurrentMenu._type == MenuType.CREDITS) return;
        _CanRender = true;
        RenderMenu();
      }
    };

    // Switch to the previous menu
    public static System.Action _SwitchMenuPrevious = () =>
    {
      _SwitchMenu(_PreviousMenuType);
    };

    public static System.Action<MenuComponent> _RemoveDropdownSelections = (MenuComponent component) =>
    {
      Menu2 menu = component._menu;

      if (menu._dropdownCount == 0)
        return;
      // Remove dropdown selections
      for (int i = 0; i < menu._dropdownCount; i++)
      {
        var component0 = menu._menuComponentsSelectable[menu._menuComponentsSelectable.Count - 1];
        menu._menuComponents.Remove(component0);
        menu._menuComponentsSelectable.Remove(component0);
        GameObject.Destroy(component0._collider.gameObject);
      }
      // Remove prompt
      var component1 = menu._menuComponents[menu._menuComponents.Count - 1];
      menu._menuComponents.Remove(component1);
      GameObject.Destroy(component1._collider.gameObject);
      // Reset dropdown count
      menu._dropdownCount = 0;
      menu._dropdownParentIndex = -1;
      // Fire action
      menu._onDropdownRemoved?.Invoke();
      // Re-render menu
      _CanRender = false;
      _Max_Height = _Start_Max_Height;
      RenderMenu();
    }
    ,

      _DropdownSelect = (MenuComponent component) =>
      {
        if (component == null || component._menu == null || component._menu._menuComponentsSelectable == null) return;

        // Set other dropdown menu components to dark grey
        var color = _COLOR_GRAY; // dark grey;
        for (int i = component._menu._menuComponentsSelectable.Count - component._menu._dropdownCount; i < component._menu._menuComponentsSelectable.Count; i++)
          component._menu._menuComponentsSelectable[i]._textColor = color;
        // Set this color to white
        component._textColor = "white";
        // Render menu
        _CanRender = false;
        RenderMenu();
      };
  }

  public static void GenericMenu(string[] prompts, string backPrompt, MenuType toMenu, System.Action<MenuComponent> beforeSwitch = null, bool switchTo = false, System.Action<MenuComponent> afterSwitch = null, System.Action<MenuComponent> onBack = null)
  {
    var menu = new Menu2(MenuType.GENERIC_MENU)
    {

    };
    foreach (var s in prompts)
      menu.AddComponent(s);
    // Add back button
    menu.AddComponent("\n");
    menu.AddComponent(backPrompt, MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
        CommonEvents._SwitchMenu(toMenu);
      });
    if (onBack != null)
      menu.AddEvent(onBack);
    if (afterSwitch != null)
      menu.AddEvent(afterSwitch);
    if (beforeSwitch != null)
      menu.AddEventFront(beforeSwitch);
    // Switch
    if (switchTo) SwitchMenu(MenuType.GENERIC_MENU);
  }

  public static void GenericMenu(string[] prompts, System.Action<MenuComponent> beforeSwitch = null, bool switchTo = false, System.Action<MenuComponent> afterSwitch = null)
  {
    var menu = new Menu2(MenuType.GENERIC_MENU)
    {

    };
    foreach (var s in prompts)
      menu.AddComponent(s);
    if (afterSwitch != null)
      menu.AddEvent(afterSwitch);
    if (beforeSwitch != null)
      menu.AddEventFront(beforeSwitch);
    // Switch
    if (switchTo) SwitchMenu(MenuType.GENERIC_MENU);
  }

  // Initialize all menus
  public static void Init()
  {
    _InMenus = true;

    // Get menu text mesh
    _Text = GameObject.Find("Menu2").transform.GetChild(0).GetComponent<TextMesh>();

    // Menu audio
    _MenuAudio = new Dictionary<Noise, System.Tuple<AudioSource, float>>();
    var index = 0;
    foreach (var component in _Menu.gameObject.GetComponents<AudioSource>())
    {
      _MenuAudio.Add((Noise)(index++), System.Tuple.Create(component, component.volume));
    }

    // Set starting menu
    _CurrentMenuType = MenuType.SPLASH;

    // Custom functions
    MenuComponent ModifyMenu_TipComponents(MenuType type, int newLineCount, int afterNewLineCount = 0)
    {
      var lines = "";
      while (newLineCount-- > 0) lines += "\n";
      var after_lines = "";
      while (afterNewLineCount-- > 0) after_lines += "\n";
      _Menus[type].AddComponent(lines)
        .AddComponent(Shop.Tip.GetTip(GameScript._GameMode) + after_lines)
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
        {
          component._visible = Settings._ShowTips;
        });
      return _Menus[type]._menuComponent_last;
    }
    void ModifyMenu_TipSwitch(MenuType type)
    {
      var onSwitch = new System.Action(() =>
      {
        if (!Settings._ShowTips) return;
        var last_c = _Menus[type]._menuComponents.Where(component => component.GetDisplayText(false).Contains("*tip")).Single();
        var afterNewLineCount = System.Text.RegularExpressions.Regex.Matches(last_c.GetDisplayText(false), "\n").Count;
        var tip = Shop.Tip.GetTip(GameScript._GameMode);

        // Check if tip has buttons to display
        if (tip.Contains("&"))
        {
          var spl = tip.Split('&');
          tip = spl[0] + "   " + spl[1].Substring(2);

          var width = spl[0].Length - 22;

          var button = spl[1].Substring(0, 2);

          var parent = _Menu.GetChild(0);

          if (parent.childCount == 0)
          {
            FunctionsC.Control GetControl(string b)
            {
              switch (b)
              {
                case ("NB"):
                  return FunctionsC.Control.Y;
                case ("SB"):
                  return FunctionsC.Control.A;
                case ("EB"):
                  return FunctionsC.Control.B;
                case ("WB"):
                  return FunctionsC.Control.X;
                case ("LT"):
                  return FunctionsC.Control.L_TRIGGER;
                case ("RT"):
                  return FunctionsC.Control.R_TRIGGER;
                case ("LB"):
                  return FunctionsC.Control.L_BUMPER;
                case ("RB"):
                  return FunctionsC.Control.R_BUMPER;
                case ("LD"):
                  return FunctionsC.Control.DPAD_LEFT;
                case ("RD"):
                  return FunctionsC.Control.DPAD_RIGHT;
                case ("UD"):
                  return FunctionsC.Control.DPAD_UP;
                case ("DD"):
                  return FunctionsC.Control.DPAD_DOWN;
                case ("PU"):
                  return FunctionsC.Control.START;
                case ("RE"):
                  return FunctionsC.Control.SELECT;
                case ("LS"):
                  return FunctionsC.Control.L_STICK;
                case ("RS"):
                  return FunctionsC.Control.R_STICK;
                default:
                  Debug.LogError("unknown control");
                  return FunctionsC.Control.A;
              }
            }

            var t = SpawnControlUI(last_c, GetControl(button));
            t.parent = _Menu.GetChild(1);
            t.transform.position = last_c._collider.transform.position;
            var p = t.localPosition;
            p.x = -0.338f + (0.007f * width) + 0.013f;
            t.localPosition = p;
            t.gameObject.SetActive(false);
            t.parent = parent;

            // Check for second control UI
            if (spl.Length == 3)
            {
              tip += "   " + spl[2].Substring(2);

              button = spl[2].Substring(0, 2);
              width += spl[1].Length + 1;

              t = SpawnControlUI(last_c, GetControl(button));
              t.parent = _Menu.GetChild(1);
              t.transform.position = last_c._collider.transform.position;
              p = t.localPosition;
              p.x = -0.338f + (0.00699f * width) + 0.013f;
              t.localPosition = p;
              t.gameObject.SetActive(false);
              t.parent = parent;
            }
          }
        }
        // Set the tip to display
        var lines_after = "";
        while (afterNewLineCount-- > 0) lines_after += '\n';
        _Menus[type]._menuComponent_last.SetDisplayText(tip + lines_after);

        // Set enable buttons
        _Menus[type]._onRendered += () =>
        {
          var parent = _Menu.GetChild(0);
          if (parent.childCount == 0) return;
          for (var i = parent.childCount - 1; i >= 0; i--)
            if (!Settings._ShowTips)
              parent.GetChild(i).gameObject.SetActive(false);
            else
            {
              if (parent.GetChild(i).gameObject.activeSelf)
                return;
              else
                parent.GetChild(i).gameObject.SetActive(true);
            }
        };
      });
      var onSwitched = new System.Action(() =>
      {
        var parent = _Menu.GetChild(0);
        if (parent.childCount > 0)
          for (var i = parent.childCount - 1; i >= 0; i--)
          {
            var g = parent.GetChild(i).gameObject;
            g.transform.parent = _Menu;
            GameObject.Destroy(g);
          }
      });
      _Menus[type]._onSwitchTo += onSwitch;
      _Menus[type]._onSwitched += onSwitched;
    }

    // Splash screen
    new Menu2(MenuType.SPLASH)
    {

    }
    .AddComponent("~1")
    .AddComponent("system booting~1\n...\n...\n")
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]\n\n"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]\n\n"))
    .AddComponent(string.Format("{0,-10}<color=yellow>{1,-25}</color>........\n", "", "[boxedworks]\n\n"));

    // Switch to main menu after splash screen
    _Menus[MenuType.SPLASH]._onRendered += () =>
    {
      CommonEvents._SwitchMenu(MenuType.MAIN);
    };
    // Allow skip
    _Menus[MenuType.SPLASH]._onSpace += () =>
    {
      _CurrentMenu.RenderFast();
    };

    // Main menu
    System.Action<MenuComponent> func_exit = (MenuComponent component) =>
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
        component._selectorColor = _COLOR_GRAY;
      }
    };

    var demoText = GameScript._Singleton._IsDemo ? $" <color={_COLOR_GRAY}>[</color><color=red>demo</color><color={_COLOR_GRAY}>]</color>" : "";
    var main_menu = new Menu2(MenuType.MAIN)
    {

    }
    .AddComponent($"<color={_COLOR_GRAY}>definitely sneaky\nbut not sneaky</color>{demoText}\n\n")
    // Show game modes menu
    .AddComponent("play\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
        CommonEvents._SwitchMenu(MenuType.MODE_SELECTION);
        SteamManager.Achievements.LoadAchievements();

        TileManager._CurrentLevel_Name = "";
        TileManager._CurrentLevel_Loadout = null;
      });

#if UNITY_STANDALONE
    // Show level editor menu
    main_menu.AddComponent("level editor\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
        CommonEvents._SwitchMenu(MenuType.EDITOR_MAIN);

        Settings.OnGamemodeChanged(Settings.GamemodeChange.LEVEL_EDITOR);
      });
#endif
    // Show options menu
    main_menu.AddComponent("options\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.OPTIONS); })
    // Show social menu
    .AddComponent("social\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
        SwitchMenu(MenuType.SOCIAL);
      })
    // Display credits
    .AddComponent("credits\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent c) =>
      {
        SwitchMenu(MenuType.CREDITS);
      })
    // Exit application
    .AddComponent("exit\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        return;
#endif
        GameScript.OnApplicationQuitS();
      })
      .AddEvent(EventType.ON_RENDER, func_exit);
    // Tip
#if UNITY_STANDALONE
    ModifyMenu_TipComponents(MenuType.MAIN, 12, 1);
#else
    ModifyMenu_TipComponents(MenuType.MAIN, 14, 1);
#endif
    ModifyMenu_TipSwitch(MenuType.MAIN);

    // Level editor main
    new Menu2(MenuType.EDITOR_MAIN)
    {

    }
    .AddComponent($"<color={_COLOR_GRAY}>level editor</color>\n\n")
    // Local levels
    .AddComponent("local levels\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS); })
    // Loaded levels
    //.AddComponent("loaded levels\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
    //  .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.EDITOR_MAIN); })
    // Level packs
    .AddComponent("level packs\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS); })
    .AddComponent("quick guide\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.HOWTOPLAY_EDITOR); })
    .AddBackButton(MenuType.MAIN);
    // Tip
    //ModifyMenu_TipComponents(MenuType.EDITOR_MAIN, 16, 1);
    //ModifyMenu_TipSwitch(MenuType.EDITOR_MAIN);

    // Level editor - levels
    void SpawnMenu_LevelEditorLevels()
    {
      var menu = new Menu2(MenuType.EDITOR_LEVELS)
      {

      };

      if (!Levels._LevelPack_SelectingLevelsFromPack)
        menu
        .AddComponent($"<color={_COLOR_GRAY}>level editor - local levels</color>\n\n");
      else
      {
        var levelpack_name = Levels._LevelPack_Current._name;
        if (levelpack_name.Contains('/'))
        {
          var splitname = levelpack_name.Split('/');
          levelpack_name = splitname[splitname.Length - 1].Trim();
        }
        if (levelpack_name.Contains('\\'))
        {
          var splitname = levelpack_name.Split('\\');
          levelpack_name = splitname[splitname.Length - 1].Trim();
        }
        if (levelpack_name.EndsWith(".levelpack"))
          levelpack_name = levelpack_name.Substring(0, levelpack_name.Length - 10);

        menu
        .AddComponent($"<color={_COLOR_GRAY}>level pack - {levelpack_name}</color>\n<color={_COLOR_GRAY}>level pack - select level to start</color>\n\n");
      }

      // Local levels
      if (!Levels._LevelPack_SelectingLevelsFromPack)
      {
        menu
        .AddComponent("new level\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {

            // Open submenu with options
            var prompt = $"=== new level\n\n";
            var selections = new List<string>();
            var actions = new List<System.Action<MenuComponent>>();

            // New blank level
            selections.Add("new blank level");
            actions.Add((MenuComponent component0) =>
            {

              // Create new entry in levels
              Levels.LevelEditor_NewMap("unnamed map");

              // Reload menu
              CommonEvents._RemoveDropdownSelections(component0);
              CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS);

              _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable.Count - 2;
              _CanRender = false;
              RenderMenu();
              _CurrentMenu._selectedComponent._onFocus?.Invoke(_CurrentMenu._selectedComponent);
              SendInput(Input.SPACE);
              _CanRender = false;
              RenderMenu();
            });

            // New level from clipboard
            selections.Add("new level from clipboard data");
            actions.Add((MenuComponent component0) =>
            {

              // Validate map data
              var map_data = ClipboardHelper.clipBoard;
              if (map_data == null) return;
              map_data = map_data.Trim();

              TileManager.LoadMap(map_data, false, true);
            });

            // Set dropdown data
            component.SetDropdownData(prompt, selections, actions, "new blank level");
          });

      }

      // Add local levels
      Levels._CurrentLevelCollectionIndex = 3;
      GameScript._GameMode = GameScript.GameModes.CLASSIC;

      var leveldata = Levels._LevelPack_SelectingLevelsFromPack ? Levels._LevelPack_Current._levelData : Levels._CurrentLevelCollection._levelData;

      for (var i = 0; i < leveldata.Length; i++)
      {
        // Gather level name
        var level_data = leveldata[i];
        if (level_data.Trim().Length == 0) continue;
        var level_meta = Levels.GetLevelMeta(level_data);
        var level_name = level_meta[1] == null ? "unnamed map" : level_meta[1];
        var level_load = level_meta[2];

        // Extra line
        var extra = "";
        if (i == leveldata.Length - 1)
          extra = "\n";

        menu.AddComponent($"{level_name}\n{extra}", MenuComponent.ComponentType.BUTTON_DROPDOWN)
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {

            // Open submenu with options
            var prompt = $"=== level: {level_name}\n=== level options\n\n";
            var selections = new List<string>();
            var actions = new List<System.Action<MenuComponent>>();

            // Play
            selections.Add("play");
            actions.Add((MenuComponent component0) =>
            {

              if (!Levels._LevelPack_SelectingLevelsFromPack)
              {
                GameScript._EditorTesting = true;
                Levels._LevelEdit_SaveIndex = _CurrentMenu._dropdownParentIndex;

                // Load map
                _EditorSaveIndex = Menu2._CurrentMenu._dropdownParentIndex;
                Levels._CurrentLevelIndex = Menu2._CurrentMenu._dropdownParentIndex - 1;
                var level_data = leveldata[_EditorSaveIndex - 1];
                GameScript.NextLevel(level_data);

                // Remove menus
                CommonEvents._RemoveDropdownSelections(component0);
                _Menu.gameObject.SetActive(false);
                _InMenus = false;
                _InPause = false;
                Time.timeScale = 1f;
                GameScript._Paused = false;

                // Show editor menu
                TileManager.EditorMenus._Menu_EditorTesting.gameObject.SetActive(true);
              }
              else
              {
                Levels._LevelPack_Playing = true;

                // Load map
                GameScript.NextLevel(_CurrentMenu._dropdownParentIndex);

                // Remove menus
                CommonEvents._RemoveDropdownSelections(component0);
                _Menu.gameObject.SetActive(false);
                _InMenus = false;
                _InPause = false;
                Time.timeScale = 1f;
                GameScript._Paused = false;
              }

              // Play music
              if (FunctionsC.MusicManager._CurrentTrack < 3)
                FunctionsC.MusicManager.PlayNextTrack();

              // Remove level preview
              if (GameScript._lp0 != null)
                GameObject.Destroy(GameScript._lp0.gameObject);
            });

            // Edit
            if (!Levels._LevelPack_SelectingLevelsFromPack)
            {
              selections.Add("edit");
              actions.Add((MenuComponent component0) =>
              {
                GameScript._EditorTesting = true;
                Levels._LevelEdit_SaveIndex = _CurrentMenu._dropdownParentIndex;

                // Load map
                _EditorSaveIndex = Menu2._CurrentMenu._dropdownParentIndex;
                var level_data = leveldata[_EditorSaveIndex - 1];
                TileManager._EnableEditorAfterLoad = true;
                Levels._CurrentLevelIndex = Menu2._CurrentMenu._dropdownParentIndex - 1;
                GameScript.NextLevel(level_data);

                // Remove menus
                CommonEvents._RemoveDropdownSelections(component0);
                _Menu.gameObject.SetActive(false);
                _InMenus = false;
                _InPause = false;
                Time.timeScale = 1f;
                GameScript._Paused = false;

                // Play music
                if (FunctionsC.MusicManager._CurrentTrack < 3)
                  FunctionsC.MusicManager.PlayNextTrack();

                // Remove level preview
                if (GameScript._lp0 != null)
                  GameObject.Destroy(GameScript._lp0.gameObject);

              });

              // Rename level
              selections.Add("rename");
              actions.Add((MenuComponent component0) =>
              {

                var rename_dialogue = TileManager.EditorMenus.ShowRenameMenuMenu();
                var save_index = _CurrentMenu._dropdownParentIndex;

                // Get current name
                var level_data = Levels._CurrentLevelCollection._levelData[Menu2._CurrentMenu._dropdownParentIndex - 1];
                var level_meta = Levels.GetLevelMeta(level_data);
                var level_name = level_meta[1];

                Debug.Log($"Got level meta: \n{level_data}\n{level_meta[0]}\n{level_meta[1]}");

                // Set current name and highlight
                var textarea = rename_dialogue.GetChild(1).GetComponent<TMPro.TMP_InputField>();
                textarea.text = level_name;

                // Set OK button action
                var okbutton = rename_dialogue.GetChild(2).GetChild(0).GetComponent<UnityEngine.UI.Button>();
                okbutton.onClick.RemoveAllListeners();
                okbutton.onClick.AddListener(() =>
                {

                  var gottext = textarea.text.Trim();
                  if (gottext.Length == 0 || !System.Text.RegularExpressions.Regex.IsMatch(gottext, @"^[\w,\s-]+$"))
                    return;

                  // Check no change
                  if (gottext == level_name)
                  {
                    TileManager.EditorMenus.HideMenus();
                    return;
                  }

                  // Save levels
                  Levels._CurrentLevelCollection._levelData[Menu2._CurrentMenu._dropdownParentIndex - 1] = $"{level_meta[0]} +{gottext}" + (level_meta[2] != null ? $"!{level_meta[2]}" : "");
                  Levels.SaveLevels();

                  // Reload menu
                  CommonEvents._RemoveDropdownSelections(component0);
                  CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS);
                  Menu2._CurrentMenu._selectionIndex = save_index;
                  Menu2._CurrentMenu._selectedComponent._onSelected?.Invoke(Menu2._CurrentMenu._selectedComponent);
                  _CanRender = false;
                  RenderMenu();

                  // Hide editor menus
                  TileManager.EditorMenus.HideMenus();
                });

                // Cancel button
                var cancelbutton = rename_dialogue.GetChild(2).GetChild(1).GetComponent<UnityEngine.UI.Button>();
                cancelbutton.onClick.RemoveAllListeners();
                cancelbutton.onClick.AddListener(() =>
                {
                  // Hide editor menus
                  TileManager.EditorMenus.HideMenus();
                });
              });

              // Duplicate level
              selections.Add("duplicate");
              actions.Add((MenuComponent component0) =>
              {
                Levels.LevelEditor_NewMap("not matter");

                var level_data = Levels._CurrentLevelCollection._levelData[Menu2._CurrentMenu._dropdownParentIndex - 1];
                var level_meta = Levels.GetLevelMeta(level_data);
                var level_name = level_meta[1] == null ? "unnamed map" : level_meta[1];
                var level_load = level_meta[2];

                Levels._CurrentLevelCollection._levelData[Levels._CurrentLevelCollection._levelData.Length - 1] = Levels.ParseLevelMeta(level_meta[0], $"{level_name} copy", level_meta[2], level_meta[3]);
                Levels.SaveLevels();

                // Kelly is awesome
                // Reload menu
                CommonEvents._RemoveDropdownSelections(component0);
                CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS);
                Menu2._CurrentMenu._selectionIndex = Menu2._CurrentMenu._menuComponentsSelectable.Count - 2;
                _CanRender = false;
                RenderMenu();
                Menu2._CurrentMenu._selectedComponent._onFocus?.Invoke(Menu2._CurrentMenu._selectedComponent);
              });

              // Delete level
              selections.Add("delete - press 4 times");
              actions.Add((MenuComponent component0) =>
              {
                Levels._Delete_Iter--;
                if (Levels._Delete_Iter == 0)
                {
                  // Delete and level
                  var save_index = _CurrentMenu._dropdownParentIndex;
                  var data_delete = Levels._CurrentLevelCollection._levelData[save_index - 1];
                  Levels._CurrentLevelCollection._levelData = Levels._CurrentLevelCollection._levelData.Where((val, index) => index != save_index - 1).ToArray();
                  Levels.SaveLevels();

                  // Get current name
                  var level_meta = Levels.GetLevelMeta(data_delete);
                  var level_name = level_meta[1];

                  // Backup data
                  var filestructure_local = "Levelpacks/Local/";
                  var trashed_files = new System.IO.DirectoryInfo($"{filestructure_local}trashed/").GetFiles().OrderBy(p => p.LastWriteTime).ToArray();
                  if (trashed_files.Length > 10)
                    System.IO.File.Delete(trashed_files[0].FullName);

                  var now = System.DateTime.Now;
                  Levels.WriteToFile($"{filestructure_local}trashed/{level_name}-{now.Year}-{now.Month}-{now.Day}--{now.Hour}-{now.Minute}-{now.Second}.txt", data_delete);

                  // Remove preview
                  if (GameScript._lp0 != null)
                    GameObject.Destroy(GameScript._lp0.gameObject);

                  // Reload menu
                  CommonEvents._RemoveDropdownSelections(component0);
                  CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS);
                  Menu2._CurrentMenu._selectionIndex = save_index - 1;
                  _CanRender = false;
                  RenderMenu();
                  Menu2._CurrentMenu._selectedComponent._onFocus?.Invoke(Menu2._CurrentMenu._selectedComponent);
                }
                else
                {
                  component0.SetDisplayText($"delete - press {Levels._Delete_Iter} more time(s)");
                  _CanRender = false;
                  RenderMenu();
                }
              });

              // Copy level data to clipboard
              selections.Add("copy level data to clipboard");
              actions.Add((MenuComponent component0) =>
              {
                ClipboardHelper.clipBoard = $"{level_data}";

                var save_index = _CurrentMenu._dropdownParentIndex;
                CommonEvents._RemoveDropdownSelections(component0);
                CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS);
                Menu2._CurrentMenu._selectionIndex = save_index;
                _CanRender = false;
                RenderMenu();
              });

            }
            else
            {

              // Download level pack to local storage
              selections.Add("download to local levels");
              actions.Add((MenuComponent component0) =>
              {

                var level_data = Levels._LevelPack_Current._levelData[Menu2._CurrentMenu._dropdownParentIndex];

                var savelevelcollection = Levels._CurrentLevelCollectionIndex;
                Levels._CurrentLevelCollectionIndex = 3;
                if (Levels._CurrentLevelCollection_Name != "levels_editor_local")
                {
                  Debug.LogError("Trying to save local levels to wrong level collection");
                  return;
                }

                System.Array.Resize(ref Levels._CurrentLevelCollection._levelData, Levels._CurrentLevelCollection._levelData.Length + 1);
                Levels._CurrentLevelCollection._levelData[Levels._CurrentLevelCollection._levelData.Length - 1] = level_data;
                Levels.SaveLevels();

                Levels._CurrentLevelCollectionIndex = savelevelcollection;

                // Remove dropdown for feedback
                var saveindex = _CurrentMenu._dropdownParentIndex;
                CommonEvents._RemoveDropdownSelections(component0);
                _CurrentMenu._selectionIndex = saveindex;
                _CanRender = true;
                RenderMenu();

#if UNITY_STANDALONE
                // Show feedback
                SteamManager.SteamMenus.ShowInformationDialogue("Level downloaded to local levels");
#endif
              });
            }


            // Set dropdown data
            component.SetDropdownData(prompt, selections, actions, "play");

          })
          .AddEvent(EventType.ON_SELECTED, (MenuComponent component) =>
          {
            Levels._Delete_Iter = 4;

            if (_CurrentMenu._hasDropdown)
            {
              if (GameScript._lp0 != null)
                GameObject.Destroy(GameScript._lp0.gameObject);
              GameScript._lp0 = TileManager.GetMapPreview(level_data).transform;

              level_meta = Levels.GetLevelMeta(level_data);
              Levels.GetHardcodedLoadout(level_meta[2]);
            }
          })

          // Add / remove map preview
          .AddEvent(EventType.ON_FOCUS, (MenuComponent component) =>
          {
            if (!_CurrentMenu._hasDropdown)
            {
              if (GameScript._lp0 != null)
                GameObject.Destroy(GameScript._lp0.gameObject);
              GameScript._lp0 = TileManager.GetMapPreview(level_data).transform;

              level_meta = Levels.GetLevelMeta(level_data);
              Levels.GetHardcodedLoadout(level_meta[2]);
            }
          })
          .AddEvent(EventType.ON_UNFOCUS, (MenuComponent component) =>
          {
            if (GameScript._lp0 != null && !_CurrentMenu._hasDropdown)
              GameObject.Destroy(GameScript._lp0.gameObject);
          });
      }

      // Back button
      menu.AddBackButton((MenuComponent) =>
      {

        // Remove map preview
        if (GameScript._lp0 != null)
          GameObject.Destroy(GameScript._lp0.gameObject);

        // Switch back to editor main
        if (Levels._LevelPack_SelectingLevelsFromPack)
        {
          Levels._LevelPack_SelectingLevelsFromPack = false;
          CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS);
          _CurrentMenu._selectionIndex = Levels._LevelPacks_Play_SaveIndex;
          _CanRender = false;
          RenderMenu();
          Menu2.SendInput(Input.SPACE);
        }
        else
          CommonEvents._SwitchMenu(MenuType.EDITOR_MAIN);
      });

      // Onswitch
      menu._onSwitchTo += () =>
      {
        SpawnMenu_LevelEditorLevels();

        // Set up loadout editing
        if (Levels._HardcodedLoadout == null)
          Levels._HardcodedLoadout = new GameScript.ItemManager.Loadout()
          {
            _id = -1,
            _equipment = new GameScript.PlayerProfile.Equipment()
          };

        _CurrentMenu._selectedComponent?._onFocus?.Invoke(_CurrentMenu._selectedComponent);
      };

      // Tip
      //ModifyMenu_TipComponents(MenuType.EDITOR_MAIN, 16, 1);
      //ModifyMenu_TipSwitch(MenuType.EDITOR_MAIN);
    }
    SpawnMenu_LevelEditorLevels();

    // Level packs
    void SpawnMenu_LevelEditorPacks()
    {

#if UNITY_STANDALONE
      var menu_levelpacks = new Menu2(MenuType.EDITOR_PACKS);
      var filestructure_local = "Levelpacks/Local/";
      var filestructure_workshop = "Levelpacks/WorkshopContent/";

      // Display local level packs
      if (!Levels._LevelPack_UploadingToWorkshop)
      {
        menu_levelpacks
        .AddComponent($"<color={_COLOR_GRAY}>level editor - level packs</color>\n\n")

        // Create a new level pack
        .AddComponent("new level pack\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent(EventType.ON_SELECTED, (MenuComponent component) =>
          {
            Levels.LevelPacks_NewLocal();

            // Reload menu
            CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS);
            _CanRender = false;
            RenderMenu();
          });

        menu_levelpacks
        .AddComponent($"<color={_COLOR_GRAY}>level packs - local</color>\n\n");

        // Load list of level packs
        Levels.LevelPacks_InitFolders();
        var levelpacks = System.IO.Directory.GetFiles("Levelpacks/Local");
        for (var i = 0; i < levelpacks.Length; i++)
        {
          var levelpack_name = levelpacks[i].Substring("Levelpacks/Local/".Length).Split('.')[0];
          var extraline = i == levelpacks.Length - 1 ? "\n" : "";

          menu_levelpacks
          .AddComponent($"{levelpack_name}\n{extraline}", MenuComponent.ComponentType.BUTTON_DROPDOWN)
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {

            // Open submenu with options
            var prompt = $"<color={_COLOR_GRAY}>=== level pack: </color>{levelpack_name}\n<color={_COLOR_GRAY}>=== level pack local options</color>\n\n";
            var selections = new List<string>();
            var actions = new List<System.Action<MenuComponent>>();

            bool localfunc_setcurrentpack()
            {
              // Make sure file exists
              var filename = $"{levelpack_name}.levelpack";
              var filename_full = $"Levelpacks/Local/{filename}";
              if (!System.IO.File.Exists(filename_full))
                return false;

              // Load level pack info into level collection
              Levels._LevelPack_Current = new Levels.LevelCollection()
              {
                _name = filename,
                _levelData = Levels.ReadFromFile(filename_full).Split('\n')
              };

              return true;
            }

            // Play level pack
            selections.Add("play");
            actions.Add((MenuComponent component0) =>
            {

              if (localfunc_setcurrentpack())
              {
                Levels._LevelPack_SelectingLevelsFromPack = true;
                Levels._LevelPacks_Play_SaveIndex = _CurrentMenu._dropdownParentIndex;

                CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS);
              }

            });

            // Edit level pack
            selections.Add("edit");
            actions.Add((MenuComponent component0) =>
            {

              if (localfunc_setcurrentpack())
              {
                Levels._LevelPackMenu_SaveIndex = _CurrentMenu._dropdownParentIndex;
                CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS_EDIT);
              }
            });

            // Rename level pack
            selections.Add("rename");
            actions.Add((MenuComponent component0) =>
            {

              if (localfunc_setcurrentpack())
              {
                var rename_dialogue = TileManager.EditorMenus.ShowRenameMenuMenu();
                var save_index = _CurrentMenu._dropdownParentIndex;

                // Get current name
                var level_pack_name = Levels._LevelPack_Current._name.Split('.')[0];

                // Set current name and highlight
                var textarea = rename_dialogue.GetChild(1).GetComponent<TMPro.TMP_InputField>();
                textarea.text = level_pack_name;

                // Set OK button action
                var okbutton = rename_dialogue.GetChild(2).GetChild(0).GetComponent<UnityEngine.UI.Button>();
                okbutton.onClick.RemoveAllListeners();
                okbutton.onClick.AddListener(() =>
                {

                  var gottext = textarea.text.Trim();
                  if (gottext.Length == 0 || !System.Text.RegularExpressions.Regex.IsMatch(gottext, @"^[\w,\s-]+$")) return;

                  // Check no change
                  if (gottext == level_pack_name)
                  {
                    TileManager.EditorMenus.HideMenus();
                    return;
                  }

                  // Rename pack
                  var name_new = $"{gottext}.levelpack";
                  if (System.IO.File.Exists($"{filestructure_local}{name_new}"))
                    return;

                  System.IO.File.Move($"{filestructure_local}{Levels._LevelPack_Current._name}", $"{filestructure_local}{name_new}");
                  Levels._LevelPack_Current._name = name_new;

                  // Reload menu
                  CommonEvents._RemoveDropdownSelections(component0);
                  CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS);
                  Menu2._CurrentMenu._selectionIndex = save_index;
                  Menu2._CurrentMenu._selectedComponent._onSelected?.Invoke(Menu2._CurrentMenu._selectedComponent);
                  _CanRender = false;
                  RenderMenu();

                  // Hide editor menus
                  TileManager.EditorMenus.HideMenus();

                });

                // Cancel button
                var cancelbutton = rename_dialogue.GetChild(2).GetChild(1).GetComponent<UnityEngine.UI.Button>();
                cancelbutton.onClick.RemoveAllListeners();
                cancelbutton.onClick.AddListener(() =>
                {
                  // Hide editor menus
                  TileManager.EditorMenus.HideMenus();
                });
              }
            });

            // Duplicate level pack
            selections.Add("duplicate");
            actions.Add((MenuComponent component0) =>
            {

              if (localfunc_setcurrentpack())
              {
                var fileloc = $"{filestructure_local}{Levels._LevelPack_Current._name}";
                System.IO.File.Copy(fileloc, $"{fileloc.Split('.')[0]}_Copy.levelpack");

                // Reload menu
                var save_index = _CurrentMenu._dropdownParentIndex;
                CommonEvents._RemoveDropdownSelections(component0);
                CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS);
                Menu2._CurrentMenu._selectionIndex = save_index;
                _CanRender = false;
                RenderMenu();
                Menu2._CurrentMenu._selectedComponent._onFocus?.Invoke(Menu2._CurrentMenu._selectedComponent);
              }

            });

            // Delete level pack
            selections.Add("delete - press 4 times");
            actions.Add((MenuComponent component0) =>
            {

              if (localfunc_setcurrentpack())
                if (--Levels._Delete_Iter == 0)
                {

                  var fileloc = $"{filestructure_local}{Levels._LevelPack_Current._name}";

                  var trashed_files = new System.IO.DirectoryInfo($"{filestructure_local}trashed/").GetFiles().OrderBy(p => p.LastWriteTime).ToArray();
                  if (trashed_files.Length > 10)
                    System.IO.File.Delete(trashed_files[0].FullName);

                  var now = System.DateTime.Now;
                  System.IO.File.Move(fileloc, $"{filestructure_local}trashed/{Levels._LevelPack_Current._name.Split('.')[0]}-{now.Year}-{now.Month}-{now.Day}--{now.Hour}-{now.Minute}-{now.Second}.levelpack");

                  // Reload menu
                  var save_index = _CurrentMenu._dropdownParentIndex;
                  CommonEvents._RemoveDropdownSelections(component0);
                  CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS);
                  Menu2._CurrentMenu._selectionIndex = save_index - 1;
                  _CanRender = false;
                  RenderMenu();
                  Menu2._CurrentMenu._selectedComponent._onFocus?.Invoke(Menu2._CurrentMenu._selectedComponent);
                }

            });

            // Upload level pack
            if (Levels._LevelPack_Current != null && Levels._LevelPack_Current._levelData.Length == 0)
            {
              selections.Add("upload to workshop - cannot upload empty level pack\n");
              actions.Add((MenuComponent component0) => { });
            }
            else
            {
              selections.Add("upload to workshop\n");
              actions.Add((MenuComponent component0) =>
              {

                if (localfunc_setcurrentpack())
                {
                  Levels._LevelPack_UploadingToWorkshop = true;

                  // Reload menu
                  CommonEvents._RemoveDropdownSelections(component0);
                  CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS);
                  _CanRender = false;
                  RenderMenu();

                }

              });
            }

            // Set dropdown data
            component.SetDropdownData(prompt, selections, actions, "play");

          })
          .AddEvent(EventType.ON_SELECTED, (MenuComponent component) =>
          {
            Levels._Delete_Iter = 4;
          });

        }

      }
      else
      {

        menu_levelpacks
        .AddComponent($"<color={_COLOR_GRAY}>level editor - choose level pack to overwrite</color>\n\n");

      }

      // Display Steam Workshop-published level packs
      menu_levelpacks
      .AddComponent($"<color={_COLOR_GRAY}>level packs - steam workshop - PUBLISHED</color>\n\n");
      /*.AddEvent((MenuComponent component) =>
      {

        // Open to published files
        try
        {
          Steamworks.SteamFriends.ActivateGameOverlayToWebPage($"https://steamcommunity.com/profiles/{Steamworks.SteamUser.GetSteamID()}/myworkshopfiles/?appid=954010&sort=score&browsefilter=myfiles&view=imagewall");
        }
        catch (System.Exception e)
        {
          Debug.LogError(e.ToString());
        }

      });*/

      if (Levels._LevelPack_UploadingToWorkshop)
        menu_levelpacks
        .AddComponent($"upload as new workshop item\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {

            // Gather workshop info
            TileManager.EditorMenus.ShowWorkshopMenu();
            var menu = TileManager.EditorMenus._Menu_Workshop_Infos;

            menu.GetChild(1).GetComponent<TMPro.TMP_InputField>().text = "";
            menu.GetChild(2).GetComponent<TMPro.TMP_InputField>().text = "";

            var button_ok = menu.GetChild(3).GetChild(0).GetComponent<UnityEngine.UI.Button>();
            var button_no = menu.GetChild(3).GetChild(1).GetComponent<UnityEngine.UI.Button>();

            button_ok.onClick.RemoveAllListeners();
            button_ok.onClick.AddListener(() =>
            {

              var title = menu.GetChild(1).GetComponent<TMPro.TMP_InputField>().text.Trim();
              var desc = menu.GetChild(2).GetComponent<TMPro.TMP_InputField>().text.Trim();

              if (title.Length == 0 || !System.Text.RegularExpressions.Regex.IsMatch(title, @"^[\w,\s-]+$")) return;
              //if (desc.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(desc, @"^[\w,\s-]+$")) return;

              // Make sure file exists / copy
              var filename = Levels._LevelPack_Current._name;
              var filename_full = $"Levelpacks/Local/{filename}";
              if (!System.IO.File.Exists(filename_full))
                return;

              // Create dir
              if (System.IO.Directory.Exists($"{filestructure_workshop}{title}"))
                System.IO.Directory.Delete($"{filestructure_workshop}{title}", true);
              System.IO.Directory.CreateDirectory($"{filestructure_workshop}{title}");
              System.IO.File.Copy(filename_full, $"{filestructure_workshop}{title}/{filename}");

              // Copy file to workshop
              SteamManager.Workshop_CreateNew(new SteamManager.SteamWorkshopItem()
              {
                _title = title,
                _description = desc,
                _filelocation = $"{System.IO.Directory.GetCurrentDirectory()}/{filestructure_workshop}{title}"
              });

              // Hide menu
              menu.gameObject.SetActive(false);

              // Reload menus
              Levels._LevelPack_UploadingToWorkshop = false;
              CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS);
              _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable.Count - 2;
              _CanRender = false;
              RenderMenu();
              SendInput(Input.SPACE);
              SendInput(Input.SPACE);
            });

            button_no.onClick.RemoveAllListeners();
            button_no.onClick.AddListener(() =>
            {

              menu.gameObject.SetActive(false);

            });


          });

      if (SteamManager._PublishedItems != null && SteamManager._PublishedItems.Count > 0)
      {
        var i = 0;
        foreach (var item_p in SteamManager._PublishedItems)
        {

          var lastline = i++ == SteamManager._PublishedItems.Count - 1 ? "\n" : "";

          // Dropdown options for published items
          menu_levelpacks
          .AddComponent($"{item_p.Value.m_rgchTitle}\n{lastline}", MenuComponent.ComponentType.BUTTON_DROPDOWN)
            .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
            {

              // Open submenu with options
              var prompt = $"<color={_COLOR_GRAY}>=== level pack: </color>{item_p.Value.m_pchFileName}\n<color={_COLOR_GRAY}>=== level pack options</color>\n\n";
              var selections = new List<string>();
              var actions = new List<System.Action<MenuComponent>>();
              var first_selection = "play";

              if (Levels._LevelPack_UploadingToWorkshop)
              {
                prompt = $"<color={_COLOR_GRAY}>=== level pack: </color>{item_p.Value.m_rgchTitle}\n<color={_COLOR_GRAY}>=== overwrite level pack?</color>\n\n";

                selections.Add("overwrite");
                actions.Add((MenuComponent component0) =>
                {

                  // Make sure file exists / copy
                  var filename = Levels._LevelPack_Current._name;
                  var filename_full = $"Levelpacks/Local/{filename}";
                  if (!System.IO.File.Exists(filename_full))
                    return;

                  var title = item_p.Value.m_rgchTitle;
                  var desc = item_p.Value.m_rgchDescription;

                  // Create dir
                  if (System.IO.Directory.Exists($"{filestructure_workshop}{title}"))
                    System.IO.Directory.Delete($"{filestructure_workshop}{title}", true);
                  System.IO.Directory.CreateDirectory($"{filestructure_workshop}{title}");
                  // Copy file
                  System.IO.File.Copy(filename_full, $"{filestructure_workshop}{title}/{filename}");

                  // Copy file to workshop
                  SteamManager.Workshop_Update(item_p.Value.m_nPublishedFileId, new SteamManager.SteamWorkshopItem()
                  {
                    _filelocation = $"{System.IO.Directory.GetCurrentDirectory()}/{filestructure_workshop}{title}"
                  });

                  // Reload menus
                  Levels._LevelPack_UploadingToWorkshop = false;
                  CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS);
                  _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable.Count - 2;
                  _CanRender = false;
                  RenderMenu();
                  SendInput(Input.SPACE);
                  SendInput(Input.SPACE);
                });

                // Set dropdown data
                component.SetDropdownData(prompt, selections, actions, "overwrite");
              }
              else
              {

                var is_installed = SteamManager.Workshop_GetInstalledLocation(item_p.Value.m_nPublishedFileId) != null;
                if (!is_installed)
                {
                  first_selection = "not subscribed to content\n";
                  selections.Add("not subscribed to content\n");
                  actions.Add((MenuComponent component0) =>
                  {
                  });
                }
                else
                {
                  selections.Add("play\n");
                  actions.Add((MenuComponent component0) =>
                  {

                    // Gather install location from Steam
                    var file_loc = SteamManager.Workshop_GetInstalledLocation(item_p.Value.m_nPublishedFileId);
                    if (file_loc == null)
                      return;

                    // Load level pack info into level collection
                    var workshop_folder = System.IO.Directory.GetFiles(file_loc);
                    if (workshop_folder.Length != 1) return;

                    var filename = workshop_folder[0];
                    var filename_full = $"{filename}";
                    Levels._LevelPack_Current = new Levels.LevelCollection()
                    {
                      _name = filename,
                      _levelData = Levels.ReadFromFile(filename_full).Split('\n')
                    };

                    // Load map
                    Levels._LevelPack_SelectingLevelsFromPack = true;
                    Levels._LevelPacks_Play_SaveIndex = _CurrentMenu._dropdownParentIndex;

                    CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS);
                  });
                }

                // Set dropdown data
                component.SetDropdownData(prompt, selections, actions, first_selection);
              }

            });
        }
      }
      else
        menu_levelpacks
        .AddComponent($"no published maps\n\n");

      // Display Steam Workshop-subscribed level packs
      if (!Levels._LevelPack_UploadingToWorkshop)
      {
        menu_levelpacks
        .AddComponent($"<color={_COLOR_GRAY}>level packs - steam workshop - SUBSCRIBED </color>\n\n");
        /*.AddEvent((MenuComponent component) =>
        {
          // Open to workshop files
          try
          {
            Steamworks.SteamFriends.ActivateGameOverlayToWebPage($"https://steamcommunity.com/profiles/{Steamworks.SteamUser.GetSteamID()}/myworkshopfiles/?appid=954010&sort=score&browsefilter=mysubscriptions&view=imagewall&browsesort=mysubscriptions&p=1");
          }
          catch (System.Exception e)
          {
            Debug.LogError(e.ToString());
          }
        });*/
        if (SteamManager._SubscribedItems != null && SteamManager._SubscribedItems.Count > 0)
        {
          var i = 0;
          foreach (var item_p in SteamManager._SubscribedItems)
          {

            var lastline = i++ == SteamManager._PublishedItems.Count - 1 ? "\n" : "";

            menu_levelpacks
            .AddComponent($"{item_p.Value.m_rgchTitle}\n{lastline}", MenuComponent.ComponentType.BUTTON_DROPDOWN)
              .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
              {

                // Open submenu with options
                var prompt = $"<color={_COLOR_GRAY}>=== level pack: </color>{item_p.Value.m_pchFileName}\n<color={_COLOR_GRAY}>=== level pack options</color>\n\n";
                var selections = new List<string>();
                var actions = new List<System.Action<MenuComponent>>();
                var first_selection = "play";

                var is_installed = SteamManager.Workshop_GetInstalledLocation(item_p.Value.m_nPublishedFileId) != null;
                if (!is_installed)
                {
                  first_selection = "subscribed content not installed\n";
                  selections.Add("subscribed content not installed\n");
                  actions.Add((MenuComponent component0) =>
                  {
                  });
                }
                else
                {
                  selections.Add("play\n");
                  actions.Add((MenuComponent component0) =>
                  {

                    // Gather install location from Steam
                    var file_loc = SteamManager.Workshop_GetInstalledLocation(item_p.Value.m_nPublishedFileId);
                    if (file_loc == null)
                      return;

                    // Load level pack info into level collection
                    var workshop_folder = System.IO.Directory.GetFiles(file_loc);
                    if (workshop_folder.Length != 1) return;

                    var filename = workshop_folder[0];
                    var filename_full = $"{filename}";
                    Levels._LevelPack_Current = new Levels.LevelCollection()
                    {
                      _name = filename,
                      _levelData = Levels.ReadFromFile(filename_full).Split('\n')
                    };

                    // Load map
                    Levels._LevelPack_SelectingLevelsFromPack = true;
                    Levels._LevelPacks_Play_SaveIndex = _CurrentMenu._dropdownParentIndex;

                    CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS);
                  });
                }

                // Set dropdown data
                component.SetDropdownData(prompt, selections, actions, first_selection);
              });

          }
        }
        else
          menu_levelpacks
          .AddComponent($"no subscribed maps\n\n");

        menu_levelpacks
        .AddComponent("open steam workshop\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {

            // Open to workshop files
            try
            {
              Steamworks.SteamFriends.ActivateGameOverlayToWebPage($"https://steamcommunity.com/app/954010/workshop/");
            }
            catch (System.Exception e)
            {
              Debug.LogError(e.ToString());
            }
          })
        .AddComponent("reload local / workshop content\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {

            // Load workshop items
            SteamManager.Workshop_GetUserItems();

            // Reload menu
            IEnumerator reloaddelay()
            {
              yield return new WaitForSecondsRealtime(1f);

              CommonEvents._RemoveDropdownSelections(_CurrentMenu._selectedComponent);
              CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS);

              _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable.Count - 3;
              _CanRender = false;
              RenderMenu();
            }
            GameScript._Singleton.StartCoroutine(reloaddelay());
          })
        /*.AddComponent("open deleted levels / packs backup\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {
            if (System.IO.Directory.Exists($"{filestructure_local}trashed"))
              System.Diagnostics.Process.Start(new System.IO.DirectoryInfo($"{filestructure_local}trashed").FullName);
          })*/

        // Return to editor main menu
        .AddBackButton(MenuType.EDITOR_MAIN);

      }
      else
      {

        menu_levelpacks.AddBackButton(MenuType.EDITOR_PACKS)
        .AddEvent((MenuComponent component) =>
        {
          Levels._LevelPack_UploadingToWorkshop = false;
          CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS);
          RenderMenu();
        });

      }

      // Load on switch
      menu_levelpacks._onSwitchTo += () =>
      {
        SpawnMenu_LevelEditorPacks();

        // Stop editing loadouts
        Levels._HardcodedLoadout = null;
        Levels._EditingLoadout = false;
      };

#endif
    }
    SpawnMenu_LevelEditorPacks();

    // Edit level packs
    void SpawnMenu_LevelPacks_Edit()
    {

#if UNITY_STANDALONE
      var menu_editpacks = new Menu2(MenuType.EDITOR_PACKS_EDIT);
      // Reload menu on switch
      menu_editpacks._onSwitchTo += () =>
      {
        SpawnMenu_LevelPacks_Edit();

        // Set current level collection to editor levels
        Levels._CurrentLevelCollectionIndex = 3;
        GameScript._GameMode = GameScript.GameModes.CLASSIC;

        // Set up loadout editing
        if (Levels._HardcodedLoadout == null)
          Levels._HardcodedLoadout = new GameScript.ItemManager.Loadout()
          {
            _id = -1,
            _equipment = new GameScript.PlayerProfile.Equipment()
          };
        Levels._EditingLoadout = true;
      };

      // Check for preload
      if (Levels._LevelPack_Current == null)
      {
        menu_editpacks.AddComponent($"lol\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE);
        return;
      }

      Debug.Log(Levels._IsOverwritingLevel);
      Debug.Log(Levels._IsReorderingLevel);
      Debug.Log(Levels._LevelPack_Current._levelData.Length == Levels._LEVELPACK_MAX);

      // Start menu
      if (Levels._IsOverwritingLevel)
        menu_editpacks
        .AddComponent($"<color={_COLOR_GRAY}>level pack - {Levels._LevelPack_Current._name.Split('.')[0]}</color>\n<color={_COLOR_GRAY}>level pack - replacing level</color>\n\n")
        .AddComponent($"<color={_COLOR_GRAY}>--- select level to replace with</color>\n\n");
      else if (Levels._IsReorderingLevel)
        menu_editpacks
        .AddComponent($"<color={_COLOR_GRAY}>level pack - {Levels._LevelPack_Current._name.Split('.')[0]}</color>\n<color={_COLOR_GRAY}>level pack - insert level before</color>\n\n")
        .AddComponent($"<color={_COLOR_GRAY}>--- select level to insert before</color>\n\n");
      else
      {
        menu_editpacks
        .AddComponent($"<color={_COLOR_GRAY}>level pack - {Levels._LevelPack_Current._name.Split('.')[0]}</color>\n<color={_COLOR_GRAY}>level pack - edit options</color>\n\n");

        // Add level to pack
        if (Levels._LevelPack_Current._levelData.Length == Levels._LEVELPACK_MAX)
          menu_editpacks.AddComponent($"add level to pack - MAX LEVELS PER PACK REACHED\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE);
        else
          menu_editpacks.AddComponent($"add level to pack\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
            .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
            {

              // Open submenu with options
              var prompt = $"<color={_COLOR_GRAY}>=== select level to add to pack</color>\n\n";
              var selections = new List<string>();
              var actions = new List<System.Action<MenuComponent>>();
              var actions_onfocus = new List<System.Action<MenuComponent>>();
              var actions_onblur = new List<System.Action<MenuComponent>>();

              var firstselection = "";

              if (Levels._CurrentLevelCollection._levelData.Length == 0)
              {

                firstselection = "no levels to add - switch to level creator";

                selections.Add($"{firstselection}");
                actions.Add((MenuComponent component0) => { CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS); });
                actions_onfocus.Add((MenuComponent component0) => { });
                actions_onblur.Add((MenuComponent component0) => { });
              }

              else
              {

                // Add dropdown action per level to select
                for (var i = 0; i < Levels._CurrentLevelCollection._levelData.Length; i++)
                {

                  var lastline = i == Levels._CurrentLevelCollection._levelData.Length - 1 ? "\n" : "";

                  var level_data = Levels._CurrentLevelCollection._levelData[i];
                  var level_meta = Levels.GetLevelMeta(level_data);
                  var level_name = level_meta[1];

                  if (level_name == null)
                  {
                    if (i == 0) firstselection = "corrupted level data";

                    selections.Add($"corrupted level data{lastline}");
                    actions.Add((MenuComponent component0) => { });
                    actions_onfocus.Add((MenuComponent component0) =>
                    {
                      if (GameScript._lp0 != null && _CurrentMenu._dropdownCount == 0)
                        GameObject.Destroy(GameScript._lp0.gameObject);
                    });
                    actions_onblur.Add((MenuComponent component0) => { });
                    continue;
                  }


                  if (i == 0) firstselection = level_name;

                  // Append level to level pack
                  selections.Add($"{level_name}{lastline}");
                  actions.Add((MenuComponent component0) =>
                  {

                    var save_selection = _CurrentMenu._selectionIndex;
                    var leveldata_save = level_data;

                    if (Levels._LevelPack_Current._levelData == null || Levels._LevelPack_Current._levelData.Length == 0 || (Levels._LevelPack_Current._levelData.Length == 1 && Levels._LevelPack_Current._levelData[0].Trim() == ""))
                      Levels._LevelPack_Current._levelData = new string[] { leveldata_save };
                    else
                    {
                      System.Array.Resize(ref Levels._LevelPack_Current._levelData, Levels._LevelPack_Current._levelData.Length + 1);
                      Levels._LevelPack_Current._levelData[Levels._LevelPack_Current._levelData.Length - 1] = level_data;
                    }

                    // Write to file
                    Levels.LevelPack_Save();

                    // Reload menu
                    if (Levels._LevelPack_Current._levelData.Length < Levels._LEVELPACK_MAX)
                    {
                      var save_index = _CurrentMenu._selectionIndex;
                      CommonEvents._RemoveDropdownSelections(component0);
                      CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS_EDIT);
                      _CanRender = false;
                      RenderMenu();
                      Menu2.SendInput(Input.SPACE);
                      Menu2._CurrentMenu._selectionIndex = save_index + 1;
                      Menu2._CurrentMenu._selectedComponent._onFocus?.Invoke(Menu2._CurrentMenu._selectedComponent);
                      _CanRender = false;
                      RenderMenu();
                    }
                    else
                    {
                      CommonEvents._RemoveDropdownSelections(component0);
                      CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS_EDIT);
                      Menu2._CurrentMenu._selectionIndex = 0;
                      Menu2._CurrentMenu._selectedComponent._onFocus?.Invoke(Menu2._CurrentMenu._selectedComponent);
                      _CanRender = false;
                      RenderMenu();
                    }
                  });

                  // Focus / blur remove level preview
                  actions_onfocus.Add((MenuComponent component0) =>
                      {
                        GameScript._lp0 = TileManager.GetMapPreview(level_data).transform;
                      });
                  actions_onblur.Add((MenuComponent component0) =>
                  {
                    if (GameScript._lp0 != null && _CurrentMenu._dropdownCount == 0)
                      GameObject.Destroy(GameScript._lp0.gameObject);
                  });
                }
              }

              // Set dropdown data
              component.SetDropdownData(prompt, selections, actions, firstselection, null, null, actions_onfocus, actions_onblur);

            });
      }

      // Show level options
      //menu_editpacks
      //.AddComponent($"<color={_COLOR_GRAY}>level - levelname</color>\n<color={_COLOR_GRAY}>level pack - edit options</color>\n\n");

      if (Levels._IsOverwritingLevel)
      {

        // Show levels
        var levelpack_length = Levels._CurrentLevelCollection._levelData.Length;

        if (levelpack_length == 0)
        {

          menu_editpacks
          .AddComponent($"no levels created\n\n");

        }
        else
          for (var i = 0; i < levelpack_length; i++)
          {

            var lineend = i == levelpack_length - 1 ? "\n" : "";

            var level_data = Levels._CurrentLevelCollection._levelData[i];
            var level_meta = Levels.GetLevelMeta(level_data);
            var level_name = level_meta[1];

            menu_editpacks
            .AddComponent($"{level_name}\n{lineend}", MenuComponent.ComponentType.BUTTON_SIMPLE)
              .AddEvent((MenuComponent component) =>
              {

                // Load map preview
                if (_CurrentMenu._hasDropdown)
                {
                  if (GameScript._lp0 != null && _CurrentMenu._dropdownCount == 0)
                    GameObject.Destroy(GameScript._lp0.gameObject);
                  level_data = Levels._CurrentLevelCollection._levelData[component._buttonIndex];
                  GameScript._lp0 = TileManager.GetMapPreview(level_data).transform;

                  level_meta = Levels.GetLevelMeta(level_data);
                  Levels.GetHardcodedLoadout(level_meta[2]);
                }

                // Overwrite level pack level with data
                Levels._IsOverwritingLevel = false;

                //Debug.Log($"Replacing in level pack index {Levels._LevelPack_SaveReorderIndex} with level editor map index {component._buttonIndex}");
                var levelmeta_old = Levels.GetLevelMeta(Levels._LevelPack_Current._levelData[Levels._LevelPack_SaveReorderIndex]);
                var levelmeta_new = Levels.GetLevelMeta(Levels._CurrentLevelCollection._levelData[component._buttonIndex]);
                levelmeta_new[2] = levelmeta_old[2];
                Levels._LevelPack_Current._levelData[Levels._LevelPack_SaveReorderIndex] = Levels.ParseLevelMeta(levelmeta_new);
                Levels.LevelPack_Save();

                // Render menu
                CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS_EDIT);
                _CurrentMenu._selectionIndex = Levels._LevelPack_SaveReorderIndex + 1;
                _CanRender = false;
                RenderMenu();
                SendInput(Input.SPACE);
                _CanRender = false;
                RenderMenu();
                SendInput(Input.UP);
                SendInput(Input.UP);

                // Dialog
                SteamManager.SteamMenus.ShowInformationDialogue("Level overwritten");
              })

              // Add / remove level preview
              .AddEvent(EventType.ON_FOCUS, (MenuComponent component) =>
              {

                // Load map preview
                if (!_CurrentMenu._hasDropdown)
                {
                  if (GameScript._lp0 != null && _CurrentMenu._dropdownCount == 0)
                    GameObject.Destroy(GameScript._lp0.gameObject);
                  level_data = Levels._CurrentLevelCollection._levelData[component._buttonIndex];
                  GameScript._lp0 = TileManager.GetMapPreview(level_data).transform;

                  level_meta = Levels.GetLevelMeta(level_data);
                  Levels.GetHardcodedLoadout(level_meta[2]);
                }
              })
              .AddEvent(EventType.ON_UNFOCUS, (MenuComponent component) =>
              {

                // Unload map preview
                if (GameScript._lp0 != null && _CurrentMenu._dropdownCount == 0 && !_CurrentMenu._hasDropdown)
                  GameObject.Destroy(GameScript._lp0.gameObject);
              });
          }

      }
      else
      {

        // Show levels
        var levelpack_length = Levels._LevelPack_Current._levelData.Length;
        levelpack_length = levelpack_length < Levels._LEVELPACK_MAX ? levelpack_length : Levels._LEVELPACK_MAX;
        var added_levels = 0;
        for (var i = 0; i < levelpack_length; i++)
        {

          var lineend = i == levelpack_length - 1 ? "\n" : "";

          var level_data = Levels._LevelPack_Current._levelData[i];
          var level_meta = Levels.GetLevelMeta(level_data);
          var level_name = level_meta[1];

          if (level_data.Length == 0)
            continue;
          else if (level_name == null)
          {

            menu_editpacks
            .AddComponent($"corrupted level data\n{lineend}", MenuComponent.ComponentType.BUTTON_SIMPLE);
            continue;
          }
          added_levels++;

          // Select level to insert before
          if (Levels._IsReorderingLevel)
            menu_editpacks
            .AddComponent($"{level_name}\n{lineend}", MenuComponent.ComponentType.BUTTON_SIMPLE)
              .AddEvent(EventType.ON_SELECTED, (MenuComponent component) =>
              {

                Levels._IsReorderingLevel = false;

                // Reorder array
                var reorderindex = _CurrentMenu._selectionIndex;
                if (reorderindex != Levels._LevelPack_SaveReorderIndex)
                {

                  var leveldata0 = Levels._LevelPack_Current._levelData[Levels._LevelPack_SaveReorderIndex];
                  var leveldata1 = Levels._LevelPack_Current._levelData[reorderindex];

                  var newleveldata = new List<string>();
                  for (var u = 0; u < Levels._LevelPack_Current._levelData.Length; u++)
                  {

                    if (u == reorderindex)
                      newleveldata.Add(leveldata0);
                    else if (u == Levels._LevelPack_SaveReorderIndex)
                      continue;

                    newleveldata.Add(Levels._LevelPack_Current._levelData[u]);

                  }
                  Levels._LevelPack_Current._levelData = newleveldata.ToArray();
                  Levels.LevelPack_Save();
                }

                // Render menu
                SpawnMenu_LevelPacks_Edit();
                var selectionindex = Levels._LevelPack_SaveReorderIndex > reorderindex ? reorderindex + 1 : reorderindex;
                _CurrentMenu._selectionIndex = selectionindex == 0 ? 1 : selectionindex;
                _CanRender = false;
                RenderMenu();
                SendInput(Input.SPACE);
              });

          // Show level edit selctions
          else
            menu_editpacks
            .AddComponent($"{level_name}\n{lineend}", MenuComponent.ComponentType.BUTTON_DROPDOWN)
              .AddEvent((MenuComponent component) =>
              {
                // Load map preview
                if (_CurrentMenu._hasDropdown)
                {
                  if (GameScript._lp0 != null && _CurrentMenu._dropdownCount == 0)
                    GameObject.Destroy(GameScript._lp0.gameObject);
                  level_data = Levels._LevelPack_Current._levelData[component._buttonIndex - 1];
                  GameScript._lp0 = TileManager.GetMapPreview(level_data).transform;

                  level_meta = Levels.GetLevelMeta(level_data);
                  Levels.GetHardcodedLoadout(level_meta[2]);
                }
              })
              .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
              {

                // Open submenu with options
                var prompt = $"<color={_COLOR_GRAY}>=== level options</color>\n\n";
                var selections = new List<string>();
                var actions = new List<System.Action<MenuComponent>>();

                // Remove from level list
                selections.Add("remove");
                actions.Add((MenuComponent component0) =>
                {

                  var saveindex = _CurrentMenu._dropdownParentIndex - 1;

                  if (Levels._LevelPack_Current._levelData.Length == 1)
                    Levels._LevelPack_Current._levelData = new string[] { };
                  else
                  {
                    var leveldata_new = new string[Levels._LevelPack_Current._levelData.Length - 1];
                    System.Array.Copy(Levels._LevelPack_Current._levelData, leveldata_new, saveindex);
                    System.Array.Copy(Levels._LevelPack_Current._levelData, saveindex + 1, leveldata_new, saveindex, Levels._LevelPack_Current._levelData.Length - (saveindex + 1));
                    Levels._LevelPack_Current._levelData = leveldata_new;
                  }

                  // Write to file
                  Levels.LevelPack_Save();

                  // Reload menu
                  var save_index = _CurrentMenu._dropdownParentIndex;
                  CommonEvents._RemoveDropdownSelections(component0);
                  CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS_EDIT);
                  _CanRender = false;
                  RenderMenu();
                  _CurrentMenu._selectionIndex = save_index == _CurrentMenu._menuComponentsSelectable.Count - 1 ? save_index - 1 : save_index;
                  _CurrentMenu._selectedComponent._onFocus?.Invoke(_CurrentMenu._selectedComponent);
                  _CanRender = false;
                  RenderMenu();

                });

                // Replace with map
                selections.Add("overwrite");
                actions.Add((MenuComponent component0) =>
                {

                  Levels._IsOverwritingLevel = true;
                  Levels._LevelPack_SaveReorderIndex = _CurrentMenu._dropdownParentIndex - 1;

                  CommonEvents._RemoveDropdownSelections(component0);
                  CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS_EDIT);
                  _CurrentMenu._selectionIndex = 0;
                  _CanRender = false;
                  RenderMenu();
                  _CurrentMenu._selectedComponent?._onFocus?.Invoke(_CurrentMenu._selectedComponent);

                });

                // Change theme
                var level_theme = "unset";
                if (level_meta[3] != null)
                {
                  var levelthemeindex = int.Parse(level_meta[3]);
                  if (levelthemeindex > -1)
                  {
                    level_theme = SceneThemes._SceneOrder_LevelEditor[levelthemeindex % SceneThemes._SceneOrder_LevelEditor.Length];
                  }
                }

                selections.Add($"set theme - {level_theme}");
                actions.Add((MenuComponent component0) =>
                {

                  if (level_meta[3] == null)
                    level_meta[3] = "0";
                  else
                    level_meta[3] = ((int.Parse(level_meta[3]) + 1) % SceneThemes._SceneOrder_LevelEditor.Length) + "";

                  Levels._LevelPack_Current._levelData[_CurrentMenu._dropdownParentIndex - 1] = Levels.ParseLevelMeta(level_meta);
                  Levels.LevelPack_Save();

                  var saveindex = _CurrentMenu._dropdownParentIndex;
                  CommonEvents._RemoveDropdownSelections(component0);
                  CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS_EDIT);
                  _CurrentMenu._selectionIndex = saveindex;
                  _CanRender = false;
                  RenderMenu();
                  SendInput(Input.SPACE);
                  _CanRender = false;
                  RenderMenu();
                  SendInput(Input.UP);
                });

                // Reorder
                selections.Add("reorder - insert before");
                actions.Add((MenuComponent component0) =>
                {

                  Levels._IsReorderingLevel = true;
                  Levels._LevelPack_SaveReorderIndex = _CurrentMenu._dropdownParentIndex - 1;

                  CommonEvents._RemoveDropdownSelections(component0);
                  CommonEvents._SwitchMenu(MenuType.EDITOR_PACKS_EDIT);
                  _CurrentMenu._selectionIndex = Levels._LevelPack_SaveReorderIndex;
                  _CanRender = false;
                  RenderMenu();

                });

                // Set hard loadout
                selections.Add("set loadout - " + (CurrentLoadout()._equipment.IsEmpty() ? "not set, using CLASSIC mode loadouts" : "set, using hard-set loadout"));
                actions.Add((MenuComponent component0) =>
                {

                  // Start editing loadout
                  Levels._LoadoutEdit_SaveIndex = _CurrentMenu._dropdownParentIndex - 1;

                  if (GameScript._lp0 != null)
                    GameObject.Destroy(GameScript._lp0.gameObject);

                  CommonEvents._SwitchMenu(MenuType.EDIT_LOADOUT);

                });

                // Download level pack to local storage
                selections.Add("download to local levels");
                actions.Add((MenuComponent component0) =>
                {

                  var level_data = Levels._LevelPack_Current._levelData[Menu2._CurrentMenu._dropdownParentIndex - 1];

                  var savelevelcollection = Levels._CurrentLevelCollectionIndex;
                  Levels._CurrentLevelCollectionIndex = 3;
                  if (Levels._CurrentLevelCollection_Name != "levels_editor_local")
                  {
                    Debug.LogError("Trying to save local levels to wrong level collection");
                    return;
                  }

                  System.Array.Resize(ref Levels._CurrentLevelCollection._levelData, Levels._CurrentLevelCollection._levelData.Length + 1);
                  Levels._CurrentLevelCollection._levelData[Levels._CurrentLevelCollection._levelData.Length - 1] = level_data;
                  Levels.SaveLevels();

                  Levels._CurrentLevelCollectionIndex = savelevelcollection;

                  // Remove dropdown for feedback
                  var saveindex = _CurrentMenu._dropdownParentIndex;
                  CommonEvents._RemoveDropdownSelections(component0);
                  _CurrentMenu._selectionIndex = saveindex;
                  _CanRender = true;
                  RenderMenu();

                  // Show feedback
                  SteamManager.SteamMenus.ShowInformationDialogue("Level downloaded to local levels");
                });

                // Set dropdown data
                component.SetDropdownData(prompt, selections, actions, "reorder - insert before");

              })

              // Add / remove level preview
              .AddEvent(EventType.ON_FOCUS, (MenuComponent component) =>
              {

                // Load map preview
                if (!_CurrentMenu._hasDropdown)
                {
                  if (GameScript._lp0 != null && _CurrentMenu._dropdownCount == 0)
                    GameObject.Destroy(GameScript._lp0.gameObject);
                  level_data = Levels._LevelPack_Current._levelData[component._buttonIndex - 1];
                  GameScript._lp0 = TileManager.GetMapPreview(level_data).transform;

                  level_meta = Levels.GetLevelMeta(level_data);
                  Levels.GetHardcodedLoadout(level_meta[2]);
                }
              })
              .AddEvent(EventType.ON_UNFOCUS, (MenuComponent component) =>
              {

                // Unload map preview
                if (GameScript._lp0 != null && _CurrentMenu._dropdownCount == 0 && !_CurrentMenu._hasDropdown)
                  GameObject.Destroy(GameScript._lp0.gameObject);
              });

        }

        // Check for no levels loaded
        if (added_levels == 0)
          menu_editpacks.AddComponent("empty level pack\n\n");

      }

      // Return to editor packs menu
      if (Levels._IsReorderingLevel || Levels._IsOverwritingLevel)
        menu_editpacks
        .AddBackButton(MenuType.EDITOR_PACKS_EDIT)
          .AddEvent(EventType.ON_SELECTED, (MenuComponent component) =>
          {
            if (GameScript._lp0 != null)
              GameObject.Destroy(GameScript._lp0.gameObject);

            var wasoverwriting = Levels._IsOverwritingLevel;
            Levels._IsReorderingLevel = Levels._IsOverwritingLevel = false;

            SpawnMenu_LevelPacks_Edit();
            _CurrentMenu._selectionIndex = Levels._LevelPack_SaveReorderIndex + 1;
            _CanRender = false;
            RenderMenu();
            SendInput(Input.SPACE);
            if (wasoverwriting)
            {
              SendInput(Input.UP);
              SendInput(Input.UP);
            }
          });
      else
        menu_editpacks
        .AddBackButton(MenuType.EDITOR_PACKS)
          .AddEvent(EventType.ON_SELECTED, (MenuComponent component) =>
          {
            if (GameScript._lp0 != null)
              GameObject.Destroy(GameScript._lp0.gameObject);

            _CurrentMenu._selectionIndex = Levels._LevelPackMenu_SaveIndex;
            _CanRender = false;
            RenderMenu();
            Menu2.SendInput(Input.SPACE);
            Menu2.SendInput(Input.DOWN);
          });
#endif
    }
    SpawnMenu_LevelPacks_Edit();

    // Social
    new Menu2(MenuType.SOCIAL)
    .AddComponent($"<color={_COLOR_GRAY}>social</color>\n\n")
    // Open reddit page
    .AddComponent("reddit\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component0) => { Application.OpenURL("https://reddit.com/r/boxedworksGames/"); })
    // Open twitter page
    .AddComponent("twitter\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component0) => { Application.OpenURL("https://www.twitter.com/boxedworks"); })
    // Open discord channel
    .AddComponent("discord\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component0) => { Application.OpenURL("https://discord.gg/gTa8dmc"); })
    // Open music source
    .AddComponent("music\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component0) => { Application.OpenURL("https://incompetech.com/music/royalty-free/?genre=Jazz"); })
    .AddBackButton(MenuType.MAIN);
    // Tip
    ModifyMenu_TipComponents(MenuType.SOCIAL, 15, 1);
    ModifyMenu_TipSwitch(MenuType.SOCIAL);

    // Shop menu
    void SpawnMenu_Shop()
    {

      // Menu components
      var format_shop = "<color={4}>{0,-26} {1,-42}</color> <color={5}>{2,-10}</color> <color={6}>{3,-10}</color>";
      var format_shop2 = $"</color>{format_shop}<color=white>";
      var m = new Menu2(MenuType.SHOP)
      {

      }
      .AddComponent($"<color={_COLOR_GRAY}>shop</color>\n\n")

      // Show available $$$
      .AddComponent("available ($$$): 10\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
        {
          if (component._focused)
            component.SetDisplayText($"</color><color={_COLOR_GRAY}>available ($$$): {Shop._AvailablePoints}</color>      <-- get ($$$) by completing new CLASSIC levels<color=white>\n");
          else
          {
            var color = Shop._AvailablePoints > 0 ? "yellow" : "red";
            component.SetDisplayText($"available ($$$): </color><color={color}>{Shop._AvailablePoints}</color><color=white>\n");
          }
        })
        .SetSelectorType(MenuComponent.SelectorType.QUESTION)

      // Show available equipment points
      .AddComponent("max equipment points: 3\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
        {
          var points = GameScript.ItemManager.Loadout._POINTS_MAX;
          if (component._focused)
            component.SetDisplayText($"</color><color={_COLOR_GRAY}>max equipment points: {points}</color> <-- the higher the number the more things you can equip<color=white>\n\n");
          else
            component.SetDisplayText($"max equipment points: </color><color=yellow>{points}</color><color=white>\n\n");
        })
        .SetSelectorType(MenuComponent.SelectorType.QUESTION);

      // Show shop filter
      if (Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART1))
        m.AddComponent("filter: available\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {
            Shop._DisplayMode++;

            SpawnMenu_Shop();
            _CurrentMenu._selectionIndex = component._buttonIndex;
            _CanRender = false;
            RenderMenu();
          })
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            var mode = (Shop.DisplayModes)Shop._DisplayMode;
            var color = mode == Shop.DisplayModes.PURCHASED ? "yellow" : _COLOR_GRAY;
            component.SetDisplayText($"filter: </color><color={color}>{mode}</color><color=white>\n\n");
          });

      // Shop table headers
      m.AddComponent("=== " + string.Format(format_shop, "name", "tags", "($$$)", "equip cost", "white", "white", "white") + "\n\n");

      // Add unlocks
      foreach (var desc in Shop._Unlocks_Descriptions)
      {
        // Check to skip unlock
        var unlock = desc.Key;
        if (Shop._Unlocks_Ignore_Shop.Contains(unlock)) { continue; }

        // Gather unlock info
        var display_mode = (Shop.DisplayModes)Shop._DisplayMode;
        var unlock_s = unlock.ToString();
        var name = unlock_s;
        var shop_details = Shop._Unlocks_Descriptions[unlock];

        var cost = shop_details.Item2;
        var equip_cost = 0;
        if (name.StartsWith("ITEM_"))
        {
          name = string.Format("{0,-20}{1,6}", name.Substring(5), "[item]");
          equip_cost = GameScript.ItemManager.GetItemValue((GameScript.ItemManager.Items)System.Enum.Parse(typeof(GameScript.ItemManager.Items), unlock_s.Substring(5), true));
        }
        else if (name.StartsWith("UTILITY_"))
        {
          name = string.Format("{0,-17}{1,9}", name.Substring(8), "[utility]");
          equip_cost = GameScript.ItemManager.GetUtilityValue((UtilityScript.UtilityType)System.Enum.Parse(typeof(UtilityScript.UtilityType), unlock_s.Substring(8), true));
        }
        else if (name.StartsWith("MOD_"))
        {
          name = string.Format("{0,-21}{1,5}", name.Substring(4), "[mod]");
          var perk = (Shop.Perk.PerkType)System.Enum.Parse(typeof(Shop.Perk.PerkType), unlock_s.Substring(4), true);
          equip_cost = GameScript.ItemManager.GetPerkValue(perk);
          shop_details = System.Tuple.Create(Shop.Perk._PERK_DESCRIPTIONS[perk], shop_details.Item2);
        }
        else if (name.StartsWith("EXTRA_"))
        {
          name = string.Format("{0,-19}{1,7}", name.Substring(6), "[extra]");
        }
        else if (name.StartsWith("MODE_"))
        {
          name = string.Format("{0,-20}{1,6}", name.Substring(5), "[mode]");
        }

        if (!Shop._Unlocks_Available.Contains(unlock))
        {
          if (display_mode == Shop.DisplayModes.ALL)
          {
            m.AddComponent($"{string.Format(format_shop2, FunctionsC.GenerateGarbageText(name), FunctionsC.GenerateGarbageText(shop_details.Item1), FunctionsC.GenerateGarbageText(cost + ""), "*", _COLOR_GRAY, _COLOR_GRAY, _COLOR_GRAY)}\n", MenuComponent.ComponentType.BUTTON_SIMPLE);
            Debug.Log("Shop unlock not available...: " + unlock);
          }
          continue;
        }
        if (Shop.Unlocked(unlock))
        {
          if (display_mode == Shop.DisplayModes.AVAILABLE) continue;
        }
        // Check for able to equip
        var equip_cost_string = equip_cost == 0 ? "-" : equip_cost + "";
        if (display_mode == Shop.DisplayModes.PURCHASED)
        {
          if (!Shop.Unlocked(unlock)) { continue; }
          var color = "yellow";
          m.AddComponent($"{string.Format(format_shop2, name, shop_details.Item1, cost, equip_cost_string, color, color, color)}\n", MenuComponent.ComponentType.BUTTON_SIMPLE);
          continue;
        }
        var set_text = true;
        var max_equip = GameScript.ItemManager.Loadout._POINTS_MAX;
        if (display_mode == Shop.DisplayModes.ALL && Shop.Unlocked(unlock))
        {
          var ct = string.Format(format_shop2, name, shop_details.Item1, cost, equip_cost_string, "yellow", "yellow", "yellow");
          if (ct.Contains('['))
          {
            var cts0 = ct.Split('[');
            var cts1 = cts0[1].Split(']');
            ct = $"{cts0[0]}<color={_COLOR_GRAY}>[{cts1[0]}]</color>{cts1[1]}";
          }
          m.AddComponent($"{ct}\n", MenuComponent.ComponentType.BUTTON_SIMPLE);
        }
        else if (equip_cost > max_equip && Shop._AvailablePoints < cost)
          m.AddComponent($"{string.Format(format_shop2, name, shop_details.Item1, cost, equip_cost_string, _COLOR_GRAY, "red", "red")}\n", MenuComponent.ComponentType.BUTTON_SIMPLE);
        else if (Shop._AvailablePoints < cost)
          m.AddComponent($"{string.Format(format_shop2, name, shop_details.Item1, cost, equip_cost_string, _COLOR_GRAY, "red", _COLOR_GRAY)}\n", MenuComponent.ComponentType.BUTTON_SIMPLE);
        else if (equip_cost > max_equip)
          m.AddComponent($"{string.Format(format_shop2, name, shop_details.Item1, cost, equip_cost_string, _COLOR_GRAY, _COLOR_GRAY, "red")}\n", MenuComponent.ComponentType.BUTTON_SIMPLE);
        else
        {
          set_text = false;
          var color = Shop.Unlocked(unlock) ? "yellow" : "white";

          // Regex search for color change pattern
          /*string tf(string input)
          {
            var match = System.Text.RegularExpressions.Regex.Match(input, @"\w*(ITEM_|UTILITY_|MOD_)\w*");
            if (match.Success)
            {
              var sub_name = "";
              var sub_type = match.Value.Split('_')[0];
              sub_name = $"<color={_COLOR_GRAY}>{sub_type}_</color>{match.Value.Substring(sub_type.Length + 1)}";
              input = input.Substring(0, match.Index) + $"</color>" + sub_name + $"<color=white>" + input.Substring(match.Index + match.Length);
            }
            return input;
          }*/

          /*var name_spl = name.Split('_');
          var name_colored = "";
          for (var i = 0; i < name_spl.Length; i++)
          {
            var word = name_spl[i];
            if (i == 0)
            {
              name_colored = $"<color={_COLOR_GRAY}>{word}_</color>";
            }
            else
            {
              name_colored += $"{word}";
              if (i < name_spl.Length - 1)
              {
                name_colored += "_";
              }
            }
          }*/
          var ct = string.Format(format_shop2, name, shop_details.Item1, cost, equip_cost_string, color, color, color);
          if (ct.Contains('['))
          {
            var cts0 = ct.Split('[');
            var cts1 = cts0[1].Split(']');
            ct = $"{cts0[0]}<color={_COLOR_GRAY}>[{cts1[0]}]</color>{cts1[1]}";
          }
          m.AddComponent(($"{ct}\n"), MenuComponent.ComponentType.BUTTON_SIMPLE)
            .AddEvent((MenuComponent component) =>
            {
              var displayText = component.GetDisplayText(false);
              var unlockText = displayText.Split('>')[2].Split('<')[0].Trim();//.Split(' ')[0].Trim();
              var prefix =
                displayText.Contains("[item]") ? "ITEM" :
                displayText.Contains("[mod]") ? "MOD" :
                displayText.Contains("[utility]") ? "UTILITY" :
                displayText.Contains("[extra]") ? "extra" :
                displayText.Contains("[mode]") ? "mode" :
                null;
              var c_split = unlockText.Split(' ');
              if (prefix == null)
              {
                unlockText = c_split[0];
              }
              else
              {
                unlockText = $"{prefix}_{c_split[0]}";
              }
              var unlock0 = (Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), unlockText, true);
              var shop_info = Shop._Unlocks_Descriptions[unlock0];
              //var equip_cost = GameScript.ItemManager.GetItemValue(unlock0);
              if (shop_info.Item2 <= Shop._AvailablePoints && !Shop.Unlocked(unlock0))
              {
                // Unlock item
                Shop.Unlock(unlock0);
                Shop.Unlock(Shop.Unlocks.TUTORIAL_PART0);
                // Increment points
                Shop._AvailablePoints -= shop_info.Item2;
                // Re-render menu
                var save_selection = _CurrentMenu._selectionIndex;
                SpawnMenu_Shop();
                _CurrentMenu._selectionIndex = save_selection;
                if (_CurrentMenu._selectedComponent.GetDisplayText(false).Trim().EndsWith("back"))
                  _CurrentMenu._selectionIndex--;
                _CurrentMenu._menuComponent_lastFocused = _CurrentMenu._menuComponentsSelectable[0];
                _CurrentMenu._selectedComponent._onFocus(_CurrentMenu._selectedComponent);
                _CanRender = false;
                RenderMenu();
                PlayNoise(Noise.PURCHASE);
              }
              else
              {
                PlayNoise(Noise.BACK);
              }
            });
        }
        if (set_text)
        {
          m.AddEvent((MenuComponent component) =>
          {
            // Save selection
            var save_selection = _CurrentMenu._selectionIndex;
            GenericMenu(new string[]
            {
            Shop._AvailablePoints < cost ?
            "cannot afford item\n\n- you do not have enough <color=yellow>($$$)</color> to buy this item\n\n- complete classic mode ranks to get more <color=yellow>($$$)</color>\n\n"
            :
            "cannot equip / purchase item\n\n- you do not have enough <color=yellow>equipment_points</color> to equip this item if you purchased it\n\n- buy more <color=yellow>MAX_EQUIP_POINTS</color> in the SHOP to equip / buy this\n\n"
            },
            "ok",
            MenuType.SHOP,
            null,
            true,
            (MenuComponent component1) =>
            {
              _Menus[MenuType.SHOP]._selectionIndex = save_selection;
              RenderMenu();
            });
          });
        }
      }
      // Back button
      m.AddComponent("\n")
      .AddBackButton((MenuComponent component) =>
      {
        CommonEvents._SwitchMenu(_InPause ? MenuType.PAUSE : MenuType.GAMETYPE_CLASSIC);
        if (_InPause)
        {
          _CurrentMenu._selectionIndex = 1;
          _CanRender = true;
          RenderMenu();
        }
      });
      m._onSwitchTo += () =>
      {
        Shop._DisplayMode = 0;
        SpawnMenu_Shop();
        _CurrentMenu._selectionIndex = Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART1) ? 3 : 2;
        _CanRender = false;
        RenderMenu();
      };
    }
    SpawnMenu_Shop();

    // Controls menu
    new Menu2(MenuType.HOW_TO_PLAY)
    {

    }
    .AddComponent($"<color={_COLOR_GRAY}>how to play</color>\n\n")
    .AddComponent("controls\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.CONTROLS); })
    .AddBackButton(MenuType.MAIN);

    // Controls menu
    Transform SpawnControlUI(MenuComponent component, FunctionsC.Control control)
    {
      var transform = FunctionsC.GetControl(control);
      transform.parent = component._collider.transform;
      var pos = component._collider.transform.position;
      pos.x = 0.4f;
      //pos.z += 0.1f;
      transform.position = pos;
      var lp = transform.localPosition;
      lp.x = -1.11f + (control == FunctionsC.Control.SELECT || control == FunctionsC.Control.START ? -0.2f : 0f);
      transform.localPosition = lp;
      return transform;
    }
    var format_controls = "{0,-25} {1,-20} {2,-25}";
    new Menu2(MenuType.CONTROLS)
    {
      _canSlowLoad = false
    }
    .AddComponent($"=== {string.Format(format_controls, "controls", "controller", "keyboard / mouse")}\n\n")
    .AddComponent($"{string.Format(format_controls, "move:", "", "w/a/s/d")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.L_STICK);
      })
    .AddComponent($"{string.Format(format_controls, "aim:", "", "mouse")} \n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.R_STICK);
      })
    .AddComponent($"{string.Format(format_controls, "left weapon:", "", "left mouse btn")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.L_TRIGGER);
      })
    .AddComponent($"{string.Format(format_controls, "right weapon:", "", "right mouse btn")} \n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.R_TRIGGER);
      })
    .AddComponent($"{string.Format(format_controls, "left utility:", "", "q")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.L_BUMPER);
      })
    .AddComponent($"{string.Format(format_controls, "right utility:", "", "e")} \n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.R_BUMPER);
      })
    .AddComponent($"{string.Format(format_controls, "reload weapon(s):", "", "r")} \n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.X);
      })
    .AddComponent($"{string.Format(format_controls, "swap weapon pair:", "", "tab")} \n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.Y);
      })
    .AddComponent($"{string.Format(format_controls, "grapple:", "", "middle mouse btn")} \n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.R_STICK);
      })
    .AddComponent($"{string.Format(format_controls, "spread arms:", "", "space")} \n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.A);
      })
    .AddComponent($"{string.Format(format_controls, "swap weapons' hand:", "", "g")} \n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.DPAD_UP);
      })
    .AddComponent($"{string.Format(format_controls, "pause:", "", "escape")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
        {
          var t = SpawnControlUI(component, FunctionsC.Control.START);
          var lp = new Vector3(0.2f, 0f, 0f);
          t.localPosition += lp;
        }
      })
    .AddComponent($"{string.Format(format_controls, "next level:", "", "page up")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
    .AddComponent($"{string.Format(format_controls, "previous level:", "", "page down")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
    .AddComponent($"\n<color={_COLOR_GRAY}>mode - </color><color=yellow>CLASSIC</color>\n")
    .AddComponent($"{string.Format(format_controls, "cycle loadout left:", "", "z")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.DPAD_LEFT);
      })
    .AddComponent($"{string.Format(format_controls, "cycle loadout right:", "", "c")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.DPAD_RIGHT);
      })
    .AddComponent("\n")
    .AddComponent($"{string.Format(format_controls, "restart map:", "", "left control/backspace")} \n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
        {
          var t = SpawnControlUI(component, FunctionsC.Control.SELECT);
          var lp = new Vector3(0.2f, 0f, 0f);
          t.localPosition += lp;
        }
      })
      .AddComponent($"{string.Format(format_controls, "whistle:", "", "v")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.DPAD_DOWN);
      })
    .AddComponent("\n")
    .AddComponent($"\n<color={_COLOR_GRAY}>mode - </color><color=yellow>SURVIVAL</color>\n")
        .AddComponent($"{string.Format(format_controls, "purchase (auto):", "", "f")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.B);
      })
    .AddComponent("\n")
    .AddComponent($"{string.Format(format_controls, "purchase (left):", "", "z")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.DPAD_LEFT);
      })
    .AddComponent($"{string.Format(format_controls, "purchase (right):", "", "c")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.DPAD_RIGHT);
      })
    .AddComponent("\n")
    .AddComponent($"{string.Format(format_controls, "drop money:", "", "v")} \n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        if (component._collider.transform.childCount == 0)
          SpawnControlUI(component, FunctionsC.Control.DPAD_DOWN);
      })
    .AddComponent("\n")
    .AddBackButton(MenuType.OPTIONS);
    _Menus[MenuType.CONTROLS]._onSwitchTo += () =>
    {
      foreach (var component in _Menus[MenuType.CONTROLS]._menuComponents)
        if (component._collider.transform.childCount != 0)
          component._collider.transform.GetChild(0).gameObject.SetActive(true);
    };
    _Menus[MenuType.CONTROLS]._onSwitched += () =>
    {
      foreach (var component in _Menus[MenuType.CONTROLS]._menuComponents)
        if (component._collider.transform.childCount != 0)
          component._collider.transform.GetChild(0).gameObject.SetActive(false);
    };

    // Mode selection menu
    var format_mode = "{0,-15} - {1,-50}\n";
    new Menu2(MenuType.MODE_SELECTION)
    {

    }
    .AddComponent($"<color={_COLOR_GRAY}>mode selection</color>\n\n")
    .AddComponent(string.Format(format_mode, "classic", "hand-placed enemies, choose loadouts"), MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
        GameScript._GameMode = GameScript.GameModes.CLASSIC;
        Settings._DIFFICULTY = PlayerPrefs.GetInt($"{GameScript._GameMode}_SavedDifficulty", 0);
        Levels._CurrentLevelCollectionIndex = Settings._DIFFICULTY;
        Settings.OnGamemodeChanged(Settings.GamemodeChange.CLASSIC);

        if (Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART1))
          CommonEvents._SwitchMenu(MenuType.GAMETYPE_CLASSIC);

        // Tutorial
        else
        {
          GenericMenu(
            new string[] {
              @"~1

if you don't know how to play, visit the '<color=yellow>HOW TO PLAY</color>' menu~2

"
            },
            "cool",
            MenuType.GAMETYPE_CLASSIC,
            null,
            true
          );
        }

      })
    // Switch to survival mode menu
    .AddComponent(string.Format(format_mode, "survival", "waves of enemies\n"), MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
        Levels._CurrentLevelCollectionIndex = 2;
        GameScript._GameMode = GameScript.GameModes.SURVIVAL;
        Settings._DIFFICULTY = PlayerPrefs.GetInt($"{GameScript._GameMode}_SavedDifficulty", 0);
        CommonEvents._SwitchMenu(MenuType.GAMETYPE_SURVIVAL);

        Settings.OnGamemodeChanged(Settings.GamemodeChange.SURVIVAL);
      })
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        component._obscured = !Shop.Unlocked(Shop.Unlocks.MODE_SURVIVAL);
      })
    .AddBackButton(MenuType.MAIN)
    // Fire event on menu switch; save last selected
    ._onSwitchTo += () =>
    {
      _SaveMenuDir = _SaveLevelSelected = -1;

      GameScript.SurvivalMode.OnLeaveMode();
    };
    // Set tip
    ModifyMenu_TipComponents(MenuType.MODE_SELECTION, 17);
    ModifyMenu_TipSwitch(MenuType.MODE_SELECTION);

    // Level display menu
    var dirs = 12;
    var levels_per_dir = 12;
    void SpawnMenu_Levels()
    {
      var format_subdirs = GameScript._GameMode == GameScript.GameModes.CLASSIC ? "{0,-7}{1,15}{2,15}{3,33}" : "{0,-20}{1,20}{2,40}";
      // Create new menu
      var m = new Menu2(MenuType.LEVELS)
      {

      }
      .AddComponent(GameScript._GameMode == GameScript.GameModes.CLASSIC ?
          string.Format($"<color={_COLOR_GRAY}>{format_subdirs}</color>", "levels", "rank", "", "") + "\n\n"
        :
          string.Format($"<color={_COLOR_GRAY}>{format_subdirs}</color>", "levels", "highest wave", "") + "\n\n"
        );

      // Load level times
      m._onSwitchTo += () =>
      {
        Levels.BufferLevelTimeDatas();
      };

      // Set level directory options
      if (GameScript._GameMode == GameScript.GameModes.CLASSIC)
      {
        dirs = Mathf.CeilToInt(Levels._CurrentLevelCollection._levelData.Length / levels_per_dir);
      }
      else if (GameScript._GameMode == GameScript.GameModes.CHALLENGE || GameScript._GameMode == GameScript.GameModes.SURVIVAL)
        dirs = Levels._CurrentLevelCollection._levelData.Length;

      for (var i = 0; i < dirs; i++)
      {
        var wave = "-";
        var rank_lowest = "";
        if (GameScript._GameMode == GameScript.GameModes.SURVIVAL)
        {
          wave = GameScript.SurvivalMode.GetHighestWave(i) + "";
          if (wave == "0")
            wave = "-";
        }
        else if (GameScript._GameMode == GameScript.GameModes.CLASSIC)
        {
          // Get lowest rank from all levels in dir
          for (var u = i == 0 ? 1 : 0; u < 12; u++)
          {
            var level_rating_data = Levels.GetLevelRating(u + (i * 12));
            if (level_rating_data == null)
            {
              rank_lowest = "-";
              break;
            }
            var level_rating = level_rating_data.Item1;

            if (rank_lowest == "")
            {
              rank_lowest = level_rating;
            }
            else if (level_rating.Length < rank_lowest.Length)
            {
              rank_lowest = level_rating;
            }
          }
        }

        var display_text =
          GameScript._GameMode == GameScript.GameModes.CLASSIC ?
            string.Format(format_subdirs, $"\\dir{i}", $"{rank_lowest}    ", "", "") + '\n'
          :
            string.Format(format_subdirs, $"\\dir{i}", $"{wave}    ", "") + '\n';
        _Menus[MenuType.LEVELS].AddComponent(display_text,
          MenuComponent.ComponentType.BUTTON_DROPDOWN)
          .AddEvent(EventType.ON_UNFOCUS, (MenuComponent component) =>
          {
            _SaveLevelSelected = -1;
          })
          // Save selected dir
          .AddEventFront((MenuComponent component) =>
          {
            _SaveMenuDir = component._buttonIndex;

            //if(GameScript._GameMode == GameScript.GameModes.SURVIVAL)
            //  GameScript._lp0 = TileManager.GetMapPreview(Levels._CurrentLevelCollection._leveldata[component._buttonIndex], 0).transform;
          })
          // Update dropdown selections
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            // Obscur dir if first level not unlocked
            if (GameScript._GameMode != GameScript.GameModes.SURVIVAL)
            {
              var first_level_iter = (component._buttonIndex) * levels_per_dir;
              if (first_level_iter > 0 && !Levels.LevelCompleted(first_level_iter - 1))
              {
                component.SetDisplayText(string.Format(format_subdirs, $"****", $"-    ", "", "") + '\n');
                return;
              }
            }
            // Obscur survival levels
            else
            {
              if (component._buttonIndex > 0 && GameScript.SurvivalMode.GetHighestWave(component._buttonIndex - 1) <= 10)
              {
                component._obscured = true;
                return;
              }
            }

            // Demo and default case
            if (GameScript._Singleton._IsDemo && component._buttonIndex >= 4)
            {
              component._obscured = true;
              return;
            }

            component._obscured = false;

            // Load CLASSIC levels
            if (GameScript._GameMode == GameScript.GameModes.CLASSIC)
            {
              // Update dropdown data
              var selections = new List<string>();
              var actions = new List<System.Action<MenuComponent>>();
              var actions_onFocus = new List<System.Action<MenuComponent>>();
              var actions_onUnfocus = new List<System.Action<MenuComponent>>();
              var actions_onCreated = new List<System.Action<MenuComponent>>();

              // Load level on select
              for (var u = 0; u < levels_per_dir; u++)
              {
                var level_iter = (component._buttonIndex) * levels_per_dir + u;
                var level_unlocked = (level_iter == 0 ? true : Levels.LevelCompleted(level_iter - 1));

                if (level_iter >= Levels._CurrentLevelCollection._levelData.Length) continue;

                //if (GameScript._Singleton._IsDemo) level_unlocked = true;
                var dev_time = Levels._CurrentLevel_LevelTimesData[Settings._DIFFICULTY][level_iter].Item1;
                var level_time_best = Levels._CurrentLevel_LevelTimesData[Settings._DIFFICULTY][level_iter].Item2;
                var text_levelTimeBest = level_time_best == -1f ? "-" : string.Format("{0:0.000}", level_time_best);

                var level_rating = Levels.GetLevelRating(dev_time, level_time_best);
                var text_rating = level_time_best == -1f ? "-" : level_rating == null ? "?" : level_rating.Item1;

                selections.Add(string.Format(format_subdirs, $"\\{(level_iter + 1)}", text_levelTimeBest, text_rating, ""));

                // Check if unlocked
                if (level_unlocked)
                {
                  actions.Add((MenuComponent component0) =>
                  {
                    // Save selected level iter
                    _SaveLevelSelected = component0._dropdownIndex;

                    // Load level
                    var levelIter = int.Parse(component0.GetDisplayText().Split(' ')[2].Trim().Substring(1)) - 1;
                    GameScript.NextLevel(levelIter);
                    CommonEvents._RemoveDropdownSelections(component0);

                    // Play music
                    if (FunctionsC.MusicManager._CurrentTrack <= 2)
                      FunctionsC.MusicManager.TransitionTo(FunctionsC.MusicManager.GetNextTrackIter());

                    // Remove menus
                    _Menu.gameObject.SetActive(false);
                    _InMenus = false;
                    TileManager._Text_LevelNum.gameObject.SetActive(true);
                    TileManager._Text_LevelTimer.gameObject.SetActive(true);
                    TileManager._Text_LevelTimer_Best.gameObject.SetActive(true);
                  });
                  // Add focus event
                  actions_onFocus.Add((MenuComponent component0) =>
                  {
                    if (component0._menu._menuComponent_lastFocused._buttonIndex > component0._menu._menuComponentsSelectable.Count - 1 - component0._menu._dropdownCount)
                      component0._menu._menuComponent_lastFocused._textColor = _COLOR_GRAY;
                    component0._textColor = "white";
                    int levelIter = int.Parse(component0.GetDisplayText().Split(' ')[2].Trim().Substring(1)) - 1;
                    GameScript._lp0 = TileManager.GetMapPreview(Levels._CurrentLevelCollection._levelData[levelIter]).transform;
                  });
                  actions_onCreated.Add((MenuComponent component0) => { });
                }
                // Disable component
                else
                {
                  actions.Add((MenuComponent component0) => { });
                  actions_onCreated.Add((MenuComponent component0) =>
                  {
                    component0._obscured = true;
                  });
                  // Add focus event
                  actions_onFocus.Add((MenuComponent component0) =>
                  {
                    if (component0._menu._menuComponent_lastFocused._buttonIndex > component0._menu._menuComponentsSelectable.Count - 1 - component0._menu._dropdownCount)
                      component0._menu._menuComponent_lastFocused._textColor = _COLOR_GRAY;
                  });
                }
                // Turn grey on unfocus
                actions_onUnfocus.Add((MenuComponent component0) =>
                {
                  component0._textColor = _COLOR_GRAY;
                });
              }
              // Get dropdown match
              var match = "";
              if (_SaveLevelSelected != -1 && _SaveLevelSelected < selections.Count)
                match = selections[_SaveLevelSelected];
              else
              {
                var lastIter = 0;
                foreach (var selection in selections)
                {
                  if (lastIter == 0) { lastIter++; continue; }
                  var level_unlocked = Levels.LevelCompleted(int.Parse(selection.Split(' ')[0].Substring(1)) - 2);
                  if (!level_unlocked) break;
                  lastIter++;
                }

                // Set match
                match = selections[lastIter - 1];

                // Check for all completed
                if (lastIter == levels_per_dir)
                {
                  var level_unlocked = Levels.LevelCompleted(int.Parse(selections[levels_per_dir - 1].Split(' ')[0].Substring(1)) - 1);
                  if (level_unlocked)
                    match = selections[0];
                }
              }
              // Check for last level
              if (Levels.LevelCompleted(143))
                match = selections[0];
              // Set dropdown data
              component.SetDropdownData($"=== {string.Format(format_subdirs, "level", "time", "rank", "preview")}\n\n", selections, actions, match, actions_onCreated, null, actions_onFocus, actions_onUnfocus);
            }

            // CHALLENGE levels; WIP
            else if (GameScript._GameMode == GameScript.GameModes.CHALLENGE)
            {
              // Update dropdown data
              var prompt = $"=== {string.Format(format_subdirs, "level", "time", "preview")}\n\n";
              var selections = new List<string>();
              var actions = new List<System.Action<MenuComponent>>();
              selections.Add("yes");
              actions.Add((MenuComponent component0) =>
              {
                GameScript.NextLevel(_SaveMenuDir);
                CommonEvents._RemoveDropdownSelections(component0);
                // Remove menus
                _Menu.gameObject.SetActive(false);
                _InMenus = false;
                TileManager._Text_LevelNum.gameObject.SetActive(true);
                TileManager._Text_LevelTimer.gameObject.SetActive(true);
                TileManager._Text_LevelTimer_Best.gameObject.SetActive(true);
              });
              // Set dropdown data
              component.SetDropdownData(prompt, selections, actions, "yes");
            }

            // SURVIVAL
            else if (GameScript._GameMode == GameScript.GameModes.SURVIVAL)
            {
              // Update dropdown data
              var prompt = $"=== {string.Format(format_subdirs, "", "", "")}\n\n";
              var selections = new List<string>();
              var actions = new List<System.Action<MenuComponent>>();
              selections.Add("select");
              actions.Add((MenuComponent component0) =>
              {
                GameScript.NextLevel(_SaveMenuDir);
                CommonEvents._RemoveDropdownSelections(component0);
                // Remove menus
                _Menu.gameObject.SetActive(false);
                _InMenus = false;
              });
              // Set dropdown data
              component.SetDropdownData(prompt, selections, actions, "select");
            }
          });
      }
      // Add 'level settings' if CLASSIC mode
      if (GameScript._GameMode == GameScript.GameModes.CLASSIC)
      {
        m.AddComponent($"\n<color={_COLOR_GRAY}>level settings</color>\n\n")
        .AddComponent("difficulty\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            var selection = Settings._DIFFICULTY == 0 ? "sneaky" : "sneakier";
            var color = selection == "sneaky" ? _COLOR_GRAY : "cyan";
            var ratings_lowest = Levels._Ranks_Lowest;
            var rating_lowest = ratings_lowest[Settings._DIFFICULTY];
            component.SetDisplayText($"difficulty: </color><color={color}>{selection} {rating_lowest}</color><color=white>\n");

            // Update dropdown data
            var selections = new List<string>();
            var actions = new List<System.Action<MenuComponent>>();
            var actions_onCreated = new List<System.Action<MenuComponent>>();
            // Set difficulty options
            void Local_SetDifficulty(int difficulty)
            {
              Settings._DIFFICULTY = difficulty;
              Levels._CurrentLevelCollectionIndex = (GameScript._GameMode == GameScript.GameModes.SURVIVAL ? 2 : 0 + difficulty);
              _SaveMenuDir = -1;
              _CanRender = false;
              Levels.BufferLevelTimeDatas();

              SpawnMenu_Levels();
              _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable.Count - 2;
              _CanRender = false;
              RenderMenu();
              SendInput(Input.SPACE);
              _CanRender = false;
              RenderMenu();
            }

            var format_ds = "{0,-9}{1,-4} - {2,-30}";
            selections.Add(string.Format(format_ds, $"sneaky", ratings_lowest[0], "base difficulty"));
            actions.Add((MenuComponent component0) =>
            {
              if (Settings._DIFFICULTY != 0)
                Local_SetDifficulty(0);
            });
            actions_onCreated.Add((MenuComponent component0) => { });
            bool difficultyLoaded = Settings._DifficultyUnlocked > 0;

            selections.Add(string.Format(format_ds, $"sneakier", ratings_lowest[1], "more enemies, harder levels, pressure"));
            if (difficultyLoaded)
            {
              actions.Add((MenuComponent component0) =>
              {
                if (Settings._DIFFICULTY != 1)
                  Local_SetDifficulty(1);
              });
              actions_onCreated.Add((MenuComponent component0) => { });
            }
            else
            {
              actions.Add((MenuComponent component0) => { });
              actions_onCreated.Add((MenuComponent component0) => { component0._obscured = true; });
            }

            // Set dropdown data
            component.SetDropdownData("difficulty level\n\n", selections, actions, Settings._DIFFICULTY == 0 ? "sneaky" : "sneakier", actions_onCreated);
          });
      }
      m.AddComponent("\n");
      // Back button
      m.AddBackButton((MenuComponent component) =>
      {
        _SaveMenuDir = -1;
        _SaveLevelSelected = -1;
        CommonEvents._SwitchMenu(GameScript._GameMode == GameScript.GameModes.CLASSIC ? MenuType.GAMETYPE_CLASSIC : MenuType.GAMETYPE_SURVIVAL);
      });

      // Destroy map preview on dropdown removed
      m._onDropdownRemoved += () =>
      {
        if (_Text.transform.childCount == 1)
          GameObject.Destroy(_Text.transform.GetChild(0).gameObject);
      };
      m._onSwitchTo += () =>
      {
        SpawnMenu_Levels();

        // Check for saved dirs
        if (_SaveMenuDir == -1)
        {
          var saveIndex0 = 0;
          foreach (var component0 in _CurrentMenu._menuComponentsSelectable)
          {
            if (saveIndex0 == 0) { saveIndex0++; continue; }
            var first_level_iter = (component0._buttonIndex) * levels_per_dir;
            var last_level_iter = (component0._buttonIndex) * levels_per_dir + (levels_per_dir - 1);
            if (!Levels.LevelCompleted(first_level_iter - 1)) break;
            if (component0.GetDisplayText(false).Contains($"dir{dirs - 1}") && Levels.LevelCompleted(last_level_iter - 1))
            {
              saveIndex0 = -1;
              break;
            }
            saveIndex0++;
          }
          _CurrentMenu._selectionIndex = saveIndex0 <= 0 ? 0 : Mathf.Clamp(saveIndex0 - 1, 0, dirs - 1);
          return;
        }
        var component = _Menus[MenuType.LEVELS]._menuComponentsSelectable[_SaveMenuDir];
        var save_saveLevelSelected = _SaveLevelSelected;
        component._onRender?.Invoke(component);
        component._onSelected?.Invoke(component);
        if (save_saveLevelSelected != -1)
        {
          _CurrentMenu._selectionIndex = (_CurrentMenu._menuComponentsSelectable.Count - _CurrentMenu._dropdownCount + save_saveLevelSelected);
          _CurrentMenu._selectedComponent._onFocus?.Invoke(_CurrentMenu._selectedComponent);
        }
        // Render
        RenderMenu();
      };

      // Tip
      //ModifyMenu_TipComponents(MenuType.LEVELS, GameScript._GameMode == GameScript.GameModes.CLASSIC ? 3 : 18, 1);
      //ModifyMenu_TipSwitch(MenuType.LEVELS);
    }
    SpawnMenu_Levels();

    // Select loadout menu
    void SpawnMenu_SelectLoadout()
    {
      var loadout_select_format = "{0,-30} {1,-45} {2,-15}";
      new Menu2(MenuType.SELECT_LOADOUT)
      {

      }
      //.AddComponent("select loadout\n\n")
      .AddComponent("=== " + string.Format(loadout_select_format, "", "equipment", "points left") + "\n")
      .AddComponent("==  " + string.Format(loadout_select_format, "", "========", "==========") + "\n\n");

      // Determine number of loadouts
      var num_loadouts = GameScript.ItemManager.Loadout._Loadouts.Length;
      for (; num_loadouts > 0; num_loadouts--)
      {
        var loadout = GameScript.ItemManager.Loadout._Loadouts[num_loadouts - 1];
        if (!loadout._equipment.IsEmpty()) break;
      }
      // Correct playerprofiles
      if (num_loadouts == 0) num_loadouts = 1;
      else if (num_loadouts < GameScript.ItemManager.Loadout._Loadouts.Length) num_loadouts++;
      foreach (var profile in GameScript.PlayerProfile._Profiles)
        profile.ChangeLoadoutIfEmpty(num_loadouts - 1);
      // Spawn selections
      for (int i = 0; i < Mathf.Clamp(num_loadouts, 1, num_loadouts); i++)
      {
        var loadout = GameScript.ItemManager.Loadout._Loadouts[i];

        // Gather equipment
        var equipment = "";
        if (loadout._equipment._item_left0 != GameScript.ItemManager.Items.NONE)
          equipment += (loadout._equipment._item_left0.ToString());
        if (loadout._equipment._item_right0 != GameScript.ItemManager.Items.NONE)
        {
          if (equipment != "") equipment += ", ";
          equipment += (loadout._equipment._item_right0.ToString());
        }
        var equipment1 = "";
        if (loadout._equipment._item_left1 != GameScript.ItemManager.Items.NONE)
          equipment1 += (loadout._equipment._item_left1.ToString());
        if (loadout._equipment._item_right1 != GameScript.ItemManager.Items.NONE)
        {
          if (equipment1 != "") equipment1 += ", ";
          equipment1 += (loadout._equipment._item_right1.ToString());
        }

        // Gather utilities
        var utilities = "";
        var utilities_strings = new List<string>();
        if (loadout._equipment._utilities_left != null && loadout._equipment._utilities_left.Length > 0)
        {
          var count = Shop.GetUtilityCount(loadout._equipment._utilities_left[0]) * loadout._equipment._utilities_left.Length;
          var count_string = count == 1 ? "" : $" x{count}";
          utilities_strings.Add($"{loadout._equipment._utilities_left[0].ToString()}{count_string}");
        }
        if (loadout._equipment._utilities_right != null && loadout._equipment._utilities_right.Length > 0)
        {
          var count = Shop.GetUtilityCount(loadout._equipment._utilities_right[0]) * loadout._equipment._utilities_right.Length;
          var count_string = count == 1 ? "" : $" x{count}";
          utilities_strings.Add($"{loadout._equipment._utilities_right[0].ToString()}{count_string}");
        }
        for (int u = 0; u < utilities_strings.Count; u++)
        {
          utilities += $"{utilities_strings[u]}";
          if (u != utilities_strings.Count - 1)
            utilities += ", ";
        }
        if (utilities.Trim().Length == 0) utilities = "-";

        // Gather perks
        var perks = "";// "-\n-\n";
        if (loadout._equipment._perks.Count > 0)
        {
          perks += $"{loadout._equipment._perks[0]}";
        }
        if (loadout._equipment._perks.Count > 1)
        {
          if (perks != "") perks += ", ";
          perks += $"{loadout._equipment._perks[1]}\n";
        }
        else if (perks != "")
          perks += '\n';
        if (perks == "") perks = "-\n";

        var perks0 = "";// "-\n-\n";
        if (loadout._equipment._perks.Count > 2)
        {
          perks0 += $"{loadout._equipment._perks[2]}";
        }
        if (loadout._equipment._perks.Count > 3)
        {
          if (perks0 != "") perks0 += ", ";
          perks0 += $"{loadout._equipment._perks[3]}\n";
        }
        if (perks0 == "") perks0 = "-\n";
        perks = perks + perks0;

        // Gather selection color
        var points_available = loadout._available_points;
        var points_available_string = $"{points_available}";
        if (equipment.Trim().Length == 0) equipment = "-";
        if (equipment1.Trim().Length == 0) equipment1 = "-";
        var color = "white";
        if (GameScript.PlayerProfile._Profiles != null)
          for (var u = 0; u < Settings._NumberPlayers; u++)
            if (u < GameScript.PlayerProfile._Profiles.Length &&
            GameScript.PlayerProfile._Profiles[u]._loadoutIndex == i)
            {
              color = "yellow";
              break;
            }

        // Gather loadout name
        var loadout_name = $"loadout {i + 1}";

        // Format string
        var perks_split = perks.Split('\n');
        var equipment_string = new string[]
        {
          equipment,
          equipment1,
          utilities,
          perks_split.Length > 0 ? perks_split[0] : "",
          perks_split.Length > 1 ? perks_split[1] : ""
        };
        var equipment_strings2 = new List<string>();
        foreach (var e in equipment_string)
        {
          if (!e.Contains('-'))
            equipment_strings2.Add(e);
        }

        var displayText = "";
        if (equipment_strings2.Count == 0)
          displayText = string.Format($"</color><color={color}>{loadout_select_format}</color><color=white>", loadout_name, "-", $"{points_available_string}") + "\n\n";
        else
        {
          displayText = string.Format($"</color><color={color}>{loadout_select_format}</color><color=white>", loadout_name, equipment_strings2[0], $"{points_available_string}");
          displayText += "\n";

          if (equipment_strings2.Count > 1)
            for (var u = 1; u < equipment_strings2.Count; u++)
            {
              var e = equipment_strings2[u];
              displayText += "    " + string.Format($"</color><color={color}>{loadout_select_format}</color><color=white>", "", e, "");
              displayText += "\n";
            }

          displayText += "\n";
        }

        /*/ Format string
        var displayText = string.Format($"</color><color={color}>{loadout_select_format}</color><color=white>", loadout_name, equipment, $"{points_available_string}");
        displayText += "\n";

        displayText += "    " + string.Format($"</color><color={color}>{loadout_select_format}</color><color=white>", "", equipment1, "");
        displayText += "\n";

        displayText += "    " + string.Format($"</color><color={color}>{loadout_select_format}</color><color=white>", "", utilities, "");
        displayText += "\n";

        displayText += "    " + string.Format($"</color><color={color}>{loadout_select_format}</color><color=white>", "", perks.Split('\n')[0], "");
        displayText += "\n";
        displayText += "    " + string.Format($"</color><color={color}>{loadout_select_format}</color><color=white>", "", perks.Split('\n')[1], "");
        displayText += "\n\n";*/

        // Set text
        _Menus[MenuType.SELECT_LOADOUT].AddComponent(displayText, MenuComponent.ComponentType.BUTTON_SIMPLE)
        // Go to edit loadout when selected
        .AddEvent((MenuComponent component) =>
        {
          GameScript.ItemManager.Loadout._CurrentLoadoutIndex = component._buttonIndex;
          CommonEvents._SwitchMenu(MenuType.EDIT_LOADOUT);
        });
      }
      _Menus[MenuType.SELECT_LOADOUT].AddBackButton((MenuComponent component) =>
      {
        // Check for empty loadout for tutorial
        foreach (var loadout in GameScript.ItemManager.Loadout._Loadouts)
          if (!loadout._equipment.IsEmpty())
          {
            Shop.Unlock(Shop.Unlocks.TUTORIAL_PART1);
            break;
          }

        // Switch back
        CommonEvents._SwitchMenu(_InPause ? MenuType.PAUSE : MenuType.GAMETYPE_CLASSIC);
        if (_InPause)
        {
          _CurrentMenu._selectionIndex = 2;
          _CanRender = true;
          RenderMenu();
        }
      });
      // Check for empty player profiles
      _Menus[MenuType.SELECT_LOADOUT]._onSwitchTo += () =>
      {
        SpawnMenu_SelectLoadout();
        foreach (var profile in GameScript.PlayerProfile._Profiles)
          if (profile._equipment.IsEmpty())
            profile._loadoutIndex++;
      };
    }
    SpawnMenu_SelectLoadout();

    // Loadout editing menu
    var loadout_format = "{0,-25} {1,-50} {2,-15}";
    var loadoutEquip_format = "{0,-25} <color=yellow>{1,-50}</color> {2,-15}";
    var loadout_format2 = "{0,-25} {1,-20}";
    var format_editLoadout = "cannot equip {0}\n\n- you do not have enough <color=yellow>equip_points</color> to equip this\n\n- try unequipping something or buying more MAX_EQUIP_POINTS from the SHOP\n";
    GameScript.ItemManager.Loadout CurrentLoadout() { return GameScript.ItemManager.Loadout._CurrentLoadout; }
    void SpawnMenu_LoadoutEditor()
    {
      var list_items = Shop.GetItemList();
      var list_utilities = Shop.GetUtilityList();
      var list_perks = Shop.GetPerkList();

      var has_item = false;
      foreach (var unlock in Shop._Unlocks)
        if (unlock.ToString().StartsWith("ITEM_"))
        {
          has_item = true;
          break;
        }

      var m = new Menu2(MenuType.EDIT_LOADOUT)
      {

      };
      m.AddComponent($"<color={_COLOR_GRAY}>edit loadout</color>\n\n")
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
        {
          component.SetDisplayText($"<color={_COLOR_GRAY}>edit loadout {CurrentLoadout()._id + 1}</color>\n\n");
        })
      .AddComponent("===\n\n");
      if (Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART1) && has_item)
      {
        /*.AddComponent("filter: UNLOCKED\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {
            Shop._LoadoutDisplayMode++;

            _CanRender = false;
            RenderMenu();
          })
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            var mode = Shop._LoadoutDisplayMode == 0 ? "UNLOCKED" : "ALL";
            component.SetDisplayText($"filter: {mode}\n");
          })*/
        m.AddComponent(string.Format(loadout_format2, "loadout type:", $"one pair") + "\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {
            var loadout = CurrentLoadout();
            loadout._two_weapon_pairs = !loadout._two_weapon_pairs;
            loadout.Save();

            SpawnMenu_LoadoutEditor();

            _CurrentMenu._selectionIndex = component._buttonIndex;

            _CanRender = false;
            RenderMenu();
          })
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            var type = CurrentLoadout()._two_weapon_pairs ? "two pairs" : "one pair";
            component.SetDisplayText(string.Format(loadout_format2, "loadout type:", $"{type}") + "\n");
          });
      }

      // Show equipment points
      m.AddComponent(string.Format(loadout_format2, "equipment points:", "0") + "\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
        {
          var available_points = CurrentLoadout()._available_points;
          var available_points_string = available_points == 0 ? $"</color><color=red>{available_points}</color><color=white>" : $"</color><color=yellow>{available_points}</color><color=white>";
          if (component._focused)
            component.SetDisplayText(string.Format($"<color={_COLOR_GRAY}>{loadout_format2}", "equipment points:", $"{available_points}</color>         <-- available points to equip items with") + "\n\n");
          else
            component.SetDisplayText(string.Format(loadout_format2, "equipment points:", $"{available_points_string}" + "\n\n"));
        })
        .SetSelectorType(MenuComponent.SelectorType.QUESTION)
      .AddComponent("=== " + string.Format(loadout_format, "", "item", "point cost") + "\n\n");

      if (has_item)
      {
        m.AddComponent("left hand\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            var item = CurrentLoadout()._equipment._item_left0;
            var item_name = item.ToString();
            var item_cost = GameScript.ItemManager.GetItemValue(item);
            if (item == GameScript.ItemManager.Items.NONE)
              component.SetDisplayText(string.Format(loadout_format, "left hand", $"-", $"-") + "\n");
            else
              component.SetDisplayText(string.Format(loadoutEquip_format, "left hand", $"{item_name}", $"{item_cost}") + "\n", true);

            // Check for two handed
            component._obscured = (CurrentLoadout()._equipment._item_right0 == GameScript.ItemManager.Items.SWORD);

            // Set dropdown data
            var selections = new List<string>();
            var actions = new List<System.Action<MenuComponent>>();
            var actions_onCreated = new List<System.Action<MenuComponent>>();
            var selection_match = item_name.ToString();

            foreach (var item0 in list_items)
            {
              var desc = "-";
              if (item0 != GameScript.ItemManager.Items.NONE)
              {
                var unlock = (Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"ITEM_{item0}", true);
                desc = Shop._Unlocks_Descriptions[unlock].Item1;

                // Check filter
                if (Shop._LoadoutDisplayMode == 0 && !Shop.Unlocked(unlock)) continue;
              }
              var it_val = GameScript.ItemManager.GetItemValue(item0);
              var use_color = it_val > CurrentLoadout()._available_points + item_cost ? "red" : "white";
              var use_color2 = use_color == "white" ? "white" : _COLOR_GRAY;
              if (item0 == item && item != GameScript.ItemManager.Items.NONE) use_color = use_color2 = "yellow";
              selections.Add(string.Format($"</color><color={use_color2}>{loadout_format}<color=white>", item0, desc, $"</color><color={use_color}>{it_val}</color>"));
              actions.Add((MenuComponent component0) =>
              {
                var item_selected = (GameScript.ItemManager.Items)System.Enum.Parse(typeof(GameScript.ItemManager.Items), component0.GetDisplayText(false).Trim().Split(' ')[2].Split('>')[2]);
                var item_other = CurrentLoadout()._equipment._item_right0;
                // Check for two handed
                if (((item_selected == GameScript.ItemManager.Items.BAT || item_selected == GameScript.ItemManager.Items.SWORD) &&
                  (item_other != GameScript.ItemManager.Items.NONE)) ||
                  ((item_other == GameScript.ItemManager.Items.BAT || item_other == GameScript.ItemManager.Items.SWORD) &&
                  (item_selected != GameScript.ItemManager.Items.NONE))
                  )
                  CurrentLoadout()._equipment._item_right0 = GameScript.ItemManager.Items.NONE;
                if (CurrentLoadout().CanEquipItem(ActiveRagdoll.Side.LEFT, 0, item_selected))
                {
                  CurrentLoadout()._equipment._item_left0 = item_selected;
                  SendInput(Input.BACK);
                }
                else
                {
                  CurrentLoadout()._equipment._item_right0 = item_other;
                  //
                  var save_selection = _CurrentMenu._dropdownParentIndex;
                  var save_dropdown = _CurrentMenu._selectionIndex;
                  var typ = "item";
                  GenericMenu(new string[]
                  {
                    string.Format(format_editLoadout, typ)
                  },
                "ok",
                  MenuType.EDIT_LOADOUT,
                  null,
                  true,
                  (MenuComponent component1) =>
                  {
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_selection;
                    SendInput(Input.SPACE);
                    SendInput(Input.SPACE);
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_dropdown;
                    RenderMenu();
                  });
                }
                // Update UI
                foreach (var profile in GameScript.PlayerProfile._Profiles)
                  profile.UpdateIcons();
              });
              // Check if item is unlocked
              if (item0 == GameScript.ItemManager.Items.NONE)
                actions_onCreated.Add((MenuComponent component0) => { });
              else
              {
                var unlocked = Shop.Unlocked((Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"ITEM_{item0}", true));
                if (unlocked)
                  actions_onCreated.Add((MenuComponent component0) => { });
                else
                  actions_onCreated.Add((MenuComponent component0) => { component0._obscured = true; });
              }
            }
            // Update dropdown data
            component.SetDropdownData("=== " + string.Format(loadout_format, "item", "tags", "point value") + "\n\n", selections, actions, selection_match, actions_onCreated);
          })
        .AddComponent("right hand\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            var item = CurrentLoadout()._equipment._item_right0;
            var item_name = item.ToString();
            var item_cost = GameScript.ItemManager.GetItemValue(item);
            if (item == GameScript.ItemManager.Items.NONE)
              component.SetDisplayText(string.Format(loadout_format, "right hand", $"-", $"-") + "\n\n");
            else
              component.SetDisplayText(string.Format(loadoutEquip_format, "right hand", $"{item_name}", $"{item_cost}") + "\n\n", true);

            // Check for two handed
            component._obscured = (CurrentLoadout()._equipment._item_left0 == GameScript.ItemManager.Items.SWORD);

            // Set dropdown data
            var selections = new List<string>();
            var actions = new List<System.Action<MenuComponent>>();
            var actions_onCreated = new List<System.Action<MenuComponent>>();
            var selection_match = item_name.ToString();

            foreach (var item0 in list_items)
            {
              var desc = "-";
              if (item0 != GameScript.ItemManager.Items.NONE)
              {
                var unlock = (Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"ITEM_{item0}", true);
                desc = Shop._Unlocks_Descriptions[unlock].Item1;

                // Check filter
                if (Shop._LoadoutDisplayMode == 0 && !Shop.Unlocked(unlock)) continue;
              }
              var it_val = GameScript.ItemManager.GetItemValue(item0);
              var use_color = it_val > CurrentLoadout()._available_points + item_cost ? "red" : "white";
              var use_color2 = use_color == "white" ? "white" : _COLOR_GRAY;
              if (item0 == item && item != GameScript.ItemManager.Items.NONE) use_color = use_color2 = "yellow";
              selections.Add(string.Format($"</color><color={use_color2}>{loadout_format}<color=white>", item0, desc, $"</color><color={use_color}>{it_val}</color>"));
              actions.Add((MenuComponent component0) =>
              {
                var item_selected = (GameScript.ItemManager.Items)System.Enum.Parse(typeof(GameScript.ItemManager.Items), component0.GetDisplayText(false).Trim().Split(' ')[2].Split('>')[2]);
                var item_other = CurrentLoadout()._equipment._item_left0;
                // Check for two handed
                if (((item_selected == GameScript.ItemManager.Items.BAT || item_selected == GameScript.ItemManager.Items.SWORD) &&
                  (item_other != GameScript.ItemManager.Items.NONE)) ||
                  ((item_other == GameScript.ItemManager.Items.BAT || item_other == GameScript.ItemManager.Items.SWORD) &&
                  (item_selected != GameScript.ItemManager.Items.NONE))
                  )
                  CurrentLoadout()._equipment._item_left0 = GameScript.ItemManager.Items.NONE;
                if (CurrentLoadout().CanEquipItem(ActiveRagdoll.Side.RIGHT, 0, item_selected))
                {
                  CurrentLoadout()._equipment._item_right0 = item_selected;
                  SendInput(Input.BACK);
                }
                else
                {
                  CurrentLoadout()._equipment._item_left0 = item_other;
                  //
                  var save_selection = _CurrentMenu._dropdownParentIndex;
                  var save_dropdown = _CurrentMenu._selectionIndex;
                  var typ = "item";
                  GenericMenu(new string[]
                  {
                    string.Format(format_editLoadout, typ)
                  },
                "ok",
                  MenuType.EDIT_LOADOUT,
                  null,
                  true,
                  (MenuComponent component1) =>
                  {
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_selection;
                    SendInput(Input.SPACE);
                    SendInput(Input.SPACE);
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_dropdown;
                    RenderMenu();
                  });
                }
                // Update UI
                foreach (var profile in GameScript.PlayerProfile._Profiles)
                  profile.UpdateIcons();
              });
              // Check if item is unlocked
              if (item0 == GameScript.ItemManager.Items.NONE)
                actions_onCreated.Add((MenuComponent component0) => { });
              else
              {
                var unlocked = Shop.Unlocked((Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"ITEM_{item0}", true));
                if (unlocked)
                  actions_onCreated.Add((MenuComponent component0) => { });
                else
                  actions_onCreated.Add((MenuComponent component0) => { component0._obscured = true; });
              }
            }
            // Update dropdown data
            component.SetDropdownData("=== " + string.Format(loadout_format, "item", "tags", "point value") + "\n\n", selections, actions, selection_match, actions_onCreated);
          });

        if (CurrentLoadout() != null && CurrentLoadout()._two_weapon_pairs)
        {
          m.AddComponent("left hand\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
            .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
            {
              var item = CurrentLoadout()._equipment._item_left1;
              var item_name = item.ToString();
              var item_cost = GameScript.ItemManager.GetItemValue(item);
              if (item == GameScript.ItemManager.Items.NONE)
                component.SetDisplayText(string.Format(loadout_format, "left hand", $"-", $"-") + "\n");
              else
                component.SetDisplayText(string.Format(loadoutEquip_format, "left hand", $"{item_name}", $"{item_cost}") + "\n", true);

              // Check for two handed
              component._obscured = (CurrentLoadout()._equipment._item_right1 == GameScript.ItemManager.Items.SWORD);

              // Set dropdown data
              var selections = new List<string>();
              var actions = new List<System.Action<MenuComponent>>();
              var actions_onCreated = new List<System.Action<MenuComponent>>();
              var selection_match = item_name.ToString();

              foreach (var item0 in list_items)
              {
                var desc = "-";
                if (item0 != GameScript.ItemManager.Items.NONE)
                {
                  var unlock = (Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"ITEM_{item0}", true);
                  desc = Shop._Unlocks_Descriptions[unlock].Item1;

                  // Check filter
                  if (Shop._LoadoutDisplayMode == 0 && !Shop.Unlocked(unlock)) continue;
                }
                var it_val = GameScript.ItemManager.GetItemValue(item0);
                var use_color = it_val > CurrentLoadout()._available_points + item_cost ? "red" : "white";
                var use_color2 = use_color == "white" ? "white" : _COLOR_GRAY;
                if (item0 == item && item != GameScript.ItemManager.Items.NONE) use_color = use_color2 = "yellow";
                selections.Add(string.Format($"</color><color={use_color2}>{loadout_format}<color=white>", item0, desc, $"</color><color={use_color}>{it_val}</color>"));
                actions.Add((MenuComponent component0) =>
                {
                  var item_selected = (GameScript.ItemManager.Items)System.Enum.Parse(typeof(GameScript.ItemManager.Items), component0.GetDisplayText(false).Trim().Split(' ')[2].Split('>')[2]);
                  var item_other = CurrentLoadout()._equipment._item_right1;
                  // Check for two handed
                  if (((item_selected == GameScript.ItemManager.Items.BAT || item_selected == GameScript.ItemManager.Items.SWORD) &&
                    (item_other != GameScript.ItemManager.Items.NONE)) ||
                    ((item_other == GameScript.ItemManager.Items.BAT || item_other == GameScript.ItemManager.Items.SWORD) &&
                    (item_selected != GameScript.ItemManager.Items.NONE))
                    )
                    CurrentLoadout()._equipment._item_right1 = GameScript.ItemManager.Items.NONE;
                  if (CurrentLoadout().CanEquipItem(ActiveRagdoll.Side.LEFT, 1, item_selected))
                  {
                    CurrentLoadout()._equipment._item_left1 = item_selected;
                    SendInput(Input.BACK);
                  }
                  else
                  {
                    CurrentLoadout()._equipment._item_right1 = item_other;
                    //
                    var save_selection = _CurrentMenu._dropdownParentIndex;
                    var save_dropdown = _CurrentMenu._selectionIndex;
                    var typ = "item";
                    GenericMenu(new string[]
                    {
                    string.Format(format_editLoadout, typ)
                    },
                    "ok",
                    MenuType.EDIT_LOADOUT,
                    null,
                    true,
                    (MenuComponent component1) =>
                    {
                      _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_selection;
                      SendInput(Input.SPACE);
                      SendInput(Input.SPACE);
                      _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_dropdown;
                      RenderMenu();
                    });
                  }
                  // Update UI
                  foreach (var profile in GameScript.PlayerProfile._Profiles)
                    profile.UpdateIcons();
                });
                // Check if item is unlocked
                if (item0 == GameScript.ItemManager.Items.NONE)
                  actions_onCreated.Add((MenuComponent component0) => { });
                else
                {
                  var unlocked = Shop.Unlocked((Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"ITEM_{item0}", true));
                  if (unlocked)
                    actions_onCreated.Add((MenuComponent component0) => { });
                  else
                    actions_onCreated.Add((MenuComponent component0) => { component0._obscured = true; });
                }
              }
              // Update dropdown data
              component.SetDropdownData("=== " + string.Format(loadout_format, "item", "tags", "point value") + "\n\n", selections, actions, selection_match, actions_onCreated);
            })
          .AddComponent("right hand\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
            .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
            {
              var item = CurrentLoadout()._equipment._item_right1;
              var item_name = item.ToString();
              var item_cost = GameScript.ItemManager.GetItemValue(item);
              if (item == GameScript.ItemManager.Items.NONE)
                component.SetDisplayText(string.Format(loadout_format, "right hand", $"-", $"-") + "\n\n");
              else
                component.SetDisplayText(string.Format(loadoutEquip_format, "right hand", $"{item_name}", $"{item_cost}") + "\n\n", true);

              // Check for two handed
              component._obscured = (CurrentLoadout()._equipment._item_left1 == GameScript.ItemManager.Items.SWORD);

              // Set dropdown data
              var selections = new List<string>();
              var actions = new List<System.Action<MenuComponent>>();
              var actions_onCreated = new List<System.Action<MenuComponent>>();
              var selection_match = item_name.ToString();

              foreach (var item0 in list_items)
              {
                var desc = "-";
                if (item0 != GameScript.ItemManager.Items.NONE)
                {
                  var unlock = (Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"ITEM_{item0}", true);
                  desc = Shop._Unlocks_Descriptions[unlock].Item1;

                  // Check filter
                  if (Shop._LoadoutDisplayMode == 0 && !Shop.Unlocked(unlock)) continue;
                }
                var it_val = GameScript.ItemManager.GetItemValue(item0);
                var use_color = it_val > CurrentLoadout()._available_points + item_cost ? "red" : "white";
                var use_color2 = use_color == "white" ? "white" : _COLOR_GRAY;
                if (item0 == item && item != GameScript.ItemManager.Items.NONE) use_color = use_color2 = "yellow";
                selections.Add(string.Format($"</color><color={use_color2}>{loadout_format}<color=white>", item0, desc, $"</color><color={use_color}>{it_val}</color>"));
                actions.Add((MenuComponent component0) =>
                {
                  var item_selected = (GameScript.ItemManager.Items)System.Enum.Parse(typeof(GameScript.ItemManager.Items), component0.GetDisplayText(false).Trim().Split(' ')[2].Split('>')[2]);
                  var item_other = CurrentLoadout()._equipment._item_left1;
                  // Check for two handed
                  if (((item_selected == GameScript.ItemManager.Items.BAT || item_selected == GameScript.ItemManager.Items.SWORD) &&
                    (item_other != GameScript.ItemManager.Items.NONE)) ||
                    ((item_other == GameScript.ItemManager.Items.BAT || item_other == GameScript.ItemManager.Items.SWORD) &&
                    (item_selected != GameScript.ItemManager.Items.NONE))
                    )
                    CurrentLoadout()._equipment._item_left1 = GameScript.ItemManager.Items.NONE;
                  if (CurrentLoadout().CanEquipItem(ActiveRagdoll.Side.RIGHT, 1, item_selected))
                  {
                    CurrentLoadout()._equipment._item_right1 = item_selected;
                    SendInput(Input.BACK);
                  }
                  else
                  {
                    CurrentLoadout()._equipment._item_left1 = item_other;
                    //
                    var save_selection = _CurrentMenu._dropdownParentIndex;
                    var save_dropdown = _CurrentMenu._selectionIndex;
                    var typ = "item";
                    GenericMenu(new string[]
                    {
                    string.Format(format_editLoadout, typ)
                    },
                    "ok",
                    MenuType.EDIT_LOADOUT,
                    null,
                    true,
                    (MenuComponent component1) =>
                    {
                      _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_selection;
                      SendInput(Input.SPACE);
                      SendInput(Input.SPACE);
                      _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_dropdown;
                      RenderMenu();
                    });
                  }                // Update UI
                  foreach (var profile in GameScript.PlayerProfile._Profiles)
                    profile.UpdateIcons();
                });
                // Check if item is unlocked
                if (item0 == GameScript.ItemManager.Items.NONE)
                  actions_onCreated.Add((MenuComponent component0) => { });
                else
                {
                  var unlocked = Shop.Unlocked((Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"ITEM_{item0}", true));
                  if (unlocked)
                    actions_onCreated.Add((MenuComponent component0) => { });
                  else
                    actions_onCreated.Add((MenuComponent component0) => { component0._obscured = true; });
                }
              }
              // Update dropdown data
              component.SetDropdownData("=== " + string.Format(loadout_format, "item", "tags", "point value") + "\n\n", selections, actions, selection_match, actions_onCreated);
            });
        }
      }
      // Utilities
      var has_utility = false;
      foreach (var unlock in Shop._Unlocks)
        if (unlock.ToString().StartsWith("UTILITY_"))
        {
          has_utility = true;
          break;
        }
      /*


              .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        var points = GameScript.ItemManager.Loadout._POINTS_MAX;
        if (component._focused)
          component.SetDisplayText($"</color><color={_COLOR_GRAY}>max equipment points: {points}</color> <-- the higher the number the more things you can equip<color=white>\n\n");
        else
          component.SetDisplayText($"max equipment points: </color><color=yellow>{points}</color><color=white>\n\n");
      })

      */
      if (has_utility)
      {
        m.AddComponent("left utility\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            var utilities = CurrentLoadout()._equipment._utilities_left;
            var utility_name = "";
            var utility_cost = 0;
            if (utilities.Length == 0)
              component.SetDisplayText(string.Format(loadout_format, $"left utility", $"-", $"-") + "\n");
            else
            {
              var utility_amount = Shop.GetUtilityCount(utilities[0]) * utilities.Length;
              utility_name = utilities[0].ToString();
              var mod = 1f;
              if (utilities[0] == UtilityScript.UtilityType.SHURIKEN)
                mod = 0.5f;
              utility_cost = (int)(GameScript.ItemManager.GetUtilityValue(utilities[0]) * utility_amount * mod);
              component.SetDisplayText(string.Format(loadoutEquip_format, "left utility", $"{utility_name} x{utility_amount}", $"{utility_cost}") + "\n", true);
            }
            // Set dropdown data
            var selections = new List<string>();
            var actions = new List<System.Action<MenuComponent>>();
            var actions_onCreated = new List<System.Action<MenuComponent>>();
            var selection_match = utility_name;

            foreach (var utility0 in list_utilities)
            {
              var desc = "-";
              if (utility0 != UtilityScript.UtilityType.NONE)
              {
                var unlock = (Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"UTILITY_{utility0}", true);
                desc = Shop._Unlocks_Descriptions[unlock].Item1;

                // Check filter
                if (Shop._LoadoutDisplayMode == 0 && !Shop.Unlocked(unlock)) continue;
              }
              var ut_val = GameScript.ItemManager.GetUtilityValue(utility0);
              var use_color = ut_val > CurrentLoadout()._available_points + utility_cost ? "red" : "white";
              var use_color2 = use_color == "white" ? "white" : _COLOR_GRAY;
              if (utility0.ToString() == utility_name && utility_name != (UtilityScript.UtilityType.NONE).ToString()) use_color = use_color2 = "yellow";
              selections.Add(string.Format($"</color><color={use_color2}>{loadout_format}<color=white>", utility0, desc, $"</color><color={use_color}>{ut_val}</color>"));
              actions.Add((MenuComponent component0) =>
              {
                var utility_selected = (UtilityScript.UtilityType)System.Enum.Parse(typeof(UtilityScript.UtilityType), component0.GetDisplayText(false).Trim().Split(' ')[2].Split('>')[2]);

                if (utility_selected == UtilityScript.UtilityType.NONE)
                  CurrentLoadout()._equipment._utilities_left = new UtilityScript.UtilityType[0];
                else if (CurrentLoadout().CanEquipUtility(ActiveRagdoll.Side.LEFT, utility_selected))
                {
                  // Check if different
                  if (CurrentLoadout()._equipment._utilities_left.Length == 0 || utility_selected != CurrentLoadout()._equipment._utilities_left[0])
                    CurrentLoadout()._equipment._utilities_left = new UtilityScript.UtilityType[] { utility_selected };
                  else
                  {
                    var list_new = new List<UtilityScript.UtilityType>();
                    foreach (var util in CurrentLoadout()._equipment._utilities_left)
                      list_new.Add(util);
                    list_new.Add(utility_selected);
                    CurrentLoadout()._equipment._utilities_left = list_new.ToArray();
                  }
                }
                // If can't equip and utility is the same, set to 1
                else if (CurrentLoadout()._equipment._utilities_left.Length != 0 && utility_selected == CurrentLoadout()._equipment._utilities_left[0])
                  CurrentLoadout()._equipment._utilities_left = new UtilityScript.UtilityType[] { utility_selected };
                else
                {
                  var save_selection = _CurrentMenu._dropdownParentIndex;
                  var save_dropdown = _CurrentMenu._selectionIndex;
                  var typ = "utility";
                  GenericMenu(new string[]
                  {
                  string.Format(format_editLoadout, typ)
                  },
                  "ok",
                  MenuType.EDIT_LOADOUT,
                  null,
                  true,
                  (MenuComponent component1) =>
                  {
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_selection;
                    SendInput(Input.SPACE);
                    SendInput(Input.SPACE);
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_dropdown;
                    RenderMenu();
                  });
                }
                // Update UI
                foreach (var profile in GameScript.PlayerProfile._Profiles)
                  profile.UpdateIcons();
              });
              // Check if utility is unlocked
              if (utility0 == UtilityScript.UtilityType.NONE)
                actions_onCreated.Add((MenuComponent component0) => { });
              else
              {
                var unlocked = Shop.Unlocked((Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"UTILITY_{utility0}", true));
                if (unlocked)
                  actions_onCreated.Add((MenuComponent component0) => { });
                else
                  actions_onCreated.Add((MenuComponent component0) => { component0._obscured = true; });
              }
            }
            // Update dropdown data
            component.SetDropdownData("=== " + string.Format(loadout_format, "item", "tags", "point value") + "\n\n", selections, actions, selection_match, actions_onCreated);
          })
        .AddComponent("right utility\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            var utilities = CurrentLoadout()._equipment._utilities_right;
            var utility_name = "";
            var utility_cost = 0;
            if (utilities.Length == 0)
              component.SetDisplayText(string.Format(loadout_format, "right utility", $"-", $"-") + "\n\n");
            else
            {
              var utility_amount = Shop.GetUtilityCount(utilities[0]) * utilities.Length;
              utility_name = utilities[0].ToString();
              var mod = 1f;
              if (utilities[0] == UtilityScript.UtilityType.SHURIKEN)
                mod = 0.5f;
              utility_cost = (int)(GameScript.ItemManager.GetUtilityValue(utilities[0]) * utility_amount * mod);
              component.SetDisplayText(string.Format(loadoutEquip_format, "right utility", $"{utility_name} x{utility_amount}", $"{utility_cost}") + "\n\n", true);
            }
            // Set dropdown data
            var selections = new List<string>();
            var actions = new List<System.Action<MenuComponent>>();
            var actions_onCreated = new List<System.Action<MenuComponent>>();
            var selection_match = utility_name;

            foreach (var utility0 in list_utilities)
            {
              // Get item desciption from Shop
              var desc = "-";
              if (utility0 != UtilityScript.UtilityType.NONE)
              {
                var unlock = (Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"UTILITY_{utility0}", true);
                desc = Shop._Unlocks_Descriptions[unlock].Item1;

                // Check filter
                if (Shop._LoadoutDisplayMode == 0 && !Shop.Unlocked(unlock)) continue;
              }
              var ut_val = GameScript.ItemManager.GetUtilityValue(utility0);
              var use_color = ut_val > CurrentLoadout()._available_points + utility_cost ? "red" : "white";
              var use_color2 = use_color == "white" ? "white" : _COLOR_GRAY;
              if (utility0.ToString() == utility_name && utility_name != (UtilityScript.UtilityType.NONE).ToString()) use_color = use_color2 = "yellow";
              selections.Add(string.Format($"</color><color={use_color2}>{loadout_format}<color=white>", utility0, desc, $"</color><color={use_color}>{ut_val}</color>"));
              actions.Add((MenuComponent component0) =>
              {
                var utility_selected = (UtilityScript.UtilityType)System.Enum.Parse(typeof(UtilityScript.UtilityType), component0.GetDisplayText(false).Trim().Split(' ')[2].Split('>')[2]);
                if (utility_selected == UtilityScript.UtilityType.NONE)
                  CurrentLoadout()._equipment._utilities_right = new UtilityScript.UtilityType[0];
                else if (CurrentLoadout().CanEquipUtility(ActiveRagdoll.Side.RIGHT, utility_selected))
                {
                  // Check if different
                  if (CurrentLoadout()._equipment._utilities_right.Length == 0 || utility_selected != CurrentLoadout()._equipment._utilities_right[0])
                    CurrentLoadout()._equipment._utilities_right = new UtilityScript.UtilityType[] { utility_selected };
                  else
                  {
                    var list_new = new List<UtilityScript.UtilityType>();
                    foreach (var util in CurrentLoadout()._equipment._utilities_right)
                      list_new.Add(util);
                    list_new.Add(utility_selected);
                    CurrentLoadout()._equipment._utilities_right = list_new.ToArray();
                  }
                }
                // If can't equip and utility is the same, set to 1
                else if (CurrentLoadout()._equipment._utilities_right.Length != 0 && utility_selected == CurrentLoadout()._equipment._utilities_right[0])
                  CurrentLoadout()._equipment._utilities_right = new UtilityScript.UtilityType[] { utility_selected };
                else
                {
                  var save_selection = _CurrentMenu._dropdownParentIndex;
                  var save_dropdown = _CurrentMenu._selectionIndex;
                  var typ = "utility";
                  GenericMenu(new string[]
                  {
                  string.Format(format_editLoadout, typ)
                  },
                  "ok",
                  MenuType.EDIT_LOADOUT,
                  null,
                  true,
                  (MenuComponent component1) =>
                  {
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_selection;
                    SendInput(Input.SPACE);
                    SendInput(Input.SPACE);
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_dropdown;
                    RenderMenu();
                  });
                }
                // Update UI
                foreach (var profile in GameScript.PlayerProfile._Profiles)
                  profile.UpdateIcons();
              });
              // Check if utility is unlocked
              if (utility0 == UtilityScript.UtilityType.NONE)
                actions_onCreated.Add((MenuComponent component0) => { });
              else
              {
                var unlocked = Shop.Unlocked((Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"UTILITY_{utility0}", true));
                if (unlocked)
                  actions_onCreated.Add((MenuComponent component0) => { });
                else
                  actions_onCreated.Add((MenuComponent component0) => { component0._obscured = true; });
              }
            }
            // Update dropdown data
            component.SetDropdownData("=== " + string.Format(loadout_format, "item", "tags", "point value") + "\n\n", selections, actions, selection_match, actions_onCreated);
          });
      }

      // Perks
      var has_perk = false;
      if (Levels._EditingLoadout)
        has_perk = true;
      else
        foreach (var unlock in Shop._Unlocks)
          if (unlock.ToString().StartsWith("MOD_"))
          {
            has_perk = true;
            break;
          }
      if (has_perk)
      {
        m.AddComponent("mods\n\n\n\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            var perks = CurrentLoadout()._equipment._perks;
            var perk_name = "";
            if (perks.Count == 0)
              component.SetDisplayText(
                string.Format(loadout_format, "mods", $"-", $"-") + "\n" +
                "    " + string.Format(loadout_format, "", $"-", $"-") + "\n" +
                "    " + string.Format(loadout_format, "", $"-", $"-") + "\n" +
                "    " + string.Format(loadout_format, "", $"-", $"-") + "\n\n");
            else
            {
              var perkstring = "";
              for (var i = 0; i < 4; i++)
              {
                var hasperk = i < perks.Count;
                perkstring += (i == 0 ? "" : "    ") + string.Format(loadoutEquip_format, i == 0 ? "mods" : "", hasperk ? $"{perks[i]}" : "-", hasperk ? "" + GameScript.ItemManager.GetPerkValue(perks[i]) : "-") + "\n";
              }
              perkstring += '\n';
              component.SetDisplayText(perkstring, true);
            }
            // Set dropdown data
            var selections = new List<string>();
            var actions = new List<System.Action<MenuComponent>>();
            var actions_onCreated = new List<System.Action<MenuComponent>>();
            var selection_match = perk_name;

            foreach (var perk0 in list_perks)
            {
              var desc = "-";

              if (perk0 != Shop.Perk.PerkType.NONE)
              {
                var unlock = (Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"MOD_{perk0}", true);
                desc = Shop.Perk._PERK_DESCRIPTIONS[perk0];

                // Check filter
                if (Shop._LoadoutDisplayMode == 0 && !Shop.Unlocked(unlock)) continue;
              }

              var per_val = GameScript.ItemManager.GetPerkValue(perk0);
              var use_color = per_val > CurrentLoadout()._available_points ? "red" : "white";
              var use_color2 = use_color == "white" ? "white" : _COLOR_GRAY;
              if (perks.Contains(perk0) && perk0 != Shop.Perk.PerkType.NONE) use_color = use_color2 = "yellow";
              selections.Add(string.Format($"</color><color={use_color2}>{loadout_format}<color=white>", perk0, desc, $"</color><color={use_color}>{per_val}</color>"));
              actions.Add((MenuComponent component0) =>
              {
                var perk_selected = (Shop.Perk.PerkType)System.Enum.Parse(typeof(Shop.Perk.PerkType), component0.GetDisplayText(false).Trim().Split(' ')[2].Split('>')[2]);
                var perk_value = GameScript.ItemManager.GetPerkValue(perk_selected);
                perks = CurrentLoadout()._equipment._perks;

                // Check for none selection
                if (perk_selected == Shop.Perk.PerkType.NONE)
                  CurrentLoadout()._equipment._perks = new List<Shop.Perk.PerkType>();

                // Check for selected already
                else if (perks.Contains(perk_selected))
                  perks.Remove(perk_selected);

                // Check for max perks
                else if (perks.Count == 4)
                {
                  var save_selection = _CurrentMenu._dropdownParentIndex;
                  var save_dropdown = _CurrentMenu._selectionIndex;
                  GenericMenu(new string[]
                  {
                     "cannot equip mod\n\n- the maximum number of mods you can equip is 4\n\n- try unequipping a mod\n"
                  },
                  "ok",
                  MenuType.EDIT_LOADOUT,
                  null,
                  true,
                  (MenuComponent component1) =>
                  {
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_selection;
                    SendInput(Input.SPACE);
                    SendInput(Input.SPACE);
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_dropdown;
                    RenderMenu();
                  });
                }

                // Check for select
                else if (perk_value <= CurrentLoadout()._available_points)
                  perks.Add(perk_selected);
                else
                {
                  var save_selection = _CurrentMenu._dropdownParentIndex;
                  var save_dropdown = _CurrentMenu._selectionIndex;
                  var typ = "mod";
                  GenericMenu(new string[]
                  {
                  string.Format(format_editLoadout, typ)
                  },
                  "ok",
                  MenuType.EDIT_LOADOUT,
                  null,
                  true,
                  (MenuComponent component1) =>
                  {
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_selection;
                    SendInput(Input.SPACE);
                    SendInput(Input.SPACE);
                    _Menus[MenuType.EDIT_LOADOUT]._selectionIndex = save_dropdown;
                    RenderMenu();
                  });
                };
                // Update UI
                foreach (var profile in GameScript.PlayerProfile._Profiles)
                  profile.UpdateIcons();
              });
              // Check if utility is unlocked
              if (perk0 == Shop.Perk.PerkType.NONE)
                actions_onCreated.Add((MenuComponent component0) => { });
              else
              {
                var unlocked = Shop.Unlocked((Shop.Unlocks)System.Enum.Parse(typeof(Shop.Unlocks), $"MOD_{perk0}", true));
                if (unlocked)
                  actions_onCreated.Add((MenuComponent component0) => { });
                else
                  actions_onCreated.Add((MenuComponent component0) => { component0._obscured = true; });
              }
            }
            // Update dropdown data
            component.SetDropdownData("=== " + string.Format(loadout_format, "mod", "description", "point value") + "\n\n", selections, actions, selection_match, actions_onCreated);
          });
      }

      if (Levels._EditingLoadout)
        m.AddBackButton(MenuType.EDITOR_PACKS_EDIT)
          .AddEvent((MenuComponent component) =>
          {
            var leveliter = Levels._LoadoutEdit_SaveIndex;
            var leveldata = Levels._LevelPack_Current._levelData[leveliter];

            _CurrentMenu._selectionIndex = leveliter + 1;
            RenderMenu();

            // Loop through players and equip loadout
            if (PlayerScript._Players != null)
              foreach (var player in PlayerScript._Players)
              {
                player.EquipLoadout(CurrentLoadout()._id, false);
              }

            var has_forced_load = leveldata.Contains("!");
            if (has_forced_load)
              leveldata = leveldata.Split('!')[0];

            // Save loadout to level data
            if (CurrentLoadout()._equipment.IsEmpty())
            {

              // Check for removing forced loadout
              if (has_forced_load)
              {
                Levels._LevelPack_Current._levelData[leveliter] = leveldata;
                Levels.LevelPack_Save();
              }

              Debug.Log("Current loadout empty");
              return;
            }

            var equipment = CurrentLoadout()._equipment;
            var utilsleft_string = "";
            var utilsright_string = "";
            var perkstring = "";

            if (equipment._utilities_left != null && equipment._utilities_left.Length > 0)
              utilsleft_string = $"{equipment._utilities_left[0]}:{equipment._utilities_left.Length}";
            if (equipment._utilities_right != null && equipment._utilities_right.Length > 0)
              utilsright_string = $"{equipment._utilities_right[0]}:{equipment._utilities_right.Length}";
            if (equipment._perks != null && equipment._perks.Count > 0)
              foreach (var perk in equipment._perks)
                perkstring += $"{perk}:";

            Levels._LevelPack_Current._levelData[leveliter] = leveldata +
              $"!{equipment._item_left0},{equipment._item_right0},{equipment._item_left1},{equipment._item_right1},{utilsleft_string},{utilsright_string},{perkstring}";
            Levels.LevelPack_Save();

            Debug.Log("Saved loadout to map data");
          });
      else
        m.AddBackButton(MenuType.SELECT_LOADOUT)
          .AddEvent((MenuComponent component) =>
          {
            for (var i = 0; i < CurrentLoadout()._id; i++)
              SendInput(Input.DOWN);
            RenderMenu();

            // Loop through players
            if (PlayerScript._Players != null)
              foreach (var player in PlayerScript._Players)
              {
                if (player == null || player._ragdoll == null || player._ragdoll._dead) continue;
                if (player._profile._loadoutIndex == CurrentLoadout()._id)
                  player.EquipLoadout(CurrentLoadout()._id, false);
              }
          });
      _Menus[MenuType.EDIT_LOADOUT]._onDropdownRemoved += () =>
      {
        foreach (var loadout in GameScript.ItemManager.Loadout._Loadouts)
          loadout.Save();

        // Check empty loadout
        foreach (var profile in GameScript.PlayerProfile._Profiles)
          profile.ChangeLoadoutIfEmpty();
      };

      // Reload menu on switch
      _Menus[MenuType.EDIT_LOADOUT]._onSwitchTo += () =>
      {
        SpawnMenu_LoadoutEditor();
        _CurrentMenu._selectionIndex = Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART1) && has_item ? 2 : 1;
        _CanRender = false;
        RenderMenu();
      };
    }
    SpawnMenu_LoadoutEditor();

    // Stats menu
    var format_overallstats = "{0,-40}{1,15}\n";
    var mStats = new Menu2(MenuType.STATS)
    {

    }
    .AddComponent($"<color={_COLOR_GRAY}>overall stats</color>\n\n");
    var iter = 0;
    foreach (var stat in Stats.OverallStats._Stats)
    {
      if (iter == 3)
        mStats.AddComponent("\n=classic_mode\n\n");
      if (iter == 9)
        mStats.AddComponent("\n=survival_mode\n\n");

      mStats.AddComponent($"{stat._name}\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
        {
          var stat0 = Stats.OverallStats._Stats[component._buttonIndex];
          component.SetDisplayText(string.Format(format_overallstats, stat0._name.Split(new string[] { "__" }, System.StringSplitOptions.None)[1], stat0._value).ToLower());
        });
      iter++;
    }
    mStats.AddComponent("\n")
    .AddComponent("reset\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEventFront((MenuComponent component) =>
      {
        Settings._DeleteStatsIter = 4;
      })
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {

        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        selections.Add($"yes - press {Settings._DeleteStatsIter + 1} more times");
        actions.Add((MenuComponent component0) =>
        {
          if (Settings._DeleteStatsIter-- <= 0)
          {

            // Reset overall stats
            Stats.OverallStats.Reset();

            // Press back button
            component0._menu._menuComponent_last._onSelected?.Invoke(component0._menu._menuComponent_last);
          }
        });

        // Update dropdown data
        component.SetDropdownData("reset overall stats\n*this <color=red>cannot</color> be undone\n\n", selections, actions, "");
      })
    .AddBackButton(MenuType.OPTIONS);

    // Pause menu
    void SpawnMenu_Pause()
    {
      var mPause = new Menu2(MenuType.PAUSE)
      {
        _onSwitchTo = () => { _InPause = true; }
      };
      mPause.AddComponent($"<color={_COLOR_GRAY}>pause</color>\n\n")
      // Unpause and hide menu
      .AddComponent("resume\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
        .AddEvent((MenuComponent component) =>
        {
          //Debug.Log("resume pressed");

          GameScript.TogglePause();
          _InPause = false;
          _InMenus = false;
          _Menu.gameObject.SetActive(false);

          // Check editing
          if (GameScript._EditorTesting)
            if (GameScript._EditorEnabled) TileManager.EditorMenus._Menu_Editor.gameObject.SetActive(true);
            else TileManager.EditorMenus._Menu_EditorTesting.gameObject.SetActive(true);
        });

      if (Levels._HardcodedLoadout != null && !GameScript._EditorTesting)
      {
        // Switch to loadouts menu
        mPause.AddComponent("edit loadouts\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {

            component._selectorType = MenuComponent.SelectorType.QUESTION;
            component._textColor = _COLOR_GRAY;

            if (component._focused)
              component.SetDisplayText($"</color><color=red>edit loadouts</color><color=white> <-- cannot edit loadouts; this level's loadout is set by the creator\n");
            else
              component.SetDisplayText("edit loadouts\n");
          });
      }

      else if (GameScript._GameMode != GameScript.GameModes.SURVIVAL)
      {
        // Switch to store
        mPause.AddComponent("shop\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.SHOP); })
        // Switch to loadouts menu
        .AddComponent("edit loadouts\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {
            if (component._selectorType == MenuComponent.SelectorType.NORMAL)
              CommonEvents._SwitchMenu(MenuType.SELECT_LOADOUT);
          })
          .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            var obscured = false;
            if (PlayerScript._Players != null)
              foreach (var player in PlayerScript._Players)
              {
                if (player == null || player._ragdoll == null || player._ragdoll._dead) continue;
                if (MathC.Get2DDistance(player.transform.position, PlayerspawnScript._PlayerSpawns[0].transform.position) > 1.2f)
                {
                  obscured = true;
                  break;
                }
              }
            if (obscured)
            {
              component._selectorType = MenuComponent.SelectorType.QUESTION;
              component._textColor = _COLOR_GRAY;
            }

            if (component._focused)
            {
              if (obscured)
                component.SetDisplayText($"</color><color=red>edit loadouts</color><color=white> <-- cannot edit loadouts during a level; go to the start, restart, or die\n");
            }
            else
              component.SetDisplayText("edit loadouts\n");
          });
      }

      if (!GameScript._EditorTesting || (GameScript._EditorTesting && !GameScript._EditorEnabled))
        // Restart the map
        mPause.AddComponent("restart\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {
            GameScript.TogglePause();
            _InPause = false;
            _InMenus = false;
            _Menu.gameObject.SetActive(false);
            TileManager.ReloadMap();
          });

      if (Levels._LevelPack_Playing)
      {
        // Restart the level pack
        mPause.AddComponent("restart level pack\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {
            GameScript.TogglePause();
            _InPause = false;
            _InMenus = false;
            _Menu.gameObject.SetActive(false);
            GameScript.NextLevel(0);
          });
      }

      // Switch to options menu
      var extrachar = GameScript.IsSurvival() ? "\n" : "";
      mPause.AddComponent("options\n" + extrachar, MenuComponent.ComponentType.BUTTON_SIMPLE)
        .AddEvent((MenuComponent component) =>
        {
          CommonEvents._SwitchMenu(MenuType.OPTIONS);
        });

      if (!GameScript.IsSurvival())
      {
        mPause.AddComponent("extras*\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
          .AddEvent((MenuComponent component) =>
          {
            CommonEvents._SwitchMenu(MenuType.EXTRAS);
          })
          .AddEvent(EventType.ON_RENDER, (MenuComponent c) =>
          {
            c._textColor = Settings._Extras_UsingAny ? "magenta" : "yellow";
          });
      }

      //
      if (Levels._LevelPack_Playing)
      {
        // Switch to level select
        mPause
          .AddComponent("exit to level pack select\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
            .AddEvent((MenuComponent component) =>
            {
              // Exit to level select
              _SaveIndex = 5;
              CommonEvents._SwitchMenu(MenuType.MODE_EXIT_CONFIRM);
            })
            .AddEvent(EventType.ON_RENDER, func_exit)
          // Switch to main menu
          .AddComponent("exit to main menu\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
            .AddEvent((MenuComponent component) =>
            {

              // Exit to main menu
              _SaveIndex = 6;
              CommonEvents._SwitchMenu(MenuType.MODE_EXIT_CONFIRM);
            })
            .AddEvent(EventType.ON_RENDER, func_exit);
      }

      else if (GameScript._EditorTesting)
      {
        // Switch to level select
        mPause
          .AddComponent("save and exit to level editor select\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
            .AddEvent((MenuComponent component) =>
            {
              // Exit to level select
              _SaveIndex = 5;
              CommonEvents._SwitchMenu(MenuType.MODE_EXIT_CONFIRM);
            })
            .AddEvent(EventType.ON_RENDER, func_exit)
          // Switch to main menu
          .AddComponent("save and exit to main menu\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
            .AddEvent((MenuComponent component) =>
            {

              // Exit to main menu
              _SaveIndex = 6;
              CommonEvents._SwitchMenu(MenuType.MODE_EXIT_CONFIRM);
            })
            .AddEvent(EventType.ON_RENDER, func_exit);
      }

      else
        // Switch to level select
        mPause
          .AddComponent("exit to level select\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
            .AddEvent((MenuComponent component) =>
            {
              _SaveIndex = 6;
              CommonEvents._SwitchMenu(MenuType.MODE_EXIT_CONFIRM);
            })
            .AddEvent(EventType.ON_RENDER, func_exit)
          // Switch to main menu
          .AddComponent("exit to main menu\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
            .AddEvent((MenuComponent component) =>
            {
              _SaveIndex = 7;
              CommonEvents._SwitchMenu(MenuType.MODE_EXIT_CONFIRM);
            })
            .AddEvent(EventType.ON_RENDER, func_exit);

      // Add pause menu stats (?)
      var format_stats = "=<color={0}>{1,-15}</color>{2,-11}{3,-11}{4,-11}{5,-11}\n";
      mPause.AddComponent((GameScript._GameMode == GameScript.GameModes.SURVIVAL ? "\n" : "") + $"\n\n<color={_COLOR_GRAY}>current session stats</color>\n")
        .AddComponent(string.Format(format_stats, "white", "===", "kills", "deaths", Settings._NumberPlayers > 1 ? "teamkills" : "", GameScript._GameMode == GameScript.GameModes.SURVIVAL ? "points" : "") + "\n");      // Gather player stats
      for (var i = 0; i < 4; i++)
      {
        if (i >= Settings._NumberPlayers)
        {
          mPause.AddComponent("=\n");
          continue;
        }
        var stat = Stats._Stats[i];
        mPause.AddComponent(string.Format(format_stats, GameScript.PlayerProfile._Profiles[i].GetColorName(), $"P{i + 1}/", $"{stat._kills}", $"{stat._deaths}", Settings._NumberPlayers > 1 ? $"{stat._teamkills}" : "", GameScript._GameMode == GameScript.GameModes.SURVIVAL ? $"{stat._points}" : ""));
      };
      // Set the onback function to be resume
      _Menus[MenuType.PAUSE]._onBack = () =>
      {
        _CurrentMenu._selectionIndex = 0;
        SendInput(Input.SPACE);
      };
      // Spawn menu
      _Menus[MenuType.PAUSE]._onSwitchTo += () =>
      {
        SpawnMenu_Pause();
      };
      // Tip
      ModifyMenu_TipComponents(MenuType.PAUSE, (GameScript._GameMode == GameScript.GameModes.SURVIVAL ? 4 : 3));
      ModifyMenu_TipSwitch(MenuType.PAUSE);
    }
    SpawnMenu_Pause();

    // Classic mode menu
    new Menu2(MenuType.GAMETYPE_CLASSIC)
    {

    }
    .AddComponent($"mode: <color={_COLOR_GRAY}>CLASSIC</color>\n\n")
    // Select level
    .AddComponent("select level\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
        if (component._textColor == "")
        {
          CommonEvents._SwitchMenu(MenuType.LEVELS);
        }
        else
        {
          if (!Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART0))
            GenericMenu(
    new string[] { @"no items~1

- you have no items~1

- go to the <color=yellow>SHOP</color> to buy something~1

" },
    "ok",
    MenuType.GAMETYPE_CLASSIC,
    null,
    true
    );
          else
            GenericMenu(
    new string[] { @"make a loadout~1

- you have no loadouts~1

- go to the <color=yellow>EDIT_LOADOUTS</color> menu and equip something~1

" },
    "ok",
    MenuType.GAMETYPE_CLASSIC,
    null,
    true
    );
        }
      })
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        component._textColor = Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART1) ? "" : _COLOR_GRAY;
      })
    // Edit loadout
    .AddComponent("edit loadouts\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
        if (component._textColor != _COLOR_GRAY)
          CommonEvents._SwitchMenu(MenuType.SELECT_LOADOUT);
        else
          GenericMenu(
            new string[] { @"no items~1

you have no items~1

go to the <color=yellow>SHOP</color> to buy something~1

" },
            "ok",
            MenuType.GAMETYPE_CLASSIC,
            null,
            true
            );
      })
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        component._textColor = Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART0) ? (Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART1) ? "" : "yellow") : _COLOR_GRAY;
      })
    // Visit shop
    .AddComponent("shop\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.SHOP); })
      .AddEvent(EventType.ON_RENDER, (MenuComponent c) =>
      {
        c._textColor = Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART0) ? "white" : "yellow";
      })
    // Extras
    .AddComponent("extras*\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.EXTRAS); })
      .AddEvent(EventType.ON_RENDER, (MenuComponent c) =>
      {
        c._textColor = Settings._Extras_UsingAny ? "magenta" : "yellow";
      })
    // Tutorial
    .AddComponent("how to play\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.HOWTOPLAY_CLASSIC); })
      .AddEvent(EventType.ON_RENDER, (MenuComponent c) =>
      {
        c._textColor = Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART1) ? "white" : "yellow";
      })
    // Back
    .AddBackButton((MenuComponent component) =>
    {
      SwitchMenu(MenuType.MODE_SELECTION);
      //GameResources._UI_Player.gameObject.SetActive(false);
    })
    //
    ._onSwitchTo += () =>
    {
      if (_PreviousMenuType == MenuType.MODE_SELECTION)
        if (!Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART0))
          _CurrentMenu._selectionIndex = 2;
        else if (!Shop.Unlocked(Shop.Unlocks.TUTORIAL_PART1))
          _CurrentMenu._selectionIndex = 1;
    };
    // Tip
    ModifyMenu_TipComponents(MenuType.GAMETYPE_CLASSIC, 12);
    ModifyMenu_TipSwitch(MenuType.GAMETYPE_CLASSIC);

    // Survival mode menu
    new Menu2(MenuType.GAMETYPE_SURVIVAL)
    {

    }
    .AddComponent($"mode: <color={_COLOR_GRAY}>SURVIVAL</color>\n\n")
    // Select level
    .AddComponent("select level\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.LEVELS); })
    // Tutorial
    .AddComponent("how to play\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.HOWTOPLAY_SURVIVAL); })
    // Back
    .AddBackButton(MenuType.MODE_SELECTION);
    // Tip
    ModifyMenu_TipComponents(MenuType.GAMETYPE_SURVIVAL, 16);
    ModifyMenu_TipSwitch(MenuType.GAMETYPE_SURVIVAL);

    var format_options = "{0,-25}{1,22}\n";
    // Options menu
    new Menu2(MenuType.OPTIONS)
    {

    }
    .AddComponent($"<color={_COLOR_GRAY}>options</color>\n\n")
    // Game options
    .AddComponent("game options\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.OPTIONS_GAME); })
    // Control options
    .AddComponent("controls\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.CONTROLS); })
    .AddComponent("control options\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.OPTIONS_CONTROLS); })
    .AddComponent("overall stats\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) => { CommonEvents._SwitchMenu(MenuType.STATS); })
    // Back button; switch menu per pause setting
    .AddBackButton((MenuComponent component) =>
    {
      component._menu._selectionIndex = 0;
      CommonEvents._SwitchMenu(_InPause ? MenuType.PAUSE : MenuType.MAIN);
      if (_InPause)
      {
        _CurrentMenu._selectionIndex = 4;
        _CanRender = true;
        RenderMenu();
      }
    });

    // Tip
    ModifyMenu_TipComponents(MenuType.OPTIONS, 15);
    ModifyMenu_TipSwitch(MenuType.OPTIONS);

    // Game settings menu
    var menu_options = new Menu2(MenuType.OPTIONS_SETTINGS)
    {

    }
    .AddComponent($"<color={_COLOR_GRAY}>game</color>\n\n");

    // Game options menu
    var menu_options = new Menu2(MenuType.OPTIONS_GAME)
    {

    }
    .AddComponent($"<color={_COLOR_GRAY}>settings</color>\n\n");

    // Non-console options
    if (Application.platform != RuntimePlatform.PS4 && Application.platform != RuntimePlatform.PS5 && Application.platform != RuntimePlatform.GameCoreXboxSeries && Application.platform != RuntimePlatform.GameCoreXboxOne && Application.platform != RuntimePlatform.XboxOne)
    {
      // Resolution dropdown
      menu_options
      .AddComponent("resolution:\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
          {
            // Set display text to current resolution
            component.SetDisplayText(string.Format(format_options, "resolution:", "" + Settings._ScreenResolution.ToString().Split('@')[0].Trim()));

            // Check if resolution exits; IE monitor may have been changed
            var res_found = false;
            for (var i = 0; i < Screen.resolutions.Length; i++)
            {
              var local_res = Screen.resolutions[i];
              if (local_res.width == Settings._ScreenResolution.width && local_res.height == Settings._ScreenResolution.height && local_res.refreshRate == Settings._ScreenResolution.refreshRate)
              {
                res_found = true;
                break;
              }
            }

            var selection_match = Settings._ScreenResolution.ToString().Split('@')[0].Trim();
            if (!res_found)
            {
              var max = Settings.GetSafeMaxResolution();
              selection_match = $"{max.width} x {max.height}";
            }

            // Set the dropdown selections to available resolutions
            var selections = new List<string>();
            var actions = new List<System.Action<MenuComponent>>();

            foreach (var res in Screen.resolutions)
            {
              var res_split = res.ToString().Split('@')[0].Trim();
              if (selections.Contains(res_split)) continue;
              // Add resolution as selection
              selections.Add(res_split);
              // Action to change resolution
              actions.Add((MenuComponent component0) =>
              {
                var resolutionText = component0.GetDisplayText().Substring(4).Trim();
                if (System.Text.RegularExpressions.Regex.Match(resolutionText, "<color=").Success)
                  resolutionText = resolutionText.Split('>')[1].Split('<')[0];
                var refresh_rate = Settings._ScreenResolution.refreshRate;

                // Check if current refresh rate is available for this resolution
                var found_refresh = false;
                foreach (var res_ in Screen.resolutions)
                  if (res_.refreshRate == refresh_rate && resolutionText == $"{res_.width} x {res_.height}")
                    found_refresh = true;

                // Gather highest refresh
                if (!found_refresh)
                  for (var i = Screen.resolutions.Length - 1; i >= 0; i--)
                  {
                    var res_ = Screen.resolutions[i];
                    //if (res_.refreshRate == 29 || res_.refreshRate == 30 || res_.refreshRate == 59 || res_.refreshRate == 60 || res_.refreshRate == 120 || res_.refreshRate == 144)
                    if (resolutionText == $"{res_.width} x {res_.height}")
                    {
                      refresh_rate = res_.refreshRate;
                      break;
                    }
                  }

                Debug.LogError($"{resolutionText} @ {refresh_rate}Hz");
                Settings.SetResolution($"{resolutionText} @ {refresh_rate}Hz");
              });
            }
            // Update dropdown data
            component.SetDropdownData("resolution\n\n", selections, actions, selection_match);
          })
      // Hertz
      .AddComponent("refresh rate:\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
        {
          // Set display text to current resolution
          var selection_match = Application.targetFrameRate == -1 ? "max" : Settings._ScreenResolution.ToString().Split('@')[1].Trim();
          component.SetDisplayText(string.Format(format_options, "refresh rate:", selection_match));
          // Set the dropdown selections to available resolutions
          var selections = new List<string>();
          var actions = new List<System.Action<MenuComponent>>();
          foreach (var res in Screen.resolutions)
          {
            //if (res.refreshRate == 29 || res.refreshRate == 30 || res.refreshRate == 59 || res.refreshRate == 60 || res.refreshRate == 120 || res.refreshRate == 144)
            if (res.width == Settings._ScreenResolution.width && res.height == Settings._ScreenResolution.height)
            {
              var res_split = res.ToString().Split('@')[1].Trim();
              if (selections.Contains(res_split)) continue;
              // Add resolution as selection
              selections.Add(res_split);
              // Action to change resolution
              actions.Add((MenuComponent component0) =>
        {
          var refreshText = component0.GetDisplayText().Substring(4).Trim();
          if (System.Text.RegularExpressions.Regex.Match(refreshText, "<color=").Success)
            refreshText = refreshText.Split('>')[1].Split('<')[0];
          var resolutionText = component0._menu._menuComponentsSelectable[component._buttonIndex - 1].GetDisplayText(false).Split(':')[1].Trim();
          Settings._UseDefaultTargetFramerate = false;
          Application.targetFrameRate = int.Parse(refreshText.Split('H')[0]);
          Settings.SetResolution(resolutionText + " @ " + refreshText);
        });
            }
          }
          // Add 'max' framerate
          selections.Add("max");
          actions.Add((MenuComponent component0) =>
          {
            Application.targetFrameRate = -1;
            Settings._UseDefaultTargetFramerate = true;
          });
          // Update dropdown data
          component.SetDropdownData("refresh rate\n*if having performance issues; change to a lower number than 'max'\n\n", selections, actions, selection_match);
        })
      // Window mode toggle
      .AddComponent("window type:\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
        {
          // Set display text
          string selection = Settings._Fullscreen ? "fullscreen" : "windowed";
          component.SetDisplayText(string.Format(format_options, "window type:", selection));

          // Set dropdown data
          var selections = new List<string>();
          var actions = new List<System.Action<MenuComponent>>();
          var selection_match = selection;
          selections.Add("windowed");
          actions.Add((MenuComponent component0) =>
          {

            // Change to windowed mode
            Settings._Fullscreen = false;
          });
          selections.Add("fullscreen");
          actions.Add((MenuComponent component0) =>
          {

            // Make sure resolution is supported
            var res_found = false;
            for (var i = 0; i < Screen.resolutions.Length; i++)
            {
              var local_res = Screen.resolutions[i];
              if (local_res.width == Settings._ScreenResolution.width && local_res.height == Settings._ScreenResolution.height && local_res.refreshRate == Settings._ScreenResolution.refreshRate)
              {
                res_found = true;
                break;
              }
            }
            if (!res_found)
            {
              Debug.LogError($"Could not find current resolution when setting fullscreen, setting to safemax");
              Settings.SetResolution($"{Settings.GetSafeMaxResolution()}");
            }
            else
              Debug.LogError($"Changing to fullscreen mode with supported resolution");

            // Change to fullscreen
            Settings._Fullscreen = true;
          });
          // Update dropdown data
          component.SetDropdownData("window type\n\n", selections, actions, selection_match);
        })
      // Quality dropdown
      .AddComponent("quality level\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
        {
          // Set display text
          component.SetDisplayText(string.Format(format_options, "quality level:", Settings._QualityLevel + ""));
          // Set dropdown data
          var selections = new List<string>();
          var actions = new List<System.Action<MenuComponent>>();
          var selection_match = "" + Settings._QualityLevel;
          for (int i = 0; i < 6; i++)
          {
            // Add quality level
            selections.Add((i).ToString());
            // Add action to update quality
            actions.Add((MenuComponent component0) =>
              {
                Settings._QualityLevel = component0._dropdownIndex;
              });
          }
          // Update dropdown data
          component.SetDropdownData("quality level\n*if having performance issues; change to a lower number than 5\n\n", selections, actions, selection_match);
        })
      // VSync
      .AddComponent("vsync\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
        .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
        {
          // Set display text
          var selection = Settings._VSync ? "on" : "off";
          component.SetDisplayText(string.Format(format_options, "vsync:", selection) + "\n");
        })
        // Toggle blood
        .AddEvent((MenuComponent component) =>
        {
          Settings._VSync = !Settings._VSync;
          _CanRender = false;
          RenderMenu();
        });
    }

    // Music volume dropdown
    menu_options
    .AddComponent("music volume\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        // Set display text
        component.SetDisplayText(string.Format(format_options, "music volume:", $"{Settings._VolumeMusic}/5"));
        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = "" + Settings._VolumeMusic;
        for (int i = 0; i < 6; i++)
        {
          // Add volume level
          selections.Add((i).ToString());
          // Add action to update music volume
          actions.Add((MenuComponent component0) =>
          {
            Settings._VolumeMusic = component0._dropdownIndex;
          });
        }
        // Update dropdown data
        component.SetDropdownData("music volume\n\n", selections, actions, selection_match);
      })
    // SFX volume dropdown
    .AddComponent("sfx volume\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        // Set display text
        component.SetDisplayText(string.Format(format_options, "sfx volume:", $"{Settings._VolumeSFX}/5") + "\n");
        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = "" + Settings._VolumeSFX;
        for (int i = 0; i < 6; i++)
        {
          // Add volume level
          selections.Add((i).ToString());
          // Add action to update sfx volume
          actions.Add((MenuComponent component0) =>
          {
            Settings._VolumeSFX = component0._dropdownIndex;
          });
        }
        // Update dropdown data
        component.SetDropdownData("sfx volume\n\n", selections, actions, selection_match);
      })

    // Toggle lightning
    .AddComponent("lightning\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        // Set display text
        var display_toggle = Settings._Toggle_Lightning._value ? "on" : "off";
        component.SetDisplayText(string.Format(format_options, "lightning:", $"{display_toggle}") + "\n");
        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = display_toggle;
        for (var i = 0; i < 2; i++)
        {
          // Toggle lightning
          switch (i)
          {
            case 0:
              selections.Add("on");
              // Add action to update sfx volume
              actions.Add((MenuComponent component0) =>
              {
                Settings._Toggle_Lightning._value = true;
              });
              break;
            case 1:
              selections.Add("off");
              // Add action to update sfx volume
              actions.Add((MenuComponent component0) =>
              {
                Settings._Toggle_Lightning._value = false;
              });
              break;
          }
        }
        // Update dropdown data
        component.SetDropdownData("lightning\n\n", selections, actions, selection_match);
      })

    // Camera type
    .AddComponent("camera type\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {

        // Set display text
        var type_text = Settings._CameraType._value ? "2D" : "3D";
        component.SetDisplayText(string.Format(format_options, "camera type:", $"{type_text}"));

        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = type_text;
        for (var i = 0; i < 2; i++)
        {
          selections.Add(i == 0 ? "3D" : "2D");
          actions.Add((MenuComponent component0) =>
          {
            var is_ortho = component0._dropdownIndex == 1;
            if (Settings._CameraZoom == 3 && is_ortho)
            {
              Settings._CameraZoom._value = 1;
            }

            Settings._CameraType._value = is_ortho;
            Settings.SetPostProcessing();
          });
        }

        // Update dropdown data
        component.SetDropdownData("camera type\n\n", selections, actions, selection_match);
      })
    // Camera zoom dropdown
    .AddComponent("camera zoom\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {

        // Set display text
        var zoom_text = Settings._CameraZoom == 0 ? "close" : Settings._CameraZoom == 1 ? "normal" : Settings._CameraZoom == 2 ? "far" : "auto [2D mode only]";
        component.SetDisplayText(string.Format(format_options, "camera zoom:", $"{zoom_text}") + "\n");

        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = zoom_text;
        for (int i = 0; i < 4; i++)
        {
          // Add selection titles
          selections.Add(i == 0 ? "close" : i == 1 ? "normal" : i == 2 ? "far" : "auto [2D mode only]");

          //
          if (i == 3)
          {
            actions.Add((MenuComponent component0) =>
            {
              if (!Settings._CameraType._value)
              {
                Settings._CameraType._value = true;
              }

              Settings._CameraZoom._value = component0._dropdownIndex;
              Settings.SetPostProcessing();
            });
          }

          //
          else
            actions.Add((MenuComponent component0) =>
            {
              Settings._CameraZoom._value = component0._dropdownIndex;
              Settings.SetPostProcessing();
            });
        }

        // Update dropdown data
        component.SetDropdownData("camera zoom\n\n", selections, actions, selection_match);
      })

    // Blood toggle
    .AddComponent("blood\n\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {

        // Set display text
        var selection = Settings._Blood ? "on" : "off";
        component.SetDisplayText(string.Format(format_options, "blood:", selection) + '\n');
      })

      // Toggle blood
      .AddEvent((MenuComponent component) =>
      {
        Settings._Blood = !Settings._Blood;
        _CanRender = false;
        RenderMenu();
      })

    // Force keyboard toggle
    .AddComponent("force keyboard\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {

        // Set display text
        var selection = Settings._ForceKeyboard ? "on" : "off";
        component.SetDisplayText(string.Format(format_options, "force keyboard:", selection));

        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = $"{selection} -";
        selections.Add("on - use this if you want to play with controllers and the keyboard");
        actions.Add((MenuComponent component0) =>
        {
          Settings._ForceKeyboard = true;
          if (ControllerManager._NumberGamepads > 0)
          {
            GameScript.PlayerProfile._Profiles[1]._directionalAxis = GameScript.PlayerProfile._Profiles[0]._directionalAxis;
            GameScript.PlayerProfile._Profiles[0]._directionalAxis = new float[3];
          }
        });
        selections.Add("off - use this if you want to play with controllers, ignoring the keyboard [DEFAULT]");
        actions.Add((MenuComponent component0) =>
        {
          Settings._ForceKeyboard = false;
          if (ControllerManager._NumberGamepads > 0)
          {
            GameScript.PlayerProfile._Profiles[0]._directionalAxis = GameScript.PlayerProfile._Profiles[1]._directionalAxis;
            GameScript.PlayerProfile._Profiles[1]._directionalAxis = new float[3];
          }
        });

        // Update dropdown data
        component.SetDropdownData("force keyboard as controller - REMEMBER this setting\n\n", selections, actions, selection_match);
      })

    // Controller rumble
    .AddComponent("controller vibration\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {

        // Set display text
        var selection = Settings._ControllerRumble ? "on" : "off";
        component.SetDisplayText(string.Format(format_options, "controller vibration:", selection) + '\n');

        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = $"{selection} -";
        selections.Add("on - the controller will vibrate when you die [DEFAULT]");
        actions.Add((MenuComponent component0) =>
        {
          Settings._ControllerRumble = true;
        });
        selections.Add("off - the controller will never vibrate");
        actions.Add((MenuComponent component0) =>
        {
          Settings._ControllerRumble = false;
        });

        // Update dropdown data
        component.SetDropdownData("controller vibration\n\n", selections, actions, selection_match);
      })

    // Level end behavior
    .AddComponent("level completion\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        // Set display text
        var selection = "next level";
        switch (Settings._LevelCompletion._value)
        {
          case 1:
            selection = "reload level";
            break;
          case 2:
            selection = "nothing";
            break;
          case 3:
            selection = "previous level";
            break;
        }
        component.SetDisplayText(string.Format(format_options, "level completion:", selection));

        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();

        selections.Add("next level     - load the next level [DEFAULT]");
        actions.Add((MenuComponent component0) =>
        {
          Settings._LevelCompletion._value = 0;
          _CanRender = false;
          RenderMenu();
        });
        selections.Add("reload level   - replay the same level");
        actions.Add((MenuComponent component0) =>
        {
          Settings._LevelCompletion._value = 1;
          _CanRender = false;
          RenderMenu();
        });
        selections.Add("nothing        - nothing is loaded or happens");
        actions.Add((MenuComponent component0) =>
        {
          Settings._LevelCompletion._value = 2;
          _CanRender = false;
          RenderMenu();
        });
        selections.Add("previous level - load the previous level");
        actions.Add((MenuComponent component0) =>
        {
          Settings._LevelCompletion._value = 3;
          _CanRender = false;
          RenderMenu();
        });

        // Update dropdown data
        component.SetDropdownData("when you beat a level, what should happen?\n\n", selections, actions, selection);
      })

    // Show tips
    .AddComponent("show tips\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        // Set display text
        var selection = Settings._ShowTips ? "on" : "off";
        component.SetDisplayText(string.Format(format_options, "show tips:", selection) + "\n");
        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = $"{selection} -";
        selections.Add("on - show game tips in some of the menus [DEFAULT]");
        actions.Add((MenuComponent component0) =>
        {
          Settings._ShowTips = true;
          _CanRender = false;
          RenderMenu();
        });
        selections.Add("off - do not display tips in menus");
        actions.Add((MenuComponent component0) =>
        {
          Settings._ShowTips = false;
          _CanRender = false;
          RenderMenu();
        });
        // Update dropdown data
        component.SetDropdownData("show tips\n\n", selections, actions, selection_match);
      })
    // Delete all save data menu
    .AddComponent("delete save data\n\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEventFront((MenuComponent component) =>
      {
        Settings._DeleteSaveDataIter = 4;
      })
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        selections.Add($"yes - press {Settings._DeleteSaveDataIter + 1} more times");
        actions.Add((MenuComponent component0) =>
        {
          if (Settings._DeleteSaveDataIter-- <= 0)
          {
            // Save some settings
            var save_music = Settings._VolumeMusic;
            var save_sfx = Settings._VolumeSFX;
            var save_res = Settings._ScreenResolution;
            var save_fullscreen = Settings._Fullscreen;
            var save_quality = Settings._QualityLevel;
            var save_vsync = Settings._VSync;
            var save_blood = Settings._Blood;
            var save_forcekeyboard = Settings._ForceKeyboard;
            var save_rumble = Settings._ControllerRumble;
            // Erase save data
            PlayerPrefs.DeleteAll();
            // Do not pause
            _InPause = false;
            _SaveMenuDir = _SaveLevelSelected = -1;
            // Reload tutorial
            GameScript.TutorialInformation._HasRestarted = false;
            // Reload settings
            Settings.Init();
            Settings._VolumeMusic = save_music;
            Settings._VolumeSFX = save_sfx;
            Settings.SetResolution("" + save_res);
            Settings._Fullscreen = save_fullscreen;
            Settings._QualityLevel = save_quality;
            Settings._VSync = save_vsync;
            Settings._Blood = save_blood;
            Settings._ForceKeyboard = save_forcekeyboard;
            Settings._ControllerRumble = save_rumble;
            Shop.Init();
            foreach (var loadout in GameScript.ItemManager.Loadout._Loadouts)
            {
              loadout._two_weapon_pairs = false;
              loadout.Load();
            }
            foreach (var profile in GameScript.PlayerProfile._Profiles)
            {
              profile._loadoutIndex = 0;
              profile.UpdateIcons();
            }
            // Erase level data
            for (int i = 0; i < Settings._LevelsCompleted.Count; i++)
              Settings._LevelsCompleted[Levels._LevelCollections[i]._name] = new List<int>();
            // Erase achievement saves
            //SteamManager.Achievements.Reset();
            // Press back button
            component0._menu._menuComponent_last._onSelected?.Invoke(component0._menu._menuComponent_last);
            // Music
            if (FunctionsC.MusicManager._CurrentTrack == 1)
              FunctionsC.MusicManager.TransitionTo(0);
          }
        });
        // Update dropdown data
        component.SetDropdownData("delete save data\n*this <color=red>cannot</color> be undone. resets level completion, <color=yellow>unlocks</color>, stats, and control settings.\n*does not delete saved maps / workshop content\n\n", selections, actions, "");
      })
    // Back button
    .AddBackButton(MenuType.OPTIONS);
    // Tip
    //ModifyMenu_TipComponents(MenuType.OPTIONS_GAME, 11, 1);
    //ModifyMenu_TipSwitch(MenuType.OPTIONS_GAME);

    _Menus[MenuType.OPTIONS_GAME]._onSwitched += () =>
    {
      if (!Settings._Blood)
        TileManager.ResetParticles();
    };

    // Credits
    var m_cred = new Menu2(MenuType.CREDITS)
    {

    };
    m_cred._onSwitchTo += () =>
    {
      GameResources._UI_Player.gameObject.SetActive(false);

      var credits = $@"definitely sneaky but not sneaky


<color={_COLOR_GRAY}>programming, level / sound design</color>
thomas sullivan


<color={_COLOR_GRAY}>various models</color>
www.reddit.com/u/quaterniusdev


<color={_COLOR_GRAY}>music</color>
";
      foreach (string musicCredit in FunctionsC.MusicManager._TrackNames)
        credits += $"<color={_COLOR_GRAY}>{musicCredit.ToLower()}</color>, kevin macleod (incompetech.com)\nlicensed under creative commons: by attribution 3.0\nhttp://creativecommons.org/licenses/by/3.0/\n\n";

      _Text.text = credits;
      _Text.transform.localPosition = new Vector3(-6f, -3.5f, -3.03f);
      if (FunctionsC.MusicManager._CurrentTrack != 2)
        FunctionsC.MusicManager.TransitionTo(2);

      IEnumerator scrollCredits()
      {
        float t = 1f;
        while (t > 0f)
        {
          _Text.transform.localPosition = new Vector3(-6f, Mathf.Lerp(-3.5f, 30.15f, 1f - t), -3.03f);
          t -= 0.0001f * (ControllerManager.GetAnyButton() && t < 0.98f ? 20f : 1f);
          yield return new WaitForSecondsRealtime(0.01f);
        }
        SwitchMenu(MenuType.MAIN);
      }
      GameScript._Singleton.StartCoroutine(scrollCredits());
    };
    m_cred._onSwitched += () =>
    {
      GameResources._UI_Player.gameObject.SetActive(true);
    };

    // Classic how to play
    new Menu2(MenuType.HOWTOPLAY_CLASSIC)
    {

    }
    .AddComponent(
    $@"<color={_COLOR_GRAY}>how to play - CLASSIC mode</color>~1


<color={_COLOR_GRAY}>overview</color>~2
in this mode, you will complete levels.~2 collect the <color=yellow>CUBE</color>~1 and bring
it back to the start of the level.~2 complete levels quickly
to earn ranks and earn <color=yellow>money ($$$)</color> to spend at the SHOP.~2 customize
your loadouts in the EDIT_LOADOUT menu.~2


<color={_COLOR_GRAY}>special controls</color>~2
cycle between multiple custom loadouts using the <color=yellow>LEFT and RIGHT D-PAD</color>
buttons.~2 for basic controls for the game, see CONTROLS in the
OPTIONS menu~2 or just figure it out as you play.~2


<color=cyan>quick start guide</color>:~2
* <color=yellow>purchase</color> an item from the SHOP~2
* <color=yellow>equip</color> the item to a loadout in the EDIT_LOADOUT menu~2
* <color=yellow>select</color> a level to play in the LEVEL_SELECT menu~1



")
    .AddBackButton(MenuType.GAMETYPE_CLASSIC, "very cool");

    // Survival how to play
    new Menu2(MenuType.HOWTOPLAY_SURVIVAL)
    {

    }
    .AddComponent(
    $@"<color={_COLOR_GRAY}>how to play - SURVIVAL mode</color>~1


<color={_COLOR_GRAY}>overview</color>~2
in this mode, you will fend off waves of enemies.~2 killing enemies
gives you points,~2 spend points on upgrades in-game to get stronger.~2
the mode never ends,~1 try to last as long as possible.~1


<color={_COLOR_GRAY}>special controls</color>~2
there are no loadouts.~1 buy items in-game with the <color=red>B or CIRCLE</color> button~2
or specify a side to buy for with the <color=yellow>LEFT and RIGHT D-PAD</color> buttons.~2
for other controls for SURVIVAL mode, see CONTROLS in the OPTIONS
menu.~2


<color={_COLOR_GRAY}>notes</color>~2
* if you die, you lose your items and upgrades.~2
* you have two sets of weapons;~1 meaning you can hold 4 weapons total.~2



")
    .AddBackButton(MenuType.GAMETYPE_SURVIVAL, "very cool");

    // Editor how to use
    new Menu2(MenuType.HOWTOPLAY_EDITOR)
    {

    }
    .AddComponent(
    $@"<color={_COLOR_GRAY}>how to use - EDITOR LEVEL</color>~1


<color={_COLOR_GRAY}>overview</color>~2
with the level editor, you are able to make any levels you have seen in the
<color=yellow>CLASSIC mode</color> and more.~2 string together levels into level packs. edit per level
options in the level pack editor such as: <color=yellow>level theme, order, and hard-set
loadout</color>.~1 finally, publish or overwrite your own level packs, and download
others' level packs on the <color=yellow>steam workshop</color>.~1

<color={_COLOR_GRAY}>special controls</color>~2
level editing controls are restricted to <color=red>mouse and keyboard</color>~1- all
keymappings are shown in the editor.~1 level testing will be done with
a gampad if plugged in.~1


<color={_COLOR_GRAY}>notes</color>~2
* updating levels in a level pack requires overwriting them.~2
* naming scheme for levels and levelpacks is a-z, 0-9, and '-'.~2



")
    .AddBackButton(MenuType.EDITOR_MAIN, "ok then");

    // Confirm exit mode
    new Menu2(MenuType.MODE_EXIT_CONFIRM)
    {

    }
    .AddComponent("are you sure you want to exit this level?\n\n")
    // Exit to level selection or main menu
    .AddComponent("exit level\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {

        // Check for custom level pack
        if (Levels._LevelPack_Playing)
        {

          Levels._LevelPack_Playing = false;
          GameScript._EditorTesting = false;

          var menutype = _Menus[MenuType.PAUSE]._selectionIndex == _Menus[MenuType.PAUSE]._menuComponentsSelectable.Count - 1 ? MenuType.MAIN : MenuType.EDITOR_PACKS;
          CommonEvents._SwitchMenu(menutype);

          if (menutype == MenuType.EDITOR_PACKS)
          {
            Levels._LevelPack_SelectingLevelsFromPack = false;
            _CurrentMenu._selectionIndex = Levels._LevelPacks_Play_SaveIndex;
            _CanRender = false;
            RenderMenu();
            SendInput(Input.SPACE);
          }
        }

        // Check for level editor
        else if (GameScript._EditorTesting)
        {
          GameScript._EditorTesting = false;

          // Save map data
          if (GameScript._EditorEnabled)
          {
            GameScript._EditorEnabled = false;
            var mapdata = TileManager.SaveMap();
            TileManager.SaveFileOverwrite(mapdata);
            TileManager.EditorDisabled(null);
          }

          var menutype = _Menus[MenuType.PAUSE]._selectionIndex == _Menus[MenuType.PAUSE]._menuComponentsSelectable.Count - 1 ? MenuType.MAIN : MenuType.EDITOR_LEVELS;
          CommonEvents._SwitchMenu(menutype);

          if (menutype == MenuType.EDITOR_LEVELS)
          {
            _CurrentMenu._selectionIndex = Levels._LevelEdit_SaveIndex;
            RenderMenu();
          }
        }

        // Normal pause
        else
        {
          var switchMenu = _Menus[MenuType.PAUSE]._selectionIndex == (GameScript._GameMode == GameScript.GameModes.CLASSIC ? 6 : 3) ? MenuType.LEVELS : MenuType.MAIN;
          CommonEvents._SwitchMenu(switchMenu);
          _InPause = false;
        }
      })
      .AddEvent(EventType.ON_RENDER, func_exit)
    .AddBackButton(MenuType.PAUSE, "back")
      .AddEvent((MenuComponent c) =>
      {
        _CurrentMenu._selectionIndex = _SaveIndex;
        _CurrentMenu._selectedComponent._onFocus?.Invoke(_CurrentMenu._selectedComponent);
        _CanRender = true;
        RenderMenu();
      });

    // Control options menu
    new Menu2(MenuType.OPTIONS_CONTROLS)
    {

    }
    .AddComponent($"<color={_COLOR_GRAY}>control options</color>\n\n")
    // Player index
    .AddComponent("player\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        // Set display text
        component.SetDisplayText(string.Format(format_options, "player:", (GameScript.PlayerProfile._CurrentSettingsProfileID + 1) + ""));
        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = (GameScript.PlayerProfile._CurrentSettingsProfileID + 1) + "";
        for (int i = 0; i < 4; i++)
        {
          // Add quality level
          selections.Add($"player {(i + 1).ToString()}");
          // Add action to update quality
          actions.Add((MenuComponent component0) =>
    {
      GameScript.PlayerProfile._CurrentSettingsProfileID = component0._dropdownIndex;
    });
        }
        // Update dropdown data
        component.SetDropdownData("player selector - choose which players' controls to edit\n\n", selections, actions, selection_match);
      })
    // Color
    .AddComponent("color\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        var colors = new string[] { "blue", "red", "yellow", "cyan", "white", "black", "orange" };
        // Set display text
        component.SetDisplayText(string.Format(format_options, "color:", colors[GameScript.PlayerProfile._CurrentSettingsProfile._playerColor]));
        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = colors[GameScript.PlayerProfile._CurrentSettingsProfile._playerColor];
        for (int i = 0; i < GameScript.PlayerProfile._Colors.Length; i++)
        {
          selections.Add($"{colors[i]}");
          // Add action to update profile color
          actions.Add((MenuComponent component0) =>
    {
      GameScript.PlayerProfile._CurrentSettingsProfile._playerColor = component0._dropdownIndex;
      GameScript.PlayerProfile._CurrentSettingsProfile.CreateHealthUI(GameScript.PlayerProfile._CurrentSettingsProfile._player == null ? 1 : GameScript.PlayerProfile._CurrentSettingsProfile._player._ragdoll._health);
    });
        }
        // Update dropdown data
        component.SetDropdownData("color select\n\n", selections, actions, selection_match);
      })
    /*/ Run toggle
    .AddComponent("run\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        // Set display text
        var selection = GameScript.PlayerProfile._CurrentSettingsProfile._holdRun ? "press" : "toggle";
        component.SetDisplayText(string.Format(formatter, "run:", selection));
        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = $"{selection} -";
        selections.Add("press - press the run button to run");
        actions.Add((MenuComponent component0) =>
        {
          GameScript.PlayerProfile._CurrentSettingsProfile._holdRun = true;
        });
        selections.Add("toggle - press the run button to toggle running");
        actions.Add((MenuComponent component0) =>
        {
          GameScript.PlayerProfile._CurrentSettingsProfile._holdRun = false;
        });
        // Update dropdown data
        component.SetDropdownData("run setting\n\n", selections, actions, selection_match);
      })*/
    // Reload both weapons at same time
    .AddComponent("reload setting\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        // Set display text
        var selection = GameScript.PlayerProfile._CurrentSettingsProfile._reloadSidesSameTime ? "both" : "one_at_a_time";
        component.SetDisplayText(string.Format(format_options, "reload setting:", selection));
        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = $"{selection} -";
        selections.Add("both -        - press the reload button to reload weapons at the same time [DEFAULT]");
        actions.Add((MenuComponent component0) =>
        {
          GameScript.PlayerProfile._CurrentSettingsProfile._reloadSidesSameTime = true;
        });
        selections.Add("one_at_a_time - press the reload button to reload weapons one at a time");
        actions.Add((MenuComponent component0) =>
        {
          GameScript.PlayerProfile._CurrentSettingsProfile._reloadSidesSameTime = false;
        });
        // Update dropdown data
        component.SetDropdownData("reload setting\n\n", selections, actions, selection_match);
      })
    // Walk direction
    .AddComponent("face walk direction\n", MenuComponent.ComponentType.BUTTON_DROPDOWN)
      .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
      {
        // Set display text
        var selection = GameScript.PlayerProfile._CurrentSettingsProfile._faceMovement ? "on" : "off";
        component.SetDisplayText(string.Format(format_options, "face walk direction:", selection));
        // Set dropdown data
        var selections = new List<string>();
        var actions = new List<System.Action<MenuComponent>>();
        var selection_match = $"{selection} -";
        selections.Add("on - face the direction you are moving if not aiming [DEFAULT]");
        actions.Add((MenuComponent component0) =>
        {
          GameScript.PlayerProfile._CurrentSettingsProfile._faceMovement = true;
        });
        selections.Add("off - ignore the direction you are moving, only face your aim direction");
        actions.Add((MenuComponent component0) =>
        {
          GameScript.PlayerProfile._CurrentSettingsProfile._faceMovement = false;
        });
        // Update dropdown data
        component.SetDropdownData("walk direction setting\n\n", selections, actions, selection_match);
      })
    // Back button
    .AddBackButton(MenuType.OPTIONS);
    // Tip
    //ModifyMenu_TipComponents(MenuType.OPTIONS_CONTROLS, 16, 1);
    //ModifyMenu_TipSwitch(MenuType.OPTIONS_CONTROLS);

    // Extras
    void SpawnMenu_Extras()
    {
      var format_extras = "{0,-12}- {1,-50}";
      var format_extras2 = "{0,-20}: {1,-50}";
      var format_extras3 = "<color={2}>{0,-20}</color>: {1,-50}";
      var menu_extras = new Menu2(MenuType.EXTRAS)
      {

      }
      .AddComponent($"<color={_COLOR_GRAY}>extras</color>\n\n")
      .AddComponent($"settings here affect the <color={_COLOR_GRAY}>CLASSIC</color> mode!\n*you cannot save best time with extras on\n\n");

      // Wrapper function to add component to extras menu
      void AddExtraSelection(
        string prompt,
        System.Func<string> prompt_value_logic,
        DropdownSelectionComponent[] dropdown_selection_datas,
        string dropdown_prompt,

        string hint,
        System.Func<bool> visibility_conditions,

        string line_end = "\n")
      {

        // Check if component should be visible; add dropdown placeholder for later
        if (visibility_conditions.Invoke())
        {
          menu_extras.AddComponent($"placeholder{line_end}", MenuComponent.ComponentType.BUTTON_DROPDOWN)
            .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
            {
              var selection = prompt_value_logic.Invoke();

              // Set dropdown data
              var selections = new List<string>();
              var actions = new List<System.Action<MenuComponent>>();
              var selection_match = $"{selection}";

              var index = 0;
              var defaultSelection = false;
              foreach (var component_data in dropdown_selection_datas)
              {
                if (index == 0 && component_data._selectionPrompt == selection) { defaultSelection = true; }
                selections.Add(string.Format(format_extras, $"{component_data._selectionPrompt}", $"{component_data._selectionDescription}" + (index++ == 0 ? " [DEFAULT]" : "")));
                actions.Add((MenuComponent component0) =>
                {
                  component_data.on_selected.Invoke(component0);
                });
              }

              // Update dropdown data
              component.SetDropdownData($"{dropdown_prompt}\n\n", selections, actions, selection_match);

              // Set display text
              if (defaultSelection)
              {
                component.SetDisplayText(string.Format(format_extras3 + line_end, $"{prompt}", selection, _COLOR_GRAY));
              }
              else
              {
                component.SetDisplayText(string.Format(format_extras3 + line_end, $"{prompt}*", selection, "magenta"));
              }
            });
        }

        // Obfuscate component and show hint for unlock
        else
        {
          menu_extras.AddComponent($"???{line_end}", MenuComponent.ComponentType.BUTTON_SIMPLE)
            .AddEvent(EventType.ON_RENDER, (MenuComponent component) =>
            {
              // Set hint text when focused
              component._selectorType = MenuComponent.SelectorType.QUESTION;
              if (component._focused)
              {
                component.SetDisplayText(string.Format(format_extras2 + line_end, "???", hint));
              }
              else
              {
                component.SetDisplayText($"???{line_end}");
              }
            });
        }
      }

      // Gravity direction
      AddExtraSelection(
        "gravity",
        () => { return Settings._Extra_Gravity == 1 ? "inverted" : Settings._Extra_Gravity == 2 ? "north" : Settings._Extra_Gravity == 3 ? "none" : "normal"; },
        new DropdownSelectionComponent[] {
          new DropdownSelectionComponent("normal", "normal gravity", (MenuComponent component) => {
            Settings._Extra_Gravity = 0;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
          }),
          new DropdownSelectionComponent("inverted", "gravity go up!", (MenuComponent component) => {
            Settings._Extra_Gravity = 1;
            Physics.gravity = new Vector3(0f, 9.81f, 0f);
          }),
          new DropdownSelectionComponent("north", "gravity go... up?", (MenuComponent component) => {
            Settings._Extra_Gravity = 2;
            Physics.gravity = new Vector3(0f, 0f, 9.81f);
          }),
          new DropdownSelectionComponent("none", "no gravity...", (MenuComponent component) => {
            Settings._Extra_Gravity = 3;
            Physics.gravity = Vector3.zero;
          }),
        },
        "set gravity's direction",

        "unlock by completing sneaky level 80, solo, with just a knife and silenced pistol",
        () => { return true; Shop.Unlocked(Shop.Unlocks.EXTRA_GRAVITY); }
      );

      // Remove bat guy
      AddExtraSelection(
        "chaser",
        () =>
        {
          switch (Settings._Extra_RemoveBatGuy._value)
          {
            case 0:
              return "on";
            case 1:
              return "on - always";
            case 2:
              return "off";
          }
          return "N/A";
        },
        new DropdownSelectionComponent[] {
        new DropdownSelectionComponent("on", "in sneakier difficulty, a person with a bat will chase you", (MenuComponent component) => {
          Settings._Extra_RemoveBatGuy._value = 0;
        }),
        new DropdownSelectionComponent("on - always", "a person with a bat will chase you in any difficulty", (MenuComponent component) => {
          Settings._Extra_RemoveBatGuy._value = 1;
        }),
        new DropdownSelectionComponent("off", "...that guy won't exist", (MenuComponent component) => {
          Settings._Extra_RemoveBatGuy._value = 2;
        }),
        },
        "modify the chasing guy",

        "unlock by...",
        () => { return true; Shop.Unlocked(Shop.Unlocks.EXTRA_CHASER); }
      );


      // Ammo
      AddExtraSelection(
        "player ammo",
        () =>
        {
          switch (Settings._Extra_PlayerAmmo._value)
          {
            case 0:
              return "1x";
            case 1:
              return "2x";
            case 2:
              return "0.5x";
            case 3:
              return "infinite";
          }
          return "N/A";
        },
        new DropdownSelectionComponent[] {
          new DropdownSelectionComponent("1x", "", (MenuComponent component) => {
            Settings._Extra_PlayerAmmo._value = 0;
          }),
          new DropdownSelectionComponent("2x", "", (MenuComponent component) => {
            Settings._Extra_PlayerAmmo._value = 1;
          }),
                    new DropdownSelectionComponent("0.5x", "", (MenuComponent component) => {
            Settings._Extra_PlayerAmmo._value = 2;
          }),
                    new DropdownSelectionComponent("infinite", "", (MenuComponent component) => {
            Settings._Extra_PlayerAmmo._value = 3;
          }),
        },
        "change the max ammo of your weapons / utilities",

        "unlock by...",
        () => { return true; return Shop.Unlocked(Shop.Unlocks.EXTRA_CHASER); },

        "\n\n"
      );

      // Superhot
      AddExtraSelection(
        "time",
        () => { return Settings._Extra_Superhot ? "movement" : "normal"; },
        new DropdownSelectionComponent[] {
        new DropdownSelectionComponent("normal", "time is normal", (MenuComponent component) => {
            Settings._Extra_Superhot = false;
        }),
        new DropdownSelectionComponent("movement", "time only moves when you move", (MenuComponent component) => {
            Settings._Extra_Superhot = true;
        }),
        },
        "set the speed that time passes",

        "unlock by completing sneaky level 40, solo, with just a knife",
        () => { return true; Shop.Unlocked(Shop.Unlocks.EXTRA_TIME); }
      );

      // Crazy zombies
      AddExtraSelection(
        "horde",
        () => { return Settings._Extra_CrazyZombies ? "on" : "off"; },
        new DropdownSelectionComponent[] {
        new DropdownSelectionComponent("off", "no horde", (MenuComponent component) => {
            Settings._Extra_CrazyZombies = false;
        }),
        new DropdownSelectionComponent("on", "a horde spawns until you pick up the cube", (MenuComponent component) => {
            Settings._Extra_CrazyZombies = true;
        }),
        },
        "toggle a horde mode",

        "unlock by...",
        () => { return true; Shop.Unlocked(Shop.Unlocks.EXTRA_HORDE); }
      );

      // Enemy multiplier
      AddExtraSelection(
        "enemy multiplier",
        () =>
        {
          switch (Settings._Extra_EnemyMultiplier._value)
          {
            case 0:
              return "1x";
            case 1:
              return "2x";
            case 2:
              return "0x";
          }
          return "N/A";
        },
        new DropdownSelectionComponent[] {
        new DropdownSelectionComponent("1x", "normal amount of enemies", (MenuComponent component) => {
          Settings._Extra_EnemyMultiplier._value = 0;
        }),
        /*new DropdownSelectionComponent("2x", "double the amount of enemies...", (MenuComponent component) => {
          Settings._Extra_EnemyMultiplier._value = 1;
        }),*/
        new DropdownSelectionComponent("0x", "no enemies", (MenuComponent component) => {
          Settings._Extra_EnemyMultiplier._value = 2;
        }),
        },
        "modify the number of enemies initially spawned",

        "unlock by...",
        () => { return true; return Shop.Unlocked(Shop.Unlocks.EXTRA_CHASER); }
      );

      // Blood type
      AddExtraSelection(
        "blood type",
        () =>
        {
          switch (Settings._Extra_BloodType._value)
          {
            case 0:
              return "normal";
            case 1:
              return "confetti";
          }
          return "N/A";
        },
        new DropdownSelectionComponent[] {
        new DropdownSelectionComponent("normal", "blood", (MenuComponent component) => {
          Settings._Extra_BloodType._value = 0;
        }),
        new DropdownSelectionComponent("confetti", "party time", (MenuComponent component) => {
          Settings._Extra_BloodType._value = 1;
        }),
        },
        "change what blood looks like",

        "unlock by...",
        () => { return true; return Shop.Unlocked(Shop.Unlocks.EXTRA_CHASER); }
      );

      // Body explode
      AddExtraSelection(
        "explode on death",
        () =>
        {
          switch (Settings._Extra_BodyExplode._value)
          {
            case 0:
              return "off";
            case 1:
              return "all";
            case 2:
              return "enemies";
            case 3:
              return "players";
          }
          return "N/A";
        },
        new DropdownSelectionComponent[] {
        new DropdownSelectionComponent("off", "", (MenuComponent component) => {
          Settings._Extra_BodyExplode._value = 0;
        }),
        new DropdownSelectionComponent("all", "", (MenuComponent component) => {
          Settings._Extra_BodyExplode._value = 1;
        }),
        new DropdownSelectionComponent("enemies", "", (MenuComponent component) => {
          Settings._Extra_BodyExplode._value = 2;
        }),
        new DropdownSelectionComponent("players", "", (MenuComponent component) => {
          Settings._Extra_BodyExplode._value = 3;
        }),
        },
        "explode on death",

        "unlock by...",
        () => { return true; return Shop.Unlocked(Shop.Unlocks.EXTRA_CHASER); },

        "\n\n"
      );

      // Back button
      menu_extras.AddBackButton((MenuComponent component) =>
      {
        if (_InPause)
        {
          _Menus[MenuType.PAUSE]._selectionIndex = 5;
          SwitchMenu(MenuType.PAUSE);
        }
        else
        {
          _Menus[MenuType.GAMETYPE_CLASSIC]._selectionIndex = 3;
          SwitchMenu(MenuType.GAMETYPE_CLASSIC);
        }
      });
      // Spawn menu
      _Menus[MenuType.EXTRAS]._onSwitchTo += () =>
      {
        SpawnMenu_Extras();
      };
      // Tip
      //ModifyMenu_TipComponents(MenuType.EXTRAS, 14);
      //ModifyMenu_TipSwitch(MenuType.EXTRAS);
    }
    SpawnMenu_Extras();

    // Controllers changed
    new Menu2(MenuType.CONTROLLERS_CHANGED)
    {

    }
    .AddComponent($"<color={_COLOR_GRAY}>controller(s) removed</color>\n\n")
    .AddComponent("plug in the missing controller(s) to resume or press ok to restart the level with less people\n\n\n")
    .AddComponent("...\n", MenuComponent.ComponentType.BUTTON_SIMPLE)
    .AddComponent("ok", MenuComponent.ComponentType.BUTTON_SIMPLE)
      .AddEvent((MenuComponent component) =>
      {
        if (ControllerManager._NumberGamepads == 0)
          _Confirmed_SwitchToKeyboard = true;

        GameScript.TogglePause();
        _InPause = false;
        _InMenus = false;
        _Menu.gameObject.SetActive(false);
      });

    // Hide menu colliders
    foreach (var menu in _Menus)
      menu.Value.ToggleColliders(false);

    // Render splash screen
    RenderMenu();
  }

  static public void CustomMenu_DifficultyComplete(int difficulty)
  {
    _Menus[MenuType.DIFFICULTY_COMPLETE] = new Menu2(MenuType.DIFFICULTY_COMPLETE)
    {

    }
    .AddComponent("<color=yellow>difficulty beaten</color>\n\n")
    .AddComponent(difficulty == 0 ? "you have unlocked a new difficulty for the CLASSIC mode!\n\nchange difficulty in the level selection menu.\n\n" : "you have beaten the hardest difficulty!\n\ncheck out the survival mode!\n\n")
    .AddBackButton(MenuType.LEVELS, difficulty == 0 ? "cool" : "wow");
    SwitchMenu(MenuType.DIFFICULTY_COMPLETE);
  }

  public static bool _CanRender = true;

  public static bool _CheckMouse = false;
  static Vector2 _LastMousePosition;
  // Update all menus
  public static void UpdateMenus()
  {
    // Check hide mouse
    if (!_CheckMouse)
    {
      _CheckMouse = (ControllerManager.GetMousePosition() - _LastMousePosition).magnitude > 10f;
      if (_CheckMouse)
        Cursor.visible = true;
    }
    _LastMousePosition = ControllerManager.GetMousePosition();

    if (!_InMenus) return;

    if (CanSendInput())
    {

      // Update menus
      foreach (var menu in _Menus)
        menu.Value._onUpdate?.Invoke();

      // Check input
      {
        if (_CheckMouse)
        {
          // Check mouse highlight
          RaycastHit h;
          var r = GameResources._Camera_Menu.ScreenPointToRay(ControllerManager.GetMousePosition());
          if (Physics.SphereCast(r, 0.1f, out h))
          {
            foreach (var component in _CurrentMenu._menuComponents)
            {
              if (!component._collider) continue;

              if (component._collider.GetInstanceID() == h.collider.GetInstanceID() &&
                _CurrentMenu._selectionIndex != component._buttonIndex)
              {
                // Trigger focus and unfocus component events
                if (component._type != MenuComponent.ComponentType.DISPLAY)
                {
                  _CurrentMenu._selectedComponent._onUnfocus?.Invoke(_CurrentMenu._selectedComponent);
                  _CurrentMenu._selectionIndex = component._buttonIndex;
                }
                component._onFocus?.Invoke(component);
                if (_CurrentMenu._timeDisplayed > 0.2f)
                {
                  _CanRender = false;
                  RenderMenu();
                }
                break;
              }
            }
          }

          // Mouse click
          if (ControllerManager.GetMouseInput(0, ControllerManager.InputMode.DOWN) && FunctionsC.IsMouseOverGameWindow())
            SendInput(Input.SPACE);
        }
      }

    }

    // Update screen
    if (_CanRender)
    {
      // Check for wait
      if (_WaitTime > 0f) { _WaitTime -= Time.unscaledDeltaTime; return; }
      // Parse text
      var length = 1;
      var text = "";
      bool display = true,
        inRichText = false,
        onRender = false;
      var richTextNum = 0;
      var richTextNest = 0;
      for (int i = 0; i < length; i++)
      {
        if (_TextBuffer.Length == 0 ||
          length == _TextBuffer.Length + 1 ||
          System.Text.RegularExpressions.Regex.Matches(_Text.text, "\n").Count == _Max_Height + 1)
        {
          _CanRender = false;
          onRender = true;
          break;
        };
        var nextChar = _TextBuffer[i];
        var nextNextChar = i < _TextBuffer.Length - 1 && i >= 0 ? _TextBuffer[i + 1] : ' ';
        if (nextChar == '~')
        {
          _WaitTime = float.Parse(_TextBuffer[i + 1] + "", System.Globalization.CultureInfo.InvariantCulture);
          display = false;
          length++;
        }
        else if (nextChar == ' ' || nextChar == '\n')
        {
          length++;
        }
        else if (nextChar == '<')
        { // <color> asdsad </color> .... <color> a <color> </color> </color>
          inRichText = true;
          length++;
          if (nextNextChar != '/')
            richTextNest++;
        }
        else if (inRichText)
        {
          if (nextChar == '>' && richTextNum++ % 2 == 1 && --richTextNest == 0)
            inRichText = false;
          else
            length++;
        }
        text += nextChar;
      }
      if (length < _TextBuffer.Length + 1) _TextBuffer = _TextBuffer.Substring(length);
      if (display)
        _Text.text += text;

      if (onRender) _CurrentMenu._onRendered?.Invoke();
      // Check for typing noise
      if (text.Trim() != "")
        if (_PlayTime <= 0f)
        {
          _PlayTime = 0.04f;
          PlayNoise(Noise.TYPE);
        }
      _PlayTime -= Time.unscaledDeltaTime;
    }
  }

  // Toggle collider selections
  void ToggleColliders(bool toggle)
  {
    foreach (var component in _menuComponents)
    {
      if (component._collider)
        component._collider.enabled = toggle;
      component._collider.gameObject.SetActive(toggle);
    }
  }

  // Draw current menu
  public static void RenderMenu()
  {
    _CurrentMenu.Render();
  }

  public enum Input
  {
    UP,
    DOWN,
    SPACE,
    BACK
  }

  static bool CanSendInput()
  {
    // Check if application is focused
    if (!Application.isFocused) return false;

    // Check time
    if (Time.unscaledTime - GameScript._LastPause < 0.1f) return false;

    // Check if not in menus
    if (!_InMenus) return false;

    // Check for editor input
    if (TileManager.EditorMenus._Menu_Map_Rename.gameObject.activeSelf) return false;
    if (TileManager.EditorMenus._Menu_Workshop_Infos.gameObject.activeSelf) return false;

#if UNITY_STANDALONE
    if (SteamManager.SteamMenus._DialogueMenuShown) return false;
#endif

    return true;
  }
  public static void SendInput(Input input)
  {
    if (!CanSendInput())
    {

#if UNITY_STANDALONE
      if (SteamManager.SteamMenus._DialogueMenuShown)
        if (input == Input.SPACE || input == Input.BACK)
          SteamManager.SteamMenus.HideInformationDialogue();
#endif

      return;
    }

    // Switch input
    switch (input)
    {
      // Arrow keys
      case Input.UP:
        if (_CurrentMenu._menuComponentsSelectable.Count == 0) break;
        _CurrentMenu._selectedComponent._onUnfocus?.Invoke(_CurrentMenu._selectedComponent);
        _CurrentMenu._selectionIndex = (_CurrentMenu._selectionIndex - 1) % _CurrentMenu._menuComponentsSelectable.Count;
        // Check for dropdown settings
        if (_CurrentMenu._dropdownCount > 0)
        {
          if (_CurrentMenu._selectionIndex < _CurrentMenu._menuComponentsSelectable.Count - _CurrentMenu._dropdownCount)
            _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable.Count - 1;
        }
        if (_CurrentMenu._selectionIndex < 0) _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable.Count - _CurrentMenu._selectionIndex;
        _CurrentMenu._selectedComponent._onFocus?.Invoke(_CurrentMenu._selectedComponent);
        _CanRender = false;
        RenderMenu();
        // Play noise
        PlayNoise(Noise.TYPE);
        break;
      case Input.DOWN:
        if (_CurrentMenu._menuComponentsSelectable.Count == 0) break;
        _CurrentMenu._selectedComponent._onUnfocus?.Invoke(_CurrentMenu._selectedComponent);
        _CurrentMenu._selectionIndex = (_CurrentMenu._selectionIndex + 1) % _CurrentMenu._menuComponentsSelectable.Count;
        // Check for dropdown settings
        if (_CurrentMenu._dropdownCount > 0)
        {
          if (_CurrentMenu._selectionIndex < _CurrentMenu._menuComponentsSelectable.Count - _CurrentMenu._dropdownCount)
            _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable.Count - _CurrentMenu._dropdownCount;
        }
        _CurrentMenu._selectedComponent._onFocus?.Invoke(_CurrentMenu._selectedComponent);
        _CanRender = false;
        RenderMenu();
        // Play noise
        PlayNoise(Noise.TYPE);
        break;
      // Add select input
      case Input.SPACE:
        if (_TextBuffer != string.Empty && _TextBuffer.Length > 10)
        {
          _CanRender = false;
          RenderMenu();
          break;
        }
        _CurrentMenu._onSpace?.Invoke();
        var save_selected = _CurrentMenu._selectedComponent;
        var save_selectedLast = _CurrentMenu._menuComponent_lastSelected;
        if (save_selected != null && !save_selected._obscured)
        {
          save_selected?._onSelected?.Invoke(save_selected);
          // Check for double select
          if (save_selectedLast != null && save_selectedLast._index == save_selected._index)
            save_selected._onDoubleSelect?.Invoke(save_selected);
          // Play noise
          PlayNoise(Noise.SELECT);
        }
        break;
      // Back button input
      case Input.BACK:
        _CurrentMenu._onBack?.Invoke();
        // Play noise
        PlayNoise(Noise.BACK);
        break;
    }
  }

  // Wrapper struct for adding dropdown selections
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

  // Wrapper for common event to switch menu
  public static void SwitchMenu(MenuType menu)
  {
    CommonEvents._SwitchMenu(menu);
  }

  public enum Noise
  {
    TYPE,
    SELECT,
    MOVE,
    BACK,
    PURCHASE
  }
  static Dictionary<Noise, System.Tuple<AudioSource, float>> _MenuAudio;
  public static System.Tuple<AudioSource, float> GetNoise(Noise noise)
  {
    return _MenuAudio[noise];
  }
  static float _LastNoise;
  public static void PlayNoise(Noise noise)
  {
    if (!_Menu.gameObject.activeSelf) return;
    if (Time.unscaledTime - _LastNoise < 0.05f) return;
    _LastNoise = Time.unscaledTime;
    var audioData = GetNoise(noise);
    var audioSource = audioData.Item1;
    audioSource.volume = audioData.Item2;
    audioSource.pitch = (0.9f + Random.value * 0.15f);
    FunctionsC.PlayOneShot(audioSource, false);
  }

  public static bool CanPause()
  {
    return _Menus.ContainsKey(MenuType.PAUSE);
  }

  static int _EditorSaveIndex;
  public static void OnPause(MenuType afterUnlockMenu)
  {
    _InMenus = true;
    _Menu.gameObject.SetActive(true);
    _Menus[MenuType.PAUSE]._selectionIndex = 0;
    if (Shop._UnlockString != string.Empty)
    {
      Shop.ShowUnlocks(afterUnlockMenu);
      CommonEvents._SwitchMenu(MenuType.GENERIC_MENU);
      PlayNoise(Noise.PURCHASE);
      //GameScript._audioListenerSource.PlayOneShot(GetNoise(Noise.PURCHASE).Item1.clip);
      return;
    }

    // Switch after beating editor levels
    if (afterUnlockMenu == MenuType.EDITOR_LEVELS)
    {
      CommonEvents._SwitchMenu(afterUnlockMenu);
      _CurrentMenu._selectionIndex = _EditorSaveIndex;
      _CurrentMenu._selectedComponent._onFocus?.Invoke(_CurrentMenu._selectedComponent);
      _CanRender = false;
      RenderMenu();
      SendInput(Input.SPACE);
      _CanRender = true;
      RenderMenu();
      return;
    }

    CommonEvents._SwitchMenu(MenuType.PAUSE);
  }

  // Show menu when number of controllers has changed
  public static bool _Confirmed_SwitchToKeyboard;
  public static int _Save_NumPlayers;
  public static void OnControllersChanged(int change, int saveamount)
  {
    if (change >= 0) return;

    // Pause
    GameScript._Paused = true;
    Time.timeScale = 0f;
    _InMenus = true;

    _Save_NumPlayers = saveamount;

    // Set menu prompt
    if (Settings._NumberPlayers == 0)
      _Menus[MenuType.CONTROLLERS_CHANGED]._menuComponents[1].SetDisplayText("looks like your controller got unplugged! plug it back in to resume or\npress 'ok' to play with keyboard instead\n\n");
    else
      if (GameScript._GameMode == GameScript.GameModes.SURVIVAL)
      _Menus[MenuType.CONTROLLERS_CHANGED]._menuComponents[1].SetDisplayText("looks like a controller got unplugged! plug it back in to resume or\npress 'ok' to resume the game (you must manually restart in survival mode)\n\n");
    else
      _Menus[MenuType.CONTROLLERS_CHANGED]._menuComponents[1].SetDisplayText("looks like a controller got unplugged! plug it back in to resume or\npress 'ok' to restart the level and play with less people\n\n");

    // Show controllers changed menu
    _Menu.gameObject.SetActive(true);
    SwitchMenu(MenuType.CONTROLLERS_CHANGED);
  }
  public static void OnControllersChangedFix()
  {
    GameScript.TogglePause();
    _InPause = false;
    _InMenus = false;
    _Menu.gameObject.SetActive(false);
  }

  /*public static void OnLoadoutChanged()
  {
    // Pause
    GameScript._Paused = true;
    Time.timeScale = 0f;
    _InMenus = true;

    GenericMenu(new string[]
    {
      "loadout changed mid-game\n\nyou cannot switch loadouts mid-game\nswitch loadouts when you are dead or reload the map\n\nif you do not switch back to your loadout, "
    })

    // Show generic menu
    _Menu.gameObject.SetActive(true);
    SwitchMenu(MenuType.CONTROLLERS_CHANGED);
  }*/

  public static void HideMenus()
  {
    _InMenus = false;
    _Menu.gameObject.SetActive(false);
  }

  public static void AppendToEditMaps(string map_data)
  {
    // Create new entry in levels
    Levels.LevelEditor_NewMap("pasted map");
    Levels._CurrentLevelCollection._levelData[Levels._CurrentLevelCollection._levelData.Length - 1] = map_data;

    // Reload menu
    CommonEvents._RemoveDropdownSelections(_CurrentMenu._menuComponentsSelectable[0]);
    CommonEvents._SwitchMenu(MenuType.EDITOR_LEVELS);

    _CurrentMenu._selectionIndex = _CurrentMenu._menuComponentsSelectable.Count - 2;
    _CanRender = false;
    RenderMenu();
    _CurrentMenu._selectedComponent._onFocus?.Invoke(_CurrentMenu._selectedComponent);
    SendInput(Input.SPACE);
    _CanRender = false;
    RenderMenu();
  }

  public static void QuickEnableMenus()
  {
    _InMenus = true;
    _InPause = true;
    GameScript._Paused = true;
    Time.timeScale = 0f;

    _CurrentMenu._selectionIndex = 0;
    _CanRender = true;
    RenderMenu();
    _Menu.gameObject.SetActive(true);
  }

}
