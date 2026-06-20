using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [SerializeField] LayoutConfig layout = new LayoutConfig();
    [SerializeField] int maxMoves = 50;
    [SerializeField] string startSceneName = "StartScene";
    [SerializeField] GameHud hud;

    PieceManager manager;
    WinLoseChecker checker;

    void Start()
    {
        hud.Build(layout, OnMenu, StartNewGame, StartNewGame);
        checker = new WinLoseChecker(maxMoves);
        manager = new PieceManager(hud.BoardRect, layout, OnTileCommit);
        StartNewGame();
    }

    public void StartNewGame()
    {
        checker.Reset();
        hud.HideEndPanels();
        hud.SetMovesDisplay(checker.MovesLeft, checker.MaxMoves, animate: false);
        manager.ResetAndShuffle();
        manager.SetAllInteractive(true);
    }

    public bool OnTileCommit(PuzzleTile tile)
    {
        if (checker.IsGameOver) return false;
        if (!manager.IsAdjacentToEmpty(tile)) return false;

        manager.SwapTileWithEmpty(tile);
        var outcome = checker.RecordMove(manager.IsSolved());
        hud.SetMovesDisplay(outcome.MovesLeft, outcome.MaxMoves, animate: true);

        if (outcome.Result == MoveResult.Win)
        {
            hud.ShowWin();
            manager.SetAllInteractive(false);
        }
        else if (outcome.Result == MoveResult.Lose)
        {
            hud.ShowLose();
            manager.SetAllInteractive(false);
        }
        return true;
    }

    void OnMenu()
    {
        SceneManager.LoadScene(startSceneName);
    }
}
