using System.Collections.Generic;
using SneakyEngine.Equippable.Item;
using SneakyEngine.Equippable.Utility;
using SneakyGame.Game.Items;
using SneakyGame.Game.Player;
using UnityEngine;

namespace SneakyGame.Ragdoll.Equippables
{

  public static class EquippableManager
  {
    static Dictionary<ItemType, ItemScript> _ItemPrefabs;

    public static void Init()
    {
      _ItemPrefabs = new();
      foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
      {
        if (itemType == ItemType.NONE) continue;

        var prefab = Resources.Load($"Items/{itemType}") as GameObject;
        var itemScript = prefab.GetComponent<ItemScript>();
        _ItemPrefabs[itemType] = itemScript;

        // Load item data json
        var jsonText = Resources.Load<TextAsset>($"Items/ItemData/{itemType}");
        var itemData = JsonUtility.FromJson<ItemData>(jsonText.text);

        itemScript._clipSize = itemData.ClipSize;
        itemScript._minimumClipToFire = itemData.MinimumClipToFire;
        itemScript._projectilesPerShot = itemData.ProjectilesPerShot;
        itemScript._burstPerShot = itemData.BurstPerShot;
        itemScript._penatrationAmount = itemData.PenetrationAmount;

        itemScript._isMelee = itemData.IsMelee;
        itemScript._twoHanded = itemData.IsTwoHanded;
        itemScript._reloadOneAtTime = itemData.ReloadOneAtTime;
        itemScript._silenced = itemData.IsSilenced;
        itemScript._throwable = itemData.IsThrowable;
        itemScript._dismember = itemData.CanDismember;
        itemScript._useOnRelease = itemData.UseOnRelease;
        itemScript._randomSpread = itemData.UseRandomSpread;
        itemScript._chargeHold = itemData.IsChargeHold;

        itemScript._reloadTime = itemData.ReloadTime;
        itemScript._useRate = itemData.UseRate;
        itemScript._burstRate = itemData.BurstRate;
        itemScript._hit_force = itemData.HitForce;
        itemScript._bullet_spread = itemData.BulletSpread;
        itemScript._shoot_force = itemData.ShootForce;
        itemScript._shoot_forward_force = itemData.ShootForwardForce;

        itemScript._customProjetile = itemData.CustomProjectile;

        itemScript.SetItemData(itemData);
      }
    }

    // Spawn a single item
    public static ItemScript SpawnItem(ItemType item)
    {
      if (item == ItemType.NONE) return null;

      var itemPrefab = _ItemPrefabs[item];
      GameObject new_item = Object.Instantiate(itemPrefab.gameObject);
      new_item.name = item.ToString();
      var script = new_item.GetComponent<ItemScript>();
      script.SetItemData(itemPrefab.GetItemData());
      new_item.transform.position = new Vector3(0f, -10f, 0f);
      return script;
    }

    public class ItemUIInfo
    {
      public Transform _Transform;
      public int _ClipSize;
    }
    static ItemUIInfo GetItemUI(string item, PlayerScript player, Transform parent)
    {
      var new_item = Object.Instantiate(Resources.Load($"Items/{item}") as GameObject, parent);
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
        case "CHAINSAW":
          transform.localPosition += new Vector3(-0.14f, 0.03f, 0f);
          transform.localScale = new Vector3(0.09f, 0.11f, 0.08f);
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
        case "COIN":
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

    public static int GetItemValue(ItemType item)
    {
      switch (item)
      {
        case ItemType.NONE:
        case ItemType.FIST:
          return 0;
        case ItemType.KNIFE:
          return 1;
        case ItemType.STUN_BATON:
          return 2;
        case ItemType.FRYING_PAN:
        case ItemType.AXE:
        case ItemType.ROCKET_FIST:
        case ItemType.RAPIER:
        case ItemType.PISTOL_SILENCED:
        case ItemType.PISTOL_MACHINE:
        case ItemType.PISTOL_DOUBLE:
        case ItemType.PISTOL_CHARGE:
        case ItemType.STICKY_GUN:
        case ItemType.RIFLE:
        case ItemType.GRENADE_LAUNCHER:
          return 3;
        case ItemType.RIFLE_LEVER:
        case ItemType.RIFLE_CHARGE:
        case ItemType.SNIPER:
        case ItemType.CROSSBOW:
        case ItemType.KATANA:
        case ItemType.CHAINSAW:
        case ItemType.SHOTGUN_DOUBLE:
        case ItemType.SHOTGUN_PUMP:
        case ItemType.BAT:
        case ItemType.UZI:
        case ItemType.REVOLVER:
          return 4;
        case ItemType.FLAMETHROWER:
        case ItemType.AK47:
        case ItemType.SHOTGUN_BURST:
        case ItemType.M16:
        case ItemType.DMR:
          return 5;
        case ItemType.ROCKET_LAUNCHER:
        case ItemType.GRENADE_HOLD:
        case ItemType.PISTOL:
          break;
      }
      return -1;
    }
    public static int GetUtilityValue(UtilityType utility)
    {
      switch (utility)
      {
        case UtilityType.NONE:
          return 0;
        case UtilityType.DASH:
        case UtilityType.SHURIKEN:
        case UtilityType.SHURIKEN_BIG:
        case UtilityType.GRENADE_STUN:
        case UtilityType.TACTICAL_BULLET:
        case UtilityType.MIRROR:
        case UtilityType.BEAR_TRAP:
        case UtilityType.COIN:
          return 1;
        case UtilityType.GRENADE:
        case UtilityType.GRENADE_IMPACT:
        case UtilityType.C4:
        case UtilityType.MINE:
        case UtilityType.MOLOTOV:
        case UtilityType.KUNAI_EXPLOSIVE:
        case UtilityType.KUNAI_STICKY:
        case UtilityType.STOP_WATCH:
        case UtilityType.TEMP_SHIELD:
          return 2;
        case UtilityType.INVISIBILITY:
        case UtilityType.MORTAR_STRIKE:
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
        case Perk.PerkType.BULLET_DESTROYER:
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

}