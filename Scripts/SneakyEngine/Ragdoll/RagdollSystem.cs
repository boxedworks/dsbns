using UnityEngine;

namespace SneakyEngine.Ragdoll
{

  public class RagdollSystem
  {

    // Footstep audio sources
    AudioSource _sfx_footstep, _sfx_footstepBloody;
    public AudioSource SfxFootstep => _sfx_footstep;
    public AudioSource SfxFootstepBloody => _sfx_footstepBloody;
    public void SetFootstepAudioSources(AudioSource footstep, AudioSource footstepBloody)
    {
      _sfx_footstep = footstep;
      _sfx_footstepBloody = footstepBloody;
    }

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

    //
    public enum TargetAlignmentType
    {
      NONE,

      ALL,
      ENEMIES,
      PLAYERS
    }

    // Death effect
    TargetAlignmentType _explodeOnDeath;
    public TargetAlignmentType ExplodeOnDeath => _explodeOnDeath;
    public void SetExplodeOnDeath(TargetAlignmentType type)
    {
      _explodeOnDeath = type;
    }


  }


}