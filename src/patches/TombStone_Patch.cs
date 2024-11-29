using HarmonyLib;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class TombStone_Patch
    {
        public static RevivePoint GetRevivePoint(this TombStone tombStone)
        {
            return tombStone.GetComponent<RevivePoint>();
        }

        [HarmonyPatch(typeof(TombStone), nameof(TombStone.UpdateDespawn))]
        public class TombStone_UpdateDespawn_CreateReviveComponentIfOwnerHasRevivePoint
        {
            public static void Postfix(TombStone __instance)
            {
                if (__instance.m_nview.IsValid() && __instance.m_nview.GetZDO().GetLong(RevivePoint.ZDO_ACCESSOR_REVIVEE_ID, 0L) != 0L && __instance.GetComponent<RevivePoint>() == null)
                {
                    __instance.gameObject.AddComponent<RevivePoint>();
                }
            }
        }

        public class TombStone_UpdateDespawn_DoNotDestroyTombStoneIfOwnerHasRevivePoint
        {
            public static bool Prefix(TombStone __instance)
            {
                if (!__instance.m_nview.IsValid())
                {
                    return false;
                }
                if (__instance.m_floater != null)
                {
                    __instance.UpdateFloater();
                }
                if (__instance.m_nview.IsOwner())
                {
                    __instance.PositionCheck();
                    if (!__instance.m_container.IsInUse() && __instance.m_container.GetInventory().NrOfItems() <= 0 && (__instance.GetComponent<RevivePoint>()?.IsValid() != true))
                    {
                        __instance.GiveBoost();
                        __instance.m_removeEffect.Create(__instance.transform.position, __instance.transform.rotation);
                        __instance.m_nview.Destroy();
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(TombStone), nameof(TombStone.Setup))]
        public class PatchSetupSetupActiveTombStone
        {
            public static void Postfix(TombStone __instance)
            {
                __instance.gameObject.AddComponent<RevivePoint>().Setup(__instance.FindOwner());
            }
        }
    }
}