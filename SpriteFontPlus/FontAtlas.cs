﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SpriteFontPlus {
    unsafe class FontAtlas {
        byte[] _byteBuffer;
        Color[] _colorBuffer;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int NodesNumber { get; private set; }

        public FontAtlasNode[] Nodes { get; private set; }

        public Texture2D Texture { get; set; }

        public FontAtlas(int w, int h, int count) {
            Width = w;
            Height = h;
            Nodes = new FontAtlasNode[count];
            Nodes[0].X = 0;
            Nodes[0].Y = 0;
            Nodes[0].Width = w;
            NodesNumber++;
        }

        public void InsertNode(int idx, int x, int y, int w) {
            if (NodesNumber + 1 > Nodes.Length) {
                var oldNodes = Nodes;
                var newLength = Nodes.Length == 0 ? 8 : Nodes.Length * 2;
                Nodes = new FontAtlasNode[newLength];
                for (var i = 0; i < oldNodes.Length; ++i) {
                    Nodes[i] = oldNodes[i];
                }
            }

            for (var i = NodesNumber; i > idx; i--)
                Nodes[i] = Nodes[i - 1];
            Nodes[idx].X = x;
            Nodes[idx].Y = y;
            Nodes[idx].Width = w;
            NodesNumber++;
        }

        public void RemoveNode(int idx) {
            if (NodesNumber == 0)
                return;
            for (var i = idx; i < NodesNumber - 1; i++)
                Nodes[i] = Nodes[i + 1];
            NodesNumber--;
        }

        public void Reset(int w, int h) {
            Width = w;
            Height = h;
            NodesNumber = 0;
            Nodes[0].X = 0;
            Nodes[0].Y = 0;
            Nodes[0].Width = w;
            NodesNumber++;
        }

        public bool AddSkylineLevel(int idx, int x, int y, int w, int h) {
            InsertNode(idx, x, y + h, w);
            for (var i = idx + 1; i < NodesNumber; i++)
                if (Nodes[i].X < Nodes[i - 1].X + Nodes[i - 1].Width) {
                    var shrink = Nodes[i - 1].X + Nodes[i - 1].Width - Nodes[i].X;
                    Nodes[i].X += shrink;
                    Nodes[i].Width -= shrink;
                    if (Nodes[i].Width <= 0) {
                        RemoveNode(i);
                        i--;
                    }
                    else {
                        break;
                    }
                }
                else {
                    break;
                }

            for (var i = 0; i < NodesNumber - 1; i++)
                if (Nodes[i].Y == Nodes[i + 1].Y) {
                    Nodes[i].Width += Nodes[i + 1].Width;
                    RemoveNode(i + 1);
                    i--;
                }

            return true;
        }

        public int RectFits(int i, int w, int h) {
            var x = Nodes[i].X;
            var y = Nodes[i].Y;
            if (x + w > Width)
                return -1;
            var spaceLeft = w;
            while (spaceLeft > 0) {
                if (i == NodesNumber)
                    return -1;
                y = Math.Max(y, Nodes[i].Y);
                if (y + h > Height)
                    return -1;
                spaceLeft -= Nodes[i].Width;
                ++i;
            }

            return y;
        }

        public bool AddRect(int rw, int rh, ref int rx, ref int ry) {
            var besth = Height;
            var bestw = Width;
            var besti = -1;
            var bestx = -1;
            var besty = -1;
            for (var i = 0; i < NodesNumber; i++) {
                var y = RectFits(i, rw, rh);
                if (y != -1)
                    if (y + rh < besth || y + rh == besth && Nodes[i].Width < bestw) {
                        besti = i;
                        bestw = Nodes[i].Width;
                        besth = y + rh;
                        bestx = Nodes[i].X;
                        besty = y;
                    }
            }

            if (besti == -1)
                return false;
            if (!AddSkylineLevel(besti, bestx, besty, rw, rh))
                return false;

            rx = bestx;
            ry = besty;
            return true;
        }

        public void RenderGlyph(GraphicsDevice device, FontGlyph glyph, int blurAmount, int strokeAmount) {
            var pad = Math.Max(FontGlyph.PadFromBlur(blurAmount), FontGlyph.PadFromBlur(strokeAmount));

            // Render glyph to byte buffer
            var bufferSize = glyph.Bounds.Width * glyph.Bounds.Height;
            var buffer = _byteBuffer;

            if ((buffer == null) || (buffer.Length < bufferSize)) {
                buffer = new byte[bufferSize];
                _byteBuffer = buffer;
            }
            Array.Clear(buffer, 0, bufferSize);

            var g = glyph.Index;
            var colorSize = glyph.Bounds.Width * glyph.Bounds.Height;
            var colorBuffer = _colorBuffer;
            if ((colorBuffer == null) || (colorBuffer.Length < colorSize)) {
                colorBuffer = new Color[colorSize];
                _colorBuffer = colorBuffer;
            }

            fixed (byte* dst = &buffer[pad + pad * glyph.Bounds.Width]) {
                glyph.Font.RenderGlyphBitmap(dst,
                    glyph.Bounds.Width - pad * 2,
                    glyph.Bounds.Height - pad * 2,
                    glyph.Bounds.Width,
                    g);
            }

            if (strokeAmount > 0) {
                var width = glyph.Bounds.Width;
                var top = width * strokeAmount;
                var bottom = (glyph.Bounds.Height - strokeAmount) * glyph.Bounds.Width;
                var right = glyph.Bounds.Width - strokeAmount;
                var left = strokeAmount;

                byte d;
                for (var i = 0; i < colorSize; ++i) {
                    var col = buffer[i];
                    var black = 0;
                    if (col == 255) {
                        colorBuffer[i].R = colorBuffer[i].G = colorBuffer[i].B = colorBuffer[i].A = 255;
                        continue;
                    }

                    if (i >= top)
                        black = buffer[i - top];
                    if (i < bottom) {
                        d = buffer[i + top];
                        black = ((255 - d) * black + 255 * d) / 255;
                    }
                    if (i % width >= left) {
                        d = buffer[i - strokeAmount];
                        black = ((255 - d) * black + 255 * d) / 255;
                    }
                    if (i % width < right) {
                        d = buffer[i + strokeAmount];
                        black = ((255 - d) * black + 255 * d) / 255;
                    }

                    if (black == 0) {
                        if (col == 0) {
                            colorBuffer[i].R = colorBuffer[i].G = colorBuffer[i].B = colorBuffer[i].A = 0; //black transparency to suit stroke
                            continue;
                        }
#if PREMULTIPLIEDALPHA
                        colorBuffer[i].R = colorBuffer[i].G = colorBuffer[i].B = colorBuffer[i].A = col;
#else
                        colorBuffer[i].R = colorBuffer[i].G = colorBuffer[i].B = 255;
                        colorBuffer[i].A = col;
#endif
                    }
                    else {
                        if (col == 0) {
                            colorBuffer[i].R = colorBuffer[i].G = colorBuffer[i].B = 0;
                            colorBuffer[i].A = (byte)black;
                            continue;
                        }

#if PREMULTIPLIEDALPHA
                        var alpha = ((255 - col) * black + 255 * col) / 255;
                        colorBuffer[i].R = colorBuffer[i].G = colorBuffer[i].B = (byte)((alpha * col) / 255);
                        colorBuffer[i].A = (byte)alpha;
#else
                        colorBuffer[i].R = colorBuffer[i].G = colorBuffer[i].B = col;
                        colorBuffer[i].A = (byte)(((255 - col) * black + 255 * col) / 255);
#endif
                    }
                }
            }
            else {
                if (blurAmount > 0) {
                    fixed (byte* bdst = &buffer[0]) {
                        Blur(bdst, glyph.Bounds.Width, glyph.Bounds.Height, glyph.Bounds.Width, blurAmount);
                    }
                }

                for (var i = 0; i < colorSize; ++i) {
                    var c = buffer[i];
#if PREMULTIPLIEDALPHA
                    colorBuffer[i].R = colorBuffer[i].G = colorBuffer[i].B = colorBuffer[i].A = c;
#else
                    colorBuffer[i].R = colorBuffer[i].G = colorBuffer[i].B = 255;
                    colorBuffer[i].A = c;
#endif
                }
            }

            // Write to texture
            if (Texture == null) {
                Texture = new Texture2D(device, Width, Height);
            }
#if TEXTURESETDATAEXT
            fixed (Color* p = colorBuffer)
#if FNA
                Texture.SetDataPointerEXT(0, glyph.Bounds, (IntPtr)p, colorSize * sizeof(Color));
#else
                Texture.SetDataEXT(0, 0, glyph.Bounds, (IntPtr)p, colorSize * sizeof(Color));
#endif
#elif XNA
          Texture.GraphicsDevice.Textures[0] = null;
          Texture.SetData(0, glyph.Bounds, colorBuffer, 0, colorSize);
#else
            Texture.SetData(0, 0, glyph.Bounds, colorBuffer, 0, colorSize);
#endif
        }

        void Blur(byte* dst, int w, int h, int dstStride, int blur) {
            int alpha;
            float sigma;
            if (blur < 1)
                return;
            sigma = blur * 0.57735f;
            alpha = (int)((1 << 16) * (1.0f - Math.Exp(-2.3f / (sigma + 1.0f))));
            BlurRows(dst, w, h, dstStride, alpha);
            BlurCols(dst, w, h, dstStride, alpha);
            BlurRows(dst, w, h, dstStride, alpha);
            BlurCols(dst, w, h, dstStride, alpha);
        }

        static void BlurCols(byte* dst, int w, int h, int dstStride, int alpha) {
            int x;
            int y;
            for (y = 0; y < h; y++) {
                var z = 0;
                for (x = 1; x < w; x++) {
                    z += (alpha * ((dst[x] << 7) - z)) >> 16;
                    dst[x] = (byte)(z >> 7);
                }

                dst[w - 1] = 0;
                z = 0;
                for (x = w - 2; x >= 0; x--) {
                    z += (alpha * ((dst[x] << 7) - z)) >> 16;
                    dst[x] = (byte)(z >> 7);
                }

                dst[0] = 0;
                dst += dstStride;
            }
        }

        static void BlurRows(byte* dst, int w, int h, int dstStride, int alpha) {
            int x;
            int y;
            for (x = 0; x < w; x++) {
                var z = 0;
                for (y = dstStride; y < h * dstStride; y += dstStride) {
                    z += (alpha * ((dst[y] << 7) - z)) >> 16;
                    dst[y] = (byte)(z >> 7);
                }

                dst[(h - 1) * dstStride] = 0;
                z = 0;
                for (y = (h - 2) * dstStride; y >= 0; y -= dstStride) {
                    z += (alpha * ((dst[y] << 7) - z)) >> 16;
                    dst[y] = (byte)(z >> 7);
                }

                dst[0] = 0;
                dst++;
            }
        }
    }
}
