#nullable disable
using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using UnityEngine;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace nucleus
{
    [BepInPlugin("com.birden.nucleusunityfixes", "Nucleus Unity general fixes", "1.0.0")]
    public class UnityFixesPlugin : BasePlugin
    {
        internal static ManualLogSource Logger;

        public override void Load()
        {
            Logger = Log;

            ClassInjector.RegisterTypeInIl2Cpp<UnityFixesBehavior>();
            AddComponent<UnityFixesBehavior>();

            var harmony = new Harmony("com.birden.nucleusunityfixes");
            harmony.PatchAll(typeof(PersistentDataPathPatch));
            harmony.PatchAll(typeof(PlayerPrefsPatches));

            Log.LogInfo("Nucleus Unity Fixes inicialized.");
        }
    }

    // This is used to make our own custom save that intercepts the legit one. changing path or other stuff simply didnt work, at least in a way that would work for multiple games.
    public static class CustomPrefsManager
    {
        public class PrefsData
        {
            public Dictionary<string, int> Ints { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, float> Floats { get; set; } = new Dictionary<string, float>();
            public Dictionary<string, string> Strings { get; set; } = new Dictionary<string, string>();
        }

        private static PrefsData data = new PrefsData();
        private static string saveFilePath = "";

        public static void Initialize(string suffix)
        {
            // Create dir/load data from dir.
            string basePath = Environment.CurrentDirectory;
            string dirPath = Path.Combine(basePath, "NC_Saves", suffix);

            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            saveFilePath = Path.Combine(dirPath, "prefs.json");
            LoadFromDisk();
        }

        //read
        public static void LoadFromDisk()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(saveFilePath);
                    data = JsonSerializer.Deserialize<PrefsData>(json) ?? new PrefsData();
                }
                catch (Exception ex)
                {
                    UnityFixesPlugin.Logger.LogError($"Cant load save json: {ex.Message}");
                }
            }
        }

        //write
        public static void SaveToDisk()
        {
            if (string.IsNullOrEmpty(saveFilePath)) return;

            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception ex)
            {
                UnityFixesPlugin.Logger.LogError($"Error when saving json: {ex.Message}");
            }
        }

        //also fully AI, basically just a copy of the PlayerPrefs methodsbut intercepted for this.
        public static void SetInt(string key, int val) { data.Ints[key] = val; SaveToDisk(); }
        public static int GetInt(string key, int def) { return data.Ints.ContainsKey(key) ? data.Ints[key] : def; }

        public static void SetFloat(string key, float val) { data.Floats[key] = val; SaveToDisk(); }
        public static float GetFloat(string key, float def) { return data.Floats.ContainsKey(key) ? data.Floats[key] : def; }

        public static void SetString(string key, string val) { data.Strings[key] = val; SaveToDisk(); }
        public static string GetString(string key, string def) { return data.Strings.ContainsKey(key) ? data.Strings[key] : def; }

        public static bool HasKey(string key) { return data.Ints.ContainsKey(key) || data.Floats.ContainsKey(key) || data.Strings.ContainsKey(key); }
        public static void DeleteKey(string key) { data.Ints.Remove(key); data.Floats.Remove(key); data.Strings.Remove(key); SaveToDisk(); }
        public static void DeleteAll() { data.Ints.Clear(); data.Floats.Clear(); data.Strings.Clear(); SaveToDisk(); }
    }

    public class UnityFixesBehavior : MonoBehaviour
    {
        public UnityFixesBehavior(IntPtr ptr) : base(ptr) { }

        private int targetWidth = 1280;
        private int targetHeight = 720;
        private float targetAspect = 0f;
        private bool debugLog = false;
        public static string playerSaveSuffix = "";
        private float uiCheckTimer = 0f;
        private float uiScaleMultiplier = 1.0f;

        private void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                string argLower = args[i].ToLower();
                if (argLower == "-screen-width" && i + 1 < args.Length) int.TryParse(args[i + 1], out targetWidth);
                else if (argLower == "-screen-height" && i + 1 < args.Length) int.TryParse(args[i + 1], out targetHeight);
                else if (argLower == "-aspect" && i + 1 < args.Length) float.TryParse(args[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out targetAspect);
                else if (argLower == "-playersave" && i + 1 < args.Length) playerSaveSuffix = args[i + 1];
                else if (argLower == "-ui" && i + 1 < args.Length) uiScaleMultiplier = float.Parse(args[i + 1], CultureInfo.InvariantCulture);
            }

            string finalSavePath = "Default";
            if (!string.IsNullOrEmpty(playerSaveSuffix))
            {
                CustomPrefsManager.Initialize(playerSaveSuffix);
                finalSavePath = Path.Combine(Environment.CurrentDirectory, "NC_Saves", playerSaveSuffix);
            }

            UnityFixesPlugin.Logger.LogInfo("      NUCLEUS UNITY FIXES IL2CPP      ");
            UnityFixesPlugin.Logger.LogInfo($"Resolution   : {targetWidth}x{targetHeight}");
            UnityFixesPlugin.Logger.LogInfo($"3D Aspect Ratio : {(targetAspect > 0f ? targetAspect.ToString(CultureInfo.InvariantCulture) : "Native")}");
            UnityFixesPlugin.Logger.LogInfo($"UI Scale     : {uiScaleMultiplier}x");
            UnityFixesPlugin.Logger.LogInfo($"Save Folder  : {finalSavePath}");

            AplicarConfiguracoesTela();
        }

        private void Update()
        {
            // alwyas focus. https://docs.unity3d.com/6000.5/Documentation/ScriptReference/Application-runInBackground.html
            Application.runInBackground = true;

            // Force windowed mode and resolution if it changes
            if (Screen.width != targetWidth || Screen.height != targetHeight || Screen.fullScreenMode != FullScreenMode.Windowed || Screen.fullScreen)
            {
                AplicarConfiguracoesTela();
            }

            // This is for IN-GAME asprect ratio or mostly 3D stuff.
            if (targetAspect > 0f && Camera.main != null)
            {
                Camera.main.aspect = targetAspect;
            }

            // force menus/UI
            uiCheckTimer += Time.deltaTime;
            if (uiCheckTimer >= 2f)
            {
                AjustarCanvasScalers();
                uiCheckTimer = 0f;
            }
        }

        private void AplicarConfiguracoesTela()
        {
            //force res/windowed
            Screen.SetResolution(targetWidth, targetHeight, FullScreenMode.Windowed);
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
        }

        //This forces the aspect ratio of menus/UI to match the current screen resolution, preventing menus going fully off screen.
        private void AjustarCanvasScalers()
        {
            // All the menu/ui
            CanvasScaler[] scalers = FindObjectsOfType<CanvasScaler>();
            foreach (var scaler in scalers)
            {
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                    float scaledWidth = 1920f / uiScaleMultiplier;
                    float scaledHeight = 1080f / uiScaleMultiplier;

                    // forces to 1080p most normal res, this is a base for the option below. https://docs.unity3d.com/2017.1/Documentation/ScriptReference/UI.CanvasScaler-referenceResolution.html
                    scaler.referenceResolution = new Vector2(scaledWidth, scaledHeight);

                    // .expand basically auto forces res the 1080p menu (above) into the current screen size. either choosing Y or X to fit the screen. https://docs.unity3d.com/2019.1/Documentation/ScriptReference/UI.CanvasScaler.ScreenMatchMode.Expand.html
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                }
            }
        }
    }

    // All the stuff under will intercept the default Unity behavior to redirect the save path and PlayerPrefs to a custom location based on the playerSaveSuffix.
    // everything under this was fully AI as i couldnt figure out how to do different saves, even the current method doesnt fully separete everything.
    [HarmonyPatch(typeof(Application), "get_persistentDataPath")]
    public static class PersistentDataPathPatch
    {
        public static bool Prefix(ref string __result)
        {
            if (!string.IsNullOrEmpty(UnityFixesBehavior.playerSaveSuffix))
            {
                try
                {
                    string basePath = Environment.CurrentDirectory;
                    string customPath = Path.Combine(basePath, "NC_Saves", UnityFixesBehavior.playerSaveSuffix);
                    if (!Directory.Exists(customPath)) Directory.CreateDirectory(customPath);
                    __result = customPath;
                    return false;
                }
                catch (Exception ex)
                {
                    UnityFixesPlugin.Logger.LogError($"Error PersistentDataPath: {ex.Message}");
                    return true;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerPrefs))]
    public static class PlayerPrefsPatches
    {
        private static bool ShouldBypass() => !string.IsNullOrEmpty(UnityFixesBehavior.playerSaveSuffix);

        [HarmonyPatch(nameof(PlayerPrefs.SetInt))]
        [HarmonyPrefix]
        public static bool SetInt(string key, int value) { if (ShouldBypass()) { CustomPrefsManager.SetInt(key, value); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.GetInt), typeof(string), typeof(int))]
        [HarmonyPrefix]
        public static bool GetIntDef(string key, int defaultValue, ref int __result) { if (ShouldBypass()) { __result = CustomPrefsManager.GetInt(key, defaultValue); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.GetInt), typeof(string))]
        [HarmonyPrefix]
        public static bool GetInt(string key, ref int __result) { if (ShouldBypass()) { __result = CustomPrefsManager.GetInt(key, 0); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.SetFloat))]
        [HarmonyPrefix]
        public static bool SetFloat(string key, float value) { if (ShouldBypass()) { CustomPrefsManager.SetFloat(key, value); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.GetFloat), typeof(string), typeof(float))]
        [HarmonyPrefix]
        public static bool GetFloatDef(string key, float defaultValue, ref float __result) { if (ShouldBypass()) { __result = CustomPrefsManager.GetFloat(key, defaultValue); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.GetFloat), typeof(string))]
        [HarmonyPrefix]
        public static bool GetFloat(string key, ref float __result) { if (ShouldBypass()) { __result = CustomPrefsManager.GetFloat(key, 0f); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.SetString))]
        [HarmonyPrefix]
        public static bool SetString(string key, string value) { if (ShouldBypass()) { CustomPrefsManager.SetString(key, value); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.GetString), typeof(string), typeof(string))]
        [HarmonyPrefix]
        public static bool GetStringDef(string key, string defaultValue, ref string __result) { if (ShouldBypass()) { __result = CustomPrefsManager.GetString(key, defaultValue); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.GetString), typeof(string))]
        [HarmonyPrefix]
        public static bool GetString(string key, ref string __result) { if (ShouldBypass()) { __result = CustomPrefsManager.GetString(key, ""); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.HasKey))]
        [HarmonyPrefix]
        public static bool HasKey(string key, ref bool __result) { if (ShouldBypass()) { __result = CustomPrefsManager.HasKey(key); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.DeleteKey))]
        [HarmonyPrefix]
        public static bool DeleteKey(string key) { if (ShouldBypass()) { CustomPrefsManager.DeleteKey(key); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.DeleteAll))]
        [HarmonyPrefix]
        public static bool DeleteAll() { if (ShouldBypass()) { CustomPrefsManager.DeleteAll(); return false; } return true; }

        [HarmonyPatch(nameof(PlayerPrefs.Save))]
        [HarmonyPrefix]
        public static bool Save() { if (ShouldBypass()) { CustomPrefsManager.SaveToDisk(); return false; } return true; }
    }
}