
using SneakyGame.Ragdoll;
using UnityEngine;

namespace SneakyGame.Enemy
{
  public class CheckSoundData
  {
    public Vector3 NoisePosition, AlertPosition;
    public ActiveRagdoll RagdollSource;

    public Loudness LoudnessLevel;
    public bool SlowReaction = true;

    public int BulletID = -1;
  }
}