using System;
using System.Linq;
using System.Collections.Generic;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer) => GetBestMove(board, 2);
    
    static Move GetBestMove(Board board, int depth)
    {
        Move bestMove = board.GetLegalMoves()
                .OrderByDescending(move => GetMoveRating(board, move, depth) * (board.IsWhiteToMove ? 1 : -1))
                .FirstOrDefault();

        return bestMove;
    }
    

    static int GetBoardRating(Board board, int depth)
    {
        Move[] legalMoves = board.GetLegalMoves();
    
        if (legalMoves.Length == 0)
        {
            if (board.IsInCheckmate()) return -10000;
            if (board.IsDraw()) return 0;
        }

        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
        if (depth == 0)
            return board.GetAllPieceLists().Sum(
                pieceList => pieceList.Sum(
                    piece => pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1)
                )
            ) * (board.IsWhiteToMove ? 1 : -1);     // Rating is always from perspective of player making the next move

        return legalMoves
            .Select(move => { 
                board.MakeMove(move); 
                int rating = -GetBoardRating(board, depth - 1); 
                board.UndoMove(move); 
                return rating;
            })
            .Max();
    }

    static int GetMoveRating(Board board, Move move, int depth)
    {
        board.MakeMove(move);
        int rating = GetBoardRating(board, depth);
        board.UndoMove(move);

        return rating;
    }
    // static int GetMoveRating(Board board, Move[] moves)
    // {
    //     foreach (Move move in moves)
    //         board.MakeMove(move);

    //     int rating = GetBoardRating(board);

    //     foreach (Move move in moves.Reverse())
    //         board.UndoMove(move);

    //     return rating;
    // }
}