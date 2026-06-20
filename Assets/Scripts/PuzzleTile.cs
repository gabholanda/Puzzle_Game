using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PuzzleTile : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static readonly Color IdleColor    = new Color(0.95f, 0.96f, 0.98f);
    public static readonly Color MovableColor = new Color(0.55f, 0.85f, 0.55f);

    public int Number { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }

    RectTransform rect;
    Image image;

    // Pushed once at Setup.
    RectTransform boardRect;
    float moveAnimDuration;
    Func<PuzzleTile, bool> onCommitRequest;

    // Pushed whenever grid state changes.
    bool interactive;
    bool canMove;
    Vector2 startAnchored;
    Vector2 axis;
    float maxDist;

    bool dragging;
    Vector2 pressLocal;
    Coroutine moveRoutine;

    public void Setup(int number, RectTransform boardRect, float moveAnimDuration,
                      Func<PuzzleTile, bool> onCommitRequest)
    {
        Number = number;
        this.boardRect = boardRect;
        this.moveAnimDuration = moveAnimDuration;
        this.onCommitRequest = onCommitRequest;
        rect = (RectTransform)transform;
        image = GetComponent<Image>();
    }

    public void PlaceAt(int row, int col, Vector2 anchored, bool animate)
    {
        Row = row;
        Col = col;
        startAnchored = anchored;
        if (animate) AnimateTo(anchored);
        else SnapTo(anchored);
    }

    public void SetMovableHighlight(bool movable)
    {
        if (image != null) image.color = movable ? MovableColor : IdleColor;
    }

    public void SetDragConstraints(bool canMove, Vector2 axis, float maxDist)
    {
        this.canMove = canMove;
        this.axis = axis;
        this.maxDist = maxDist;
    }

    public void SetInteractive(bool interactive)
    {
        this.interactive = interactive;
    }

    void SnapTo(Vector2 pos)
    {
        if (moveRoutine != null) { StopCoroutine(moveRoutine); moveRoutine = null; }
        rect.anchoredPosition = pos;
    }

    void AnimateTo(Vector2 pos)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveRoutine(pos));
    }

    IEnumerator MoveRoutine(Vector2 target)
    {
        Vector2 start = rect.anchoredPosition;
        float t = 0f;
        float duration = Mathf.Max(0.0001f, moveAnimDuration);
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            rect.anchoredPosition = Vector2.Lerp(start, target, k);
            yield return null;
        }
        rect.anchoredPosition = target;
        moveRoutine = null;
    }

    public void OnPointerDown(PointerEventData data)
    {
        dragging = false;
        if (!interactive || !canMove) return;

        if (moveRoutine != null) { StopCoroutine(moveRoutine); moveRoutine = null; }
        rect.anchoredPosition = startAnchored;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boardRect, data.position, data.pressEventCamera, out pressLocal);

        dragging = true;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData data)
    {
        if (!dragging) return;

        Vector2 currentLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            boardRect, data.position, data.pressEventCamera, out currentLocal);

        Vector2 dragDelta = currentLocal - pressLocal;
        float projected = Vector2.Dot(dragDelta, axis);
        projected = Mathf.Clamp(projected, 0f, maxDist);
        rect.anchoredPosition = startAnchored + axis * projected;
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (!dragging) return;
        dragging = false;

        float traveled = Vector2.Distance(rect.anchoredPosition, startAnchored);
        if (maxDist > 0f && traveled >= maxDist * 0.5f)
        {
            Vector2 fallback = startAnchored;
            bool accepted = onCommitRequest != null && onCommitRequest(this);
            if (!accepted) AnimateTo(fallback);
        }
        else
        {
            AnimateTo(startAnchored);
        }
    }
}
