using System;
using System.Buffers;
using System.Net;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class MobilePacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestDeathAnimation()
        {
            Serial killed = 0x1;
            Serial corpse = 0x1000;

            var data = new DeathAnimation(killed, corpse).Compile();

            Span<byte> expectedData = stackalloc byte[13];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xAF); // Packet ID
            expectedData.Write(ref pos, killed);
            expectedData.Write(ref pos, corpse);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, 0);
#endif

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestBondStatus()
        {
            Serial petSerial = 0x1;
            var bonded = true;

            var data = new BondedStatus(petSerial, bonded).Compile();

            Span<byte> expectedData = stackalloc byte[11];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xBF);   // Packet ID
            expectedData.Write(ref pos, (ushort)0x0B); // Length
            expectedData.Write(ref pos, (ushort)0x19); // Sub-packet

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // Command
#else
            pos++;
#endif

            expectedData.Write(ref pos, petSerial);
            expectedData.Write(ref pos, bonded);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileMoving()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var noto = 10;

            var data = new MobileMoving(m, noto).Compile();

            Span<byte> expectedData = stackalloc byte[17];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x77); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, (ushort)m.Body);
            expectedData.Write(ref pos, m.Location);
            expectedData.Write(ref pos, (byte)m.Direction);
            expectedData.Write(ref pos, (ushort)m.Hue);
            expectedData.Write(ref pos, (byte)m.GetPacketFlags());
            expectedData.Write(ref pos, (byte)noto);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileMovingOld()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var noto = 10;

            var data = new MobileMoving(m, noto).Compile();

            Span<byte> expectedData = stackalloc byte[17];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x77); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, (ushort)m.Body);
            expectedData.Write(ref pos, m.Location);
            expectedData.Write(ref pos, (byte)m.Direction);
            expectedData.Write(ref pos, (ushort)m.Hue);
            expectedData.Write(ref pos, (byte)m.GetOldPacketFlags());
            expectedData.Write(ref pos, (byte)noto);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileName()
        {
            var m = new Mobile(0x1)
            {
                Name = "Some Really Long Mobile Name That Gets Cut off"
            };
            m.DefaultMobileInit();

            var data = new MobileName(m).Compile();

            Span<byte> expectedData = stackalloc byte[37];
            var pos = 0;
            expectedData.Write(ref pos, (byte)0x98);
            expectedData.Write(ref pos, (ushort)0x25);
            expectedData.Write(ref pos, m.Serial);
            expectedData.WriteAsciiFixed(ref pos, m.Name ?? "", 29);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0);
#endif

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileAnimation()
        {
            Serial mobile = 0x1;
            var action = 200;
            var frameCount = 5;
            var repeatCount = 1;
            var reverse = false;
            var repeat = false;
            byte delay = 5;

            var data = new MobileAnimation(
                mobile,
                action,
                frameCount,
                repeatCount,
                !reverse,
                repeat,
                delay
            ).Compile();

            Span<byte> expectedData = stackalloc byte[14];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x6E);
            expectedData.Write(ref pos, mobile);
            expectedData.Write(ref pos, (ushort)action);
            expectedData.Write(ref pos, (ushort)frameCount);
            expectedData.Write(ref pos, (ushort)repeatCount);
            expectedData.Write(ref pos, reverse);
            expectedData.Write(ref pos, repeat);
            expectedData.Write(ref pos, delay);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestNewMobileAnimation()
        {
            Serial mobile = 0x1;
            var action = 200;
            var frameCount = 5;
            byte delay = 5;

            var data = new NewMobileAnimation(
                mobile,
                action,
                frameCount,
                delay
            ).Compile();

            Span<byte> expectedData = stackalloc byte[10];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xE2);
            expectedData.Write(ref pos, mobile);
            expectedData.Write(ref pos, (ushort)action);
            expectedData.Write(ref pos, (ushort)frameCount);
            expectedData.Write(ref pos, delay);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileStatusCompact()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var canBeRenamed = false;

            var data = new MobileStatusCompact(canBeRenamed, m).Compile();

            Span<byte> expectedData = stackalloc byte[43];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x11);                  // Packet ID
            expectedData.Write(ref pos, (ushort)expectedData.Length); // Length

            expectedData.Write(ref pos, m.Serial);
            expectedData.WriteAsciiFixed(ref pos, m.Name ?? "", 30);

            expectedData.WriteReverseAttribute(ref pos, m.Hits, m.HitsMax, true);
            expectedData.Write(ref pos, canBeRenamed);

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // type
#endif

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(ProtocolChanges.Version70610)]
        [InlineData(ProtocolChanges.Version400a)]
        [InlineData(ProtocolChanges.Version502b)]
        public void TestMobileStatusExtended(ProtocolChanges changes)
        {
            var beholder = new Mobile(0x1)
            {
                Name = "Random Mobile 1"
            };
            beholder.DefaultMobileInit();

            var beheld = new Mobile(0x2)
            {
                Name = "Random Mobile 2"
            };
            beheld.DefaultMobileInit();

            var ns = new NetState(
                new AccountPacketTests.TestConnectionContext
                {
                    RemoteEndPoint = IPEndPoint.Parse("127.0.0.1")
                }
            )
            {
                ProtocolChanges = changes
            };

            var data = new MobileStatus(beholder, beheld, ns).Compile();

            Span<byte> expectedData = stackalloc byte[121]; // Max Size
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x11);
            pos += 2; // Length

            int type;
            var notSelf = beholder != beheld;

            if (notSelf)
            {
                type = 0;
            }
            else if (Core.HS && ns.ExtendedStatus)
            {
                type = 6;
            }
            else if (Core.ML && ns.SupportsExpansion(Expansion.ML))
            {
                type = 5;
            }
            else
            {
                type = Core.AOS ? 4 : 3;
            }

            expectedData.Write(ref pos, beheld.Serial);
            expectedData.WriteAsciiFixed(ref pos, beheld.Name, 30);

            expectedData.WriteReverseAttribute(ref pos, beheld.Hits, beheld.HitsMax, notSelf);

            expectedData.Write(ref pos, beheld.CanBeRenamedBy(beheld));
            expectedData.Write(ref pos, (byte)type);

            if (type > 0)
            {
                expectedData.Write(ref pos, beheld.Female);
                expectedData.Write(ref pos, (ushort)beheld.Str);
                expectedData.Write(ref pos, (ushort)beheld.Dex);
                expectedData.Write(ref pos, (ushort)beheld.Int);

                expectedData.WriteReverseAttribute(ref pos, beheld.Stam, beheld.StamMax, notSelf);
                expectedData.WriteReverseAttribute(ref pos, beheld.Mana, beheld.ManaMax, notSelf);

                expectedData.Write(ref pos, beheld.TotalGold);
                expectedData.Write(
                    ref pos,
                    (ushort)(Core.AOS ? beheld.PhysicalResistance : (int)(beheld.ArmorRating + 0.5))
                );
                expectedData.Write(ref pos, (ushort)(Mobile.BodyWeight + beheld.TotalWeight));

                if (type >= 5)
                {
                    expectedData.Write(ref pos, (ushort)beheld.MaxWeight);
                    expectedData.Write(ref pos, (byte)(beheld.Race.RaceID + 1)); // 0x00 for a non-ML enabled account
                }

                expectedData.Write(ref pos, (ushort)beheld.StatCap);
                expectedData.Write(ref pos, (byte)beheld.Followers);
                expectedData.Write(ref pos, (byte)beheld.FollowersMax);

                if (type >= 4)
                {
                    expectedData.Write(ref pos, (ushort)beheld.FireResistance);
                    expectedData.Write(ref pos, (ushort)beheld.ColdResistance);
                    expectedData.Write(ref pos, (ushort)beheld.PoisonResistance);
                    expectedData.Write(ref pos, (ushort)beheld.EnergyResistance);
                    expectedData.Write(ref pos, (ushort)beheld.Luck);
                }

                var min = 0;
                var max = 0;
                beheld.Weapon?.GetStatusDamage(beheld, out min, out max);

                expectedData.Write(ref pos, (ushort)min);
                expectedData.Write(ref pos, (ushort)max);

                expectedData.Write(ref pos, beheld.TithingPoints);

                if (type >= 6)
                {
                    for (var i = 0; i < 15; ++i)
                    {
                        expectedData.Write(ref pos, (ushort)beheld.GetAOSStatus(i));
                    }
                }
            }

            expectedData.Slice(1, 2).Write((ushort)pos); // Length

            expectedData = expectedData.Slice(0, pos);
            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData("None")]
        [InlineData("Lesser")]
        [InlineData("Lethal")]
        public void TestHealthbarPoison(string pName)
        {
            var p = Poison.GetPoison(pName);
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Poison = p;

            var data = new HealthbarPoison(m).Compile();

            Span<byte> expectedData = stackalloc byte[12];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x17); // Packet ID
            expectedData.Write(ref pos, (ushort)12); // Length
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, 0x10001); // Show Bar?, Poison Bar
            expectedData.Write(ref pos, (byte)((p?.Level ?? -1) + 1));

            AssertThat.Equal(data, expectedData);
            Assert.Equal(p?.Level, m.Poison?.Level);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void TestYellowBar(bool isBlessed, bool isYellowHealth)
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();
            m.Blessed = isBlessed;
            m.YellowHealthbar = isYellowHealth;

            var data = new HealthbarYellow(m).Compile();

            Span<byte> expectedData = stackalloc byte[12];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x17); // Packet ID
            expectedData.Write(ref pos, (ushort)12); // Length
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, 0x10002); // Show Bar?, Yellow Bar
            expectedData.Write(ref pos, isBlessed || isYellowHealth);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileUpdate()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileUpdate(m).Compile();

            Span<byte> expectedData = stackalloc byte[19];
            var pos = 0;

            var hue = m.SolidHueOverride >= 0 ? m.SolidHueOverride : m.Hue;

            expectedData.Write(ref pos, (byte)0x20); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, (ushort)m.Body);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // Unknown
#else
            pos++;
#endif
            expectedData.Write(ref pos, (ushort)hue);
            expectedData.Write(ref pos, (byte)m.GetPacketFlags());
            expectedData.Write(ref pos, (ushort)m.X);
            expectedData.Write(ref pos, (ushort)m.Y);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (ushort)2); // Unknown
#else
            pos += 2;
#endif
            expectedData.Write(ref pos, (byte)m.Direction);
            expectedData.Write(ref pos, (byte)m.Z);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileUpdateOld()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileUpdateOld(m).Compile();

            Span<byte> expectedData = stackalloc byte[19];
            var pos = 0;

            var hue = m.SolidHueOverride >= 0 ? m.SolidHueOverride : m.Hue;

            expectedData.Write(ref pos, (byte)0x20); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, (ushort)m.Body);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // Unknown
#else
            pos++;
#endif
            expectedData.Write(ref pos, (ushort)hue);
            expectedData.Write(ref pos, (byte)m.GetOldPacketFlags());
            expectedData.Write(ref pos, (ushort)m.X);
            expectedData.Write(ref pos, (ushort)m.Y);
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (ushort)2); // Unknown
#else
            pos += 2;
#endif
            expectedData.Write(ref pos, (byte)m.Direction);
            expectedData.Write(ref pos, (byte)m.Z);

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(10, 1024, 0, 0)]
        [InlineData(10, 1024, 11, 2048)]
        public void TestMobileIncoming(int hairItemId, int hairHue, int facialHairItemId, int facialHairHue)
        {
            var beholder = new Mobile(0x1)
            {
                Name = "Random Mobile 1"
            };
            beholder.DefaultMobileInit();

            var beheld = new Mobile(0x2)
            {
                Name = "Random Mobile 2"
            };
            beheld.DefaultMobileInit();
            beheld.AddItem(
                new Item((Serial)0x1000)
                {
                    Layer = Layer.OneHanded
                }
            );

            // Test Dupe
            beheld.AddItem(
                new Item((Serial)0x1001)
                {
                    Layer = Layer.OneHanded
                }
            );

            beheld.HairItemID = hairItemId;
            beheld.HairHue = hairHue;
            beheld.FacialHairItemID = facialHairItemId;
            beheld.FacialHairHue = facialHairHue;

            var data = new MobileIncoming(beholder, beheld).Compile();

            Span<bool> layers = stackalloc bool[256];
#if NO_LOCAL_INIT
      layers.Clear();
#endif

            var items = beheld.Items;
            var count = items.Count;

            if (beheld.HairItemID > 0)
            {
                count++;
            }

            if (beheld.FacialHairItemID > 0)
            {
                count++;
            }

            var length = 23 + count * 9; // Max Size

            Span<byte> expectedData = stackalloc byte[length];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x78);
            pos += 2; // Length

            var isSolidHue = beheld.SolidHueOverride >= 0;

            expectedData.Write(ref pos, beheld.Serial);
            expectedData.Write(ref pos, (ushort)beheld.Body);
            expectedData.Write(ref pos, (ushort)beheld.X);
            expectedData.Write(ref pos, (ushort)beheld.Y);
            expectedData.Write(ref pos, (byte)beheld.Z);
            expectedData.Write(ref pos, (byte)beheld.Direction);
            expectedData.Write(ref pos, (ushort)(isSolidHue ? beheld.SolidHueOverride : beheld.Hue));
            expectedData.Write(ref pos, (byte)beheld.GetPacketFlags());
            expectedData.Write(ref pos, (byte)Notoriety.Compute(beholder, beheld));

            byte layer;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                layer = (byte)item.Layer;

                if (!item.Deleted && !layers[layer] && beholder.CanSee(item))
                {
                    layers[layer] = true;

                    expectedData.Write(ref pos, item.Serial);
                    expectedData.Write(ref pos, (ushort)(item.ItemID & 0xFFFF));
                    expectedData.Write(ref pos, layer);
                    expectedData.Write(ref pos, (ushort)(isSolidHue ? beheld.SolidHueOverride : item.Hue));
                }
            }

            layer = (byte)Layer.Hair;
            var itemId = beheld.HairItemID & 0xFFFF;

            if (itemId > 0 && !layers[layer])
            {
                expectedData.Write(ref pos, HairInfo.FakeSerial(beheld));
                expectedData.Write(ref pos, (ushort)itemId);
                expectedData.Write(ref pos, layer);
                expectedData.Write(ref pos, (ushort)(isSolidHue ? beheld.SolidHueOverride : beheld.HairHue));
            }

            layer = (byte)Layer.FacialHair;
            itemId = beheld.FacialHairItemID & 0xFFFF;

            if (itemId > 0 && !layers[layer])
            {
                expectedData.Write(ref pos, FacialHairInfo.FakeSerial(beheld));
                expectedData.Write(ref pos, (ushort)itemId);
                expectedData.Write(ref pos, layer);
                expectedData.Write(ref pos, (ushort)(isSolidHue ? beheld.SolidHueOverride : beheld.FacialHairHue));
            }

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, 0); // Zero serial, terminate list
#else
            pos += 4;
#endif

            expectedData.Slice(1, 2).Write((ushort)pos); // Length
            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(ProtocolChanges.Version6000, 0, 0, 0, 0)]
        [InlineData(ProtocolChanges.Version6000, 10, 1024, 0, 0)]
        [InlineData(ProtocolChanges.Version6000, 10, 1024, 11, 2048)]
        [InlineData(ProtocolChanges.Version7000, 0, 0, 0, 0)]
        [InlineData(ProtocolChanges.Version7000, 10, 1024, 0, 0)]
        [InlineData(ProtocolChanges.Version7000, 10, 1024, 11, 2048)]
        public void TestMobileIncomingOld(
            ProtocolChanges protocolChanges, int hairItemId, int hairHue, int facialHairItemId, int facialHairHue
        )
        {
            var beholder = new Mobile(0x1)
            {
                Name = "Random Mobile 1"
            };
            beholder.DefaultMobileInit();

            var beheld = new Mobile(0x2)
            {
                Name = "Random Mobile 2"
            };
            beheld.DefaultMobileInit();
            beheld.AddItem(
                new Item((Serial)0x1000)
                {
                    Layer = Layer.OneHanded
                }
            );

            // Test Dupe
            beheld.AddItem(
                new Item((Serial)0x1001)
                {
                    Layer = Layer.OneHanded
                }
            );

            beheld.HairItemID = hairItemId;
            beheld.HairHue = hairHue;
            beheld.FacialHairItemID = facialHairItemId;
            beheld.FacialHairHue = facialHairHue;

            var ns = new NetState(
                new AccountPacketTests.TestConnectionContext
                {
                    RemoteEndPoint = IPEndPoint.Parse("127.0.0.1")
                }
            )
            {
                ProtocolChanges = protocolChanges
            };

            var data = (ns.StygianAbyss
                    ? (Packet)new MobileIncomingSA(beholder, beheld)
                    : new MobileIncomingOld(beholder, beheld))
                .Compile();

            Span<bool> layers = stackalloc bool[256];
#if NO_LOCAL_INIT
      layers.Clear();
#endif

            var items = beheld.Items;
            var count = items.Count;

            if (beheld.HairItemID > 0)
            {
                count++;
            }

            if (beheld.FacialHairItemID > 0)
            {
                count++;
            }

            var length = 23 + count * 9; // Max Size

            Span<byte> expectedData = stackalloc byte[length];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x78);
            pos += 2; // Length

            var isSolidHue = beheld.SolidHueOverride >= 0;

            expectedData.Write(ref pos, beheld.Serial);
            expectedData.Write(ref pos, (ushort)beheld.Body);
            expectedData.Write(ref pos, (ushort)beheld.X);
            expectedData.Write(ref pos, (ushort)beheld.Y);
            expectedData.Write(ref pos, (byte)beheld.Z);
            expectedData.Write(ref pos, (byte)beheld.Direction);
            expectedData.Write(ref pos, (ushort)(isSolidHue ? beheld.SolidHueOverride : beheld.Hue));
            expectedData.Write(ref pos, (byte)(ns.StygianAbyss ? beheld.GetOldPacketFlags() : beheld.GetPacketFlags()));
            expectedData.Write(ref pos, (byte)Notoriety.Compute(beholder, beheld));

            byte layer;
            int itemId;
            int hue;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                layer = (byte)item.Layer;

                if (!item.Deleted && !layers[layer] && beholder.CanSee(item))
                {
                    layers[layer] = true;
                    itemId = item.ItemID & 0x7FFF;
                    hue = isSolidHue ? beheld.SolidHueOverride : item.Hue;

                    if (hue != 0)
                    {
                        itemId |= 0x8000;
                    }

                    expectedData.Write(ref pos, item.Serial);
                    expectedData.Write(ref pos, (ushort)itemId);
                    expectedData.Write(ref pos, layer);
                    expectedData.Write(ref pos, (ushort)hue);
                }
            }

            layer = (byte)Layer.Hair;
            itemId = beheld.HairItemID & 0x7FFF;

            if (itemId > 0 && !layers[layer])
            {
                hue = isSolidHue ? beheld.SolidHueOverride : beheld.HairHue;

                if (hue != 0)
                {
                    itemId |= 0x8000;
                }

                expectedData.Write(ref pos, HairInfo.FakeSerial(beheld));
                expectedData.Write(ref pos, (ushort)itemId);
                expectedData.Write(ref pos, layer);
                expectedData.Write(ref pos, (ushort)hue);
            }

            layer = (byte)Layer.FacialHair;
            itemId = beheld.FacialHairItemID & 0x7FFF;

            if (itemId > 0 && !layers[layer])
            {
                hue = isSolidHue ? beheld.SolidHueOverride : beheld.FacialHairHue;

                if (hue != 0)
                {
                    itemId |= 0x8000;
                }

                expectedData.Write(ref pos, FacialHairInfo.FakeSerial(beheld));
                expectedData.Write(ref pos, (ushort)itemId);
                expectedData.Write(ref pos, layer);
                expectedData.Write(ref pos, (ushort)hue);
            }

#if NO_LOCAL_INIT
      expectedData.Write(ref pos, 0); // Zero serial, terminate list
#else
            pos += 4;
#endif

            expectedData.Slice(1, 2).Write((ushort)pos); // Length
            expectedData = expectedData.Slice(0, pos);

            AssertThat.Equal(data, expectedData);
        }
    }

    [Collection("Sequential Tests")]
    public class SequentialMobilePacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestMobileHits()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileHits(m).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xA1); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.WriteAttribute(ref pos, m.Hits, m.HitsMax, false);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileHitsN()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileHitsN(m).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xA1); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.WriteAttribute(ref pos, m.Hits, m.HitsMax, true);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileMana()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileMana(m).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xA2); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.WriteAttribute(ref pos, m.Mana, m.ManaMax, false);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileManaN()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileManaN(m).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xA2); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.WriteAttribute(ref pos, m.Mana, m.ManaMax, true);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileStam()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileStam(m).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xA3); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.WriteAttribute(ref pos, m.Stam, m.StamMax, false);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileStamN()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileStamN(m).Compile();

            Span<byte> expectedData = stackalloc byte[9];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xA3); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.WriteAttribute(ref pos, m.Stam, m.StamMax, true);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileAttributes()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileAttributes(m).Compile();

            Span<byte> expectedData = stackalloc byte[17];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x2D); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.WriteAttribute(ref pos, m.Hits, m.HitsMax, false);
            expectedData.WriteAttribute(ref pos, m.Mana, m.ManaMax, false);
            expectedData.WriteAttribute(ref pos, m.Stam, m.StamMax, false);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMobileAttributesN()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            var data = new MobileAttributesN(m).Compile();

            Span<byte> expectedData = stackalloc byte[17];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x2D); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.WriteAttribute(ref pos, m.Hits, m.HitsMax, true);
            expectedData.WriteAttribute(ref pos, m.Mana, m.ManaMax, true);
            expectedData.WriteAttribute(ref pos, m.Stam, m.StamMax, true);

            AssertThat.Equal(data, expectedData);
        }
    }
}
