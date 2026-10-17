using System.Numerics;

using AAEmu.Game.Models.Game.AI.v2.Controls;

namespace AAEmu.UnitTests.Game.Models.Game.AI.v2.Controls;

/// <summary>
/// Tests for <see cref="AiPathHandler"/>, in particular joining a shared path at the checkpoint
/// nearest to the NPC instead of always index 0 (issue #1256).
/// </summary>
public class AiPathHandlerTests
{
    private static AiPathPoint Point(float x, float y, float z) =>
        new() { Position = new Vector3(x, y, z), Action = AiPathPointAction.None, Param = string.Empty };

    [Test]
    public async Task FindNearestCheckpointIndex_EmptyList_ReturnsMinusOne()
    {
        var result = AiPathHandler.FindNearestCheckpointIndex([], Vector3.Zero);

        await Assert.That(result).IsEqualTo(-1);
    }

    [Test]
    public async Task FindNearestCheckpointIndex_PicksClosestPoint_NotIndexZero()
    {
        // A route that starts (index 0) far from where the NPC actually is.
        AiPathPoint[] points =
        [
            Point(13082f, 14402f, 144f),
            Point(13062f, 14407f, 141f),
            Point(13055f, 14388f, 136f),
            Point(12985f, 14395f, 122f), // spawn end of the route
        ];

        var spawn = new Vector3(12985.8f, 14395.0f, 122f);
        var result = AiPathHandler.FindNearestCheckpointIndex(points, spawn);

        // Index 3 is ~1 unit away; index 0 is ~100 units away.
        await Assert.That(result).IsEqualTo(3);
    }

    [Test]
    public async Task FindNearestCheckpointIndex_WhenSpawnAtStart_ReturnsZero()
    {
        AiPathPoint[] points =
        [
            Point(13082f, 14402f, 144f),
            Point(12549f, 14591f, 116f),
        ];

        var result = AiPathHandler.FindNearestCheckpointIndex(points, new Vector3(13082f, 14402f, 144f));

        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task FindNearestCheckpointIndex_SkipsActionOnlyZeroPositionRows()
    {
        // Action-only rows (loop/speed/return) carry Vector3.Zero and must never be selected.
        AiPathPoint[] points =
        [
            new() { Position = Vector3.Zero, Action = AiPathPointAction.DisableLoop, Param = string.Empty },
            Point(100f, 100f, 10f),
        ];

        var result = AiPathHandler.FindNearestCheckpointIndex(points, new Vector3(100f, 100f, 10f));

        await Assert.That(result).IsEqualTo(1);
    }
}
