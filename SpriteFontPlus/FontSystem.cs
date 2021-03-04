using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpriteFontPlus {
    unsafe class FontSystem : IDisposable {
        class GlyphCollection {
            internal readonly Int32Map<FontGlyph> Glyphs = new Int32Map<FontGlyph>();
        }

        readonly Int32Map<GlyphCollection> _glyphs = new Int32Map<GlyphCollection>();

        readonly List<Font> _fonts = new List<Font>();
        float _ith;
        float _itw;
        FontAtlas _currentAtlas;
        Point _size;
        int _fontSize;

        public int FontSize {
            get { return _fontSize; }

            set {
                if (value == _fontSize) {
                    return;
                }

                _fontSize = value;
                foreach (var f in _fonts) {
                    f.Recalculate(_fontSize);
                }
            }
        }

        public Color Color;
        public readonly int BlurAmount;
        public readonly int StrokeAmount;
        public float Spacing;
        public Vector2 Scale;
        public bool UseKernings = true;

        public int? DefaultCharacter = ' ';

        public FontAtlas CurrentAtlas {
            get {
                if (_currentAtlas == null) {
                    _currentAtlas = new FontAtlas(_size.X, _size.Y, 256);
                    Atlases.Add(_currentAtlas);
                }

                return _currentAtlas;
            }
        }

        public List<FontAtlas> Atlases { get; } = new List<FontAtlas>();

        public event EventHandler CurrentAtlasFull;

        public FontSystem(int width, int height, int blurAmount = 0, int strokeAmount = 0) {
            if (width <= 0) {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0) {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (blurAmount < 0 || blurAmount > 20) {
                throw new ArgumentOutOfRangeException(nameof(blurAmount));
            }

            if (strokeAmount < 0 || strokeAmount > 20) {
                throw new ArgumentOutOfRangeException(nameof(strokeAmount));
            }

            if (strokeAmount != 0 && blurAmount != 0) {
                throw new ArgumentException("Cannot have both blur and stroke.");
            }

            BlurAmount = blurAmount;
            StrokeAmount = strokeAmount;

            _size = new Point(width, height);

            _itw = 1.0f / _size.X;
            _ith = 1.0f / _size.Y;
            ClearState();
        }

        public void Dispose() {
            if (_fonts != null) {
                foreach (var font in _fonts)
                    font.Dispose();
                _fonts.Clear();
            }
            Atlases?.Clear();
            _currentAtlas = null;
            _glyphs?.Clear();
        }

        public void ClearState() {
            FontSize = 12;
            Color = Color.White;
            Spacing = 0;
        }

        public void AddFontMem(byte[] data) {
            var font = Font.FromMemory(data);

            font.Recalculate(FontSize);
            _fonts.Add(font);
        }

        GlyphCollection GetGlyphsCollection(int size) {
            GlyphCollection result;
            if (_glyphs.TryGetValue(size, out result)) {
                return result;
            }

            result = new GlyphCollection();
            _glyphs[size] = result;
            return result;
        }

        public float DrawText(SpriteBatch batch, float x, float y, StringBuilder str, float depth) {
            if (str.Length == 0) return 0.0f;

            var collection = GetGlyphsCollection(FontSize);

            // Determine ascent and lineHeight from first character
            float ascent = 0, lineHeight = 0;
            for (var i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1) {
                var codepoint = StringBuilderConvertToUtf32(str, i);

                var glyph = GetGlyph(batch.GraphicsDevice, collection, codepoint);
                if (glyph == null) {
                    continue;
                }

                ascent = glyph.Font.Ascent;
                lineHeight = glyph.Font.LineHeight;
                break;
            }

            var q = new FontGlyphSquad();

            var originX = 0.0f;
            var originY = 0.0f;

            originY += ascent;

            FontGlyph prevGlyph = null;
            for (var i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1) {
                var codepoint = StringBuilderConvertToUtf32(str, i);

                if (codepoint == '\n') {
                    originX = 0.0f;
                    originY += lineHeight;
                    prevGlyph = null;
                    continue;
                }

                var glyph = GetGlyph(batch.GraphicsDevice, collection, codepoint);
                if (glyph == null) {
                    continue;
                }

                GetQuad(glyph, prevGlyph, collection, Spacing, ref originX, ref originY, &q);

                var sourceRect = new Rectangle((int)(q.S0 * _size.X),
                    (int)(q.T0 * _size.Y),
                    (int)((q.S1 - q.S0) * _size.X),
                    (int)((q.T1 - q.T0) * _size.Y));

                batch.Draw(glyph.Atlas.Texture,
                    new Vector2(x + q.X0 * scaleX, y + q.Y0 * scaleY),
                    sourceRect,
                    Color,
                    0f,
                    Vector2.Zero,
                    new Vector2(scaleX, scaleY),
                    SpriteEffects.None,
                    depth);

                prevGlyph = glyph;
            }

            return x;
        }


        public float DrawText(SpriteBatch batch, float x, float y, string str, float depth) {
            if (string.IsNullOrEmpty(str)) return 0.0f;

            var collection = GetGlyphsCollection(FontSize);

            // Determine ascent and lineHeight from first character
            float ascent = 0, lineHeight = 0;
            for (var i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1) {
                var codepoint = char.ConvertToUtf32(str, i);

                var glyph = GetGlyph(batch.GraphicsDevice, collection, codepoint);
                if (glyph == null) {
                    continue;
                }

                ascent = glyph.Font.Ascent;
                lineHeight = glyph.Font.LineHeight;
                break;
            }

            var q = new FontGlyphSquad();

            var originX = 0.0f;
            var originY = 0.0f;

            originY += ascent;

            FontGlyph prevGlyph = null;
            for (var i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1) {
                var codepoint = char.ConvertToUtf32(str, i);

                if (codepoint == '\n') {
                    originX = 0.0f;
                    originY += lineHeight;
                    prevGlyph = null;
                    continue;
                }

                var glyph = GetGlyph(batch.GraphicsDevice, collection, codepoint);
                if (glyph == null) {
                    continue;
                }

                GetQuad(glyph, prevGlyph, collection, Spacing, ref originX, ref originY, &q);

                var sourceRect = new Rectangle((int)(q.S0 * _size.X),
                    (int)(q.T0 * _size.Y),
                    (int)((q.S1 - q.S0) * _size.X),
                    (int)((q.T1 - q.T0) * _size.Y));

                batch.Draw(glyph.Atlas.Texture,
                    new Vector2(x + q.X0 * scaleX, y + q.Y0 * scaleY),
                    sourceRect,
                    Color,
                    0f,
                    Vector2.Zero,
                    new Vector2(scaleX, scaleY),
                    SpriteEffects.None,
                    depth);

                prevGlyph = glyph;
            }

            return x;
        }

        public float TextBounds(float x, float y, string str, ref Bounds bounds) {
            if (string.IsNullOrEmpty(str)) return 0.0f;

            var collection = GetGlyphsCollection(FontSize);

            // Determine ascent and lineHeight from first character
            float ascent = 0, lineHeight = 0;
            for (var i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1) {
                var codepoint = char.ConvertToUtf32(str, i);

                var glyph = GetGlyph(null, collection, codepoint);
                if (glyph == null) {
                    continue;
                }

                ascent = glyph.Font.Ascent;
                lineHeight = glyph.Font.LineHeight;
                break;
            }


            var q = new FontGlyphSquad();
            float startx = 0;
            float advance = 0;

            y += ascent;

            float minx, maxx, miny, maxy;
            minx = maxx = x;
            miny = maxy = y;
            startx = x;

            FontGlyph prevGlyph = null;

            for (var i = 0; i < str.Length; i += char.IsSurrogatePair(str, i) ? 2 : 1) {
                var codepoint = char.ConvertToUtf32(str, i);

                if (codepoint == '\n') {
                    x = startx;
                    y += lineHeight;
                    prevGlyph = null;
                    continue;
                }

                var glyph = GetGlyph(null, collection, codepoint);
                if (glyph == null) {
                    continue;
                }

                GetQuad(glyph, prevGlyph, collection, Spacing, ref x, ref y, &q);
                if (q.X0 < minx)
                    minx = q.X0;
                if (x > maxx)
                    maxx = x;
                if (q.Y0 < miny)
                    miny = q.Y0;
                if (q.Y1 > maxy)
                    maxy = q.Y1;

                prevGlyph = glyph;
            }

            advance = x - startx;

            bounds.X = minx;
            bounds.Y = miny;
            bounds.X2 = maxx;
            bounds.Y2 = maxy;

            return advance;
        }

        public float TextBounds(float x, float y, StringBuilder str, ref Bounds bounds) {
            if (str == null || str.Length <= 0) return 0.0f;

            var collection = GetGlyphsCollection(FontSize);

            // Determine ascent and lineHeight from first character
            float ascent = 0, lineHeight = 0;
            for (var i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1) {
                var codepoint = StringBuilderConvertToUtf32(str, i);

                var glyph = GetGlyph(null, collection, codepoint);
                if (glyph == null) {
                    continue;
                }

                ascent = glyph.Font.Ascent;
                lineHeight = glyph.Font.LineHeight;
                break;
            }


            var q = new FontGlyphSquad();
            float startx = 0;
            float advance = 0;

            y += ascent;

            float minx, maxx, miny, maxy;
            minx = maxx = x;
            miny = maxy = y;
            startx = x;

            FontGlyph prevGlyph = null;

            for (var i = 0; i < str.Length; i += StringBuilderIsSurrogatePair(str, i) ? 2 : 1) {
                var codepoint = StringBuilderConvertToUtf32(str, i);

                if (codepoint == '\n') {
                    x = startx;
                    y += lineHeight;
                    prevGlyph = null;
                    continue;
                }

                var glyph = GetGlyph(null, collection, codepoint);
                if (glyph == null) {
                    continue;
                }

                GetQuad(glyph, prevGlyph, collection, Spacing, ref x, ref y, &q);
                if (q.X0 < minx)
                    minx = q.X0;
                if (x > maxx)
                    maxx = x;
                if (q.Y0 < miny)
                    miny = q.Y0;
                if (q.Y1 > maxy)
                    maxy = q.Y1;

                prevGlyph = glyph;
            }

            advance = x - startx;

            bounds.X = minx;
            bounds.Y = miny;
            bounds.X2 = maxx;
            bounds.Y2 = maxy;

            return advance;
        }

        bool StringBuilderIsSurrogatePair(StringBuilder sb, int index) {
            if (sb == null)
                throw new ArgumentNullException(nameof(sb));
            if (index < 0 || index > sb.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index + 1 < sb.Length)
                return char.IsSurrogatePair(sb[index], sb[index + 1]);
            return false;
        }

        int StringBuilderConvertToUtf32(StringBuilder sb, int index) {
            if (sb == null)
                throw new ArgumentNullException(nameof(sb));
            if (index < 0 || index > sb.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (!char.IsHighSurrogate(sb[index]))
                return sb[index];

            if (index >= sb.Length - 1)
                throw new Exception("Invalid High Surrogate.");

            return char.ConvertToUtf32(sb[index], sb[index + 1]);
        }

        public void Reset(int width, int height) {
            Atlases.Clear();

            _glyphs.Clear();

            if (width == _size.X && height == _size.Y)
                return;

            _size = new Point(width, height);
            _itw = 1.0f / _size.X;
            _ith = 1.0f / _size.Y;
        }

        public void Reset() {
            Reset(_size.X, _size.Y);
        }

        int GetCodepointIndex(int codepoint, out Font font) {
            font = null;

            var g = 0;
            foreach (var f in _fonts) {
                g = f.GetGlyphIndex(codepoint);
                if (g != 0) {
                    font = f;
                    break;
                }
            }

            return g;
        }

        FontGlyph GetGlyphWithoutBitmap(GlyphCollection collection, int codepoint) {
            FontGlyph glyph = null;
            if (collection.Glyphs.TryGetValue(codepoint, out glyph)) {
                return glyph;
            }

            Font font;
            var g = GetCodepointIndex(codepoint, out font);
            if (g == 0) {
                collection.Glyphs[codepoint] = null;
                return null;
            }

            int advance, lsb, x0, y0, x1, y1;
            font.BuildGlyphBitmap(g, FontSize, font.Scale, &advance, &lsb, &x0, &y0, &x1, &y1);

            var pad = Math.Max(FontGlyph.PadFromBlur(BlurAmount), FontGlyph.PadFromBlur(StrokeAmount));
            var gw = x1 - x0 + pad * 2;
            var gh = y1 - y0 + pad * 2;
            var offset = FontGlyph.PadFromBlur(BlurAmount);

            glyph = new FontGlyph {
                Font = font,
                Codepoint = codepoint,
                Size = FontSize,
                Index = g,
                Bounds = new Rectangle(0, 0, gw, gh),
                XAdvance = (int)(font.Scale * advance * 10.0f),
                XOffset = x0 - offset,
                YOffset = y0 - offset
            };

            collection.Glyphs[codepoint] = glyph;

            return glyph;
        }

        FontGlyph GetGlyphInternal(GraphicsDevice graphicsDevice, GlyphCollection glyphs, int codepoint) {
            var glyph = GetGlyphWithoutBitmap(glyphs, codepoint);
            if (glyph == null) {
                return null;
            }

            if (graphicsDevice == null || glyph.Atlas != null)
                return glyph;

            var currentAtlas = CurrentAtlas;
            int gx = 0, gy = 0;
            var gw = glyph.Bounds.Width;
            var gh = glyph.Bounds.Height;
            if (!currentAtlas.AddRect(gw, gh, ref gx, ref gy)) {
                CurrentAtlasFull?.Invoke(this, EventArgs.Empty);

                // This code will force creation of new atlas
                _currentAtlas = null;
                currentAtlas = CurrentAtlas;

                // Try to add again
                if (!currentAtlas.AddRect(gw, gh, ref gx, ref gy)) {
                    throw new Exception(string.Format("Could not add rect to the newly created atlas. gw={0}, gh={1}", gw, gh));
                }
            }

            glyph.Bounds.X = gx;
            glyph.Bounds.Y = gy;

            currentAtlas.RenderGlyph(graphicsDevice, glyph, BlurAmount, StrokeAmount);

            glyph.Atlas = currentAtlas;

            return glyph;
        }

        FontGlyph GetGlyph(GraphicsDevice graphicsDevice, GlyphCollection glyphs, int codepoint) {
            var result = GetGlyphInternal(graphicsDevice, glyphs, codepoint);
            if (result == null && DefaultCharacter != null) {
                result = GetGlyphInternal(graphicsDevice, glyphs, DefaultCharacter.Value);
            }

            return result;
        }

        void GetQuad(FontGlyph glyph, FontGlyph prevGlyph, GlyphCollection collection, float spacing, ref float x, ref float y, FontGlyphSquad* q) {
            if (prevGlyph != null) {
                float adv = 0;
                if (UseKernings && glyph.Font == prevGlyph.Font) {
                    adv = prevGlyph.Font.GetGlyphKernAdvance(prevGlyph.Index, glyph.Index) * glyph.Font.Scale;
                }

                x += (int)(adv + spacing + 0.5f);
            }

            float rx = 0;
            float ry = 0;

            rx = x + glyph.XOffset;
            ry = y + glyph.YOffset;
            q->X0 = rx;
            q->Y0 = ry;
            q->X1 = rx + glyph.Bounds.Width;
            q->Y1 = ry + glyph.Bounds.Height;
            q->S0 = glyph.Bounds.X * _itw;
            q->T0 = glyph.Bounds.Y * _ith;
            q->S1 = glyph.Bounds.Right * _itw;
            q->T1 = glyph.Bounds.Bottom * _ith;

            x += (int)(glyph.XAdvance / 10.0f + 0.5f);
        }
    }
}
