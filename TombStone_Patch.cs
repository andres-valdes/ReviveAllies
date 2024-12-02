using HarmonyLib;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class TombStone_Patch
    {
        [HarmonyPatch(typeof(TombStone), nameof(TombStone.Start))]
        public static class PatchTombStoneStartRegisterReviveRPC
        {
            private static bool Prefix(TombStone __instance)
            {
                __instance.m_nview = __instance.GetComponent<ZNetView>();
                __instance.m_nview?.Register("ReviveOwner", __instance.RPC_Revive);
                return true;
            }
        }

        public static bool IsWithinReviveWindow(this TombStone tombStone)
        {
            return ZNet.instance.GetTime().Ticks <= tombStone.m_nview.GetZDO().GetLong("timeOfDeath") + ReviveAllies.reviveWindowInTicks;
        }

        public static bool IsRevivable(this TombStone tombStone)
        {
            return tombStone.IsWithinReviveWindow() && (tombStone.m_nview?.GetZDO()?.GetBool("is_active_tombstone") ?? false);
        }

        public static bool IsInstanceOwner(this TombStone tombStone)
        {
            long? localPlayerID = Player.m_localPlayer?.GetPlayerID();
            if (localPlayerID == null)
            {
                return false;
            }
            return localPlayerID == tombStone.FindOwner().GetPlayerID();
        }

        public static void RPC_Revive(this TombStone tombStone, long sender)
        {
            ReviveAllies.logger.LogInfo("___ REVIVING SELF: REVIVE RECEIVED ___");
            if (!tombStone.IsRevivable())
            {
                ReviveAllies.logger.LogInfo("___ REVIVING SELF: TOMBSTONE NOT REVIVABLE ___");
            }
            if (!tombStone.IsInstanceOwner())
            {
                ReviveAllies.logger.LogInfo("___ REVIVING SELF: NOT OWNER OF TOMBSTONE ___");
                return;
            }
            ReviveAllies.logger.LogInfo("___ REVIVING SELF: TOMBSTONE VALID ___");
            ClientRespawnManager.RequestRespawnAtTombstone(tombStone);
        }

        [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
        public class PatchTombStoneInteractReviveFromTombStone
        {
            public static bool Prefix(TombStone __instance, ref bool hold, ref bool __result)
            {
                if (hold)
                {
                    __result = false;
                    return false;
                }
                long? tombStoneOwnerID = __instance.FindOwner()?.GetPlayerID();
                long? localPlayerID = Player.m_localPlayer?.GetPlayerID();
                if (tombStoneOwnerID == null || localPlayerID == null || tombStoneOwnerID == localPlayerID || !__instance.IsRevivable())
                {
                    return true;
                }
                __instance.m_nview.InvokeRPC("ReviveOwner");
                Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, string.Format("You are reviving {0}", __instance.m_nview.GetZDO().GetString("ownerName")), 0, null); 
                ReviveAllies.logger.LogInfo("___ REVIVING PLAYER: REVIVE SENT ___");
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(TombStone), nameof(TombStone.UpdateDespawn))]
        public class PatchUpdateDespawnDoNotDestroyIfReviveIsActive
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
                    if (!__instance.m_container.IsInUse() && __instance.m_container.GetInventory().NrOfItems() <= 0 && !__instance.IsRevivable())
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
            public static void Prefix(TombStone __instance)
            {
                TombStoneManager.SetActiveTombStone(__instance);
            }
        }

        [HarmonyPatch(typeof(TombStone), nameof(TombStone.GetHoverText))]
        public class PatchTombStoneGetHoverTextShowRevive
        {
            public static bool Prefix(TombStone __instance, ref string __result)
            {
                long? tombStoneOwnerID = __instance.FindOwner()?.GetPlayerID();
                long? localPlayerID = Player.m_localPlayer?.GetPlayerID();
                if (tombStoneOwnerID == null || localPlayerID == null || tombStoneOwnerID == localPlayerID || !__instance.IsRevivable())
                {
                    return true;
                }
                if (!__instance.m_nview.IsValid())
                {
                    return true;
                }
                string @string = __instance.m_nview.GetZDO().GetString("ownerName");
                string text = __instance.m_text + " " + @string;
                __result = Localization.instance.Localize(text) + Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] Revive");
                return false;
            }
        }
    }
}