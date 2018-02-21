using System;
using Server;
using Server.Items;

namespace Server.Items
{
	public class CreateFoodScroll : SpellScroll
	{
		[Constructible]
		public CreateFoodScroll() : this( 1 )
		{
		}

		[Constructible]
		public CreateFoodScroll( int amount ) : base( 1, 0x1F2F, amount )
		{
		}

		public CreateFoodScroll( Serial serial ) : base( serial )
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