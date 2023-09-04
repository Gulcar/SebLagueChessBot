using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

using ChessChallenge.Application;

public class MyBot : IChessBot
{
    // TODO:
    // searcha se naprej ce so na voljo captures
    // veliko boljsi evaluation
    // mogoce probi nazaj dobit v tt lower in upper bound

    int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };

    const int infinity = 1_000_000_000;

    struct TTEntry
    {
        public uint Key;
        public byte Depth;
        public int Eval;
    }

    const int ttSize = 21_333_333; // 256_000_000 / sizeof(TTEntry)(12);
    TTEntry[] transpositionTable = new TTEntry[ttSize];

    ChallengeController.MyStats myStats;
    bool printMoves = false;
    public MyBot(ChallengeController.MyStats stats)
    {
        myStats = stats;
    }

    public Move Think(Board board, Timer timer)
    {
        myStats.PositionsEvaluated = 0;
        myStats.BranchesPrunned = 0;
        myStats.Transpositions = 0;

        Move move = Move.NullMove;

        for (int depth = 1; depth < 256; depth++)
        {
            move = Search(board, depth, move);

            myStats.DepthSearched = depth;

            if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 120)
                break;
        }

        return move;
    }

    Move Search(Board board, int depth, Move prevBest)
    {
        bool white = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();
        OrderMoves(moves);
        Move bestMove = moves[0];
        int bestEval = -infinity;

        int alpha = -infinity;
        int beta = infinity;

        if (printMoves)
            Console.WriteLine($"\n\npossible moves (depth {depth}):");

        if (!prevBest.IsNull)
            SearchMove(prevBest);

        foreach (Move m in moves)
            SearchMove(m);

        void SearchMove(Move m)
        {
            board.MakeMove(m);
            int eval = -Negamax(board, !white, depth - 1, -beta, -alpha);
            board.UndoMove(m);

            if (printMoves)
                Console.WriteLine($"{m} -> {eval}");

            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = m;
            }

            if (eval > alpha)
            {
                alpha = eval;
            }
        }

        myStats.Evaluation = bestEval;
        return bestMove;
    }

    // https://en.wikipedia.org/wiki/Negamax
    int Negamax(Board board, bool white, int depth, int alpha, int beta)
    {
        int alphaOg = alpha;

        ulong ttIndex = board.ZobristKey % ttSize;
        TTEntry ttEntry = transpositionTable[ttIndex];

        if (ttEntry.Key == (uint)board.ZobristKey &&
            ttEntry.Depth >= depth)
        {
            myStats.Transpositions++;
            return ttEntry.Eval;
        }

        if (board.IsInCheckmate())
            return -infinity;

        if (board.IsDraw())
            return 0;

        if (depth == 0)
            return Evaluate(board);

        Move[] moves = board.GetLegalMoves();
        OrderMoves(moves);
        int bestEval = -infinity;

        foreach (Move m in moves)
        {
            board.MakeMove(m);
            int eval = -Negamax(board, !white, depth - 1, -beta, -alpha);
            board.UndoMove(m);

            if (eval > bestEval)
            {
                bestEval = eval;
            }

            if (eval > alpha)
            {
                alpha = eval;
            }

            if (alpha >= beta)
            {
                myStats.BranchesPrunned++;
                break;
            }
        }

        if (bestEval > alphaOg && bestEval < beta)
        {
            ttEntry.Key = (uint)board.ZobristKey;
            ttEntry.Depth = (byte)depth;
            ttEntry.Eval = bestEval;
            transpositionTable[ttIndex] = ttEntry;
        }

        return bestEval;
    }

    int Evaluate(Board board)
    {
        int eval = 0;
        int side = board.IsWhiteToMove ? 1 : -1;

        for (PieceType i = PieceType.Pawn; i <= PieceType.King; i++)
        {
            eval += board.GetPieceList(i, true).Count * pieceValues[(int)i] * side;
            eval -= board.GetPieceList(i, false).Count * pieceValues[(int)i] * side;
        }

        eval += board.GetLegalMoves().Length * 2;
        board.ForceSkipTurn();
        eval -= board.GetLegalMoves().Length * 2;
        board.UndoSkipTurn();

        myStats.PositionsEvaluated++;
        return eval;
    }

    void OrderMoves(Move[] moves)
    {
        int j = 0;

        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].IsPromotion || moves[i].IsCapture)
            {
                Move m = moves[j];
                moves[j] = moves[i];
                moves[i] = m;
                j++;
            }
        }
    }
}
