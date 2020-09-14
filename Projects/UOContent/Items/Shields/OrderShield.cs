using Server.Guilds;

namespace Server.Items
{
    public class OrderShield : BaseShield
    {
        [Constructible]
        public OrderShield() : base(0x1BC4)
        {
            if (!Core.AOS)
            {
                LootType = LootType.Newbied;
            }

            Weight = 7.0;
        }

        public OrderShield(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 1;
        public override int BaseFireResistance => 0;
        public override int BaseColdResistance => 0;
        public override int BasePoisonResistance => 0;
        public override int BaseEnergyResistance => 0;

        public override int InitMinHits => 100;
        public override int InitMaxHits => 125;

        public override int AosStrReq => 95;

        public override int ArmorBase => 30;

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (Weight == 6.0)
            {
                Weight = 7.0;
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override bool OnEquip(Mobile from) => Validate(from) && base.OnEquip(from);

        public override void OnSingleClick(Mobile from)
        {
            if (Validate(Parent as Mobile))
            {
                base.OnSingleClick(@from);
            }
        }

        public virtual bool Validate(Mobile m)
        {
            if (Core.AOS || m?.Player != true || m.AccessLevel != AccessLevel.Player)
            {
                return true;
            }

            if (!(m.Guild is Guild g) || g.Type != GuildType.Order)
            {
                m.FixedEffect(0x3728, 10, 13);
                Delete();

                return false;
            }

            return true;
        }
    }
}
