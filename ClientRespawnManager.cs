﻿using UnityEngine;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class ClientRespawnManager
    {
        public static bool isForceRespawning = false;

        public static void RequestRespawnAtTombstone(TombStone tombStone)
        {
            Vector3 tombStoneLocation = tombStone.transform.position;
            ReviveAllies.logger.LogInfo(
                string.Format(
                    "___ REQUESTING RESPAWN AT LOCATION <{0}, {1}, {2}> ___",
                    tombStoneLocation.x,
                    tombStoneLocation.y,
                    tombStoneLocation.z
                )
            );
            TombStoneManager.isRespawningFromTombstone = true;
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "You are being revived.");
            Game.instance.RequestRespawn(10f);
        }

        public static void RequestForceRespawn()
        {
            if (isForceRespawning)
            {
                return;
            }
            TombStoneManager.ClearActiveTombStone();
            isForceRespawning = true;
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "You are being returned to a familiar place.");
            Game.instance.RequestRespawn(10f);
        }
    }
}
