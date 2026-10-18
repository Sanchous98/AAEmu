using AAEmu.Game.Core.Managers;
using AAEmu.Game.Core.Managers.World;

namespace AAEmu.UnitTests.Game.Core.Managers;

public class SaveManagerTests
{
    [Test]
    public async Task Constructor_DoesNotCallDeps()
    {
        var mockTask = Mock.Of<ITaskManager>();
        var mockHousing = Mock.Of<IHousingManager>();
        var mockMail = Mock.Of<IMailManager>();
        var mockItem = Mock.Of<IItemManager>();
        var mockAuction = Mock.Of<IAuctionManager>();
        var mockCrime = Mock.Of<ICrimeManager>();
        var mockWorld = Mock.Of<IWorldManager>();

        var manager = new SaveManager(
            mockTask.Object,
            mockHousing.Object,
            mockMail.Object,
            mockItem.Object,
            mockAuction.Object,
            mockCrime.Object,
            mockWorld.Object);

        await Assert.That(manager).IsNotNull();
        Mock.VerifyNoOtherCalls(mockTask);
        Mock.VerifyNoOtherCalls(mockHousing);
        Mock.VerifyNoOtherCalls(mockMail);
        Mock.VerifyNoOtherCalls(mockItem);
        Mock.VerifyNoOtherCalls(mockAuction);
        Mock.VerifyNoOtherCalls(mockCrime);
        Mock.VerifyNoOtherCalls(mockWorld);
    }

    [Test]
    public async Task DoSave_WhenSaveInProgress_ChecksFlagUnderLock()
    {
        // Arrange
        var manager = CreateSaveManager();
        var lockObject = GetPrivateField<object>(manager, "_lock");
        SetPrivateField(manager, "_isSaving", true);

        System.Threading.Tasks.Task<bool> saveTask;
        bool completedWhileLockHeld;

        // Act - hold the manager lock while starting a save
        lock (lockObject)
        {
            saveTask = System.Threading.Tasks.Task.Run(() => manager.DoSave());

            // If the _isSaving check were performed outside the lock, DoSave would return
            // immediately instead of waiting for the in-progress save to release the lock.
            completedWhileLockHeld = saveTask.Wait(TimeSpan.FromMilliseconds(250));
        }

        // Assert
        await Assert.That(completedWhileLockHeld).IsFalse();

        var result = await saveTask;
        await Assert.That(result).IsFalse();
        await Assert.That(GetPrivateField<bool>(manager, "_isSaving")).IsTrue();
    }

    #region Helpers

    private static SaveManager CreateSaveManager()
    {
        return new SaveManager(
            Mock.Of<ITaskManager>().Object,
            Mock.Of<IHousingManager>().Object,
            Mock.Of<IMailManager>().Object,
            Mock.Of<IItemManager>().Object,
            Mock.Of<IAuctionManager>().Object,
            Mock.Of<ICrimeManager>().Object,
            Mock.Of<IWorldManager>().Object);
    }

    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (T)field!.GetValue(obj);
    }

    #endregion
}
