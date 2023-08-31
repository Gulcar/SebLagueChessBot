using Raylib_cs;
using System.Numerics;

namespace ChessChallenge.Application
{
    public static class MatchStatsUI
    {
        public static void DrawMatchStats(ChallengeController controller)
        {
            int nameFontSize = UIHelper.ScaleInt(40);
            int regularFontSize = UIHelper.ScaleInt(35);
            int headerFontSize = UIHelper.ScaleInt(45);
            Color col = new(180, 180, 180, 255);
            Vector2 startPos = UIHelper.Scale(new Vector2(1500, 250));
            float spacingY = UIHelper.Scale(35);

            if (controller.PlayerWhite.IsBot && controller.PlayerBlack.IsBot)
            {
                DrawNextText($"Game {controller.CurrGameNumber} of {controller.TotalGameCount}", headerFontSize, Color.WHITE);
                startPos.Y += spacingY * 2;

                DrawStats(controller.BotStatsA);
                startPos.Y += spacingY * 2;
                DrawStats(controller.BotStatsB);
                startPos.Y += spacingY;
            }

            if (controller.PlayerWhite.IsBot || controller.PlayerBlack.IsBot)
            {
                DrawNextText($"{controller.myStats.ThinkingTime}ms   (avg {(int)controller.myStats.ThinkingTimeAvg}ms)", regularFontSize, col);
                DrawNextText($"Time: {(int)(Raylib.GetTime() - controller.myStats.TimeStarted)}s", regularFontSize, col);
                DrawNextText($"Positions Evaluated: {controller.myStats.PositionsEvaluatedCurrent}", regularFontSize, col);
                DrawNextText($"Branches Prunned: {controller.myStats.BranchesPrunnedCurrent}", regularFontSize, col);
                DrawNextText($"Evaluation: {controller.myStats.Evaluation}", regularFontSize, col);
            }

            void DrawStats(ChallengeController.BotMatchStats stats)
            {
                DrawNextText(stats.BotName + ":", nameFontSize, Color.WHITE);
                DrawNextText($"Score: +{stats.NumWins} ={stats.NumDraws} -{stats.NumLosses}", regularFontSize, col);
                DrawNextText($"Num Timeouts: {stats.NumTimeouts}", regularFontSize, col);
                DrawNextText($"Num Illegal Moves: {stats.NumIllegalMoves}", regularFontSize, col);
            }

            void DrawNextText(string text, int fontSize, Color col)
            {
                UIHelper.DrawText(text, startPos, fontSize, 1, col);
                startPos.Y += spacingY;
            }
        }
    }
}