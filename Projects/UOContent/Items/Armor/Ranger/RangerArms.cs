namespace Server.Items
{
    [Flippable(0x13dc, 0x13d4)]
    public class RangerArms : BaseArmor
    {
        [Constructible]
        public RangerArms() : base(0x13DC)
        {
            Weight = 4.0;
            Hue = 0x59C;
        }

        public RangerArms(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 4;

        public override int InitMinHits => 35;
        public override int InitMaxHits => 45;

        public override int AosStrReq => 25;
        public override int OldStrReq => 25;

        public override int ArmorBase => 16;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Studded;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override int LabelNumber => 1041493; // studded sleeves, ranger armor

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            if (Weight == 1.0)
            {
                Weight = 4.0;
            }
        }
    }
}
