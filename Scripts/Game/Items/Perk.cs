
using System.Collections.Generic;

namespace Assets.Scripts.Game.Items
{

  public static class Perk
  {

    public enum PerkType
    {
      PENETRATION_UP,
      ARMOR_UP,
      EXPLOSION_RESISTANCE,
      EXPLOSIONS_UP,
      FASTER_RELOAD,
      MAX_AMMO_UP,
      FIRE_RATE_UP,
      LASER_SIGHTS,
      AKIMBO,
      NO_SLOWMO,
      SMART_BULLETS,
      GRAPPLE_MASTER,
      SPEED_UP,
      EXPLOSIVE_PARRY,
      MARTIAL_ARTIST,
      THRUST,

      TWIN,

      NONE
    }

    public static Dictionary<PerkType, string> _PERK_DESCRIPTIONS;

    public static void Init()
    {
      _PERK_DESCRIPTIONS = new Dictionary<PerkType, string>
      {
        { PerkType.PENETRATION_UP, "bullets penetrate deeper" },
        { PerkType.ARMOR_UP, "gain 2 extra health" },
        { PerkType.EXPLOSION_RESISTANCE, "survive self-explosions" },
        { PerkType.EXPLOSIONS_UP, "1.25x explosion radius" },
        { PerkType.FASTER_RELOAD, "1.4x reload speed" },
        { PerkType.MAX_AMMO_UP, "1.5x clip size" },
        { PerkType.AKIMBO, "dual wield big guns" },
        { PerkType.LASER_SIGHTS, "guns have laser sights" },
        { PerkType.FIRE_RATE_UP, "guns shoot faster" },
        { PerkType.NO_SLOWMO, "no slowmo; harder" },
        { PerkType.SMART_BULLETS, "strong gun = smart bullet" },
        { PerkType.THRUST, "extra melee range" },
        { PerkType.GRAPPLE_MASTER, "grapple armor" },
        { PerkType.MARTIAL_ARTIST, "empty hands are fists" },
        { PerkType.SPEED_UP, "1.15x movement speed" },
        { PerkType.EXPLOSIVE_PARRY, "parried bullets explode" },
        { PerkType.TWIN, "summon linked twin" },
      };
    }

    public static bool HasPerk(int playerId, PerkType perk)
    {
      return PlayerProfile.s_Profiles[playerId]._Equipment._Perks.Contains(perk);
    }

    public static List<PerkType> GetPerks(int playerId)
    {
      return PlayerProfile.s_Profiles[playerId]._Equipment._Perks;
    }

    public static int GetNumPerks(int playerId)
    {
      return PlayerProfile.s_Profiles[playerId]._Equipment._Perks.Count;
    }

    public static void BuyPerk(int playerId, PerkType perk)
    {
      if (!GameScript.s_IsZombieGameMode) return;

      PlayerProfile.s_Profiles[playerId]._Equipment._Perks.Add(perk);
      PlayerProfile.s_Profiles[playerId].UpdatePerkIcons();

      switch (perk)
      {
        // Increase max ammo
        case PerkType.MAX_AMMO_UP:
          foreach (var player in PlayerScript.s_Players)
          {
            if (player._Id == playerId)
            {
              player._Ragdoll.RefillAmmo();
              player._Profile.UpdateIcons();
              return;
            }
          }
          break;
        // Give laser sights to ranged weapons
        case PerkType.LASER_SIGHTS:
          foreach (var player in PlayerScript.s_Players)
          {
            if (player._Id == playerId)
            {
              player._Ragdoll._ItemL?.AddLaserSight();
              player._Ragdoll._ItemR?.AddLaserSight();
              return;
            }
          }
          break;
        // Give two extra health
        case PerkType.ARMOR_UP:
          foreach (var player in PlayerScript.s_Players)
          {
            if (player._Id == playerId)
            {
              player._Ragdoll._health += 2;
              player._Profile.UpdateHealthUI();
              if (player._Ragdoll._health > 3)
                player._Ragdoll.AddArmor();
              return;
            }
          }
          break;
      }
    }

  }

}