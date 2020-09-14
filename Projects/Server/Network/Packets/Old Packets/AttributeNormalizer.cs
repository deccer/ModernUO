/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: AttributeNormalizer.cs                                          *
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
    public static class AttributeNormalizer
    {
        public static int Maximum { get; set; } = 25;

        public static bool Enabled { get; set; } = true;

        public static void Write(PacketWriter stream, int cur, int max)
        {
            if (Enabled && max != 0)
            {
                stream.Write((short)Maximum);
                stream.Write((short)(cur * Maximum / max));
            }
            else
            {
                stream.Write((short)max);
                stream.Write((short)cur);
            }
        }

        public static void WriteReverse(PacketWriter stream, int cur, int max)
        {
            if (Enabled && max != 0)
            {
                stream.Write((short)(cur * Maximum / max));
                stream.Write((short)Maximum);
            }
            else
            {
                stream.Write((short)cur);
                stream.Write((short)max);
            }
        }
    }
}
