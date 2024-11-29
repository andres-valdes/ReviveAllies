using UnityEngine;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class ClientRespawnManager
    {
        public static bool isUsingRevivePoint = false;

        private static float lastRespawnTime = 0f;

        public static void RespawnAt(RevivePoint revivePoint)
        {
            isUsingRevivePoint = true;
            Vector3 revivePointLocation = revivePoint.transform.position;
            ReviveAllies.logger.LogInfo(
                string.Format(
                    "Respawning at",
                    revivePointLocation.x,
                    revivePointLocation.y,
                    revivePointLocation.z
                )
            );
            Game.instance.RequestRespawn(10f, true);
            revivePoint.GetComponent<TombStone>().m_container.TakeAll(Player.m_localPlayer);
        }

        public static void ForceRespawn()
        {
            if (Time.time - lastRespawnTime < 10f)
            {
                return;
            }
            ReviveAllies.logger.LogInfo("Force respawning.");
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "You are being returned to a familiar place.");
            lastRespawnTime = Time.time;
            Game.instance.RequestRespawn(10f, true);
        }
    }
}
