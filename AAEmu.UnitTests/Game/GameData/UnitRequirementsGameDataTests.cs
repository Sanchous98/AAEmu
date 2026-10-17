using AAEmu.Game.GameData;
using AAEmu.Game.Models.Game.Skills;
using AAEmu.Game.Models.Game.Skills.Static;
using AAEmu.Game.Models.Game.Skills.Templates;
using AAEmu.Game.Models.Game.Units;
using AAEmu.Game.Models.Game.Units.Static;

namespace AAEmu.UnitTests.Game.GameData;

/// <summary>
/// Tests for UnitRequirementsGameData, in particular the <c>or_unit_reqs</c> (OR) semantics:
/// a skill/sphere/quest component with OR requirements must be allowed as soon as a single
/// requirement passes, and denied when none pass.
/// </summary>
public class UnitRequirementsGameDataTests : SqliteTestBase
{
    protected override void CreateTestSchema()
    {
        base.CreateTestSchema();

        using var command = Connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS unit_reqs (
                id INTEGER PRIMARY KEY,
                owner_id INTEGER NOT NULL,
                owner_type TEXT NOT NULL,
                kind_id INTEGER NOT NULL,
                value1 INTEGER NOT NULL,
                value2 INTEGER NOT NULL
            )";
        command.ExecuteNonQuery();
    }

    private void InsertUnitReq(uint id, uint ownerId, string ownerType, UnitReqsKindType kind, uint value1, uint value2)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO unit_reqs (id, owner_id, owner_type, kind_id, value1, value2)
            VALUES (@id, @ownerId, @ownerType, @kindId, @value1, @value2)";
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@ownerId", ownerId);
        command.Parameters.AddWithValue("@ownerType", ownerType);
        command.Parameters.AddWithValue("@kindId", (uint)kind);
        command.Parameters.AddWithValue("@value1", value1);
        command.Parameters.AddWithValue("@value2", value2);
        command.ExecuteNonQuery();
    }

    private UnitRequirementsGameData CreateLoadedGameData()
    {
        var gameData = new UnitRequirementsGameData();
        gameData.Load(Connection);
        return gameData;
    }

    private static SkillTemplate CreateSkill(uint id, bool orUnitReqs)
    {
        return new SkillTemplate
        {
            Id = id,
            OrUnitReqs = orUnitReqs,
            TargetType = SkillTargetType.Self
        };
    }

    [Test]
    public async Task CanUseSkill_OrUnitReqs_OneRequirementSatisfied_IsAllowed()
    {
        // Arrange: skill requires level 50 OR level >= 1; owner is level 10 (second passes)
        InsertUnitReq(1, 9001, "Skill", UnitReqsKindType.Level, 50, 0);
        InsertUnitReq(2, 9001, "Skill", UnitReqsKindType.Level, 1, 0);
        var gameData = CreateLoadedGameData();

        var owner = new Unit { Level = 10 };
        var skill = CreateSkill(9001, orUnitReqs: true);

        // Act
        var result = gameData.CanUseSkill(skill, owner, null);

        // Assert
        await Assert.That(result.ResultKey).IsEqualTo(SkillResultKeys.ok);
    }

    [Test]
    public async Task CanUseSkill_OrUnitReqs_NoRequirementSatisfied_IsDenied()
    {
        // Arrange: skill requires level 50 OR level 60; owner is level 10 (none pass)
        InsertUnitReq(1, 9002, "Skill", UnitReqsKindType.Level, 50, 0);
        InsertUnitReq(2, 9002, "Skill", UnitReqsKindType.Level, 60, 0);
        var gameData = CreateLoadedGameData();

        var owner = new Unit { Level = 10 };
        var skill = CreateSkill(9002, orUnitReqs: true);

        // Act
        var result = gameData.CanUseSkill(skill, owner, null);

        // Assert
        await Assert.That(result.ResultKey).IsNotEqualTo(SkillResultKeys.ok);
        await Assert.That(result.ResultKey).IsEqualTo(SkillResultKeys.skill_urk_level);
    }

    [Test]
    public async Task CanUseSkill_DefaultAndUnitReqs_AllRequirementsSatisfied_IsAllowed()
    {
        // Arrange: skill requires level >= 1 AND level <= 10; owner is level 10 (both pass)
        InsertUnitReq(1, 9003, "Skill", UnitReqsKindType.Level, 1, 0);
        InsertUnitReq(2, 9003, "Skill", UnitReqsKindType.Level, 1, 10);
        var gameData = CreateLoadedGameData();

        var owner = new Unit { Level = 10 };
        var skill = CreateSkill(9003, orUnitReqs: false);

        // Act
        var result = gameData.CanUseSkill(skill, owner, null);

        // Assert
        await Assert.That(result.ResultKey).IsEqualTo(SkillResultKeys.ok);
    }

    [Test]
    public async Task CanUseSkill_DefaultAndUnitReqs_OneRequirementFails_IsDenied()
    {
        // Arrange: skill requires level >= 1 AND level >= 50; owner is level 10 (second fails)
        InsertUnitReq(1, 9004, "Skill", UnitReqsKindType.Level, 1, 0);
        InsertUnitReq(2, 9004, "Skill", UnitReqsKindType.Level, 50, 0);
        var gameData = CreateLoadedGameData();

        var owner = new Unit { Level = 10 };
        var skill = CreateSkill(9004, orUnitReqs: false);

        // Act
        var result = gameData.CanUseSkill(skill, owner, null);

        // Assert
        await Assert.That(result.ResultKey).IsNotEqualTo(SkillResultKeys.ok);
        await Assert.That(result.ResultKey).IsEqualTo(SkillResultKeys.skill_urk_level);
    }
}
