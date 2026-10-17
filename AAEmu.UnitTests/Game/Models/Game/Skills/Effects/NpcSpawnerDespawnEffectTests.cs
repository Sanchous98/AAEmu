using System.Reflection;

using AAEmu.Game.Core.Managers.World;
using AAEmu.Game.Models.Game.NPChar;
using AAEmu.Game.Models.Game.Skills.Effects;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.World;

namespace AAEmu.UnitTests.Game.Models.Game.Skills.Effects;

public class NpcSpawnerDespawnEffectTests
{
    private const uint EventSpawnerId = 555;

    /// <summary>
    /// Builds a minimal world + spawn manager + event spawner (the kind that
    /// <see cref="SpawnManager.GetNpcSpawner(uint)"/> returns) with one spawned NPC.
    /// </summary>
    private static (WorldInstance World, NpcSpawner Spawner, Npc Npc) CreateEventSpawnerWithNpc()
    {
        // Default Transform.InstanceId is uint.MaxValue; matching it avoids the
        // Transform.InstanceId setter resolving WorldManager.Instance from DI.
        var world = new WorldInstance(new WorldTemplate(), 1, true, uint.MaxValue);
        // This fixture has no runtime services for the world finalizer to shut down.
        GC.SuppressFinalize(world);
        world.SpawnManager = new SpawnManager(world);

        var spawner = new NpcSpawner
        {
            UnitId = 4242,
            NpcSpawnerIds = [EventSpawnerId]
        };
        world.SpawnManager.AddNpcSpawner(spawner); // event spawner branch

        var npc = new Npc
        {
            ObjId = 1,
            ParentWorld = world,
            Spawner = spawner
        };
        spawner.SpawnedNpcs[spawner.SpawnerId] = [npc];

        return (world, spawner, npc);
    }

    private static HashSet<GameObject> GetQueuedDespawns(SpawnManager manager)
    {
        var property = typeof(SpawnManager).GetProperty("Despawns", BindingFlags.NonPublic | BindingFlags.Instance);
        return (HashSet<GameObject>)property!.GetValue(manager)!;
    }

    [Test]
    public async Task Apply_WithEventSpawner_QueuesItsNpcsForDespawn()
    {
        // Arrange
        var (world, spawner, npc) = CreateEventSpawnerWithNpc();
        var caster = new BaseUnit { ParentWorld = world };
        var effect = new NpcSpawnerDespawnEffect { Id = 1, SpawnerId = EventSpawnerId };

        // Act
        effect.Apply(caster, null, null, null, null, null, null, DateTime.UtcNow);

        // Assert
        await Assert.That(spawner.Id).IsEqualTo(EventSpawnerId);
        await Assert.That(GetQueuedDespawns(world.SpawnManager)).Contains(npc);
    }

    [Test]
    public async Task Apply_WithUnknownSpawner_DoesNotQueueAnything()
    {
        // Arrange
        var (world, _, npc) = CreateEventSpawnerWithNpc();
        var caster = new BaseUnit { ParentWorld = world };
        var effect = new NpcSpawnerDespawnEffect { Id = 1, SpawnerId = 999 };

        // Act
        effect.Apply(caster, null, null, null, null, null, null, DateTime.UtcNow);

        // Assert
        await Assert.That(GetQueuedDespawns(world.SpawnManager)).DoesNotContain(npc);
    }

    [Test]
    public async Task GetNpcSpawner_OnlyReturnsEventSpawners()
    {
        // Arrange
        var (world, spawner, _) = CreateEventSpawnerWithNpc();

        // Act
        var found = world.SpawnManager.GetNpcSpawner(EventSpawnerId);

        // Assert
        await Assert.That(found).Contains(spawner);
        await Assert.That(world.SpawnManager.GetNpcSpawner(4242)).IsEmpty();
    }
}
