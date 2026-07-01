using System.Collections.Generic;

namespace MonsterRespawnTimer;

internal static class MonsterRespawnTimerHudStatus
{
    public const string ProbeText = "EC";

    public static bool ShouldRenderForSpectateState(bool isSpectating)
    {
        return !isSpectating;
    }

    public static bool ShouldShowProbe(IReadOnlyList<EnemySlotState> slotStates)
    {
        for (var index = 0; index < slotStates.Count; index++)
        {
            if (slotStates[index].IsAssigned && slotStates[index].IsVisible)
            {
                return false;
            }
        }

        return true;
    }
}
