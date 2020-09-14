using System;
using Server.Spells;

namespace Server.Items
{
    public class GiantSpikeTrap : BaseTrap
    {
        [Constructible]
        public GiantSpikeTrap() : base(1)
        {
        }

        public GiantSpikeTrap(Serial serial) : base(serial)
        {
        }

        public override bool PassivelyTriggered => true;
        public override TimeSpan PassiveTriggerDelay => TimeSpan.Zero;
        public override int PassiveTriggerRange => 3;
        public override TimeSpan ResetDelay => TimeSpan.FromSeconds(0.0);

        public override void OnTrigger(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
            {
                return;
            }

            Effects.SendLocationEffect(Location, Map, 0x1D99, 48, 2, GetEffectHue(), 0);

            if (from.Alive && CheckRange(from.Location, 0))
            {
                SpellHelper.Damage(TimeSpan.FromTicks(1), @from, @from, Utility.Dice(10, 7, 0));
            }
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
