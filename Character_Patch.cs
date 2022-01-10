using HarmonyLib;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class Character_Patch
    {
        [HarmonyPatch(typeof(Character), nameof(Player.CheckDeath))]
        public class PatchCheckDeathCheckRespawnTimeout
        {
            public static bool Prefix(Character __instance)
            {
                if (__instance is Player player)
                {
                    if (player.GetPlayerID() != Game.instance.GetPlayerProfile().GetPlayerID())
                    {
                        return true;
                    }
                    TombStone activeTombstone = TombStoneManager.GetActiveTombStone();
                    if (activeTombstone == null)
                    {
                        return true;
                    }
                    if (!activeTombstone.IsWithinReviveWindow())
                    {
                        ReviveAllies.logger.LogInfo("___ REVIVE WINDOW TIMED OUT, RESPAWNING AT DEFAULT LOCATION ___");
                        ClientRespawnManager.RequestForceRespawn();
                    }
                }
                return true;
            }
        }
    }
}
