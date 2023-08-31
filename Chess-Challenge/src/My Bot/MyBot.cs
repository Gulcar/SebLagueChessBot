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

    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 9999 };

    ChallengeController.MyStats myStats;

    public MyBot(ChallengeController.MyStats stats)
    {
        myStats = stats;
    }

    public Move Think(Board board, Timer timer)
    {
        myStats.PositionsEvaluated = 0;
        myStats.BranchesPrunned = 0;

        bool white = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[0];
        int bestEval = white ? int.MinValue : int.MaxValue;

        foreach (Move m in moves)
        {
            board.MakeMove(m);
            int eval = Minimax(board, !white, 4, int.MinValue, int.MaxValue);
            board.UndoMove(m);

            if ((white && eval > bestEval) || (!white && eval < bestEval))
            {
                bestEval = eval;
                bestMove = m;
            }
        }

        myStats.Evaluation = bestEval;
        return bestMove;
    }

    int Minimax(Board board, bool white, int depth, int alpha, int beta)
    {
        if (depth == 0)
            return Evaluate(board);

        if (board.IsDraw())
            return 0;

        if (board.IsInCheckmate())
            return white ? int.MinValue : int.MaxValue;

        Move[] moves = board.GetLegalMoves();
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

        return bestEval;
    }

    int Evaluate(Board board)
    {
        int eval = 0;

        for (PieceType i = PieceType.Pawn; i <= PieceType.King; i++)
        {
            eval += board.GetPieceList(i, true).Count * pieceValues[(int)i];
            eval -= board.GetPieceList(i, false).Count * pieceValues[(int)i];
        }

        myStats.PositionsEvaluated++;
        return eval;
    }
}
