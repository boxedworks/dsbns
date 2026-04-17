using System.Collections.Generic;
using Assets.Scripts.Settings;
using Assets.Scripts.Settings.Serialization;
using UnityEngine;

namespace Assets.Scripts.UI.Menus
{
  public class MenuComponent
  {
    //
    static SettingsSaveData SettingsModule { get { return SettingsHelper.s_SaveData.Settings; } }

    //
    public Menu _menu;

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
    public bool _useDropdownPreprompt;
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

    public MenuComponent(Menu menu, string text, ComponentType componentType = ComponentType.DISPLAY)
    {
      _menu = menu;

      _textColor = string.Empty;

      _visible = true;
      _obscured = false;

      _dropdownIndex = -1;

      // Add prior component listener
      _onUnfocus += component =>
      {
        _menu._MenuComponent_lastFocused = component;
      };

      // Add button text for button types
      if (componentType != ComponentType.DISPLAY)
      {
        text = $"{GetEmptySelector()} {text}";

        _onSelected += component =>
        {
          _menu._MenuComponent_lastSelected = component;
        };
      }

      // Add scroll feature
      _onFocus += component =>
      {
        _focused = true;
        if (_menu._MenuComponent_lastFocused == null) return;

        var scrollThreshold = 2;
        if (Menu.s_MaxHeight > Menu.s_StartMaxHeight && component._height <= Menu.s_MaxHeight - Menu.s_StartMaxHeight + scrollThreshold)
          Menu.s_MaxHeight = Mathf.Clamp(Menu.s_MaxHeight - Mathf.Clamp(_menu._MenuComponent_lastFocused._height - component._height, 0, 1000), Menu.s_StartMaxHeight, _menu._MaxHeight);
        else if (component._height >= Menu.s_MaxHeight - scrollThreshold)
          Menu.s_MaxHeight = Mathf.Clamp(Menu.s_MaxHeight - Mathf.Clamp(_menu._MenuComponent_lastFocused._height - component._height, -1000, 0), Menu.s_StartMaxHeight, _menu._MaxHeight);
      };
      _onUnfocus += component =>
      {
        _focused = false;
      };

      // Set dropdown events
      _onSelected += component =>
      {
        if (_menu == null || _menu._MenuComponentsSelectable == null)
          return;
        if (_buttonIndex >= _menu._MenuComponentsSelectable.Count - _menu._DropdownCount)
          return;
        CommonEvents._RemoveDropdownSelections(component);
      };
      if (componentType == ComponentType.BUTTON_DROPDOWN)
      {
        _onSelected += component =>
        {
          if (_dropdownSelections == null) return;
          _menu._DropdownCount = _dropdownSelections.Length;
          _menu._DropdownParentIndex = _menu._SelectionIndex;
          _menu._SelectedComponent._onUnfocus?.Invoke(_menu._SelectedComponent);
          _menu._SelectionIndex = -1;
          Menu.s_Text.text = _menu.GetDisplayText();
          _menu._OnRendered?.Invoke();

          // Create prompt
          var prompt = _useDropdownPreprompt ? $"\n===========\n{_dropdownPrompt}" : _dropdownPrompt;
          _menu.AddComponent(prompt);
          Menu.s_TextBuffer = string.Empty;
          Menu.s_TextBuffer += prompt;

          // Create dropdown selections
          var iter = 0;
          foreach (var selection in _dropdownSelections)
          {
            // Create a simple button for each selection
            _menu.AddComponent($"{selection}\n", ComponentType.BUTTON_SIMPLE)
              // Register selection action
              .AddEvent(_dropdownActions[iter])
              // Set text
              .AddEvent(component0 =>
              {
                if (component0._obscured) return;
                CommonEvents._DropdownSelect(component0);
              })
              // Before selection renders, update text
              .AddEvent(Menu.EventType.ON_RENDER, component0 =>
              {
                component0.SetDisplayText($"{_dropdownSelections[component0._dropdownIndex]}\n");
              })
              // Add focus event
              .AddEvent(Menu.EventType.ON_FOCUS, component0 =>
              {
                if (_dropdownOnFocus == null || component0._dropdownIndex > _dropdownOnFocus.Length - 1) return;
                _dropdownOnFocus[component0._dropdownIndex]?.Invoke(component0);
              })
              .AddEvent(Menu.EventType.ON_UNFOCUS, component0 =>
              {
                if (_dropdownOnUnfocus == null || component0._dropdownIndex > _dropdownOnFocus.Length - 1) return;
                _dropdownOnUnfocus[component0._dropdownIndex]?.Invoke(component0);
              });

            // Add double selected event
            if (_dropdownOnDoubleSelected != null && iter < _dropdownOnDoubleSelected.Length)
              _menu.AddEvent(Menu.EventType.ON_SELECTED_DOUBLE, _dropdownOnDoubleSelected[iter]);

            // Fire on created event
            if (_dropdownOnCreated != null && iter < _dropdownOnCreated.Length)
              _dropdownOnCreated[iter]?.Invoke(_menu._MenuComponent_last);

            // Set selection index to first dropdown selection
            if (_menu._MenuComponent_last._textColor == "white")
              _menu._SelectionIndex = _menu._MenuComponent_last._buttonIndex;

            // Register dropdown index
            _menu._MenuComponent_last._dropdownIndex = iter;

            // Update the text buffer with selection
            var displayText = _menu._MenuComponent_last._obscured ? FunctionsC.GenerateGarbageText(selection) : selection;
            displayText = (_menu._MenuComponent_last._buttonIndex == _menu._SelectionIndex ? "[<color=yellow>*</color>" : "[ ") + $"] <color={_menu._MenuComponent_last._textColor}>{displayText}</color>";
            Menu.s_TextBuffer += $"{displayText}\n";
            iter++;
          }
          _menu._SelectedComponent._onFocus?.Invoke(_menu._SelectedComponent);

          if (Menu.s_TextBuffer.Contains("..") || Menu.s_Text.text.Contains(".."))
          {
            Menu._CanRender = false;
            Menu.RenderMenu();
          }
          else
          {
            if (SettingsModule.TextSpeedFast)
            {
              Menu._CanRender = false;
              Menu.RenderMenu();
            }
            else
              Menu._CanRender = true;
          }
        };
      }

      _displayText = text;
      _type = componentType;
      // Get start text height
      var startHeight = 0;
      // Check height of current text
      var h0 = 0;
      if (Menu.s_Text.text.Contains("\n"))
        h0 = Menu.s_Text.text.Split('\n').Length - 1;
      // Check height of components
      foreach (var component in menu._MenuComponents)
      {
        int h = 0;
        if (component._displayText.Contains("\n"))
          h = component._displayText.Split('\n').Length - 1;
        startHeight += h;
      }
      // Set indexes
      foreach (var component in menu._MenuComponents)
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
      gameObject.transform.parent = Menu.s_Menu;
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
          actions_onCreated.Add(component => { });
      }
      var match_found = false;
      foreach (var selection in selections)
      {
        var color = Menu._COLOR_GRAY; // dark grey;
        if (!match_found &&
          (selection_match != string.Empty && selection.Contains(selection_match) ||
          selection_match == "" && iter == 0))
        {
          match_found = true;
          color = "white";
        }
        actions_onCreated[iter] += component =>
        {
          component._textColor = color;
        };
        var last_char = iter == selections.Count - 1 ? "\n" : "";
        selections_final[iter++] = $"{selection}{last_char}";
      }
      // Add a back button
      selections_final[iter] = "back";
      actions_selected.Add(component0 =>
      {
        component0._menu._SelectionIndex = _buttonIndex;
        CommonEvents._RemoveDropdownSelections(component0);
      });
      actions_onCreated.Add(component =>
      {
        component._textColor = Menu._COLOR_GRAY;
      });

      _dropdownPrompt = prompt;
      _useDropdownPreprompt = true;
      _dropdownSelections = selections_final;
      _dropdownActions = actions_selected.ToArray();
      _dropdownOnCreated = actions_onCreated.ToArray();
      _dropdownOnDoubleSelected = action_onDoubleSelect?.ToArray();
      _dropdownOnFocus = action_onFocus?.ToArray();
      _dropdownOnUnfocus = action_onUnfocus?.ToArray();
    }
  }

}