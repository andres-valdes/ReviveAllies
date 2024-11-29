using HarmonyLib;
using UnityEngine;

namespace Ratzu.Valheim.ReviveAllies
{
	public static class Hud_Patch
	{
		[HarmonyPatch(typeof(Hud), nameof(Hud.UpdateCrosshair))]
		public static class Hud_UpdateCrosshair
		{
			public static bool Prefix(Hud __instance, ref Player player, ref float bowDrawPercentage)
			{
				if (player.IsAttached() && player.GetAttachCameraPoint() != null)
				{
					__instance.m_crosshair.gameObject.SetActive(value: false);
				}
				else if (!__instance.m_crosshair.gameObject.activeSelf)
				{
					__instance.m_crosshair.gameObject.SetActive(value: true);
				}
				GameObject hoverObject = player.GetHoverObject();
				Hoverable hoverable = (hoverObject ? hoverObject.GetPreferredComponentInParent<Hoverable>() : null);
				if (hoverable != null && !TextViewer.instance.IsVisible())
				{
					string text = hoverable.GetHoverText();
					if (ZInput.IsGamepadActive())
					{
						text = text.Replace("[<color=yellow><b><sprite=", "<sprite=");
						text = text.Replace("\"></b></color>]", "\">");
					}
					__instance.m_hoverName.text = text;
					__instance.m_crosshair.color = ((__instance.m_hoverName.text.Length > 0) ? Color.yellow : Hud.s_whiteHalfAlpha);
				}
				else
				{
					__instance.m_crosshair.color = Hud.s_whiteHalfAlpha;
					__instance.m_hoverName.text = "";
				}
				Piece hoveringPiece = player.GetHoveringPiece();
				if ((bool)hoveringPiece)
				{
					WearNTear component = hoveringPiece.GetComponent<WearNTear>();
					if ((bool)component)
					{
						__instance.m_pieceHealthRoot.gameObject.SetActive(value: true);
						__instance.m_pieceHealthBar.SetValue(component.GetHealthPercentage());
					}
					else
					{
						__instance.m_pieceHealthRoot.gameObject.SetActive(value: false);
					}
				}
				else
				{
					__instance.m_pieceHealthRoot.gameObject.SetActive(value: false);
				}
				if (bowDrawPercentage > 0f)
				{
					float num = Mathf.Lerp(1f, 0.15f, bowDrawPercentage);
					__instance.m_crosshairBow.gameObject.SetActive(value: true);
					__instance.m_crosshairBow.transform.localScale = new Vector3(num, num, num);
					__instance.m_crosshairBow.color = Color.Lerp(new Color(1f, 1f, 1f, 0f), Color.yellow, bowDrawPercentage);
				}
				else
				{
					__instance.m_crosshairBow.gameObject.SetActive(value: false);
				}
				return false;
			}
		}
	}
}

