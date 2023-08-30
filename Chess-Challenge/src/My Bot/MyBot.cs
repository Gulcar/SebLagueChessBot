using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

public class MyBot : IChessBot
{
    int[] pieceValues = { 0, 10, 30, 30, 50, 90, 900 };

    public Move Think(Board board, Timer timer)
    {
        bool white = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[0];
        int bestEval = white ? int.MinValue : int.MaxValue;

        foreach (Move m in moves)
        {
            board.MakeMove(m);
            int eval = Minimax(board, !white, 3);
            board.UndoMove(m);

            if ((white && eval > bestEval) || (!white && eval < bestEval))
            {
                bestEval = eval;
                bestMove = m;
            }
        }

        return bestMove;
    }

    // TODO: bool white mogoce ne rabis glede na to da board ve ce je IsWhiteToMove
    int Minimax(Board board, bool white, int depth)
    {
        if (depth == 0)
            return Evaluate(board);

        if (board.IsDraw())
            return 0;

        if (board.IsInCheckmate())
            return white ? -100000 : 100000;

        Move[] moves = board.GetLegalMoves();
        int bestEval = white ? int.MinValue : int.MaxValue;

        foreach (Move m in moves)
        {
            board.MakeMove(m);
            int eval = Minimax(board, !white, depth - 1);
            board.UndoMove(m);

            if (white) bestEval = Math.Max(eval, bestEval);
            else bestEval = Math.Min(eval, bestEval);
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

        return eval;
    }
}
