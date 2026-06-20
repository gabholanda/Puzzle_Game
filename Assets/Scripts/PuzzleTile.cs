using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class PuzzleTile : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static readonly Color IdleColor = new Color(0.95f, 0.96f, 0.98f);
    public static readonly Color MovableColor = new Color(0.55f, 0.85f, 0.55f);

    public int Number { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }

    private RectTransform rect;
    private PuzzleGame game;
    private Image image;

    private bool dragging;
    private Vector2 startAnchored;
    private Vector2 emptyAnchored;
    private Vector2 axis;
    private float maxDist;
    private Vector2 pressLocal;

    private Coroutine moveRoutine;

    public void Init(int number, PuzzleGame game)
    {
        Number = number;
        this.game = game;
        rect = (RectTransform)transform;
        image = GetComponent<Image>();
    }

    public void SetMovableHighlight(bool movable)
    {
        if (image != null)
            image.color = movable ? MovableColor : IdleColor;
    }

    public void PlaceAt(int row, int col, bool animate)
    {
        Row = row;
        Col = col;
        Vector2 target = game.GridToAnchored(row, col);
        if (animate) AnimateTo(target);
        else SnapTo(target);
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
        float duration = Mathf.Max(0.0001f, game.MoveAnimDuration);
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
        if (!game.CanInteract) return;
        if (!game.IsAdjacentToEmpty(this)) return;

        if (moveRoutine != null) { StopCoroutine(moveRoutine); moveRoutine = null; }
        rect.anchoredPosition = game.GridToAnchored(Row, Col);

        startAnchored = rect.anchoredPosition;
        emptyAnchored = game.GridToAnchored(game.EmptyGrid.x, game.EmptyGrid.y);

        Vector2 delta = emptyAnchored - startAnchored;
        maxDist = delta.magnitude;
        axis = maxDist > 0.001f ? delta / maxDist : Vector2.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            game.BoardRect, data.position, data.pressEventCamera, out pressLocal);

        dragging = true;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData data)
    {
        if (!dragging) return;

        Vector2 currentLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            game.BoardRect, data.position, data.pressEventCamera, out currentLocal);

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
            game.CommitMove(this);
        }
        else
        {
            AnimateTo(startAnchored);
        }
    }
}
