using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PieceManager
{
    public const int Size = 3;

    readonly RectTransform boardRect;
    readonly LayoutConfig layout;
    readonly Func<PuzzleTile, bool> onTileCommit;

    readonly PuzzleTile[,] grid = new PuzzleTile[Size, Size];
    readonly List<PuzzleTile> tiles = new List<PuzzleTile>();
    Vector2Int emptyGrid;

    public PieceManager(RectTransform boardRect, LayoutConfig layout,
                        Func<PuzzleTile, bool> onTileCommit)
    {
        this.boardRect = boardRect;
        this.layout = layout;
        this.onTileCommit = onTileCommit;
    }

    public bool IsAdjacentToEmpty(PuzzleTile tile)
    {
        int dr = Mathf.Abs(tile.Row - emptyGrid.x);
        int dc = Mathf.Abs(tile.Col - emptyGrid.y);
        return dr + dc == 1;
    }

    public bool IsSolved()
    {
        for (int r = 0; r < Size; r++)
        for (int c = 0; c < Size; c++)
        {
            bool last = r == Size - 1 && c == Size - 1;
            var t = grid[r, c];
            if (last)
            {
                if (t != null) return false;
            }
            else
            {
                if (t == null) return false;
                if (t.Number != r * Size + c + 1) return false;
            }
        }
        return true;
    }

    public void SwapTileWithEmpty(PuzzleTile tile)
    {
        int oldRow = tile.Row, oldCol = tile.Col;
        int newRow = emptyGrid.x, newCol = emptyGrid.y;
        grid[oldRow, oldCol] = null;
        grid[newRow, newCol] = tile;
        emptyGrid = new Vector2Int(oldRow, oldCol);
        tile.PlaceAt(newRow, newCol, GridToAnchored(newRow, newCol), animate: true);
        RefreshAllTileState();
    }

    public void ResetAndShuffle()
    {
        if (tiles.Count == 0)
        {
            for (int i = 0; i < Size * Size - 1; i++)
                tiles.Add(CreateTile(i + 1));
        }

        for (int r = 0; r < Size; r++)
            for (int c = 0; c < Size; c++)
                grid[r, c] = null;

        for (int i = 0; i < tiles.Count; i++)
        {
            int r = i / Size, c = i % Size;
            grid[r, c] = tiles[i];
            tiles[i].PlaceAt(r, c, GridToAnchored(r, c), animate: false);
        }
        emptyGrid = new Vector2Int(Size - 1, Size - 1);

        Shuffle();
        RefreshAllTileState();
    }

    public void SetAllInteractive(bool interactive)
    {
        for (int i = 0; i < tiles.Count; i++)
            tiles[i].SetInteractive(interactive);
    }

    Vector2 GridToAnchored(int row, int col)
    {
        float step = layout.cellSize + layout.tileSpacing;
        float x = (col - 1) * step;
        float y = (1 - row) * step;
        return new Vector2(x, y);
    }

    void RefreshAllTileState()
    {
        Vector2 emptyA = GridToAnchored(emptyGrid.x, emptyGrid.y);
        for (int i = 0; i < tiles.Count; i++)
        {
            var t = tiles[i];
            bool adj = IsAdjacentToEmpty(t);
            Vector2 startA = GridToAnchored(t.Row, t.Col);
            Vector2 delta = emptyA - startA;
            float dist = delta.magnitude;
            Vector2 axis = dist > 0.001f ? delta / dist : Vector2.zero;
            t.SetDragConstraints(adj, axis, dist);
            t.SetMovableHighlight(adj);
        }
    }

    PuzzleTile CreateTile(int number)
    {
        var go = new GameObject("Tile_" + number, typeof(Image), typeof(PuzzleTile));
        go.transform.SetParent(boardRect, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(layout.cellSize, layout.cellSize);
        var img = go.GetComponent<Image>();
        img.color = PuzzleTile.IdleColor;
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
        tmp.fontSize = layout.cellSize * 0.55f;
        tmp.color = new Color(0.12f, 0.14f, 0.22f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;

        var tile = go.GetComponent<PuzzleTile>();
        tile.Setup(number, boardRect, layout.moveAnimDuration, onTileCommit);
        return tile;
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

                lastFrom = CellIndex(emptyGrid);
                SwapWithEmptyNoAnim(pick);
            }
            attempts++;
        } while (IsSolved() && attempts < 20);
    }

    static int CellIndex(Vector2Int p) => p.x * Size + p.y;

    List<Vector2Int> NeighborsOfEmpty()
    {
        var list = new List<Vector2Int>(4);
        int er = emptyGrid.x, ec = emptyGrid.y;
        if (er > 0)        list.Add(new Vector2Int(er - 1, ec));
        if (er < Size - 1) list.Add(new Vector2Int(er + 1, ec));
        if (ec > 0)        list.Add(new Vector2Int(er, ec - 1));
        if (ec < Size - 1) list.Add(new Vector2Int(er, ec + 1));
        return list;
    }

    void SwapWithEmptyNoAnim(Vector2Int pos)
    {
        var t = grid[pos.x, pos.y];
        int er = emptyGrid.x, ec = emptyGrid.y;
        grid[er, ec] = t;
        grid[pos.x, pos.y] = null;
        emptyGrid = pos;
        t.PlaceAt(er, ec, GridToAnchored(er, ec), animate: false);
    }
}
