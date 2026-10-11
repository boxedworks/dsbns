
using SneakyGame.Settings;
using SneakyGame.Settings.Serialization;
using UnityEngine;

namespace SneakyGame.Game.Tutorial
{

  /// <summary>
  /// Holds are information about tutorial such as how to restart or change weapons.
  /// </summary>
  public static class TutorialInformation
  {

    //
    static SettingsSaveData SettingsModule { get { return SettingsHelper.s_SaveData.Settings; } }
    static LevelSaveData LevelModule { get { return SettingsHelper.s_SaveData.LevelData; } }

    public static bool _HasRestarted // Will be true when the player has restarted the game
    {
      set
      {
        LevelModule.HasRestarted = value;
      }
      get
      {
        return LevelModule.HasRestarted;
      }
    }

    public static Transform _TutorialArrow,
      _Tutorial_Restart_Controller0, _Tutorial_Restart_Controller1,
      _Tutorial_Restart_Keyboard0, _Tutorial_Restart_Keyboard1;

    public static void Init()
    {
      _TutorialArrow = GameObject.Find("TutorialArrow").transform;
      _Tutorial_Restart_Controller0 = GameObject.Find("RestartTutorial0").transform;
      _Tutorial_Restart_Controller1 = GameObject.Find("RestartTutorial1").transform;
      _Tutorial_Restart_Keyboard0 = GameObject.Find("RestartTutorial2").transform;
      _Tutorial_Restart_Keyboard1 = GameObject.Find("RestartTutorial3").transform;
    }
  }

}