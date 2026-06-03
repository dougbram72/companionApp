namespace KeypadCompanion.Domain;

public enum RawDeviceEventKind
{
    Pressed,
    Released,
    RotatedClockwise,
    RotatedCounterClockwise,
}

public sealed record RawDeviceEvent(
    InputId InputId,
    RawDeviceEventKind Kind,
    DateTimeOffset Timestamp);
