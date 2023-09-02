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
    // upostevanje timerja in glede na timer globina searcha
    // transposition table
    // boljsi evaluation
    // mogoce negamax porabi manj tokenov
    // poglej Board.GetLegalMovesNonAlloc

    int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };

    struct TTEntry
    {
        public ulong Key;
        public short Depth;
        public int Eval;
    }

    const int ttSize = 16_000_000; // 256_000_000 / 16;
    TTEntry[] transpositionTable = new TTEntry[ttSize];

    ChallengeController.MyStats myStats;
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
        int bestEval = white ? int.MinValue : int.MaxValue;

        int alpha = int.MinValue;
        int beta = int.MaxValue;

        if (!prevBest.IsNull)
            SearchMove(prevBest);

        foreach (Move m in moves)
            SearchMove(m);
        
        void SearchMove(Move m)
        {
            board.MakeMove(m);
            int eval = Minimax(board, !white, depth - 1, alpha, beta);
            board.UndoMove(m);

            if ((white && eval > bestEval) || (!white && eval < bestEval))
            {
                bestEval = eval;
                bestMove = m;
            }

            if (white) alpha = Math.Max(eval, alpha);
            else beta = Math.Min(eval, beta);
        }

        myStats.Evaluation = bestEval;
        return bestMove;
    }

    int Minimax(Board board, bool white, int depth, int alpha, int beta)
    {
        ulong ttIndex = board.ZobristKey % ttSize;
        TTEntry ttEntry = transpositionTable[ttIndex];

        if (ttEntry.Key == board.ZobristKey &&
            ttEntry.Depth >= depth)
        {
            myStats.Transpositions++;
            return ttEntry.Eval;
        }

        if (depth == 0)
            return Evaluate(board);

        if (board.IsDraw())
            return 0;

        if (board.IsInCheckmate())
            return white ? int.MinValue : int.MaxValue;

        Move[] moves = board.GetLegalMoves();
        OrderMoves(moves);
        int bestEval = white ? int.MinValue : int.MaxValue;

        foreach (Move m in moves)
        {
            board.MakeMove(m);
            int eval = Minimax(board, !white, depth - 1, alpha, beta);
            board.UndoMove(m);

            if (white)
            {
                bestEval = Math.Max(eval, bestEval);
                alpha = Math.Max(eval, alpha);
            }
            else
            {
                bestEval = Math.Min(eval, bestEval);
                beta = Math.Min(eval, beta);
            }

            if (beta <= alpha)
            {
                myStats.BranchesPrunned++;
                return bestEval;
            }
        }

        ttEntry.Key = board.ZobristKey;
        ttEntry.Depth = (short)depth;
        ttEntry.Eval = bestEval;
        transpositionTable[ttIndex] = ttEntry;

        return bestEval;
    }

    int Evaluate(Board board)
    {
        int eval = 0;
        int side = board.IsWhiteToMove ? 1 : -1;

        for (PieceType i = PieceType.Pawn; i <= PieceType.King; i++)
        {
            eval += board.GetPieceList(i, true).Count * pieceValues[(int)i];
            eval -= board.GetPieceList(i, false).Count * pieceValues[(int)i];
        }

        eval += board.GetLegalMoves().Length * 2 * side;
        board.ForceSkipTurn();
        eval -= board.GetLegalMoves().Length * 2 * side;
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
