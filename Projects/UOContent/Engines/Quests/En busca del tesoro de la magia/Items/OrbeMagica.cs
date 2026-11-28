using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.MagicTreasure;

[SerializationGenerator(0, false)]
public partial class OrbeMagica : QuestItem
{
    [Constructible]
    public OrbeMagica() : base(0xE2E)
    {
        Name = "Orbe Mágica";
    }

    public override bool CanDrop(PlayerMobile pm) =>
        pm.Quest is not EnBuscaDelTesoroDeLaMagiaQuest quest ||
        quest.IsExpired ||
        !quest.IsObjectiveInProgress(typeof(ReturnOrbeMagicaObjective));
}
