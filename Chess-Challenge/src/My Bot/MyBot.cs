﻿using System;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    int movesSearched = 0;

    public Move Think(Board board, Timer timer)
    {
        MoveScore moveScore = Search(board, 4);
        Move move = moveScore.move;
        Console.WriteLine($"Positions searched: {movesSearched} \t  Score: {moveScore.score}");
        movesSearched = 0;
        return move;
    }

    MoveScore Search(Board board, int depth) => Search(board, depth, new MoveScore(Move.NullMove, int.MinValue + 1), new MoveScore(Move.NullMove, int.MaxValue)); 

    MoveScore Search(Board board, int depth, MoveScore whiteBest, MoveScore blackBest, bool capturesOnly = false)
    {
        if (depth == 0 && !capturesOnly)
            return Search(board, depth, whiteBest, blackBest, capturesOnly: true);

        Move[] legalMoves = OrderedLegalMoves(board, capturesOnly);

        if (legalMoves.Length == 0)
        {
            if (board.IsInCheckmate())
                return new(Move.NullMove, int.MinValue + 1);
            else
                return new(Move.NullMove, 0);
        }

        if (capturesOnly)
        {
            int standPat = Evaluate(board);

            if (standPat >= blackBest.score)
                return blackBest;
            if (standPat > whiteBest.score)
                whiteBest = new(Move.NullMove, standPat);
        }   

        foreach (var move in legalMoves)
        {
            int score = -MakeMove(board, move, board => Search(board, depth - 1, -blackBest, -whiteBest, capturesOnly).score);

            if (score >= blackBest.score)
                return blackBest.move == Move.NullMove
                       ? new(legalMoves[0], blackBest.score)
                       : blackBest;
            if (score > whiteBest.score)
                whiteBest = new(move, score);
        }

        return whiteBest.move == Move.NullMove
               ? new(legalMoves[0], whiteBest.score)
               : whiteBest;
    }

    MoveScore TestMove(MoveScore moveScore, Board board) => new(new("a6h7", board), moveScore.score);

    Move[] OrderedLegalMoves(Board board, bool capturesOnly = false)
    {
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 0 };

        return board.GetLegalMoves(capturesOnly)
            .Select(move => (move, score: RateMove(move)))
            .OrderByDescending(moveScore => moveScore.score)
            .Select(moveScore => moveScore.move)
            .ToArray();

        int RateMove(Move move)
        {
            // return MakeMove(board, move, board => -Evaluate(board));
            return (move.IsCapture ? pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType] : 0)
                 + (move.IsPromotion ? pieceValues[(int)move.PromotionPieceType] : 0)
                 - (board.SquareIsAttackedByOpponent(move.TargetSquare) ? pieceValues[(int)move.MovePieceType] : 0);
        }
    }

    int Evaluate(Board board)
    {
        movesSearched += 1;

        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 0 };
        return board.GetAllPieceLists().Sum(
                pieceList => pieceList.Sum(
                    piece => pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1)
                )
            ) * (board.IsWhiteToMove ? 1 : -1)     // Rating is always from perspective of player making the next move
            + board.GetLegalMoves().Count() * 10;
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