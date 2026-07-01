using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonsterRespawnTimer;

internal static class MonsterRespawnTimerController
{
    private const float RefreshIntervalSeconds = 0.25f;

    private static readonly EnemySlotTracker Tracker = new();

    private static MonsterRespawnTimerHudView? hudView;
    private static float nextRefreshTime;
    private static float lastRefreshTime;
    private static bool readinessErrorLogged;

    public static void Tick(string source)
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        var now = Time.unscaledTime;
        var elapsed = lastRefreshTime > 0f ? now - lastRefreshTime : 0f;
        lastRefreshTime = now;
        nextRefreshTime = now + RefreshIntervalSeconds;

        TickNow(elapsed);
    }

    public static void OnRunManagerChangeLevel()
    {
        nextRefreshTime = 0f;
        Tick("RunManager.ChangeLevel");
    }

    public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetRefresh();
        Tick("SceneManager.sceneLoaded");
    }

    public static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        DestroyHud();
        Tick("SceneManager.activeSceneChanged");
    }

    public static void OnWillRenderCanvases()
    {
        Tick("Canvas.willRenderCanvases");
    }

    public static void DestroyActiveHud()
    {
        DestroyHud();
    }

    private static void TickNow(float elapsedSeconds)
    {
        if (!IsHudReady())
        {
            if (hudView is { IsAlive: true })
            {
                DestroyHud();
            }

            return;
        }

        if (!EnsureHud())
        {
            Tracker.Reset();
            return;
        }

        var snapshots = EnemySnapshotReader.ReadSnapshots();
        Tracker.Update(snapshots, elapsedSeconds);
        hudView?.Render(Tracker.Slots);
    }

    private static bool EnsureHud()
    {
        if (hudView is { IsAlive: true })
        {
            return true;
        }

        hudView?.Destroy();
        hudView = null;

        if (!MonsterRespawnTimerHudView.TryCreate(out var createdView))
        {
            return false;
        }

        hudView = createdView;
        Tracker.Reset();
        return true;
    }

    private static void DestroyHud()
    {
        hudView?.Destroy();
        hudView = null;
        Tracker.Reset();
        lastRefreshTime = 0f;
    }

    private static void ResetRefresh()
    {
        nextRefreshTime = 0f;
        lastRefreshTime = 0f;
        readinessErrorLogged = false;
    }

    private static bool IsHudReady()
    {
        try
        {
            if (RunManager.instance == null)
            {
                return false;
            }

            if (!SemiFunc.RunIsLevel())
            {
                return false;
            }

            if (!MonsterRespawnTimerHudStatus.ShouldRenderForSpectateState(SemiFunc.IsSpectating()))
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            if (!readinessErrorLogged)
            {
                readinessErrorLogged = true;
                Plugin.Logger.LogError($"MonsterRespawnTimer could not check whether the run is in a level: {ex}");
            }

            return false;
        }

        return GameObject.Find("Game Hud") != null;
    }
}
