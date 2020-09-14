/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpSpriteImage.cs                                              *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Network;

namespace Server.Gumps
{
    public class GumpSpriteImage : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("picinpic");

        public GumpSpriteImage(int x, int y, int gumpID, int width, int height, int sx, int sy)
        {
            X = x;
            Y = y;
            GumpID = gumpID;
            Width = width;
            Height = height;
            SX = sx;
            SY = sy;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int GumpID { get; set; }

        public int SX { get; set; }

        public int SY { get; set; }

        public override string Compile(NetState ns) => $"{{ picinpic {X} {Y} {GumpID} {Width} {Height} {SX} {SY} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(GumpID);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);
            disp.AppendLayout(SX);
            disp.AppendLayout(SY);
        }
    }
}
