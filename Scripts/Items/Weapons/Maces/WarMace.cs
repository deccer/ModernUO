using System;
using Server.Network;
using Server.Items;

namespace Server.Items
{
	[FlippableAttribute( 0x1407, 0x1406 )]
	public class WarMace : BaseBashing
	{
		public override WeaponAbility PrimaryAbility{ get{ return WeaponAbility.CrushingBlow; } }
		public override WeaponAbility SecondaryAbility{ get{ return WeaponAbility.MortalStrike; } }

		public override int AosStrengthReq{ get{ return 80; } }
		public override int AosMinDamage{ get{ return 16; } }
		public override int AosMaxDamage{ get{ return 17; } }
		public override int AosSpeed{ get{ return 26; } }
		public override float MlSpeed{ get{ return 4.00f; } }

		public override int OldStrengthReq{ get{ return 30; } }
		public override int OldMinDamage{ get{ return 10; } }
		public override int OldMaxDamage{ get{ return 30; } }
		public override int OldSpeed{ get{ return 32; } }

		public override int InitMinHits{ get{ return 31; } }
		public override int InitMaxHits{ get{ return 110; } }

		[Constructible]
		public WarMace() : base( 0x1407 )
		{
			Weight = 17.0;
		}

		public WarMace( Serial serial ) : base( serial )
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