﻿using HarmonyLib;
using UnityEngine;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class Game_Patch
    {
        [HarmonyPatch(typeof(Game), nameof(Game.Shutdown))]
        public static class PatchShutdownClearActiveTombStone
        {
            private static void Postfix(Game __instance)
            {
                ReviveAllies.logger.LogInfo("Shutting down, clearing revive point.");
                RevivePoint revivePoint = Player.m_localPlayer.GetRevivePoint();
                Component.Destroy(revivePoint);
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.FindSpawnPoint))]
        public static class PatchFindSpawnPointUseRevivePoint
        {
            public static bool Prefix(Game __instance, ref bool __result, ref Vector3 point, ref bool usedLogoutPoint, ref float dt)
            {
                __instance.m_respawnWait += dt;
                usedLogoutPoint = false;
                if (!__instance.m_respawnAfterDeath && __instance.m_playerProfile.HaveLogoutPoint())
                {
                    Vector3 logoutPoint = __instance.m_playerProfile.GetLogoutPoint();
                    ZNet.instance.SetReferencePosition(logoutPoint);
                    if (__instance.m_respawnWait > __instance.m_respawnLoadDuration && ZNetScene.instance.IsAreaReady(logoutPoint))
                    {
                        if (!ZoneSystem.instance.GetGroundHeight(logoutPoint, out var height))
                        {
                            Vector3 vector = logoutPoint;
                            ZLog.Log("Invalid spawn point, no ground " + vector.ToString());
                            __instance.m_respawnWait = 0f;
                            __instance.m_playerProfile.ClearLoguoutPoint();
                            point = Vector3.zero;
                            __result = false;
                            return false;
                        }

                        __instance.m_playerProfile.ClearLoguoutPoint();
                        point = logoutPoint;
                        if (point.y < height)
                        {
                            point.y = height;
                        }

                        point.y += 0.25f;
                        usedLogoutPoint = true;
                        ZLog.Log("Spawned after " + __instance.m_respawnWait);
                        __result = true;
                        return false;
                    }

                    point = Vector3.zero;
                    return false;
                }

                if (ClientRespawnManager.isUsingRevivePoint) {
                    ReviveAllies.logger.LogInfo("Respawning at RevivePoint.");
                    RevivePoint revivePoint = RevivePoint.GetRevivePoint(__instance.GetPlayerProfile());
                    if (revivePoint != null && revivePoint.IsValid())
                    {
                        ReviveAllies.logger.LogInfo(string.Format("Found valid RevivePoint, respawning at <{0}, {1}, {2}>", revivePoint.transform.position.x, revivePoint.transform.position.y, revivePoint.transform.position.z));
                        point = revivePoint.transform.position;
                        ZNet.instance.SetReferencePosition(point);
                        if (__instance.m_respawnWait > __instance.m_respawnLoadDuration && ZNetScene.instance.IsAreaReady(point))
                        {
                            __result = true;
                            return false;
                        }

                        point = Vector3.zero;
                        __result = false;
                        return false;
                    } else {
                        ReviveAllies.logger.LogInfo("RevivePoint is invalid, falling back to default respawn behavior.");
                    }
                }

                if (__instance.m_playerProfile.HaveCustomSpawnPoint())
                {
                    Vector3 customSpawnPoint = __instance.m_playerProfile.GetCustomSpawnPoint();
                    ZNet.instance.SetReferencePosition(customSpawnPoint);
                    if (__instance.m_respawnWait > __instance.m_respawnLoadDuration && ZNetScene.instance.IsAreaReady(customSpawnPoint))
                    {
                        Bed bed = __instance.FindBedNearby(customSpawnPoint, 5f);
                        if (bed != null)
                        {
                            ZLog.Log("Found bed at custom spawn point");
                            point = bed.GetSpawnPoint();
                            __result = true;
                            return true;
                        }

                        ZLog.Log("Failed to find bed at custom spawn point, using original");
                        __instance.m_playerProfile.ClearCustomSpawnPoint();
                        __instance.m_respawnWait = 0f;
                        point = Vector3.zero;
                        __result = false;
                        return false;
                    }

                    point = Vector3.zero;
                    __result = false;
                    return false;
                }

                if (ZoneSystem.instance.GetLocationIcon(__instance.m_StartLocation, out var pos))
                {
                    point = pos + Vector3.up * 2f;
                    ZNet.instance.SetReferencePosition(point);
                    __result = ZNetScene.instance.IsAreaReady(point);
                    return false;
                }

                ZNet.instance.SetReferencePosition(Vector3.zero);
                point = Vector3.zero;
                __result = false;
                return false;
            }
        }
    }
}