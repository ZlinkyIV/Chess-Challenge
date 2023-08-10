using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    int movesSearched = 0;

    public Move Think(Board board, Timer timer)
    {
        Move move = Search(board, 3).move;
        Console.WriteLine($"Positions searched: {movesSearched}");
        return move;
    }

    MoveScore Search(Board board, int depth) => Search(board, depth, new MoveScore(Move.NullMove, int.MinValue + 1), new MoveScore(Move.NullMove, int.MaxValue)); 

    MoveScore Search(Board board, int depth, MoveScore whiteBest, MoveScore blackBest)
    {
        foreach (var move in board.GetLegalMoves())
        {
            int score = depth == 0 
                        ? Quiescence(board, whiteBest.score, blackBest.score)
                        : MakeMove(board, move, (board) => -Search(board, depth - 1, -blackBest, -whiteBest).score);

            if (score > blackBest.score)
                return blackBest;
            if (score > whiteBest.score)
                whiteBest = new(move, score);
        }
        return whiteBest;
    }

    int Quiescence(Board board, int whiteBest, int blackBest)
    {
        int standPat = Evaluate(board);

        if (standPat >= blackBest)
            return blackBest;
        if (whiteBest < standPat)
            whiteBest = standPat;

        foreach (var capture in board.GetLegalMoves(true))
        {
            int score = MakeMove(board, capture, (board) => -Quiescence(board, -blackBest, -whiteBest));

            if (score >= blackBest)
                return blackBest;
            if ( score > whiteBest)
            whiteBest = score;
        }

        return whiteBest;
    }

    Move[] OrderMoves(Board board, Move[] moves)
    {
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        return 
            moves
            .Select(move => Tuple.Create(move, (
                  (move.IsCapture 
                   ? pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType] 
                   : 0)
                + (move.IsPromotion 
                   ? pieceValues[(int)move.PromotionPieceType]
                   : 0)
                - (board.SquareIsAttackedByOpponent(move.TargetSquare) 
                   ? pieceValues[(int)move.MovePieceType] 
                   : 0)
            )))
            .OrderByDescending(moveScore => moveScore.Item2)
            .Select(moveScore => moveScore.Item1)
            .ToArray();
    }

    int Evaluate(Board board)
    {
        movesSearched += 1;

        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
        return board.GetAllPieceLists().Sum(
                pieceList => pieceList.Sum(
                    piece => pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1)
                )
            ) * (board.IsWhiteToMove ? 1 : -1);     // Rating is always from perspective of player making the next move        
    }

    TResult MakeMove<TResult>(Board board, Move move, Func<Board, TResult> evaluate)
    {
        board.MakeMove(move);
        TResult evaluation = evaluate(board);
        board.UndoMove(move);
        return evaluation;
    }

    struct MoveScore
    {
        public Move move;
        public int score;

        public MoveScore(Move move, int score)
        {
            this.move = move;
            this.score = score;
        }

        public static MoveScore operator -(MoveScore moveScore) => new(moveScore.move, -moveScore.score);
    }
}