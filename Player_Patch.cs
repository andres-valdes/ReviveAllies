using HarmonyLib;
using UnityEngine;
using System;

namespace Ratzu.Valheim.ReviveAllies
{
    public static class Player_Patch
    {
		[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
		public class PatchPlayerAwakeRegisterCollisionRPCsAndCheckCollisions
        {
			public static void Postfix(Player __instance)
            {
				CapsuleCollider localCollider = Player.m_localPlayer?.m_collider;
				CapsuleCollider instanceCollider = __instance.m_collider;
				if (localCollider != null && instanceCollider != null)
                {
					Physics.IgnoreCollision(
						localCollider, 
						instanceCollider, 
						(Player.m_localPlayer?.m_nview?.GetZDO()?.GetBool("dead") ?? false)
							|| (__instance.m_nview?.GetZDO()?.GetBool("dead") ?? false)
					);
                }
				__instance.m_nview?.Register("OnSpawn", (long sender) => __instance.RPC_OnSpawn(sender));
			}
        }

		[HarmonyPatch(typeof(Player), nameof(Player.FindHoverObject))]
		public class PatchIgnoreDeadPlayersOnHover
		{
			public static bool Prefix(Player __instance, ref GameObject hover, ref Character hoverCreature)
			{
				hover = null;
				hoverCreature = null;
				RaycastHit[] array = Physics.RaycastAll(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, 50f, __instance.m_interactMask);
				Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
				RaycastHit[] array2 = array;
				for (int i = 0; i < array2.Length; i++)
				{
					RaycastHit raycastHit = array2[i];
					if ((bool)raycastHit.collider.attachedRigidbody && raycastHit.collider.attachedRigidbody.gameObject == __instance.gameObject)
					{
						continue;
					}
					if (hoverCreature == null)
					{
						Character character = (raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>());
						if (character != null && (!character.GetBaseAI() || !character.GetBaseAI().IsSleeping()))
						{
							hoverCreature = character;
						}
					}
					if (raycastHit.collider.GetComponent<Player>()?.m_nview?.GetZDO()?.GetBool("dead") ?? false)
                    {
						continue;
                    }
					if (Vector3.Distance(__instance.m_eye.position, raycastHit.point) < __instance.m_maxInteractDistance)
					{
						if (raycastHit.collider.GetComponent<Hoverable>() != null)
						{
							hover = raycastHit.collider.gameObject;
						}
						else if ((bool)raycastHit.collider.attachedRigidbody)
						{
							hover = raycastHit.collider.attachedRigidbody.gameObject;
						}
						else
						{
							hover = raycastHit.collider.gameObject;
						}
					}
					break;
				}
				return false;
			}
		}

		[HarmonyPatch(typeof(Player), nameof(Player.RPC_OnDeath))]
		public class PatchRPC_OnDeathDisableCollisionsOnDeath
		{
			public static void Postfix(Player __instance)
			{
				CapsuleCollider localPlayerCollider = Player.m_localPlayer?.m_collider;
				CapsuleCollider deadPlayerCollider = __instance.m_collider;
				if (localPlayerCollider != null && deadPlayerCollider != null)
				{
					Physics.IgnoreCollision(localPlayerCollider, deadPlayerCollider, true);
				}
			}
		}

		[HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
		public class PatchOnSpawnedSendRPC_OnSpawn
		{
			public static void Postfix(Player __instance)
			{
				__instance?.m_nview?.InvokeRPC("OnSpawn");
			}
		}

		public static void RPC_OnSpawn(this Player spawnedPlayer, long sender)
        {
			CapsuleCollider localPlayerCollider = Player.m_localPlayer?.m_collider;
			CapsuleCollider spawnedPlayerCollider = spawnedPlayer.m_collider;
			if (localPlayerCollider != null && spawnedPlayerCollider != null)
			{
                Physics.IgnoreCollision(
					localPlayerCollider,
					spawnedPlayerCollider,
                    false
                );
            }
		}
        

		[HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        public class PatchOnDeathDoNotRequestRespawn
        {
            public static bool Prefix(Player __instance)
            {
                bool num = __instance.HardDeath();
                __instance.m_nview.GetZDO().Set("dead", value: true);
                __instance.m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath");
                Game.instance.GetPlayerProfile().m_playerStats.m_deaths++;
                Game.instance.GetPlayerProfile().SetDeathPoint(__instance.transform.position);
                __instance.CreateDeathEffects();
                __instance.CreateTombStone();
                __instance.GetFoods().Clear();
                if (num)
                {
                    __instance.m_skills.OnDeath();
                }
                __instance.m_seman.RemoveAllStatusEffects();
                if (!num)
                {
                    __instance.Message(MessageHud.MessageType.TopLeft, "$msg_softdeath");
                }
                __instance.Message(MessageHud.MessageType.Center, "$msg_youdied");
				__instance.Message(MessageHud.MessageType.TopLeft, "You can wait to be revived at your grave or press [$KEY_Use] to give up.");
				__instance.ShowTutorial("death");
                Minimap.instance.AddPin(__instance.transform.position, Minimap.PinType.Death, $"$hud_mapday {EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds())}", save: true, isChecked: false, 0L);
                if (__instance.m_onDeath != null)
                {
                    __instance.m_onDeath();
                }
                string eventLabel = "biome:" + __instance.GetCurrentBiome();
                Gogan.LogEvent("Game", "Death", eventLabel, 0L);
				CapsuleCollider deadPlayerCollider = __instance.m_collider; 
				foreach (Player player in ZNetScene.FindObjectsOfType<Player>())
                {
					CapsuleCollider playerCollider = player.m_collider;
					Physics.IgnoreCollision(deadPlayerCollider, playerCollider, true);
				}
				return false;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        public class PatchUpdateForceKillOnUseIfHasActiveTombStone
        {
            public static bool Prefix(Player __instance)
			{
                if (!__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner())
                {
                    return false;
                }
                bool flag = __instance.TakeInput();
				__instance.UpdateHover();
				if (flag)
				{
					if (Player.m_debugMode && Console.instance.IsCheatsEnabled())
					{
						if (Input.GetKeyDown(KeyCode.Z))
						{
							__instance.ToggleDebugFly();
						}
						if (Input.GetKeyDown(KeyCode.B))
						{
							__instance.ToggleNoPlacementCost();
						}
						if (Input.GetKeyDown(KeyCode.K))
						{
							Console.instance.TryRunCommand("killall");
						}
						if (Input.GetKeyDown(KeyCode.L))
						{
							Console.instance.TryRunCommand("removedrops");
						}
					}
					if (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse"))
					{
						bool alt = ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
						if ((bool)__instance.m_hovering)
						{
							__instance.Interact(__instance.m_hovering, hold: false, alt);
						}
						else if (TombStoneManager.GetActiveTombStone() != null)
                        {
							ReviveAllies.logger.LogInfo("___ KILLING SELF ___");
							ClientRespawnManager.RequestForceRespawn();
                        } else if (__instance.m_doodadController != null)
						{
							__instance.StopDoodadControl();
						}
					}
					else if (ZInput.GetButton("Use") || ZInput.GetButton("JoyUse"))
					{
						bool alt2 = ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace");
						if ((bool)__instance.m_hovering)
						{
							__instance.Interact(__instance.m_hovering, hold: true, alt2);
						}
					}
					if (ZInput.GetButtonDown("Hide") || ZInput.GetButtonDown("JoyHide"))
					{
						if (__instance.GetRightItem() != null || __instance.GetLeftItem() != null)
						{
							if (!__instance.InAttack())
							{
								__instance.HideHandItems();
							}
						}
						else if (!__instance.IsSwiming() || __instance.IsOnGround())
						{
							__instance.ShowHandItems();
						}
					}
					if (ZInput.GetButtonDown("ToggleWalk"))
					{
						__instance.SetWalk(!__instance.GetWalk());
						if (__instance.GetWalk())
						{
							__instance.Message(MessageHud.MessageType.TopLeft, "$msg_walk $hud_on");
						}
						else
						{
							__instance.Message(MessageHud.MessageType.TopLeft, "$msg_walk $hud_off");
						}
					}
					if (ZInput.GetButtonDown("Sit") || (!__instance.InPlaceMode() && ZInput.GetButtonDown("JoySit")))
					{
						if (__instance.InEmote() && __instance.IsSitting())
						{
							__instance.StopEmote();
						}
						else
						{
							__instance.StartEmote("sit", oneshot: false);
						}
					}
					if (ZInput.GetButtonDown("GPower") || (ZInput.GetButtonDown("JoyGPower") && !ZInput.GetButton("JoyLTrigger")))
					{
						__instance.StartGuardianPower();
					}
					if (ZInput.GetButtonDown("AutoPickup"))
					{
						__instance.m_enableAutoPickup = !__instance.m_enableAutoPickup;
						__instance.Message(MessageHud.MessageType.TopLeft, "$hud_autopickup:" + (__instance.m_enableAutoPickup ? "$hud_on" : "$hud_off"));
					}
					if (Input.GetKeyDown(KeyCode.Alpha1))
					{
						__instance.UseHotbarItem(1);
					}
					if (Input.GetKeyDown(KeyCode.Alpha2))
					{
						__instance.UseHotbarItem(2);
					}
					if (Input.GetKeyDown(KeyCode.Alpha3))
					{
						__instance.UseHotbarItem(3);
					}
					if (Input.GetKeyDown(KeyCode.Alpha4))
					{
						__instance.UseHotbarItem(4);
					}
					if (Input.GetKeyDown(KeyCode.Alpha5))
					{
						__instance.UseHotbarItem(5);
					}
					if (Input.GetKeyDown(KeyCode.Alpha6))
					{
						__instance.UseHotbarItem(6);
					}
					if (Input.GetKeyDown(KeyCode.Alpha7))
					{
						__instance.UseHotbarItem(7);
					}
					if (Input.GetKeyDown(KeyCode.Alpha8))
					{
						__instance.UseHotbarItem(8);
					}
				}
				__instance.UpdatePlacement(flag, Time.deltaTime);
				return false;
			}
        }

		[HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
		public class TakeInputPatchAllowWhileDeadIfPlayerHasActiveTombStone
        {
			public static bool Prefix(Player __instance, ref bool __result)
			{
				bool result = (!Chat.instance || !Chat.instance.HasFocus()) && !Console.IsVisible() && !TextInput.IsVisible() && !StoreGui.IsVisible() && !InventoryGui.IsVisible() && !Menu.IsVisible() && (!TextViewer.instance || !TextViewer.instance.IsVisible()) && !Minimap.IsOpen() && !GameCamera.InFreeFly();
				if ((__instance.IsDead() && TombStoneManager.GetActiveTombStone() == null) || __instance.InCutscene() || __instance.IsTeleporting())
				{
					result = false;
				}

				__result = result;
				return false;
			}
		}

		[HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
		public class PatchCreateTombStoneAlwaysCreate
		{
			public static bool Prefix(Player __instance)
			{
				__instance.UnequipAllItems();
				GameObject obj = UnityEngine.Object.Instantiate(__instance.m_tombstone, __instance.GetCenterPoint(), __instance.transform.rotation);
				obj.GetComponent<Container>().GetInventory().MoveInventoryToGrave(__instance.m_inventory);
				TombStone component = obj.GetComponent<TombStone>();
				PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
				component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
				return false;
			}
		}
	}
}
