using NAudio.CoreAudioApi;

namespace KeypadCompanion.Services;

public sealed class WindowsVolumeService : IVolumeService
{
    public void AdjustVolume(double deltaPercent)
    {
        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var current = device.AudioEndpointVolume.MasterVolumeLevelScalar;
        var next = Math.Clamp(current + (float)(deltaPercent / 100.0), 0f, 1f);
        device.AudioEndpointVolume.MasterVolumeLevelScalar = next;
    }

    public void ToggleMute()
    {
        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        device.AudioEndpointVolume.Mute = !device.AudioEndpointVolume.Mute;
    }
}
