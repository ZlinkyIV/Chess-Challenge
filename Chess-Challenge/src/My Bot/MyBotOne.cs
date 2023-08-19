using System;
using System.Linq;
using ChessChallenge.API;

public class MyBotOne : IChessBot
{
    public Move Think(Board board, Timer timer) => 
        board.GetLegalMoves()
            .Select(move => (move, score: board.MakeMove(move, board => -Search(board, 2))))
            .MaxBy(moveScore => moveScore.score)
            .move;

    public int Search(Board board, int depth) =>
        depth == 0 
            ? Evaluate(board)
            : board.GetLegalMoves()
                .Select(move => board.MakeMove(move, board => -Search(board, depth - 1)))
                .Max();

    public int Evaluate(Board board) =>
        board.GetAllPieceLists().Sum(
                pieceList => new int[] { 0, 100, 300, 300, 500, 900, 10000 } [(int)pieceList.TypeOfPieceInList] * (pieceList.IsWhitePieceList ? 1 : -1) * pieceList.Count
            ) * (board.IsWhiteToMove ? 1 : -1);
}

public static class ChessChallengeAPIExtensions
{
    public static TResult MakeMove<TResult>(this Board board, Move move, Func<Board, TResult> evaluate)
    {
        board.MakeMove(move);
        TResult evaluation = evaluate(board);
        board.UndoMove(move);
        return evaluation;
    }
}