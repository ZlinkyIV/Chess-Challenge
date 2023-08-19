using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    int movesSearched = 0;

    // TranspositionTable transpositionTable;
    // bool createdTranspositionTable = false;

    // void CreateTranspositionTable()
    // {
    //     createdTranspositionTable = true;
    //     transpositionTable = new(16777216);
    // }

    public Move Think(Board board, Timer timer)
    {
        // if (!createdTranspositionTable) CreateTranspositionTable();

        var searchResult = SearchWrapper(board, () => timer.MillisecondsElapsedThisTurn > 750);

        // int score = MakeMove(board, searchResult.move, board => -Evaluate(board));

        // Positions searched: {movesSearched}
        // Transpositions: {transpositionTable.Count} \t Num Overwrites: {transpositionTable.numOverwrites}
        Console.WriteLine($"Depth searched: {searchResult.depth} \t  Time elapsed: {timer.MillisecondsElapsedThisTurn} \t Score: {searchResult.score} \t Number of legal moves: {board.GetLegalMoves().Length}");
        movesSearched = 0;

        return searchResult.move;
    }

    (Move move, int score, int depth) SearchWrapper(Board board, Func<bool> cancelSearch)
    {
        var best = (move: Move.NullMove, score: int.MinValue + 1);
        int maxDepth = 0;

        for (int depth = 1; !cancelSearch(); depth++)
        {
            best = board.GetLegalMoves()
                .Select(move => (move, score: MakeMove(board, move, board => -Search(board, depth - 1, cancelSearch))))
                .MaxBy(moveScore => moveScore.score);
            maxDepth = depth;
        }

        return (best.move, best.score, maxDepth);
    }

    int Search(Board board, int depth, Func<bool> cancelSearch, int whiteBest = int.MinValue + 1, int blackBest = int.MaxValue, bool capturesOnly = false)
    {
        // if (cancelSearch()) return 0;

        if (depth == 0 && !capturesOnly)
            return Search(board, depth, cancelSearch, whiteBest, blackBest, capturesOnly: true);

        Move[] legalMoves = OrderedLegalMoves(board, capturesOnly);

        if (legalMoves.Length == 0)
            return board.IsInCheckmate()
                ? int.MinValue + 1
                : 0;

        if (capturesOnly)
        {
            int standPat = Evaluate(board);

            if (standPat >= blackBest)
            {
                // transpositionTable.Add(board, blackBest);
                return blackBest;
            }
            if (standPat > whiteBest)
                whiteBest = standPat;
        }   

        foreach (var move in legalMoves)
        {
            int score = MakeMove(board, move, board => -Search(board, depth - 1, cancelSearch, -blackBest, -whiteBest, capturesOnly));

            if (score >= blackBest)
            {
                // transpositionTable.Add(board, blackBest);
                return blackBest;
            }
            if (score > whiteBest)
                whiteBest = score;
        }

        // transpositionTable.Add(board, whiteBest);
        return whiteBest;
    }

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
            // return -transpositionTable.Get(MakeMove(board, move, board => board))
            return 
                 + (pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType])
                 + pieceValues[(int)move.PromotionPieceType]
                 - (board.SquareIsAttackedByOpponent(move.TargetSquare) ? pieceValues[(int)move.MovePieceType] : 0);
        }
    }

    int Evaluate(Board board)
    {
        movesSearched += 1;

        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 0 };
        return (
                board.GetAllPieceLists()
                    .Sum(pieceList => pieceValues[(int)pieceList.TypeOfPieceInList] * (pieceList.IsWhitePieceList ? 1 : -1) * pieceList.Count)
                // + board.GetLegalMoves()
                //     .Sum(move => board.GetPiece(move.StartSquare).IsWhite ? 1 : -1)
            ) * (board.IsWhiteToMove ? 1 : -1);     // Rating is always from perspective of player making the next move
    }

    TResult MakeMove<TResult>(Board board, Move move, Func<Board, TResult> evaluate)
    {
        board.MakeMove(move);
        TResult evaluation = evaluate(board);
        board.UndoMove(move);
        return evaluation;
    }

    // struct TranspositionTable
    // {
    //     Dictionary<ulong, int> table;
    //     public int numOverwrites = 0;

    //     public TranspositionTable(int maxSize)
    //     {
    //         table = new(maxSize);
    //     }

    //     public int Count => table.Count;

    //     public void Add(Board board, int value)
    //     {
    //         if (table.ContainsKey(board.ZobristKey)) numOverwrites += 1;
    //         table[board.ZobristKey] = value;
    //     }

    //     public int Get(Board board) => table.GetValueOrDefault(board.ZobristKey);
    // }
}