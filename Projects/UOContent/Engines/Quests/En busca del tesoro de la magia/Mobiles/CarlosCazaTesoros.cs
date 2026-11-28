using ModernUO.Serialization;
using Server.Factions;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.Engines.Quests.MagicTreasure;

[SerializationGenerator(0, false)]
public partial class CarlosCazaTesoros : BaseQuester
{
    private static readonly ILogger _logger = LogFactory.GetLogger(typeof(CarlosCazaTesoros));

    [Constructible]
    public CarlosCazaTesoros()
    {
    }

    public override bool ClickTitle => false;

    public override string DefaultName => "Carlos, el caza tesoros";

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = Race.Human.RandomSkinHue();
        Female = false;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(new FancyShirt(Utility.RandomDyedHue()));
        AddItem(new LongPants(Utility.RandomNondyedHue()));
        AddItem(new Boots());
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
        Direction = GetDirectionTo(player);

        if (player.Quest is EnBuscaDelTesoroDeLaMagiaQuest quest)
        {
            if (quest.ExpireIfNeeded())
            {
                return;
            }

            SayTo(player, "Si has encontrado el Orbe Mágica, entrégamela antes de que venza el plazo.");
            return;
        }

        if (player.Quest != null)
        {
            return;
        }

        if (QuestSystem.CanOfferQuest(player, typeof(EnBuscaDelTesoroDeLaMagiaQuest)))
        {
            new EnBuscaDelTesoroDeLaMagiaQuest(player).SendOffer();
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        Direction = GetDirectionTo(from);

        if (from is not PlayerMobile player || dropped is not OrbeMagica orb ||
            player.Quest is not EnBuscaDelTesoroDeLaMagiaQuest quest)
        {
            return base.OnDragDrop(from, dropped);
        }

        if (quest.ExpireIfNeeded())
        {
            return false;
        }

        var objective = quest.FindObjective<ReturnOrbeMagicaObjective>();

        if (objective?.Completed != false)
        {
            return base.OnDragDrop(from, dropped);
        }

        if (Faction.Find(player) != null)
        {
            var silver = new Silver(1000);

            if (!player.PlaceInBackpack(silver))
            {
                silver.Delete();
                SayTo(player, "Necesitas espacio en la mochila para recibir la recompensa de facción.");
                return false;
            }
        }

        player.BankBox.DropItem(new Gold(100_000));
        orb.Delete();
        objective.Complete();

        _logger.Information(
            "Quest {Quest} completada por el personaje {Character} de la cuenta {Account}",
            "En busca del tesoro de la magia",
            player.Name,
            player.Account?.Username ?? "sin cuenta"
        );

        quest.Complete();
        SayTo(player, "¡Excelente! Has completado la misión y recibido tu recompensa.");
        return true;
    }
}
