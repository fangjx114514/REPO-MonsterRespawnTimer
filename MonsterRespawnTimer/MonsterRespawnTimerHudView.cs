using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

namespace MonsterRespawnTimer;

internal sealed class MonsterRespawnTimerHudView
{
    private static readonly Color NameColor = new(0.79f, 0.91f, 0.90f, 1f);
    private static readonly Color TimerColor = new(1f, 0.72f, 0.28f, 1f);
    private static readonly Color FlashColor = new(0.45f, 0.9f, 1f, 1f);

    private readonly List<SlotView> slots = new(EnemySlotTracker.MaxSlots);
    private TextMeshProUGUI? probeText;
    private GameObject? root;

    public bool IsAlive => root != null;

    public static bool TryCreate(out MonsterRespawnTimerHudView view)
    {
        view = new MonsterRespawnTimerHudView();
        return view.Create();
    }

    public void Render(IReadOnlyList<EnemySlotState> slotStates)
    {
        if (root == null)
        {
            return;
        }

        if (probeText != null)
        {
            probeText.text = MonsterRespawnTimerHudStatus.ProbeText;
            probeText.gameObject.SetActive(MonsterRespawnTimerHudStatus.ShouldShowProbe(slotStates));
        }

        var nameColumnWidth = CalculateNameColumnWidth(slotStates);

        for (var index = 0; index < slots.Count; index++)
        {
            var hasState = index < slotStates.Count && slotStates[index].IsAssigned && slotStates[index].IsVisible;
            slots[index].Root.SetActive(hasState);
            if (!hasState)
            {
                continue;
            }

            var state = slotStates[index];
            slots[index].SetNameColumnWidth(nameColumnWidth);
            slots[index].NameText.text = state.DisplayName;
            slots[index].StatusText.text = BuildStatusRichText(state);
            slots[index].StatusText.gameObject.SetActive(!string.IsNullOrEmpty(state.StatusText));
        }
    }

    public void Destroy()
    {
        if (root != null)
        {
            UnityEngine.Object.Destroy(root);
            root = null;
        }

        slots.Clear();
        probeText = null;
    }

    private bool Create()
    {
        var parent = FindHudParent();
        if (parent == null)
        {
            return false;
        }

        var font = FindHudFont();
        if (font == null)
        {
            return false;
        }

        root = new GameObject("MonsterRespawnTimer HUD", typeof(RectTransform));
        root.SetActive(true);
        root.transform.SetParent(parent, false);
        root.layer = parent.gameObject.layer;

        var rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.localScale = Vector3.one;

        for (var index = 0; index < EnemySlotTracker.MaxSlots; index++)
        {
            slots.Add(CreateSlot(index, font, root.transform));
        }

        probeText = CreateProbe(font, root.transform);

        return true;
    }

    private static Transform? FindHudParent()
    {
        var gameHud = GameObject.Find("Game Hud");
        if (gameHud != null)
        {
            return gameHud.transform;
        }

        return null;
    }

    private static TMP_FontAsset? FindHudFont()
    {
        var taxHaul = GameObject.Find("Tax Haul");
        if (taxHaul != null)
        {
            var taxText = taxHaul.GetComponent<TMP_Text>();
            if (taxText?.font != null)
            {
                return taxText.font;
            }
        }

        if (HealthUI.instance?.uiText?.font != null)
        {
            return HealthUI.instance.uiText.font;
        }

#pragma warning disable CS0618
        return UnityEngine.Object.FindObjectOfType<TextMeshProUGUI>()?.font;
#pragma warning restore CS0618
    }

    private static SlotView CreateSlot(int index, TMP_FontAsset font, Transform parent)
    {
        var slotRoot = new GameObject($"MonsterRespawnTimer Slot {index}", typeof(RectTransform));
        slotRoot.transform.SetParent(parent, false);
        slotRoot.layer = parent.gameObject.layer;

        var slotRect = (RectTransform)slotRoot.transform;
        slotRect.anchorMin = new Vector2(1f, 0f);
        slotRect.anchorMax = new Vector2(1f, 0f);
        slotRect.pivot = new Vector2(1f, 0f);
        slotRect.anchoredPosition = new Vector2(
            MonsterRespawnTimerHudLayout.CalculateSlotX(-MonsterRespawnTimerHudLayout.RightMargin, index),
            MonsterRespawnTimerHudLayout.CalculateSlotY(MonsterRespawnTimerHudLayout.BottomMargin, index));
        slotRect.sizeDelta = new Vector2(MonsterRespawnTimerHudLayout.SlotWidth, MonsterRespawnTimerHudLayout.SlotHeight);
        slotRect.localScale = Vector3.one;

        var nameText = CreateRightAnchoredText(
            "Name",
            font,
            slotRoot.transform,
            Vector2.zero,
            new Vector2(MonsterRespawnTimerHudLayout.NameColumnMaxWidth, MonsterRespawnTimerHudLayout.SlotHeight),
            NameColor,
            MonsterRespawnTimerHudLayout.NameFontSize);
        var statusText = CreateRightAnchoredText(
            "Status",
            font,
            slotRoot.transform,
            new Vector2(-(MonsterRespawnTimerHudLayout.NameColumnMinWidth + MonsterRespawnTimerHudLayout.NameStatusGap), 0f),
            new Vector2(MonsterRespawnTimerHudLayout.StatusColumnWidth, MonsterRespawnTimerHudLayout.SlotHeight),
            TimerColor,
            MonsterRespawnTimerHudLayout.StatusFontSize);

        slotRoot.SetActive(false);
        return new SlotView(slotRoot, nameText, statusText);
    }

    private static TextMeshProUGUI CreateProbe(TMP_FontAsset font, Transform parent)
    {
        var textObject = new GameObject("Probe", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        textObject.layer = parent.gameObject.layer;

        var rect = (RectTransform)textObject.transform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-MonsterRespawnTimerHudLayout.RightMargin, MonsterRespawnTimerHudLayout.BottomMargin);
        rect.sizeDelta = new Vector2(42f, 18f);
        rect.localScale = Vector3.one;

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.color = NameColor;
        text.fontSize = MonsterRespawnTimerHudLayout.NameFontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 8f;
        text.fontSizeMax = MonsterRespawnTimerHudLayout.NameFontSize;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Right;
        text.raycastTarget = false;
        text.text = MonsterRespawnTimerHudStatus.ProbeText;
        return text;
    }

    private static TextMeshProUGUI CreateRightAnchoredText(string name, TMP_FontAsset font, Transform parent, Vector2 position, Vector2 size, Color color, float fontSize)
    {
        var textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        textObject.layer = parent.gameObject.layer;

        var rect = (RectTransform)textObject.transform;
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.color = color;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 9f;
        text.fontSizeMax = fontSize;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.Right;
        text.raycastTarget = false;
        text.richText = true;

        return text;
    }

    private float CalculateNameColumnWidth(IReadOnlyList<EnemySlotState> slotStates)
    {
        var width = MonsterRespawnTimerHudLayout.NameColumnMinWidth;
        var sampleText = slots.FirstOrDefault()?.NameText;
        if (sampleText == null)
        {
            return width;
        }

        foreach (var state in slotStates)
        {
            if (!state.IsAssigned || !state.IsVisible)
            {
                continue;
            }

            var preferredWidth = sampleText.GetPreferredValues(state.DisplayName).x + 2f;
            width = Mathf.Max(width, Mathf.Min(MonsterRespawnTimerHudLayout.NameColumnMaxWidth, preferredWidth));
        }

        return width;
    }

    private static string BuildStatusRichText(EnemySlotState state)
    {
        var timerTexts = state.TimerTexts;
        var flashText = state.FlashText;

        if (timerTexts.Count == 0)
        {
            return string.IsNullOrEmpty(flashText) ? string.Empty : FormatFlash(flashText);
        }

        if (string.IsNullOrEmpty(flashText))
        {
            return string.Join(" ", timerTexts.Select(FormatTimer));
        }

        if (timerTexts.Count == 1)
        {
            return FormatFlash(flashText) + FormatFlashTimerGap() + FormatTimer(timerTexts[0]);
        }

        var parts = new List<string>(timerTexts.Count + 1);
        for (var index = 0; index < timerTexts.Count - 1; index++)
        {
            parts.Add(FormatTimer(timerTexts[index]));
        }

        parts.Add(FormatFlash(flashText) + FormatFlashTimerGap() + FormatTimer(timerTexts[timerTexts.Count - 1]));
        return string.Join(" ", parts);
    }

    private static string FormatFlashTimerGap()
    {
        return "<space=" + MonsterRespawnTimerHudLayout.FlashTimerGapEm.ToString("0.###", CultureInfo.InvariantCulture) + "em>";
    }

    private static string FormatTimer(string text)
    {
        return Colorize(text, TimerColor);
    }

    private static string FormatFlash(string text)
    {
        return Colorize(text, FlashColor);
    }

    private static string Colorize(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }

    private sealed class SlotView
    {
        public SlotView(GameObject root, TextMeshProUGUI nameText, TextMeshProUGUI statusText)
        {
            Root = root;
            NameText = nameText;
            StatusText = statusText;
        }

        public GameObject Root { get; }
        public TextMeshProUGUI NameText { get; }
        public TextMeshProUGUI StatusText { get; }

        public void SetNameColumnWidth(float nameColumnWidth)
        {
            var nameRect = (RectTransform)NameText.transform;
            nameRect.sizeDelta = new Vector2(nameColumnWidth, MonsterRespawnTimerHudLayout.SlotHeight);

            var statusRect = (RectTransform)StatusText.transform;
            statusRect.anchoredPosition = new Vector2(-(nameColumnWidth + MonsterRespawnTimerHudLayout.NameStatusGap), 0f);
        }
    }
}
