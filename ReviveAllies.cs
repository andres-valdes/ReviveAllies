using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace Ratzu.Valheim.ReviveAllies
{
    [BepInPlugin(Guid, Name, Version)]
    public class ReviveAllies : BaseUnityPlugin
    {
        public const string Version = "1.0.0";
        public const string Name = "Revive Allies";
        public const string Guid = "ratzu.mods.reviveallies";
        public const string Namespace = "Ratzu.Valheim." + nameof(ReviveAllies);

        // Configuration
        private static int reviveWindowInSeconds = 60;
        public static long reviveWindowInTicks = (long)(reviveWindowInSeconds * 1000.0 * 10000.0);

        private Harmony _harmony;
        public static ManualLogSource logger;

        private void Awake()
        {
            logger = Logger;
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Guid);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}