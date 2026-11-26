
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine.InputSystem;

[BepInPlugin(
    "com.carlos.firstpersoncamera",
    "OpenCampfire First Person Camera",
    "1.0.0")]
public class FirstPersonCameraPlugin : BaseUnityPlugin
{
    private Harmony harmony;

    private static FirstPersonCameraPlugin Instance;
    private static bool firstPersonEnabled;

    private static readonly FieldInfo ForceFirstPersonFrameCount =
        AccessTools.Field(
            typeof(GameplayCamera),
            "mForceFirstPersonFrameCount");

    private void Awake()
    {
        Instance = this;

        Logger.LogInfo("FIRST PERSON CAMERA 1.0.0 INITIALIZING");

        if (ForceFirstPersonFrameCount == null)
        {
            Logger.LogError("Could not find mForceFirstPersonFrameCount.");
            return;
        }

        harmony = new Harmony("com.carlos.firstpersoncamera");

        var updateMethod = AccessTools.Method(
            typeof(GameplayCamera), "Update");

        if (updateMethod == null)
        {
            Logger.LogError("GameplayCamera.Update not found.");
            return;
        }

        var prefix = new HarmonyMethod(
            typeof(FirstPersonCameraPlugin).GetMethod(
                nameof(CameraUpdatePrefix),
                BindingFlags.Static | BindingFlags.NonPublic));

        harmony.Patch(updateMethod, prefix: prefix);

        Logger.LogInfo("PATCHED: GameplayCamera.Update");
        Logger.LogInfo("Press G to toggle first-person mode.");
        Logger.LogInfo("FIRST PERSON CAMERA INITIALIZED");
    }

    private static bool IsTyping()
    {
        var chat = CCSCore.Singleton<UI.ChatPopup>.Instance;
        return chat != null && chat.IsShown();
    }

    private static void CameraUpdatePrefix(GameplayCamera __instance)
    {
        if (Keyboard.current != null &&
            Keyboard.current.gKey.wasPressedThisFrame &&
            !IsTyping())
        {
            var player = PlayerTracker.MyPlayer;

            if (player != null && player.SpawnedCharacter != null)
            {
                firstPersonEnabled = !firstPersonEnabled;

                Instance.Logger.LogInfo(
                    "First-person mode: " +
                    (firstPersonEnabled ? "ENABLED" : "DISABLED"));
            }
        }

        if (firstPersonEnabled)
        {
            var player = PlayerTracker.MyPlayer;

            if (player != null && player.SpawnedCharacter != null)
            {
                ForceFirstPersonFrameCount.SetValue(__instance, 2);
            }
        }
    }

    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
        firstPersonEnabled = false;
        Instance = null;
    }
}
