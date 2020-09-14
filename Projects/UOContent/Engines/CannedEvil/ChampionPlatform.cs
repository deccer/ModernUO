using Server.Items;

namespace Server.Engines.CannedEvil
{
    public class ChampionPlatform : BaseAddon
    {
        private ChampionSpawn m_Spawn;

        public ChampionPlatform(ChampionSpawn spawn)
        {
            m_Spawn = spawn;

            for (var x = -2; x <= 2; ++x)
            {
                for (var y = -2; y <= 2; ++y)
                {
                    AddComponent(0x750, x, y, -5);
                }
            }

            for (var x = -1; x <= 1; ++x)
            {
                for (var y = -1; y <= 1; ++y)
                {
                    AddComponent(0x750, x, y, 0);
                }
            }

            for (var i = -1; i <= 1; ++i)
            {
                AddComponent(0x751, i, 2, 0);
                AddComponent(0x752, 2, i, 0);

                AddComponent(0x753, i, -2, 0);
                AddComponent(0x754, -2, i, 0);
            }

            AddComponent(0x759, -2, -2, 0);
            AddComponent(0x75A, 2, 2, 0);
            AddComponent(0x75B, -2, 2, 0);
            AddComponent(0x75C, 2, -2, 0);
        }

        public ChampionPlatform(Serial serial) : base(serial)
        {
        }

        public void AddComponent(int id, int x, int y, int z)
        {
            var ac = new AddonComponent(id);

            ac.Hue = 0x497;

            AddComponent(ac, x, y, z);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            m_Spawn?.Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Spawn);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Spawn = reader.ReadItem() as ChampionSpawn;

                        if (m_Spawn == null)
                        {
                            Delete();
                        }

                        break;
                    }
            }
        }
    }
}
