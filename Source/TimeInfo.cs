using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Assembly_CSharp.TasInfo.mm.Source.Extensions;
using Assembly_CSharp.TasInfo.mm.Source.Utils;
using GlobalEnums;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    internal static class TimeInfo {
        private static readonly FieldInfo TeleportingFieldInfo = typeof(CameraController).GetFieldInfo("teleporting");
        private static readonly FieldInfo TilemapDirtyFieldInfo = typeof(GameManager).GetFieldInfo("tilemapDirty");
        private static readonly FieldInfo SceneLoadFieldInfo = typeof(GameManager).GetField("sceneLoad", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool timeStart = false;
        private static bool timeEnd = false;
        private static float inGameTime = 0f;
        private static readonly int minorVersion = int.Parse(Constants.GAME_VERSION.Substring(2, 1));

        private static string FormattedTime {
            get {
                if (inGameTime == 0) {
                    return string.Empty;
                } else if (inGameTime < 60) {
                    return inGameTime.ToString("F2");
                } else if (inGameTime < 3600) {
                    int minute = (int)(inGameTime / 60);
                    float second = inGameTime - minute * 60;
                    return $"{minute}:{second.ToString("F2").PadLeft(5, '0')}";
                } else {
                    int hour = (int)(inGameTime / 3600);
                    int minute = (int)((inGameTime - hour * 3600) / 60);
                    float second = inGameTime - hour * 3600 - minute * 60;
                    return $"{hour}:{minute.ToString().PadLeft(2, '0')}:{second.ToString("F2").PadLeft(5, '0')}";
                }
            }
        }

        private static GameState lastGameState = GameState.INACTIVE;
        private static bool lookForTeleporting = false;
        private static bool wasLoading = false;
        private static bool mmsRoomDupe = false;

        public static void OnPreRender(GameManager gameManager, StringBuilder infoBuilder) {
            string currentScene = gameManager.sceneName;
            string nextScene = gameManager.nextSceneName;
            GameState gameState = gameManager.GameState;

            //TODO: Determine end logic based on autosplitter
            bool sceneLoadActivationAllowed = false;
            object sceneLoad = SceneLoadFieldInfo.GetValue(gameManager);
            if (sceneLoad == null) {
                sceneLoadActivationAllowed = true;
            }
            else {
                PropertyInfo activationAllowedProp = sceneLoad.GetType().GetProperty("IsActivationAllowed");
                
                if (activationAllowedProp != null) {
                    sceneLoadActivationAllowed = (bool)activationAllowedProp.GetValue(sceneLoad);
                }
            }
            
            if (!timeStart && nextScene == "Tut_01" && sceneLoadActivationAllowed) {
                // StartNewGame
                timeStart = true;
                inGameTime = ConfigManager.StartingGameTime;
            }

            // if (timeStart && !timeEnd && ) {
            //     timeEnd = true;
            // }

            bool timePaused = false;
            
            // migrated from https://github.com/AlexKnauth/silksong-autosplit-wasm/blob/master/src/lib.rs
            try {
                bool loadingMenu = (currentScene == "Quit_To_Menu")
                    || (currentScene != "Menu_Title" && (string.IsNullOrEmpty(nextScene)
                    || nextScene == "Menu_Title"));
                if (gameState == GameState.PLAYING && lastGameState == GameState.MAIN_MENU) {
                    lookForTeleporting = true;
                }
                if (lookForTeleporting && (gameState != GameState.PLAYING && gameState != GameState.ENTERING_LEVEL)) {
                    lookForTeleporting = false;
                }
                if (gameState == GameState.LOADING && lastGameState == GameState.CUTSCENE && currentScene == "Opening_Sequence") {
                    mmsRoomDupe = true;
                }
                else if (gameState == GameState.PLAYING) {
                    mmsRoomDupe = false;
                }
                bool acceptingInput = gameManager.inputHandler.acceptingInput;
                HeroTransitionState heroTransitionState = gameManager.hero_ctrl?.transitionState ?? HeroTransitionState.WAITING_TO_TRANSITION;
                UIState uiState = gameManager.ui.uiState;
                bool sceneLoadNull = (SceneLoadFieldInfo.GetValue(gameManager) == null);

                timePaused = (lookForTeleporting)
                    || ((gameState == GameState.PLAYING || gameState == GameState.ENTERING_LEVEL) && uiState != UIState.PLAYING)
                    || (gameState != GameState.PLAYING && gameState != GameState.CUTSCENE && !acceptingInput && !mmsRoomDupe)
                    || ((gameState == GameState.EXITING_LEVEL && (sceneLoadNull || sceneLoadActivationAllowed) && !mmsRoomDupe) || gameState == GameState.LOADING)
                    || (heroTransitionState == HeroTransitionState.WAITING_TO_ENTER_LEVEL)
                    || (uiState != UIState.PLAYING && (loadingMenu || (uiState != UIState.PAUSED && uiState != UIState.CUTSCENE && !string.IsNullOrEmpty(nextScene))) && nextScene != currentScene);
            } catch {
                // ignore
            }

            lastGameState = gameState;

            if (timeStart && !timePaused && !timeEnd) {
                inGameTime += Time.unscaledDeltaTime;
            }

            List<string> result = new();
            if (!string.IsNullOrEmpty(gameManager.sceneName) && ConfigManager.ShowSceneName) {
                result.Add(gameManager.sceneName);
            }

            if (inGameTime > 0 && ConfigManager.ShowTime) {
                result.Add(FormattedTime);
            }

            var isLoading = gameState == GameState.EXITING_LEVEL ||
                                            gameState == GameState.ENTERING_LEVEL ||
                                            gameState == GameState.LOADING;
            var infoFlags = TasInfoFlags.None;
            if (ConfigManager.DisableFFDuringLoads) {
                if (isLoading && !wasLoading) {
                    infoFlags |= TasInfoFlags.SetFFUnsafe;
                } else if (!isLoading && wasLoading) {
                    infoFlags |= TasInfoFlags.SetFFSafe;
                }

                if (isLoading) {
                    infoFlags |= TasInfoFlags.IsFFUnsafe;
                }
            }
            wasLoading = isLoading;
            patch_GameManager.InfoFlags = (int)infoFlags;

            if (ConfigManager.ShowUnscaledTime) {
                var utime = TimeSpan.FromSeconds(Time.unscaledTime);
                var hours = utime.Hours > 0 ? $"{utime.Hours:00}:" : "";
                var minutes = utime.Minutes > 0 ? $"{utime.Minutes:00}:" : "";
                result.Add($"UT {hours}{minutes}{utime.Seconds:00}.{utime.Milliseconds:000}");
            }

            string resultString = StringUtils.Join("  ", result);
            if (!string.IsNullOrEmpty(resultString)) {
                infoBuilder.AppendLine(resultString);
            }

            if (ConfigManager.ShowTimeMinusFixedTime) {
                infoBuilder.AppendLine($"T-FT {1000*(Time.time - Time.fixedTime):00.0000} ms");
            }
        }
    }
}