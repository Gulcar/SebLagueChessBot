using ChessChallenge.API;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ChessChallenge.Application
{
    static class Program
    {
        const bool hideRaylibLogs = true;
        static Camera2D cam;

        public static bool playingMt => mtChallangeControllers.Count > 0;
        public static List<ChallengeController> mtChallangeControllers = new();

        public static ChallengeController mainController;

        public static void Main()
        {
            //TestEvaluation();

            Vector2 loadedWindowSize = GetSavedWindowSize();
            int screenWidth = (int)loadedWindowSize.X;
            int screenHeight = (int)loadedWindowSize.Y;

            if (hideRaylibLogs)
            {
                unsafe
                {
                    Raylib.SetTraceLogCallback(&LogCustom);
                }
            }

            Raylib.InitWindow(screenWidth, screenHeight, "Chess Coding Challenge");
            Raylib.SetTargetFPS(60);

            UpdateCamera(screenWidth, screenHeight);

            mainController = new();

            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(22, 22, 22, 255));
                Raylib.BeginMode2D(cam);

                mainController.Update();

                if (playingMt)
                    mtChallangeControllers.ForEach(c => c.Update());

                mainController.Draw();

                Raylib.EndMode2D();

                mainController.DrawOverlay();

                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();

            UIHelper.Release();
        }

        public static void SetWindowSize(Vector2 size)
        {
            Raylib.SetWindowSize((int)size.X, (int)size.Y);
            UpdateCamera((int)size.X, (int)size.Y);
            SaveWindowSize();
        }

        public static Vector2 ScreenToWorldPos(Vector2 screenPos) => Raylib.GetScreenToWorld2D(screenPos, cam);

        static void UpdateCamera(int screenWidth, int screenHeight)
        {
            cam = new Camera2D();
            cam.target = new Vector2(0, 15);
            cam.offset = new Vector2(screenWidth / 2f, screenHeight / 2f);
            cam.zoom = screenWidth / 1280f * 0.7f;
        }


        [UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static unsafe void LogCustom(int logLevel, sbyte* text, sbyte* args)
        {
        }

        static Vector2 GetSavedWindowSize()
        {
            if (File.Exists(FileHelper.PrefsFilePath))
            {
                string prefs = File.ReadAllText(FileHelper.PrefsFilePath);
                if (!string.IsNullOrEmpty(prefs))
                {
                    if (prefs[0] == '0')
                    {
                        return Settings.ScreenSizeSmall;
                    }
                    else if (prefs[0] == '1')
                    {
                        return Settings.ScreenSizeBig;
                    }
                }
            }
            return Settings.ScreenSizeSmall;
        }

        static void SaveWindowSize()
        {
            Directory.CreateDirectory(FileHelper.AppDataPath);
            bool isBigWindow = Raylib.GetScreenWidth() > Settings.ScreenSizeSmall.X;
            File.WriteAllText(FileHelper.PrefsFilePath, isBigWindow ? "1" : "0");
        }

        static void TestEvaluation()
        {
            ChallengeController.MyStats stats = new();
            MyBot myBot = new(stats);

            while (true)
            {
                string? input = Console.ReadLine();
                if (input == null) System.Environment.Exit(1);

                Chess.Board cboard = new();
                cboard.LoadPosition(input);
                API.Board board = new(cboard);

                Console.WriteLine($"static eval: {myBot.Evaluate(board)}");
            }
        }

        public static void StartMyBotvsEvilBotMT()
        {
            StopMyBotvsEvilBotMT();

            const int numGames = 10;
            for (int i = 0; i < numGames; i++)
            {
                ChallengeController c = new();
                mtChallangeControllers.Add(c);
                c.isPlayingMt = true;
                c.mtStartFenIndex = 1000 / 12 * (i + 1);
                c.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.EvilBot);
            }
        }

        public static void StopMyBotvsEvilBotMT()
        {
            if (playingMt)
                Console.WriteLine("stopped playing mt");

            mtChallangeControllers.Clear();
        }

    }


}
