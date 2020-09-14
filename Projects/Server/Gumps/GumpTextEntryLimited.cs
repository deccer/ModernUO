using Server.Network;

namespace Server.Gumps
{
    public class GumpTextEntryLimited : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("textentrylimited");

        public GumpTextEntryLimited(
            int x, int y, int width, int height, int hue, int entryID, string initialText, int size = 0
        )
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Hue = hue;
            EntryID = entryID;
            InitialText = initialText;
            Size = size;
        }

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Hue { get; set; }

        public int EntryID { get; set; }

        public string InitialText { get; set; }

        public int Size { get; set; }

        public override string Compile(NetState ns) =>
            $"{{ textentrylimited {X} {Y} {Width} {Height} {Hue} {EntryID} {Parent.Intern(InitialText)} {Size} }}";

        public override void AppendTo(NetState ns, IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
            disp.AppendLayout(X);
            disp.AppendLayout(Y);
            disp.AppendLayout(Width);
            disp.AppendLayout(Height);
            disp.AppendLayout(Hue);
            disp.AppendLayout(EntryID);
            disp.AppendLayout(Parent.Intern(InitialText));
            disp.AppendLayout(Size);

            disp.TextEntries++;
        }
    }
}
