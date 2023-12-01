public static class Stats
{
  // Class for player stats
  public class Stat
  {
    int _id;

    public int _kills, _deaths, _teamkills;
    public int _points
    {
      get
      {
        if (GameScript.s_GameMode != GameScript.GameModes.SURVIVAL) return -1;
        return GameScript.SurvivalMode.GetTotalPoints(_id);
      }
    }

    public Stat(int id)
    {
      _id = id;
      _kills = _deaths = _teamkills = 0;
    }
  }

  // Static class holding overall game stats
  /*public static class OverallStats
  {
    public static FunctionsC.SaveableStat_Int _Kills, _Deaths, _Team_Kills,
      // Classic mode
      _Levels_Completed,
      _Enemies_Killed_Knife, _Enemies_Killed_Pistol, _Enemies_Killed_PistolSilenced, _Enemies_Killed_Grenade, _Enemies_Killed_Revolver,
      // Survival mode
      _Waves_Played,
      _Enemies_Killed_Survival_Basic, _Enemies_Killed_Survival_Armored
    ;

    public static FunctionsC.SaveableStat_Int[] _Stats;

    public static void Init()
    {
      _Kills = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Kills)}", 0);
      _Deaths = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Deaths)}", 0);
      _Team_Kills = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Team_Kills)}", 0);

      _Levels_Completed = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Levels_Completed)}", 0);
      _Enemies_Killed_Knife = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Enemies_Killed_Knife)}", 0);
      _Enemies_Killed_Pistol = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Enemies_Killed_Pistol)}", 0);
      _Enemies_Killed_PistolSilenced = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Enemies_Killed_PistolSilenced)}", 0);
      _Enemies_Killed_Grenade = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Enemies_Killed_Grenade)}", 0);
      _Enemies_Killed_Revolver = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Enemies_Killed_Revolver)}", 0);

      _Waves_Played = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Waves_Played)}", 0);
      _Enemies_Killed_Survival_Basic = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Enemies_Killed_Survival_Basic)}", 0);
      _Enemies_Killed_Survival_Armored = new FunctionsC.SaveableStat_Int($"OverallStats_{nameof(_Enemies_Killed_Survival_Armored)}", 0);

      _Stats = new FunctionsC.SaveableStat_Int[]
      {
        _Kills, _Deaths, _Team_Kills,

        _Levels_Completed,
        _Enemies_Killed_Knife, _Enemies_Killed_Pistol, _Enemies_Killed_PistolSilenced, _Enemies_Killed_Grenade, _Enemies_Killed_Revolver,

        _Waves_Played,
        _Enemies_Killed_Survival_Basic, _Enemies_Killed_Survival_Armored
      };
    }

    public static void Reset()
    {
      foreach (var stat in _Stats)
        stat.Reset();
    }
  }*/

  // Holds player stats
  public static Stat[] _Stats;


  public static void Init()
  {
    Reset_Local();

    //OverallStats.Init();
  }

  public static void Reset_Local()
  {
    _Stats = new Stat[4];
    for (var i = 0; i < _Stats.Length; i++)
      _Stats[i] = new Stat(i);
  }

  public static void Reset_Overall()
  {
    //OverallStats.Reset();
  }

  public static void RecordKill(int playerId)
  {
    _Stats[playerId]._kills++;
    //OverallStats._Kills++;
  }
  public static void RecordDeath(int playerId)
  {
    _Stats[playerId]._deaths++;
    //OverallStats._Deaths++;
  }
  public static void RecordTeamkill(int playerId)
  {
    _Stats[playerId]._teamkills++;
    //OverallStats._Team_Kills++;
  }
}
