

namespace SneakyEngine.Effects
{

  public class VfxSystem
  {

    // Blood / blood smoke toggles
    bool _useBlood, _useBloodSmoke;
    public bool UseBlood => _useBlood;
    public bool UseBloodSmoke => _useBloodSmoke;
    public void SetUseBlood(bool toggle)
    {
      _useBlood = toggle;
    }
    public void SetUseBloodSmoke(bool toggle)
    {
      _useBloodSmoke = toggle;
    }

    // Muzzle flash toggle
    bool _useMuzzleFlash;
    public bool UseMuzzleFlash => _useMuzzleFlash;
    public void SetUseMuzzleFlash(bool toggle)
    {
      _useMuzzleFlash = toggle;
    }

    // Blood type
    public enum BloodParticleType
    {
      BLOOD,
      CONFETTI
    }
    BloodParticleType _bloodType;
    public BloodParticleType BloodType => _bloodType;
    public void SetBloodType(BloodParticleType type)
    {
      _bloodType = type;
    }


  }
}