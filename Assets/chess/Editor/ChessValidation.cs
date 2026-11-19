using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// 실행 예: Unity -batchmode -projectPath <project> -executeMethod ChessValidation.RunRules
// 결과 파일: -chessValidationResults <absolute JSON path>
public static class ChessValidation
{
    [Serializable]
    private class TestResult
    {
        public string name;
        public bool passed;
        public string detail;
        public long milliseconds;
    }

    [Serializable]
    private class TestReport
    {
        public string unityVersion;
        public string completedUtc;
        public int passed;
        public int failed;
        public List<TestResult> tests = new List<TestResult>();
    }

    public static void RunRules()
    {
        TestReport report = new TestReport { unityVersion = Application.unityVersion };
        Run(report, "Initial position perft 20 / 400 / 8902", InitialPerft);
        Run(report, "Pinned pieces preserve king safety", PinnedPiece);
        Run(report, "Kings cannot capture kings or defended pieces", KingSafety);
        Run(report, "Pawn double move requires its starting rank", PawnStartingRank);
        Run(report, "White and black can castle on both sides", BothCastleSides);
        Run(report, "Castle disallowed while checked or through attack", CastleUnderAttack);
        Run(report, "Castle rights require unmoved home king and rook", CastleRights);
        Run(report, "Queenside castle permits attack on b-file", QueensideBFile);
        Run(report, "En passant is legal only immediately after double move", EnPassantLifetime);
        Run(report, "En passant capture updates the production board", EnPassantCapture);
        Run(report, "En passant cannot expose own king", EnPassantKingSafety);
        Run(report, "Fool's mate has check and no legal reply", FoolsMate);
        Run(report, "Stalemate has no check and no legal move", Stalemate);
        Run(report, "Insufficient material distinguishes mating possibilities", InsufficientMaterial);
        Run(report, "Threefold claim and fivefold automatic draw", RepetitionThresholds);
        Run(report, "Repetition includes turn and castling rights", RepetitionRights);
        Run(report, "Repetition includes legal en passant only", RepetitionEnPassant);
        Run(report, "50-move claim and 75-move automatic draw", MoveCountDraws);
        Run(report, "Halfmove clock resets on pawn moves and captures", HalfmoveClock);
        Run(report, "Promotion offers queen rook bishop and knight", PromotionChoices);
        Run(report, "Invalid or unavailable promotion keeps the pawn", PromotionGuards);
        Run(report, "Build scene has active game components and assigned prefabs", BuildScene);
        report.completedUtc = DateTime.UtcNow.ToString("O");

        string json = JsonUtility.ToJson(report, true);
        string output = Argument("-chessValidationResults");
        if (!string.IsNullOrEmpty(output))
        {
            string absolutePath = Path.GetFullPath(output);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, json);
        }

        Debug.Log("CHESS_VALIDATION " + report.passed + " passed, " + report.failed + " failed\n" + json);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(report.failed == 0 ? 0 : 1);
        }
    }

    private static void Run(TestReport report, string name, Action action)
    {
        Stopwatch timer = Stopwatch.StartNew();
        TestResult result = new TestResult { name = name };
        try
        {
            action();
            result.passed = true;
            result.detail = "Passed";
            report.passed++;
        }
        catch (Exception exception)
        {
            result.detail = exception.ToString();
            report.failed++;
            Debug.LogError(name + "\n" + exception);
        }

        result.milliseconds = timer.ElapsedMilliseconds;
        report.tests.Add(result);
    }

    private static void InitialPerft()
    {
        using (Position position = Position.Initial())
        {
            Equal(20L, Perft(position, Team.White, 1), "perft depth 1");
            Equal(400L, Perft(position, Team.White, 2), "perft depth 2");
            Equal(8902L, Perft(position, Team.White, 3), "perft depth 3");
            Equal(32, position.PieceCount(), "simulation restored all pieces");
            True(!position.Board.GetPiece(4, 0).HasMoved, "simulation restored king rights");
            True(!position.State.canEnPassant, "simulation restored en passant");
        }
    }

    private static long Perft(Position position, Team side, int depth)
    {
        long count = 0;
        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 8; z++)
            {
                Piece piece = position.Board.GetPiece(x, z);
                if (piece == null || piece.Team != side) continue;
                List<ChessMove> moves = position.Rules.GetLegalMoves(piece);
                if (depth == 1)
                {
                    count += moves.Count;
                    continue;
                }

                foreach (ChessMove move in moves)
                {
                    Undo undo = position.Apply(piece, move);
                    count += Perft(position, Other(side), depth - 1);
                    position.Restore(undo);
                }
            }
        }
        return count;
    }

    private static void PinnedPiece()
    {
        using (Position p = new Position())
        {
            p.Put(PieceType.King, Team.White, "e1");
            p.Put(PieceType.King, Team.Black, "a8");
            Piece rook = p.Put(PieceType.Rook, Team.White, "e2");
            p.Put(PieceType.Rook, Team.Black, "e8");
            True(!p.Has(rook, "d2"), "pinned rook cannot leave e-file");
            True(p.Has(rook, "e3"), "pinned rook can stay on pin line");
            True(p.Has(rook, "e8"), "pinned rook can capture checking-line attacker");
            Equal(4, p.PieceCount(), "move generation must not remove pieces");
            True(!rook.HasMoved && rook.X == 4 && rook.Z == 1, "move generation restores piece state");
        }
    }

    private static void KingSafety()
    {
        using (Position p = new Position())
        {
            Piece king = p.Put(PieceType.King, Team.White, "e1");
            p.Put(PieceType.King, Team.Black, "e3");
            True(!p.Has(king, "e2"), "kings cannot become adjacent");
            p.Put(PieceType.Rook, Team.White, "e7");
            True(!p.Has(p.At("e7"), "e3"), "enemy king is never a capture destination");
        }
        using (Position p = new Position())
        {
            Piece king = p.Put(PieceType.King, Team.White, "e1");
            p.Put(PieceType.King, Team.Black, "a8");
            p.Put(PieceType.Knight, Team.Black, "d2");
            p.Put(PieceType.Rook, Team.Black, "d8");
            True(!p.Has(king, "d2"), "king cannot capture a defended piece");
        }
    }

    private static void PawnStartingRank()
    {
        using (Position p = new Position())
        {
            p.Kings();
            Piece white = p.Put(PieceType.Pawn, Team.White, "b3");
            Piece black = p.Put(PieceType.Pawn, Team.Black, "g6");
            True(p.Has(white, "b4") && !p.Has(white, "b5"), "white double move from rank 3");
            True(p.Has(black, "g5") && !p.Has(black, "g4"), "black double move from rank 6");
        }
    }

    private static void BothCastleSides()
    {
        foreach (Team side in new[] { Team.White, Team.Black })
        {
            using (Position p = new Position())
            {
                string rank = side == Team.White ? "1" : "8";
                Piece king = p.Put(PieceType.King, side, "e" + rank);
                p.Put(PieceType.Rook, side, "a" + rank);
                p.Put(PieceType.Rook, side, "h" + rank);
                p.Put(PieceType.King, Other(side), side == Team.White ? "e8" : "e1");
                ChessMove shortCastle = p.Find(king, "g" + rank);
                ChessMove longCastle = p.Find(king, "c" + rank);
                True(shortCastle.castle && longCastle.castle, "both moves must be castles");
                Undo undo = p.Apply(king, shortCastle);
                True(p.At("f" + rank).Type == PieceType.Rook, "rook follows king on short castle");
                True(p.At("h" + rank) == null && p.At("e" + rank) == null, "castle clears origin squares");
                p.Restore(undo);
                undo = p.Apply(king, longCastle);
                True(p.At("d" + rank).Type == PieceType.Rook, "rook follows king on long castle");
                p.Restore(undo);
                p.Rules.PlayMove(king, longCastle);
                True(p.At("c" + rank) == king && p.At("d" + rank).Type == PieceType.Rook, "production castle moves both pieces");
                True(king.HasMoved && p.At("d" + rank).HasMoved, "production castle consumes both movement rights");
            }
        }
    }

    private static void CastleUnderAttack()
    {
        foreach (string attacker in new[] { "e8", "f8", "g8" })
        {
            using (Position p = new Position())
            {
                Piece king = p.Put(PieceType.King, Team.White, "e1");
                p.Put(PieceType.Rook, Team.White, "h1");
                p.Put(PieceType.King, Team.Black, "a8");
                p.Put(PieceType.Rook, Team.Black, attacker);
                True(!p.Has(king, "g1"), "castle must reject rook attack from " + attacker);
            }
        }
    }

    private static void CastleRights()
    {
        using (Position p = new Position())
        {
            Piece king = p.Put(PieceType.King, Team.White, "e1");
            Piece rook = p.Put(PieceType.Rook, Team.White, "h1");
            p.Put(PieceType.King, Team.Black, "e8");
            rook.SetState(7, 0, true);
            True(!p.Has(king, "g1"), "moved rook cannot castle");
            rook.SetState(7, 0, false);
            king.SetState(4, 0, true);
            True(!p.Has(king, "g1"), "moved king cannot castle");
        }
        using (Position p = new Position())
        {
            Piece king = p.Put(PieceType.King, Team.White, "d1");
            p.Put(PieceType.Rook, Team.White, "h1");
            p.Put(PieceType.King, Team.Black, "e8");
            foreach (ChessMove move in p.Rules.GetLegalMoves(king))
                True(!move.castle, "king off home square cannot castle");
        }
    }

    private static void QueensideBFile()
    {
        using (Position p = new Position())
        {
            Piece king = p.Put(PieceType.King, Team.White, "e1");
            p.Put(PieceType.Rook, Team.White, "a1");
            p.Put(PieceType.King, Team.Black, "h8");
            p.Put(PieceType.Rook, Team.Black, "b8");
            True(p.Find(king, "c1").castle, "b1 attack does not affect king path");
        }
    }

    private static void EnPassantLifetime()
    {
        using (Position p = new Position())
        {
            p.Kings();
            Piece pawn = p.Put(PieceType.Pawn, Team.White, "e5");
            Piece enemy = p.Put(PieceType.Pawn, Team.Black, "d7");
            p.Apply(enemy, p.Find(enemy, "d5"));
            ChessMove move = p.Find(pawn, "d6");
            True(move.enPassant, "double pawn move enables capture");
            Undo undo = p.Apply(pawn, move);
            True(p.At("d5") == null && p.At("d6") == pawn, "en passant removes the adjacent pawn");
            p.Restore(undo);
            Piece king = p.At("e1");
            p.Apply(king, p.Find(king, "f1"));
            True(!p.State.canEnPassant && !p.Has(pawn, "d6"), "en passant expires after another move");
        }
    }

    private static void EnPassantKingSafety()
    {
        using (Position p = new Position())
        {
            p.Kings();
            Piece pawn = p.Put(PieceType.Pawn, Team.White, "e5");
            p.Put(PieceType.Pawn, Team.Black, "d5");
            p.Put(PieceType.Rook, Team.Black, "e7");
            p.SetEnPassant("d6", "d5");
            True(!p.Has(pawn, "d6"), "en passant cannot expose e-file pin");
        }
        using (Position p = new Position())
        {
            p.Put(PieceType.King, Team.White, "h5");
            p.Put(PieceType.King, Team.Black, "h8");
            Piece pawn = p.Put(PieceType.Pawn, Team.White, "g5");
            p.Put(PieceType.Pawn, Team.Black, "f5");
            p.Put(PieceType.Rook, Team.Black, "a5");
            p.SetEnPassant("f6", "f5");
            True(!p.Has(pawn, "f6"), "removing two pawns cannot expose horizontal rook attack");
        }
    }

    private static void EnPassantCapture()
    {
        using (Position p = new Position())
        {
            p.Kings();
            Piece pawn = p.Put(PieceType.Pawn, Team.White, "e5");
            p.Put(PieceType.Pawn, Team.Black, "d5");
            p.SetEnPassant("d6", "d5");
            p.State.halfmoveClock = 35;
            p.Rules.PlayMove(pawn, p.Find(pawn, "d6"));
            True(p.At("d5") == null && p.At("e5") == null && p.At("d6") == pawn, "en passant removes victim and moves attacker");
            True(!p.State.canEnPassant, "en passant capture clears old capture right");
            Equal(0, p.State.halfmoveClock, "en passant resets halfmove clock");
            Equal(3, p.PieceCount(), "en passant removes exactly one pawn");
        }
    }

    private static void FoolsMate()
    {
        using (Position p = Position.Initial())
        {
            p.Move("f2", "f3");
            p.Move("e7", "e5");
            p.Move("g2", "g4");
            p.Move("d8", "h4");
            True(p.Rules.IsCheck(Team.White), "Fool's mate checks white king");
            True(!p.Rules.HasMove(Team.White), "Fool's mate has no legal defense");
        }
    }

    private static void Stalemate()
    {
        using (Position p = new Position())
        {
            p.Put(PieceType.King, Team.White, "c6");
            p.Put(PieceType.Queen, Team.White, "c7");
            p.Put(PieceType.King, Team.Black, "a8");
            True(!p.Rules.IsCheck(Team.Black), "stalemate king is not checked");
            True(!p.Rules.HasMove(Team.Black), "stalemate has no legal move");
        }
    }

    private static void InsufficientMaterial()
    {
        using (Position p = new Position())
        {
            p.Kings();
            True(p.Draw.IsInsufficientMaterial(), "king versus king");
            p.Put(PieceType.Knight, Team.White, "b1");
            True(p.Draw.IsInsufficientMaterial(), "one knight versus king");
            p.Put(PieceType.Knight, Team.White, "g1");
            True(!p.Draw.IsInsufficientMaterial(), "two knights permit possible mate");
        }
        using (Position p = new Position())
        {
            p.Kings();
            p.Put(PieceType.Bishop, Team.White, "c1");
            True(p.Draw.IsInsufficientMaterial(), "one bishop versus king");
            p.Put(PieceType.Bishop, Team.Black, "f8");
            True(p.Draw.IsInsufficientMaterial(), "only same-color bishops");
            p.Put(PieceType.Bishop, Team.Black, "c8");
            True(!p.Draw.IsInsufficientMaterial(), "opposite-color bishops permit possible mate");
        }
        using (Position p = new Position())
        {
            p.Kings();
            p.Put(PieceType.Pawn, Team.White, "a2");
            True(!p.Draw.IsInsufficientMaterial(), "a pawn can promote");
        }
    }

    private static void RepetitionThresholds()
    {
        using (Position p = Position.Initial())
        {
            p.Draw.RecordPosition();
            for (int cycle = 1; cycle <= 4; cycle++)
            {
                p.MoveAndRecord("g1", "f3");
                p.MoveAndRecord("g8", "f6");
                p.MoveAndRecord("f3", "g1");
                p.MoveAndRecord("f6", "g8");
                Equal(cycle + 1, p.Draw.RepetitionCount, "initial position repeated");
                True(p.Draw.CanClaimDraw == (cycle >= 2), "threefold threshold");
                True((p.Draw.AutomaticDrawReason.Length > 0) == (cycle == 4), "fivefold threshold");
            }
        }
    }

    private static void RepetitionRights()
    {
        using (Position p = Position.Initial())
        {
            p.Draw.RecordPosition();
            p.State.turn = Team.Black;
            p.Draw.RecordPosition();
            Equal(1, p.Draw.RepetitionCount, "other side to move differs");
            p.State.turn = Team.White;
            p.Draw.RecordPosition();
            Equal(2, p.Draw.RepetitionCount, "original side to move repeats");
            p.At("h1").SetState(7, 0, true);
            p.Draw.RecordPosition();
            Equal(1, p.Draw.RepetitionCount, "lost castling right distinguishes layout");
        }
    }

    private static void RepetitionEnPassant()
    {
        using (Position p = new Position())
        {
            p.Kings();
            p.Put(PieceType.Pawn, Team.White, "e5");
            p.Put(PieceType.Pawn, Team.Black, "d5");
            p.Draw.RecordPosition();
            p.SetEnPassant("d6", "d5");
            p.Draw.RecordPosition();
            Equal(1, p.Draw.RepetitionCount, "legal en passant changes position identity");
            p.State.canEnPassant = false;
            p.Draw.RecordPosition();
            Equal(2, p.Draw.RepetitionCount, "position without en passant repeats");
        }
        using (Position p = new Position())
        {
            p.Kings();
            p.Put(PieceType.Pawn, Team.White, "e5");
            p.Put(PieceType.Pawn, Team.Black, "d5");
            p.Put(PieceType.Rook, Team.Black, "e7");
            p.Draw.RecordPosition();
            p.SetEnPassant("d6", "d5");
            p.Draw.RecordPosition();
            Equal(2, p.Draw.RepetitionCount, "pinned illegal en passant does not change identity");
        }
    }

    private static void MoveCountDraws()
    {
        using (Position p = Position.Initial())
        {
            p.State.halfmoveClock = 99;
            True(!p.Draw.CanClaimDraw, "99 halfmoves is below claim threshold");
            p.State.halfmoveClock = 100;
            True(p.Draw.CanClaimDraw && p.Draw.ClaimReason.Contains("50-move"), "100 halfmoves permits claim");
            p.State.halfmoveClock = 149;
            True(p.Draw.AutomaticDrawReason.Length == 0, "149 halfmoves is not automatic");
            p.State.halfmoveClock = 150;
            True(p.Draw.AutomaticDrawReason.Contains("75-move"), "150 halfmoves automatically draws");
            p.State.waitPromotion = true;
            True(!p.Draw.CanClaimDraw, "promotion must finish before draw claim");
            p.State.waitPromotion = false;
            p.State.gameOver = true;
            True(!p.Draw.CanClaimDraw, "finished game cannot claim again");
        }
    }

    private static void HalfmoveClock()
    {
        using (Position p = Position.Initial())
        {
            p.State.halfmoveClock = 12;
            Piece knight = p.At("g1");
            p.Rules.PlayMove(knight, p.Find(knight, "f3"));
            Equal(13, p.State.halfmoveClock, "quiet move increments halfmove clock");
            Piece pawn = p.At("e2");
            p.Rules.PlayMove(pawn, p.Find(pawn, "e4"));
            Equal(0, p.State.halfmoveClock, "pawn move resets clock");
            True(p.State.canEnPassant, "production pawn double move enables en passant");
        }
        using (Position p = new Position())
        {
            p.Kings();
            Piece rook = p.Put(PieceType.Rook, Team.White, "a1");
            p.Put(PieceType.Knight, Team.Black, "a7");
            p.State.halfmoveClock = 49;
            p.Rules.PlayMove(rook, p.Find(rook, "a7"));
            Equal(0, p.State.halfmoveClock, "capture resets clock");
            Equal(3, p.PieceCount(), "capture removes one piece");
        }
    }

    private static void PromotionChoices()
    {
        foreach (Team side in new[] { Team.White, Team.Black })
        foreach (PieceType choice in new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight })
        {
            using (Position p = new Position())
            {
                p.Kings();
                p.ConfigurePrefabs();
                string square = side == Team.White ? "a8" : "a1";
                Piece pawn = p.Put(PieceType.Pawn, side, square);
                ChessPromotion promotion = new ChessPromotion(p.Board, p.State, null);
                promotion.Start(pawn);
                True(p.State.waitPromotion, "last rank starts promotion");
                True(promotion.Promote(choice), "promotion succeeds");
                Piece result = p.At(square);
                True(result != null && result.Type == choice && result.Team == side, "selected promotion piece replaces pawn");
                True(result.HasMoved, "promoted rook never gains castling rights");
                True(!p.State.waitPromotion && p.State.promotionPawn == null, "promotion clears pending state");
            }
        }
    }

    private static void PromotionGuards()
    {
        using (Position p = new Position())
        {
            p.Kings();
            Piece pawn = p.Put(PieceType.Pawn, Team.White, "a8");
            ChessPromotion promotion = new ChessPromotion(p.Board, p.State, null);
            promotion.Start(pawn);
            True(!promotion.Promote(PieceType.King), "king is not a promotion choice");
            True(!promotion.Promote(PieceType.Pawn), "pawn is not a promotion choice");
            True(!promotion.Promote(PieceType.Queen), "missing prefab cannot complete promotion");
            True(p.At("a8") == pawn && p.State.waitPromotion, "failed promotion preserves pawn and pending choice");
            p.ConfigurePrefabs();
            True(promotion.Promote(PieceType.Queen), "promotion can be retried after prefab becomes available");
            True(!promotion.Promote(PieceType.Rook), "finished promotion cannot run twice");
        }
    }

    private static void BuildScene()
    {
        const string path = "Assets/chess/Scenes/Chess.unity";
        bool found = false;
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled && scene.path == path) found = true;
        True(found, "Chess scene must be enabled in build settings");
        True(AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null, "Chess scene asset exists");

        Scene originalActive = SceneManager.GetActiveScene();
        Scene chessScene = SceneManager.GetSceneByPath(path);
        bool openedForValidation = !chessScene.IsValid() || !chessScene.isLoaded;
        if (openedForValidation) chessScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            List<BoardManager> managers = new List<BoardManager>();
            List<GameObject> legacyLogs = new List<GameObject>();
            List<Camera> cameras = new List<Camera>();
            foreach (GameObject rootObject in chessScene.GetRootGameObjects())
            {
                managers.AddRange(rootObject.GetComponentsInChildren<BoardManager>(true));
                cameras.AddRange(rootObject.GetComponentsInChildren<Camera>(true));
                foreach (Transform child in rootObject.GetComponentsInChildren<Transform>(true))
                    if (child.name == "LogText") legacyLogs.Add(child.gameObject);
            }

            Equal(1, managers.Count, "scene must have exactly one BoardManager");
            BoardManager board = managers[0];
            True(board.enabled && board.gameObject.activeInHierarchy, "BoardManager must be enabled on an active object");
            ChessGame game = board.GetComponent<ChessGame>();
            True(game != null && game.enabled && game.gameObject.activeInHierarchy, "ChessGame must be active on the BoardManager object");

            SerializedObject serializedBoard = new SerializedObject(board);
            foreach (string field in new[] { "tilePrefab", "pawnPrefab", "rookPrefab", "knightPrefab", "bishopPrefab", "queenPrefab", "kingPrefab" })
            {
                SerializedProperty property = serializedBoard.FindProperty(field);
                True(property != null && property.objectReferenceValue != null, "scene prefab must be assigned: " + field);
            }

            Equal(1, legacyLogs.Count, "scene must have exactly one original LogText object");
            True(legacyLogs[0].activeInHierarchy, "original LogText must be active in the restored scene");
            True(game.logText != null && game.logText.gameObject == legacyLogs[0], "ChessGame must display logs through the original LogText");
            True(game.logText.font != null, "original LogText must have its assigned font asset");
            Camera mainCamera = cameras.Find(camera => camera.CompareTag("MainCamera"));
            True(mainCamera != null && mainCamera.enabled, "scene must have an active main camera");
            True(!mainCamera.orthographic && mainCamera.clearFlags == CameraClearFlags.Skybox,
                "scene must preserve its original perspective camera and skybox");
            True(mainCamera.rect == new Rect(0f, 0f, 1f, 1f), "original camera must use the full viewport");
            if (openedForValidation) True(!chessScene.isDirty, "scene inspection must not modify the asset");
        }
        finally
        {
            if (openedForValidation) EditorSceneManager.CloseScene(chessScene, true);
            if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
        }
    }

    private static Team Other(Team team) { return team == Team.White ? Team.Black : Team.White; }
    private static Vector2Int Square(string square) { return new Vector2Int(square[0] - 'a', square[1] - '1'); }
    private static void True(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Equal(long expected, long actual, string message)
    {
        if (expected != actual) throw new InvalidOperationException(message + ": expected " + expected + ", got " + actual);
    }
    private static string Argument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
        return null;
    }
    private static void Field(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        True(field != null, "fixture field exists: " + name);
        field.SetValue(target, value);
    }

    // 말을 새로 생성하지 않고 보드 상태만 저장해서 수순의 경우의 수를 센다.
    private struct Undo
    {
        public Piece piece, captured, rook;
        public int x, z;
        public bool moved, rookMoved, canEnPassant;
        public Vector2Int enPassantTile, enPassantPawn;
        public ChessMove move;
        public Team turn;
        public int halfmoveClock;
    }

    private sealed class Position : IDisposable
    {
        private readonly GameObject root;
        public readonly BoardManager Board;
        public readonly ChessState State;
        public readonly ChessRules Rules;
        public readonly ChessDrawRules Draw;

        public Position()
        {
            root = new GameObject("ChessValidationFixture");
            root.SetActive(false);
            Board = root.AddComponent<BoardManager>();
            Field(Board, "size", 8);
            Field(Board, "pieces", new Piece[8, 8]);
            Field(Board, "block", new MaterialPropertyBlock());
            Field(Board, "piecesRoot", root.transform);
            State = new ChessState();
            Rules = new ChessRules(Board, State);
            Draw = new ChessDrawRules(Board, State, Rules);
        }

        public static Position Initial()
        {
            Position p = new Position();
            PieceType[] row = { PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen, PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook };
            for (int x = 0; x < 8; x++)
            {
                p.Put(row[x], Team.White, x, 0);
                p.Put(PieceType.Pawn, Team.White, x, 1);
                p.Put(PieceType.Pawn, Team.Black, x, 6);
                p.Put(row[x], Team.Black, x, 7);
            }
            return p;
        }

        public void Kings()
        {
            Put(PieceType.King, Team.White, "e1");
            Put(PieceType.King, Team.Black, "e8");
        }

        public Piece Put(PieceType type, Team team, string square)
        {
            Vector2Int location = Square(square);
            return Put(type, team, location.x, location.y);
        }

        private Piece Put(PieceType type, Team team, int x, int z)
        {
            True(Board.GetPiece(x, z) == null, "fixture square must be empty");
            GameObject obj = new GameObject(team + " " + type);
            obj.transform.SetParent(root.transform);
            Piece piece = type == PieceType.Pawn ? obj.AddComponent<Pawn>() : obj.AddComponent<Piece>();
            piece.Init(team, type, x, z);
            Board.SetPiece(x, z, piece);
            return piece;
        }

        public Piece At(string square)
        {
            Vector2Int location = Square(square);
            return Board.GetPiece(location.x, location.y);
        }

        public bool Has(Piece piece, string square)
        {
            Vector2Int destination = Square(square);
            foreach (ChessMove move in Rules.GetLegalMoves(piece)) if (move.to == destination) return true;
            return false;
        }

        public ChessMove Find(Piece piece, string square)
        {
            Vector2Int destination = Square(square);
            foreach (ChessMove move in Rules.GetLegalMoves(piece)) if (move.to == destination) return move;
            throw new InvalidOperationException("Expected legal move to " + square);
        }

        public void Move(string from, string to)
        {
            Piece piece = At(from);
            True(piece != null && piece.Team == State.turn, "fixture move uses side to move");
            Apply(piece, Find(piece, to));
        }

        public void MoveAndRecord(string from, string to) { Move(from, to); Draw.RecordPosition(); }

        public void SetEnPassant(string target, string pawn)
        {
            State.canEnPassant = true;
            State.enPassantTile = Square(target);
            State.enPassantPawn = Square(pawn);
        }

        public Undo Apply(Piece piece, ChessMove move)
        {
            Undo undo = new Undo
            {
                piece = piece, x = piece.X, z = piece.Z, moved = piece.HasMoved,
                move = move, canEnPassant = State.canEnPassant,
                enPassantTile = State.enPassantTile, enPassantPawn = State.enPassantPawn,
                turn = State.turn, halfmoveClock = State.halfmoveClock,
                captured = move.enPassant ? Board.GetPiece(move.enPassantPos.x, move.enPassantPos.y) : Board.GetPiece(move.to.x, move.to.y)
            };
            Board.SetPiece(piece.X, piece.Z, null);
            if (move.enPassant) Board.SetPiece(move.enPassantPos.x, move.enPassantPos.y, null);
            if (move.castle)
            {
                undo.rook = Board.GetPiece(move.rookFrom, undo.z);
                undo.rookMoved = undo.rook.HasMoved;
                Board.SetPiece(move.rookFrom, undo.z, null);
                Board.SetPiece(move.rookTo, undo.z, undo.rook);
                undo.rook.SetState(move.rookTo, undo.z, true);
            }
            Board.SetPiece(move.to.x, move.to.y, piece);
            piece.SetState(move.to.x, move.to.y, true);
            State.canEnPassant = piece.Type == PieceType.Pawn && Math.Abs(move.to.y - undo.z) == 2;
            if (State.canEnPassant)
            {
                State.enPassantTile = new Vector2Int(move.to.x, (move.to.y + undo.z) / 2);
                State.enPassantPawn = move.to;
            }
            State.halfmoveClock = piece.Type == PieceType.Pawn || undo.captured != null ? 0 : State.halfmoveClock + 1;
            State.turn = Other(piece.Team);
            return undo;
        }

        public void Restore(Undo undo)
        {
            Board.SetPiece(undo.move.to.x, undo.move.to.y, undo.move.enPassant ? null : undo.captured);
            Board.SetPiece(undo.x, undo.z, undo.piece);
            undo.piece.SetState(undo.x, undo.z, undo.moved);
            if (undo.move.enPassant) Board.SetPiece(undo.move.enPassantPos.x, undo.move.enPassantPos.y, undo.captured);
            if (undo.rook != null)
            {
                Board.SetPiece(undo.move.rookTo, undo.z, null);
                Board.SetPiece(undo.move.rookFrom, undo.z, undo.rook);
                undo.rook.SetState(undo.move.rookFrom, undo.z, undo.rookMoved);
            }
            State.canEnPassant = undo.canEnPassant;
            State.enPassantTile = undo.enPassantTile;
            State.enPassantPawn = undo.enPassantPawn;
            State.turn = undo.turn;
            State.halfmoveClock = undo.halfmoveClock;
        }

        public int PieceCount()
        {
            int count = 0;
            for (int x = 0; x < 8; x++) for (int z = 0; z < 8; z++) if (Board.GetPiece(x, z) != null) count++;
            return count;
        }

        public void ConfigurePrefabs()
        {
            foreach (string name in new[] { "queen", "rook", "bishop", "knight" })
            {
                GameObject prefab = new GameObject(name + "ValidationPrefab");
                prefab.transform.SetParent(root.transform);
                Field(Board, name + "Prefab", prefab);
            }
        }

        public void Dispose() { UnityEngine.Object.DestroyImmediate(root); }
    }
}
