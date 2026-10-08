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

    //
    public enum TargetAlignmentType
    {
      NONE,

      ALL,
      ENEMIES,
      PLAYERS
    }

    // Explode on death effect
    TargetAlignmentType _explodeOnDeath;
    public TargetAlignmentType ExplodeOnDeath => _explodeOnDeath;
    public void SetExplodeOnDeath(TargetAlignmentType type)
    {
      _explodeOnDeath = type;
    }


  }


}