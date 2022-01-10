using HarmonyLib;
using UnityEngine;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class Hud_Patch
    {
        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBlackScreen))]
        public class PatchUpdateBlackScreenDoNotFadeWhileTombStoneIsActive
        {
            public static bool Prefix(Hud __instance, ref Player player, ref float dt)
            {
				if (player == null || (player.IsDead() && TombStoneManager.GetActiveTombStone() == null) || player.IsTeleporting() || Game.instance.IsShuttingDown() || player.IsSleeping())
				{
					__instance.m_loadingScreen.gameObject.SetActive(value: true);
					float alpha = __instance.m_loadingScreen.alpha;
					float fadeDuration = __instance.GetFadeDuration(player);
					alpha = Mathf.MoveTowards(alpha, 1f, dt / fadeDuration);
					if (Game.instance.IsShuttingDown())
					{
						alpha = 1f;
					}
					__instance.m_loadingScreen.alpha = alpha;
					if (player != null && player.IsSleeping())
					{
						__instance.m_sleepingProgress.SetActive(value: true);
						__instance.m_loadingProgress.SetActive(value: false);
						__instance.m_teleportingProgress.SetActive(value: false);
					}
					else if (player != null && player.ShowTeleportAnimation())
					{
						__instance.m_loadingProgress.SetActive(value: false);
						__instance.m_sleepingProgress.SetActive(value: false);
						__instance.m_teleportingProgress.SetActive(value: true);
					}
					else if ((bool)Game.instance && Game.instance.WaitingForRespawn())
					{
						if (!__instance.m_haveSetupLoadScreen)
						{
							__instance.m_haveSetupLoadScreen = true;
							if (__instance.m_useRandomImages)
							{
								string text = string.Concat(str2: UnityEngine.Random.Range(0, __instance.m_loadingImages).ToString(), str0: __instance.m_loadingImagePath, str1: "loading");
								ZLog.Log("Loading image:" + text);
								__instance.m_loadingImage.sprite = Resources.Load<Sprite>(text);
							}
							string text2 = __instance.m_loadingTips[UnityEngine.Random.Range(0, __instance.m_loadingTips.Count)];
							ZLog.Log("tip:" + text2);
							__instance.m_loadingTip.text = Localization.instance.Localize(text2);
						}
						__instance.m_loadingProgress.SetActive(value: true);
						__instance.m_sleepingProgress.SetActive(value: false);
						__instance.m_teleportingProgress.SetActive(value: false);
					}
					else
					{
						__instance.m_loadingProgress.SetActive(value: false);
						__instance.m_sleepingProgress.SetActive(value: false);
						__instance.m_teleportingProgress.SetActive(value: false);
					}
				}
				else
				{
					__instance.m_haveSetupLoadScreen = false;
					float fadeDuration2 = __instance.GetFadeDuration(player);
					float alpha2 = __instance.m_loadingScreen.alpha;
					alpha2 = Mathf.MoveTowards(alpha2, 0f, dt / fadeDuration2);
					__instance.m_loadingScreen.alpha = alpha2;
					if (__instance.m_loadingScreen.alpha <= 0f)
					{
						__instance.m_loadingScreen.gameObject.SetActive(value: false);
					}
				}
				return false;
			}
        }
    }
}
