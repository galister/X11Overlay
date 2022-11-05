using X11Overlay.Core;
using X11Overlay.GFX;
using X11Overlay.Numerics;
using X11Overlay.Overlays.Simple;
using X11Overlay.UI;

namespace X11Overlay.Overlays;

/// <summary>
/// An overlay that shows time and has some buttons
/// </summary>
public class Watch : InteractableOverlay
{
    private static Watch? _instance;
    
    private readonly Canvas _canvas;

    private readonly Vector3 _localPosition = new(-0.05f, -0.05f, 0.15f);

    private readonly List<Control> _batteryControls = new();

    private readonly BaseOverlay _keyboard;
    private readonly BaseOverlay[] _screens;
    
    public Watch(string? altTimezone1, string? altTimezone2, BaseOverlay keyboard, IEnumerable<BaseOverlay> screens) : base("Watch")
    {
        if (_instance != null)
            throw new InvalidOperationException("Can't have more than one Watch!");
        _instance = this;
        
        _keyboard = keyboard;
        _screens = screens.ToArray();
        
        WidthInMeters = 0.125f;
        ShowHideBinding = false;
        WantVisible = true;
        ZOrder = 67;

        // 400 x 200
        _canvas = new Canvas(400, 200);

        Canvas.CurrentBgColor = HexColor.FromRgb("#353535");
        
        _canvas.AddControl(new Panel(0, 0, 400, 200));

        Canvas.CurrentFgColor = HexColor.FromRgb("#FFFFFF");
        
        Canvas.CurrentFont = new Font("LiberationSans-Bold.ttf", 46);
        _canvas.AddControl(new DateTimeLabel("HH:mm", TimeZoneInfo.Local, 19, 107, 200, 50));

        var b14Pt = Canvas.CurrentFont = new Font("LiberationSans-Bold.ttf", 14);
        _canvas.AddControl(new DateTimeLabel("d", TimeZoneInfo.Local, 20, 80, 200, 50));
        _canvas.AddControl(new DateTimeLabel("dddd", TimeZoneInfo.Local, 20, 60, 200, 50));

        Font? b24Pt = null;
        
        if (altTimezone1 != null)
        {
            
            Canvas.CurrentFgColor = HexColor.FromRgb("#99BBAA");
            var tz = TimeZoneInfo.FindSystemTimeZoneById(altTimezone1);
            var tzDisplay = altTimezone1.Split('/').Last();
            
            Canvas.CurrentFont = b14Pt;
            _canvas.AddControl(new Label(tzDisplay, 210, 137, 200, 50));
            
            b24Pt = Canvas.CurrentFont = new Font("LiberationSans-Bold.ttf", 24);
            _canvas.AddControl(new DateTimeLabel("HH:mm", tz, 210, 107, 200, 50));
        }
        
        if (altTimezone2 != null)
        {
            Canvas.CurrentFgColor = HexColor.FromRgb("#AA99BB");
            var tz = TimeZoneInfo.FindSystemTimeZoneById(altTimezone2);
            var tzDisplay = altTimezone2.Split('/').Last();

            Canvas.CurrentFont = b14Pt;
            _canvas.AddControl(new Label(tzDisplay, 210, 82, 200, 50));

            b24Pt ??= new Font("LiberationSans-Bold.ttf", 24);
            Canvas.CurrentFont = b24Pt;
            _canvas.AddControl(new DateTimeLabel("HH:mm", tz, 210, 52, 200, 50));
        }

        // Volume controls
        
        Canvas.CurrentBgColor = HexColor.FromRgb("#222222");
        Canvas.CurrentFgColor = HexColor.FromRgb("#AAAAAA");
        Canvas.CurrentFont = b14Pt;

        _canvas.AddControl(new Panel(325, 114, 50, 36));
        _canvas.AddControl(new Panel(325, 50, 50, 36));
        _canvas.AddControl(new Label("Vol", 334, 94, 50, 30));

        Canvas.CurrentBgColor = HexColor.FromRgb("#505050");
        
        _canvas.AddControl(new Button("+", 327, 116, 46, 32));
        _canvas.AddControl(new Button("-", 327, 52, 46, 32));
        
        // Bottom row
        
        var numButtons = _screens.Length + 1;
        var btnWidth = 400 / numButtons;
        
        Canvas.CurrentBgColor = HexColor.FromRgb("#406050");
        Canvas.CurrentFgColor = HexColor.FromRgb("#AACCBB");

        _canvas.AddControl(new Button("Kbd", 2, 2, (uint)btnWidth - 4U, 36)
        {
            PointerDown = () => _keyboard.ToggleVisible()
        });

        Canvas.CurrentBgColor = HexColor.FromRgb("#405060");        
        Canvas.CurrentFgColor = HexColor.FromRgb("#AACCBB");
        Canvas.CurrentFgColor = HexColor.FromRgb("#AABBCC");

        for (var s = 1; s <= _screens.Length; s++)
        {
            var screen = _screens[s - 1];
            _canvas.AddControl(new Button($"Scr {s}", btnWidth * s + 2, 2, (uint)btnWidth - 4U, 36)
            {
                PointerDown = () => screen.ToggleVisible()
            });
        }
        
        _canvas.BuildInteractiveLayer();
    }

    private void OnBatteryStatesUpdated()
    {
        foreach (var c in _batteryControls) 
            _canvas.RemoveControl(c);
        _batteryControls.Clear();
        
        var numStates = InputManager.BatteryStates.Count;

        if (numStates > 0)
        {
            var stateWidth = 400 / numStates;

            for (var s = 0; s < numStates; s++)
            {
                var state = InputManager.BatteryStates[s];
                var indicator = new BatteryIndicator(state, stateWidth * s + 2, 162, (uint)stateWidth - 4U, 36);
                _canvas.AddControl(indicator);
                _batteryControls.Add(indicator);
            }
        }
        _canvas.MarkDirty();
    }

    public override void Initialize()
    {
        Texture = _canvas.Initialize();
        
        UpdateInteractionTransform();
        base.Initialize();
    }

    protected internal override void Render()
    {
        _canvas.Render();
        
        base.Render();
    }

    protected internal override void AfterInput(bool batteryStateUpdated)
    {
        base.AfterInput(batteryStateUpdated);
        
        var controller = InputManager.PoseState["LeftHand"];

        Transform = controller.TranslatedLocal(_localPosition);
        UploadTransform();
        
        if (batteryStateUpdated)
            OnBatteryStatesUpdated();
    }

    protected internal override void OnPointerDown(PointerHit hitData)
    {
        base.OnPointerDown(hitData);
        _canvas.OnPointerDown(hitData.uv, hitData.hand);
    }

    protected internal override void OnPointerUp(PointerHit hitData)
    {
        base.OnPointerUp(hitData);
        _canvas.OnPointerUp(hitData.hand);
    }

    protected internal override void OnPointerHover(PointerHit hitData)
    {
        base.OnPointerHover(hitData);
        _canvas.OnPointerHover(hitData.uv, hitData.hand);
    }

    protected internal override void OnPointerLeft(LeftRight hand)
    {
        base.OnPointerLeft(hand);
        _canvas.OnPointerLeft(hand);
    }
}