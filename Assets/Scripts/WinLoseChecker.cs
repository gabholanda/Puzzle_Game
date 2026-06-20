public enum MoveResult { InProgress, Win, Lose }

public readonly struct MoveOutcome
{
    public readonly int MovesLeft;
    public readonly int MaxMoves;
    public readonly MoveResult Result;

    public MoveOutcome(int movesLeft, int maxMoves, MoveResult result)
    {
        MovesLeft = movesLeft;
        MaxMoves = maxMoves;
        Result = result;
    }
}

public class WinLoseChecker
{
    public int MaxMoves { get; private set; }
    public int MovesUsed { get; private set; }
    public int MovesLeft => MaxMoves - MovesUsed;
    public bool IsGameOver { get; private set; }

    public WinLoseChecker(int maxMoves)
    {
        MaxMoves = maxMoves;
        Reset();
    }

    public void Reset()
    {
        MovesUsed = 0;
        IsGameOver = false;
    }

    public MoveOutcome RecordMove(bool wasSolved)
    {
        MovesUsed++;
        MoveResult result =
            wasSolved              ? MoveResult.Win  :
            MovesUsed >= MaxMoves  ? MoveResult.Lose :
                                     MoveResult.InProgress;
        if (result != MoveResult.InProgress) IsGameOver = true;
        return new MoveOutcome(MovesLeft, MaxMoves, result);
    }
}
