using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Virtual (recycling) grid for a vertical ScrollRect.
//
// Only the slots that are inside the viewport (plus one buffer row on each
// side) exist. Scrolling moves slots that left the viewport to the other end
// and rebinds them, so the number of GameObjects stays constant no matter how
// many items the inventory holds.
//
// It replaces the GridLayoutGroup + ContentSizeFitter on the content object:
// both are disabled in Setup, and this class sets the content height and the
// position of every slot itself, using the same numbers as the old layout
// (cell size, spacing, padding are read from the GridLayoutGroup).
public class InventoryVirtualGrid : MonoBehaviour
{
    private const int BufferRows = 1;

    private ScrollRect scrollRect;
    private RectTransform content;
    private RectTransform viewport;
    private InventorySlotView prefab;
    private Action<int, InventorySlotView> bind;

    private Vector2 cellSize;
    private Vector2 spacing;
    private RectOffset padding;

    private int itemCount;
    private int columns = 1;
    // Bound index range, inclusive. -1 means nothing is bound.
    private int firstIndex = -1;
    private int lastIndex = -1;
    private bool ready;

    private readonly Dictionary<int, InventorySlotView> active = new Dictionary<int, InventorySlotView>();
    private readonly Stack<InventorySlotView> free = new Stack<InventorySlotView>();
    private readonly List<int> releaseBuffer = new List<int>();

    // Slots that are currently visible. Iterating it does not allocate.
    public Dictionary<int, InventorySlotView>.ValueCollection ActiveSlots
    {
        get { return active.Values; }
    }

    // Number of slot GameObjects that were ever created (active + pooled).
    public int CreatedSlotCount
    {
        get { return active.Count + free.Count; }
    }

    public void Setup(InventorySlotView slotPrefab, Action<int, InventorySlotView> bindSlot,
        Vector2 cellSize, Vector2 spacing, RectOffset padding)
    {
        scrollRect = GetComponent<ScrollRect>();
        content = scrollRect.content;
        viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)transform;
        prefab = slotPrefab;
        bind = bindSlot;
        this.cellSize = cellSize;
        this.spacing = spacing;
        this.padding = padding;

        // The grid layout would fight with the manual positions.
        LayoutGroup layoutGroup = content.GetComponent<LayoutGroup>();
        if (layoutGroup != null) layoutGroup.enabled = false;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;

        // Top-stretch content: anchoredPosition.y is the scroll offset (>= 0).
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0f, 1f);

        scrollRect.onValueChanged.AddListener(OnScroll);
        ready = true;
    }

    // Call when the data changed. keepScroll keeps the current scroll offset
    // (clamped to the new content height), otherwise the list jumps to the top.
    public void SetItemCount(int count, bool keepScroll)
    {
        itemCount = count;
        ReleaseAll();
        if (!keepScroll)
        {
            scrollRect.StopMovement();
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
        }
        Relayout();
    }

    // Rebinds the visible slots without changing the layout (data of the
    // items changed, not their number).
    public void RefreshVisible()
    {
        foreach (KeyValuePair<int, InventorySlotView> pair in active)
        {
            bind(pair.Key, pair.Value);
        }
    }

    // Viewport size changed (resolution, window resize): the column count may change.
    private void OnRectTransformDimensionsChange()
    {
        if (!ready || !isActiveAndEnabled) return;
        ReleaseAll();
        Relayout();
    }

    private void OnScroll(Vector2 normalizedPosition)
    {
        UpdateVisible();
    }

    private void Relayout()
    {
        float width = viewport.rect.width;
        if (width <= 0f)
        {
            // Not laid out yet. OnRectTransformDimensionsChange calls us again.
            return;
        }

        // Same formula GridLayoutGroup uses for a flexible constraint.
        columns = Mathf.Max(1, Mathf.FloorToInt(
            (width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));

        int rows = Mathf.CeilToInt(itemCount / (float)columns);
        float height = padding.vertical;
        if (rows > 0)
        {
            height += rows * cellSize.y + (rows - 1) * spacing.y;
        }
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        float maxScroll = Mathf.Max(0f, height - viewport.rect.height);
        Vector2 position = content.anchoredPosition;
        position.y = Mathf.Clamp(position.y, 0f, maxScroll);
        content.anchoredPosition = position;

        UpdateVisible();
    }

    private void UpdateVisible()
    {
        if (!ready || itemCount == 0)
        {
            ReleaseAll();
            return;
        }

        float rowHeight = cellSize.y + spacing.y;
        float scrollY = content.anchoredPosition.y;
        float viewHeight = viewport.rect.height;

        int firstRow = Mathf.Max(0, Mathf.FloorToInt((scrollY - padding.top) / rowHeight) - BufferRows);
        int lastRow = Mathf.FloorToInt((scrollY + viewHeight - padding.top) / rowHeight) + BufferRows;

        int newFirst = firstRow * columns;
        int newLast = Mathf.Min(itemCount - 1, (lastRow + 1) * columns - 1);

        if (newFirst > newLast)
        {
            ReleaseAll();
            return;
        }
        if (newFirst == firstIndex && newLast == lastIndex) return;

        // 1. Recycle slots that left the range.
        releaseBuffer.Clear();
        foreach (KeyValuePair<int, InventorySlotView> pair in active)
        {
            if (pair.Key < newFirst || pair.Key > newLast)
            {
                releaseBuffer.Add(pair.Key);
            }
        }
        for (int i = 0; i < releaseBuffer.Count; i++)
        {
            Release(releaseBuffer[i]);
        }

        // 2. Fill the indices that entered the range.
        for (int index = newFirst; index <= newLast; index++)
        {
            if (active.ContainsKey(index)) continue;

            InventorySlotView view = Acquire();
            Place(view, index);
            active.Add(index, view);
            bind(index, view);
        }

        firstIndex = newFirst;
        lastIndex = newLast;
    }

    private InventorySlotView Acquire()
    {
        InventorySlotView view;
        if (free.Count > 0)
        {
            view = free.Pop();
        }
        else
        {
            view = Instantiate(prefab, content, false);
        }
        view.gameObject.SetActive(true);
        return view;
    }

    private void Release(int index)
    {
        InventorySlotView view = active[index];
        active.Remove(index);
        view.OnRecycle();
        view.gameObject.SetActive(false);
        free.Push(view);
    }

    private void ReleaseAll()
    {
        releaseBuffer.Clear();
        foreach (int index in active.Keys)
        {
            releaseBuffer.Add(index);
        }
        for (int i = 0; i < releaseBuffer.Count; i++)
        {
            Release(releaseBuffer[i]);
        }
        firstIndex = -1;
        lastIndex = -1;
    }

    private void Place(InventorySlotView view, int index)
    {
        RectTransform rect = view.SlotRect;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = cellSize;

        int row = index / columns;
        int column = index % columns;
        rect.anchoredPosition = new Vector2(
            padding.left + column * (cellSize.x + spacing.x),
            -(padding.top + row * (cellSize.y + spacing.y)));
    }
}
