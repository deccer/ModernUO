using System;
using Server.Engines.ConPVP;
using Server.Spells;

namespace Server.Items
{
    public abstract class BaseStrengthPotion : BasePotion
    {
        public BaseStrengthPotion(PotionEffect effect) : base(0xF09, effect)
        {
        }

        public BaseStrengthPotion(Serial serial) : base(serial)
        {
        }

        public abstract int StrOffset { get; }
        public abstract TimeSpan Duration { get; }

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

        public bool DoStrength(Mobile from)
        {
            // TODO: Verify scaled; is it offset, duration, or both?
            if (SpellHelper.AddStatOffset(from, StatType.Str, Scale(from, StrOffset), Duration))
            {
                from.FixedEffect(0x375A, 10, 15);
                from.PlaySound(0x1E7);
                return true;
            }

            from.SendLocalizedMessage(502173); // You are already under a similar effect.
            return false;
        }

        public override void Drink(Mobile from)
        {
            if (DoStrength(from))
            {
                PlayDrinkEffect(from);

                if (!DuelContext.IsFreeConsume(from))
                {
                    Consume();
                }
            }
        }
    }
}
