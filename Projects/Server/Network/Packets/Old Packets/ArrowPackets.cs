/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ArrowPackets.cs                                                 *
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
    public sealed class CancelArrow : Packet
    {
        public CancelArrow() : base(0xBA, 6)
        {
            Stream.Write((byte)0);
            Stream.Write((short)-1);
            Stream.Write((short)-1);
        }
    }

    public sealed class SetArrow : Packet
    {
        public SetArrow(int x, int y) : base(0xBA, 6)
        {
            Stream.Write((byte)1);
            Stream.Write((short)x);
            Stream.Write((short)y);
        }
    }

    public sealed class CancelArrowHS : Packet
    {
        public CancelArrowHS(int x, int y, Serial s) : base(0xBA, 10)
        {
            Stream.Write((byte)0);
            Stream.Write((short)x);
            Stream.Write((short)y);
            Stream.Write(s);
        }
    }

    public sealed class SetArrowHS : Packet
    {
        public SetArrowHS(int x, int y, Serial s) : base(0xBA, 10)
        {
            Stream.Write((byte)1);
            Stream.Write((short)x);
            Stream.Write((short)y);
            Stream.Write(s);
        }
    }
}
