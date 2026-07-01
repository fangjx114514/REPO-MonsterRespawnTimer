using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;

namespace MonsterRespawnTimer;

internal static class EnemySnapshotReader
{
    private static readonly FieldInfo? SpawnedField = AccessTools.Field(typeof(EnemyParent), "Spawned");
    private static bool spawnedReadErrorLogged;
    private static bool localizedNameErrorLogged;

    public static List<EnemySnapshot> ReadSnapshots()
    {
        var snapshots = new List<EnemySnapshot>();
        var enemies = EnemyDirector.instance?.enemiesSpawned;
        if (enemies == null)
        {
            return snapshots;
        }

        foreach (var enemyParent in enemies)
        {
            if (enemyParent == null)
            {
                continue;
            }

            var displayName = ReadDisplayName(enemyParent);
            snapshots.Add(new EnemySnapshot(
                enemyParent.GetInstanceID().ToString(CultureInfo.InvariantCulture),
                displayName,
                ReadGroupName(enemyParent, displayName),
                ReadSpawned(enemyParent),
                enemyParent.DespawnedTimer));
        }

        return snapshots;
    }

    private static bool ReadSpawned(EnemyParent enemyParent)
    {
        try
        {
            if (SpawnedField?.GetValue(enemyParent) is bool spawned)
            {
                return spawned;
            }
        }
        catch (Exception ex)
        {
            if (!spawnedReadErrorLogged)
            {
                spawnedReadErrorLogged = true;
                Plugin.Logger.LogError($"MonsterRespawnTimer could not read EnemyParent.Spawned and will use GameObject active state instead: {ex}");
            }
        }

        return enemyParent.gameObject.activeInHierarchy;
    }

    private static string ReadDisplayName(EnemyParent enemyParent)
    {
        try
        {
            var localized = enemyParent.enemyNameLocalized?.GetLocalizedString();
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }
        }
        catch (Exception ex)
        {
            if (!localizedNameErrorLogged)
            {
                localizedNameErrorLogged = true;
                Plugin.Logger.LogError($"MonsterRespawnTimer could not localize an enemy name and will use the raw enemy name instead: {ex}");
            }
        }

        if (!string.IsNullOrWhiteSpace(enemyParent.enemyName))
        {
            return enemyParent.enemyName;
        }

        return "Enemy";
    }

    private static string ReadGroupName(EnemyParent enemyParent, string fallbackDisplayName)
    {
        return string.IsNullOrWhiteSpace(enemyParent.enemyName) ? fallbackDisplayName : enemyParent.enemyName;
    }
}
