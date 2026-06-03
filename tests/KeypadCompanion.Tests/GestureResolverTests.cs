using KeypadCompanion.Domain;
using KeypadCompanion.Services;
using Microsoft.Extensions.Time.Testing;

namespace KeypadCompanion.Tests;

public sealed class GestureResolverTests
{
    [Fact]
    public async Task EmitsSinglePressAfterDoubleClickWindow()
    {
        var timeProvider = new FakeTimeProvider();
        using var resolver = new GestureResolver(timeProvider);
        resolver.UpdateSettings(new GestureSettings
        {
            LongPressMilliseconds = 500,
            DoubleClickMilliseconds = 275,
        });

        var resolved = new List<ResolvedTriggerEvent>();
        resolver.TriggerResolved += (_, triggerEvent) => resolved.Add(triggerEvent);

        var t0 = timeProvider.GetUtcNow();
        resolver.HandleRawEvent(new RawDeviceEvent(InputId.Key1, RawDeviceEventKind.Pressed, t0));
        resolver.HandleRawEvent(new RawDeviceEvent(InputId.Key1, RawDeviceEventKind.Released, t0.AddMilliseconds(50)));
        await Task.Yield();

        timeProvider.Advance(TimeSpan.FromMilliseconds(274));
        await Task.Yield();
        Assert.Empty(resolved);

        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        await WaitForAsync(() => resolved.Count == 1);

        var press = Assert.Single(resolved);
        Assert.Equal(InputId.Key1, press.InputId);
        Assert.Equal(TriggerType.Press, press.TriggerType);
    }

    [Fact]
    public async Task EmitsDoubleClickWithoutSinglePress()
    {
        var timeProvider = new FakeTimeProvider();
        using var resolver = new GestureResolver(timeProvider);
        resolver.UpdateSettings(new GestureSettings
        {
            LongPressMilliseconds = 500,
            DoubleClickMilliseconds = 275,
        });

        var resolved = new List<ResolvedTriggerEvent>();
        resolver.TriggerResolved += (_, triggerEvent) => resolved.Add(triggerEvent);

        var t0 = timeProvider.GetUtcNow();
        resolver.HandleRawEvent(new RawDeviceEvent(InputId.Key2, RawDeviceEventKind.Pressed, t0));
        resolver.HandleRawEvent(new RawDeviceEvent(InputId.Key2, RawDeviceEventKind.Released, t0.AddMilliseconds(40)));
        await Task.Yield();

        timeProvider.Advance(TimeSpan.FromMilliseconds(100));
        await Task.Yield();

        resolver.HandleRawEvent(new RawDeviceEvent(InputId.Key2, RawDeviceEventKind.Pressed, t0.AddMilliseconds(140)));
        resolver.HandleRawEvent(new RawDeviceEvent(InputId.Key2, RawDeviceEventKind.Released, t0.AddMilliseconds(190)));
        await Task.Yield();

        timeProvider.Advance(TimeSpan.FromMilliseconds(400));
        await WaitForAsync(() => resolved.Count == 1);

        var trigger = Assert.Single(resolved);
        Assert.Equal(TriggerType.DoubleClick, trigger.TriggerType);
    }

    [Fact]
    public async Task EmitsLongPressWithoutSinglePress()
    {
        var timeProvider = new FakeTimeProvider();
        using var resolver = new GestureResolver(timeProvider);
        resolver.UpdateSettings(new GestureSettings
        {
            LongPressMilliseconds = 500,
            DoubleClickMilliseconds = 275,
        });

        var resolved = new List<ResolvedTriggerEvent>();
        resolver.TriggerResolved += (_, triggerEvent) => resolved.Add(triggerEvent);

        var t0 = timeProvider.GetUtcNow();
        resolver.HandleRawEvent(new RawDeviceEvent(InputId.Key3, RawDeviceEventKind.Pressed, t0));
        resolver.HandleRawEvent(new RawDeviceEvent(InputId.Key3, RawDeviceEventKind.Released, t0.AddMilliseconds(650)));
        await Task.Yield();

        timeProvider.Advance(TimeSpan.FromMilliseconds(400));
        await WaitForAsync(() => resolved.Count == 1);

        var trigger = Assert.Single(resolved);
        Assert.Equal(TriggerType.LongPress, trigger.TriggerType);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 25; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }
    }
}
