using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBotOne : IChessBot
{
    public Move Think(Board board, Timer timer) => 
        board.GetLegalMoves()
            .Select(move => (
                move, 
                rating: EvaluateMove(board, move, board => 
                    board.GetAllPieceLists()
                        .Sum(
                            pieceList => pieceList.Sum(
                                piece => new int[] { 0, 100, 300, 300, 500, 900, 10000 } [(int)piece.PieceType] * (piece.IsWhite ? 1 : -1)
                            )
                        ) * (board.IsWhiteToMove ? 1 : -1)
                )
            ))
            .OrderByDescending(moveRating => moveRating.rating)
            .Select(moveRating => moveRating.move)
            .FirstOrDefault();

    TResult EvaluateMove<TResult>(Board board, Move move, Func<Board, TResult> evaluate)
    {
        board.MakeMove(move);
        TResult evaluation = evaluate(board);
        board.UndoMove(move);
        return evaluation;
    }
}