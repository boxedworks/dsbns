using System.Collections.Generic;
using Assets.Scripts.Settings;
using Assets.Scripts.Settings.Serialization;
using Assets.Scripts.Game.Items;
using UnityEngine;
using Assets.Scripts.Ragdoll.Equippables;

public static class ItemManager
{
  //
  static SettingsSaveData SettingsModule { get { return SettingsHelper.s_SaveData.Settings; } }
  static LevelSaveData LevelModule { get { return SettingsHelper.s_SaveData.LevelData; } }

  //
  class Item
  {
    public Items _type;
    public GameObject _gameObject;

    public Item(Items itemType, ref GameObject gameObject)
    {
      _type = itemType;
      _gameObject = gameObject;
    }
  }

  static List<Item> _Items;
  public static Transform _Container;

  public enum Items
  {
    NONE,
    REVOLVER,
    DMR,
    RIFLE,
    RIFLE_LEVER,
    SNIPER,
    UZI,
    KNIFE,
    BAT,
    SHOTGUN_PUMP,
    SHOTGUN_DOUBLE,
    SHOTGUN_BURST,
    PISTOL,
    PISTOL_SILENCED,
    PISTOL_MACHINE,
    PISTOL_DOUBLE,
    GRENADE_HOLD,
    AK47,
    M16,
    ROCKET_LAUNCHER,
    KATANA,
    FRYING_PAN,
    AXE,
    CROSSBOW,
    GRENADE_LAUNCHER,
    FLAMETHROWER,
    ROCKET_FIST,
    STICKY_GUN,
    PISTOL_CHARGE,
    RAPIER,
    RIFLE_CHARGE,
    FIST,
    STUN_BATON,
  }

  // Spawn a single item
  public static ItemScript SpawnItem(Items item)
  {
    if (item == Items.NONE) return null;
    if (_Items == null) _Items = new List<Item>();

    var name = item.ToString();
    if (_Container == null) _Container = GameObject.Find("Items").transform;

    GameObject new_item = Object.Instantiate(Resources.Load($"Items/{name}") as GameObject, _Container);
    new_item.name = name;
    var script = new_item.GetComponent<ItemScript>();
    script._type = item;
    new_item.transform.position = new Vector3(0f, -10f, 0f);
    _Items.Add(new Item(item, ref new_item));
    return script;
  }

  public class ItemUIInfo
  {
    public Transform _Transform;
    public int _ClipSize;
  }
  public static ItemUIInfo GetItemUI(string item, PlayerScript player, Transform parent)
  {
    GameObject new_item = Object.Instantiate(Resources.Load($"Items/{item}") as GameObject, parent);
    new_item.name = item;
    new_item.layer = 11;

    var script = new_item.GetComponent<ItemScript>();
    script._ragdoll = player != null ? player._Ragdoll : null;
    var clipSize = script.GetClipSize();
    Object.DestroyImmediate(script);

    var colliders = new_item.GetComponents<Collider>();
    for (var i = 0; i < colliders.Length; i++)
      Object.DestroyImmediate(colliders[i]);

    var rigidbody = new_item.GetComponent<Rigidbody>();
    if (rigidbody) Object.DestroyImmediate(rigidbody);

    return new ItemUIInfo()
    {
      _Transform = new_item.transform,
      _ClipSize = clipSize
    };
  }

  // Local parsing function
  public static ItemUIInfo LoadIcon(string name, int iter, PlayerScript forPlayer, Transform uiParent)
  {
    // Get icon
    var itemInfo = GetItemUI(name, forPlayer, uiParent);
    var transform = itemInfo._Transform;

    // Move BG
    var bg = uiParent.GetChild(1).transform;
    bg.localPosition = new Vector3(0.05f + (iter + 1) * 0.4f, -0.05f, 0f);
    bg.localScale = new Vector3(0.6f, 0.74f + (iter + 1) * 0.85f, 0.001f);

    // Set transform
    transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
    transform.localPosition = new Vector3(0.7f + iter * 0.8f, 0f, 0f);
    transform.localEulerAngles = new Vector3(0f, 90f, 0f);
    switch (transform.name)
    {
      case "KNIFE":
      case "ROCKET_FIST":
        transform.localPosition += new Vector3(-0.11f, -0.02f, 0f);
        transform.localScale = new Vector3(0.13f, 0.13f, 0.13f);
        transform.localEulerAngles += new Vector3(6.8f, 0f, 0f);
        break;
      case "AXE":
        transform.localPosition += new Vector3(-0.11f, 0.03f, 0f);
        transform.localScale = new Vector3(0.1f, 0.12f, 0.1f);
        transform.localEulerAngles += new Vector3(90f, 0f, 0f);
        break;
      case "STUN_BATON":
        transform.localPosition += new Vector3(-0.03f, 0.03f, 0f);
        transform.localScale = new Vector3(0.17f, 0.17f, 0.17f);
        transform.localEulerAngles = new Vector3(75f, 90f, 0f);
        break;
      case "FRYING_PAN":
        transform.localPosition += new Vector3(-0.2f, 0.03f, 0f);
        transform.localScale = new Vector3(0.13f, 0.17f, 0.13f);
        transform.localEulerAngles = new Vector3(0f, 0f, 270f);
        break;
      case "BAT":
        transform.localPosition += new Vector3(-0.15f, 0f, 0f);
        transform.localScale = new Vector3(0.11f, 0.12f, 0.11f);
        transform.localEulerAngles += new Vector3(81f, 0f, 0f);
        break;
      case "KATANA":
      case "RAPIER":
        transform.localPosition += new Vector3(-0.2f, -0.05f, 0f);
        transform.localScale = new Vector3(0.11f, 0.1f, 0.11f);
        transform.localEulerAngles = new Vector3(8f, 0f, -75f);
        break;
      case "FIST":
        transform.localPosition += new Vector3(-0.2f, 0.03f, 0f);
        transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        break;
      case "PISTOL_SILENCED":
      case "PISTOL":
      case "PISTOL_DOUBLE":
      case "PISTOL_CHARGE":
        transform.localPosition += new Vector3(-0.19f, 0.06f, 0f);
        transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        break;
      case "RIFLE_CHARGE":
        transform.localPosition += new Vector3(0.03f, 0.06f, 0f);
        transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        break;
      case "PISTOL_MACHINE":
        transform.localPosition += new Vector3(-0.19f, 0.08f, 0f);
        transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        break;
      case "REVOLVER":
        transform.localPosition += new Vector3(-0.22f, -0.01f, 0f);
        transform.localScale = new Vector3(0.13f, 0.13f, 0.13f);
        break;
      case "SHOTGUN_DOUBLE":
        transform.localPosition += new Vector3(0.08f, 0.04f, 0f);
        transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
        break;
      case "SHOTGUN_PUMP":
        transform.localPosition += new Vector3(0f, 0.02f, 0f);
        transform.localScale = new Vector3(0.1f, 0.09f, 0.1f);
        break;
      case "SHOTGUN_BURST":
        transform.localPosition += new Vector3(0.01f, 0f, 0f);
        transform.localScale = new Vector3(0.11f, 0.1f, 0.13f);
        break;
      case "UZI":
        transform.localPosition += new Vector3(-0.16f, 0.07f, 0f);
        transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        break;
      case "AK47":
      case "FLAMETHROWER":
        transform.localPosition += new Vector3(-0.11f, 0.03f, 0f);
        transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
        break;
      case "M16":
        transform.localPosition += new Vector3(-0.14f, 0.03f, 0f);
        transform.localScale = new Vector3(0.09f, 0.11f, 0.08f);
        break;
      case "DMR":
      case "RIFLE":
      case "RIFLE_LEVER":
        transform.localPosition += new Vector3(-0.14f, 0.03f, 0f);
        transform.localScale = new Vector3(0.09f, 0.11f, 0.08f);
        break;
      case "CROSSBOW":
        transform.localPosition += new Vector3(-0.35f, -0.07f, 0f);
        transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        transform.localEulerAngles = new Vector3(-30f, 90f, 90f);
        break;
      case "GRENADE_LAUNCHER":
        transform.localPosition += new Vector3(-0.21f, 0.05f, 0f);
        transform.localScale = new Vector3(0.11f, 0.11f, 0.11f);
        break;
      case "MORTAR_STRIKE":
        transform.localPosition += new Vector3(-0.15f, 0.05f, 0f);
        transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
        break;
      case "SNIPER":
        transform.localPosition += new Vector3(-0.14f, 0.03f, 0f);
        transform.localScale = new Vector3(0.09f, 0.11f, 0.08f);
        break;
      case "GRENADE_IMPACT":
      case "GRENADE":
      case "MOLOTOV":
        transform.localPosition += new Vector3(-0.23f, 0.03f, 0f);
        transform.localEulerAngles = new Vector3(25f, -90f, 0f);
        transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        break;
      case "TACTICAL_BULLET":
        transform.localPosition += new Vector3(-0.13f, 0.03f, 0f);
        transform.localEulerAngles = new Vector3(17f, -90f, 0f);
        transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        break;
      case "MIRROR":
        transform.localPosition += new Vector3(-0.14f, 0f, 0f);
        transform.localEulerAngles = new Vector3(0f, 180f, -16f);
        transform.localScale = new Vector3(0.8f, 0.8f, 0.2f);
        break;
      case "C4":
        transform.localPosition += new Vector3(-0.15f, 0.02f, 0f);
        transform.localEulerAngles = new Vector3(0f, 90f, -90f);
        transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        break;
      case "BEAR_TRAP":
        transform.localPosition += new Vector3(-0.21f, 0.04f, 0f);
        transform.localEulerAngles = new Vector3(0f, 90f, -30f);
        transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        break;
      case "MINE":
        transform.localPosition += new Vector3(-0.16f, 0.04f, 0f);
        transform.localEulerAngles = new Vector3(0f, 90f, 30f);
        transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        break;
      case "SHURIKEN":
      case "SHURIKEN_BIG":
        transform.localPosition += new Vector3(-0.22f, 0.02f, 0f);
        transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        break;
      case "KUNAI_EXPLOSIVE":
      case "KUNAI_STICKY":
        transform.localPosition += new Vector3(-0.14f, -0.03f, 0f);
        transform.localEulerAngles = new Vector3(90f, 90f, 90f);
        transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        break;
      case "STOP_WATCH":
      case "INVISIBILITY":
        transform.localPosition += new Vector3(-0.25f, 0f, 0f);
        transform.localEulerAngles = new Vector3(0f, 90f, -90f);
        transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        break;
      case "STICKY_GUN":
        transform.localPosition += new Vector3(-0.06f, 0.03f, 0f);
        transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        break;
      case "GRENADE_STUN":
        transform.localPosition += new Vector3(-0.13f, -0.01f, 0f);
        transform.localEulerAngles = new Vector3(0f, 0f, -90f);
        transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
        break;
      case "TEMP_SHIELD":
        transform.localPosition += new Vector3(-0.2f, 0.03f, 0f);
        transform.localEulerAngles = new Vector3(90f, 0f, 0f);
        transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

        break;
      default:
        Debug.LogWarning($"Unhandled positioning of Utility UI type: {name}");

        transform.localPosition += new Vector3(-0.19f, 0.08f, 0f);
        transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        break;
    }
    // Color
    var meshes = new List<Renderer>();
    var transform_base = transform.Find("Mesh");
    transform_base.gameObject.layer = 11;
    var mesh_base = transform_base.GetComponent<Renderer>();
    if (mesh_base != null) meshes.Add(mesh_base);
    for (int i = 0; i < transform_base.childCount; i++)
    {
      transform_base.GetChild(i).gameObject.layer = 11;
      mesh_base = transform_base.GetChild(i).GetComponent<Renderer>();
      if (mesh_base != null) meshes.Add(mesh_base);
    }
    foreach (var mesh in meshes)
      for (int i = 0; i < mesh.sharedMaterials.Length; i++)
      {
        var shared = mesh.sharedMaterials;
        switch (shared[i].name)
        {
          case "Item":
            shared[i] = GameScript.s_Singleton._ItemMaterials[0];
            break;
          case "Item 1":
            shared[i] = GameScript.s_Singleton._ItemMaterials[1];
            break;
          case "Red":
            shared[i] = GameScript.s_Singleton._ItemMaterials[2];
            break;
          default:
            Debug.LogWarning($"Unhandled material in Item UI: {shared[i].name}");
            break;
        }
        mesh.sharedMaterials = shared;
      }

    return itemInfo;
  }

  public static GameObject GetItem(Items itemType)
  {
    if (_Items != null)
      foreach (var i in _Items)
      {
        if (i._type == itemType)
        {
          _Items.Remove(i);
          return i._gameObject;
        }
      }
    return null;
  }

  public static int GetItemValue(Items item)
  {
    switch (item)
    {
      case Items.NONE:
      case Items.FIST:
        return 0;
      case Items.KNIFE:
        return 1;
      case Items.STUN_BATON:
        return 2;
      case Items.FRYING_PAN:
      case Items.AXE:
      case Items.ROCKET_FIST:
      case Items.RAPIER:
      case Items.PISTOL_SILENCED:
      case Items.PISTOL_MACHINE:
      case Items.PISTOL_DOUBLE:
      case Items.PISTOL_CHARGE:
      case Items.STICKY_GUN:
      case Items.RIFLE:
      case Items.GRENADE_LAUNCHER:
        return 3;
      case Items.RIFLE_LEVER:
      case Items.RIFLE_CHARGE:
      case Items.SNIPER:
      case Items.CROSSBOW:
      case Items.KATANA:
      case Items.SHOTGUN_DOUBLE:
      case Items.SHOTGUN_PUMP:
      case Items.BAT:
      case Items.UZI:
      case Items.REVOLVER:
        return 4;
      case Items.FLAMETHROWER:
      case Items.AK47:
      case Items.SHOTGUN_BURST:
      case Items.M16:
      case Items.DMR:
        return 5;
      case Items.ROCKET_LAUNCHER:
      case Items.GRENADE_HOLD:
      case Items.PISTOL:
        break;
    }
    return -1;
  }
  public static int GetUtilityValue(UtilityScript.UtilityType utility)
  {
    switch (utility)
    {
      case UtilityScript.UtilityType.NONE:
        return 0;
      case UtilityScript.UtilityType.DASH:
      case UtilityScript.UtilityType.SHURIKEN:
      case UtilityScript.UtilityType.SHURIKEN_BIG:
      case UtilityScript.UtilityType.GRENADE_STUN:
      case UtilityScript.UtilityType.TACTICAL_BULLET:
      case UtilityScript.UtilityType.MIRROR:
      case UtilityScript.UtilityType.BEAR_TRAP:
        return 1;
      case UtilityScript.UtilityType.GRENADE:
      case UtilityScript.UtilityType.GRENADE_IMPACT:
      case UtilityScript.UtilityType.C4:
      case UtilityScript.UtilityType.MINE:
      case UtilityScript.UtilityType.MOLOTOV:
      case UtilityScript.UtilityType.KUNAI_EXPLOSIVE:
      case UtilityScript.UtilityType.KUNAI_STICKY:
      case UtilityScript.UtilityType.STOP_WATCH:
      case UtilityScript.UtilityType.TEMP_SHIELD:
        return 2;
      case UtilityScript.UtilityType.INVISIBILITY:
      case UtilityScript.UtilityType.MORTAR_STRIKE:
        return 3;
    }
    return 100;
  }
  public static int GetPerkValue(Perk.PerkType perk)
  {
    switch (perk)
    {
      case Perk.PerkType.FIRE_RATE_UP:
      case Perk.PerkType.AKIMBO:
        break;
      case Perk.PerkType.NONE:
      case Perk.PerkType.NO_SLOWMO:
        return 0;
      case Perk.PerkType.LASER_SIGHTS:
      case Perk.PerkType.MARTIAL_ARTIST:
        return 1;
      case Perk.PerkType.THRUST:
      case Perk.PerkType.SPEED_UP:
      case Perk.PerkType.TWIN:
        return 2;
      case Perk.PerkType.EXPLOSIONS_UP:
      case Perk.PerkType.GRAPPLE_MASTER:
      case Perk.PerkType.EXPLOSION_RESISTANCE:
      case Perk.PerkType.MAX_AMMO_UP:
      case Perk.PerkType.EXPLOSIVE_PARRY:
        return 3;
      case Perk.PerkType.PENETRATION_UP:
      case Perk.PerkType.ARMOR_UP:
      case Perk.PerkType.FASTER_RELOAD:
        return 4;
      case Perk.PerkType.SMART_BULLETS:
        return 6;
    }
    return 100;
  }
}