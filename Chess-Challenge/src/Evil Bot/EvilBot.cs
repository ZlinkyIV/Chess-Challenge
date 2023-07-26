using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

namespace ChessChallenge.Example
{
    public class EvilBot : IChessBot
    {
        int movesSearched = 0;

        public Move Think(Board board, Timer timer) => GetBestMove(board, 3);
        
        Move GetBestMove(Board board, int depth)
        {
            MoveRating evaluation = Evaluate(board, depth);
            
            movesSearched = 0;

            return evaluation.moves[0];
        }

        MoveRating Evaluate(Board board, int depth, int myBestMove = int.MinValue, int opponentBestMove = int.MaxValue)
        {
            Span<Move> legalMoves = stackalloc Move[256];
            board.GetLegalMovesNonAlloc(ref legalMoves);
        
            if (legalMoves.Length == 0)
            {
                if (board.IsInCheckmate()) return new(int.MinValue);
                if (board.IsDraw()) return new(0);
            }

            if (depth == 1)
            {
                return legalMoves.ToArray()
                    .Select(move => RateMove(board, move))
                    .OrderByDescending(moveRating => moveRating.rating)
                    .First();
            }

            MoveRating best = new(int.MinValue + 1);
            Move moveForBestRating = Move.NullMove;

            foreach (var move in legalMoves)
            {
                board.MakeMove(move); 
                MoveRating moveRating = Evaluate(board, depth - 1, -opponentBestMove, -myBestMove);
                board.UndoMove(move);

                moveRating = new MoveRating{rating = moveRating.rating * -1, moves = moveRating.moves};

                if (moveRating.rating > best.rating)
                {
                    best = moveRating;
                    moveForBestRating = move;
                }
                
                // if (moveRating.rating >= opponentBestMove) // return bestOpponentMove;   // Opponent will avoid this branch so there's no use looking down it.
                // if (moveRating.rating > myBestMove) myBestMove = moveRating.rating;
            }
            return best.AddMove(moveForBestRating);
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

            return new MoveRating(rating).AddMove(move);
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

            public MoveRating AddMove(Move move) 
            {
                moves.Insert(0, move);
                return this;
            }
        }
    }
}