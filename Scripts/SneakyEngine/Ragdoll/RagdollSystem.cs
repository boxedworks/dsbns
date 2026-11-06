using SneakyEngine.Engine;
using UnityEngine;

namespace SneakyEngine.Ragdoll
{

  public class RagdollSystem
  {

    public static RagdollSystem Singleton => SneakyEngineSystem.RagdollSystem;

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

    // Color override
    Color _colorOverride = new(255f / 255f, 203f / 255f, 196f / 255f);
    public Color ColorOverride => _colorOverride;
    public void SetColorOverride(Color color)
    {
      _colorOverride = color;
    }

    // Trigger extension
    // System.Action<Collider, ActiveRagdoll> _onTriggerEnter;
    // public System.Action<Collider, ActiveRagdoll> OnTriggerEnter => _onTriggerEnter;
    // public void RegisterOnTriggerEnter(System.Action<Collider, ActiveRagdoll> action)
    // {
    //   _onTriggerEnter = action;
    // }

  }


}