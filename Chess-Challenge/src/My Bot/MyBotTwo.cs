using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBoTwo : IChessBot
{
    int movesSearched = 0;

    public Move Think(Board board, Timer timer) => GetBestMove(board, 1);
    
    Move GetBestMove(Board board, int depth)
    {
        MoveRating evaluation = Evaluate(board, depth, new(int.MinValue + 1), new(int.MaxValue));   // "MinValue + 1" == NO TOUCHY! -- https://stackoverflow.com/questions/3622347/1-int-minvalue-int-minvalue-is-this-a-bug

        Console.WriteLine($"Searched {movesSearched} moves");
        Console.WriteLine($"Rating: {evaluation.rating}");
        Console.WriteLine($"Best path: {string.Concat(evaluation.moves.Select(m => $"{m.ToString()[6..]} "))}");
        Console.WriteLine($"Last move is capture: {evaluation.moves[^1].IsCapture}");
        Console.WriteLine("");
        
        movesSearched = 0;

        return evaluation.moves[0];
    }

    MoveRating Evaluate(Board board, int depth, MoveRating myBestMove, MoveRating opponentBestMove, bool quiescenceSearch = false)
    {
        if (!quiescenceSearch && depth == 0)
            return Evaluate(board, depth, myBestMove, opponentBestMove, true);

        Span<Move> legalMovesSpan = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref legalMovesSpan, quiescenceSearch);
        // Move[] legalMoves = OrderMoves(board, legalMovesSpan.ToArray());
        Move[] legalMoves = legalMovesSpan.ToArray();

        // if (depth == 4) Console.WriteLine("Possible moves: " + string.Concat(legalMoves.Select(m => $"{m.ToString()[6..]}  ")));

        // if (quiescenceSearch) Console.WriteLine($"Depth: {depth}  Count: {legalMoves.Length}");
    
        if (legalMoves.Length == 0)
        {
            if (board.IsInCheckmate()) return new(int.MinValue + 1);    // "MinValue + 1" == NO TOUCHY! -- https://stackoverflow.com/questions/3622347/1-int-minvalue-int-minvalue-is-this-a-bug
            if (board.IsDraw()) return new(0);

            if (quiescenceSearch)
                return new(RateBoard(board));
        }

        if (quiescenceSearch)
        {
            MoveRating standPat = new(RateBoard(board));

            if (standPat.rating >= opponentBestMove.rating) return opponentBestMove;
            if (standPat.rating > myBestMove.rating)  myBestMove = standPat;
        }

        Move moveForBestRating = Move.NullMove;
        foreach (var move in legalMoves)
        {
            board.MakeMove(move); 
            MoveRating moveEvaluation = -Evaluate(board, depth - 1, -opponentBestMove, -myBestMove, quiescenceSearch);
            board.UndoMove(move);

            // if (depth == 4) Console.WriteLine($"{move.ToString()[6..]}  {moveRating.rating}  {string.Concat(moveRating.moves.ToArray().Select(m => $"{m.ToString()[6..]}  "))}");

            if (moveEvaluation.rating >= opponentBestMove.rating) return opponentBestMove;    // Opponent will avoid this branch, so there's no use looking down it.
            if (moveEvaluation.rating > myBestMove.rating)
            {
                myBestMove = moveEvaluation;
                moveForBestRating = move;
            }
        }

        if (moveForBestRating != Move.NullMove)
            myBestMove.AddMove(moveForBestRating);

        return myBestMove;
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

    MoveRating RateMove(Board board, Move move)
    {
        movesSearched += 1;

        board.MakeMove(move); 

        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
        int rating = board.GetAllPieceLists().Sum(
                pieceList => pieceList.Sum(
                    piece => pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1)
                )
            ) * (board.IsWhiteToMove ? -1 : 1);     // Rating is always from perspective of player making the next move

        board.UndoMove(move);

        MoveRating moveRating = new(rating);
        moveRating.AddMove(move);
        return moveRating;
    }

    int RateBoard(Board board)
    {
        movesSearched += 1;

        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
        return board.GetAllPieceLists().Sum(
                pieceList => pieceList.Sum(
                    piece => pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1)
                )
            ) * (board.IsWhiteToMove ? 1 : -1);     // Rating is always from perspective of player making the next move        
    }

    struct MoveRating
    {
        public List<Move> moves = new();
        public int rating;

        public MoveRating(int rating, Move? move = null)
        {
            this.rating = rating;

            if (move != null)
                moves.Add((Move)move);
        }

        public void AddMove(Move move) 
        {
            moves.Insert(0, move);
        }

        public static MoveRating operator -(MoveRating moveRating) => new() { rating = -moveRating.rating, moves = moveRating.moves };
    }
}