using UnityEngine;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class ClientRespawnManager
    {
        public static Vector3? OverridenNextSpawnLocation { get; set; } = null;
        public static void RequestRespawnAt(Vector3 spawnLocation)
        {
            ReviveAllies.logger.LogInfo(
                string.Format(
                    "___ REQUESTING RESPAWN AT LOCATION <{0}, {1}, {2}> ___",
                    spawnLocation.x,
                    spawnLocation.y,
                    spawnLocation.z
                )
            );
            OverridenNextSpawnLocation = spawnLocation;
            Game.instance.RequestRespawn(10f);
        }

        public static void RequestForceRespawn()
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.TopLeft, "You are being returned to a familiar place.");
            Game.instance.RequestRespawn(10f);
        }
    }
}
