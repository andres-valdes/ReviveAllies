using HarmonyLib;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class Character_Patch
    {
        // Force respawn after timeout
        
        // [HarmonyPatch(typeof(Character), nameof(Character.CheckDeath))]
        // public class PatchCheckDeathCheckRespawnTimeout
        // {
        //     public static void Postfix(Character __instance)
        //     {
        //         if ( __instance is Player player)
        //         {
        //             if (!player.IsDead())
        //             {
        //                 return;
        //             }
        //             RevivePoint revivePoint = player.GetRevivePoint();
        //             if (revivePoint == null || !revivePoint.IsValid())
        //             {
        //                 ClientRespawnManager.ForceRespawn();
        //             }
        //         }
        //     }
        // }
    }
}
