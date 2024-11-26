using HarmonyLib;
using UnityEngine;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class Game_Patch
    {
        [HarmonyPatch(typeof(Game), nameof(Game.Shutdown))]
        public static class PatchShutdownClearActiveTombStone
        {
            private static bool Prefix(Game __instance)
            {
                TombStoneManager.ClearActiveTombStone();
                return true;
            }
        }
    }
}
