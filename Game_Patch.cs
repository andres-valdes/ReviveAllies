using HarmonyLib;
using UnityEngine;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class Game_Patch
    {
        [HarmonyPatch(typeof(Game), nameof(Game.RequestRespawn))]
        public static class PatchRequestRespawnClearActiveTombStone
        {
            private static bool Prefix(Game __instance)
			{
				TombStoneManager.ClearActiveTombStone();
                return true;
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.Shutdown))]
        public static class PatchShutdownClearActiveTombStone
        {
            private static bool Prefix(Game __instance)
            {
                TombStoneManager.ClearActiveTombStone();
                return true;
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.FindSpawnPoint))]
        public class PatchFindSpawnPointUseOverridenNextSpawnLocation
        {
            public static bool Prefix(Game __instance, ref Vector3 point, ref bool usedLogoutPoint, ref float dt, ref bool __result)
			{
				__instance.m_respawnWait += dt;
				usedLogoutPoint = false;
				if (__instance.m_playerProfile.HaveLogoutPoint())
				{
					Vector3 logoutPoint = __instance.m_playerProfile.GetLogoutPoint();
					ZNet.instance.SetReferencePosition(logoutPoint);
					if (__instance.m_respawnWait > 8f && ZNetScene.instance.IsAreaReady(logoutPoint))
					{
						if (!ZoneSystem.instance.GetGroundHeight(logoutPoint, out var height))
						{
							ZLog.Log("Invalid spawn point, no ground " + logoutPoint);
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
					__result = false;
					return false;
				}
				if (ClientRespawnManager.OverridenNextSpawnLocation != null)
				{
					Vector3 nextSpawnPoint = (Vector3) ClientRespawnManager.OverridenNextSpawnLocation;
					ReviveAllies.logger.LogInfo(string.Format("=== ATTEMPTING TO SPAWN ON TOMB STONE, LOCATION: <{0}, {1}, {2}> ===", nextSpawnPoint.x, nextSpawnPoint.y, nextSpawnPoint.z));
					ZNet.instance.SetReferencePosition(nextSpawnPoint);
					if (__instance.m_respawnWait > 8f && ZNetScene.instance.IsAreaReady(nextSpawnPoint))
                    {
						point = nextSpawnPoint;
						ClientRespawnManager.OverridenNextSpawnLocation = null;
						__result = true;
						return false;
					}
					point = Vector3.zero;
					__result = false;
					return false;

				}
				if (__instance.m_playerProfile.HaveCustomSpawnPoint())
				{
					Vector3 customSpawnPoint = __instance.m_playerProfile.GetCustomSpawnPoint();
					ZNet.instance.SetReferencePosition(customSpawnPoint);
					if (__instance.m_respawnWait > 8f && ZNetScene.instance.IsAreaReady(customSpawnPoint))
					{
						Bed bed = __instance.FindBedNearby(customSpawnPoint, 5f);
						if (bed != null)
						{
							ZLog.Log("Found bed at custom spawn point");
							point = bed.GetSpawnPoint();
							__result = true;
							return false;
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
