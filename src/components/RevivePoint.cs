using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using System.Security.Cryptography;

namespace Ratzu.Valheim.ReviveAllies
{
    public class RevivePoint : MonoBehaviour, Interactable, Hoverable
    {
        const float DESTROY_DELAY = 2f;
        const string ZDO_ACCESSOR_CREATION_TIME = "revive_point_creation_time";
        public const string ZDO_ACCESSOR_REVIVEE_ID = "revive_point_revivee_id";

        private static List<RevivePoint> revivePoints = new List<RevivePoint>();

        protected ZNetView m_nview;

        void Awake()
        {
            m_nview = GetComponent<ZNetView>();
            TombStone tombStone = GetComponent<TombStone>();
            revivePoints.Add(this);
            InvokeRepeating(nameof(UpdateDespawn), DESTROY_DELAY, DESTROY_DELAY);
        }

        void Update()
        {
        }

        void UpdateDespawn()
        {
            if (m_nview?.IsValid() != true)
            {
                return;
            }
            if (!IsValid())
            {
                ReviveAllies.logger.LogInfo("RevivePoint is not valid, destroying.");
                ReviveAllies.logger.LogInfo(string.Format("RevivePoint {0} destroyed.", GetZDOID()));
                revivePoints.Remove(this);
                Destroy(this);
            }
        }
        public void ClearRevivee()
        {
            m_nview?.GetZDO()?.Set(ZDO_ACCESSOR_REVIVEE_ID, 0);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (user is Player player)
            {
                if (player == null || player.GetPlayerID() == GetRevivee()?.GetPlayerID())
                {
                    return false;
                }
                ReviveRevivee();
                return true;
            }
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverText()
        {
            if (m_nview?.IsValid() != true)
            {
                return "";
            }
            return Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] Revive " + (GetRevivee()?.GetPlayerName() ?? ""));
        }

        public string GetHoverName()
        {
            return "";
        }

        public void Setup(Player revivee)
        {
            ReviveAllies.logger.LogInfo("RevivePoint Setup: Setting up revive point.");
            if (m_nview?.IsValid() != true)
            {
                ReviveAllies.logger.LogInfo("RevivePoint Setup: Failed, NView invalid.");
                return;
            }
            SetCreationTime(ZNet.instance.GetTime().Ticks);
            SetRevivee(revivee);
        }

        public long GetReviveeID()
        {
            return m_nview?.GetZDO()?.GetLong(ZDO_ACCESSOR_REVIVEE_ID) ?? 0;
        }

        public Player GetRevivee()
        {
            return Player.GetAllPlayers().Find(player => player.GetPlayerID() == GetReviveeID());
        }

        private void SetRevivee(Player revivee)
        {
            ReviveAllies.logger.LogInfo("RevivePoint Setup: Revivee set.");
            m_nview?.GetZDO()?.Set(ZDO_ACCESSOR_REVIVEE_ID, revivee.GetPlayerID());
        }

        public ZDOID GetZDOID()
        {
            if (m_nview?.IsValid() != true)
            {
                return ZDOID.None;
            }
            return m_nview?.GetZDO()?.m_uid ?? ZDOID.None;
        }

        long? GetCreationTime()
        {
            if (m_nview?.IsValid() != true)
            {
                return null;
            }
            return m_nview?.GetZDO()?.GetLong(ZDO_ACCESSOR_CREATION_TIME);
        }

        void SetCreationTime(long creationTime)
        {
            m_nview?.GetZDO()?.Set(ZDO_ACCESSOR_CREATION_TIME, creationTime);
        }

        void ReviveRevivee()
        {
            if (m_nview?.IsValid() != true)
            {
                return;
            }
            ReviveAllies.logger.LogInfo("Reviving player.");
            Player revivee = GetRevivee();
            revivee?.m_nview?.InvokeRPC("Revive");
        }

        bool IsWithinReviveWindow()
        {
            return ZNet.instance.GetTime().Ticks <= GetCreationTime() + ReviveAllies.reviveWindowInTicks;
        }

        public bool IsValid()
        {
            if (GetReviveeID() == 0L)
            {
                ReviveAllies.logger.LogInfo("RevivePoint Invalid: No revivee.");
                return false;
            }
            if (!IsWithinReviveWindow())
            {
                ReviveAllies.logger.LogInfo("RevivePoint Invalid: Outside revive window.");
                return false;
            }
            return true;
        }

        public static RevivePoint GetRevivePoint(Player player)
        {
            ReviveAllies.logger.LogInfo("Getting revive point for player.");
            foreach (RevivePoint revivePoint in revivePoints)
            {
                ZDOID revivePointID = revivePoint.GetZDOID();
                long playerID = player.GetPlayerID();
                ReviveAllies.logger.LogInfo(string.Format("Found revive point {0} for player {1}.", revivePointID, playerID));
            }
            return revivePoints.Find(r => r.GetReviveeID() == player.GetPlayerID());
        }

        public static RevivePoint GetRevivePoint(PlayerProfile profile)
        {
            ReviveAllies.logger.LogInfo("Getting revive point for player profile.");
            foreach (RevivePoint revivePoint in revivePoints)
            {
                ZDOID revivePointID = revivePoint.GetZDOID();
                long playerID = profile.GetPlayerID();
                ReviveAllies.logger.LogInfo(string.Format("Found revive point {0} for player {1}.", revivePointID, playerID));
            }
            return revivePoints.Find(r => r.GetReviveeID() == profile.GetPlayerID());
        }
    }
}

