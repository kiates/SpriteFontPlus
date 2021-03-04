using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpriteFontPlus {
    public class DynamicSpriteFont: IDisposable {
        internal struct TextureEnumerator : IEnumerable<Texture2D> {
            readonly FontSystem _font;

            public TextureEnumerator(FontSystem font) {
                _font = font;
            }

            public IEnumerator<Texture2D> GetEnumerator() {
                foreach (var atlas in _font.Atlases) {
                    yield return atlas.Texture;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        readonly FontSystem _fontSystem;

        public IEnumerable<Texture2D> Textures {
            get { return new TextureEnumerator(_fontSystem); }
        }

        public int Size {
            get { return _fontSystem.FontSize; }
            set { _fontSystem.FontSize = value; }
        }

        public float Spacing {
            get { return _fontSystem.Spacing; }
            set { _fontSystem.Spacing = value; }
        }

        public bool UseKernings {
            get { return _fontSystem.UseKernings; }

            set { _fontSystem.UseKernings = value; }
        }

        public int? DefaultCharacter {
            get { return _fontSystem.DefaultCharacter; }

            set { _fontSystem.DefaultCharacter = value; }
        }

        public event EventHandler CurrentAtlasFull {
            add { _fontSystem.CurrentAtlasFull += value; }

            remove { _fontSystem.CurrentAtlasFull -= value; }
        }

        DynamicSpriteFont(byte[] ttf, int defaultSize, int textureWidth, int textureHeight, int blur, int stroke) {
            _fontSystem = new FontSystem(textureWidth, textureHeight, blur, stroke) {
                FontSize = defaultSize
            };

            _fontSystem.AddFontMem(ttf);
        }

        public void Dispose() {
            _fontSystem?.Dispose();
        }

        public float DrawString(SpriteBatch batch, StringBuilder text, Vector2 pos, Color color) {
            return DrawString(batch, text, pos, 0f, color, Vector2.Zero, Vector2.One);
        }

        public float DrawString(SpriteBatch batch, StringBuilder text, Vector2 pos, float depth, Color color,
          Vector2 origin, Vector2 scale) {
            var result = _fontSystem.DrawText(batch, pos.X, pos.Y, text, depth, color, origin, scale.X, scale.Y);

            return result;
        }

        public float DrawString(SpriteBatch batch, string text, Vector2 pos, Color color) {
            return DrawString(batch, text, pos, 0f, color, Vector2.Zero, Vector2.One);
        }

        public float DrawString(SpriteBatch batch, string text, Vector2 pos, float depth, Color color, Vector2 origin,
          Vector2 scale) {
            return _fontSystem.DrawText(batch, pos.X, pos.Y, text, depth, color, origin, scale.X, scale.Y);
        }

        public void AddTtf(byte[] ttf) {
            _fontSystem.AddFontMem(ttf);
        }

        public void AddTtf(Stream ttfStream) {
            AddTtf(ttfStream.ToByteArray());
        }

        public Vector2 MeasureString(string text) {
            var bounds = new Bounds();
            _fontSystem.TextBounds(0, 0, text, ref bounds);

            return new Vector2(bounds.X2, bounds.Y2);
        }

        public Vector2 MeasureString(StringBuilder text) {
            var bounds = new Bounds();
            _fontSystem.TextBounds(0, 0, text, ref bounds);

            return new Vector2(bounds.X2, bounds.Y2);
        }

        public Rectangle GetTextBounds(Vector2 position, string text) {
            var bounds = new Bounds();
            _fontSystem.TextBounds(position.X, position.Y, text, ref bounds);

            return new Rectangle((int)bounds.X, (int)bounds.Y, (int)(bounds.X2 - bounds.X), (int)(bounds.Y2 - bounds.Y));
        }

        public void Reset(int width, int height) {
            _fontSystem.Reset(width, height);
        }

        public void Reset() {
            _fontSystem.Reset();
        }

        public static DynamicSpriteFont FromTtf(byte[] ttf, int defaultSize, int textureWidth = 1024, int textureHeight = 1024, int blur = 0, int stroke = 0) {
            return new DynamicSpriteFont(ttf, defaultSize, textureWidth, textureHeight, blur, stroke);
        }

        public static DynamicSpriteFont FromTtf(Stream ttfStream, int defaultSize, int textureWidth = 1024, int textureHeight = 1024, int blur = 0, int stroke = 0) {
            return FromTtf(ttfStream.ToByteArray(), defaultSize, textureWidth, textureHeight, blur, stroke);
        }
    }
}
