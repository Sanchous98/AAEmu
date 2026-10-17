using AAEmu.Commons.Utils;
using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models;
using AAEmu.Game.Models.Game;

using Microsoft.Extensions.DependencyInjection;

namespace AAEmu.UnitTests.Game.Core.Managers.World;

/// <summary>
/// Regression tests for the shutdown race where the physics thread outlived the DI container and
/// its next tick resolved <see cref="AppConfiguration.Instance"/> on a disposed
/// <see cref="IServiceProvider"/> (ObjectDisposedException).
/// </summary>
[NotInParallel]
public sealed class PhysicsManagerShutdownTests
{
    [Before(Test)]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.Configure<AppConfiguration>(config => config.World = new WorldConfig { TargetPhysicsTps = 25f });
        SingletonContainer.ServiceProvider = services.BuildServiceProvider();
    }

    [After(Test)]
    public void Teardown()
    {
        SingletonContainer.ServiceProvider = null;
    }

    [Test]
    public async Task Stop_WhenThreadRunning_WaitsForThreadToExit()
    {
        // Arrange
        using var started = new ManualResetEventSlim(false);
        var manager = new PhysicsManager { ThreadRunning = true };
        manager._thread = new Thread(() =>
        {
            started.Set();
            while (manager.ThreadRunning)
                Thread.Sleep(1);
        }) { Name = "Physics-test" };
        manager._thread.Start();
        started.Wait();

        // Act
        manager.Stop();

        // Assert: Stop must not return until the loop has fully left (otherwise a late tick can
        // touch the disposed DI container).
        await Assert.That(manager._thread.IsAlive).IsFalse();
        await Assert.That(manager.ThreadRunning).IsFalse();
    }

    [Test]
    public async Task Stop_WhenThreadNotStarted_DoesNotThrow()
    {
        // Arrange: mirrors a world whose physics thread was never started.
        var manager = new PhysicsManager { ThreadRunning = true };
        manager._thread = new Thread(() => { });

        // Act
        manager.Stop();

        // Assert: joining an unstarted thread would throw ThreadStateException.
        await Assert.That(manager.ThreadRunning).IsFalse();
    }
}
