using System.Collections.Generic;
using System.IO;
using Server.Items;

namespace Server.Commands
{
    public class SignParser
    {
        private static readonly Queue<Item> m_ToDelete = new Queue<Item>();

        public static void Initialize()
        {
            CommandSystem.Register("SignGen", AccessLevel.Administrator, SignGen_OnCommand);
        }

        [Usage("SignGen")]
        [Description("Generates world/shop signs on all facets.")]
        public static void SignGen_OnCommand(CommandEventArgs c)
        {
            Parse(c.Mobile);
        }

        public static void Parse(Mobile from)
        {
            var cfg = Path.Combine(Core.BaseDirectory, "Data/signs.cfg");

            if (File.Exists(cfg))
            {
                var list = new List<SignEntry>();
                from.SendMessage("Generating signs, please wait.");

                using (var ip = new StreamReader(cfg))
                {
                    string line;

                    while ((line = ip.ReadLine()) != null)
                    {
                        var split = line.Split(' ');

                        var e = new SignEntry(
                            line.Substring(
                                split[0].Length + 1 + split[1].Length + 1 + split[2].Length + 1 +
                                split[3].Length + 1 + split[4].Length + 1
                            ),
                            new Point3D(Utility.ToInt32(split[2]), Utility.ToInt32(split[3]), Utility.ToInt32(split[4])),
                            Utility.ToInt32(split[1]),
                            Utility.ToInt32(split[0])
                        );

                        list.Add(e);
                    }
                }

                Map[] brit = { Map.Felucca, Map.Trammel };
                Map[] fel = { Map.Felucca };
                Map[] tram = { Map.Trammel };
                Map[] ilsh = { Map.Ilshenar };
                Map[] malas = { Map.Malas };
                Map[] tokuno = { Map.Tokuno };

                for (var i = 0; i < list.Count; ++i)
                {
                    var e = list[i];

                    var maps = e.m_Map switch
                    {
                        0 => brit,
                        1 => fel,
                        2 => tram,
                        3 => ilsh,
                        4 => malas,
                        5 => tokuno,
                        _ => null
                    };

                    for (var j = 0; maps?.Length > j; ++j)
                    {
                        Add_Static(e.m_ItemID, e.m_Location, maps[j], e.m_Text);
                    }
                }

                from.SendMessage("Sign generating complete.");
            }
            else
            {
                from.SendMessage("{0} not found!", cfg);
            }
        }

        public static void Add_Static(int itemID, Point3D location, Map map, string name)
        {
            var eable = map.GetItemsInRange(location, 0);

            foreach (var item in eable)
            {
                if (item is Sign && item.Z == location.Z && item.ItemID == itemID)
                {
                    m_ToDelete.Enqueue(item);
                }
            }

            eable.Free();

            while (m_ToDelete.Count > 0)
            {
                m_ToDelete.Dequeue().Delete();
            }

            Item sign;

            if (name.StartsWith("#"))
            {
                sign = new LocalizedSign(itemID, Utility.ToInt32(name.Substring(1)));
            }
            else
            {
                sign = new Sign(itemID);
                sign.Name = name;
            }

            if (map == Map.Malas)
            {
                if (location.X >= 965 && location.Y >= 502 && location.X <= 1012 && location.Y <= 537)
                {
                    sign.Hue = 0x47E;
                }
                else if (location.X >= 1960 && location.Y >= 1278 && location.X < 2106 && location.Y < 1413)
                {
                    sign.Hue = 0x44E;
                }
            }

            sign.MoveToWorld(location, map);
        }

        private class SignEntry
        {
            public readonly int m_ItemID;
            public readonly Point3D m_Location;
            public readonly int m_Map;
            public readonly string m_Text;

            public SignEntry(string text, Point3D pt, int itemID, int mapLoc)
            {
                m_Text = text;
                m_Location = pt;
                m_ItemID = itemID;
                m_Map = mapLoc;
            }
        }
    }
}
