using KeypadCompanion.Domain;

namespace KeypadCompanion.Services;

public sealed class GestureResolver(TimeProvider? timeProvider = null) : IGestureResolver
{
    private sealed class GestureState
    {
        public DateTimeOffset PressStartedAt { get; set; }
        public bool IsPressed { get; set; }
        public DateTimeOffset? PendingTapReleasedAt { get; set; }
        public CancellationTokenSource? PendingSingleClickCancellation { get; set; }
    }

    private readonly object _sync = new();
    private readonly Dictionary<InputId, GestureState> _states = new();
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    private GestureSettings _settings = new();

    public event EventHandler<ResolvedTriggerEvent>? TriggerResolved;

    public void UpdateSettings(GestureSettings settings)
    {
        lock (_sync)
        {
            _settings = new GestureSettings
            {
                LongPressMilliseconds = Math.Max(100, settings.LongPressMilliseconds),
                DoubleClickMilliseconds = Math.Max(100, settings.DoubleClickMilliseconds),
            };
        }
    }

    public void HandleRawEvent(RawDeviceEvent deviceEvent)
    {
        switch (deviceEvent.Kind)
        {
            case RawDeviceEventKind.RotatedClockwise:
                Emit(deviceEvent.InputId, TriggerType.RotateCw, deviceEvent.Timestamp);
                return;
            case RawDeviceEventKind.RotatedCounterClockwise:
                Emit(deviceEvent.InputId, TriggerType.RotateCcw, deviceEvent.Timestamp);
                return;
        }

        lock (_sync)
        {
            var state = GetState(deviceEvent.InputId);
            switch (deviceEvent.Kind)
            {
                case RawDeviceEventKind.Pressed:
                    state.IsPressed = true;
                    state.PressStartedAt = deviceEvent.Timestamp;

                    if (state.PendingTapReleasedAt is not null &&
                        deviceEvent.Timestamp - state.PendingTapReleasedAt <= TimeSpan.FromMilliseconds(_settings.DoubleClickMilliseconds))
                    {
                        state.PendingSingleClickCancellation?.Cancel();
                    }

                    break;

                case RawDeviceEventKind.Released:
                    if (!state.IsPressed)
                    {
                        return;
                    }

                    state.IsPressed = false;
                    var pressDuration = deviceEvent.Timestamp - state.PressStartedAt;

                    if (pressDuration >= TimeSpan.FromMilliseconds(_settings.LongPressMilliseconds))
                    {
                        CancelPendingTap(state);
                        Emit(deviceEvent.InputId, TriggerType.LongPress, deviceEvent.Timestamp);
                        return;
                    }

                    if (state.PendingTapReleasedAt is not null &&
                        deviceEvent.Timestamp - state.PendingTapReleasedAt <= TimeSpan.FromMilliseconds(_settings.DoubleClickMilliseconds))
                    {
                        CancelPendingTap(state);
                        Emit(deviceEvent.InputId, TriggerType.DoubleClick, deviceEvent.Timestamp);
                        return;
                    }

                    state.PendingTapReleasedAt = deviceEvent.Timestamp;
                    state.PendingSingleClickCancellation?.Dispose();
                    state.PendingSingleClickCancellation = new CancellationTokenSource();
                    _ = DispatchSinglePressLaterAsync(deviceEvent.InputId, state.PendingTapReleasedAt.Value, state.PendingSingleClickCancellation.Token);
                    break;
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            foreach (var state in _states.Values)
            {
                CancelPendingTap(state);
            }
        }
    }

    private GestureState GetState(InputId inputId)
    {
        if (_states.TryGetValue(inputId, out var existing))
        {
            return existing;
        }

        var created = new GestureState();
        _states[inputId] = created;
        return created;
    }

    private async Task DispatchSinglePressLaterAsync(InputId inputId, DateTimeOffset releasedAt, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(_settings.DoubleClickMilliseconds), _timeProvider, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        lock (_sync)
        {
            var state = GetState(inputId);
            if (state.PendingTapReleasedAt != releasedAt)
            {
                return;
            }

            CancelPendingTap(state);
        }

        Emit(inputId, TriggerType.Press, releasedAt);
    }

    private void CancelPendingTap(GestureState state)
    {
        state.PendingTapReleasedAt = null;

        if (state.PendingSingleClickCancellation is not null)
        {
            state.PendingSingleClickCancellation.Cancel();
            state.PendingSingleClickCancellation.Dispose();
            state.PendingSingleClickCancellation = null;
        }
    }

    private void Emit(InputId inputId, TriggerType triggerType, DateTimeOffset timestamp)
    {
        TriggerResolved?.Invoke(this, new ResolvedTriggerEvent(inputId, triggerType, timestamp));
    }
}
