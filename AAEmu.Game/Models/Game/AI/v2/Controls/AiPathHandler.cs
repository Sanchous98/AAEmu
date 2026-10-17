using System.Globalization;
using System.Numerics;
using AAEmu.Game.Models.Game.AI.v2.Framework;
using AAEmu.Game.Utils;

namespace AAEmu.Game.Models.Game.AI.v2.Controls;

public class AiPathHandler(NpcAi aiOwner)
{
    public NpcAi Owner { get; } = aiOwner;

    /// <summary>
    /// Loaded AI Path Points
    /// </summary>
    public List<AiPathPoint> AiPathPoints { get; set; } = [];

    /// <summary>
    /// Queue of locations to go to next
    /// </summary>
    public Queue<AiPathPoint> AiPathPointsRemaining { get; set; } = new();

    public bool AiPathLooping { get; set; } = true; // Needs to be set to true to trigger initial loading into queue
    /// <summary>
    /// Speed multiplier when moving on the Path
    /// </summary>
    public float AiPathSpeed { get; set; } = 1f;

    /// <summary>
    /// Stance to use when moving on the Path; 5-walk, 4-run, 3-stand still
    /// </summary>
    public byte AiPathActorFlags { get; set; } = 4;

    /// <summary>
    /// Currently targeted position for path movement
    /// </summary>
    public Vector3 TargetPosition { get; set; } = Vector3.Zero;

    /// <summary>
    /// Does path movement and dequeuing as needed
    /// </summary>
    /// <param name="delta"></param>
    /// <returns>Returns true as long as there is still unhandled path movement</returns>
    public bool RunCurrentPath(TimeSpan delta)
    {
        // Queue empty? refill!
        if (AiPathPointsRemaining.Count <= 0 && AiPathPoints.Count > 0 && AiPathLooping)
        {
            AiPathLooping = false;
            foreach (var aiPathPoint in AiPathPoints)
            {
                AiPathPointsRemaining.Enqueue(aiPathPoint);
            }
        }

        // Are we there yet?
        if (TargetPosition != Vector3.Zero && MathUtil.CalculateDistance(TargetPosition, Owner.Owner.Transform.World.Position, true) < Owner.Owner.Template.Scale)
        {
            TargetPosition = Vector3.Zero;
        }

        // No current target? Set it!
        if (TargetPosition == Vector3.Zero && AiPathPointsRemaining.Count > 0)
        {
            var nextPos = AiPathPointsRemaining.Dequeue();
            switch (nextPos.Action)
            {
                case AiPathPointAction.None:
                    break;
                case AiPathPointAction.DisableLoop:
                    AiPathLooping = false;
                    break;
                case AiPathPointAction.EnableLoop:
                    AiPathLooping = true;
                    break;
                case AiPathPointAction.Speed:
                    if (float.TryParse(nextPos.Param, CultureInfo.InvariantCulture, out var newSpeed))
                        AiPathSpeed = newSpeed;
                    break;
                case AiPathPointAction.ActorFlags:
                    if (byte.TryParse(nextPos.Param, out var newStance))
                        AiPathActorFlags = newStance;
                    break;
                case AiPathPointAction.ReturnToCommandSet:
                    Owner.GoToRunCommandSet();
                    return true;
                default:
                    throw new NotSupportedException($"Not supported nextPos.Action value: {nextPos.Action}");
            }

            // Set next move point if it's not zero
            if (!nextPos.Position.Equals(Vector3.Zero))
            {
                TargetPosition = nextPos.Position;
            }
        }

        // We know where to go? Then go that direction
        if (TargetPosition != Vector3.Zero)
        {
            // A concurrent NpcControlEffect (RunCommandSet/FollowPath) can (re)start the legacy
            // Simulation route after FollowPathBehavior.Enter disabled it. Keep it disabled for as
            // long as the v2 handler is driving movement, otherwise both movers fight over the
            // transform and the NPC oscillates in place without making progress (#1256).
            Owner.Owner.Simulation.MoveToPathEnabled = false;

            var moveSpeed = Owner.GetRealMovementSpeed(AiPathSpeed);
            // var moveFlags = Owner.GetRealMovementFlags(moveSpeed);
            moveSpeed *= delta.Milliseconds / 1000.0;
            Owner.Owner.MoveTowards(TargetPosition, (float)moveSpeed, AiPathActorFlags);
            // Owner.Owner.MoveTowards(TargetPosition, AiPathSpeed * Owner.Owner.BaseMoveSpeed * (delta.Milliseconds / 1000.0f), AiPathStanceFlags);

            // Move the idle "home" location along with the path, so it doesn't immediately trigger a return to home state when going into combat
            Owner.IdlePosition = Owner.Owner.Transform.World.Position;
        }

        return HasUnhandledPathMovementData();
    }

    public bool HasUnhandledPathMovementData()
    {
        return AiPathPointsRemaining.Count > 0 || AiPathLooping || TargetPosition != Vector3.Zero;
    }

    public bool HasPathMovementData()
    {
        return AiPathPointsRemaining.Count > 0 || (AiPathLooping && AiPathPoints.Count > 0);
    }

    /// <summary>
    /// Returns the index of the path point closest to <paramref name="from"/>, or -1 when the list is empty.
    /// Paths are shared between spawn points, so joining at the nearest checkpoint (instead of always
    /// index 0) lets an NPC spawned anywhere along the route walk forward rather than backtracking first.
    /// </summary>
    public static int FindNearestCheckpointIndex(IReadOnlyList<AiPathPoint> points, Vector3 from)
    {
        if (points == null || points.Count == 0)
            return -1;

        var bestIndex = 0;
        var bestDistance = float.MaxValue;
        for (var i = 0; i < points.Count; i++)
        {
            // Action-only rows (speed, loop, return...) carry a zero position and must not be picked.
            if (points[i].Position.Equals(Vector3.Zero))
                continue;

            var distance = MathUtil.CalculateDistance(points[i].Position, from, true);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
