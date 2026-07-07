using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonsterRespawnTimer;

[BepInPlugin(PluginMetadata.Guid, PluginMetadata.Name, PluginMetadata.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; private set; } = null!;

    private Harmony? harmony;
    private GameObject? runnerObject;

    private void Awake()
    {
        Logger = base.Logger;

        gameObject.transform.parent = null;
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        harmony = new Harmony(PluginMetadata.Guid);
        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            harmony.UnpatchSelf();
            harmony = null;
            Logger.LogError($"MonsterRespawnTimer failed to patch game methods and will stay disabled: {ex}");
            enabled = false;
            return;
        }

        runnerObject = new GameObject("MonsterRespawnTimer Runner");
        DontDestroyOnLoad(runnerObject);
        runnerObject.hideFlags = HideFlags.HideAndDontSave;
        runnerObject.AddComponent<MonsterRespawnTimerRunner>();

        SceneManager.sceneLoaded += MonsterRespawnTimerController.OnSceneLoaded;
        SceneManager.activeSceneChanged += MonsterRespawnTimerController.OnActiveSceneChanged;
        Canvas.willRenderCanvases += MonsterRespawnTimerController.OnWillRenderCanvases;

        Logger.LogInfo($"Plugin {PluginMetadata.Guid} build {PluginMetadata.Version} is loaded.");
    }

    private void Update()
    {
        MonsterRespawnTimerController.Tick("Plugin.Update");
    }

    private void LateUpdate()
    {
        MonsterRespawnTimerController.Tick("Plugin.LateUpdate");
    }

    private void FixedUpdate()
    {
        MonsterRespawnTimerController.Tick("Plugin.FixedUpdate");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= MonsterRespawnTimerController.OnSceneLoaded;
        SceneManager.activeSceneChanged -= MonsterRespawnTimerController.OnActiveSceneChanged;
        Canvas.willRenderCanvases -= MonsterRespawnTimerController.OnWillRenderCanvases;

        MonsterRespawnTimerController.DestroyActiveHud();

        if (runnerObject != null)
        {
            Destroy(runnerObject);
            runnerObject = null;
        }

        harmony?.UnpatchSelf();
        harmony = null;
    }
}

internal static class PluginMetadata
{
    public const string Guid = "fangjx114514.monsterrespawntimer";
    public const string Name = "Monster Respawn Timer";
    public const string Version = "1.0.2";
}

[HarmonyPatch]
internal static class MonsterRespawnTimerHarmonyPatches
{
    [HarmonyPatch(typeof(SemiFunc), nameof(SemiFunc.OnSceneSwitch))]
    [HarmonyPrefix]
    private static void Prefix()
    {
        MonsterRespawnTimerController.DestroyActiveHud();
    }

    [HarmonyPatch(typeof(RoundDirector), "Update")]
    [HarmonyPostfix]
    private static void Postfix()
    {
        MonsterRespawnTimerController.Tick("RoundDirector.Update");
    }

    [HarmonyPatch(typeof(RunManager), "Update")]
    [HarmonyPostfix]
    private static void RunManagerUpdatePostfix()
    {
        MonsterRespawnTimerController.Tick("RunManager.Update");
    }

    [HarmonyPatch(typeof(EnemyDirector), "Update")]
    [HarmonyPostfix]
    private static void EnemyDirectorUpdatePostfix()
    {
        MonsterRespawnTimerController.Tick("EnemyDirector.Update");
    }

    [HarmonyPatch(typeof(RunManager), nameof(RunManager.ChangeLevel))]
    [HarmonyPostfix]
    private static void RunManagerChangeLevelPostfix()
    {
        MonsterRespawnTimerController.OnRunManagerChangeLevel();
    }
}
