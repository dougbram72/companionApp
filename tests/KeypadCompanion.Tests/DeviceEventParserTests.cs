using KeypadCompanion.Domain;
using KeypadCompanion.Services;

namespace KeypadCompanion.Tests;

public sealed class DeviceEventParserTests
{
    private readonly DeviceEventParser _parser = new();

    [Theory]
    [InlineData("KEY_1_DOWN", InputId.Key1, RawDeviceEventKind.Pressed)]
    [InlineData("KEY_8_UP", InputId.Key8, RawDeviceEventKind.Released)]
    [InlineData("ENC_BTN_DOWN", InputId.EncoderButton, RawDeviceEventKind.Pressed)]
    [InlineData("ENC_CW", InputId.EncoderClockwise, RawDeviceEventKind.RotatedClockwise)]
    [InlineData("ENC_CCW", InputId.EncoderCounterClockwise, RawDeviceEventKind.RotatedCounterClockwise)]
    public void ParsesKnownFirmwareEvents(string line, InputId expectedInput, RawDeviceEventKind expectedKind)
    {
        var parsed = _parser.TryParse(line, out var deviceEvent);

        Assert.True(parsed);
        Assert.NotNull(deviceEvent);
        Assert.Equal(expectedInput, deviceEvent!.InputId);
        Assert.Equal(expectedKind, deviceEvent.Kind);
    }

    [Fact]
    public void RejectsUnknownLines()
    {
        var parsed = _parser.TryParse("ESP8266 macropad ready", out var deviceEvent);

        Assert.False(parsed);
        Assert.Null(deviceEvent);
    }
}
