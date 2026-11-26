
using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("com.carlos.cosmeticinspector", "Cosmetic Inspector", "1.8.0")]
public class CosmeticInspectorPlugin : BaseUnityPlugin
{
    private Harmony harmony;

    private void Awake()
    {
        Logger.LogInfo("COSMETIC INSPECTOR 1.8.0 INITIALIZING");

        harmony = new Harmony("com.carlos.cosmeticinspector");

        PatchMethod(typeof(CosmeticsHandler), "SelectCosmetic");
        PatchMethod(typeof(CosmeticsHandler), "SelectAndEquipCosmetic");
        PatchMethod(typeof(CosmeticsHandler), "EquipCosmetic");
        PatchMethod(typeof(PlayerCharacter), "OnCosmeticChanged");

        PatchLockedCatSelection();

        Logger.LogInfo("COSMETIC INSPECTOR 1.8.0 INITIALIZED");
    }

    private void PatchMethod(Type type, string methodName)
    {
        var method = AccessTools.Method(type, methodName);

        if (method == null)
        {
            Logger.LogWarning("METHOD NOT FOUND: " + type.Name + "." + methodName);
            return;
        }

        var prefix = new HarmonyMethod(
            typeof(CosmeticInspectorPlugin).GetMethod(
                nameof(TracePrefix),
                BindingFlags.Static | BindingFlags.NonPublic));

        var postfix = new HarmonyMethod(
            typeof(CosmeticInspectorPlugin).GetMethod(
                nameof(TracePostfix),
                BindingFlags.Static | BindingFlags.NonPublic));

        harmony.Patch(method, prefix: prefix, postfix: postfix);

        Logger.LogInfo("PATCHED: " + type.Name + "." + methodName);
    }

    private void PatchLockedCatSelection()
    {
        var method = AccessTools.Method(
            typeof(UI.PlayerHubGridUI),
            "TryEquipCosmetic",
            new[] { typeof(CosmeticData) });

        if (method == null)
        {
            Logger.LogError("CAT PATCH FAILED: TryEquipCosmetic not found.");
            return;
        }

        var prefix = new HarmonyMethod(
            typeof(CosmeticInspectorPlugin).GetMethod(
                nameof(LockedCatEquipPrefix),
                BindingFlags.Static | BindingFlags.NonPublic));

        harmony.Patch(method, prefix: prefix);

        Logger.LogInfo(
            "PATCHED: UI.PlayerHubGridUI.TryEquipCosmetic (Locked cats)");
    }

    private static bool LockedCatEquipPrefix(object[] __args)
    {
        var plugin = FindPlugin();

        if (__args == null || __args.Length == 0)
            return true;

        var cosmetic = __args[0] as CosmeticData;

        if (cosmetic == null ||
            !cosmetic.IsDemoLocked ||
            cosmetic.CategoryRef == null ||
            cosmetic.CategoryRef.CategoryEnum !=
                CharacterCustomizer.CosmeticCategoryEnum.Species ||
            string.IsNullOrEmpty(cosmetic.Asset) ||
            !cosmetic.Asset.StartsWith(
                "Species_Cat_", StringComparison.Ordinal))
        {
            return true;
        }

        var player = PlayerTracker.MyPlayer;

        if (player == null || player.SpawnedCharacter == null)
            return true;

        var character = player.SpawnedCharacter;

        if (character.CharacterModel == null)
            return true;

        var handler = Singleton<CosmeticsHandler>.Instance;

        if (handler == null)
            return true;

        var controller = Singleton<UI.PlayerHubUIController>.Instance;

        bool wasPreviewActive =
            controller != null && controller.DemoSpeciesPreviewActive;

        controller?.ClearDemoSpeciesPreview();

        if (handler.IsAlreadySelected(cosmetic))
        {
            if (wasPreviewActive)
                handler.RestoreEquippedSpecies(character);

            plugin?.Logger.LogInfo(
                "CAT: Already selected - " + cosmetic.Name);

            return false;
        }

        handler.SelectAndEquipCosmetic(cosmetic, character);

        controller?.NotifyAppearanceChanged();

        var questHandler = Singleton<QuestHandler>.Instance;

        if (questHandler != null)
        {
            questHandler.OnCosmeticChanged(
                cosmetic.CategoryRef.CategoryEnum);
        }

        plugin?.Logger.LogInfo(
            "CAT: Selection handled - " + cosmetic.Name +
            " [" + cosmetic.Asset + "]");

        return false;
    }

    private static void TracePrefix(
        object[] __args,
        MethodBase __originalMethod)
    {
        var plugin = FindPlugin();

        if (plugin == null)
            return;

        plugin.Logger.LogInfo(
            "=== COSMETIC TRACE BEFORE: " +
            __originalMethod.Name + " ===");

        LogArguments(plugin, __args);
    }

    private static void TracePostfix(
        object[] __args,
        MethodBase __originalMethod)
    {
        var plugin = FindPlugin();

        if (plugin == null)
            return;

        plugin.Logger.LogInfo(
            "=== COSMETIC TRACE AFTER: " +
            __originalMethod.Name + " ===");

        LogArguments(plugin, __args);
    }

    private static CosmeticInspectorPlugin FindPlugin()
    {
        return BepInEx.Bootstrap.Chainloader.ManagerObject
            .GetComponent<CosmeticInspectorPlugin>();
    }

    private static void LogArguments(
        CosmeticInspectorPlugin plugin,
        object[] args)
    {
        if (args == null || args.Length == 0)
        {
            plugin.Logger.LogInfo("No arguments.");
            return;
        }

        for (int i = 0; i < args.Length; i++)
        {
            object arg = args[i];

            if (arg == null)
            {
                plugin.Logger.LogInfo("ARG[" + i + "]: null");
                continue;
            }

            plugin.Logger.LogInfo(
                "ARG[" + i + "]: " +
                arg.GetType().FullName + " = " + arg);

            if (arg is CosmeticData cosmetic)
            {
                plugin.Logger.LogInfo("  Name: " + cosmetic.Name);
                plugin.Logger.LogInfo("  Asset: " + cosmetic.Asset);
                plugin.Logger.LogInfo("  IsDemoLocked: " + cosmetic.IsDemoLocked);
                plugin.Logger.LogInfo("  IsOwned: " + cosmetic.IsOwned);
                plugin.Logger.LogInfo("  IsUnlocked: " + cosmetic.IsUnlocked);
                plugin.Logger.LogInfo("  IsUnlockFlagSet: " + cosmetic.IsUnlockFlagSet);
            }
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(10f);

        var db = CosmeticDatabase.Instance;

        if (db == null)
        {
            Logger.LogError("CosmeticDatabase.Instance is NULL.");
            yield break;
        }

        var records = db.GetSortedList();

        Logger.LogInfo("=== COSMETIC RUNTIME STATE ===");
        Logger.LogInfo("Total records: " + records.Count);

        foreach (var item in records)
        {
            Logger.LogInfo("--- " + item.Name + " [" + item.Asset + "] ---");
            Logger.LogInfo("FullGameOnly: " + item.IsDemoLocked);
            Logger.LogInfo("IsOwned: " + item.IsOwned);
            Logger.LogInfo("IsUnlocked: " + item.IsUnlocked);
            Logger.LogInfo("IsUnlockFlagSet: " + item.IsUnlockFlagSet);
        }

        Logger.LogInfo("=== END RUNTIME STATE ===");
    }
}
