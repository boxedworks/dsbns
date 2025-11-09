

using System.Collections.Generic;
using Steamworks;
using UnityEngine;

public static class Achievements
{
  //
  static Settings.LevelSaveData LevelModule { get { return Settings.s_SaveData.LevelData; } }

  //
  public enum Achievement
  {
    KILL,
    DIE,
    EXPLODE,
    BAT_DEFLECT,
    TEAM_KILL,

    GRAPPLE_KILL,
    GRAPPLE_NECK,

    BULLET_DESTROY,

    DEMO_LEVEL_0,
    DEMO_LEVEL_1,
    DEMO_LEVEL_2,

    LEVEL_0_COMPLETED,
    DIFFICULTY_1,
    DIFFICULTY_2,

    TIME_BEAT_ALL,
    TIME_BEAT_SNEAKY,

    SURVIVAL_BUY_ITEM,
    SURVIVAL_BUY_MOD,

    SURVIVAL_MAP0_10,
    SURVIVAL_MAP0_20,
    SURVIVAL_MAP1_10,
    SURVIVAL_MAP1_20,

    EXTRA_UNLOCK1,
    EXTRA_UNLOCK_ALL,
    EXTRA_USE_CONFETTI,
    EXTRA_SUPERH,

    FRYING_PAN_RAIN,
    TABLE_FLIP,

    MOD_SPLIT,
    BRIEFING,
  }

  static Dictionary<Achievement, float> _Achievement_Last;
  public static void LoadAchievements()
  {

    _Achievement_Last = new Dictionary<Achievement, float>();
    var checkSteam = GameScript.s_UsingSteam;

    foreach (var achievement in System.Enum.GetValues(typeof(Achievement)))
    {
      var isUnlocked = false;
      if (checkSteam && SteamUserStats.GetAchievement(((Achievement)achievement).ToString(), out isUnlocked))
      {
        //Debug.Log($"Achievement {achievement} unlocked: {isUnlocked}");
        if (isUnlocked)
        {
          _Achievement_Last.Add((Achievement)achievement, -2f);
          continue;
        }
      }

      _Achievement_Last.Add((Achievement)achievement, -1f);
    }

    // Check missed achievments
#if UNITY_STANDALONE

    // Extras
    {
      // Unlock one achievement
      if (Shop.AnyExtrasUnlocked())
        UnlockAchievement(Achievement.EXTRA_UNLOCK1);

      // Unlocked all achievements
      if (Shop.AllExtrasUnlocked())
        UnlockAchievement(Achievement.EXTRA_UNLOCK_ALL);
    }

    // Survival
    {
      var highestSurvival0 = LevelModule.GetHighestSurvivalWave(0);
      if (highestSurvival0 >= 10)
        UnlockAchievement(Achievement.SURVIVAL_MAP0_10);
      if (highestSurvival0 >= 20)
        UnlockAchievement(Achievement.SURVIVAL_MAP0_20);

      var highestSurvival1 = LevelModule.GetHighestSurvivalWave(1);
      if (highestSurvival1 >= 10)
        UnlockAchievement(Achievement.SURVIVAL_MAP1_10);
      if (highestSurvival1 >= 20)
        UnlockAchievement(Achievement.SURVIVAL_MAP1_20);
    }
#endif
  }

  public static void UnlockAchievement(Achievement achievement)
  {
    // Debug log
#if UNITY_EDITOR
    Debug.Log($"Unlocking Steam achievement: {achievement}");
    return;
#else
      if (Debug.isDebugBuild){
        Debug.Log($"Unlocking Steam achievement: {achievement}");
        return;
      }
#endif

    if (
      //GameScript._Singleton._IsDemo ||
      //Debug.isDebugBuild ||
      (!SteamManager.Initialized && GameScript.s_UsingSteam)
    )
      return;

    // Check achievements buffer
    var lastAchievmentCheck = _Achievement_Last[achievement];
    if (lastAchievmentCheck == -2f || (lastAchievmentCheck != -1f && Time.time - lastAchievmentCheck < 60f))
      return;
    _Achievement_Last[achievement] = Time.time;

    // Debug log
#if UNITY_EDITOR
    Debug.Log($"Awarding Steam achievement: {achievement}");
#else
      if (Debug.isDebugBuild)
        Debug.Log($"Awarding Steam achievement: {achievement}");
#endif

    if (GameScript.s_UsingSteam)
      UnlockAchievement_Steam(achievement);
    else
      UnlockAchievement_EOS(achievement);
  }

  // Unlock on Steam
  static void UnlockAchievement_Steam(Achievement achievement)
  {
    try
    {
      if (SteamUserStats.SetAchievement(achievement.ToString()))
      {
        _Achievement_Last[achievement] = -2f;
      }
      SteamUserStats.StoreStats();
    }
    catch (System.Exception e)
    {
      Debug.LogError(e.ToString());
    }
  }
  // Unlock on Epic
  static void UnlockAchievement_EOS(Achievement achievement)
  {
    //EOSSDKComponent.UnlockAchievement_EOS(achievement);
  }
}