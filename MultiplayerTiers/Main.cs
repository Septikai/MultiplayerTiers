using BepInEx;
using Epic.OnlineServices.Lobby;
using HarmonyLib;
using HarmonyLib.Tools;
using Interfaces;
using UnityEngine;

namespace MultiplayerTiers
{
    [BepInPlugin(ModName, ModGUID, ModVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string ModName = "MultiplayerTiers";
        public const string ModAuthor = "Septikai";
        public const string ModVersion = "1.0.0";
        private const string ModGUID = "me.septikai.MultiplayerTiers";
        internal Harmony Harmony;

        internal void Awake()
        {
            // Creating new harmony instance
            Harmony = new Harmony(ModGUID);

            // Applying patches
            Harmony.PatchAll();
            Logger.LogInfo($"{ModName} successfully loaded! Made by {ModAuthor}");
        }
    }

    [HarmonyPatch(typeof(VersionNumberTextMesh), nameof(VersionNumberTextMesh.Start))]
    public class VersionNumberTextMeshPatch
    {
        public static void Postfix(VersionNumberTextMesh __instance)
        {
            __instance.textMesh.text += $"\n<color=red>{Main.ModName} v{Main.ModVersion} by {Main.ModAuthor}</color>";
        }
    }

    [HarmonyPatch(typeof(HudController), nameof(HudController.StartTier))]
    public class StartTierPatch
    {
        [HarmonyDebug]
        [HarmonyPostfix]
        public static void SetMaxPlayers(HudController __instance)
        {
            LobbyController.instance.SetMaxPlayer(4);
        }
    }

    [HarmonyPatch(typeof(GameModeStartButton), nameof(GameModeStartButton.FixedUpdate))]
    public class GameModeStartButtonTextPatch
    {
        private static int _playersOnButton;
        
        [HarmonyDebug]
        [HarmonyPrefix]
        public static void PlayersOnButton(GameModeStartButton __instance)
        {
            _playersOnButton = __instance._playersOnButton;
        }
        
        [HarmonyDebug]
        [HarmonyPostfix]
        public static void DisplayFloatingText(GameModeStartButton __instance)
        {
            var playerCount = LobbyController.instance.GetPlayerCount();
            if (__instance.targetMode == GameModeStartButton.Mode.TiersOfHeck &&
                playerCount > 1)
            {
                if (_playersOnButton > 0 && _playersOnButton < playerCount)
                {
                    __instance.floatingText.enabled = true;
                    __instance.floatingText.text = __instance._waitingForOtherPlayersString;
                    __instance.floatingText.color = Color.white;
                }
                else __instance.floatingText.enabled = false;
                
                if (!__instance._inCountdown && _playersOnButton == playerCount &&
                    __instance.targetMode != GameModeStartButton.Mode.StartGame)
                {
                    __instance.StartCountDown();
                }
            }

            _playersOnButton = 0;
        }
    }

    [HarmonyPatch(typeof(GameModeStartButton), nameof(GameModeStartButton.StartCountDown))]
    public class GameModeStartButtonCountdownPatch
    {
        [HarmonyDebug]
        [HarmonyPostfix]
        public static void StartCountDown(GameModeStartButton __instance)
        {
            var playerCount = LobbyController.instance.GetPlayerCount();
            if ((__instance.targetMode != GameModeStartButton.Mode.StartGame || !VersusMode.instance.GameModeActive() || playerCount >= 2) &&
                (__instance.targetMode == GameModeStartButton.Mode.TiersOfHeck && playerCount >= 1))
            {
                __instance._inCountdown = true;
                __instance.TurnOnFirstLights();
                __instance.Invoke(nameof(GameModeStartButton.TurnOnSecondLights), 1f);
                __instance.Invoke(nameof(GameModeStartButton.TurnOnFinalLights), 2f);
                __instance.Invoke(nameof(GameModeStartButton.ShowGameModePrompt), 3f);
            }
        }
    }
}
