using MonsterRespawnTimer;

var tests = new (string Name, Action Body)[]
{
    ("keeps_first_eleven_slots_in_initial_order", KeepsFirstElevenSlotsInInitialOrder),
    ("does_not_reorder_existing_slots_when_snapshot_order_changes", DoesNotReorderExistingSlotsWhenSnapshotOrderChanges),
    ("groups_swarm_enemies_into_one_display_slot", GroupsSwarmEnemiesIntoOneDisplaySlot),
    ("groups_swarm_enemies_by_internal_name", GroupsSwarmEnemiesByInternalName),
    ("places_flash_before_rightmost_timer", PlacesFlashBeforeRightmostTimer),
    ("shows_flash_only_for_sudden_respawn_timer_decrease", ShowsFlashOnlyForSuddenRespawnTimerDecrease),
    ("shows_flash_when_sudden_respawn_completion_removes_timer", ShowsFlashWhenSuddenRespawnCompletionRemovesTimer),
    ("does_not_flash_when_missing_respawning_enemy_returns", DoesNotFlashWhenMissingRespawningEnemyReturns),
    ("respects_flash_threshold_boundary", RespectsFlashThresholdBoundary),
    ("reset_clears_slots_and_flash_state", ResetClearsSlotsAndFlashState),
    ("hud_probe_is_visible_only_until_enemy_slots_render", HudProbeIsVisibleOnlyUntilEnemySlotsRender),
    ("hud_hides_while_local_player_is_spectating", HudHidesWhileLocalPlayerIsSpectating),
    ("hud_layout_uses_larger_text_and_flash_gap", HudLayoutUsesLargerTextAndFlashGap),
    ("hud_layout_keeps_slots_right_aligned", HudLayoutKeepsSlotsRightAligned),
    ("hud_layout_stacks_slots_upward", HudLayoutStacksSlotsUpward),
};

var passed = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL {test.Name}: {ex.Message}");
        Environment.Exit(1);
    }
}

Console.WriteLine($"PASS {passed}/{tests.Length}");

static void KeepsFirstElevenSlotsInInitialOrder()
{
    var tracker = new EnemySlotTracker();

    tracker.Update(MakeSnapshots(12), elapsedSeconds: 0f);

    AssertEqual(EnemySlotTracker.MaxSlots, tracker.Slots.Count, "slot count");
    for (var index = 0; index < EnemySlotTracker.MaxSlots; index++)
    {
        AssertEqual($"enemy-{index}", tracker.Slots[index].StableId, $"slot {index} id");
        AssertEqual($"Enemy {index}", tracker.Slots[index].DisplayName, $"slot {index} name");
    }
}

static void GroupsSwarmEnemiesIntoOneDisplaySlot()
{
    var tracker = new EnemySlotTracker();

    tracker.Update(new[]
    {
        new EnemySnapshot("gnome-0", "Gnome", true, 0f),
        new EnemySnapshot("gnome-1", "Gnome", false, 18f),
        new EnemySnapshot("gnome-2", "Gnome", false, 44f),
        new EnemySnapshot("gnome-3", "Gnome", true, 0f),
        new EnemySnapshot("banger-0", "Banger", true, 0f),
        new EnemySnapshot("banger-1", "Banger", false, 12f),
        new EnemySnapshot("banger-2", "Banger", false, 39f),
        new EnemySnapshot("duck-0", "Duck", true, 0f),
    }, elapsedSeconds: 0f);

    AssertEqual(3, tracker.Slots.Count, "display slot count");
    AssertEqual("Gnome 2/4", tracker.Slots[0].DisplayName, "gnome display name");
    AssertSequence(new[] { "18s", "44s" }, tracker.Slots[0].TimerTexts, "gnome timers");
    AssertEqual("18s 44s", tracker.Slots[0].StatusText, "gnome status");
    AssertEqual("Banger 1/3", tracker.Slots[1].DisplayName, "banger display name");
    AssertSequence(new[] { "12s", "39s" }, tracker.Slots[1].TimerTexts, "banger timers");
    AssertEqual("Duck", tracker.Slots[2].DisplayName, "duck display name");
}

static void GroupsSwarmEnemiesByInternalName()
{
    var tracker = new EnemySlotTracker();

    tracker.Update(new[]
    {
        new EnemySnapshot("gnome-0", "Localized Gnome", "Gnome", true, 0f),
        new EnemySnapshot("gnome-1", "Localized Gnome", "Gnome", false, 22f),
        new EnemySnapshot("duck-0", "Localized Duck", "Duck", true, 0f),
    }, elapsedSeconds: 0f);

    AssertEqual(2, tracker.Slots.Count, "display slot count");
    AssertEqual("Localized Gnome 1/2", tracker.Slots[0].DisplayName, "localized group display name");
    AssertEqual("22s", tracker.Slots[0].StatusText, "localized group status");
    AssertEqual("Localized Duck", tracker.Slots[1].DisplayName, "localized normal display name");
}

static void PlacesFlashBeforeRightmostTimer()
{
    var tracker = new EnemySlotTracker();

    tracker.Update(new[]
    {
        new EnemySnapshot("banger-0", "Banger", true, 0f),
        new EnemySnapshot("banger-1", "Banger", false, 20f),
        new EnemySnapshot("banger-2", "Banger", false, 40f),
    }, elapsedSeconds: 0f);

    tracker.Update(new[]
    {
        new EnemySnapshot("banger-0", "Banger", true, 0f),
        new EnemySnapshot("banger-1", "Banger", false, 19.75f),
        new EnemySnapshot("banger-2", "Banger", false, 34f),
    }, elapsedSeconds: 0.25f);

    AssertEqual("-6s", tracker.Slots[0].FlashText, "banger flash");
    AssertEqual("20s -6s 34s", tracker.Slots[0].StatusText, "flash before rightmost timer");
}

static void DoesNotReorderExistingSlotsWhenSnapshotOrderChanges()
{
    var tracker = new EnemySlotTracker();
    tracker.Update(MakeSnapshots(4), elapsedSeconds: 0f);

    var reversed = MakeSnapshots(4).Reverse().ToArray();
    tracker.Update(reversed, elapsedSeconds: 0.25f);

    AssertEqual("enemy-0", tracker.Slots[0].StableId, "slot 0 id");
    AssertEqual("enemy-1", tracker.Slots[1].StableId, "slot 1 id");
    AssertEqual("enemy-2", tracker.Slots[2].StableId, "slot 2 id");
    AssertEqual("enemy-3", tracker.Slots[3].StableId, "slot 3 id");
}

static void ShowsFlashOnlyForSuddenRespawnTimerDecrease()
{
    var tracker = new EnemySlotTracker();

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 20f),
    }, elapsedSeconds: 0f);

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 19.75f),
    }, elapsedSeconds: 0.25f);

    AssertEqual("", tracker.Slots[0].FlashText, "natural countdown flash");

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 14.5f),
    }, elapsedSeconds: 0.25f);

    AssertEqual("-5s", tracker.Slots[0].FlashText, "sudden decrease flash");

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 14.25f),
    }, elapsedSeconds: 0.25f);

    AssertEqual("-5s", tracker.Slots[0].FlashText, "flash remains during one second window");

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 13.25f),
    }, elapsedSeconds: 1f);

    AssertEqual("", tracker.Slots[0].FlashText, "flash expires");
}

static void ShowsFlashWhenSuddenRespawnCompletionRemovesTimer()
{
    var tracker = new EnemySlotTracker();

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 30f),
        new EnemySnapshot("enemy-1", "Enemy 1", false, 18f),
    }, elapsedSeconds: 0f);

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", true, 0f),
        new EnemySnapshot("enemy-1", "Enemy 1", true, 0f),
    }, elapsedSeconds: 0.25f);

    AssertEqual("-30s", tracker.Slots[0].FlashText, "enemy 0 forced respawn flash");
    AssertEqual("-30s", tracker.Slots[0].StatusText, "enemy 0 status without timer");
    AssertEqual("-18s", tracker.Slots[1].FlashText, "enemy 1 forced respawn flash");
    AssertEqual("-18s", tracker.Slots[1].StatusText, "enemy 1 status without timer");

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", true, 0f),
        new EnemySnapshot("enemy-1", "Enemy 1", true, 0f),
    }, elapsedSeconds: 1f);

    AssertEqual("", tracker.Slots[0].FlashText, "enemy 0 forced respawn flash expires");
    AssertEqual("", tracker.Slots[0].StatusText, "enemy 0 forced respawn status expires");
    AssertEqual("", tracker.Slots[1].FlashText, "enemy 1 forced respawn flash expires");
    AssertEqual("", tracker.Slots[1].StatusText, "enemy 1 forced respawn status expires");
}

static void DoesNotFlashWhenMissingRespawningEnemyReturns()
{
    var tracker = new EnemySlotTracker();

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 30f),
    }, elapsedSeconds: 0f);

    tracker.Update(Array.Empty<EnemySnapshot>(), elapsedSeconds: 5f);
    AssertEqual(false, tracker.Slots[0].IsVisible, "visibility while missing");

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 20f),
    }, elapsedSeconds: 0.25f);

    AssertEqual("", tracker.Slots[0].FlashText, "flash after return");
    AssertEqual("20s", tracker.Slots[0].StatusText, "status after return");
}

static void ResetClearsSlotsAndFlashState()
{
    var tracker = new EnemySlotTracker();
    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 20f),
    }, elapsedSeconds: 0f);
    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 10f),
    }, elapsedSeconds: 0.25f);

    AssertEqual("-10s", tracker.Slots[0].FlashText, "flash before reset");

    tracker.Update(Array.Empty<EnemySnapshot>(), elapsedSeconds: 0.25f);
    AssertEqual(false, tracker.Slots[0].IsVisible, "visibility before reset");

    tracker.Reset();

    AssertEqual(0, tracker.Slots.Count, "slot count after reset");

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 9.75f),
    }, elapsedSeconds: 0.25f);

    AssertEqual(1, tracker.Slots.Count, "slot count after reset re-add");
    AssertEqual("enemy-0", tracker.Slots[0].StableId, "stable id after reset re-add");
    AssertEqual(true, tracker.Slots[0].IsVisible, "visibility after reset re-add");
    AssertEqual("", tracker.Slots[0].FlashText, "stale flash after reset");
    AssertEqual("10s", tracker.Slots[0].TimerText, "timer text after reset re-add");
    AssertEqual("10s", tracker.Slots[0].StatusText, "status text after reset re-add");
}

static void HudProbeIsVisibleOnlyUntilEnemySlotsRender()
{
    var tracker = new EnemySlotTracker();

    AssertEqual(true, MonsterRespawnTimerHudStatus.ShouldShowProbe(tracker.Slots), "probe with no slots");

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", true, 0f),
    }, elapsedSeconds: 0f);

    AssertEqual(false, MonsterRespawnTimerHudStatus.ShouldShowProbe(tracker.Slots), "probe with visible slot");

    tracker.Update(Array.Empty<EnemySnapshot>(), elapsedSeconds: 0.25f);

    AssertEqual(true, MonsterRespawnTimerHudStatus.ShouldShowProbe(tracker.Slots), "probe with hidden slots");
}

static void HudHidesWhileLocalPlayerIsSpectating()
{
    AssertEqual(true, MonsterRespawnTimerHudStatus.ShouldRenderForSpectateState(false), "render while alive");
    AssertEqual(false, MonsterRespawnTimerHudStatus.ShouldRenderForSpectateState(true), "hide while spectating");
}

static void HudLayoutUsesLargerTextAndFlashGap()
{
    AssertEqual(16f, MonsterRespawnTimerHudLayout.NameFontSize, "name font size");
    AssertEqual(15.5f, MonsterRespawnTimerHudLayout.StatusFontSize, "status font size");
    AssertEqual(true, MonsterRespawnTimerHudLayout.StatusFontSize < MonsterRespawnTimerHudLayout.NameFontSize, "status smaller than name");
    AssertEqual(0.5f, MonsterRespawnTimerHudLayout.FlashTimerGapEm, "flash timer gap");
}

static void HudLayoutKeepsSlotsRightAligned()
{
    var anchorX = -MonsterRespawnTimerHudLayout.RightMargin;

    AssertEqual(anchorX, MonsterRespawnTimerHudLayout.CalculateSlotX(anchorX, 0), "slot 0 x");
    AssertEqual(anchorX, MonsterRespawnTimerHudLayout.CalculateSlotX(anchorX, 1), "slot 1 x");
    AssertEqual(anchorX, MonsterRespawnTimerHudLayout.CalculateSlotX(anchorX, 7), "slot 7 x");
}

static void HudLayoutStacksSlotsUpward()
{
    AssertEqual(MonsterRespawnTimerHudLayout.BottomMargin, MonsterRespawnTimerHudLayout.CalculateSlotY(MonsterRespawnTimerHudLayout.BottomMargin, 0), "slot 0 y");
    AssertEqual(27f, MonsterRespawnTimerHudLayout.CalculateSlotY(MonsterRespawnTimerHudLayout.BottomMargin, 1), "slot 1 y");
    AssertEqual(174f, MonsterRespawnTimerHudLayout.CalculateSlotY(MonsterRespawnTimerHudLayout.BottomMargin, 8), "slot 8 y");
    AssertEqual(216f, MonsterRespawnTimerHudLayout.CalculateSlotY(MonsterRespawnTimerHudLayout.BottomMargin, 10), "slot 10 y");
}

static void RespectsFlashThresholdBoundary()
{
    var tracker = new EnemySlotTracker();
    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 20f),
    }, elapsedSeconds: 0f);

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 17.76f),
    }, elapsedSeconds: 0.25f);

    AssertEqual("", tracker.Slots[0].FlashText, "just under threshold flash");

    tracker.Reset();
    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 20f),
    }, elapsedSeconds: 0f);

    tracker.Update(new[]
    {
        new EnemySnapshot("enemy-0", "Enemy 0", false, 17.75f),
    }, elapsedSeconds: 0.25f);

    AssertEqual("-2s", tracker.Slots[0].FlashText, "exact threshold flash");
}

static EnemySnapshot[] MakeSnapshots(int count)
{
    return Enumerable.Range(0, count)
        .Select(index => new EnemySnapshot(
            $"enemy-{index}",
            $"Enemy {index}",
            true,
            0f))
        .ToArray();
}

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{label}: expected '{expected}', got '{actual}'");
    }
}

static void AssertSequence<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string label)
{
    if (expected.Count != actual.Count)
    {
        throw new InvalidOperationException($"{label}: expected count '{expected.Count}', got '{actual.Count}'");
    }

    for (var index = 0; index < expected.Count; index++)
    {
        AssertEqual(expected[index], actual[index], $"{label} item {index}");
    }
}
