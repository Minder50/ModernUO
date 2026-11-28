using System;
using Server.Mobiles;

namespace Server.Engines.Quests.MagicTreasure;

public class EnBuscaDelTesoroDeLaMagiaQuest : QuestSystem
{
    private static readonly Type[] _typeReferenceTable =
    {
        typeof(ReturnOrbeMagicaObjective)
    };

    private DateTime _deadline;

    public EnBuscaDelTesoroDeLaMagiaQuest(PlayerMobile from) : base(from)
    {
    }

    public EnBuscaDelTesoroDeLaMagiaQuest()
    {
    }

    public override object Name => "En busca del tesoro de la magia";

    public override object OfferMessage =>
        "Hola, soy un aventurero muy interesado en los tesoros del Inframundo. En especial busco el Orbe Mágica que está en algún lugar desconocido, en el Castillo de Camelot propiedad de un personaje muy poderoso. Si tienes éxito te recompensaré. Si aceptas tienes quince días para traermelo.";

    public override int Picture => 0x15B5;

    public override bool IsTutorial => false;

    public override TimeSpan RestartDelay => TimeSpan.Zero;

    public override Type[] TypeReferenceTable => _typeReferenceTable;

    public bool IsExpired => _deadline != DateTime.MinValue && Core.Now >= _deadline;

    public override void Accept()
    {
        if (From.Quest != null)
        {
            return;
        }

        _deadline = Core.Now + TimeSpan.FromDays(15);
        base.Accept();

        if (From.Quest == this)
        {
            AddObjective(new ReturnOrbeMagicaObjective());
        }
    }

    public bool ExpireIfNeeded()
    {
        if (!IsExpired)
        {
            return false;
        }

        Cancel();
        From.SendMessage("Se ha vencido el plazo de quince días para completar la misión.");
        return true;
    }

    public override void Slice()
    {
        if (ExpireIfNeeded())
        {
            return;
        }

        base.Slice();
    }

    public override void ChildDeserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        if (version == 0)
        {
            _deadline = reader.ReadDateTime();
        }
    }

    public override void ChildSerialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0);
        writer.Write(_deadline);
    }
}
