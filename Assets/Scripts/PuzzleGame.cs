using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PuzzleGame : MonoBehaviour
{
    public const int Size = 3;

    [SerializeField]
    public int maxMoves = 50;

    [Header("Layout")]
    public float cellSize = 200f;
    public float tileSpacing = 6f;
    public float boardCenterY = -60f;

    [Header("Animation")]
    public float moveAnimDuration = 0.18f;

    [Header("Navigation")]
    public string startSceneName = "StartScene";

    private RectTransform boardRect;
    private readonly PuzzleTile[,] grid = new PuzzleTile[Size, Size];
    private readonly List<PuzzleTile> tiles = new List<PuzzleTile>();

    public Vector2Int EmptyGrid { get; private set; }
    public RectTransform BoardRect => boardRect;
    public float MoveAnimDuration => moveAnimDuration;
    public bool CanInteract => !gameEnded;

    private int moveCount;
    private bool gameEnded;

    private TextMeshProUGUI movesText;
    private GameObject winPanel;
    private GameObject losePanel;

    private enum MovesTier { Safe, Warning, Critical }
    private MovesTier currentTier = MovesTier.Safe;
    private Coroutine movesAnimRoutine;

    void Start()
    {
        UIHelpers.EnsureEventSystem();
        BuildUI();
        StartNewGame();
    }

    void BuildUI()
    {
        var canvas = UIHelpers.CreateCanvas();
        UIHelpers.CreateStretch(canvas.transform, "Background", new Color(0.10f, 0.12f, 0.18f));

        movesText = UIHelpers.CreateText(canvas.transform, "Moves",
            "Moves left: 50", 48, Color.white,
            new Vector2(750, 80), new Vector2(0, 460));

        var menuBtn = UIHelpers.CreateButton(canvas.transform, "MenuButton",
            "Menu", new Color(0.35f, 0.38f, 0.45f),
            new Vector2(200, 80), new Vector2(-380, 460));
        menuBtn.onClick.AddListener(() => SceneManager.LoadScene(startSceneName));

        var resetBtn = UIHelpers.CreateButton(canvas.transform, "ResetButton",
            "Reset", new Color(0.35f, 0.38f, 0.45f),
            new Vector2(200, 80), new Vector2(380, 460));
        resetBtn.onClick.AddListener(StartNewGame);

        float boardSize = Size * cellSize + (Size - 1) * tileSpacing + 30f;
        var boardPanel = UIHelpers.CreatePanel(canvas.transform, "Board",
            new Color(0.18f, 0.20f, 0.26f),
            new Vector2(boardSize, boardSize), new Vector2(0, boardCenterY));
        boardRect = boardPanel.GetComponent<RectTransform>();

        winPanel = BuildEndPanel(canvas.transform, "WinPanel", "You Win!", new Color(0.25f, 0.75f, 0.45f));
        winPanel.SetActive(false);

        losePanel = BuildEndPanel(canvas.transform, "LosePanel", "Out of Moves!", new Color(0.85f, 0.35f, 0.35f));
        losePanel.SetActive(false);
    }

    GameObject BuildEndPanel(Transform parent, string name, string title, Color accent)
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

        UIHelpers.CreateText(card.transform, "Title",
            title, 120, accent,
            new Vector2(780, 240), new Vector2(0, 120));

        var againBtn = UIHelpers.CreateButton(card.transform, "PlayAgainBtn",
            "Play Again?", accent,
            new Vector2(520, 160), new Vector2(0, -120));
        againBtn.onClick.AddListener(StartNewGame);

        return go;
    }

    public void StartNewGame()
    {
        moveCount = 0;
        gameEnded = false;
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        UpdateMovesUI();
        InitOrResetTiles();
        Shuffle();
        RefreshMovableHighlights();
    }

    void RefreshMovableHighlights()
    {
        for (int i = 0; i < tiles.Count; i++)
            tiles[i].SetMovableHighlight(IsAdjacentToEmpty(tiles[i]));
    }

    void InitOrResetTiles()
    {
        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                grid[r, c] = null;

        if (tiles.Count == 0)
        {
            for (int i = 0; i < Size * Size - 1; i++)
                tiles.Add(CreateTile(i + 1));
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            int r = i / Size;
            int c = i % Size;
            grid[r, c] = tiles[i];
            tiles[i].PlaceAt(r, c, animate: false);
        }
        EmptyGrid = new Vector2Int(Size - 1, Size - 1);
    }

    PuzzleTile CreateTile(int number)
    {
        var go = new GameObject("Tile_" + number, typeof(Image), typeof(PuzzleTile));
        go.transform.SetParent(boardRect, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(cellSize, cellSize);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.95f, 0.96f, 0.98f);
        img.raycastTarget = true;

        var labelGO = new GameObject("Number", typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;
        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.text = number.ToString();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = cellSize * 0.55f;
        tmp.color = new Color(0.12f, 0.14f, 0.22f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;

        var tile = go.GetComponent<PuzzleTile>();
        tile.Init(number, this);
        return tile;
    }

    public Vector2 GridToAnchored(int row, int col)
    {
        float step = cellSize + tileSpacing;
        float x = (col - 1) * step;
        float y = (1 - row) * step;
        return new Vector2(x, y);
    }

    public bool IsAdjacentToEmpty(PuzzleTile tile)
    {
        int dr = Mathf.Abs(tile.Row - EmptyGrid.x);
        int dc = Mathf.Abs(tile.Col - EmptyGrid.y);
        return dr + dc == 1;
    }

    public void CommitMove(PuzzleTile tile)
    {
        if (gameEnded || !IsAdjacentToEmpty(tile)) return;

        int oldRow = tile.Row;
        int oldCol = tile.Col;
        int newRow = EmptyGrid.x;
        int newCol = EmptyGrid.y;

        grid[oldRow, oldCol] = null;
        grid[newRow, newCol] = tile;
        EmptyGrid = new Vector2Int(oldRow, oldCol);
        tile.PlaceAt(newRow, newCol, animate: true);

        moveCount++;
        UpdateMovesUI();
        RefreshMovableHighlights();

        if (IsSolved())
        {
            gameEnded = true;
            winPanel.SetActive(true);
        }
        else if (moveCount >= maxMoves)
        {
            gameEnded = true;
            losePanel.SetActive(true);
        }
    }

    bool IsSolved()
    {
        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                bool lastCell = (r == Size - 1 && c == Size - 1);
                var t = grid[r, c];
                if (lastCell)
                {
                    if (t != null) return false;
                }
                else
                {
                    if (t == null) return false;
                    if (t.Number != r * Size + c + 1) return false;
                }
            }
        }
        return true;
    }

    void UpdateMovesUI()
    {
        int remaining = Mathf.Max(0, maxMoves - moveCount);
        movesText.text = $"Moves left: {remaining}";

        float ratio = maxMoves > 0 ? (float)remaining / maxMoves : 0f;
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

        if (moveCount == 0)
        {
            if (movesAnimRoutine != null) StopCoroutine(movesAnimRoutine);
            movesText.color = tierColor;
            movesText.transform.localScale = Vector3.one;
            movesAnimRoutine = null;
            return;
        }

        if (movesAnimRoutine != null) StopCoroutine(movesAnimRoutine);
        movesAnimRoutine = StartCoroutine(AnimateMovesText(tierColor, tierWorsened));
    }

    System.Collections.IEnumerator AnimateMovesText(Color targetColor, bool alarm)
    {
        Transform t = movesText.transform;
        float duration = alarm ? 0.45f : 0.16f;
        float peakScale = alarm ? 1.55f : 1.18f;
        Color startColor = alarm ? Color.white : movesText.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            float s = 1f + (peakScale - 1f) * Mathf.Sin(k * Mathf.PI);
            t.localScale = new Vector3(s, s, 1f);
            if (alarm)
                movesText.color = Color.Lerp(startColor, targetColor, k);
            yield return null;
        }
        t.localScale = Vector3.one;
        movesText.color = targetColor;
        movesAnimRoutine = null;
    }

    void Shuffle()
    {
        var rng = new System.Random();
        const int kRandomMoves = 150;
        int attempts = 0;
        do
        {
            int lastFrom = -1;
            for (int i = 0; i < kRandomMoves; i++)
            {
                var options = NeighborsOfEmpty();
                Vector2Int pick;
                int safety = 0;
                do
                {
                    pick = options[rng.Next(options.Count)];
                    safety++;
                } while (CellIndex(pick) == lastFrom && options.Count > 1 && safety < 8);

                lastFrom = CellIndex(EmptyGrid);
                SwapWithEmpty(pick);
            }
            attempts++;
        } while (IsSolved() && attempts < 20);
    }

    static int CellIndex(Vector2Int p) => p.x * Size + p.y;

    List<Vector2Int> NeighborsOfEmpty()
    {
        var list = new List<Vector2Int>(4);
        int er = EmptyGrid.x, ec = EmptyGrid.y;
        if (er > 0) list.Add(new Vector2Int(er - 1, ec));
        if (er < Size - 1) list.Add(new Vector2Int(er + 1, ec));
        if (ec > 0) list.Add(new Vector2Int(er, ec - 1));
        if (ec < Size - 1) list.Add(new Vector2Int(er, ec + 1));
        return list;
    }

    void SwapWithEmpty(Vector2Int pos)
    {
        var t = grid[pos.x, pos.y];
        int er = EmptyGrid.x, ec = EmptyGrid.y;
        grid[er, ec] = t;
        grid[pos.x, pos.y] = null;
        EmptyGrid = pos;
        t.PlaceAt(er, ec, animate: false);
    }
}
