namespace Server.Items
{
    [Flippable(0x1c08, 0x1c09)]
    public class LeatherSkirt : BaseArmor
    {
        [Constructible]
        public LeatherSkirt() : base(0x1C08) => Weight = 1.0;

        public LeatherSkirt(Serial serial) : base(serial)
        {
        }

        public override int BasePhysicalResistance => 2;
        public override int BaseFireResistance => 4;
        public override int BaseColdResistance => 3;
        public override int BasePoisonResistance => 3;
        public override int BaseEnergyResistance => 3;

        public override int InitMinHits => 30;
        public override int InitMaxHits => 40;

        public override int AosStrReq => 20;
        public override int OldStrReq => 10;

        public override int ArmorBase => 13;

        public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;
        public override CraftResource DefaultResource => CraftResource.RegularLeather;

        public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

        public override bool AllowMaleWearer => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            if (Weight == 3.0)
            {
                Weight = 1.0;
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
