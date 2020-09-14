using System.Text.Json;
using Server.Json;
using Server.Network;
using Server.Spells;
using Server.Spells.Ninjitsu;

namespace Server.Regions
{
    public class TwistedWealdDesertRegion : MondainRegion
    {
        public TwistedWealdDesertRegion(DynamicJson json, JsonSerializerOptions options) : base(json, options)
        {
        }

        public static void Initialize()
        {
            EventSink.Login += Desert_OnLogin;
        }

        public override void OnEnter(Mobile m)
        {
            var ns = m.NetState;
            if (ns != null && !TransformationSpellHelper.UnderTransformation(m, typeof(AnimalForm)) &&
                m.AccessLevel == AccessLevel.Player)
            {
                ns.Send(SpeedControl.WalkSpeed);
            }
        }

        public override void OnExit(Mobile m)
        {
            var ns = m.NetState;
            if (ns != null && !TransformationSpellHelper.UnderTransformation(m, typeof(AnimalForm)))
            {
                ns.Send(SpeedControl.Disable);
            }
        }

        private static void Desert_OnLogin(Mobile m)
        {
            if (m.Region.IsPartOf<TwistedWealdDesertRegion>() && m.AccessLevel == AccessLevel.Player)
            {
                m.NetState.Send(SpeedControl.WalkSpeed);
            }
        }
    }
}
