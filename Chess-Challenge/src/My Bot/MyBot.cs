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

    int[] pawnValueTable =
    {
         0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 25, 25, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5, -5,-10,  0,  0,-10, -5,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0
    };

    int[] knightValueTable =
    {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50,
    };

    int[] bishopValueTable =
    {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20,
    };

    int[] rookValueTable =
    {
          0,  0,  0,  0,  0,  0,  0,  0,
          5, 10, 10, 10, 10, 10, 10,  5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
          0,  0,  0,  5,  5,  0,  0,  0
    };

    int[] queenValueTable =
    {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    };

    int[] kingValueTable =
    {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20
    };

    int[][] valueTables;

    ChallengeController.MyStats myStats;
    public MyBot(ChallengeController.MyStats stats)
    {
        myStats = stats;
        valueTables = new int[][] { new int[0], pawnValueTable, knightValueTable, bishopValueTable, rookValueTable, queenValueTable, kingValueTable };
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

        PieceList[] allPieceLists = board.GetAllPieceLists();
        foreach (PieceList pieceList in allPieceLists)
        {
            int side = pieceList.IsWhitePieceList ? 1 : -1;

            eval += pieceValues[(int)pieceList.TypeOfPieceInList] * pieceList.Count * side;

            foreach (Piece piece in pieceList)
            {
                int x = piece.Square.File;
                int y = piece.Square.Rank;
                if (pieceList.IsWhitePieceList) y = 7 - y;

                eval += valueTables[(int)piece.PieceType][x + y * 8] * side;
            }
        }

        myStats.PositionsEvaluated++;
        return eval;
    }
}
