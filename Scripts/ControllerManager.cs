using UnityEngine;
using UnityEngine.InputSystem;

public static class ControllerManager
{

  public static UnityEngine.InputSystem.Utilities.ReadOnlyArray<Gamepad> _Gamepads
  {
    get
    {
      return Gamepad.all;
    }
  }
  public static int _NumberGamepads
  {
    get
    {
      return _Gamepads.Count;
    }
  }

  public enum Axis
  {
    LSTICK_X,
    LSTICK_Y,
    RSTICK_X,
    RSTICK_Y,
    DPAD_X,
    DPAD_Y,
    L2,
    R2
  }

  public delegate bool ControllerFunction(string input);

  static Input_action _Hanlder;
  static Input_action.MenuActions _HandlerMenu;
  static Input_action.PlayerActions _HandlerPlayer;

  public static void Init()
  {
    _Hanlder = new Input_action();

    _HandlerMenu = new Input_action.MenuActions(_Hanlder);
    _HandlerMenu.SetCallbacks(new HandlerMenu());

    _HandlerPlayer = new Input_action.PlayerActions(_Hanlder);
    _HandlerPlayer.SetCallbacks(new HandlerPlayer());

    _Hanlder.Enable();
  }

  public static void Update()
  {
    /*if (HandlerPlayer._reloadtimer != -1f)
      HandlerPlayer._reloadtimer += Time.unscaledDeltaTime;
    if(HandlerPlayer._reloadtimer > 1.5f)
    {
      if(!Menu2._InMenus)
        GameScript.TogglePause();
      Menu2.OnLoadoutHold();
      HandlerPlayer._reloadtimer = -1f;
    }*/
  }

  class HandlerMenu : Input_action.IMenuActions
  {
    void Input_action.IMenuActions.OnSelect(InputAction.CallbackContext context)
    {
      if (context.phase != InputActionPhase.Started) return;
      if (!Menu2._InMenus) return;
      Menu2.SendInput(Menu2.Input.SPACE);
      FunctionsC.OnControllerInput();
    }

    void Input_action.IMenuActions.OnBack(InputAction.CallbackContext context)
    {
      if (context.phase != InputActionPhase.Started) return;
      if (!Menu2._InMenus) return;
      Menu2.SendInput(Menu2.Input.BACK);
      FunctionsC.OnControllerInput();
    }
    void Input_action.IMenuActions.OnUp(InputAction.CallbackContext context)
    {
      if (context.phase != InputActionPhase.Started) return;
      if (!Menu2._InMenus) return;
      Menu2.SendInput(Menu2.Input.UP);
      FunctionsC.OnControllerInput();
    }
    void Input_action.IMenuActions.OnDown(InputAction.CallbackContext context)
    {
      if (context.phase != InputActionPhase.Started) return;
      if (!Menu2._InMenus) return;
      Menu2.SendInput(Menu2.Input.DOWN);
      FunctionsC.OnControllerInput();
    }
    void Input_action.IMenuActions.OnLeft(InputAction.CallbackContext context)
    {

    }
    void Input_action.IMenuActions.OnRight(InputAction.CallbackContext context)
    {

    }
    void Input_action.IMenuActions.OnPageLeft(InputAction.CallbackContext context)
    {
      /*if (Menu._CurrentMenu == null) return;
      GameScript.PlayerProfile.OnControllerMove();
      if (Menu._CurrentMenu == Menu._Menu_LevelSelect)
        Menu._CurrentMenu._menuActions[Menu._CurrentMenu._menuActions.Length - 3](0);
      else if (Menu._CurrentMenu == Menu._Menu_LevelSelected)
        GameScript.MoveLevelSelectedMenu(-1);*/
      /*if (!Menu2._InMenus) return;
      PlayerScript p = GetPlayer(context);
      if (p != null)
        p._profile._loadoutIter--;
        */
    }
    void Input_action.IMenuActions.OnPageRight(InputAction.CallbackContext context)
    {
      /*if (Menu._CurrentMenu == null) return;
      GameScript.PlayerProfile.OnControllerMove();
      if (Menu._CurrentMenu == Menu._Menu_LevelSelect)
        Menu._CurrentMenu._menuActions[Menu._CurrentMenu._menuActions.Length - 2](0);
      else if (Menu._CurrentMenu == Menu._Menu_LevelSelected)
        GameScript.MoveLevelSelectedMenu(1);*/
      /*if (!Menu2._InMenus) return;
      PlayerScript p = GetPlayer(context);
      if (p != null)
        p._profile._loadoutIter++;*/
    }
    void Input_action.IMenuActions.OnPause(InputAction.CallbackContext context)
    {
      if (context.phase != InputActionPhase.Started) return;

      // Level editor
      if (GameScript._EditorTesting)
      {

        /*GameScript._EditorTesting = false;

        // If editing, save map
        if (GameScript._EditorEnabled)
        {
          GameScript._EditorEnabled = false;
          TileManager.SaveFileOverwrite(TileManager.SaveMap());
          TileManager.EditorDisabled(null);
        }*/

        // Exit to menus
        if (Menu2._InMenus && Menu2._InPause)
        {
          Menu2._InMenus = false;
          Menu2._Menu.gameObject.SetActive(false);
          Menu2._InPause = false;
          GameScript.TogglePause();

          // Check editing menus
          if (GameScript._EditorEnabled) TileManager.EditorMenus._Menu_Editor.gameObject.SetActive(true);
          else TileManager.EditorMenus._Menu_EditorTesting.gameObject.SetActive(true);
        }
        else if (!Menu2._InMenus)
        {
          GameScript.TogglePause();
          TileManager.EditorMenus.HideMenus();
        }

        return;
      }

      if (GameScript._EditorEnabled) return;
      if (Menu2._InMenus && Menu2._InPause)
      {
        Menu2._InMenus = false;
        Menu2._Menu.gameObject.SetActive(false);
        Menu2._InPause = false;
        GameScript.TogglePause();
      }
      else if (!Menu2._InMenus)
        GameScript.TogglePause();
    }
  }

  public static Gamepad GetPlayerGamepad(int playerID)
  {
    if (_NumberGamepads == 0) return null;
    if (Settings._ForceKeyboard && playerID == 0) return null;
    if (Settings._ForceKeyboard)
      playerID--;
    if (playerID >= _Gamepads.Count)
      return null;
    return _Gamepads[playerID];
  }
  static PlayerScript GetPlayer(InputAction.CallbackContext obj)
  {
    if (PlayerScript._Players == null || PlayerScript._Players.Count == 0) return null;
    // Check keyboard
    if (obj.control.device.name.Equals("Keyboard") && (_NumberGamepads == 0 || Settings._ForceKeyboard))
      return PlayerScript._Players[0];
    // Check controllers
    foreach (var player in PlayerScript._Players)
    {
      var g = GetPlayerGamepad(player._id);
      if (g == null) continue;
      if (g.deviceId == obj.control.device.deviceId)
        return player;
    }
    return null;
  }

  class HandlerPlayer : Input_action.IPlayerActions
  {
    //public static float _reloadtimer = -1f;

    void Input_action.IPlayerActions.OnReload(InputAction.CallbackContext context)
    {
      if (context.phase != InputActionPhase.Started) return;
      // Quick-remove in menu loadout editor
      if (Menu2._InMenus && Menu2._CurrentMenu._type == Menu2.MenuType.EDIT_LOADOUT)
        Menu2._CurrentMenu.QuickRemoveEdit();
    }
    void Input_action.IPlayerActions.OnReloadMap(InputAction.CallbackContext context)
    {
      if (context.phase != InputActionPhase.Started) return;
      if (!Menu2._InMenus)
      {
        PlayerScript p = GetPlayer(context);
        if (p == null)
        {
          TileManager.ReloadMap();
          return;
        }
        p.ReloadMap();
      }
    }
    void Input_action.IPlayerActions.OnCycleWeaponLeft(InputAction.CallbackContext context)
    {

    }
    void Input_action.IPlayerActions.OnCycleWeaponRight(InputAction.CallbackContext context)
    {

    }
    void Input_action.IPlayerActions.OnLaserSight(InputAction.CallbackContext context)
    {
      if (context.phase != InputActionPhase.Started) return;
      if (Menu2._InMenus) return;
      PlayerScript p = GetPlayer(context);
      if (p == null) return;
      p.ToggleLaser();
    }
    void Input_action.IPlayerActions.OnWeaponLeft(InputAction.CallbackContext context) { }
    void Input_action.IPlayerActions.OnWeaponRight(InputAction.CallbackContext context) { }
  }

  public static float GetControllerAxis(int playerID, Axis axis)
  {
    Gamepad g = GetPlayerGamepad(playerID);
    if (g == null) return 0f;
    switch (axis)
    {
      case (Axis.LSTICK_X):
        return g.leftStick.x.ReadValue();
      case (Axis.LSTICK_Y):
        return g.leftStick.y.ReadValue();
      case (Axis.RSTICK_X):
        return g.rightStick.x.ReadValue();
      case (Axis.RSTICK_Y):
        return g.rightStick.y.ReadValue();
      case (Axis.DPAD_X):
        return g.dpad.ReadValue().x;
      case (Axis.DPAD_Y):
        return g.dpad.ReadValue().y;
      case (Axis.L2):
        return g.leftTrigger.ReadValue();
      case (Axis.R2):
        return g.rightTrigger.ReadValue();
    }
    return -1f;
  }

  public enum InputMode
  {
    DOWN,
    UP,
    HOLD
  }
  public static bool GetMouseInput(int iter, InputMode mode)
  {
    if (Mouse.current == null) return false;
    UnityEngine.InputSystem.Controls.ButtonControl button = (iter == 0 ? Mouse.current.leftButton :
      iter == 1 ? Mouse.current.rightButton : Mouse.current.middleButton);
    if (mode == InputMode.HOLD)
      return button.isPressed;
    else if (mode == InputMode.DOWN)
      return button.wasPressedThisFrame;
    return button.wasReleasedThisFrame;
  }
  public static Vector2 GetMousePosition()
  {
    if (Mouse.current == null) return Vector2.zero;
    return Mouse.current.position.ReadValue();
  }

  public enum Key
  {
    W,
    A,
    S,
    D,
    ARROW_U,
    ARROW_D,
    ARROW_L,
    ARROW_R,
    R,
    V,
    SPACE,
    SHIFT_L,
    SHIFT_R,
    T,
    X,
    BACKSPACE,
    DELETE,
    C,
    Z,
    G,
    L,
    O,
    PERIOD,
    PERIOD_NUMPAD,
    BACKSLASH,
    Q,
    E,
    INSERT,
    U,
    N,
    K,
    M,
    B,
    H,
    F,
    PAGE_DOWN,
    PAGE_UP,
    MULTIPLY_NUMPAD,
    HOME,
    ESCAPE,
    END,
    CONTROL_LEFT,
    CONTROL_RIGHT,
    ONE,
    TWO,
    THREE,
    FOUR,
    FIVE,
    SIX,
    SEVEN,
    EIGHT,
    NINE,
    ZERO,
    COMMA,
    TAB,
    MINUS,
    EQUALS,

    F1,
    F2,
    F3,
    F4,
    F5,
    F6,

    BACKQUOTE
  }
  public static bool GetKey(Key key, InputMode mode = InputMode.DOWN)
  {
    UnityEngine.InputSystem.Controls.KeyControl gotKey = null;
    Keyboard keyboard = Keyboard.current;
    if (keyboard == null) return false;
    switch (key)
    {
      case (Key.W):
        gotKey = keyboard.wKey;
        break;
      case (Key.A):
        gotKey = keyboard.aKey;
        break;
      case (Key.S):
        gotKey = keyboard.sKey;
        break;
      case (Key.D):
        gotKey = keyboard.dKey;
        break;
      case (Key.ARROW_U):
        gotKey = keyboard.upArrowKey;
        break;
      case (Key.ARROW_D):
        gotKey = keyboard.downArrowKey;
        break;
      case (Key.ARROW_L):
        gotKey = keyboard.leftArrowKey;
        break;
      case (Key.ARROW_R):
        gotKey = keyboard.rightArrowKey;
        break;
      case (Key.R):
        gotKey = keyboard.rKey;
        break;
      case (Key.SPACE):
        gotKey = keyboard.spaceKey;
        break;
      case (Key.SHIFT_L):
        gotKey = keyboard.leftShiftKey;
        break;
      case (Key.SHIFT_R):
        gotKey = keyboard.leftShiftKey;
        break;
      case (Key.T):
        gotKey = keyboard.tKey;
        break;
      case (Key.V):
        gotKey = keyboard.vKey;
        break;
      case (Key.N):
        gotKey = keyboard.nKey;
        break;
      case (Key.B):
        gotKey = keyboard.bKey;
        break;
      case (Key.X):
        gotKey = keyboard.xKey;
        break;
      case (Key.F):
        gotKey = keyboard.fKey;
        break;
      case (Key.BACKSPACE):
        gotKey = keyboard.backspaceKey;
        break;
      case (Key.BACKSLASH):
        gotKey = keyboard.backslashKey;
        break;
      case (Key.DELETE):
        gotKey = keyboard.deleteKey;
        break;
      case (Key.C):
        gotKey = keyboard.cKey;
        break;
      case (Key.Z):
        gotKey = keyboard.zKey;
        break;
      case (Key.G):
        gotKey = keyboard.gKey;
        break;
      case (Key.L):
        gotKey = keyboard.lKey;
        break;
      case (Key.O):
        gotKey = keyboard.oKey;
        break;
      case (Key.PERIOD):
        gotKey = keyboard.periodKey;
        break;
      case (Key.PERIOD_NUMPAD):
        gotKey = keyboard.numpadPeriodKey;
        break;
      case (Key.MINUS):
        gotKey = keyboard.minusKey;
        break;
      case (Key.EQUALS):
        gotKey = keyboard.equalsKey;
        break;
      case (Key.Q):
        gotKey = keyboard.qKey;
        break;
      case (Key.E):
        gotKey = keyboard.eKey;
        break;
      case (Key.INSERT):
        gotKey = keyboard.insertKey;
        break;
      case (Key.U):
        gotKey = keyboard.uKey;
        break;
      case (Key.M):
        gotKey = keyboard.mKey;
        break;
      case (Key.K):
        gotKey = keyboard.kKey;
        break;
      case (Key.H):
        gotKey = keyboard.hKey;
        break;
      case (Key.PAGE_UP):
        gotKey = keyboard.pageUpKey;
        break;
      case (Key.PAGE_DOWN):
        gotKey = keyboard.pageDownKey;
        break;
      case (Key.MULTIPLY_NUMPAD):
        gotKey = keyboard.numpadMultiplyKey;
        break;
      case (Key.HOME):
        gotKey = keyboard.homeKey;
        break;
      case (Key.ESCAPE):
        gotKey = keyboard.escapeKey;
        break;
      case (Key.END):
        gotKey = keyboard.endKey;
        break;
      case (Key.CONTROL_LEFT):
        gotKey = keyboard.leftCtrlKey;
        break;
      case (Key.CONTROL_RIGHT):
        gotKey = keyboard.rightCtrlKey;
        break;
      case (Key.ONE):
        gotKey = keyboard.digit1Key;
        break;
      case (Key.TWO):
        gotKey = keyboard.digit2Key;
        break;
      case (Key.THREE):
        gotKey = keyboard.digit3Key;
        break;
      case (Key.FOUR):
        gotKey = keyboard.digit4Key;
        break;
      case (Key.FIVE):
        gotKey = keyboard.digit5Key;
        break;
      case (Key.SIX):
        gotKey = keyboard.digit6Key;
        break;
      case (Key.SEVEN):
        gotKey = keyboard.digit7Key;
        break;
      case (Key.EIGHT):
        gotKey = keyboard.digit8Key;
        break;
      case (Key.NINE):
        gotKey = keyboard.digit9Key;
        break;
      case (Key.ZERO):
        gotKey = keyboard.digit0Key;
        break;
      case (Key.COMMA):
        gotKey = keyboard.commaKey;
        break;
      case (Key.TAB):
        gotKey = keyboard.tabKey;
        break;

      case (Key.F1):
        gotKey = keyboard.f1Key;
        break;
      case (Key.F2):
        gotKey = keyboard.f2Key;
        break;
      case (Key.F3):
        gotKey = keyboard.f3Key;
        break;
      case (Key.F4):
        gotKey = keyboard.f4Key;
        break;
      case (Key.F5):
        gotKey = keyboard.f5Key;
        break;

      case Key.BACKQUOTE:
        gotKey = keyboard.backquoteKey;
        break;
    }
    switch (mode)
    {
      case (InputMode.DOWN):
        return gotKey.wasPressedThisFrame;
      case (InputMode.UP):
        return gotKey.wasReleasedThisFrame;
      case (InputMode.HOLD):
        return gotKey.isPressed;
    }
    return false;
  }

  public static bool ShiftHeld()
  {
    return GetAnyKeysHeld(Key.SHIFT_L, Key.SHIFT_R);
  }

  public static bool GetAnyKeysHeld(params Key[] key)
  {
    return GetAnyKey(InputMode.HOLD, key);
  }

  public static bool GetAnyKey(InputMode mode, params Key[] key)
  {

    foreach (var key_ in key)
    {
      if (GetKey(key_, mode))
      {
        return true;
      }
    }

    return false;
  }

  public static bool GetAnyButton()
  {
    // Check for keyboard skips
    if (GetKey(Key.SPACE, InputMode.HOLD) || GetKey(Key.BACKSPACE, InputMode.HOLD) || GetMouseInput(0, InputMode.HOLD))
      return true;
    // Check for controller skips
    if (_NumberGamepads == 0) return false;

    // If _ForceKeyboard, start at player index 1; player index 0 is the keyboard
    int offset = Settings._ForceKeyboard ? 1 : 0;
    for (int i = offset; i < _NumberGamepads + offset; i++)
    {
      var gamepad = GetPlayerGamepad(i);
      if (gamepad.buttonNorth.isPressed || gamepad.buttonSouth.isPressed || gamepad.buttonWest.isPressed ||
        gamepad.buttonEast.isPressed || gamepad.startButton.isPressed || gamepad.selectButton.isPressed)
        return true;
    }
    return false;
  }
}
