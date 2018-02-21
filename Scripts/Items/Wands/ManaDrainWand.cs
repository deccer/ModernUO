using System;
using Server;
using Server.Spells.Fourth;
using Server.Targeting;

namespace Server.Items
{
	public class ManaDrainWand : BaseWand
	{
		[Constructible]
		public ManaDrainWand() : base( WandEffect.ManaDraining, 5, 30 )
		{
		}

		public ManaDrainWand( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

		public override void OnWandUse( Mobile from )
		{
			Cast( new ManaDrainSpell( from, this ) );
		}
	}
}