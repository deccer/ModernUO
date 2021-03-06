namespace Server.Items
{
    [Flippable(0x232A, 0x232B)]
    public class WinterGiftPackage2003 : GiftBox
    {
        [Constructible]
        public WinterGiftPackage2003()
        {
            DropItem(new Snowman());
            DropItem(new WreathDeed());
            DropItem(new BlueSnowflake());
            DropItem(new RedPoinsettia());
        }

        public WinterGiftPackage2003(Serial serial) : base(serial)
        {
        }

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
