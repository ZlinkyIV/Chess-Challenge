using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer) => 
        board.GetLegalMoves()
            .Select(move => (move, rating: move.IsCapture ? 10 : 0))
            .OrderByDescending(moveRating => moveRating.rating)
            .Select(moveRating => moveRating.move)
            .FirstOrDefault();
}