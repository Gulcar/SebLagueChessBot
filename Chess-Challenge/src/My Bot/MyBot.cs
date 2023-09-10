using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

using ChessChallenge.Application;
using System.ComponentModel;

public class MyBot : IChessBot
{
    // TODO:
    // searcha se naprej ce so na voljo captures
    // veliko boljsi evaluation:
    // - bolj pazi na kralja (castle, preveri napade)
    // - doubled, blocked, isolated pawns
    // mogoce probi nazaj dobit v tt lower in upper bound
    // simplificira naj na koncu ce vodi

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
        myStats.BranchesPruned = 0;
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
                myStats.BranchesPruned++;
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

    public int Evaluate(Board board)
    {
        int eval = 10;
        int side = board.IsWhiteToMove ? 1 : -1;

        int totalPieceEval = 0;

        for (PieceType i = PieceType.Pawn; i <= PieceType.King; i++)
        {
            int w = board.GetPieceList(i, true).Count * pieceValues[(int)i];
            eval += w * side;

            int b = board.GetPieceList(i, false).Count * pieceValues[(int)i];
            eval -= b * side;

            totalPieceEval += w + b;
        }

        // endgame bo 1 ob totalPieceEval 42250 in 0 ob 43250 vmes bo linearno
        float middlegameWeight = Math.Clamp((totalPieceEval - 42250) / 1000.0f, 0.0f, 1.0f);
        float endgameWeight = 1.0f - middlegameWeight;

        if (middlegameWeight > 0.0f)
        for (int i = 0; i < 2; i++)
        {
            bool white = i == 0 ? board.IsWhiteToMove : !board.IsWhiteToMove;
            int side2 = i == 0 ? 1 : -1;

            ulong pawns = board.GetPieceBitboard(PieceType.Pawn, white);
            int add = (int)(20 * side2 * middlegameWeight);
            if ((pawns & 0b00000000_00000000_00000000_00010000_00000000_00000000_00000000_00000000) > 0) eval += add;
            if ((pawns & 0b00000000_00000000_00000000_00001000_00000000_00000000_00000000_00000000) > 0) eval += add;
            if ((pawns & 0b00000000_00000000_00000000_00000000_00010000_00000000_00000000_00000000) > 0) eval += add;
            if ((pawns & 0b00000000_00000000_00000000_00000000_00001000_00000000_00000000_00000000) > 0) eval += add;

            // za podvojene kmete na istem filu
            add /= 2;
            if (BitOperations.PopCount(pawns & 0x8080808080808080) > 1) eval -= add;
            if (BitOperations.PopCount(pawns & 0x4040404040404040) > 1) eval -= add;
            if (BitOperations.PopCount(pawns & 0x2020202020202020) > 1) eval -= add;
            if (BitOperations.PopCount(pawns & 0x1010101010101010) > 1) eval -= add;
            if (BitOperations.PopCount(pawns & 0x0808080808080808) > 1) eval -= add;
            if (BitOperations.PopCount(pawns & 0x0404040404040404) > 1) eval -= add;
            if (BitOperations.PopCount(pawns & 0x0202020202020202) > 1) eval -= add;
            if (BitOperations.PopCount(pawns & 0x0101010101010101) > 1) eval -= add;
        }
        

        if (endgameWeight > 0.0f)
        {
            PieceList pawnList = board.GetPieceList(PieceType.Pawn, true);
            foreach (Piece piece in pawnList)
                eval += (int)((piece.Square.Rank - 3) * 5 * endgameWeight * side);

            pawnList = board.GetPieceList(PieceType.Pawn, false);
            foreach (Piece piece in pawnList)
                eval -= (int)((4 - piece.Square.Rank) * 5 * endgameWeight * side);
        }


        Square whiteKing = board.GetKingSquare(true);
        Square blackKing = board.GetKingSquare(false);

        float wkdistToCenter = Math.Abs(3.5f - whiteKing.Rank) + Math.Abs(3.5f - whiteKing.File);
        eval -= (int)(wkdistToCenter * 5 * side * (endgameWeight * 2f - 1f));

        float bkdistToCenter = Math.Abs(3.5f - blackKing.Rank) + Math.Abs(3.5f - blackKing.File);
        eval += (int)(bkdistToCenter * 5 * side * (endgameWeight * 2f - 1f));

        // TODO: tukaj lahko naredis ubistvu za vse robove ne samo za zacetne pozicije
        ulong knights = board.GetPieceBitboard(PieceType.Knight, true);
        if ((knights & 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000010) > 0) eval -= 15 * side;
        if ((knights & 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_01000000) > 0) eval -= 15 * side;
        knights = board.GetPieceBitboard(PieceType.Knight, false);
        if ((knights & 0b00000010_00000000_00000000_00000000_00000000_00000000_00000000_00000000) > 0) eval += 15 * side;
        if ((knights & 0b01000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000) > 0) eval += 15 * side;


        myStats.PositionsEvaluated++;
        return eval;
    }

    void OrderMoves(Move[] moves)
    {
        int j = 0;

        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].IsCapture || moves[i].IsPromotion)
            {
                (moves[i], moves[j]) = (moves[j], moves[i]);
                //Move m = moves[j];
                //moves[j] = moves[i];
                //moves[i] = m;
                j++;
            }
        }
    }
}
