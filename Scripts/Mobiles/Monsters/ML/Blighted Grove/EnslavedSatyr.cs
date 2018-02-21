using System;
using Server;
using Server.Items;

namespace Server.Mobiles
{
	[CorpseName( "an enslaved satyr corpse" )]
	public class EnslavedSatyr : Satyr
	{
		[Constructible]
		public EnslavedSatyr()
		{
			Name = "an enslaved satyr";
		}

		/*
		// TODO: uncomment once added
		public override void OnDeath( Container c )
		{
			base.OnDeath( c );

			if ( Utility.RandomDouble() < 0.1 )
				c.DropItem( new ParrotItem() );
		}
		*/

		public EnslavedSatyr( Serial serial )
			: base( serial )
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
	}
}
