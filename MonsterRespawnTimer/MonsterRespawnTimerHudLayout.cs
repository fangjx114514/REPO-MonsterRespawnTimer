namespace MonsterRespawnTimer;

internal static class MonsterRespawnTimerHudLayout
{
    public const float NameColumnMinWidth = 88f;
    public const float NameColumnMaxWidth = 154f;
    public const float StatusColumnWidth = 154f;
    public const float NameStatusGap = 4f;
    public const float FlashTimerGapEm = 0.5f;
    public const float SlotWidth = StatusColumnWidth + NameStatusGap + NameColumnMaxWidth;
    public const float SlotHeight = 20f;
    public const float SlotGap = 1f;
    public const float RightMargin = 14f;
    public const float BottomMargin = 6f;
    public const float NameFontSize = 16f;
    public const float StatusFontSize = 15.5f;

    public static float CalculateSlotX(float startX, int index)
    {
        return startX;
    }

    public static float CalculateSlotY(float bottomY, int index)
    {
        return bottomY + index * (SlotHeight + SlotGap);
    }
}
