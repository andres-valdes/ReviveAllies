using UnityEngine;
using Ratzu.Valheim.ReviveAllies;
public class RevivePoint : MonoBehaviour, Interactable, Hoverable
{
    const string ZDO_ACCESSOR_CREATION_TIME = "revive_point_creation_time";
    const string ZDO_ACCESSOR_LOCATION = "revive_point_location";
    const string ZDO_ACCESSOR_REVIVEE_ID = "revive_point_revivee_id";
    const string ZDO_ACCESSOR_REVIVEE_NAME = "revive_point_revivee_name";
    const string ZDO_ACCESSOR_PLAYER_ACTIVE_REVIVE_POINT = "active_revive_point";

    protected ZNetView m_nview;

    void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        if (m_nview.IsOwner() && m_nview.GetZDO().GetLong(ZDO_ACCESSOR_CREATION_TIME, 0L) == 0L)
        {
            SetCreationTime(ZNet.instance.GetTime().Ticks);
            SetLocation(transform.position);
        }
    }

    void Update()
    {
        if (!IsValid())
        {
            Destroy(this);
        }
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        Player player = user.GetComponent<Player>();
        if (player == null || player.GetPlayerID() == GetRevivee()?.GetPlayerID())
        {
            return false;
        }
        ReviveOwner();
        return true;
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
        if (m_nview?.IsValid() != true)
        {
            return;
        }
        m_nview?.GetZDO()?.Set(ZDO_ACCESSOR_REVIVEE_ID, revivee.GetPlayerID());
        m_nview?.GetZDO()?.Set(ZDO_ACCESSOR_REVIVEE_NAME, revivee.GetPlayerName());
        revivee?.m_nview?.GetZDO()?.Set(ZDO_ACCESSOR_PLAYER_ACTIVE_REVIVE_POINT, m_nview?.GetZDO()?.m_uid ?? ZDOID.None);
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

    void SetLocation(Vector3 location)
    {
        m_nview?.GetZDO()?.Set(ZDO_ACCESSOR_LOCATION, location);
    }

    void ReviveOwner()
    {
        if (m_nview?.IsValid() != true)
        {
            return;
        }
        Player owner = GetRevivee();
        owner?.m_nview?.InvokeRPC("Revive", Player.m_localPlayer);
    }

    Player GetRevivee()
    {
        long reviveeID = m_nview?.GetZDO()?.GetLong(ZDO_ACCESSOR_REVIVEE_ID) ?? 0L;
        if (reviveeID == 0L)
        {
            return null;
        }
        return Player.GetPlayer(reviveeID);
    }

    bool IsReviveeActiveRevivePoint()
    {
        return GetRevivee()?.m_nview?.GetZDO()?.GetZDOID(ZDO_ACCESSOR_PLAYER_ACTIVE_REVIVE_POINT) == m_nview?.GetZDO().m_uid;
    }

    bool IsWithinReviveWindow()
    {
        return ZNet.instance.GetTime().Ticks <= GetCreationTime() + ReviveAllies.reviveWindowInTicks;
    }

    bool IsValid()
    {
        return m_nview?.IsValid() == true && IsWithinReviveWindow() && IsReviveeActiveRevivePoint();
    }
}
