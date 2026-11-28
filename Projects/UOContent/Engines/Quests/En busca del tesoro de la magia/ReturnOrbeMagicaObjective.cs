using Server.Gumps;

namespace Server.Engines.Quests.MagicTreasure;

public class ReturnOrbeMagicaObjective : QuestObjective
{
    public override int Message => 0;

    public override void RenderMessage(ref DynamicGumpBuilder builder)
    {
        BaseQuestGump.AddHtmlObject(
            ref builder,
            70,
            130,
            300,
            100,
            "Encuentra el Orbe Mágica y entrégasela a Carlos, el caza tesoros.",
            BaseQuestGump.Blue,
            false,
            false
        );
    }

    public override void RenderProgress(ref DynamicGumpBuilder builder)
    {
        BaseQuestGump.AddHtmlObject(
            ref builder,
            70,
            260,
            270,
            100,
            Completed ? "Has entregado el Orbe Mágica." : "Aún debes encontrar y entregar el Orbe Mágica.",
            BaseQuestGump.Blue,
            false,
            false
        );
    }
}
