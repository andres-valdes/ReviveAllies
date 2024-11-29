using HarmonyLib;
using UnityEngine;
using System;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class Player_Patch
    {

        public static void RPC_Revive(this Player player, long sender)
        {
            RevivePoint revivePoint = player.GetRevivePoint();
            if (revivePoint == null) {
                return;
            }
            ClientRespawnManager.RespawnAt(revivePoint);
        }

        public static RevivePoint GetRevivePoint(this Player player)
        {
            return RevivePoint.GetRevivePoint(player);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        public class Player_Awake_RegisterRPCs
        {
            public static void Postfix(Player __instance)
            {
                __instance.m_nview.Register("Revive", __instance.RPC_Revive);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
        public class Player_Interact_SelectPreferredComponentOnInteract
        {
            public static bool Prefix(Player __instance, ref GameObject go, ref bool hold, ref bool alt)
            {
                if (__instance.InAttack() || __instance.InDodge() || (hold && Time.time - __instance.m_lastHoverInteractTime < 0.2f))
                {
                    return false;
                }
                Interactable componentInParent = go.GetPreferredComponentInParent<Interactable>();
                if (componentInParent != null)
                {
                    __instance.m_lastHoverInteractTime = Time.time;
                    if (componentInParent.Interact(__instance, hold, alt))
                    {
                        __instance.DoInteractAnimation((componentInParent as MonoBehaviour).gameObject);
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.FindHoverObject))]
        public class PatchIgnoreDeadPlayersOnHover
        {
            public static bool Prefix(Player __instance, ref GameObject hover, ref Character hoverCreature)
            {
                hover = null;
                hoverCreature = null;
                RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, 50f, __instance.m_interactMask);
                Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
                RaycastHit[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    RaycastHit raycastHit = array2[i];
                    if ((bool)raycastHit.collider.attachedRigidbody && raycastHit.collider.attachedRigidbody.gameObject == __instance.gameObject)
                    {
                        continue;
                    }
                    if (hoverCreature == null)
                    {
                        Character character = (raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>());
                        if (character != null && (!character.GetBaseAI() || !character.GetBaseAI().IsSleeping()))
                        {
                            hoverCreature = character;
                        }
                    }
                    if (raycastHit.collider.GetComponent<Player>()?.m_nview?.GetZDO()?.GetBool("dead") ?? false)
                    {
                        continue;
                    }
                    if (Vector3.Distance(__instance.m_eye.position, raycastHit.point) < __instance.m_maxInteractDistance)
                    {
                        if (raycastHit.collider.GetPreferredComponent<Hoverable>() != null)
                        {
                            hover = raycastHit.collider.gameObject;
                        }
                        else if ((bool)raycastHit.collider.attachedRigidbody)
                        {
                            hover = raycastHit.collider.attachedRigidbody.gameObject;
                        }
                        else
                        {
                            hover = raycastHit.collider.gameObject;
                        }
                    }
                    break;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        public class Player_Update_DisableCollisionsWithDeadPlayers
        {
            public static void Postfix(Player __instance)
            {
                CapsuleCollider currentPlayerCollider = __instance.m_collider;
                bool currentPlayerIsDead = __instance.IsDead();
                foreach (Player player in Player.GetAllPlayers())
                {
                    bool playerIsDead = player.IsDead();
                    CapsuleCollider playerCollider = player.m_collider;
                    if (currentPlayerCollider != null && playerCollider != null)
                    {
                        Physics.IgnoreCollision(currentPlayerCollider, playerCollider, playerIsDead || currentPlayerIsDead);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        public class Player_OnSpawned_ClearRevivePoint
        {
            public static void Postfix(Player __instance)
            {
                ReviveAllies.logger.LogInfo("Spawned, clearing revive point.");
                RevivePoint revivePoint = __instance.GetRevivePoint();
                if (revivePoint == null) {
                    return;
                }
                ClientRespawnManager.isUsingRevivePoint = false;
                revivePoint.ClearRevivee();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        public class PatchOnDeathDoNotRequestRespawn
        {
            public static bool Prefix(Player __instance)
            {
                if (!__instance.m_nview.IsOwner())
                {
                    Debug.Log("OnDeath call but not the owner");
                    return false;
                }
                bool flag = __instance.HardDeath();
                __instance.m_nview.GetZDO().Set(ZDOVars.s_dead, value: true);
                __instance.m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath");
                Game.instance.IncrementPlayerStat(PlayerStatType.Deaths);
                switch (__instance.m_lastHit.m_hitType)
                {
                    case HitData.HitType.Undefined:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByUndefined);
                        break;
                    case HitData.HitType.EnemyHit:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEnemyHit);
                        break;
                    case HitData.HitType.PlayerHit:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPlayerHit);
                        break;
                    case HitData.HitType.Fall:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFall);
                        break;
                    case HitData.HitType.Drowning:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByDrowning);
                        break;
                    case HitData.HitType.Burning:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBurning);
                        break;
                    case HitData.HitType.Freezing:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFreezing);
                        break;
                    case HitData.HitType.Poisoned:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPoisoned);
                        break;
                    case HitData.HitType.Water:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByWater);
                        break;
                    case HitData.HitType.Smoke:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySmoke);
                        break;
                    case HitData.HitType.EdgeOfWorld:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEdgeOfWorld);
                        break;
                    case HitData.HitType.Impact:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByImpact);
                        break;
                    case HitData.HitType.Cart:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByCart);
                        break;
                    case HitData.HitType.Tree:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTree);
                        break;
                    case HitData.HitType.Self:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySelf);
                        break;
                    case HitData.HitType.Structural:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStructural);
                        break;
                    case HitData.HitType.Turret:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTurret);
                        break;
                    case HitData.HitType.Boat:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBoat);
                        break;
                    case HitData.HitType.Stalagtite:
                        Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStalagtite);
                        break;
                    default:
                        ZLog.LogWarning("Not implemented death type " + __instance.m_lastHit.m_hitType);
                        break;
                }
                Game.instance.GetPlayerProfile().SetDeathPoint(__instance.transform.position);
                __instance.CreateDeathEffects();
                __instance.CreateTombStone();
                __instance.m_foods.Clear();
                if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathSkillsReset))
                {
                    __instance.m_skills.Clear();
                }
                else if (flag)
                {
                    __instance.m_skills.OnDeath();
                }
                __instance.m_seman.RemoveAllStatusEffects();
                __instance.m_timeSinceDeath = 0f;
                if (!flag)
                {
                    __instance.Message(MessageHud.MessageType.TopLeft, "$msg_softdeath");
                }
                __instance.Message(MessageHud.MessageType.Center, "$msg_youdied");
                __instance.ShowTutorial("death");
                Minimap.instance.AddPin(__instance.transform.position, Minimap.PinType.Death, $"$hud_mapday {EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds())}", save: true, isChecked: false, 0L);
                if (__instance.m_onDeath != null)
                {
                    __instance.m_onDeath();
                }
                string eventLabel = "biome:" + __instance.GetCurrentBiome();
                Gogan.LogEvent("Game", "Death", eventLabel, 0L);
                return false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        public class PatchUpdateForceKillOnUseIfHasActiveTombStone
        {
            public static void Postfix(Player __instance)
            {
                if (!__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner())
                {
                    return;
                }
                if (__instance.TakeInput())
                {
                    if (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse"))
                    {
                        if (__instance.GetRevivePoint()?.IsValid() == true)
                        {
                            ClientRespawnManager.ForceRespawn();
                        } else {
                            ReviveAllies.logger.LogInfo("Force respawn: Active revive point not found.");
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
        public class TakeInputPatchAllowWhileDeadIfPlayerHasActiveTombStone
        {
            public static void Postfix(Player __instance, ref bool __result)
            {
                if (__instance.GetRevivePoint()?.IsValid() == true)
                {
                    __result = true;
                }
            }
        }
    }
}