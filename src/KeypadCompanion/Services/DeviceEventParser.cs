using KeypadCompanion.Domain;

namespace KeypadCompanion.Services;

public sealed class DeviceEventParser
{
    public bool TryParse(string? line, out RawDeviceEvent? deviceEvent)
    {
        deviceEvent = null;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var trimmed = line.Trim();
        var now = DateTimeOffset.Now;

        if (trimmed.StartsWith("KEY_", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 3 &&
                int.TryParse(parts[1], out var keyNumber) &&
                keyNumber is >= 1 and <= 8)
            {
                var inputId = (InputId)(keyNumber - 1);
                deviceEvent = parts[2].Equals("DOWN", StringComparison.OrdinalIgnoreCase)
                    ? new RawDeviceEvent(inputId, RawDeviceEventKind.Pressed, now)
                    : parts[2].Equals("UP", StringComparison.OrdinalIgnoreCase)
                        ? new RawDeviceEvent(inputId, RawDeviceEventKind.Released, now)
                        : null;
            }

            return deviceEvent is not null;
        }

        deviceEvent = trimmed switch
        {
            "ENC_BTN_DOWN" => new RawDeviceEvent(InputId.EncoderButton, RawDeviceEventKind.Pressed, now),
            "ENC_BTN_UP" => new RawDeviceEvent(InputId.EncoderButton, RawDeviceEventKind.Released, now),
            "ENC_CW" => new RawDeviceEvent(InputId.EncoderClockwise, RawDeviceEventKind.RotatedClockwise, now),
            "ENC_CCW" => new RawDeviceEvent(InputId.EncoderCounterClockwise, RawDeviceEventKind.RotatedCounterClockwise, now),
            _ => null,
        };

        return deviceEvent is not null;
    }
}
