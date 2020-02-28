using Microsoft.Xna.Framework;


namespace FontStashSharp {
    internal class FontGlyph {
        private readonly Int32Map<int> _kernings = new Int32Map<int>();

        public Font Font;
        public FontAtlas Atlas;
        public int Codepoint;
        public int Index;
        public int Size;
        public int Blur;
        public Rectangle Bounds;
        public int XAdvance;
        public int XOffset;
        public int YOffset;

        public int Pad => PadFromBlur(Blur);

        public int GetKerning(FontGlyph nextGlyph) {
            int result;
            if (_kernings.TryGetValue(nextGlyph.Index, out result)) {
                return result;
            }
            result = Font.GetGlyphKernAdvance(Index, nextGlyph.Index);
            _kernings[nextGlyph.Index] = result;

            return result;
        }

        public static int PadFromBlur(int blur) {
            return blur + 2;
        }
    }
}
