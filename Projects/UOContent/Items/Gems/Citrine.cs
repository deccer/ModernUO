namespace Server.Items
{
    public class Citrine : Item
    {
        [Constructible]
        public Citrine(int amount = 1) : base(0xF15)
        {
            Stackable = true;
            Amount = amount;
        }

        public Citrine(Serial serial) : base(serial)
        {
        }

        public override double DefaultWeight => 0.1;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
