using ManagedDoom;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using System.Diagnostics;

namespace UnoDoom.Game;

/// <summary>
/// Touch input overlay for controlling the game on touch devices.
/// Provides virtual joysticks and action buttons.
/// </summary>
public sealed partial class TouchInputOverlay : UserControl
{
    private UnoUserInput? _input;
    private Doom? _doom;
    private int _frameCount;

    // Movement joystick state
    private uint? _movementPointerId;
    private Point _movementCenter;
    private Point _movementCurrent;
    private const double JoystickRadius = 75.0;
    private const double KnobRadius = 30.0;
    private const double DeadZone = 0.15;

    // Look joystick state
    private uint? _lookPointerId;
    private Point _lookCenter;
    private Point _lookCurrent;

    // Button pointer tracking for multi-touch
    private uint? _firePointerId;
    private uint? _usePointerId;

    // Button states
    private bool _firePressed;
    private bool _usePressed;
    private bool _runToggled;
    private int _currentWeapon = 1;

    // Virtual key states for BuildTicCmd
    private bool _moveForward;
    private bool _moveBackward;
    private bool _strafeLeft;
    private bool _strafeRight;
    private bool _turnLeft;
    private bool _turnRight;
    private int _weaponToSelect = -1;

    /// <summary>
    /// Event raised when the user requests to hide the overlay.
    /// </summary>
    public event EventHandler? HideRequested;

    public TouchInputOverlay()
    {
        this.InitializeComponent();

        // Set up touch handling
        this.PointerPressed += OnPointerPressed;
        this.PointerMoved += OnPointerMoved;
        this.PointerReleased += OnPointerReleased;
        this.PointerCanceled += OnPointerReleased;
        this.PointerCaptureLost += OnPointerReleased;

        // Initialize joystick centers
        _movementCenter = new Point(100, 100);
        _lookCenter = new Point(100, 100);
    }

    public void Initialize(UnoUserInput input, Doom doom)
    {
        _input = input;
        _doom = doom;
    }

    public void SetFrameCount(int frameCount)
    {
        _frameCount = frameCount;
    }

    /// <summary>
    /// Gets the current movement input values.
    /// </summary>
    public (float forward, float strafe, float turn) GetMovementInput()
    {
        float forward = 0;
        float strafe = 0;
        float turn = 0;

        // Calculate movement from left joystick
        if (_movementPointerId.HasValue)
        {
            var dx = _movementCurrent.X - _movementCenter.X;
            var dy = _movementCurrent.Y - _movementCenter.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            var normalizedDistance = Math.Min(distance / JoystickRadius, 1.0);

            if (normalizedDistance > DeadZone)
            {
                var angle = Math.Atan2(dy, dx);
                var adjustedDistance = (normalizedDistance - DeadZone) / (1.0 - DeadZone);

                // Forward/backward (Y axis, inverted)
                forward = (float)(-Math.Sin(angle) * adjustedDistance);
                // Strafe (X axis)
                strafe = (float)(Math.Cos(angle) * adjustedDistance);
            }
        }
        
        // Calculate turn from right joystick
        if (_lookPointerId.HasValue)
        {
            var dx = _lookCurrent.X - _lookCenter.X;
            var distance = Math.Abs(dx);
            var normalizedDistance = Math.Min(distance / JoystickRadius, 1.0);

            if (normalizedDistance > DeadZone)
            {
                var adjustedDistance = (normalizedDistance - DeadZone) / (1.0 - DeadZone);
                turn = (float)(Math.Sign(dx) * adjustedDistance);
            }
        }

        return (forward, strafe, turn);
    }

    public bool IsFirePressed => _firePressed;
    public bool IsUsePressed => _usePressed;
    public bool IsRunToggled => _runToggled;
    public int WeaponToSelect
    {
        get
        {
            var weapon = _weaponToSelect;
            _weaponToSelect = -1;
            return weapon;
        }
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var position = point.Position;
        var pointerId = point.PointerId;

        // Check if touching movement area (left side)
        if (IsInMovementArea(position) && !_movementPointerId.HasValue)
        {
            _movementPointerId = pointerId;
            _movementCenter = GetMovementAreaCenter();
            _movementCurrent = position;
            UpdateMovementKnob();
            CapturePointer(e.Pointer);
            e.Handled = true;
            return;
        }

        // Check if touching look area (right side, but not buttons)
        if (IsInLookArea(position) && !_lookPointerId.HasValue)
        {
            _lookPointerId = pointerId;
            _lookCenter = GetLookAreaCenter();
            _lookCurrent = position;
            UpdateLookKnob();
            CapturePointer(e.Pointer);
            e.Handled = true;
            return;
        }

        // Check action buttons
        if (IsInFireButton(position) && !_firePointerId.HasValue)
        {
            _firePointerId = pointerId;
            SetFirePressed(true);
            CapturePointer(e.Pointer);
            e.Handled = true;
            return;
        }

        if (IsInUseButton(position) && !_usePointerId.HasValue)
        {
            _usePointerId = pointerId;
            SetUsePressed(true);
            CapturePointer(e.Pointer);
            e.Handled = true;
            return;
        }

        if (IsInMenuButton(position))
        {
            SendKeyPress(DoomKey.Escape);
            e.Handled = true;
            return;
        }

        if (IsInHideButton(position))
        {
            HideRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }

        if (IsInRunButton(position))
        {
            _runToggled = !_runToggled;
            UpdateRunButtonVisual();
            e.Handled = true;
            return;
        }

        if (IsInMapButton(position))
        {
            SendKeyPress(DoomKey.Tab);
            e.Handled = true;
            return;
        }

        if (IsInWeaponPrevButton(position))
        {
            CycleWeapon(-1);
            e.Handled = true;
            return;
        }

        if (IsInWeaponNextButton(position))
        {
            CycleWeapon(1);
            e.Handled = true;
            return;
        }
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var position = point.Position;
        var pointerId = point.PointerId;

        if (_movementPointerId == pointerId)
        {
            _movementCurrent = position;
            UpdateMovementKnob();
            e.Handled = true;
        }
        else if (_lookPointerId == pointerId)
        {
            _lookCurrent = position;
            UpdateLookKnob();
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        var pointerId = point.PointerId;

        if (_movementPointerId == pointerId)
        {
            _movementPointerId = null;
            ResetMovementKnob();
            ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
        else if (_lookPointerId == pointerId)
        {
            _lookPointerId = null;
            ResetLookKnob();
            ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
        else if (_firePointerId == pointerId)
        {
            _firePointerId = null;
            SetFirePressed(false);
            ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
        else if (_usePointerId == pointerId)
        {
            _usePointerId = null;
            SetUsePressed(false);
            ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }

    private void UpdateMovementKnob()
    {
        var dx = _movementCurrent.X - _movementCenter.X;
        var dy = _movementCurrent.Y - _movementCenter.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        // Clamp to joystick radius
        if (distance > JoystickRadius)
        {
            dx = dx / distance * JoystickRadius;
            dy = dy / distance * JoystickRadius;
        }

        // Update knob position (offset by knob radius for centering)
        Canvas.SetLeft(JoystickKnob, 70 + dx);
        Canvas.SetTop(JoystickKnob, 70 + dy);

        // Update virtual key states based on joystick position
        var normalizedDistance = Math.Min(distance / JoystickRadius, 1.0);
        if (normalizedDistance > DeadZone)
        {
            var angle = Math.Atan2(dy, dx);
            // Convert angle to direction flags
            // Up: -PI/2, Down: PI/2, Left: PI, Right: 0
            _moveForward = dy < -JoystickRadius * DeadZone;
            _moveBackward = dy > JoystickRadius * DeadZone;
            _strafeLeft = dx < -JoystickRadius * DeadZone;
            _strafeRight = dx > JoystickRadius * DeadZone;
        }
        else
        {
            _moveForward = false;
            _moveBackward = false;
            _strafeLeft = false;
            _strafeRight = false;
        }
    }

    private void UpdateLookKnob()
    {
        var dx = _lookCurrent.X - _lookCenter.X;
        var dy = _lookCurrent.Y - _lookCenter.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        // Clamp to joystick radius
        if (distance > JoystickRadius)
        {
            dx = dx / distance * JoystickRadius;
            dy = dy / distance * JoystickRadius;
        }

        // Update knob position
        Canvas.SetLeft(LookJoystickKnob, 70 + dx);
        Canvas.SetTop(LookJoystickKnob, 70 + dy);

        // Update turn state based on horizontal position
        var horizontalDeadZone = JoystickRadius * DeadZone;
        _turnLeft = dx < -horizontalDeadZone;
        _turnRight = dx > horizontalDeadZone;
    }

    private void ResetMovementKnob()
    {
        Canvas.SetLeft(JoystickKnob, 70);
        Canvas.SetTop(JoystickKnob, 70);
        _moveForward = false;
        _moveBackward = false;
        _strafeLeft = false;
        _strafeRight = false;
    }

    private void ResetLookKnob()
    {
        Canvas.SetLeft(LookJoystickKnob, 70);
        Canvas.SetTop(LookJoystickKnob, 70);
        _turnLeft = false;
        _turnRight = false;
    }

    private Point GetMovementAreaCenter()
    {
        var transform = MovementArea.TransformToVisual(this);
        var topLeft = transform.TransformPoint(new Point(0, 0));
        return new Point(topLeft.X + 100, topLeft.Y + 100);
    }

    private Point GetLookAreaCenter()
    {
        var transform = LookArea.TransformToVisual(this);
        var topLeft = transform.TransformPoint(new Point(0, 0));
        return new Point(topLeft.X + 100, topLeft.Y + 100);
    }

    private bool IsInMovementArea(Point position)
    {
        return IsPointInElement(position, MovementArea);
    }

    private bool IsInLookArea(Point position)
    {
        return IsPointInElement(position, LookArea);
    }

    private bool IsInFireButton(Point position)
    {
        return IsPointInElement(position, FireButton);
    }

    private bool IsInUseButton(Point position)
    {
        return IsPointInElement(position, UseButton);
    }

    private bool IsInMenuButton(Point position)
    {
        return IsPointInElement(position, MenuButton);
    }

    private bool IsInHideButton(Point position)
    {
        return IsPointInElement(position, HideButton);
    }

    private bool IsInRunButton(Point position)
    {
        return IsPointInElement(position, RunButton);
    }

    private bool IsInMapButton(Point position)
    {
        return IsPointInElement(position, MapButton);
    }

    private bool IsInWeaponPrevButton(Point position)
    {
        return IsPointInElement(position, WeaponPrevButton);
    }

    private bool IsInWeaponNextButton(Point position)
    {
        return IsPointInElement(position, WeaponNextButton);
    }

    private bool IsPointInElement(Point position, FrameworkElement element)
    {
        try
        {
            var transform = element.TransformToVisual(this);
            var topLeft = transform.TransformPoint(new Point(0, 0));
            var bounds = new Rect(topLeft.X, topLeft.Y, element.ActualWidth, element.ActualHeight);
            return bounds.Contains(position);
        }
        catch
        {
            return false;
        }
    }

    private void SetFirePressed(bool pressed)
    {
        if (_firePressed != pressed)
        {
            _firePressed = pressed;
            FireButton.Background = new SolidColorBrush(
                pressed ? Windows.UI.Color.FromArgb(0xC0, 0xFF, 0x00, 0x00)
                        : Windows.UI.Color.FromArgb(0x80, 0xFF, 0x00, 0x00));

            if (_doom != null && _input != null)
            {
                var eventType = pressed ? EventType.KeyDown : EventType.KeyUp;
                _input.SetKeyStatus(eventType, DoomKey.LControl, _doom, new EventTimestamp(_frameCount));
            }
        }
    }

    private void SetUsePressed(bool pressed)
    {
        if (_usePressed != pressed)
        {
            _usePressed = pressed;
            UseButton.Background = new SolidColorBrush(
                pressed ? Windows.UI.Color.FromArgb(0xC0, 0x00, 0xFF, 0x00)
                        : Windows.UI.Color.FromArgb(0x80, 0x00, 0xFF, 0x00));

            if (_doom != null && _input != null)
            {
                var eventType = pressed ? EventType.KeyDown : EventType.KeyUp;
                _input.SetKeyStatus(eventType, DoomKey.Space, _doom, new EventTimestamp(_frameCount));
            }
        }
    }

    private void UpdateRunButtonVisual()
    {
        RunButton.Background = new SolidColorBrush(
            _runToggled ? Windows.UI.Color.FromArgb(0xC0, 0xFF, 0xFF, 0x00)
                        : Windows.UI.Color.FromArgb(0x60, 0x80, 0x80, 0x80));
        RunButtonText.Text = _runToggled ? "WALK" : "RUN";
    }

    private void SendKeyPress(DoomKey key)
    {
        if (_doom != null && _input != null)
        {
            _input.SetKeyStatus(EventType.KeyDown, key, _doom, new EventTimestamp(_frameCount));
            // Schedule key release
            DispatcherQueue.TryEnqueue(() =>
            {
                _input?.SetKeyStatus(EventType.KeyUp, key, _doom, new EventTimestamp(_frameCount));
            });
        }
    }

    private void CycleWeapon(int direction)
    {
        _currentWeapon += direction;
        if (_currentWeapon < 1) _currentWeapon = 7;
        if (_currentWeapon > 7) _currentWeapon = 1;

        _weaponToSelect = _currentWeapon;

        // Send weapon key
        var weaponKey = _currentWeapon switch
        {
            1 => DoomKey.Num1,
            2 => DoomKey.Num2,
            3 => DoomKey.Num3,
            4 => DoomKey.Num4,
            5 => DoomKey.Num5,
            6 => DoomKey.Num6,
            7 => DoomKey.Num7,
            _ => DoomKey.Num1
        };

        SendKeyPress(weaponKey);
    }

    /// <summary>
    /// Apply touch input to the TicCmd. Call this from UnoUserInput.BuildTicCmd.
    /// </summary>
    public void ApplyToTicCmd(TicCmd cmd, int speed)
    {
        var (forward, strafe, turn) = GetMovementInput();

        // Apply movement
        if (Math.Abs(forward) > 0.1f)
        {
            var moveSpeed = _runToggled ? 1 : speed;
            cmd.ForwardMove += (sbyte)(forward * PlayerBehavior.ForwardMove[moveSpeed]);
        }

        if (Math.Abs(strafe) > 0.1f)
        {
            var moveSpeed = _runToggled ? 1 : speed;
            cmd.SideMove += (sbyte)(strafe * PlayerBehavior.SideMove[moveSpeed]);
        }

        // Apply turning
        if (Math.Abs(turn) > 0.1f)
        {
            var turnSpeed = _runToggled ? 1 : speed;
            cmd.AngleTurn -= (short)(turn * PlayerBehavior.AngleTurn[turnSpeed] * 2);
        }

        // Apply fire
        if (_firePressed)
        {
            cmd.Buttons |= TicCmdButtons.Attack;
        }

        // Apply use
        if (_usePressed)
        {
            cmd.Buttons |= TicCmdButtons.Use;
        }
    }

    // Expose movement states for UnoUserInput
    public bool IsMoveForward => _moveForward || (_movementPointerId.HasValue && GetMovementInput().forward > 0.1f);
    public bool IsMoveBackward => _moveBackward || (_movementPointerId.HasValue && GetMovementInput().forward < -0.1f);
    public bool IsStrafeLeft => _strafeLeft || (_movementPointerId.HasValue && GetMovementInput().strafe < -0.1f);
    public bool IsStrafeRight => _strafeRight || (_movementPointerId.HasValue && GetMovementInput().strafe > 0.1f);
    public bool IsTurnLeft => _turnLeft || (_lookPointerId.HasValue && GetMovementInput().turn < -0.1f);
    public bool IsTurnRight => _turnRight || (_lookPointerId.HasValue && GetMovementInput().turn > 0.1f);
}
