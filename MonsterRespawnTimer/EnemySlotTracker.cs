using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MonsterRespawnTimer;

internal readonly struct EnemySnapshot
{
    public EnemySnapshot(string stableId, string displayName, bool isSpawned, float respawnTimer)
        : this(stableId, displayName, displayName, isSpawned, respawnTimer)
    {
    }

    public EnemySnapshot(string stableId, string displayName, string groupName, bool isSpawned, float respawnTimer)
    {
        StableId = stableId;
        DisplayName = displayName;
        GroupName = groupName;
        IsSpawned = isSpawned;
        RespawnTimer = respawnTimer;
    }

    public string StableId { get; }
    public string DisplayName { get; }
    public string GroupName { get; }
    public bool IsSpawned { get; }
    public float RespawnTimer { get; }
}

internal sealed class EnemySlotTracker
{
    public const int MaxSlots = 11;
    public const float FlashDurationSeconds = 1f;
    public const float SuddenDecreaseThresholdSeconds = 2f;

    private readonly List<EnemySlotState> slots = new(MaxSlots);

    public IReadOnlyList<EnemySlotState> Slots => slots;

    public void Reset()
    {
        slots.Clear();
    }

    public void Update(IReadOnlyList<EnemySnapshot> snapshots, float elapsedSeconds)
    {
        var safeElapsed = Math.Max(0f, elapsedSeconds);

        AddNewSlots(snapshots);

        foreach (var slot in slots)
        {
            slot.Apply(snapshots, safeElapsed);
        }
    }

    private void AddNewSlots(IReadOnlyList<EnemySnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            if (string.IsNullOrWhiteSpace(snapshot.StableId))
            {
                continue;
            }

            var displayKey = EnemySlotState.CreateDisplayKey(snapshot);
            var existingSlot = slots.FirstOrDefault(slot => slot.DisplayKey == displayKey);
            if (existingSlot != null)
            {
                existingSlot.AddMemberIfMissing(snapshot);
                continue;
            }

            if (slots.Count >= MaxSlots)
            {
                continue;
            }

            slots.Add(new EnemySlotState(displayKey, snapshot));
        }
    }
}

internal sealed class EnemySlotState
{
    private readonly List<EnemyMemberState> members = new();

    public EnemySlotState(string displayKey, EnemySnapshot snapshot)
    {
        DisplayKey = displayKey;
        StableId = displayKey.StartsWith("group:", StringComparison.Ordinal) ? displayKey : snapshot.StableId;
        BaseDisplayName = NormalizeDisplayName(snapshot.DisplayName);
        AddMemberIfMissing(snapshot);
    }

    public string DisplayKey { get; }
    public string StableId { get; }
    public string BaseDisplayName { get; private set; }
    public bool IsAssigned => members.Count > 0;
    public bool IsVisible { get; private set; } = true;
    public bool IsSpawned => members.Where(member => member.IsVisible).All(member => member.IsSpawned);
    public float RespawnTimer => members
        .Where(member => member.IsVisible && !member.IsSpawned)
        .Select(member => member.RespawnTimer)
        .DefaultIfEmpty(0f)
        .Min();
    public string DisplayName
    {
        get
        {
            if (!IsSwarmKey(DisplayKey) || members.Count <= 1)
            {
                return BaseDisplayName;
            }

            var visibleMembers = members.Where(member => member.IsVisible).ToArray();
            var totalCount = visibleMembers.Length > 0 ? visibleMembers.Length : members.Count;
            var spawnedCount = visibleMembers.Count(member => member.IsSpawned);
            return $"{BaseDisplayName} {spawnedCount.ToString(CultureInfo.InvariantCulture)}/{totalCount.ToString(CultureInfo.InvariantCulture)}";
        }
    }
    public IReadOnlyList<string> TimerTexts => members
        .Where(member => member.IsVisible && !member.IsSpawned)
        .Select(member => member.TimerText)
        .Where(text => text.Length > 0)
        .ToArray();
    public string TimerText => string.Join(" ", TimerTexts);
    public string FlashText => members
        .Where(member => member.IsVisible)
        .Select(member => member.FlashText)
        .LastOrDefault(text => text.Length > 0) ?? string.Empty;
    public string StatusText => BuildStatusText(TimerTexts, FlashText);

    public static string CreateDisplayKey(EnemySnapshot snapshot)
    {
        var groupName = NormalizeDisplayName(snapshot.GroupName);
        if (IsSwarmName(groupName))
        {
            return "group:" + groupName.ToLowerInvariant();
        }

        return "enemy:" + snapshot.StableId;
    }

    public void AddMemberIfMissing(EnemySnapshot snapshot)
    {
        if (members.Any(member => member.StableId == snapshot.StableId))
        {
            return;
        }

        members.Add(new EnemyMemberState(snapshot));
    }

    public void Apply(IReadOnlyList<EnemySnapshot> snapshots, float elapsedSeconds)
    {
        foreach (var member in members)
        {
            var snapshot = snapshots.FirstOrDefault(candidate => candidate.StableId == member.StableId);
            if (string.IsNullOrEmpty(snapshot.StableId))
            {
                member.MarkUnavailable(elapsedSeconds);
                continue;
            }

            member.Apply(snapshot, elapsedSeconds);
        }

        var visible = members.Where(member => member.IsVisible).ToArray();
        IsVisible = visible.Length > 0;
        var nameSource = visible.Length > 0 ? visible[0].DisplayName : members[0].DisplayName;
        BaseDisplayName = NormalizeDisplayName(nameSource);
    }

    private static string BuildStatusText(IReadOnlyList<string> timerTexts, string flashText)
    {
        if (timerTexts.Count == 0)
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(flashText))
        {
            return string.Join(" ", timerTexts);
        }

        if (timerTexts.Count == 1)
        {
            return flashText + " " + timerTexts[0];
        }

        var parts = new List<string>(timerTexts.Count + 1);
        for (var index = 0; index < timerTexts.Count - 1; index++)
        {
            parts.Add(timerTexts[index]);
        }

        parts.Add(flashText);
        parts.Add(timerTexts[timerTexts.Count - 1]);
        return string.Join(" ", parts);
    }

    private static bool IsSwarmKey(string displayKey)
    {
        return displayKey.StartsWith("group:", StringComparison.Ordinal);
    }

    private static bool IsSwarmName(string displayName)
    {
        return string.Equals(displayName, "Gnome", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(displayName, "Banger", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeDisplayName(string displayName)
    {
        return string.IsNullOrWhiteSpace(displayName) ? "Enemy" : displayName.Trim();
    }
}

internal sealed class EnemyMemberState
{
    private float lastRespawnTimer;
    private float flashRemainingSeconds;

    public EnemyMemberState(EnemySnapshot snapshot)
    {
        StableId = snapshot.StableId;
        DisplayName = NormalizeDisplayName(snapshot.DisplayName);
        IsVisible = true;
        IsSpawned = snapshot.IsSpawned;
        RespawnTimer = Math.Max(0f, snapshot.RespawnTimer);
        lastRespawnTimer = RespawnTimer;
        FlashText = string.Empty;
    }

    public string StableId { get; }
    public string DisplayName { get; private set; }
    public bool IsVisible { get; private set; }
    public bool IsSpawned { get; private set; }
    public float RespawnTimer { get; private set; }
    public string TimerText => IsSpawned ? string.Empty : FormatSeconds(RespawnTimer);
    public string FlashText { get; private set; }

    public void Apply(EnemySnapshot snapshot, float elapsedSeconds)
    {
        TickFlash(elapsedSeconds);

        var wasVisible = IsVisible;
        var wasRespawning = wasVisible && !IsSpawned;
        var previousTimer = lastRespawnTimer;

        IsVisible = true;
        DisplayName = NormalizeDisplayName(snapshot.DisplayName);

        IsSpawned = snapshot.IsSpawned;
        RespawnTimer = Math.Max(0f, snapshot.RespawnTimer);

        if (!IsSpawned && wasRespawning)
        {
            var actualDecrease = previousTimer - RespawnTimer;
            var extraDecrease = actualDecrease - Math.Max(0f, elapsedSeconds);
            if (extraDecrease >= EnemySlotTracker.SuddenDecreaseThresholdSeconds)
            {
                var displayDecrease = Math.Max(1, (int)Math.Round(actualDecrease, MidpointRounding.AwayFromZero));
                FlashText = $"-{displayDecrease.ToString(CultureInfo.InvariantCulture)}s";
                flashRemainingSeconds = EnemySlotTracker.FlashDurationSeconds;
            }
        }

        lastRespawnTimer = RespawnTimer;
    }

    public void MarkUnavailable(float elapsedSeconds)
    {
        TickFlash(elapsedSeconds);
        IsVisible = false;
    }

    private void TickFlash(float elapsedSeconds)
    {
        if (flashRemainingSeconds <= 0f)
        {
            FlashText = string.Empty;
            flashRemainingSeconds = 0f;
            return;
        }

        flashRemainingSeconds -= Math.Max(0f, elapsedSeconds);
        if (flashRemainingSeconds <= 0f)
        {
            FlashText = string.Empty;
            flashRemainingSeconds = 0f;
        }
    }

    private static string FormatSeconds(float seconds)
    {
        var rounded = Math.Max(0, (int)Math.Ceiling(seconds));
        return rounded.ToString(CultureInfo.InvariantCulture) + "s";
    }

    private static string NormalizeDisplayName(string displayName)
    {
        return string.IsNullOrWhiteSpace(displayName) ? "Enemy" : displayName.Trim();
    }
}
