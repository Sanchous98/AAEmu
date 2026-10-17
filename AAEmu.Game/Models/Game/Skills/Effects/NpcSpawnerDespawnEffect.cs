using AAEmu.Game.Core.Packets;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;

namespace AAEmu.Game.Models.Game.Skills.Effects;

public class NpcSpawnerDespawnEffect : EffectTemplate
{
    public uint SpawnerId { get; set; }

    public override bool OnActionTime => false;

    public override void Apply(BaseUnit caster, SkillCaster casterObj, BaseUnit target, SkillCastTarget targetObj,
        CastAction castObj, EffectSource source, SkillObject skillObject, DateTime time,
        CompressedGamePackets packetBuilder = null)
    {
        Logger.Info($"NpcSpawnerDespawnEffect: SpawnerId={SpawnerId}");

        if (caster?.ParentWorld?.SpawnManager == null)
            return;

        // GetNpcSpawner(id) only returns event spawners, so the regular world spawners
        // (loaded from the npc_spawns.json files) are never touched by this effect.
        var spawners = caster.ParentWorld.SpawnManager.GetNpcSpawner(SpawnerId);
        if (spawners is not { Count: not 0 })
        {
            Logger.Info($"NpcSpawnerDespawnEffect: SpawnerId={SpawnerId} not found in spawners.");
            return;
        }

        foreach (var spawner in spawners)
        {
            Logger.Info($"NpcSpawnerDespawnEffect: Do Despawn effect id={Id}, Npc unitId={spawner.UnitId} spawnerId={SpawnerId} worldId={caster.Transform.WorldId}");
            // The paired NpcSpawnerSpawnEffect forces RespawnTime = 0, so a forced (Now)
            // despawn removes the event NPCs without scheduling a respawn.
            spawner.DespawnNpcsNow();
        }
    }
}
