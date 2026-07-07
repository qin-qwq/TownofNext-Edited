using System;
using TONE.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;
using static TONE.Translator;

namespace TONE;

// 参考：https://github.com/Gurge44/EndlessHostRoles/blob/main/Modules/ClientControlGUI.cs
public class ClientControlGUI : MonoBehaviour
{
    public static ClientControlGUI Instance;
    public bool IsOpen;
    public static Rect PanelRect => Instance != null && Instance.IsOpen ? Instance._windowRect : Rect.zero;
    public static Rect ToggleRect { get; private set; }

    private Vector2 _scroll;
    private float _contentH;
    private Rect _windowRect;
    private bool _dragging;
    private Vector2 _dragOffset;
    private bool _windowInitialized;

    private readonly Dictionary<string, bool> _sectionExpanded = new();
    private readonly Dictionary<string, float> _sectionCurrentHeight = new();
    private readonly Dictionary<string, float> _sectionTargetHeight = new();
    private readonly Dictionary<string, float> _sectionFullHeight = new();

    private float _toggleRotation;

    private float _panelAnimProgress;
    private float _panelAnimTarget;
    private const float PanelAnimSpeed = 8f;

    private static float PlatformScale => OperatingSystem.IsAndroid() ? 0.6f : 0.5f;
    private static float Scale => Screen.width / 1080f * PlatformScale;
    private static int FontSize => Mathf.Max(12, Mathf.RoundToInt(21f * Scale));
    private static float ButtonHeight => 66f * Scale;
    private static float ButtonWidth => (OperatingSystem.IsAndroid() ? 360f : 340f) * Scale;
    private static float Padding => 10f * Scale;
    private static int ChipFontSize => Mathf.Max(10, FontSize - 4);
    private static float ScrollbarColumnWidth => (OperatingSystem.IsAndroid() ? 42f : 22f) * Scale;
    private float _lastScale = -1f;

    private GUIStyle _sAction, _sHost, _sDanger, _sSection, _sToggle, _sWindow, _sTitleBar, _sDragHint;
    private Camera _cam;
    public bool shouldSkip = false;

    private void Awake()
    {
        _cam = Camera.main;
        Instance = this;
        _panelAnimProgress = _panelAnimTarget = IsOpen ? 1f : 0f;
        SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
        Logger.Info("ClientControlGUI initialised", "ClientControlGUI");
    }

    private void OnDestroy()
    {
        SceneManager.remove_sceneLoaded((Action<Scene, LoadSceneMode>)OnSceneLoaded);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cam = Camera.main;
        IsOpen = false;
        _panelAnimProgress = _panelAnimTarget = 0f;
    }

    private void Update()
    {
        if (!Mathf.Approximately(_panelAnimProgress, _panelAnimTarget))
        {
            _panelAnimProgress = Mathf.MoveTowards(_panelAnimProgress, _panelAnimTarget, Time.deltaTime * PanelAnimSpeed);
        }
    }

    public void TogglePanel()
    {
        IsOpen = !IsOpen;
        _panelAnimTarget = IsOpen ? 1f : 0f;
    }

    private void RebuildStyles()
    {
        _lastScale = Scale;

        int toggleSize = Mathf.Max(1, Mathf.RoundToInt(48f * Scale));
        int toggleRadius = toggleSize / 4;

        _sToggle = new GUIStyle
        {
            fontSize = FontSize + 4,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = RoundedTexture(toggleSize, toggleSize, toggleRadius, new Color(0.25f, 0.18f, 0.20f, 1f), new Color(0.65f, 0.55f, 0.58f, 1f)), textColor = Color.white },
            hover = { background = RoundedTexture(toggleSize, toggleSize, toggleRadius, new Color(0.35f, 0.28f, 0.30f, 1f), new Color(0.75f, 0.65f, 0.68f, 1f)), textColor = Color.white },
            active = { background = RoundedTexture(toggleSize, toggleSize, toggleRadius, new Color(0.15f, 0.10f, 0.12f, 1f), new Color(0.40f, 0.32f, 0.35f, 1f)), textColor = Color.white }
        };

        int winW = Mathf.Max(1, Mathf.RoundToInt(ButtonWidth + Padding * 4f + ScrollbarColumnWidth));
        int winH = Mathf.Max(1, Mathf.RoundToInt(Screen.height * (OperatingSystem.IsAndroid() ? 0.82f : 0.65f)));

        _sWindow = new GUIStyle
        {
            normal = { background = RoundedTexture(winW, winH, 22, new Color(0.12f, 0.09f, 0.11f, 1f), new Color(0.30f, 0.23f, 0.25f, 1f)) }
        };

        _sTitleBar = new GUIStyle
        {
            fontSize = FontSize + 3,
            fontStyle = FontStyle.BoldAndItalic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.85f, 0.90f, 1f) }
        };

        _sDragHint = new GUIStyle
        {
            fontSize = Mathf.Max(10, FontSize - 5),
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.80f, 0.65f, 0.68f, 1f) }
        };

        _sSection = new GUIStyle
        {
            fontSize = FontSize,
            fontStyle = FontStyle.BoldAndItalic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.80f, 0.85f, 1f) }
        };

        _sAction = MakeBtn(
            new Color(0.35f, 0.25f, 0.28f, 1f),
            new Color(0.65f, 0.50f, 0.55f, 1f),
            new Color(0.20f, 0.14f, 0.16f, 1f)
        );

        _sHost = MakeBtn(
            new Color(0.45f, 0.35f, 0.38f, 1f),
            new Color(0.75f, 0.60f, 0.65f, 1f),
            new Color(0.25f, 0.18f, 0.20f, 1f)
        );

        _sDanger = MakeBtn(
            new Color(0.38f, 0.07f, 0.07f, 1f),
            new Color(0.60f, 0.12f, 0.12f, 1f),
            new Color(0.24f, 0.04f, 0.04f, 1f)
        );
    }

    private static GUIStyle MakeBtn(Color normal, Color hover, Color active)
    {
        int width = Mathf.Max(1, Mathf.RoundToInt(ButtonWidth));
        int height = Mathf.Max(1, Mathf.RoundToInt(ButtonHeight));
        int radius = Mathf.Max(1, Mathf.RoundToInt(ButtonHeight * 0.38f));
        return new GUIStyle
        {
            fontSize = FontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            richText = true,
            normal = { background = RoundedTexture(width, height, radius, normal, Lift(normal, 0.10f)), textColor = Color.white },
            hover = { background = RoundedTexture(width, height, radius, hover, Lift(hover, 0.10f)), textColor = Color.white },
            active = { background = RoundedTexture(width, height, radius, active, Lift(active, 0.06f)), textColor = Color.white }
        };
    }

    private static Color Lift(Color color, float add) =>
        new(Mathf.Clamp01(color.r + add), Mathf.Clamp01(color.g + add), Mathf.Clamp01(color.b + add), 1f);

    private static Texture2D RoundedTexture(int width, int height, int radius, Color fill, Color edge)
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        radius = Mathf.Clamp(radius, 0, Mathf.Min(width, height) / 2);
        var tex = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Bilinear
        };

        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                float a = CornerAlpha(px, py, width, height, radius);
                Color c = a <= 0f ? Color.clear
                    : a >= 1f ? fill
                    : Color.Lerp(edge, fill, a);
                tex.SetPixel(px, py, c);
            }
        }
        tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        return tex;
    }

    private static float CornerAlpha(int px, int py, int w, int h, int r)
    {
        int cx, cy;
        if (px < r && py < r) { cx = r; cy = r; }
        else if (px >= w - r && py < r) { cx = w - r; cy = r; }
        else if (px < r && py >= h - r) { cx = r; cy = h - r; }
        else if (px >= w - r && py >= h - r) { cx = w - r; cy = h - r; }
        else return 1f;

        float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
        if (d >= r + 1f) return 0f;
        if (d <= r - 1f) return 1f;
        return r + 0.5f - d;
    }

    private void InitWindowRect()
    {
        float w = ButtonWidth + Padding * 4f + ScrollbarColumnWidth;
        float h = Screen.height * (OperatingSystem.IsAndroid() ? 0.82f : 0.65f);
        _windowRect = new Rect(20f * Scale, (Screen.height - h) * 0.5f, w, h);
        _windowInitialized = true;
    }

    private void OnGUI()
    {
        if (!HudManager.InstanceExists) return;
        if (!_windowInitialized) InitWindowRect();
        if (Math.Abs(_lastScale - Scale) > 0.01f) RebuildStyles();

        float targetRotation = IsOpen ? 90f : 0f;
        _toggleRotation = Mathf.LerpAngle(_toggleRotation, targetRotation, Time.deltaTime * 10f);

        HandleDrag();
        DrawToggle();

        if (_panelAnimProgress > 0.001f)
            DrawWindow();
    }

    private void DrawToggle()
    {
        float size = 48f * Scale;
        float x, y;

        if (IsOpen || _panelAnimProgress > 0.001f)
        {
            x = _windowRect.x + _windowRect.width + 8f * Scale;
            y = _windowRect.y + (_windowRect.height - size) * 0.5f;
        }
        else
        {
            x = Screen.width * 0.3f - size * 0.5f;
            y = Screen.height - size - 10f * Scale;
        }

        ToggleRect = new Rect(x, y, size, size);
        bool fadeOut = !IsOpen && (GameStates.IsInGame || GameSettingMenu.Instance);
        Color prev = GUI.color;
        if (fadeOut) GUI.color = new Color(1f, 1f, 1f, 0.10f);

        Matrix4x4 oldMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(_toggleRotation, new Vector2(x + size / 2f, y + size / 2f));

        if (GUI.Button(new Rect(x, y, size, size), IsOpen ? "X" : "=", _sToggle))
        {
            TogglePanel();
        }

        GUI.matrix = oldMatrix;
        if (fadeOut) GUI.color = prev;
    }

    private void HandleDrag()
    {
        if (!IsOpen || _panelAnimProgress < 1f) return;

        Event e = Event.current;
        float titleH = ButtonHeight * 0.80f + Padding;
        var titleRect = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, titleH);

        switch (e.type)
        {
            case EventType.MouseDown when titleRect.Contains(e.mousePosition):
                _dragging = true;
                _dragOffset = e.mousePosition - new Vector2(_windowRect.x, _windowRect.y);
                e.Use();
                break;
            case EventType.MouseDrag when _dragging:
                float nx = Mathf.Clamp(e.mousePosition.x - _dragOffset.x, 0, Screen.width - _windowRect.width);
                float ny = Mathf.Clamp(e.mousePosition.y - _dragOffset.y, 0, Screen.height - _windowRect.height);
                _windowRect.x = nx;
                _windowRect.y = ny;
                e.Use();
                break;
            case EventType.MouseUp:
                _dragging = false;
                break;
        }
    }

    private void DrawWindow()
    {
        float scale = Mathf.Lerp(0.9f, 1f, _panelAnimProgress);
        float alpha = _panelAnimProgress;

        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;

        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), _windowRect.center);

        GUI.Box(_windowRect, "", _sWindow);

        float titleH = ButtonHeight * 0.80f + Padding;

        GUI.Label(
            new Rect(_windowRect.x, _windowRect.y + Padding * 0.6f, _windowRect.width, ButtonHeight * 0.55f),
            GetString("TONEClientControls"),
            _sTitleBar
        );

        GUI.Label(
            new Rect(_windowRect.x, _windowRect.y + ButtonHeight * 0.58f + Padding * 0.4f, _windowRect.width, ButtonHeight * 0.38f),
            GetString("DragToMove"),
            _sDragHint
        );

        float scrollY = _windowRect.y + titleH + Padding * 0.4f;
        float scrollH = _windowRect.height - titleH - Padding;
        float visibleW = _windowRect.width - Padding * 2f;
        float contentW = visibleW - ScrollbarColumnWidth - 1f;

        var outerRect = new Rect(_windowRect.x + Padding, scrollY, visibleW, scrollH);
        var innerRect = new Rect(0, 0, contentW, _contentH);

        GUI.skin.verticalScrollbar.fixedWidth = ScrollbarColumnWidth;
        GUI.skin.verticalScrollbarThumb.fixedWidth = ScrollbarColumnWidth;

        _scroll = GUI.BeginScrollView(outerRect, _scroll, innerRect, false, false);
        float y = Padding * 0.5f;
        DrawButtons(ref y, contentW);
        _contentH = y + Padding;
        GUI.EndScrollView();

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }

    private static string Label(string label, string shortcut = null)
    {
        if (OperatingSystem.IsAndroid() || shortcut == null) return label;
        return $"{label}\n<color=#7a9cbf><size={ChipFontSize}>{shortcut}</size></color>";
    }

    private void DrawButtons(ref float y, float w)
    {
        bool amHost = AmongUsClient.Instance && AmongUsClient.Instance.AmHost;
        bool inGame = GameStates.IsInGame;
        bool inLobby = GameStates.IsLobby;
        bool inMeeting = GameStates.IsMeeting;
        bool firstMeeting = MeetingStates.FirstMeeting;
        bool countdown = GameStates.IsCountDown;
        bool notJoined = GameStates.IsNotJoined;
        bool localAlive = PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.IsAlive();
        bool canMove = GameStates.IsCanMove;
        bool noGameEnd = Options.NoGameEnd.GetBool();

        var sections = new List<(string title, List<(string label, GUIStyle style, Action action, bool enabled)> buttons)>();

        var generalButtons = new List<(string label, GUIStyle style, Action action, bool enabled)>
        {
            (Label(GetString("DumpLog"), "CTRL + F1"), _sAction, () => { Logger.Info("Send logs", "ClientControlGUI"); Utils.DumpLog(); }, true),
            (Label(GetString("ReloadedFileColors"), "F5 + T"), _sAction, () => { Logger.Info("Reloaded Custom Translation File Colors", "ClientControlGUI"); LoadLangs(); Logger.SendInGame("Reloaded Custom Translation File"); }, true),
            (Label(GetString("ExportedCustomTranslation"), "F5 + X"), _sAction, () => { Logger.Info("Exported Custom Translation and Role File", "ClientControlGUI"); ExportCustomTranslation(); Logger.SendInGame("Exported Custom Translation File"); }, true),
            (Label(GetString("CopySettings"), "ALT + C"), _sAction, () => Utils.CopyCurrentSettings(), !notJoined),
            (Label(GetString("FixBlackscreen"), "F5 + F"), _sAction, () =>
            {
                Logger.Info("Attempted to fix Black Screen", "ClientControlGUI");
                ExileController.Instance?.ReEnableGameplay();
                ControllerManagerUpdatePatch.CompletedRepairingPlayer.Add(PlayerControl.LocalPlayer.PlayerId);
            }, !ControllerManagerUpdatePatch.CompletedRepairingPlayer.Contains(PlayerControl.LocalPlayer.PlayerId) && firstMeeting && inGame && !inMeeting),
            (InGameRoleInfoMenu.Showing ? GetString("HideRoleInfo") : GetString("ShowRoleInfo"), _sAction, () =>
            {
                if (!InGameRoleInfoMenu.Showing)
                {
                    InGameRoleInfoMenu.SetRoleInfoRef(PlayerControl.LocalPlayer);
                    InGameRoleInfoMenu.Show();
                }
                else if (InGameRoleInfoMenu.Showing) InGameRoleInfoMenu.Hide();
            }, inGame && (canMove || inMeeting) && GameModeBase.GetGameMode() is CustomGameMode.Standard)
        };
        sections.Add((GetString("General"), generalButtons));

        if (inLobby)
        {
            var lobbyButtons = new List<(string label, GUIStyle style, Action action, bool enabled)>();
            if (amHost && countdown)
            {
                lobbyButtons.Add((Label(GetString("StartImmediately"), "SHIFT"), _sHost, () =>
                {
                    var invalidColor = Main.EnumeratePlayerControls().Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId).ToArray();
                    if (invalidColor.Any())
                    {
                        GameStartManager.Instance.ResetStartState(); //Hope this works
                        Logger.SendInGame(GetString("Error.InvalidColorPreventStart"));
                        Logger.Info("Invalid Color Detected on force start!", "ClientControlGUI");
                    }
                    else
                    {
                        Logger.Info("Countdown timer changed to 0", "ClientControlGUI");
                        GameStartManager.Instance.countDownTimer = 0;
                    }
                }, true));
                lobbyButtons.Add((Label(GetString("CancelCountdown"), "C"), _sDanger, () =>
                {
                    Logger.Info("Reset Countdown", "ClientControlGUI");
                    GameStartManager.Instance.ResetStartState();
                    Logger.SendInGame(GetString("CancelStartCountDown"));
                }, true));
            }
            if (amHost)
            {
                lobbyButtons.Add((Label(GetString("ShowActiveSettings"), "CTRL + N"), _sHost, () => { Main.isChatCommand = true; Utils.ShowActiveSettings(); }, true));
                lobbyButtons.Add((Label(GetString("ResetAllOptions"), "CTRL + SHIFT + ENTER + DEL"), _sDanger, () => { OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValueNoRpc(x.DefaultValue)); Logger.SendInGame(GetString("RestTONESetting")); }, true));
            }
            if (lobbyButtons.Count > 0)
                sections.Add((GetString("Lobby"), lobbyButtons));
        }

        if (inGame)
        {
            var ingameButtons = new List<(string label, GUIStyle style, Action action, bool enabled)>();
            if (amHost && localAlive)
                ingameButtons.Add((Label(GetString("KillSelf"), "CTRL + SHIFT + ENTER + E"), _sDanger, () =>
                {
                    PlayerControl.LocalPlayer.SetDeathReason(PlayerState.DeathReason.etc);
                    PlayerControl.LocalPlayer.SetRealKiller(PlayerControl.LocalPlayer);
                    PlayerControl.LocalPlayer.RpcExileV3();

                    Utils.SendMessage(GetString("HostKillSelfByCommand"), title: $"<color=#ff0000>{GetString("DefaultSystemMessageTitle")}</color>");
                }, true));

            if (amHost)
            {
                if (!inMeeting)
                    ingameButtons.Add((Label(GetString("CallMeeting"), "SHIFT + ENTER + M"), _sHost, () => { if (GameStates.IsHideNSeek) return; if (Utils.GetTimeStamp() - Main.LastMeetingEnded < 2) return; PlayerControl.LocalPlayer.NoCheckStartMeeting(null, force: true); }, true));
                else
                {
                    ingameButtons.Add((Label(GetString("EndMeeting"), "SHIFT + ENTER + M"), _sHost, () =>
                    {
                        if (GameStates.IsHideNSeek) return;

                        foreach (var pva in MeetingHud.Instance.playerStates)
                        {
                            if (pva == null) continue;

                            if (pva.VotedFor < 253)
                                MeetingHud.Instance.RpcClearVote(pva.TargetPlayerId);
                        }
                        List<MeetingHud.VoterState> statesList = [];
                        MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
                        MeetingHud.Instance.RpcClose();
                    }, true));
                    ingameButtons.Add((Label(GetString("EndByVotes"), "F6"), _sHost, () => { shouldSkip = true; MeetingHud.Instance.CheckForEndVoting(); }, true));
                }
                ingameButtons.Add((Label(GetString("OpenYourChat"), "SHIFT + ENTER + C"), _sHost, () => HudManager.Instance.Chat.SetVisible(true), true));
                if (noGameEnd)
                    ingameButtons.Add((Label(GetString("ForceGameEnd"), "SHIFT + ENTER + L"), _sDanger, () =>
                    {
                        NameNotifyManager.Notice.Clear();
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                        GameManager.Instance.LogicFlow.CheckEndCriteria();
                        GameEndCheckerForNormal.GameIsEnded = true;
                        if (GameStates.IsHideNSeek)
                        {
                            GameEndCheckerForNormal.StartEndGame(GameOverReason.ImpostorDisconnect);
                        }
                    }, true));
            }

            if (ingameButtons.Count > 0)
            {
                if (amHost)
                {
                    var hostButtons = ingameButtons.FindAll(b =>
                        b.label.Contains(GetString("CallMeeting")) ||
                        b.label.Contains(GetString("EndMeeting")) ||
                        b.label.Contains(GetString("EndByVotes")) ||
                        b.label.Contains(GetString("OpenYourChat")) ||
                        b.label.Contains(GetString("OpenChatForAll")) ||
                        b.label.Contains(GetString("ForceGameEnd")));
                    var selfButtons = ingameButtons.FindAll(b => b.label.Contains(GetString("KillSelf")));

                    if (selfButtons.Count > 0) sections.Add((GetString("Self"), selfButtons));
                    if (hostButtons.Count > 0) sections.Add((GetString("HostControls"), hostButtons));
                }
                else
                {
                    sections.Add((GetString("InGame"), ingameButtons));
                }
            }
        }

        foreach (var section in sections)
        {
            DrawAnimatedSection(ref y, w, section.title, section.buttons, _sSection);
        }
    }

    private void DrawAnimatedSection(ref float y, float w, string title, List<(string label, GUIStyle style, Action action, bool enabled)> buttons, GUIStyle headerStyle)
    {
        EnsureSectionKey(title);
        if (!_sectionExpanded.ContainsKey(title))
            _sectionExpanded[title] = true;

        float headerH = ButtonHeight * 0.52f + Padding * 0.4f;
        y += Padding * 2f;

        var headerRect = new Rect(0, y, w, ButtonHeight * 0.50f);
        if (GUI.Button(headerRect, (_sectionExpanded[title] ? "▼ " : "▶ ") + title, headerStyle))
            ToggleSection(title);

        y += ButtonHeight * 0.52f + Padding * 0.4f;

        float fullContentH = 0f;
        foreach (var btn in buttons)
            if (btn.enabled)
                fullContentH += ButtonHeight + Padding * 0.7f;

        UpdateSectionHeight(title, fullContentH);
        float currentH = GetSectionCurrentHeight(title);

        var groupRect = new Rect(0, y, w, currentH);
        GUI.BeginGroup(groupRect);

        if (_sectionExpanded[title] || currentH > headerH * 0.1f)
        {
            float innerY = 0f;
            foreach (var btn in buttons)
            {
                if (!btn.enabled) continue;
                if (GUI.Button(new Rect(0, innerY, w, ButtonHeight), btn.label, btn.style))
                {
                    try { btn.action(); }
                    catch (Exception e) { Logger.Error(e.ToString(), "ClientControlGUI"); }
                }
                innerY += ButtonHeight + Padding * 0.7f;
            }
        }

        GUI.EndGroup();
        y += currentH;
    }

    private void EnsureSectionKey(string key)
    {
        if (!_sectionExpanded.ContainsKey(key))
        {
            _sectionExpanded[key] = true;
            _sectionCurrentHeight[key] = 0f;
            _sectionTargetHeight[key] = 0f;
            _sectionFullHeight[key] = 0f;
        }
    }

    private void ToggleSection(string key)
    {
        _sectionExpanded[key] = !_sectionExpanded[key];
        _sectionTargetHeight[key] = _sectionExpanded[key] ? _sectionFullHeight[key] : 0f;
    }

    private void UpdateSectionHeight(string key, float fullHeight)
    {
        _sectionFullHeight[key] = fullHeight;
        if (!_sectionTargetHeight.ContainsKey(key))
            _sectionTargetHeight[key] = fullHeight;
        else if (_sectionExpanded[key])
            _sectionTargetHeight[key] = fullHeight;
    }

    private float GetSectionCurrentHeight(string key)
    {
        if (!_sectionCurrentHeight.ContainsKey(key))
            _sectionCurrentHeight[key] = _sectionFullHeight.ContainsKey(key) ? _sectionFullHeight[key] : 0f;

        float current = _sectionCurrentHeight[key];
        float target = _sectionTargetHeight.ContainsKey(key) ? _sectionTargetHeight[key] : 0f;
        float newCurrent = Mathf.MoveTowards(current, target, Time.deltaTime * 800f);
        _sectionCurrentHeight[key] = newCurrent;
        return newCurrent;
    }
}
