/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DamagePackets.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;

namespace Server.Network
{
    public sealed class DamagePacketOld : Packet
    {
        public DamagePacketOld(Serial mobile, int amount) : base(0xBF)
        {
            EnsureCapacity(11);

            Stream.Write((short)0x22);
            Stream.Write((byte)1);
            Stream.Write(mobile);

            Stream.Write((byte)Math.Clamp(amount, 0, 255));
        }
    }

    public sealed class DamagePacket : Packet
    {
        public DamagePacket(Serial mobile, int amount) : base(0x0B, 7)
        {
            Stream.Write(mobile);

            Stream.Write((ushort)Math.Clamp(amount, 0, 0xFFFF));
        }
    }
}
