
using System;
using UnityEngine;
using Valve.VR;

namespace Assets.Scripts.XR
{

  public class XRUIManager
  {

    static XRUIManager s_Singleton { get { return GameScript.s_Singleton._XrUiManager; } }
    public static bool s_HasControl { get { return s_Singleton._hoveredControl != null; } }

    XRCameraSettings _cameraSettings;
    public static Vector2 _CameraPosition { get { return s_Singleton._cameraSettings._Position; } }
    public static float _CameraHeight { get { return s_Singleton._cameraSettings._Height; } }
    public static float _CameraSize { get { return s_Singleton._cameraSettings._Size; } }
    public static void UpdateCamera()
    {
      s_Singleton._cameraSettings.UpdateCamera();
    }

    // UI Components
    Transform _uiControls,
      _transformControlsBg,
      _buttonRestartMap, _buttonLoadoutLeft, _buttonLoadoutRight, _buttonResetUi, _buttonToggleTransformControls,
      _stylusPosition, _stylusHeight, _stylusSize, _stylusRotation;

    Transform _pointerR;
    Light _pointerR_light;
    Rigidbody _pointerR_physics;

    Transform _controllerLeft { get { return GameResources._XrLeft; } }
    Transform _controllerRight { get { return GameResources._XrRight; } }

    Transform[] _controls;

    public XRUIManager()
    {
      _cameraSettings = new();

      _uiControls = GameObject.Find("XrControls").transform;
      _uiControls.parent = _controllerLeft;
      _uiControls.localPosition = new Vector3(-0.0044f, 0f, -0.1967f);
      _uiControls.localRotation = Quaternion.Euler(90f, 0f, 0f);
      _uiControls.localScale = Vector3.one * 0.11f;

      _pointerR = _uiControls.GetChild(3);
      _pointerR_physics = _uiControls.GetChild(4).GetComponent<Rigidbody>();
      _pointerR.parent = _pointerR_physics.transform.parent = _controllerRight;
      _pointerR.localPosition = Vector3.zero;
      _pointerR.localScale = _pointerR_physics.transform.localScale = Vector3.one * 0.02f;
      Physics.IgnoreCollision(_pointerR.GetComponent<Collider>(), _pointerR_physics.GetComponent<Collider>());

      _pointerR_light = _pointerR.gameObject.AddComponent<Light>();
      _pointerR_light.color = Color.red * Color.white;
      _pointerR_light.shadows = LightShadows.Hard;
      _pointerR_light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;

      _pointerR_physics.gameObject.AddComponent<PointerCollider>();

      var restartMapControls = _uiControls.GetChild(0);
      _buttonRestartMap = restartMapControls.GetChild(1);

      var loadoutControls = _uiControls.GetChild(1);
      _buttonLoadoutLeft = loadoutControls.GetChild(1);
      _buttonLoadoutRight = loadoutControls.GetChild(2);

      var cameraTransformControls = _uiControls.GetChild(2);
      _transformControlsBg = cameraTransformControls.GetChild(0);
      _stylusPosition = cameraTransformControls.GetChild(1).GetChild(0);
      _stylusSize = cameraTransformControls.GetChild(2).GetChild(0);
      _stylusHeight = cameraTransformControls.GetChild(3).GetChild(0);
      _stylusRotation = cameraTransformControls.GetChild(4).GetChild(0);
      _buttonResetUi = cameraTransformControls.GetChild(5).GetChild(0);
      _buttonToggleTransformControls = cameraTransformControls.GetChild(6);

      _controls = new Transform[] {
        _buttonRestartMap,
        _buttonLoadoutLeft,
        _buttonLoadoutRight,
        _stylusPosition,
        _stylusSize,
        _stylusHeight,
        _stylusRotation,
        _buttonResetUi,
        _buttonToggleTransformControls
      };
      foreach (var control in _controls)
        control.GetComponent<Renderer>().sharedMaterial.color = Color.gray;
    }

    Transform _hoveredControl;
    Vector3 _hoveredControlSavePosition;
    bool _dragControlX, _dragControlY;
    public void Update()
    {

      // Check player select
      var itemRightAction = SteamVR_Actions.Player.RightItem;
      if (itemRightAction.stateDown)
      {
        if (s_HasControl)
          ButtonDown();
      }
      else if (itemRightAction.stateUp)
      {
        if (s_HasControl)
          ButtonUp();
      }

      // Check if selection controller is hovered over control
      var controlFound = false;
      if (!_triggerToggle)
      {
        foreach (var control in _controls)
        {
          if (!control.gameObject.activeInHierarchy)
            continue;
          if (AreSpheresIntersecting(control.position, control.lossyScale.x * 0.5f, _controllerRight.position, _pointerR.transform.lossyScale.x * 0.5f))
          {
            controlFound = true;

            if (_hoveredControl != control)
            {
              if (_hoveredControl != null)
                UnselectHoveredControl();

              _hoveredControl = control;
              _hoveredControlSavePosition = _hoveredControl.localPosition;
              _hoveredControl.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            }

            break;
          }
        }
      }
      else
      {
        controlFound = _hoveredControl != null;
      }

      //_cameraSettings._Pitch = _cameraSettings._YSpin = 0f;
      if (controlFound)
      {
        if ((_dragControlX || _dragControlY) && _triggerToggle)
        {
          // Get controller position in local space of ui controls parent
          var newPos = _hoveredControl.parent.InverseTransformPoint(_controllerRight.position);
          var posClamp = 0.145f;
          var isXAxisGreatest = Mathf.Abs(newPos.x) > Mathf.Abs(newPos.y);
          newPos.x = _dragControlX && isXAxisGreatest ? Mathf.Clamp(newPos.x, -posClamp, posClamp) : _hoveredControlSavePosition.x;
          newPos.y = _dragControlY && !isXAxisGreatest ? Mathf.Clamp(newPos.y, -posClamp, posClamp) : _hoveredControlSavePosition.y;
          newPos.z = 0f;
          _hoveredControl.localPosition = newPos;

          // Normalized value
          var normalizedValue = isXAxisGreatest ? (newPos.x / posClamp) : (newPos.y / posClamp);

          // Update value based on which control is being dragged
          if (Mathf.Abs(normalizedValue) > 0.15f)
          {
            normalizedValue -= 0.15f * Mathf.Sign(normalizedValue);

            // Position
            if (_hoveredControl.parent.name == _stylusPosition.parent.name)
            {
              var posClampVal = 15f;

              // Translate input value based on camera rotation
              var dir = Vector3.zero;
              if (isXAxisGreatest)
                dir.x = normalizedValue;
              else
                dir.z = normalizedValue;
              dir = Quaternion.Euler(0f, _cameraSettings._YSpin, 0f) * dir * (_cameraSettings._Size < 3f ? 0.5f : 1f);

              var val = _cameraSettings._Position.x;
              val = Mathf.Clamp(val + dir.x * 0.2f, -posClampVal, posClampVal);
              _cameraSettings._Position.x = val;

              val = _cameraSettings._Position.y;
              val = Mathf.Clamp(val + dir.z * 0.2f, -posClampVal, posClampVal);
              _cameraSettings._Position.y = val;
            }

            // Size
            else if (_hoveredControl.parent.name == _stylusSize.parent.name)
            {
              var val = _cameraSettings._Size;
              val = Mathf.Clamp(val - normalizedValue * 0.5f, 0.5f, 120f);
              _cameraSettings._Size = val;
              _pointerR_light.range = 10f * (_cameraSettings._Size * 0.3f);
            }

            // Height
            else if (_hoveredControl.parent.name == _stylusHeight.parent.name)
            {
              var val = _cameraSettings._Height;
              val = Mathf.Clamp(val + normalizedValue * 0.4f, 0f, 50f);
              _cameraSettings._Height = val;
            }

            // Rotation
            else if (_hoveredControl.parent.name == _stylusRotation.parent.name)
            {
              if (isXAxisGreatest)
              {
                var val = _cameraSettings._YSpin;
                val += normalizedValue * 1.3f;
                if (val > 360f || val < -360f)
                  val = 0f;
                _cameraSettings._YSpin = val;
                // _cameraSettings._YSpin = normalizedValue;
              }
              else
              {
                var val = _cameraSettings._Pitch;
                val += normalizedValue * 0.6f;
                if (val > 360f || val < -360f)
                  val = 0f;
                _cameraSettings._Pitch = val;
                // _cameraSettings._Pitch = normalizedValue;
              }
            }

            _cameraSettings.UpdateCamera();
          }

        }
      }
      else if (!controlFound && _hoveredControl != null)
      {
        if (!_triggerToggle)
          UnselectHoveredControl();
      }

    }

    public void FixedUpdate()
    {
      _pointerR_physics.MovePosition(_controllerRight.position);
    }

    void UnselectHoveredControl()
    {
      if (_hoveredControl != null)
      {
        if (_hoveredControl.parent.name == _stylusPosition.parent.name ||
            _hoveredControl.parent.name == _stylusSize.parent.name ||
            _hoveredControl.parent.name == _stylusHeight.parent.name ||
            _hoveredControl.parent.name == _stylusRotation.parent.name)
          _hoveredControl.localPosition = _hoveredControlSavePosition;

        _hoveredControl.GetComponent<Renderer>().sharedMaterial.color = Color.gray;
        _hoveredControl = null;
      }
    }

    public static void ButtonDown()
    {
      s_Singleton.OnDown();
    }
    public static void ButtonUp()
    {
      s_Singleton.OnUp();
    }

    bool _triggerToggle;
    void OnDown()
    {
      _triggerToggle = true;
      _pointerR.transform.localScale = Vector3.one * 0.015f;

      if (_hoveredControl == null) return;

      // Check buttons
      _dragControlX = false;
      _dragControlY = false;
      if (
        _hoveredControl.parent.name == _buttonRestartMap.parent.name ||
        _hoveredControl.parent.name == _buttonLoadoutLeft.parent.name ||
        _hoveredControl.parent.name == _buttonLoadoutRight.parent.name ||
        _hoveredControl.parent.name == _buttonResetUi.parent.name ||
        _hoveredControl.parent.name == _buttonToggleTransformControls.name
        )
      {
      }
      else if (
        _hoveredControl.parent.name == _stylusPosition.parent.name ||
        _hoveredControl.parent.name == _stylusRotation.parent.name
        )
      {
        _dragControlX = true;
        _dragControlY = true;
      }
      else if (
        _hoveredControl.parent.name == _stylusSize.parent.name ||
        _hoveredControl.parent.name == _stylusHeight.parent.name
        )
      {
        _dragControlY = true;
      }
    }
    void OnUp()
    {
      _triggerToggle = false;
      _pointerR.transform.localScale = Vector3.one * 0.02f;

      if (_hoveredControl == null) return;

      // Check buttons
      if (_hoveredControl.parent.name == _buttonRestartMap.parent.name)
      {
        ControllerManager.ReloadMap();
      }
      else if (_hoveredControl.name == _buttonLoadoutLeft.name)
      {
        if (GameScript.s_GameMode == GameScript.GameModes.ZOMBIE)
        {
          PlayerScript.HandleDpadDirectionStatic(0, 2);
        }
        else
        {
          var profile = PlayerProfile.s_Profiles[0];
          profile._LoadoutIndex--;
        }
      }
      else if (_hoveredControl.name == _buttonLoadoutRight.name)
      {
        if (GameScript.s_GameMode == GameScript.GameModes.ZOMBIE)
        {
          PlayerScript.HandleDpadDirectionStatic(0, 3);
        }
        else
        {
          var profile = PlayerProfile.s_Profiles[0];
          profile._LoadoutIndex++;
        }
      }
      else if (_hoveredControl.parent.name == _buttonResetUi.parent.name)
      {
        _cameraSettings = new();
      }
      else if (_hoveredControl.name == _buttonToggleTransformControls.name)
      {
        ToggleTransformControls();
      }
    }

    void ToggleTransformControls()
    {
      ToggleTransformControls(!_transformControlsBg.gameObject.activeSelf);
    }
    void ToggleTransformControls(bool toggle)
    {
      _transformControlsBg.gameObject.SetActive(toggle);
      _stylusPosition.parent.gameObject.SetActive(toggle);
      _stylusSize.parent.gameObject.SetActive(toggle);
      _stylusHeight.parent.gameObject.SetActive(toggle);
      _stylusRotation.parent.gameObject.SetActive(toggle);
      _buttonResetUi.parent.gameObject.SetActive(toggle);

      if (toggle)
      {
        _buttonToggleTransformControls.localPosition = new Vector3(1.2f, 0f, 0f);
      }
      else
      {
        _buttonToggleTransformControls.localPosition = new Vector3(-1.05f, 0f, 0f);
      }
    }

    //
    static bool AreSpheresIntersecting(Vector3 centerA, float radiusA, Vector3 centerB, float radiusB)
    {
      // Use sqrMagnitude to avoid the expensive square root operation
      float distanceSquared = (centerA - centerB).sqrMagnitude;
      float radiusSum = radiusA + radiusB;
      return distanceSquared <= (radiusSum * radiusSum);
    }

  }

}