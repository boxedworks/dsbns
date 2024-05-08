using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameResources
{
  public static GameObject _Player, _PlayerNetwork, _Enemy, _Ragdoll, _Bullet, _Door, _Button, _Laser, _Playerspawn,
    _Barrel_Explosive,
    _Explosive_Scar,
    _Table, _Table_Bush,
    _TableSmall, _TableSmall_Bush,
    _Chair, _Chair_Stump,
    _BookcaseClosed,

    _BookcaseOpen, _BookcaseOpen_Bush,
    _BookcaseBig, _BookcaseBig_Bush,

    _Television,
    _Books,

    _RugRectangle,


    _Barrel, _Barrel_Rock,
    _ColumnNormal, _ColumnBroken,
    _Rock0, _Rock1,
    _CandelBig, _CandelTable, _CandelBarrel,
    _Powerup,
    _Lock,
    _LaserBeam,
    _NavmeshBarrier,
    _Interactable,
    _Fake_Tile, _Tile_Wall,
    _Arch,
    _Money,

    _Armor,
    _Crown,

    _PerkTypes,

    s_Game, s_Particles, s_AmmoUi;

  public static TMPro.TextMeshPro s_AmmoSideUi;

  public static Renderer s_Blood0;

  public static bool _Loaded;

  public static Transform _Container_Objects, _UI_Player, s_Backrooms;
  public static Camera _Camera_Main, _Camera_Menu;
  public static MeshRenderer _CameraFader;

  public static int _Layermask_Ragdoll;

  public static void Init()
  {
    _Layermask_Ragdoll = ~0;
    _Layermask_Ragdoll &= ~(1 << LayerMask.NameToLayer("UI"));
    _Layermask_Ragdoll &= ~(1 << LayerMask.NameToLayer("Bullet"));
    _Layermask_Ragdoll &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
    _Layermask_Ragdoll &= ~(1 << LayerMask.NameToLayer("CAMERA2"));

    _Player = Resources.Load("Player") as GameObject;
    _PlayerNetwork = Resources.Load("NetworkPlayer") as GameObject;
    _Enemy = Resources.Load("Enemy") as GameObject;
    _Ragdoll = Resources.Load("Ragdoll") as GameObject;

    _Bullet = Resources.Load("Bullet") as GameObject;
    _Door = Resources.Load("Door") as GameObject;
    _Button = Resources.Load("Button") as GameObject;
    _Laser = Resources.Load("Laser") as GameObject;
    _Playerspawn = Resources.Load("PlayerSpawn") as GameObject;
    _Barrel_Explosive = Resources.Load("ExplosiveBarrel") as GameObject;
    _Explosive_Scar = Resources.Load("ExplosionMark") as GameObject;

    _TableSmall = Resources.Load("TableSmall") as GameObject;
    _TableSmall_Bush = Resources.Load("TableSmall_Bush") as GameObject;

    _BookcaseClosed = Resources.Load("BookcaseClosed") as GameObject;

    _BookcaseOpen = Resources.Load("BookcaseOpen") as GameObject;
    _BookcaseOpen_Bush = Resources.Load("BookcaseOpen_Bush") as GameObject;
    _BookcaseBig = Resources.Load("BookcaseBig") as GameObject;
    _BookcaseBig_Bush = Resources.Load("BookcaseBig_Bush") as GameObject;

    _Television = Resources.Load("Television") as GameObject;
    _Books = Resources.Load("Books") as GameObject;

    _RugRectangle = Resources.Load("RugRectangle") as GameObject;

    _Barrel = Resources.Load("Barrel") as GameObject;
    _Barrel_Rock = Resources.Load("Barrel_Rock") as GameObject;

    _Chair = Resources.Load("Chair") as GameObject;
    _Chair_Stump = Resources.Load("Chair_Stump") as GameObject;

    _Table = Resources.Load("Table") as GameObject;
    _Table_Bush = Resources.Load("Table_Bush") as GameObject;

    _ColumnNormal = Resources.Load("Column") as GameObject;
    _ColumnBroken = Resources.Load("ColumnBroken") as GameObject;

    _Rock0 = Resources.Load("Rock0") as GameObject;
    _Rock1 = Resources.Load("Rock1") as GameObject;

    _CandelBig = Resources.Load("Candel_Tall") as GameObject;
    _CandelTable = Resources.Load("Candel_Table") as GameObject;
    _CandelBarrel = Resources.Load("Candel_Barrel") as GameObject;

    _Powerup = Resources.Load("Powerup") as GameObject;

    _Lock = Resources.Load("Lock") as GameObject;

    _LaserBeam = Resources.Load("LaserBeam") as GameObject;

    _NavmeshBarrier = Resources.Load("NavMeshBarrier") as GameObject;
    _Interactable = Resources.Load("Interactable") as GameObject;

    _Fake_Tile = Resources.Load("FakeTile") as GameObject;
    _Tile_Wall = Resources.Load("TileWall") as GameObject;

    _Arch = Resources.Load("Arch") as GameObject;

    _Money = Resources.Load("Money") as GameObject;

    _PerkTypes = Resources.Load("PerkTypes") as GameObject;

    _Camera_Main = GameObject.Find("Main Camera").GetComponent<Camera>();
    _Camera_Menu = GameObject.Find("Menu Camera").GetComponent<Camera>();

    _CameraFader = GameObject.Find("Fader").GetComponent<MeshRenderer>();

    _Container_Objects = GameObject.Find("Objects").transform;
    _UI_Player = _Camera_Main.transform.GetChild(0).GetChild(2);

    _Armor = Resources.Load("Armor") as GameObject;
    _Crown = Resources.Load("Crown") as GameObject;

    s_Game = GameObject.Find("Game");
    s_Particles = GameObject.Find("Particles");
    s_AmmoUi = GameObject.Find("AmmoUI");
    s_AmmoSideUi = s_AmmoUi.transform.GetChild(0).GetComponent<TMPro.TextMeshPro>();

    s_Blood0 = GameObject.Find("Blood_0").GetComponent<Renderer>();

    s_Backrooms = GameObject.Find("Backrooms").transform;
    s_Backrooms.gameObject.SetActive(false);

    //
    //if (GameScript._s_Singleton.ReplacementShader != null)
    //  _Camera_Main.SetReplacementShader(GameScript._s_Singleton.ReplacementShader, "");

    //
    _Loaded = true;
  }
}