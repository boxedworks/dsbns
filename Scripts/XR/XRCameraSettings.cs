
using UnityEngine;

namespace Assets.Scripts.XR
{
  public class XRCameraSettings
  {
    public Vector2 _Position;
    public float _Height, _Size, _Pitch;

    public XRCameraSettings()
    {
      _Position = new Vector2(0f, -5f);
      _Height = -10f;
      _Size = 30f;
      _Pitch = 0f;

      SetCameraClippingPlanes(0.05f, 35f);
      UpdateCamera();
    }

    public void UpdateCamera()
    {
      var camera = GameResources._Camera_Main;
      var xOrigin = camera.transform.parent;
      var cameraParent = xOrigin.parent;

      var playerspawnpos = PlayerspawnScript._PlayerSpawns[0].transform.position;

      cameraParent.localScale = Vector3.one * _Size;
      cameraParent.position = playerspawnpos + new Vector3(_Position.x, _Height - _Size, _Position.y);

      xOrigin.rotation = Quaternion.Euler(_Pitch, 0f, 0f);

      SetCameraClippingPlanes(0.05f, 35f + _Size * 0.5f);
    }

    //
    static void SetCameraClippingPlanes(float near, float far)
    {
      var camera = GameResources._Camera_Main;
      var camera2 = GameResources._Camera_Menu;
      var cameraPP = GameResources._Camera_IgnorePP;

      camera.nearClipPlane = camera2.nearClipPlane = cameraPP.nearClipPlane = near;
      camera.farClipPlane = camera2.farClipPlane = cameraPP.farClipPlane = far;
    }
  }
}