using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 장면 설정 없이 생성되는 대국 화면
public class ChessUI : MonoBehaviour
{
    public const float BoardViewportWidth = 0.68f;

    private static readonly Color PanelColor = new Color(0.055f, 0.075f, 0.105f);
    private static readonly Color CardColor = new Color(0.10f, 0.13f, 0.17f);
    private static readonly Color InkColor = new Color(0.94f, 0.92f, 0.86f);
    private static readonly Color MutedColor = new Color(0.61f, 0.67f, 0.73f);
    private static readonly Color AccentColor = new Color(0.84f, 0.69f, 0.42f);

    private ChessGame game;
    private Font font;
    private RectTransform canvasRoot;
    private Text statusText;
    private Text turnText;
    private Text whiteCapturedText;
    private Text blackCapturedText;
    private Text historyText;
    private Text resultText;
    private Text confirmationText;
    private ScrollRect historyScroll;
    private Button resignButton;
    private Button claimButton;
    private GameObject promotionPanel;
    private GameObject resultPanel;
    private GameObject confirmationPanel;
    private Action confirmationAction;
    private string previousHistory = "\0";

    public bool IsBlockingInput => confirmationPanel != null && confirmationPanel.activeSelf;

    public void Init(ChessGame chessGame)
    {
        game = chessGame;
        if (canvasRoot == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateCanvas();
            CreateSidebar();
            CreatePromotionPanel();
            CreateResultPanel();
            CreateConfirmationPanel();
        }

        CloseConfirmation();
        Refresh();
    }

    public void Refresh()
    {
        if (game == null || canvasRoot == null) return;

        turnText.text = game.IsGameOver ? "GAME FINISHED" : game.Turn == Team.White ? "WHITE TO MOVE" : "BLACK TO MOVE";
        turnText.color = game.IsGameOver ? AccentColor : InkColor;
        statusText.text = game.StatusText;
        whiteCapturedText.text = "White captured: " + CapturedOrDash(game.WhiteCapturedText);
        blackCapturedText.text = "Black captured: " + CapturedOrDash(game.BlackCapturedText);
        resignButton.interactable = !game.IsGameOver && !game.IsPromotionPending;
        claimButton.gameObject.SetActive(game.CanClaimDraw && !game.IsGameOver && !game.IsPromotionPending);
        promotionPanel.SetActive(game.IsPromotionPending && !game.IsGameOver);
        resultPanel.SetActive(game.IsGameOver);
        resultText.text = game.StatusText;

        if (previousHistory != game.HistoryText)
        {
            previousHistory = game.HistoryText;
            historyText.text = string.IsNullOrEmpty(previousHistory) ? "Your first move starts the game." : previousHistory;
            historyText.color = string.IsNullOrEmpty(previousHistory) ? MutedColor : InkColor;
            Canvas.ForceUpdateCanvases();
            historyScroll.verticalNormalizedPosition = 0f;
        }
    }

    private void Update()
    {
        if (IsBlockingInput && Input.GetKeyDown(KeyCode.Escape)) CloseConfirmation();
    }

    private void CreateCanvas()
    {
        canvasRoot = Rect("Chess HUD", transform);
        Canvas canvas = canvasRoot.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        canvasRoot.gameObject.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null)
        {
            GameObject events = new GameObject("Chess Event System", typeof(EventSystem), typeof(StandaloneInputModule));
            events.transform.SetParent(transform, false);
        }

        Text boardLabel = Label("Board label", canvasRoot, "LOCAL CHESS", 14, AccentColor, TextAnchor.UpperLeft);
        Place(boardLabel.rectTransform, new Vector2(0f, 1f), new Vector2(BoardViewportWidth, 1f), new Vector2(24f, -40f), new Vector2(-24f, -18f));
        Text boardHint = Label("Board hint", canvasRoot, "Select a piece, then choose a highlighted square.", 14, MutedColor, TextAnchor.LowerCenter);
        Place(boardHint.rectTransform, Vector2.zero, new Vector2(BoardViewportWidth, 0f), new Vector2(16f, 18f), new Vector2(-16f, 56f));
    }

    private void CreateSidebar()
    {
        RectTransform panel = Box("Sidebar", canvasRoot, PanelColor);
        Place(panel, new Vector2(BoardViewportWidth, 0f), Vector2.one, Vector2.zero, Vector2.zero);

        RectTransform line = Box("Accent line", panel, AccentColor);
        Place(line, new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -7f), new Vector2(-24f, -4f));

        Text title = Label("Title", panel, "CHESS", 32, InkColor, TextAnchor.UpperLeft);
        Top(title.rectTransform, 22f, 42f);
        Text subtitle = Label("Subtitle", panel, "TWO PLAYERS / ONE BOARD", 11, MutedColor, TextAnchor.UpperLeft);
        Top(subtitle.rectTransform, 65f, 22f);

        RectTransform statusCard = Box("Status card", panel, CardColor);
        Top(statusCard, 103f, 101f);
        turnText = Label("Turn", statusCard, "WHITE TO MOVE", 18, InkColor, TextAnchor.UpperLeft);
        Place(turnText.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(15f, -39f), new Vector2(-15f, -15f));
        statusText = Label("Status", statusCard, string.Empty, 15, MutedColor, TextAnchor.UpperLeft);
        Place(statusText.rectTransform, Vector2.zero, Vector2.one, new Vector2(15f, 9f), new Vector2(-15f, -47f));

        whiteCapturedText = Label("White captures", panel, string.Empty, 13, MutedColor, TextAnchor.UpperLeft);
        Top(whiteCapturedText.rectTransform, 216f, 30f);
        blackCapturedText = Label("Black captures", panel, string.Empty, 13, MutedColor, TextAnchor.UpperLeft);
        Top(blackCapturedText.rectTransform, 250f, 30f);

        Text historyHeading = Label("History heading", panel, "MOVE HISTORY", 12, AccentColor, TextAnchor.UpperLeft);
        Top(historyHeading.rectTransform, 287f, 24f);
        CreateHistory(panel);

        claimButton = MakeButton("Claim draw", panel, "Claim draw", game.ClaimDraw, true);
        Place(claimButton.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0f), new Vector2(24f, 163f), new Vector2(-24f, 199f));

        Button newGameButton = MakeButton("New game", panel, "New game", RequestNewGame, true);
        Place(newGameButton.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0.5f, 0f), new Vector2(24f, 104f), new Vector2(-5f, 148f));
        resignButton = MakeButton("Resign", panel, "Resign", RequestResign, false);
        Place(resignButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(5f, 104f), new Vector2(-24f, 148f));

        Button flipButton = MakeButton("Flip board", panel, "Flip board", game.FlipBoard, false);
        Place(flipButton.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1f, 0f), new Vector2(24f, 52f), new Vector2(-24f, 92f));
        Text footer = Label("Footer", panel, "Legal moves are highlighted in green.", 11, MutedColor, TextAnchor.MiddleCenter);
        Place(footer.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(20f, 13f), new Vector2(-20f, 38f));
    }

    private void CreateHistory(RectTransform panel)
    {
        RectTransform scrollRoot = Box("Move history", panel, new Color(0.035f, 0.05f, 0.075f));
        Place(scrollRoot, Vector2.zero, Vector2.one, new Vector2(24f, 216f), new Vector2(-24f, -317f));
        historyScroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        historyScroll.horizontal = false;
        historyScroll.vertical = true;
        historyScroll.movementType = ScrollRect.MovementType.Clamped;
        historyScroll.scrollSensitivity = 24f;

        RectTransform viewport = Box("Viewport", scrollRoot, Color.white);
        Stretch(viewport, 12f, 10f, 23f, 10f);
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        historyText = Label("Moves", viewport, string.Empty, 17, InkColor, TextAnchor.UpperLeft);
        historyText.lineSpacing = 1.3f;
        RectTransform content = historyText.rectTransform;
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        historyScroll.viewport = viewport;
        historyScroll.content = content;

        RectTransform track = Box("Scroll track", scrollRoot, CardColor);
        Place(track, new Vector2(1f, 0f), Vector2.one, new Vector2(-12f, 10f), new Vector2(-6f, -10f));
        Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
        RectTransform handle = Box("Scroll handle", track, MutedColor);
        Stretch(handle, 0f, 0f, 0f, 0f);
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = handle.GetComponent<Image>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        historyScroll.verticalScrollbar = scrollbar;
        historyScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
    }

    private void CreatePromotionPanel()
    {
        RectTransform overlay = Overlay("Promotion", out RectTransform card, 480f, 224f);
        promotionPanel = overlay.gameObject;
        Text heading = Label("Promotion heading", card, "PROMOTE YOUR PAWN", 22, InkColor, TextAnchor.MiddleCenter);
        Top(heading.rectTransform, 24f, 36f);
        Text hint = Label("Promotion hint", card, "Choose a piece or press its key.", 15, MutedColor, TextAnchor.MiddleCenter);
        Top(hint.rectTransform, 66f, 26f);

        PieceType[] types = { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight };
        string[] labels = { "Queen\n[Q]", "Rook\n[R]", "Bishop\n[B]", "Knight\n[N]" };
        for (int i = 0; i < types.Length; i++)
        {
            PieceType type = types[i];
            Button button = MakeButton("Promote " + type, card, labels[i], () => game.Promote(type), i == 0);
            Place(button.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero, new Vector2(24f + i * 110f, 28f), new Vector2(126f + i * 110f, 106f));
        }
    }

    private void CreateResultPanel()
    {
        RectTransform overlay = Overlay("Result", out RectTransform card, 440f, 226f);
        resultPanel = overlay.gameObject;
        Text heading = Label("Result heading", card, "GAME OVER", 14, AccentColor, TextAnchor.MiddleCenter);
        Top(heading.rectTransform, 24f, 25f);
        resultText = Label("Result text", card, string.Empty, 24, InkColor, TextAnchor.MiddleCenter);
        Top(resultText.rectTransform, 59f, 70f);
        Button restart = MakeButton("Play again", card, "Play again", () => game.RestartGame(), true);
        Place(restart.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-125f, 28f), new Vector2(125f, 74f));
    }

    private void CreateConfirmationPanel()
    {
        RectTransform overlay = Overlay("Confirmation", out RectTransform card, 440f, 224f);
        overlay.anchorMax = Vector2.one;
        confirmationPanel = overlay.gameObject;
        Text heading = Label("Confirmation heading", card, "CONFIRM ACTION", 14, AccentColor, TextAnchor.MiddleCenter);
        Top(heading.rectTransform, 23f, 25f);
        confirmationText = Label("Confirmation text", card, string.Empty, 21, InkColor, TextAnchor.MiddleCenter);
        Top(confirmationText.rectTransform, 60f, 66f);
        Button cancel = MakeButton("Cancel", card, "Cancel", CloseConfirmation, false);
        Place(cancel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0.5f, 0f), new Vector2(24f, 27f), new Vector2(-6f, 73f));
        Button confirm = MakeButton("Confirm", card, "Confirm", ConfirmAction, true);
        Place(confirm.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(6f, 27f), new Vector2(-24f, 73f));
        confirmationPanel.SetActive(false);
    }

    private void RequestNewGame()
    {
        if (!game.HasMoves || game.IsGameOver)
        {
            game.RestartGame();
            return;
        }

        ShowConfirmation("Start a new game?\nThe current game will be cleared.", game.RestartGame);
    }

    private void RequestResign()
    {
        ShowConfirmation(game.Turn + " resigns?\nThe other player will win.", game.Resign);
    }

    private void ShowConfirmation(string message, Action action)
    {
        confirmationText.text = message;
        confirmationAction = action;
        confirmationPanel.SetActive(true);
        confirmationPanel.transform.SetAsLastSibling();
    }

    private void CloseConfirmation()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        confirmationAction = null;
    }

    private void ConfirmAction()
    {
        Action action = confirmationAction;
        CloseConfirmation();
        action?.Invoke();
    }

    private RectTransform Overlay(string name, out RectTransform card, float width, float height)
    {
        RectTransform overlay = Box(name + " overlay", canvasRoot, new Color(0.015f, 0.025f, 0.04f, 0.58f));
        Place(overlay, Vector2.zero, new Vector2(BoardViewportWidth, 1f), Vector2.zero, Vector2.zero);
        card = Box(name + " card", overlay, PanelColor);
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(width, height);
        card.anchoredPosition = Vector2.zero;
        RectTransform accent = Box(name + " accent", card, AccentColor);
        Place(accent, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -3f), Vector2.zero);
        overlay.gameObject.SetActive(false);
        return overlay;
    }

    private Button MakeButton(string name, Transform parent, string caption, Action action, bool primary)
    {
        RectTransform rect = Box(name, parent, primary ? AccentColor : CardColor);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(0.76f, 0.76f, 0.76f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.43f, 0.43f, 0.43f, 0.7f);
        button.colors = colors;
        button.onClick.AddListener(() => action?.Invoke());
        Text text = Label(name + " label", rect, caption, 16, primary ? PanelColor : InkColor, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 6f, 4f, 6f, 4f);
        return button;
    }

    private Text Label(string name, Transform parent, string content, int size, Color color, TextAnchor alignment)
    {
        RectTransform rect = Rect(name, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.text = content;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        text.supportRichText = false;
        return text;
    }

    private static RectTransform Box(string name, Transform parent, Color color)
    {
        RectTransform rect = Rect(name, parent);
        rect.gameObject.AddComponent<Image>().color = color;
        return rect;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 insetMin, Vector2 insetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = insetMin;
        rect.offsetMax = insetMax;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        Place(rect, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));
    }

    private static void Top(RectTransform rect, float top, float height)
    {
        Place(rect, new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -top - height), new Vector2(-24f, -top));
    }

    private static string CapturedOrDash(string captured)
    {
        return string.IsNullOrWhiteSpace(captured) ? "-" : captured;
    }
}
