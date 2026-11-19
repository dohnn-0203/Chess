#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

// 개발 빌드에서 명령행으로 실행하는 화면과 대국 통합 검사
public class ChessSmokeTest : MonoBehaviour
{
    [Serializable]
    private class Report
    {
        public bool passed;
        public bool screenshotsSkipped;
        public string unityVersion;
        public string startedUtc;
        public string finishedUtc;
        public string failure;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
        public List<string> screenshots = new List<string>();
    }

    private readonly Report report = new Report();
    private string outputDirectory;
    private ChessGame game;
    private BoardManager board;
    private ChessUI ui;
    private bool completed;
    private float startedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        int argument = Array.IndexOf(args, "-chessSmokeTest");
        if (argument < 0) return;
        if (argument + 1 >= args.Length)
        {
            Debug.LogError("The -chessSmokeTest argument requires an output directory.");
            Application.Quit(2);
            return;
        }

        GameObject runner = new GameObject("Chess smoke test");
        DontDestroyOnLoad(runner);
        ChessSmokeTest smoke = runner.AddComponent<ChessSmokeTest>();
        smoke.outputDirectory = Path.GetFullPath(args[argument + 1]);
        smoke.Begin();
    }

    private void Begin()
    {
        Directory.CreateDirectory(outputDirectory);
        report.unityVersion = Application.unityVersion;
        report.screenshotsSkipped = Array.IndexOf(Environment.GetCommandLineArgs(), "-chessSkipScreenshots") >= 0;
        report.startedUtc = DateTime.UtcNow.ToString("O");
        startedAt = Time.realtimeSinceStartup;
        Application.logMessageReceived += RecordLog;
        StartCoroutine(RunTracked());
    }

    private void Update()
    {
        if (!completed && Time.realtimeSinceStartup - startedAt > 90f)
            Finish("Smoke test timed out after 90 seconds.");
    }

    private IEnumerator RunTracked()
    {
        IEnumerator scenarios = RunScenarios();
        while (!completed)
        {
            bool hasNext;
            object instruction = null;
            try
            {
                hasNext = scenarios.MoveNext();
                if (hasNext) instruction = scenarios.Current;
            }
            catch (Exception exception)
            {
                Finish(exception.ToString());
                yield break;
            }
            if (!hasNext)
            {
                Finish(null);
                yield break;
            }
            yield return instruction;
        }
    }

    private IEnumerator RunScenarios()
    {
        for (int i = 0; i < 180; i++)
        {
            game = FindObjectOfType<ChessGame>();
            if (game != null && PrivateField<ChessState>(game, "state") != null) break;
            yield return null;
        }
        Check(game != null && PrivateField<ChessState>(game, "state") != null, "Scene initializes ChessGame");
        board = game.GetComponent<BoardManager>();
        ui = game.GetComponent<ChessUI>();
        Check(board != null && ui != null && ui.GetComponentInChildren<Canvas>() != null, "Board and optional game controls exist");
        Check(game.logText != null && game.logText.gameObject.activeInHierarchy, "Original scene LogText is visible");
        Check(game.logText.font != null, "Original scene LogText has its font asset");
        bool hasSidebar = false;
        foreach (Transform child in ui.GetComponentsInChildren<Transform>(true))
            if (child.name == "Sidebar") hasSidebar = true;
        Check(!hasSidebar, "Original design does not create a permanent sidebar");
        Check(FindObjectsOfType<Tile>().Length == 64, "Scene contains exactly 64 board tiles");
        CheckFreshGame("Initial game");
        Check(Camera.main != null && Camera.main.rect == new Rect(0f, 0f, 1f, 1f), "Board camera uses the original full viewport");
        Check(!Camera.main.orthographic && Camera.main.clearFlags == CameraClearFlags.Skybox, "Board preserves the original perspective and skybox");
        Check(FindButton("Menu") != null && FindButton("New game") == null && !ui.IsBlockingInput,
            "Optional menu starts closed with only its toggle available");
        CheckRecentLog("Initial game");
        Check(game.logText.text.Contains("White"), "Original log shows the initial player");

        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
        for (int i = 0; i < 12; i++) yield return null;
        Canvas.ForceUpdateCanvases();
        Check(Screen.width == 1280 && Screen.height == 720, "Window renders at 1280x720");
        CheckButtonLabels("1280x720 initial HUD");
        Physics.SyncTransforms();
        CheckPieceRaycast("a1");
        CheckPieceRaycast("h1");
        CheckPieceRaycast("a8");
        CheckPieceRaycast("h8");
        CheckPieceRaycast("e2");
        yield return null;
        yield return new WaitForEndOfFrame();
        Capture("initial-1280.png");

        Move("f2", "f3");
        Move("e7", "e5");
        Move("g2", "g4");
        Move("d8", "h4");
        Check(game.IsGameOver && game.StatusText.Contains("Checkmate") && game.StatusText.Contains("Black wins"), "Fool's mate ends with Black winning");
        Check(HistoryCount() == 4 && game.HistoryText.Contains("Qd8-h4#"), "Checkmate records all four plies and mate notation");
        CheckRecentLog("Checkmate");
        Check(game.logText.text.Contains(game.StatusText), "Original log shows the completed game result");
        Piece unmovedPawn = At("a2");
        game.ClickTile(TileAt("a2"));
        game.ClickTile(TileAt("a3"));
        Check(At("a2") == unmovedPawn && At("a3") == null && HistoryCount() == 4, "Board input is blocked after checkmate");
        CheckButtonLabels("Checkmate dialog");
        yield return new WaitForEndOfFrame();
        Capture("checkmate-1280.png");

        ClickButton("Play again");
        CheckFreshGame("Play again after checkmate");
        Move("e2", "e4");
        ClickButton("New game");
        Check(ui.IsBlockingInput && !game.CanInteract, "New game confirmation blocks board input");
        Piece blackPawn = At("e7");
        game.ClickTile(TileAt("e7"));
        game.ClickTile(TileAt("e5"));
        Check(At("e7") == blackPawn && HistoryCount() == 1, "Confirmation prevents a move while open");
        ClickButton("Cancel");
        Check(!ui.IsBlockingInput && game.CanInteract && game.Turn == Team.Black && HistoryCount() == 1, "Cancel preserves the current game");
        ClickButton("New game");
        ClickButton("Confirm");
        CheckFreshGame("Confirmed new game");

        ClickButton("Resign");
        Check(ui.IsBlockingInput && !game.IsGameOver, "Resign opens a confirmation without ending the game");
        ClickButton("Cancel");
        Check(game.CanInteract && !game.IsGameOver, "Cancelled resignation preserves play");
        ClickButton("Resign");
        ClickButton("Confirm");
        Check(game.IsGameOver && game.StatusText.Contains("White resigned") && game.StatusText.Contains("Black wins"), "Confirmed resignation awards the opposing player");
        ClickButton("Play again");
        CheckFreshGame("Play again after resignation");

        Move("e2", "e4");
        Move("d7", "d5");
        Move("e4", "d5");
        Check(CountPieces() == 31 && At("d5").Team == Team.White && At("e4") == null, "Capture updates piece occupancy");
        Check(game.WhiteCapturedText == "P" && game.BlackCapturedText == "None", "Capture updates the correct captured-piece list");
        Check(HistoryCount() == 3 && game.HistoryText.Contains("e4xd5"), "Capture is recorded in the move history");
        Vector3 firstCameraPosition = Camera.main.transform.position;
        Quaternion firstCameraRotation = Camera.main.transform.rotation;
        Vector3 boardCenter = new Vector3(3.5f * board.TileSize, 0f, 3.5f * board.TileSize);
        Vector3 flippedPosition = boardCenter + Quaternion.Euler(0f, 180f, 0f) * (firstCameraPosition - boardCenter);
        ClickButton("Flip board");
        Check(Vector3.Distance(flippedPosition, Camera.main.transform.position) < 0.01f
            && Mathf.Abs(Quaternion.Angle(firstCameraRotation, Camera.main.transform.rotation) - 180f) < 0.01f,
            "Flip board mirrors the original camera pose around the board center");
        Check(!ui.IsBlockingInput && game.CanInteract, "Flip board closes the optional menu and restores board input");
        ClickButton("Flip board");
        Check(Vector3.Distance(firstCameraPosition, Camera.main.transform.position) < 0.01f
            && Quaternion.Angle(firstCameraRotation, Camera.main.transform.rotation) < 0.01f,
            "Second flip restores the original camera pose");
        game.RestartGame();
        CheckFreshGame("Restart clears a captured game");

        // 승급 직전 배치만 구성하고 실제 이동과 승급은 공개 입력 경로로 검사한다.
        Piece promotionPawn = At("a2");
        board.RemovePiece(At("a7"));
        board.RemovePiece(At("a8"));
        board.SetPiece(0, 1, null);
        board.MovePiece(promotionPawn, 0, 6);
        Move("a7", "a8", false);
        Check(game.IsPromotionPending && game.Turn == Team.White && !game.CanInteract && HistoryCount() == 0, "Promotion waits for a choice before completing the turn");
        Check(FindButton("Promote Knight") != null && FindButton("Promote Queen") != null, "Promotion presents visible piece choices");
        CheckButtonLabels("Promotion dialog");
        yield return new WaitForEndOfFrame();
        Capture("promotion-1280.png");
        ClickButton("New game");
        Check(ui.IsBlockingInput, "New game confirms during a pending promotion");
        ClickButton("Cancel");
        Check(game.IsPromotionPending && !ui.IsBlockingInput, "Cancel preserves the pending promotion");
        ClickButton("Promote Knight");
        Check(!game.IsPromotionPending && game.Turn == Team.Black && At("a8").Type == PieceType.Knight && At("a8").HasMoved, "Underpromotion creates a moved knight and advances the turn once");
        Check(HistoryCount() == 1 && game.HistoryText.Contains("a7-a8=N"), "Underpromotion is recorded in the history");
        Check(CountPieces() == 30, "Promotion replaces the pawn without duplicating pieces");
        game.RestartGame();
        CheckFreshGame("Restart after promotion");

        Screen.SetResolution(1024, 768, FullScreenMode.Windowed);
        for (int i = 0; i < 12; i++) yield return null;
        Canvas.ForceUpdateCanvases();
        Check(Screen.width == 1024 && Screen.height == 768, "Window renders at 1024x768");
        CheckButtonLabels("1024x768 initial HUD");
        yield return new WaitForEndOfFrame();
        Capture("initial-1024.png");
        Check(report.errors.Count == 0, "No Unity errors or exceptions occurred during integration checks");
    }

    private void CheckFreshGame(string context)
    {
        Check(CountPieces() == 32 && game.Turn == Team.White && !game.IsGameOver && !game.IsPromotionPending && game.CanInteract,
            context + ": fresh board and legal input");
        Check(HistoryCount() == 0 && !game.HasMoves && game.WhiteCapturedText == "None" && game.BlackCapturedText == "None",
            context + ": history and captures reset");
    }

    private void Move(string from, string to, bool completeTurn = true)
    {
        Piece moving = At(from);
        Check(moving != null && moving.Team == game.Turn, "Move " + from + "-" + to + " starts with the active player's piece");
        Team before = game.Turn;
        game.ClickTile(TileAt(from));
        game.ClickTile(TileAt(to));
        Check(At(to) == moving && At(from) == null, "Move " + from + "-" + to + " updates the board");
        if (completeTurn) Check(game.Turn != before, "Move " + from + "-" + to + " advances the turn");
    }

    private Piece At(string square)
    {
        return board.GetPiece(square[0] - 'a', square[1] - '1');
    }

    private Tile TileAt(string square)
    {
        GameObject[,] tiles = PrivateField<GameObject[,]>(board, "tiles");
        return tiles[square[0] - 'a', square[1] - '1'].GetComponent<Tile>();
    }

    private int CountPieces()
    {
        int count = 0;
        for (int x = 0; x < board.Size; x++)
            for (int z = 0; z < board.Size; z++)
                if (board.GetPiece(x, z) != null) count++;
        return count;
    }

    private int HistoryCount()
    {
        return PrivateField<List<string>>(game, "history").Count;
    }

    private Button FindButton(string name)
    {
        foreach (Button button in ui.GetComponentsInChildren<Button>())
            if (button.name == name && button.gameObject.activeInHierarchy) return button;
        return null;
    }

    private void ClickButton(string name)
    {
        Button button = FindButton(name);
        if (button == null && (name == "New game" || name == "Resign" || name == "Flip board" || name == "Claim draw"))
        {
            Button menu = FindButton("Menu");
            Check(menu != null && menu.interactable, "Menu is available for action: " + name);
            menu.onClick.Invoke();
            Check(ui.IsBlockingInput && !game.CanInteract, "Open menu prevents board input: " + name);
            button = FindButton(name);
        }
        Check(button != null && button.interactable, "Button is available: " + name);
        button.onClick.Invoke();
    }

    private void CheckRecentLog(string context)
    {
        string displayed = game.logText.text;
        Check(displayed == game.RecentLogText && !string.IsNullOrWhiteSpace(displayed), context + ": original log displays the current game messages");
        Check(displayed.Split('\n').Length <= 3, context + ": original log keeps at most three lines");
    }

    private void CheckButtonLabels(string context)
    {
        Canvas.ForceUpdateCanvases();
        int count = 0;
        foreach (Button button in ui.GetComponentsInChildren<Button>())
        {
            if (!button.gameObject.activeInHierarchy) continue;
            Text label = button.GetComponentInChildren<Text>();
            if (label == null) continue;
            Rect rect = label.rectTransform.rect;
            Check(label.preferredWidth <= rect.width + 1f && label.preferredHeight <= rect.height + 1f,
                context + ": label fits " + button.name);
            count++;
        }
        Check(count >= 1, context + ": visible controls were inspected");
    }

    private void Capture(string filename)
    {
        if (report.screenshotsSkipped)
        {
            report.checks.Add("Screenshot skipped by explicit headless option; browser visual verification required: " + filename);
            return;
        }
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        Check(screenshot != null && screenshot.width == Screen.width && screenshot.height == Screen.height, "Capture has expected dimensions: " + filename);
        File.WriteAllBytes(Path.Combine(outputDirectory, filename), screenshot.EncodeToPNG());
        report.screenshots.Add(filename);
        Color32[] pixels = screenshot.GetPixels32();
        int darkest = 765;
        int brightest = 0;
        int brightSamples = 0;
        int stepX = Mathf.Max(1, screenshot.width / 64);
        int stepY = Mathf.Max(1, screenshot.height / 40);
        for (int y = stepY / 2; y < screenshot.height; y += stepY)
            for (int x = stepX / 2; x < screenshot.width; x += stepX)
            {
                Color32 pixel = pixels[y * screenshot.width + x];
                int light = pixel.r + pixel.g + pixel.b;
                darkest = Mathf.Min(darkest, light);
                brightest = Mathf.Max(brightest, light);
                if (light > 210) brightSamples++;
            }
        Destroy(screenshot);
        Check(brightest - darkest > 90 && brightSamples > 16, "Capture contains visible rendered content: " + filename);
    }

    private void CheckPieceRaycast(string square)
    {
        Piece piece = At(square);
        Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();
        Check(renderers.Length > 0, "Piece has visible geometry: " + square);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(bounds.center);
        Check(screenPoint.z > 0f && Camera.main.pixelRect.Contains(screenPoint), "Piece center is visible in board viewport: " + square);
        RaycastHit hit;
        bool hitPiece = Physics.Raycast(Camera.main.ScreenPointToRay(screenPoint), out hit, 1000f);
        Check(hitPiece && hit.collider.GetComponentInParent<Piece>() == piece, "Pointer ray resolves the intended piece: " + square);
    }

    private static T PrivateField<T>(object instance, string name)
    {
        FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new MissingFieldException(instance.GetType().Name, name);
        return (T)field.GetValue(instance);
    }

    private void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        report.checks.Add(description);
    }

    private void RecordLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            report.errors.Add(type + ": " + condition + "\n" + stackTrace);
    }

    private void Finish(string failure)
    {
        if (completed) return;
        completed = true;
        Application.logMessageReceived -= RecordLog;
        report.failure = failure;
        report.finishedUtc = DateTime.UtcNow.ToString("O");
        report.passed = string.IsNullOrEmpty(failure) && report.errors.Count == 0;
        File.WriteAllText(Path.Combine(outputDirectory, "smoke-report.json"), JsonUtility.ToJson(report, true));
        Debug.Log("Chess smoke checks: " + (report.passed ? "PASS" : "FAIL") + " (" + report.checks.Count + " checks)");
        Application.Quit(report.passed ? 0 : 1);
    }
}
#endif
