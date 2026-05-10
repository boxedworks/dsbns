
using UnityEngine;

namespace Assets.Scripts.XR
{
  public class XRCameraSettings
  {
    public Vector2 _Position;
    public float _Height, _Size, _Pitch, _YSpin;

    public XRCameraSettings()
    {
      _Position = new Vector2(0f, 0f);
      _Height = 15f;// - GameResources._Camera_Main.transform.localPosition.y * 0.2f * _Size;
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

      float useSize = ((int)_Size) / 3 * 3;
      if (useSize == 0f)
        useSize = 0.5f;

      var playerspawnpos = PlayerspawnScript._PlayerSpawns[0].transform.position;
      var mapCenter = TileManager._Floor.position;
      cameraParent.rotation = Quaternion.Euler(_Pitch, _YSpin, 0f);
      cameraParent.localScale = Vector3.one * useSize;

      var camPos = camera.transform.position;
      var camDis = camPos - xOrigin.position;
      xOrigin.position = mapCenter - camDis;

      xOrigin.position += new Vector3(_Position.x, _Height + _Size * 0.1f, _Position.y);

      SetCameraClippingPlanes(0.05f, 35f + useSize * 0.5f + _Height);
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