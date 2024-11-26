using HarmonyLib;
using UnityEngine;

namespace Ratzu.Valheim.ReviveAllies
{
	public static class Hud_Patch
	{

		[HarmonyPatch(typeof(Hud), nameof(Hud.GetFadeDuration))]
		public class PatchGetFadeDurationFadeAtRespawnSpeed
		{
			static void Postfix(ref float __result)
			{
				if (Player.m_localPlayer != null && Player.m_localPlayer.IsDead() && TombStoneManager.GetActiveTombStone() != null)
				{
					__result = 60f;
				}
			}
		}
	}
}
