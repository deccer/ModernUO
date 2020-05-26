using System;

namespace Server.Items
{
  public class BrazierTall : BaseLight
  {
    [Constructible]
    public BrazierTall() : base(0x19AA)
    {
      Movable = false;
      Duration = TimeSpan.Zero; // Never burnt out
      Burning = true;
      Light = LightType.Circle300;
      Weight = 25.0;
    }

    public BrazierTall(Serial serial) : base(serial)
    {
    }

    public override int LitItemID => 0x19AA;

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);
      writer.Write(0);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);
      int version = reader.ReadInt();
    }
  }
}