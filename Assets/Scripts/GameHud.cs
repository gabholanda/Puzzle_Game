using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHud : MonoBehaviour
{
    public RectTransform BoardRect { get; private set; }

    TextMeshProUGUI movesText;
    GameObject winPanel;
    GameObject losePanel;

    enum MovesTier { Safe, Warning, Critical }
    MovesTier currentTier = MovesTier.Safe;
    Coroutine movesAnimRoutine;

    public void Build(LayoutConfig layout, Action onMenu, Action onReset, Action onPlayAgain)
    {
        UIHelpers.EnsureEventSystem();
        var canvas = UIHelpers.CreateCanvas();
        UIHelpers.CreateStretch(canvas.transform, "Background", new Color(0.10f, 0.12f, 0.18f));

        movesText = UIHelpers.CreateText(canvas.transform, "Moves",
            "Moves left: 0", 48, Color.white,
            new Vector2(750, 80), new Vector2(0, 380));

        var menuBtn = UIHelpers.CreateButton(canvas.transform, "MenuButton",
            "Menu", new Color(0.35f, 0.38f, 0.45f),
            new Vector2(200, 80), new Vector2(-380, 460));
        menuBtn.onClick.AddListener(() => onMenu?.Invoke());

        var resetBtn = UIHelpers.CreateButton(canvas.transform, "ResetButton",
            "Reset", new Color(0.35f, 0.38f, 0.45f),
            new Vector2(200, 80), new Vector2(380, 460));
        resetBtn.onClick.AddListener(() => onReset?.Invoke());

        const int kBoardSide = 3;
        float boardSize = kBoardSide * layout.cellSize + (kBoardSide - 1) * layout.tileSpacing + 30f;
        var boardPanel = UIHelpers.CreatePanel(canvas.transform, "Board",
            new Color(0.18f, 0.20f, 0.26f),
            new Vector2(boardSize, boardSize), new Vector2(0, layout.boardCenterY));
        BoardRect = boardPanel.GetComponent<RectTransform>();

        winPanel  = BuildEndPanel(canvas.transform, "WinPanel",  "You Win!",       new Color(0.25f, 0.75f, 0.45f), onPlayAgain);
        losePanel = BuildEndPanel(canvas.transform, "LosePanel", "Out of Moves!",  new Color(0.85f, 0.35f, 0.35f), onPlayAgain);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }

    GameObject BuildEndPanel(Transform parent, string name, string title, Color accent, Action onPlayAgain)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        var card = UIHelpers.CreatePanel(go.transform, "Card",
            new Color(0.16f, 0.18f, 0.24f),
            new Vector2(820, 620), Vector2.zero);

        UIHelpers.CreateText(card.transform, "Title", title, 120, accent,
            new Vector2(780, 240), new Vector2(0, 120));

        var againBtn = UIHelpers.CreateButton(card.transform, "PlayAgainBtn",
            "Play Again?", accent,
            new Vector2(520, 160), new Vector2(0, -120));
        againBtn.onClick.AddListener(() => onPlayAgain?.Invoke());

        return go;
    }

    public void ShowWin()        => winPanel.SetActive(true);
    public void ShowLose()       => losePanel.SetActive(true);
    public void HideEndPanels()
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }

    public void SetMovesDisplay(int movesLeft, int maxMoves, bool animate)
    {
        movesText.text = $"Moves left: {movesLeft}";

        float ratio = maxMoves > 0 ? (float)movesLeft / maxMoves : 0f;
        MovesTier newTier;
        Color tierColor;
        if (ratio <= 0.10f)
        {
            newTier = MovesTier.Critical;
            tierColor = new Color(0.95f, 0.30f, 0.30f);
        }
        else if (ratio <= 0.50f)
        {
            newTier = MovesTier.Warning;
            tierColor = new Color(1.00f, 0.85f, 0.20f);
        }
        else
        {
            newTier = MovesTier.Safe;
            tierColor = Color.white;
        }

        bool tierWorsened = newTier > currentTier;
        currentTier = newTier;

        if (!animate)
        {
            if (movesAnimRoutine != null) { StopCoroutine(movesAnimRoutine); movesAnimRoutine = null; }
            movesText.color = tierColor;
            movesText.transform.localScale = Vector3.one;
            return;
        }

        if (movesAnimRoutine != null) StopCoroutine(movesAnimRoutine);
        movesAnimRoutine = StartCoroutine(AnimateMovesText(tierColor, tierWorsened));
    }

    IEnumerator AnimateMovesText(Color targetColor, bool alarm)
    {
        Transform t = movesText.transform;
        float duration  = alarm ? 0.45f : 0.16f;
        float peakScale = alarm ? 1.55f : 1.18f;
        Color startColor = alarm ? Color.white : movesText.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            float s = 1f + (peakScale - 1f) * Mathf.Sin(k * Mathf.PI);
            t.localScale = new Vector3(s, s, 1f);
            if (alarm) movesText.color = Color.Lerp(startColor, targetColor, k);
            yield return null;
        }
        t.localScale = Vector3.one;
        movesText.color = targetColor;
        movesAnimRoutine = null;
    }
}
