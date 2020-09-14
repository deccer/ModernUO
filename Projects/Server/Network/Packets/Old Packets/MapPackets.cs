/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: MapPackets.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Network
{
    public sealed class MapPatches : Packet
    {
        // TODO: Base this on the client version and expansion
        public MapPatches() : base(0xBF)
        {
            EnsureCapacity(9 + 4 * 8);

            Stream.Write((short)0x18);

            Stream.Write(4);

            Stream.Write(Map.Felucca.Tiles.Patch.StaticBlocks);
            Stream.Write(Map.Felucca.Tiles.Patch.LandBlocks);

            Stream.Write(Map.Trammel.Tiles.Patch.StaticBlocks);
            Stream.Write(Map.Trammel.Tiles.Patch.LandBlocks);

            Stream.Write(Map.Ilshenar.Tiles.Patch.StaticBlocks);
            Stream.Write(Map.Ilshenar.Tiles.Patch.LandBlocks);

            Stream.Write(Map.Malas.Tiles.Patch.StaticBlocks);
            Stream.Write(Map.Malas.Tiles.Patch.LandBlocks);
        }
    }

    public sealed class InvalidMapEnable : Packet
    {
        public InvalidMapEnable() : base(0xC6, 1)
        {
        }
    }

    public sealed class MapChange : Packet
    {
        public MapChange(Map map) : base(0xBF)
        {
            EnsureCapacity(6);

            Stream.Write((short)0x08);
            Stream.Write((byte)map.MapID);
        }
    }
}
