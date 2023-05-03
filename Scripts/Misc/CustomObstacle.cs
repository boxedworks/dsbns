using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomObstacle : MonoBehaviour
{
  public enum InteractType
  {
    REMOVEBARRIER,
    BUYITEM,
    BUYUTILITY,
    BUYPERK,
    BUYRANDOM
  }

  public InteractType _type;

  static readonly int _MAX_ROOMS = 12;

  int index;
  public int _index
  {
    get
    {
      return index;
    }
    set
    {
      CheckInit();
      if (_type == InteractType.BUYITEM) index = value % _BUYABLE_ITEM_TIERS.Count;
      else if (_type == InteractType.BUYUTILITY) index = value % _BUYABLE_UTILITY_TIERS.Count;
      else if (_type == InteractType.BUYPERK) index = value % _BUYABLE_PERK_TIERS.Count;
      else if (_type == InteractType.REMOVEBARRIER) index = value % _MAX_ROOMS;
      else index = 0;
    }
  }
  int index2;
  public int _index2
  {
    get { return index2; }
    set { index2 = value % _MAX_ROOMS; }
  }

  // Weapons
  static Dictionary<int, List<System.Tuple<GameScript.ItemManager.Items, int>>> _BUYABLE_ITEM_TIERS;
  System.Tuple<GameScript.ItemManager.Items, int> _weapon_info;

  // Perks
  static Dictionary<int, List<System.Tuple<Shop.Perk.PerkType, int>>> _BUYABLE_PERK_TIERS;
  System.Tuple<Shop.Perk.PerkType, int> _perk_info;

  // Utilities
  static Dictionary<int, List<System.Tuple<UtilityScript.UtilityType, int>>> _BUYABLE_UTILITY_TIERS;
  System.Tuple<UtilityScript.UtilityType, int> _utility_info;

  // Random
  static List<CustomObstacle> _RandomObstacles;
  InteractType _randomType;

  public static List<CustomObstacle> _CustomBarriers, _CustomInteractables, _CustomCandles;
  public static Dictionary<int, List<CustomObstacle>> _CustomSpawners;

  static int _CustomInteractables_index, _CustomCandle_index;

  // Interactable data
  bool _interactable;
  TextMesh _text;
  public int _pointCost;
  MeshRenderer _renderer;

  // Starting room
  public static CustomObstacle _PlayerSpawn;

  static Material[] _Materials;

  CandleScript candleScript;
  CandleScript _candleScript
  {
    get
    {
      if (candleScript == null) candleScript = GetComponent<CandleScript>();
      return candleScript;
    }
    set { candleScript = value; }
  }

  static void CheckInit()
  {

    // Create pricing lists
    if (_BUYABLE_ITEM_TIERS == null)
    {
      _BUYABLE_ITEM_TIERS = new Dictionary<int, List<System.Tuple<GameScript.ItemManager.Items, int>>>();

      _BUYABLE_ITEM_TIERS.Add(0, new List<System.Tuple<GameScript.ItemManager.Items, int>>());
      _BUYABLE_ITEM_TIERS[0].Add(System.Tuple.Create(GameScript.ItemManager.Items.KNIFE, 15));

      _BUYABLE_ITEM_TIERS.Add(1, new List<System.Tuple<GameScript.ItemManager.Items, int>>());
      _BUYABLE_ITEM_TIERS[1].Add(System.Tuple.Create(GameScript.ItemManager.Items.PISTOL, 25));

      _BUYABLE_ITEM_TIERS.Add(2, new List<System.Tuple<GameScript.ItemManager.Items, int>>());
      _BUYABLE_ITEM_TIERS[2].Add(System.Tuple.Create(GameScript.ItemManager.Items.RIFLE, 40));

      _BUYABLE_ITEM_TIERS.Add(3, new List<System.Tuple<GameScript.ItemManager.Items, int>>());
      _BUYABLE_ITEM_TIERS[3].Add(System.Tuple.Create(GameScript.ItemManager.Items.DOUBLE_PISTOL, 100));
      _BUYABLE_ITEM_TIERS[3].Add(System.Tuple.Create(GameScript.ItemManager.Items.SHOTGUN_PUMP, 150));
      _BUYABLE_ITEM_TIERS[3].Add(System.Tuple.Create(GameScript.ItemManager.Items.CROSSBOW, 85));
      _BUYABLE_ITEM_TIERS[3].Add(System.Tuple.Create(GameScript.ItemManager.Items.MACHINE_PISTOL, 75));

      _BUYABLE_ITEM_TIERS.Add(4, new List<System.Tuple<GameScript.ItemManager.Items, int>>());
      _BUYABLE_ITEM_TIERS[4].Add(System.Tuple.Create(GameScript.ItemManager.Items.UZI, 160));
      _BUYABLE_ITEM_TIERS[4].Add(System.Tuple.Create(GameScript.ItemManager.Items.REVOLVER, 250));
      _BUYABLE_ITEM_TIERS[4].Add(System.Tuple.Create(GameScript.ItemManager.Items.GRENADE_LAUNCHER, 150));
      _BUYABLE_ITEM_TIERS[4].Add(System.Tuple.Create(GameScript.ItemManager.Items.SHOTGUN_DOUBLE, 275));
      _BUYABLE_ITEM_TIERS[4].Add(System.Tuple.Create(GameScript.ItemManager.Items.DMR, 200));
      _BUYABLE_ITEM_TIERS[4].Add(System.Tuple.Create(GameScript.ItemManager.Items.RIFLE_LEVER, 200));
      _BUYABLE_ITEM_TIERS[4].Add(System.Tuple.Create(GameScript.ItemManager.Items.STICKY_GUN, 175));

      _BUYABLE_ITEM_TIERS.Add(5, new List<System.Tuple<GameScript.ItemManager.Items, int>>());
      _BUYABLE_ITEM_TIERS[5].Add(System.Tuple.Create(GameScript.ItemManager.Items.AK47, 325));
      _BUYABLE_ITEM_TIERS[5].Add(System.Tuple.Create(GameScript.ItemManager.Items.M16, 300));
      _BUYABLE_ITEM_TIERS[5].Add(System.Tuple.Create(GameScript.ItemManager.Items.SHOTGUN_BURST, 325));
      _BUYABLE_ITEM_TIERS[5].Add(System.Tuple.Create(GameScript.ItemManager.Items.SWORD, 300));
      _BUYABLE_ITEM_TIERS[5].Add(System.Tuple.Create(GameScript.ItemManager.Items.AXE, 250));
      _BUYABLE_ITEM_TIERS[5].Add(System.Tuple.Create(GameScript.ItemManager.Items.FLAMETHROWER, 300));
      _BUYABLE_ITEM_TIERS[5].Add(System.Tuple.Create(GameScript.ItemManager.Items.SNIPER, 250));
    }
    if (_BUYABLE_PERK_TIERS == null)
    {
      _BUYABLE_PERK_TIERS = new Dictionary<int, List<System.Tuple<Shop.Perk.PerkType, int>>>();

      _BUYABLE_PERK_TIERS.Add(0, new List<System.Tuple<Shop.Perk.PerkType, int>>());
      _BUYABLE_PERK_TIERS[0].Add(System.Tuple.Create(Shop.Perk.PerkType.LASER_SIGHTS, 75));

      _BUYABLE_PERK_TIERS.Add(1, new List<System.Tuple<Shop.Perk.PerkType, int>>());
      _BUYABLE_PERK_TIERS[1].Add(System.Tuple.Create(Shop.Perk.PerkType.EXPLOSION_RESISTANCE, 200));
      _BUYABLE_PERK_TIERS[1].Add(System.Tuple.Create(Shop.Perk.PerkType.EXPLOSIONS_UP, 300));
      _BUYABLE_PERK_TIERS[1].Add(System.Tuple.Create(Shop.Perk.PerkType.AKIMBO, 250));

      _BUYABLE_PERK_TIERS.Add(2, new List<System.Tuple<Shop.Perk.PerkType, int>>());
      _BUYABLE_PERK_TIERS[2].Add(System.Tuple.Create(Shop.Perk.PerkType.ARMOR_UP, 300));
      _BUYABLE_PERK_TIERS[2].Add(System.Tuple.Create(Shop.Perk.PerkType.MAX_AMMO_UP, 350));
      _BUYABLE_PERK_TIERS[2].Add(System.Tuple.Create(Shop.Perk.PerkType.FASTER_RELOAD, 400));
      _BUYABLE_PERK_TIERS[2].Add(System.Tuple.Create(Shop.Perk.PerkType.SMART_BULLETS, 450));
    }
    if (_BUYABLE_UTILITY_TIERS == null)
    {
      _BUYABLE_UTILITY_TIERS = new Dictionary<int, List<System.Tuple<UtilityScript.UtilityType, int>>>();

      _BUYABLE_UTILITY_TIERS.Add(0, new List<System.Tuple<UtilityScript.UtilityType, int>>());
      _BUYABLE_UTILITY_TIERS[0].Add(System.Tuple.Create(UtilityScript.UtilityType.GRENADE, 50));

      _BUYABLE_UTILITY_TIERS.Add(1, new List<System.Tuple<UtilityScript.UtilityType, int>>());
      _BUYABLE_UTILITY_TIERS[1].Add(System.Tuple.Create(UtilityScript.UtilityType.SHURIKEN, 20));
      _BUYABLE_UTILITY_TIERS[1].Add(System.Tuple.Create(UtilityScript.UtilityType.GRENADE_IMPACT, 40));
      _BUYABLE_UTILITY_TIERS[1].Add(System.Tuple.Create(UtilityScript.UtilityType.KUNAI_STICKY, 35));
      _BUYABLE_UTILITY_TIERS[1].Add(System.Tuple.Create(UtilityScript.UtilityType.C4, 40));
      _BUYABLE_UTILITY_TIERS[1].Add(System.Tuple.Create(UtilityScript.UtilityType.STOP_WATCH, 40));
    }

  }

  public void Init(bool midgame = false)
  {
    CheckInit();

    // Add to list
    if (_CustomInteractables == null)
      _CustomInteractables = new List<CustomObstacle>();
    if (!_CustomInteractables.Contains(this))
      _CustomInteractables.Add(this);

    // Init shared colors
    _renderer = transform.GetChild(1).GetComponent<MeshRenderer>();
    if (_Materials == null)
    {
      _Materials = new Material[]
      {
        new Material(_renderer.sharedMaterial),
        new Material(_renderer.sharedMaterial),
        new Material(_renderer.sharedMaterial),
        new Material(_renderer.sharedMaterial),
      };
    }
    Resources.UnloadAsset(_renderer.sharedMaterial);

    var use_type = _type;

    // Check random type
    var random = use_type == InteractType.BUYRANDOM;
    if (random)
    {
      if (_RandomObstacles == null) _RandomObstacles = new List<CustomObstacle>();
      if (!_RandomObstacles.Contains(this)) _RandomObstacles.Add(this);
      var r = Random.value;
      // Set type to item
      if (r < 0.6f)
        use_type = InteractType.BUYITEM;
      // Utility
      else if (r < 0.8f)
        use_type = InteractType.BUYUTILITY;
      // Perk
      else
        use_type = InteractType.BUYPERK;
      _randomType = use_type;
    }

    // Set defaults
    _interactable = true;
    transform.parent = GameResources._Container_Objects;
    _text = transform.GetChild(0).GetComponent<TextMesh>();
    if (use_type == InteractType.REMOVEBARRIER)
    {
      _pointCost = 75;
      _text.text = $"remove wall: {_pointCost}";
      _renderer.sharedMaterial = _Materials[0];
    }
    // Select random items based on tier
    else if (use_type == InteractType.BUYITEM)
    {
      if (random)
      {
        var weapons = _BUYABLE_ITEM_TIERS[Random.Range(0, _BUYABLE_ITEM_TIERS.Count)];
        var weapon_index = Random.Range(0, weapons.Count);
        _weapon_info = weapons[weapon_index];
      }
      else
      {
        var weapons = _BUYABLE_ITEM_TIERS[_index];
        if (weapons.Count == 0) return;
        var iter = 0;
        var weapon_index = Random.Range(0, weapons.Count);
        while (iter < weapons.Count)
        {
          weapon_index = (weapon_index + iter++) % weapons.Count;
          var weapon = weapons[weapon_index].Item1;
          var found = false;
          foreach (var other_interactable in _CustomInteractables)
            if (other_interactable._type == _type && other_interactable._weapon_info != null && other_interactable._weapon_info.Item1 == weapon)
            {
              found = true;
              break;
            }
          if (!found) break;
        }
        _weapon_info = weapons[weapon_index];
      }
      _pointCost = _weapon_info.Item2;
      _text.text = $"buy {_weapon_info.Item1}: {_pointCost}";
      _renderer.sharedMaterial = _Materials[1];
      _renderer.sharedMaterial.SetColor("_EmissionColor", Color.blue);
    }
    else if (use_type == InteractType.BUYUTILITY)
    {
      if (random)
      {
        var utilities = _BUYABLE_UTILITY_TIERS[Random.Range(0, _BUYABLE_UTILITY_TIERS.Count)];
        var utility_index = Random.Range(0, utilities.Count);
        _utility_info = utilities[utility_index];
      }
      else
      {
        var utilities = _BUYABLE_UTILITY_TIERS[_index];
        if (utilities.Count == 0) return;
        var iter = 0;
        var utility_index = Random.Range(0, utilities.Count);
        while (iter < utilities.Count)
        {
          utility_index = (utility_index + iter++) % utilities.Count;
          var utility = utilities[utility_index].Item1;
          var found = false;
          foreach (var other_interactable in _CustomInteractables)
            if (other_interactable._type == _type && other_interactable._weapon_info != null && other_interactable._utility_info.Item1 == utility)
            {
              found = true;
              break;
            }
          if (!found) break;
        }
        _utility_info = utilities[utility_index];
      }
      _pointCost = _utility_info.Item2;
      _text.text = $"buy {_utility_info.Item1}: {_pointCost}";
      _renderer.sharedMaterial = _Materials[2];
      _renderer.sharedMaterial.SetColor("_EmissionColor", Color.green);
    }
    else if (use_type == InteractType.BUYPERK)
    {
      if (random)
      {
        var perks = _BUYABLE_PERK_TIERS[Random.Range(0, _BUYABLE_PERK_TIERS.Count)];
        var perk_index = Random.Range(0, perks.Count);
        _perk_info = perks[perk_index];
      }
      else
      {
        var perks = _BUYABLE_PERK_TIERS[_index];
        if (perks.Count == 0) return;
        var iter = 0;
        var perk_index = Random.Range(0, perks.Count);
        while (iter < perks.Count)
        {
          perk_index = (perk_index + iter++) % perks.Count;
          var perk = perks[perk_index].Item1;
          var found = false;
          foreach (var other_interactable in _CustomInteractables)
            if (other_interactable._type == _type && other_interactable._weapon_info != null && other_interactable._perk_info.Item1 == perk)
            {
              found = true;
              break;
            }
          if (!found) break;
        }
        _perk_info = perks[perk_index];
      }
      _pointCost = _perk_info.Item2;
      _text.text = $"buy {_perk_info.Item1}: {_pointCost}\n<color=yellow>{Shop.Perk._PERK_DESCRIPTIONS[_perk_info.Item1]}</color>";
      _renderer.sharedMaterial = _Materials[3];
      _renderer.sharedMaterial.SetColor("_EmissionColor", Color.red);
    }

    // Hide interactable
    if (!GameScript._EditorEnabled && !midgame)
      gameObject.SetActive(false);

    // Set scale
    transform.GetChild(1).localScale = Vector3.zero;
  }

  public void InitMoveableBarrier()
  {
    // Add to dictionary
    if (_CustomBarriers == null)
      _CustomBarriers = new List<CustomObstacle>();
    _CustomBarriers.Add(this);
    // Move to objects
    transform.parent = GameResources._Container_Objects;
    // Add navmeshobstacle
    var navMesh = gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
    navMesh.size = new Vector3(1.2f, 2f, 0.4f);
    navMesh.carveOnlyStationary = true;
    navMesh.carving = true;
  }

  public void InitZombieSpawn()
  {
    // Add to dictionary
    if (_CustomSpawners == null)
      _CustomSpawners = new Dictionary<int, List<CustomObstacle>>();
    if (!_CustomSpawners.ContainsKey(_index))
      _CustomSpawners[_index] = new List<CustomObstacle>();
    _CustomSpawners[_index].Add(this);
  }

  public void InitCandle()
  {
    if (_CustomCandles == null)
      _CustomCandles = new List<CustomObstacle>();
    _CustomCandles.Add(this);
  }

  // Player interacted with object
  public enum InteractSide { DEFAULT, LEFT, RIGHT }
  public void Interact(PlayerScript p, InteractSide side)
  {
    // If player is dead, do not buy
    if (p == null || p._ragdoll == null || p._ragdoll._dead) return;

    // If text not visible, do not do anything
    if (!_text.gameObject.activeSelf) return;

    // Purchase
    if (GameScript.SurvivalMode.HasPoints(p._Id, _pointCost))
    {
      // Check random
      var use_type = _type == InteractType.BUYRANDOM ? _randomType : _type;

      // Type
      if (use_type == InteractType.REMOVEBARRIER)
      {
        gameObject.SetActive(false);
        _CustomInteractables.Remove(this);
        p.RemoveInteractable();
        GameScript.SurvivalMode.OpenRoom(_index, _index2);
        // Play sound
        p._ragdoll.PlaySound("Survival/Buy_Wall");
      }
      else if (use_type == InteractType.BUYITEM)
      {
        var item_info = _weapon_info;
        var item_type = item_info.Item1;

        var equip_side = ActiveRagdoll.Side.RIGHT;

        var akimbo = Shop.Perk.HasPerk(p._Id, Shop.Perk.PerkType.AKIMBO);

        // Check if can equip
        var equip_info = CheckEquip(p, item_type, side, akimbo, 0);
        if (!equip_info.Item1)
        {
          var text = equip_info.Item3;
          if (text != null && text.Length > 0) p._ragdoll.DisplayText(text);
          return;
        }
        equip_side = equip_info.Item2;

        // Check if replacing a weapon
        if (equip_info.Item3 == "BOTH")
        {
          if (p._Profile._item_left == GameScript.ItemManager.Items.NONE && p._Profile._item_right == GameScript.ItemManager.Items.NONE) { }
          else if (p._Profile._item_left_other != GameScript.ItemManager.Items.NONE && p._Profile._item_right_other != GameScript.ItemManager.Items.NONE) { }
          else if (p._Profile._item_left_other == GameScript.ItemManager.Items.NONE && p._Profile._item_right_other == GameScript.ItemManager.Items.NONE)
          {
            p._Profile._item_left_other = p._Profile._item_left;
            p._Profile._item_right_other = p._Profile._item_right;
          }
          else
          {
            GameScript.ItemManager.Items[] items = null;
            if (p._Profile._item_left_other == GameScript.ItemManager.Items.NONE && p._Profile._item_left != GameScript.ItemManager.Items.NONE)
              items = new GameScript.ItemManager.Items[] { p._Profile._item_left, p._Profile._item_right };
            else if (p._Profile._item_right_other == GameScript.ItemManager.Items.NONE && p._Profile._item_right != GameScript.ItemManager.Items.NONE)
              items = new GameScript.ItemManager.Items[] { p._Profile._item_right, p._Profile._item_left };
            if (items != null)
              foreach (var current_item in items)
              {
                // Try to equip in other slots
                var equip_info2 = CheckEquip(p, current_item, InteractSide.DEFAULT, akimbo, 1);
                var equip_side2 = equip_info2.Item2;

                var current_item2 = (equip_side2 == ActiveRagdoll.Side.LEFT ? p._Profile._item_left_other : p._Profile._item_right_other);
                if (current_item2 == GameScript.ItemManager.Items.NONE)
                  if (equip_info2.Item1)
                  {
                    if (equip_side2 == ActiveRagdoll.Side.LEFT)
                      p._Profile._item_left_other = current_item;
                    else
                      p._Profile._item_right_other = current_item;
                    break;
                  }
              }
          }
        }
        else
        {
          var current_item = (equip_side == ActiveRagdoll.Side.LEFT ? p._Profile._item_left : p._Profile._item_right);
          if (current_item != GameScript.ItemManager.Items.NONE)
          {
            // Try to equip in other slots
            var equip_info2 = CheckEquip(p, current_item, InteractSide.DEFAULT, akimbo, 1);
            var equip_side2 = equip_info2.Item2;

            var current_item2 = (equip_side2 == ActiveRagdoll.Side.LEFT ? p._Profile._item_left_other : p._Profile._item_right_other);
            if (current_item2 == GameScript.ItemManager.Items.NONE)
              if (equip_info2.Item1)
              {
                if (equip_side2 == ActiveRagdoll.Side.LEFT)
                  p._Profile._item_left_other = current_item;
                else
                  p._Profile._item_right_other = current_item;
              }
          }
        }

        // Equip
        p._ragdoll.EquipItem(item_type, equip_side);
        if (equip_info.Item3 == "BOTH")
          p._Profile._item_right = GameScript.ItemManager.Items.NONE;

        // Update UI
        if (equip_side == ActiveRagdoll.Side.LEFT)
          p._Profile._item_left = item_type;
        else
          p._Profile._item_right = item_type;
        GameScript.PlayerProfile.s_Profiles[p._Id].UpdateIcons();

        // Play sfx
        p._ragdoll.PlaySound("Survival/Buy_Weapon");

#if UNITY_STANDALONE
        // Achievement
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.SURVIVAL_BUY_ITEM);
#endif
      }
      else if (use_type == InteractType.BUYUTILITY)
      {
        var utility_type = _utility_info.Item1;
        var equip_side = side == InteractSide.LEFT ? ActiveRagdoll.Side.LEFT : ActiveRagdoll.Side.RIGHT;
        if (side == InteractSide.DEFAULT) equip_side = ActiveRagdoll.Side.RIGHT;

        // Check utility side
        var equipment = p._Profile._equipment;
        var add = false;
        if (equipment._utilities_left.Length == 0 && equipment._utilities_right.Length == 0) { }
        if (side == InteractSide.DEFAULT)
        {
          if (equipment._utilities_right.Length > 0 && equipment._utilities_right[0] == utility_type)
          {
            // Check utility cap
            if (equipment._utilities_right.Length == Shop._Utility_Cap[utility_type])
            {
              if (equipment._utilities_left.Length == 0)
              {
                equip_side = ActiveRagdoll.Side.LEFT;
              }
              else if (equipment._utilities_left.Length > 0 && equipment._utilities_left[0] == utility_type)
              {
                // Check utility cap
                if (equipment._utilities_left.Length == Shop._Utility_Cap[utility_type])
                {
                  p._ragdoll.DisplayText("i have too many..");
                  return;
                }
                equip_side = ActiveRagdoll.Side.LEFT;
                add = true;
              }
              else
              {
                p._ragdoll.DisplayText("i have too many..");
                return;
              }
            }
            else
            {
              add = true;
            }
          }
          else if (equipment._utilities_left.Length > 0 && equipment._utilities_left[0] == utility_type)
          {
            // Check utility cap
            if (equipment._utilities_left.Length == Shop._Utility_Cap[utility_type])
            {
              if (equipment._utilities_right.Length == 0)
              {
                equip_side = ActiveRagdoll.Side.RIGHT;
              }
              else if (equipment._utilities_right.Length > 0 && equipment._utilities_right[0] == utility_type)
              {
                // Check utility cap
                if (equipment._utilities_right.Length == Shop._Utility_Cap[utility_type])
                {
                  p._ragdoll.DisplayText("i have too many..");
                  return;
                }
                equip_side = ActiveRagdoll.Side.RIGHT;
                add = true;
              }
              else
              {
                p._ragdoll.DisplayText("i have too many..");
                return;
              }
            }
            else
            {
              equip_side = ActiveRagdoll.Side.LEFT;
              add = true;
            }
          }
        }
        else if (equip_side == ActiveRagdoll.Side.LEFT)
        {
          if (equipment._utilities_left.Length == 0) { }
          else if (equipment._utilities_left.Length > 0 && equipment._utilities_left[0] == utility_type)
          {
            // Check utility cap
            if (equipment._utilities_left.Length == Shop._Utility_Cap[utility_type])
            {
              p._ragdoll.DisplayText("i have too many..");
              return;
            }

            // Mark to add
            add = true;
          }
          else if (equipment._utilities_right.Length == 0) { equip_side = ActiveRagdoll.Side.RIGHT; }
          else if (equipment._utilities_left.Length > 0) { }
          else return;
        }
        else if (equip_side == ActiveRagdoll.Side.RIGHT)
        {
          if (equipment._utilities_right.Length == 0) { }
          else if (equipment._utilities_right.Length > 0 && equipment._utilities_right[0] == utility_type)
          {
            // Check utility cap
            if (equipment._utilities_right.Length == Shop._Utility_Cap[utility_type])
            {
              p._ragdoll.DisplayText("i have too many..");
              return;
            }

            // Mark to add
            add = true;
          }
          else if (equipment._utilities_left.Length == 0) { equip_side = ActiveRagdoll.Side.RIGHT; }
          else if (equipment._utilities_right.Length > 0) { }
          else return;
        }

        // Check if replacing utility
        var utilities = (equip_side == ActiveRagdoll.Side.LEFT ? equipment._utilities_left : equipment._utilities_right);

        // Equip
        var saveamount = add ? p.UtilityCount(equip_side) + 1 : -1;
        var new_amount = add ? utilities.Length + 1 : 1;
        if (equip_side == ActiveRagdoll.Side.LEFT)
        {
          equipment._utilities_left = new UtilityScript.UtilityType[new_amount];
          for (var i = 0; i < equipment._utilities_left.Length; i++)
            equipment._utilities_left[i] = utility_type;
        }
        else
        {
          equipment._utilities_right = new UtilityScript.UtilityType[new_amount];
          for (var i = 0; i < equipment._utilities_right.Length; i++)
            equipment._utilities_right[i] = utility_type;
        }

        p.RegisterUtility(equip_side, saveamount);

        // Update UI
        GameScript.PlayerProfile.s_Profiles[p._Id].UpdateIcons();

        // Play sound
        p._ragdoll.PlaySound("Survival/Buy_Weapon");

#if UNITY_STANDALONE
        // Achievement
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.SURVIVAL_BUY_ITEM);
#endif
      }
      else if (use_type == InteractType.BUYPERK)
      {
        var perk = _perk_info.Item1;

        if (Shop.Perk.HasPerk(p._Id, perk))
        {
          var text_haveAlready = "i have this already";
          p._ragdoll.DisplayText(text_haveAlready);
          return;
        }

        // Check max perks
        if (Shop.Perk.GetNumPerks(p._Id) > 3)
        {
          var text_haveAlready = "i already have 4 mods";
          p._ragdoll.DisplayText(text_haveAlready);
          return;
        }

        Shop.Perk.BuyPerk(p._Id, perk);

        // Update UI
        GameScript.PlayerProfile.s_Profiles[p._Id].UpdateIcons();

        // Play sound
        p._ragdoll.PlaySound("Survival/Buy_Weapon");

#if UNITY_STANDALONE
        // Achievement
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.SURVIVAL_BUY_ITEM);
        SteamManager.Achievements.UnlockAchievement(SteamManager.Achievements.Achievement.SURVIVAL_BUY_MOD);
#endif
      }

      // Reduce points
      GameScript.SurvivalMode.SpendPoints(p._Id, _pointCost);
    }
    else
      p._ragdoll.DisplayText("not enough points..");
  }

  float _desiredScale;
  public void Update()
  {
    // Scale
    if (!_interactable) return;
    transform.GetChild(1).localScale += ((new Vector3(1f, 1f, 1f) * (GameScript._EditorEnabled ? 0.3f : _desiredScale)) - transform.GetChild(1).localScale) * Time.deltaTime * 8f;
    _text.transform.localScale += ((new Vector3(1f, 1f, 1f) * ((_desiredScale - 0.15f) / 0.15f)) - _text.transform.localScale) * Time.deltaTime * 8f;
    if (_text.transform.localScale.x < 0f) _text.transform.localScale = Vector3.zero;
  }

  //
  System.Tuple<bool, ActiveRagdoll.Side, string> CheckEquip(PlayerScript p, GameScript.ItemManager.Items item_type, InteractSide side, bool akimbo, int equipmentIndex)
  {
    var equip_side = ActiveRagdoll.Side.RIGHT;
    if (side == InteractSide.LEFT) equip_side = ActiveRagdoll.Side.LEFT;

    var item_type_l = equipmentIndex == 0 ? p._Profile._item_left : p._Profile._item_left_other;
    var item_type_r = equipmentIndex == 0 ? p._Profile._item_right : p._Profile._item_right_other;

    var has_item_l = item_type_l != GameScript.ItemManager.Items.NONE;
    var has_item_r = item_type_r != GameScript.ItemManager.Items.NONE;

    var has_actual_two_handed = (item_type_l != GameScript.ItemManager.Items.NONE && Shop.IsActuallyTwoHanded(item_type_l)) ||
      (item_type_r != GameScript.ItemManager.Items.NONE && Shop.IsActuallyTwoHanded(item_type_r));
    var actual_two_handed_side = item_type_l != GameScript.ItemManager.Items.NONE ? ActiveRagdoll.Side.LEFT : ActiveRagdoll.Side.RIGHT;
    var actual_two_handed_type = actual_two_handed_side == ActiveRagdoll.Side.LEFT ? item_type_l : item_type_r;

    // Ragdoll text
    var text = "";

    var text_needAkimbo = "i can't hold both";
    var text_haveAlready = "i have this already";

    // DEFAULT
    if (side == InteractSide.DEFAULT)
    {
      if (!has_item_r && !has_item_l) { }
      else if (has_actual_two_handed)
        if (actual_two_handed_type == item_type)
          return System.Tuple.Create(false, equip_side, text_haveAlready);
        else
          return System.Tuple.Create(true, actual_two_handed_side, text);
      else if (Shop.IsActuallyTwoHanded(item_type))
        if (actual_two_handed_type == item_type)
          return System.Tuple.Create(false, equip_side, text_haveAlready);
        else
          return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT, "BOTH");
      else if (has_item_r && has_item_l)
      {
        if (Shop.IsTwoHanded(item_type_r) && Shop.IsTwoHanded(item_type))
        {
          if (item_type_r == item_type)
          {
            if (item_type_l != item_type)
            {
              if (akimbo)
                return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT, text);
              else
                text = (text_needAkimbo);
            }
            else
              text = (text_haveAlready);
            return System.Tuple.Create(false, equip_side, text);
          }
          else if (akimbo && !Shop.IsTwoHanded(item_type_l))
            equip_side = ActiveRagdoll.Side.LEFT;
        }
        else if (Shop.IsTwoHanded(item_type_l) && Shop.IsTwoHanded(item_type))
        {
          if (item_type_l == item_type)
          {
            if (item_type_r != item_type)
            {
              if (akimbo)
                return System.Tuple.Create(true, ActiveRagdoll.Side.RIGHT, text);
              else
                text = (text_needAkimbo);
            }
            else
              text = (text_haveAlready);
            return System.Tuple.Create(false, equip_side, text);
          }
          else if (akimbo && !Shop.IsTwoHanded(item_type_r))
            return System.Tuple.Create(false, ActiveRagdoll.Side.RIGHT, text);
          equip_side = ActiveRagdoll.Side.LEFT;
        }
        else if (item_type_r == item_type)
        {
          if (item_type_l == item_type)
          {
            text = (text_haveAlready);
            return System.Tuple.Create(false, equip_side, text);
          }
          else
            equip_side = ActiveRagdoll.Side.LEFT;
        }
      }
      else if (has_item_r && !has_item_l)
      {
        if (Shop.IsTwoHanded(item_type_r) && Shop.IsTwoHanded(item_type))
        {
          if (akimbo)
            return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT, text);
          else
          {
            if (item_type == item_type_r)
            {
              text = (text_haveAlready);
              return System.Tuple.Create(false, ActiveRagdoll.Side.RIGHT, text);
            }
            return System.Tuple.Create(true, ActiveRagdoll.Side.RIGHT, text);
          }
        }
        else
          equip_side = ActiveRagdoll.Side.LEFT;
      }
      else if (!has_item_r && has_item_l)
      {
        if (Shop.IsTwoHanded(item_type_l) && Shop.IsTwoHanded(item_type))
        {
          if (item_type_l == item_type)
            if (akimbo)
              return System.Tuple.Create(true, ActiveRagdoll.Side.RIGHT, text);
            else
            {
              if (item_type == item_type_l)
              {
                text = (text_haveAlready);
                return System.Tuple.Create(false, ActiveRagdoll.Side.LEFT, text);
              }
              return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT, text);
            }
        }
      }

    }
    // LEFT
    else if (side == InteractSide.LEFT)
    {

      if (!has_item_l && !has_item_r) { }
      else if (has_actual_two_handed)
        if (actual_two_handed_type == item_type)
          return System.Tuple.Create(false, equip_side, text_haveAlready);
        else
          return System.Tuple.Create(true, actual_two_handed_side, text);
      else if (Shop.IsActuallyTwoHanded(item_type))
        if (actual_two_handed_type == item_type)
          return System.Tuple.Create(false, equip_side, text_haveAlready);
        else
          return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT, "BOTH");
      else if (has_item_l && !has_item_r)
      {
        if (item_type_l == item_type)
        {
          text = (text_haveAlready);
          return System.Tuple.Create(false, ActiveRagdoll.Side.LEFT, text);
        }
      }
      else if (has_item_l && has_item_r)
      {
        if (Shop.IsTwoHanded(item_type_l) && Shop.IsTwoHanded(item_type))
        {
          if (item_type_l == item_type)
          {
            text = (text_haveAlready);
            return System.Tuple.Create(false, equip_side, text);
          }
        }
        else if (Shop.IsTwoHanded(item_type_r) && Shop.IsTwoHanded(item_type))
        {
          if (akimbo)
            return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT, text);
          else
          {
            text = (text_needAkimbo);
            return System.Tuple.Create(false, equip_side, text);
          }
        }
        else if (item_type_l == item_type)
        {
          text = (text_haveAlready);
          return System.Tuple.Create(false, equip_side, text);
        }
      }
      else if (!has_item_l && has_item_r)
      {
        if (Shop.IsTwoHanded(item_type))
        {
          if (Shop.IsTwoHanded(item_type_r))
          {
            if (akimbo)
              return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT, text);
            else
            {
              text = (text_needAkimbo);
              return System.Tuple.Create(false, equip_side, text);
            }
          }
        }
      }
    }
    // RIGHT
    else
    {

      if (!has_item_r && !has_item_l) { }
      else if (has_actual_two_handed)
        if (actual_two_handed_type == item_type)
          return System.Tuple.Create(false, equip_side, text_haveAlready);
        else
          return System.Tuple.Create(true, actual_two_handed_side, text);
      else if (Shop.IsActuallyTwoHanded(item_type))
        if (actual_two_handed_type == item_type)
          return System.Tuple.Create(false, equip_side, text_haveAlready);
        else
          return System.Tuple.Create(true, ActiveRagdoll.Side.LEFT, "BOTH");
      else if (has_item_r && !has_item_l)
      {
        if (item_type_r == item_type)
        {
          text = (text_haveAlready);
          return System.Tuple.Create(false, ActiveRagdoll.Side.RIGHT, text);
        }
      }
      else if (has_item_r && has_item_l)
      {
        if (Shop.IsTwoHanded(item_type_r) && Shop.IsTwoHanded(item_type))
        {
          if (item_type_r == item_type)
          {
            text = (text_haveAlready);
            return System.Tuple.Create(false, equip_side, text);
          }
        }
        else if (Shop.IsTwoHanded(item_type_l) && Shop.IsTwoHanded(item_type))
        {
          if (akimbo)
            return System.Tuple.Create(true, ActiveRagdoll.Side.RIGHT, text);
          else
          {
            text = (text_needAkimbo);
            return System.Tuple.Create(false, equip_side, text);
          }
        }
        else if (item_type_r == item_type)
        {
          text = (text_haveAlready);
          return System.Tuple.Create(false, equip_side, text);
        }
      }
      else if (has_item_l && !has_item_r)
      {
        if (Shop.IsTwoHanded(item_type))
        {
          if (Shop.IsTwoHanded(item_type_l))
          {
            if (akimbo)
              return System.Tuple.Create(true, ActiveRagdoll.Side.RIGHT, text);
            else
            {
              text = (text_needAkimbo);
              return System.Tuple.Create(false, equip_side, text);
            }
          }
        }
      }

    }

    return System.Tuple.Create(true, equip_side, Shop.IsActuallyTwoHanded(item_type) ? "BOTH" : text);
  }

  public static void Randomize()
  {
    foreach (var r in _RandomObstacles)
      r.Init(true);
  }

  enum CType
  {
    INTERACT,
    CANDLE
  }
  void Handle(CType ctype)
  {
    if (this == null) return;
    if (ctype == CType.CANDLE && !_candleScript._enabled) return;

    // Check for nearby players
    _desiredScale = 0f;
    if (PlayerScript.s_Players != null)
    {
      var min_dist = 1000f;
      foreach (var player in PlayerScript.s_Players)
      {
        if (player._ragdoll._dead || player == null) continue;
        var path = new UnityEngine.AI.NavMeshPath();
        var filter = new UnityEngine.AI.NavMeshQueryFilter();
        filter.areaMask = 1;
        filter.agentTypeID = GameScript.IsSurvival() ? TileManager._navMeshSurface2.agentTypeID : TileManager._navMeshSurface.agentTypeID;
        var usePosition = transform.position;
        if (ctype == CType.CANDLE)
        {
          var hit = new UnityEngine.AI.NavMeshHit();
          UnityEngine.AI.NavMesh.SamplePosition(transform.position - new Vector3(0f, 1f, 0f), out hit, 2f, filter.areaMask);
          usePosition = hit.position;
        }
        if (UnityEngine.AI.NavMesh.CalculatePath(usePosition, player.transform.position, filter, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
        {
          var dist = FunctionsC.GetPathLength(path.corners);
          if (dist < min_dist) min_dist = dist;
          if (dist < 0.6f) break;
        }

        //Debug.DrawLine(usePosition, player.transform.position, Color.red, 1f);
      }

      if (ctype == CType.INTERACT)
        _desiredScale = (min_dist < 0.6f ? 0.3f : (min_dist < 1.5f ? 0.15f : 0f));
      else if (ctype == CType.CANDLE)
      {
        //Debug.Log(min_dist);
        var min = GameScript.IsSurvival() ? 5.5f : 7;
        var max = GameScript.IsSurvival() ? 8.5f : 12f;
        if (min_dist < min) _candleScript._normalizedEnable = 1f;
        else if (min_dist > max) _candleScript._normalizedEnable = 0f;
        else _candleScript._normalizedEnable = 1f - ((min_dist - min) / (max - min));
      }
    }
    if (_text != null && _text.gameObject != null)
      _text.gameObject.SetActive(_desiredScale >= 0.13f);
  }

  public static void HandleAll()
  {
    if (_CustomInteractables != null)
      _CustomInteractables[_CustomInteractables_index++ % _CustomInteractables.Count].Handle(CType.INTERACT);
    if (_CustomCandles != null)
      _CustomCandles[_CustomCandle_index++ % _CustomCandles.Count].Handle(CType.CANDLE);
  }

  // Reset all data containers
  public static void Reset()
  {
    _CustomBarriers = null;
    _CustomSpawners = null;
    _CustomCandles = null;
    _PlayerSpawn = null;

    _RandomObstacles = null;

    _CustomInteractables = null;
    _CustomInteractables_index = 0;
  }
}