namespace Ratzu.Valheim.ReviveAllies
{
    public static class TombStoneManager
    {
        public static bool isRespawningFromTombstone = false;
        private static TombStone activeTombStone;
        public static TombStone CreateTombStone(Player player)
        {
            player.UnequipAllItems();
            UnityEngine.GameObject obj = UnityEngine.Object.Instantiate(player.m_tombstone, player.GetCenterPoint(), player.transform.rotation);
            obj.GetComponent<Container>().GetInventory().MoveInventoryToGrave(player.m_inventory);
            TombStone component = obj.GetComponent<TombStone>();
            PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
            component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
            return component;
        }

        public static TombStone GetActiveTombStone()
        {
            return activeTombStone;
        }

        public static void SetActiveTombStone(TombStone tombStone)
        {
            ClearActiveTombStone();
            activeTombStone = tombStone;
            tombStone.m_nview.GetZDO().Set("is_active_tombstone", true);
            ReviveAllies.logger.LogInfo("___ ACTIVE TOMBSTONE SET ___");
        }

        public static void ClearActiveTombStone()
        {
            if (activeTombStone == null)
            {
                return;
            }
            if (isRespawningFromTombstone) {
                activeTombStone.m_container.TakeAll(Player.m_localPlayer);
                isRespawningFromTombstone = false;
            }
            TombStone tombStoneToUnset = activeTombStone;
            activeTombStone = null;
            tombStoneToUnset.m_nview.GetZDO().Set("is_active_tombstone", false);
            ReviveAllies.logger.LogInfo("___ ACTIVE TOMBSTONE CLEARED ___");
        }
    }
}
