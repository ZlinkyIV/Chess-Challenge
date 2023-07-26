using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    int movesSearched = 0;

    public Move Think(Board board, Timer timer) => GetBestMove(board, 4);
    
    Move GetBestMove(Board board, int depth)
    {
        MoveRating evaluation = Evaluate(board, depth, new(int.MinValue + 1), new(int.MaxValue));   // "MinValue + 1" == NO TOUCHY! -- https://stackoverflow.com/questions/3622347/1-int-minvalue-int-minvalue-is-this-a-bug

        Console.WriteLine($"Searched {movesSearched} moves");
        Console.WriteLine($"Rating: {evaluation.rating}");
        Console.WriteLine($"Best path: {string.Concat(evaluation.moves.Select(m => $"{m.ToString()[6..]} "))}");
        Console.WriteLine("");
        
        movesSearched = 0;

        return evaluation.moves[0];
    }

    MoveRating Evaluate(Board board, int depth, MoveRating myBestMove, MoveRating opponentBestMove)
    {
        Span<Move> legalMoves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref legalMoves);

        // if (depth == 4) Console.WriteLine("Possible moves: " + string.Concat(legalMoves.ToArray().Select(m => $"{m.ToString()[6..]}  ")));
    
        if (legalMoves.Length == 0)
        {
            if (board.IsInCheckmate()) return new(int.MinValue);
            if (board.IsDraw()) return new(0);

            Console.WriteLine($"There are no possible moves on this board and no reason why: {board.GetFenString()}");
        }

        if (depth == 1)
        {
            return legalMoves.ToArray()
                .Select(move => RateMove(board, move))
                .OrderByDescending(moveRating => moveRating.rating)
                .First();
        }

        MoveRating best = new(int.MinValue);    // "MinValue + 1" == NO TOUCHY! -- https://stackoverflow.com/questions/3622347/1-int-minvalue-int-minvalue-is-this-a-bug
        Move moveForBestRating = Move.NullMove;

        foreach (var move in legalMoves)
        {
            board.MakeMove(move); 
            MoveRating moveRating = -Evaluate(board, depth - 1, -opponentBestMove, -myBestMove);
            board.UndoMove(move);

            // if (depth == 4) Console.WriteLine($"{move.ToString()[6..]}  {moveRating.rating}  {string.Concat(moveRating.moves.ToArray().Select(m => $"{m.ToString()[6..]}  "))}");

            if (moveRating.rating > best.rating)
            {
                best = moveRating;
                moveForBestRating = move;
            }
            
            if (moveRating.rating >= opponentBestMove.rating) return opponentBestMove;      // Opponent will avoid this branch, so there's no use looking down it.
            if (moveRating.rating > myBestMove.rating) myBestMove = moveRating;
        }

        best.AddMove(moveForBestRating);
        return best;
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

    struct MoveRating
    {
        public List<Move> moves;
        public int rating;

        public MoveRating(int rating)
        {
            this.moves = new();
            this.rating = rating;
        }

        public void AddMove(Move move) 
        {
            moves.Insert(0, move);
        }

        public static MoveRating operator -(MoveRating moveRating) => new() { rating = -moveRating.rating, moves = moveRating.moves };
    }
}