using System.Collections.Generic;
using Assets.Scripts.Game.Items;
using Assets.Scripts.Ragdoll;
using Assets.Scripts.Ragdoll.Equippables;
using Assets.Scripts.Settings;
using Assets.Scripts.Settings.Serialization;
using Assets.Scripts.UI.Menus;
using UnityEngine;

// Assists with controls
public class PlayerProfile
{
  //
  static SettingsSaveData SettingsModule { get { return SettingsHelper.s_SaveData.Settings; } }
  static LevelSaveData LevelModule { get { return SettingsHelper.s_SaveData.LevelData; } }

  //
  static int s_currentSettingsID;
  public static int s_CurrentSettingsProfileID
  {
    get
    {
      return s_currentSettingsID;
    }
    set
    {
      s_currentSettingsID = value % 4;
      if (s_currentSettingsID < 0)
        s_currentSettingsID = 3;
    }
  }
  public static PlayerProfile s_CurrentSettingsProfile { get { return s_Profiles[s_CurrentSettingsProfileID]; } }

  static int s_iD;
  public static PlayerProfile[] s_Profiles;

  public int _Id;
  public PlayerProfileData _profileSettings { get { return SettingsModule.PlayerProfiles[_Id]; } }

  // The player this profile is attatched to
  public PlayerScript _Player
  {
    get
    {
      if (PlayerScript.s_Players == null) return null;
      foreach (var player in PlayerScript.s_Players)
        if (player == null) continue;
        else if (player._Id == _Id) return player;
      return null;
    }
  }

  public float[] _directionalAxis;

  public int _LoadoutIndex
  {
    get { return _profileSettings.LoadoutIndex; }
    set
    {
      if (Levels._HardcodedLoadout != null && !GameScript.s_EditorTesting) return;
      if (!GameScript.s_IsMissionsGameMode) return;
      if (_Player?._Ragdoll?._IsGrappling ?? false) return;

      // Locate valid loadout to equip
      var iter = Loadout._Loadouts.Length;
      var difference = value - _LoadoutIndex;

      var profileSettings = _profileSettings;
      var setIndex = profileSettings.LoadoutIndex;
      while (iter >= 0)
      {
        setIndex = (setIndex + difference) % Loadout._Loadouts.Length;
        if (setIndex < 0) setIndex = Loadout._Loadouts.Length + setIndex;

        var equipment = Loadout._Loadouts[setIndex]._Equipment;
        if (!equipment.IsEmpty()) break;
        iter--;
      }

      {
        var equipment = Loadout._Loadouts[setIndex]._Equipment;
        if (iter == -1 && equipment.IsEmpty())
          setIndex = 0;
      }

      // Set loadout index
      profileSettings.LoadoutIndex = setIndex;
      SettingsModule.UpdatePlayerProfile(_Id, profileSettings);
      UpdateIcons();

      // If in loadout menu, update colors
      if (Menu.s_InMenus)
      {
        Menu.PlayNoise(Menu.Noise.LOADOUT_SWAP);
        if (Menu.s_CurrentMenu._Type == Menu.MenuType.SELECT_LOADOUT)
        {
          Menu.TriggerActionSwapTo(Menu.MenuType.SELECT_LOADOUT);
          Menu._CanRender = false;
          Menu.RenderMenu();
        }
      }
    }
  }
  public Equipment _Equipment
  {
    get
    {
      return _Loadout._Equipment;
    }
  }
  public Loadout _Loadout
  {
    get
    {
      // Level packs
      if (Levels._HardcodedLoadout != null && !GameScript.s_EditorTesting) return Levels._HardcodedLoadout;

      // PARTY mode
      if (GameScript.s_IsPartyGameMode) return VersusMode.s_PlayerLoadouts;

      // ZOMBIE mode
      if (GameScript.s_IsZombieGameMode && SurvivalManager.s_PlayerLoadouts != null) return SurvivalManager.s_PlayerLoadouts[_Id];

      // MISSIONS mode
      if (_LoadoutIndex > Loadout._Loadouts.Length)
        _LoadoutIndex = 0;
      return Loadout._Loadouts[_LoadoutIndex];
    }
  }

  public bool _reloadSidesSameTime
  {
    get { return _profileSettings.ReloadSameTime; }
    set
    {
      var profileSettings = _profileSettings;
      profileSettings.ReloadSameTime = value;
      SettingsModule.UpdatePlayerProfile(_Id, profileSettings);
    }
  }

  ItemManager.Items GetItem(ItemManager.Items item, ItemManager.Items other)
  {
    if (item == ItemManager.Items.NONE && !ShopHelper.IsActuallyTwoHanded(other) && _Equipment._Perks.Contains(Perk.PerkType.MARTIAL_ARTIST))
      return ItemManager.Items.FIST;
    return item;
  }

  public ItemManager.Items _ItemLeft
  {
    get
    {
      var item = _EquipmentIndex == 0 ? _Equipment._ItemLeft0 : _Equipment._ItemLeft1;
      var itemOther = _EquipmentIndex == 0 ? _Equipment._ItemRight0 : _Equipment._ItemRight1;
      return GetItem(item, itemOther);
    }
    set { if (_EquipmentIndex == 0) _Equipment._ItemLeft0 = value; else _Equipment._ItemLeft1 = value; }
  }
  public ItemManager.Items _ItemRight
  {
    get
    {
      var item = _EquipmentIndex == 0 ? _Equipment._ItemRight0 : _Equipment._ItemRight1;
      var itemOther = _EquipmentIndex == 0 ? _Equipment._ItemLeft0 : _Equipment._ItemLeft1;
      return GetItem(item, itemOther);
    }
    set { if (_EquipmentIndex == 0) _Equipment._ItemRight0 = value; else _Equipment._ItemRight1 = value; }
  }
  public ItemManager.Items _ItemLeft_Other
  {
    get
    {
      var item = _EquipmentIndex == 1 ? _Equipment._ItemLeft0 : _Equipment._ItemLeft1;
      var itemOther = _EquipmentIndex == 1 ? _Equipment._ItemRight0 : _Equipment._ItemRight1;
      return GetItem(item, itemOther);
    }
    set { if (_EquipmentIndex == 1) _Equipment._ItemLeft0 = value; else _Equipment._ItemLeft1 = value; }
  }
  public ItemManager.Items _ItemRight_Other
  {
    get
    {
      var item = _EquipmentIndex == 1 ? _Equipment._ItemRight0 : _Equipment._ItemRight1;
      var itemOther = _EquipmentIndex == 1 ? _Equipment._ItemLeft0 : _Equipment._ItemLeft1;
      return GetItem(item, itemOther);
    }
    set { if (_EquipmentIndex == 1) _Equipment._ItemRight0 = value; else _Equipment._ItemRight1 = value; }
  }

  // Used for two sets of equipment
  int equipmentIndex;
  public int _EquipmentIndex
  {
    get { return equipmentIndex; }
    set { equipmentIndex = value % 2; }
  }

  public class Equipment
  {
    public Equipment()
    {
      _UtilitiesLeft = new UtilityScript.UtilityType[0];
      _UtilitiesRight = new UtilityScript.UtilityType[0];

      _Perks = new List<Perk.PerkType>();
    }
    public ItemManager.Items _ItemLeft0, _ItemRight0, _ItemLeft1, _ItemRight1;

    public UtilityScript.UtilityType[] _UtilitiesLeft, _UtilitiesRight;
    public List<Perk.PerkType> _Perks;

    public bool HasWeapons0()
    {
      return _ItemLeft0 != ItemManager.Items.NONE ||
        _ItemRight0 != ItemManager.Items.NONE;
    }
    public bool HasWeapons1()
    {
      return _ItemLeft1 != ItemManager.Items.NONE ||
        _ItemRight1 != ItemManager.Items.NONE;
    }

    public bool IsEmpty()
    {
      return !HasWeapons0() && !HasWeapons1() &&
        _UtilitiesLeft.Length == 0 &&
        _UtilitiesRight.Length == 0 &&
        _Perks.Count == 0;
    }
  }

  public static Color[] s_PlayerColors = new Color[] { Color.blue, Color.red, Color.yellow, Color.cyan, Color.green, Color.magenta, new Color(1f, 0.4f, 0f), Color.white, Color.black };
  public Color GetColor()
  {
    return s_PlayerColors[_profileSettings.Color];
  }
  public string GetColorName(bool visual = true)
  {
    switch (_profileSettings.Color)
    {
      case 0:
        return "blue";
      case 1:
        return "red";
      case 2:
        return "yellow";
      case 3:
        return visual ? "cyan" : "#00FFFF";
      case 4:
        return "green";
      case 5:
        return visual ? "magenta" : "#FF00FF";
      case 6:
        return "orange";
      case 7:
        return "white";
      case 8:
        return "black";
    }
    return "";
  }

  public bool _faceMovement
  {
    get
    {
      return _profileSettings.FaceLookDirection;
    }
    set
    {
      var profileSettings = _profileSettings;
      profileSettings.FaceLookDirection = value;
      SettingsModule.UpdatePlayerProfile(_Id, profileSettings);
    }
  }

  public int _playerColor
  {
    get
    {
      return _profileSettings.Color;
    }
    set
    {

      var profileSettings = _profileSettings;

      // Clamp
      profileSettings.Color = value % s_PlayerColors.Length;
      if (profileSettings.Color < 0)
        profileSettings.Color = s_PlayerColors.Length - 1;

      // Save
      SettingsModule.UpdatePlayerProfile(_Id, profileSettings);

      // Update UI
      var ui = GameResources._UI_Player;
      ui.GetChild(_Id).GetChild(0).GetComponent<TextMesh>().color = GetColor();

      // Check for alive player
      if (PlayerScript.s_Players != null)
        foreach (PlayerScript p in PlayerScript.s_Players)
          if (p._Id == _Id && !p._Ragdoll._IsDead) p._Ragdoll.ChangeColor(GetColor());

      // Play SFX
      Menu.PlayNoise(Menu.Noise.COLOR_CHANGE);

      // Update UI
      CreateHealthUI(_Player == null ? 1 : _Player._Ragdoll._health);
    }
  }
  Transform _ui { get { return GameResources._UI_Player.GetChild(_Id); } }
  public Transform _VersusUI;
  TMPro.TextMeshPro _loadoutIndexText;

  MeshRenderer[] _health_UI, _perk_UI;

  public PlayerProfile()
  {
    _Id = s_iD++;
    _VersusUI = _ui.GetChild(6);
    _loadoutIndexText = _ui.GetChild(7).GetComponent<TMPro.TextMeshPro>();

    if (s_Profiles == null) s_Profiles = new PlayerProfile[4];
    s_Profiles[_Id] = this;

    _directionalAxis = new float[3];

    // Check empty loadout
    ChangeLoadoutIfEmpty();

    // Update profile equipment icons
    _playerColor = _playerColor;
    UpdateIcons();
    CreateHealthUI(1);
  }

  //
  void Show()
  {
    var localPosition = _ui.localPosition;
    localPosition.y = 0f;
    _ui.localPosition = localPosition;
  }
  void Hide()
  {
    var localPosition = _ui.localPosition;
    localPosition.y = -200f;
    _ui.localPosition = localPosition;
  }

  public static void ShowAll()
  {
    foreach (var playerProfile in s_Profiles)
      playerProfile.Show();
  }
  public static void HideAll()
  {
    foreach (var playerProfile in s_Profiles)
      playerProfile.Hide();
  }

  // Check empty loadout and switch to a new one if empty
  public void ChangeLoadoutIfEmpty(int max_loadout = 4)
  {
    // Bounds-check _loadoutIndex
    if (max_loadout != 4 && _LoadoutIndex > max_loadout)
      _LoadoutIndex = 0;
    else if (_Equipment.IsEmpty())
      _LoadoutIndex++;

    // Update UI
    UpdateIcons();
  }

  public void HandleMenuInput()
  {

    // Accommodate for _ForceKeyboard; checking for -1 controllerID and controllerID > Gamepad.all.Count
    if (
      SettingsHelper._ForceKeyboard && _Id == 0 ||
      _Id + (SettingsHelper._ForceKeyboard ? -1 : 0) >= ControllerManager._NumberGamepads
      )
    {
      _menuDownTime_down = _menuDownTime_up = 0f;

      // Check color change
      if (ControllerManager.GetKey(ControllerManager.Key.ONE, ControllerManager.InputMode.DOWN))
      {
        var playerColor = _playerColor;
        playerColor--;
        if (playerColor < 0)
          playerColor = s_PlayerColors.Length - 1;
        _playerColor = playerColor;
      }
      if (ControllerManager.GetKey(ControllerManager.Key.TWO, ControllerManager.InputMode.DOWN))
        _playerColor = ++_playerColor % s_PlayerColors.Length;

      return;
    }

    // Check axis hold
    if (!(_menuDownTime_down != 0f && _menuDownTime_up != 0f))
    {
      if (_menuDownTime_down != 0f && Time.unscaledTime - _menuDownTime_down > 0.09f)
      {
        _menuDownTime_down = Time.unscaledTime;

        Down();
      }
      else if (_menuDownTime_up != 0f && Time.unscaledTime - _menuDownTime_up > 0.09f)
      {
        _menuDownTime_up = Time.unscaledTime;

        Up();
      }
    }

    var gamepad = ControllerManager.GetPlayerGamepad(_Id);
    if (gamepad != null)
    {

      // Check color change
      if (gamepad.leftShoulder.wasPressedThisFrame)
      {
        var playerColor = _playerColor;
        playerColor--;
        if (playerColor < 0)
          playerColor = s_PlayerColors.Length - 1;
        _playerColor = playerColor;
      }
      if (gamepad.rightShoulder.wasPressedThisFrame)
        _playerColor = ++_playerColor % s_PlayerColors.Length;

      // Check axis selections
      for (var i = 0; i < 2; i++)
      {
        var y = 0f;
        switch (i)
        {
          case 0:
            y = ControllerManager.GetControllerAxis(_Id, ControllerManager.Axis.LSTICK_Y);
            break;
          case 1:
            y = ControllerManager.GetControllerAxis(_Id, ControllerManager.Axis.DPAD_Y);
            break;
        }

        if (y > 0.75f)
        {
          if (_directionalAxis[i] <= 0f)
          {
            _directionalAxis[i] = 1f;

            SetDownTime(false);
          }
        }

        else if (y < -0.75f)
        {
          if (_directionalAxis[i] >= 0f)
          {
            _directionalAxis[i] = -1f;

            SetDownTime(true);
          }
        }

        else
        {
          if (_directionalAxis[i] != 0f)
          {
            _directionalAxis[i] = 0f;

            SetUpTime(true);
            SetUpTime(false);
          }
        }
      }
    }

  }

  //
  float _menuDownTime_down, _menuDownTime_up;
  public void SetDownTime(bool down)
  {
    if (down)
    {
      if (_menuDownTime_down == 0f)
        Down();

      _menuDownTime_down = Time.unscaledTime + 0.4f;
    }
    else
    {
      if (_menuDownTime_up == 0f)
        Up();

      _menuDownTime_up = Time.unscaledTime + 0.4f;
    }
  }
  public void SetUpTime(bool down)
  {
    if (down)
      _menuDownTime_down = 0f;
    else
      _menuDownTime_up = 0f;
  }

  //
  public void HandleInput()
  {

    // Accommodate for _ForceKeyboard; checking for -1 controllerID and controllerID > Gamepad.all.Count
    if (SettingsHelper._ForceKeyboard && _Id == 0 || _Id + (SettingsHelper._ForceKeyboard ? -1 : 0) >= ControllerManager._NumberGamepads)
    {

      // Check loadout change
      if (ControllerManager.GetKey(ControllerManager.Key.Z))
      {
        if (Menu.s_CurrentMenu._Type == Menu.MenuType.VERSUS)
        {
          if (Menu.s_InMenus)
            VersusMode.IncrementPlayerTeam(_Id, -1);
        }
        else
          _LoadoutIndex--;
      }
      if (ControllerManager.GetKey(ControllerManager.Key.C))
      {
        if (Menu.s_CurrentMenu._Type == Menu.MenuType.VERSUS)
        {
          if (Menu.s_InMenus)
            VersusMode.IncrementPlayerTeam(_Id, 1);
        }
        else
          _LoadoutIndex++;
      }

      return;
    }

    // Check in menus
    if (!Menu.s_InMenus)
      return;

    // Check axis selections
    var gamepad = ControllerManager.GetPlayerGamepad(_Id);
    if (gamepad != null)
    {
      if (gamepad.dpad.left.wasPressedThisFrame)
      {
        if (Menu.s_CurrentMenu._Type == Menu.MenuType.VERSUS)
          VersusMode.IncrementPlayerTeam(_Id, -1);
        else
          _LoadoutIndex--;
      }
      if (gamepad.dpad.right.wasPressedThisFrame)
      {
        if (Menu.s_CurrentMenu._Type == Menu.MenuType.VERSUS)
          VersusMode.IncrementPlayerTeam(_Id, 1);
        else
          _LoadoutIndex++;
      }
    }
  }

  void Up()
  {
    Menu.SendInput(Menu.Input.UP);
    FunctionsC.OnControllerInput();
  }
  void Down()
  {
    Menu.SendInput(Menu.Input.DOWN);
    FunctionsC.OnControllerInput();
  }

  //
  Animation[] _itemAnimations;
  public void Update()
  {

    // Update icon animations
    if (_itemAnimations != null)
      for (int i = 0; i < _itemAnimations.Length; i++)
      {
        var icon = GetItemIcon(i);
        if (icon == null) continue;

        var animation = _itemAnimations[i];
        animation?.Update();
      }
  }

  // Create health icons
  static Color _ExtraHealthColor = Color.gray;
  public void CreateHealthUI(int health)
  {
    _health_UI = new MeshRenderer[] {
        _ui.GetChild(3).gameObject.GetComponent<MeshRenderer>(),
        _ui.GetChild(4).gameObject.GetComponent<MeshRenderer>(),
        _ui.GetChild(5).gameObject.GetComponent<MeshRenderer>()
      };
    foreach (var renderer in _health_UI)
    {
      var color = GetColor();
      renderer.material.color = color;
    }
    UpdateHealthUI(health);
  }

  ItemIcon GetItemIcon(int index)
  {
    var iconType = (IconType)index;
    _itemIcons.TryGetValue(iconType, out var icon);
    return icon;
  }

  //
  public void PlayAnimation(Animation.AnimationType type, float duration, bool isItem, ActiveRagdoll.Side side)
  {
    int animationIndex;
    if (isItem)
      animationIndex = side == ActiveRagdoll.Side.LEFT ? 0 : 1;
    else
      animationIndex = side == ActiveRagdoll.Side.LEFT ? 2 : 3;

    var currentAnimation = _itemAnimations[animationIndex];
    if (currentAnimation != null)
      currentAnimation._OnComplete?.Invoke();
    var newAnimation = new Animation(GetItemIcon(animationIndex)._base, type, duration);
    newAnimation._OnComplete += () =>
    {
      _itemAnimations[animationIndex] = null;
    };
    _itemAnimations[animationIndex] = newAnimation;
  }
  public void TryPlayAnimation(Animation.AnimationType type, float duration, bool isItem, ActiveRagdoll.Side side)
  {
    int animationIndex;
    if (isItem)
      animationIndex = side == ActiveRagdoll.Side.LEFT ? 0 : 1;
    else
      animationIndex = side == ActiveRagdoll.Side.LEFT ? 2 : 3;
    var currentAnimation = _itemAnimations[animationIndex];
    if (currentAnimation != null)
    {
      if (currentAnimation._AnimationType == type)
        return;
    }

    PlayAnimation(type, duration, isItem, side);
  }

  //
  public class Animation
  {

    Transform _transform;

    public enum AnimationType
    {
      None,

      OutOfAmmo,
      Shoot,
      ShootLarge,
      Reload,

      MeleeSlice,
    }
    AnimationType _animationType;
    public AnimationType _AnimationType { get { return _animationType; } }

    public System.Action _OnComplete;

    //
    float _animationTime, _animationDuration;
    Vector3 _animationStartPos, _animationStartEulerAngles;

    public Animation(Transform transform, AnimationType animationType, float duration)
    {
      _transform = transform;
      _animationType = animationType;

      _animationStartPos = _transform.localPosition;
      _animationStartEulerAngles = _transform.localEulerAngles;

      _animationDuration = duration;
      _animationTime = 0f;

      _OnComplete += () =>
      {
        _transform.localPosition = _animationStartPos;
        _transform.localEulerAngles = _animationStartEulerAngles;
      };
    }

    //
    public void Update()
    {

      // Check if animation finished
      bool isInfiniteDuration = _animationDuration <= 0f;
      if (!isInfiniteDuration && _animationTime > _animationDuration)
      {
        _OnComplete?.Invoke();
        return;
      }
      _animationTime += Time.deltaTime;

      var animationTimeNormalized = _animationTime / _animationDuration;
      var endPos = _transform.position;

      // Apply animation effect based on type
      switch (_animationType)
      {
        case AnimationType.OutOfAmmo:
          var shakeIntensity = 0.15f;
          var shakeDisplacement = Random.insideUnitSphere * shakeIntensity * 0.1f;
          _transform.localPosition = _animationStartPos + shakeDisplacement;
          break;

        case AnimationType.Shoot:
          shakeIntensity = 0.75f * (1f - animationTimeNormalized);
          shakeDisplacement = Random.insideUnitSphere * shakeIntensity * 0.1f;
          _transform.localPosition = _animationStartPos + shakeDisplacement;
          break;
        case AnimationType.ShootLarge:
          shakeIntensity = 1.5f * (1f - animationTimeNormalized);
          shakeDisplacement = Random.insideUnitSphere * shakeIntensity * 0.1f;
          _transform.localPosition = _animationStartPos + shakeDisplacement;
          break;

        case AnimationType.Reload:
          var rotationAngle = 360f * animationTimeNormalized;
          if (rotationAngle > 180f)
            rotationAngle = 360f - rotationAngle;
          var angles = _animationStartEulerAngles - new Vector3(rotationAngle * 0.2f, 0f, 0f);
          _transform.localEulerAngles = angles;

          break;

        case AnimationType.MeleeSlice:

          var moveDistance = 0.12f;
          var moveDirection = Vector3.right;

          var moveOffset = moveDirection * moveDistance * Mathf.Sin(animationTimeNormalized * Mathf.PI);
          _transform.localPosition = _animationStartPos + moveOffset;

          break;
      }
    }

    //
    public void OnAnimationRemoved()
    {

      switch (_animationType)
      {
      }
    }

  }

  // Remove health icons
  public void RemoveHealthUI()
  {
    if (_health_UI != null && _health_UI.Length > 0)
    {
      for (int i = _health_UI.Length - 1; i >= 0; i--)
        Object.Destroy(_health_UI[i]);
      _health_UI = null;
    }
  }

  public void UpdateHealthUI()
  {
    UpdateHealthUI(_Player._Ragdoll._health);
  }
  public void UpdateHealthUI(int health)
  {
    for (int i = 0; i < _health_UI.Length; i++)
    {
      _health_UI[i].gameObject.SetActive(i < health);
      if (health > 3)
      {
        if (i < health - 3)
          _health_UI[i].material.color = _ExtraHealthColor;
        else
          _health_UI[i].material.color = GetColor();
      }
      else
        _health_UI[i].material.color = GetColor();
    }
  }

  public enum IconType
  {
    ITEM_LEFT,
    ITEM_RIGHT,
    UTILITY_LEFT,
    UTILITY_RIGHT
  }

  Dictionary<IconType, ItemIcon> _itemIcons;
  class ItemIcon
  {
    public Transform _base, _ammoUI;
    public Transform[] _ammo;
    public int _ammoCount, _ammoVisible;

    public Vector3 _baseLocalPosition;

    static Vector2 _Offset = new Vector2(0.1f, 0.08f);

    public void Destroy()
    {
      Object.Destroy(_base.gameObject);
      foreach (var t in _ammo)
        Object.Destroy(t.gameObject);
      Object.Destroy(_ammoUI.gameObject);
    }

    public void Init(ItemManager.ItemUIInfo data, int index, System.Tuple<PlayerScript, bool, ActiveRagdoll.Side> item_data, bool is_utility)
    {
      _base = data._Transform;
      _base.localPosition += new Vector3(_Offset.x, _Offset.y, 0f);
      _baseLocalPosition = _base.localPosition;
      _ammoCount = _ammoVisible = data._ClipSize;

      // Spawn ammo border
      GameResources.s_AmmoSideUi.text = !is_utility ? "" : item_data.Item3 == ActiveRagdoll.Side.LEFT ? "L" : "R";
      _ammoUI = Object.Instantiate(GameResources.s_AmmoUi).transform;
      _ammoUI.gameObject.layer = 11;
      _ammoUI.parent = _base.parent;
      _ammoUI.localPosition = new Vector3(0.8f + index * 0.8f, -0.15f, 0f);
      _ammoUI.localEulerAngles = new Vector3(90f, 0f, 0f);
      _ammoUI.localScale = new Vector3(0.8f, 0.004f, 0.25f);

      // Create ammo meshes
      _ammo = new Transform[_ammoCount];
      Vector3 localScale = new Vector3(0.8f / _ammoCount * (_ammoCount >= 3 ? (_ammoCount >= 12 ? 0.5f : 0.7f) : 0.82f), 0.18f, 0.001f);

      // Hide ammo if in-game
      var ammo = _ammoCount;
      if (item_data.Item1 != null)
      {
        var player = item_data.Item1;
        var isUtility = item_data.Item2;
        var side = item_data.Item3;
        if (!isUtility)
        {
          if (side == ActiveRagdoll.Side.LEFT)
            ammo = player._Ragdoll._ItemL != null ? player._Ragdoll._ItemL.GetClip() : ammo;
          else
            ammo = player._Ragdoll._ItemR != null ? player._Ragdoll._ItemR.GetClip() : ammo;
        }
        else if (side == ActiveRagdoll.Side.LEFT)
          ammo = player._UtilitiesLeft != null ? player._UtilitiesLeft.Count : ammo;
        else
        {
          if (player._HasTwin)
            ammo = player._ConnectedTwin._UtilitiesRight != null ? player._ConnectedTwin._UtilitiesRight.Count : ammo;
          else
            ammo = player._UtilitiesRight != null ? player._UtilitiesRight.Count : ammo;
        }
      }
      if (ammo != _ammoCount) _ammoVisible = ammo;

      // Create meshes
      for (int i = 0; i < _ammoCount; i++)
      {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.layer = 11;
        _ammo[i] = cube.transform;
        _ammo[i].parent = _base.parent;
        _ammo[i].localScale = localScale;
        _ammo[i].localEulerAngles = Vector3.zero;
        _ammo[i].localPosition = new Vector3(0.36f + index * 0.8f + _Offset.x, -0.23f + _Offset.y, 0f) + new Vector3(Mathf.Lerp(0f, 0.7f, (float)i / _ammoCount), 0f, 0f) + new Vector3(localScale.x / 2f, 0f, 0f);
        _ammo[i].GetComponent<Renderer>().sharedMaterial = GameScript.s_Singleton._ItemMaterials[0];
        if (i >= ammo) _ammo[i].gameObject.SetActive(false);
      }
    }

    // Update bullets
    public void Clip_Deincrement()
    {
      if (_ammoVisible <= 0 || _ammoVisible - 1 >= _ammo.Length) return;
      _ammo[--_ammoVisible].gameObject.SetActive(false);
    }
    public void Clip_Reload(int amount)
    {

      // Reload all
      if (amount < 1)
      {
        foreach (var t in _ammo)
          t.gameObject.SetActive(true);
        _ammoVisible = _ammoCount;
      }

      // Reload not all
      else
      {
        for (var i = 0; i < amount; i++)
        {
          if (_ammoVisible >= _ammo.Length) return;
          _ammo[_ammoVisible++].gameObject.SetActive(true);
        }
      }
    }

    public void UpdateIconManual(int clip)
    {
      _ammoVisible = clip;
      for (int i = 0; i < _ammoCount; i++)
        _ammo[i].gameObject.SetActive(i < clip);
    }
  }

  ItemIcon GetItemIcon(ActiveRagdoll.Side side)
  {
    var iconType = side == ActiveRagdoll.Side.LEFT ? IconType.ITEM_LEFT : IconType.ITEM_RIGHT;
    return _itemIcons.TryGetValue(iconType, out var icon) ? icon : null;
  }
  ItemIcon GetUtilityIcon(ActiveRagdoll.Side side)
  {
    var iconType = side == ActiveRagdoll.Side.LEFT ? IconType.UTILITY_LEFT : IconType.UTILITY_RIGHT;
    return _itemIcons.TryGetValue(iconType, out var icon) ? icon : null;
  }

  public void ItemUse(ActiveRagdoll.Side side)
  {
    GetItemIcon(side)?.Clip_Deincrement();
  }
  public void ItemReload(ActiveRagdoll.Side side, int amount)
  {
    GetItemIcon(side)?.Clip_Reload(amount);
  }
  public void ItemSetClip(ActiveRagdoll.Side side, int clip)
  {
    GetItemIcon(side)?.UpdateIconManual(clip);
  }

  public void UtilityUse(ActiveRagdoll.Side side)
  {
    var utility = GetUtilityIcon(side);
    utility?.Clip_Deincrement();
  }
  public void UtilityReload(ActiveRagdoll.Side side, int amount)
  {
    var utility = GetUtilityIcon(side);
    utility?.Clip_Reload(amount);
  }

  public bool CanUtilityReload(ActiveRagdoll.Side side)
  {
    var util = GetUtilityIcon(side);
    if (util._ammo.Length == 0) return false;
    return !util._ammo[util._ammo.Length - 1].gameObject.activeSelf;
  }

  public int GetUtilitiesLength(ActiveRagdoll.Side side)
  {
    // Check which utilities to check
    var utils = _Equipment._UtilitiesLeft;
    if (side == ActiveRagdoll.Side.RIGHT)
      utils = _Equipment._UtilitiesRight;

    // Check for no utilities
    if (utils.Length == 0)
      return 0;

    //
    return utils.Length * ShopHelper.GetUtilityCount(utils[0]);
  }

  //
  public void UpdateVersusUI()
  {
    var bg = _ui.GetChild(1).transform;

    _VersusUI.gameObject.SetActive(GameScript.s_IsPartyGameMode);
    if (GameScript.s_IsPartyGameMode)
    {

      var xpos = 0.701f;
      var ypos = -0.113f;
      if (bg.localScale.y >= 4.14f)
      {
        xpos = 3f;
        ypos = 0.35f;
      }
      else if (bg.localScale.y >= 3.29f)
      {
        xpos = 3.17f;
      }
      else if (bg.localScale.y >= 2.44f)
      {
        xpos = 2.38f;
      }
      else if (bg.localScale.y >= 1.59f)
      {
        xpos = 1.55f;
      }

      _VersusUI.localPosition = new Vector3(xpos, ypos, 0f);
      UpdateVersusScore();
    }
  }
  public void UpdateVersusScore()
  {
    var score = VersusMode.GetPlayerScore(_Id);
    var scoreText = _VersusUI.GetChild(1).GetComponent<TMPro.TextMeshPro>();

    scoreText.text = $"{score}";
  }

  // Update loadout index
  public void UpdateLoadoutIndex()
  {
    var bg = _ui.GetChild(1).transform;

    if (GameScript.s_IsMissionsGameMode && SettingsModule.ShowLoadoutIndexes)
    {
      _loadoutIndexText.enabled = true;
      _loadoutIndexText.text = $"{_LoadoutIndex + 1}";

      var xpos = 0.49f;
      var ypos = -0.24f;
      if (bg.localScale.y >= 4.14f)
      {
        xpos = 3.82f;
      }
      else if (bg.localScale.y >= 3.29f)
      {
        xpos = 3f;
      }
      else if (bg.localScale.y >= 2.44f)
      {
        xpos = 2.17f;
      }
      else if (bg.localScale.y >= 1.59f)
      {
        xpos = 1.36f;
      }

      _loadoutIndexText.transform.localPosition = new Vector3(xpos, ypos, 0f);
    }
    else
    {
      _loadoutIndexText.enabled = false;
    }
  }

  //
  public void UpdateIcons()
  {

    // Perks
    UpdatePerkIcons();

    // Clear weapon icons
    if (_itemIcons != null)
      foreach (var t in _itemIcons) { t.Value.Destroy(); }
    _itemIcons = new();
    _itemAnimations = new Animation[4];

    // Load icons per player equipment
    var utilLength_left = GetUtilitiesLength(ActiveRagdoll.Side.LEFT);
    var utilLength_right = GetUtilitiesLength(ActiveRagdoll.Side.RIGHT);
    var bg = _ui.GetChild(1).transform;

    // Check for empty
    if (_ItemLeft == ItemManager.Items.NONE && _ItemRight == ItemManager.Items.NONE && utilLength_left == 0 && utilLength_right == 0)
    {
      bg.localPosition = new Vector3(0f, -0.05f, 0f);
      bg.localScale = new Vector3(0.6f, 0.74f, 0.001f);

      UpdateVersusUI();
      UpdateLoadoutIndex();
      return;
    }

    var equipmentIter = 0;
    if (_ItemLeft != ItemManager.Items.NONE)
    {
      var itemIcon = new ItemIcon();
      itemIcon.Init(ItemManager.LoadIcon(_ItemLeft.ToString(), equipmentIter, _Player, _ui), equipmentIter++, System.Tuple.Create(_Player, false, ActiveRagdoll.Side.LEFT), false);
      _itemIcons.Add(IconType.ITEM_LEFT, itemIcon);
    }
    if (_ItemRight != ItemManager.Items.NONE)
    {
      var itemIcon = new ItemIcon();
      itemIcon.Init(ItemManager.LoadIcon(_ItemRight.ToString(), equipmentIter, _Player, _ui), equipmentIter++, System.Tuple.Create(_Player, false, ActiveRagdoll.Side.RIGHT), false);
      _itemIcons.Add(IconType.ITEM_RIGHT, itemIcon);
    }

    // Load utilities
    var utilsLoaded = new List<System.Tuple<ItemManager.ItemUIInfo, int, ActiveRagdoll.Side>>();
    if (_Equipment._UtilitiesLeft.Length > 0)
    {
      var itemInfo = ItemManager.LoadIcon(_Equipment._UtilitiesLeft[0].ToString(), equipmentIter, _Player, _ui);
      itemInfo._ClipSize = utilLength_left;
      utilsLoaded.Add(System.Tuple.Create(itemInfo, equipmentIter++, ActiveRagdoll.Side.LEFT));
    }
    if (_Equipment._UtilitiesRight.Length > 0)
    {
      var itemInfo = ItemManager.LoadIcon(_Equipment._UtilitiesRight[0].ToString(), equipmentIter, _Player, _ui);
      itemInfo._ClipSize = utilLength_right;
      utilsLoaded.Add(System.Tuple.Create(itemInfo, equipmentIter++, ActiveRagdoll.Side.RIGHT));
    }
    foreach (var util in utilsLoaded)
    {
      var itemIcon = new ItemIcon();
      itemIcon.Init(util.Item1, util.Item2, System.Tuple.Create(_Player, true, util.Item3), true);
      _itemIcons.Add(util.Item3 == ActiveRagdoll.Side.LEFT ? IconType.UTILITY_LEFT : IconType.UTILITY_RIGHT, itemIcon);
    }



    //
    UpdateVersusUI();
    UpdateLoadoutIndex();
  }

  //
  public void UpdatePerkIcons()
  {
    // UI
    if (_perk_UI == null)
      _perk_UI = new MeshRenderer[] { _ui.GetChild(2).GetChild(0).GetComponent<MeshRenderer>(), _ui.GetChild(2).GetChild(1).GetComponent<MeshRenderer>(), _ui.GetChild(2).GetChild(2).GetComponent<MeshRenderer>(), _ui.GetChild(2).GetChild(3).GetComponent<MeshRenderer>() };

    // Current perks
    var perks = Perk.GetPerks(_Id);

    // Check empty
    if (perks.Count == 0)
    {
      foreach (var perkRend in _perk_UI)
        perkRend.gameObject.SetActive(false);
      return;
    }

    // Check normal
    for (var i = 0; i < _perk_UI.Length; i++)
    {
      var has_perk = i < perks.Count;
      _perk_UI[i].gameObject.SetActive(has_perk);
      if (!has_perk) continue;
      _perk_UI[i].material.mainTexture = GameResources._PerkTypes.transform.GetChild((int)perks[i]).GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
    }
  }

  //
  public void OnPlayerSpawn()
  {
    UpdateIcons();

    CreateHealthUI(_Player == null ? 1 : _Player._Ragdoll._health);
  }
}