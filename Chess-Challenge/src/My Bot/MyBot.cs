using System;
using System.Linq;
using System.Collections.Generic;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer) => GetBestMove(board);
    
    private Move GetBestMove(Board board)
    {
        return board.GetLegalMoves()
            .OrderByDescending(move => GetRatingOfMove(board, move) * (board.IsWhiteToMove ? 1 : -1))
            .FirstOrDefault();
    }

    private int GetBoardRating(Board board)
    {
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        return board.GetAllPieceLists().Sum(
            pieceList => pieceList.Sum(
                piece => pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1)
            )
        );
    }

    private int GetRatingOfMove(Board board, Move move)
    {
        board.MakeMove(move);
        int rating = GetBoardRating(board);
        board.UndoMove(move);

        return rating;
    }

    // private struct Path
    // {
    //     public List<Move> moves;

    //     public Path(Move[] moves) => this.moves = new(moves);
    // }
}