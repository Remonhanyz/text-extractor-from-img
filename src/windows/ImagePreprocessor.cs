using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ArabicTextExtractor
{
    public static class ImagePreprocessor
    {
        public static Bitmap Preprocess(Bitmap src)
        {
            if (src == null) return null;

            // 1. Detect if image has a dark background (common in dark mode apps / editors)
            bool isDark = IsDarkBackground(src);

            // 2. High-quality upscale (screen text is typically 96 DPI, ~12-14px; 2.2x brings it to ~30-35px)
            float scale = 2.2f;
            int padding = 24; // 24px white margin around borders prevents glyph edge clipping

            int targetW = (int)(src.Width * scale) + (padding * 2);
            int targetH = (int)(src.Height * scale) + (padding * 2);

            Bitmap scaledBmp = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(scaledBmp))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                Rectangle destRect = new Rectangle(padding, padding, (int)(src.Width * scale), (int)(src.Height * scale));

                if (isDark)
                {
                    // Invert RGB: Dark background becomes white, white text becomes dark
                    ColorMatrix invertMatrix = new ColorMatrix(new float[][] {
                        new float[] {-1, 0, 0, 0, 0},
                        new float[] {0, -1, 0, 0, 0},
                        new float[] {0, 0, -1, 0, 0},
                        new float[] {0, 0, 0, 1, 0},
                        new float[] {1, 1, 1, 0, 1}
                    });
                    using (ImageAttributes ia = new ImageAttributes())
                    {
                        ia.SetColorMatrix(invertMatrix);
                        g.DrawImage(src, destRect, 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                    }
                }
                else
                {
                    g.DrawImage(src, destRect, 0, 0, src.Width, src.Height, GraphicsUnit.Pixel);
                }
            }

            // 3. ClearType Fringe Suppression & Grayscale Conversion
            // Eliminates subpixel chromatic fringes and sharpens Arabic dots
            Bitmap grayBmp = new Bitmap(scaledBmp.Width, scaledBmp.Height, PixelFormat.Format32bppArgb);
            using (Graphics gGray = Graphics.FromImage(grayBmp))
            {
                float c = 1.15f; // Mild contrast enhancement to emphasize tiny Arabic dots
                float t = (1.0f - c) / 2.0f;
                ColorMatrix grayMatrix = new ColorMatrix(new float[][] {
                    new float[] {0.299f * c, 0.299f * c, 0.299f * c, 0, 0},
                    new float[] {0.587f * c, 0.587f * c, 0.587f * c, 0, 0},
                    new float[] {0.114f * c, 0.114f * c, 0.114f * c, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {t, t, t, 0, 1}
                });
                using (ImageAttributes ia = new ImageAttributes())
                {
                    ia.SetColorMatrix(grayMatrix);
                    gGray.DrawImage(scaledBmp, new Rectangle(0, 0, grayBmp.Width, grayBmp.Height),
                        0, 0, scaledBmp.Width, scaledBmp.Height, GraphicsUnit.Pixel, ia);
                }
            }
            scaledBmp.Dispose();

            return grayBmp;
        }

        private static bool IsDarkBackground(Bitmap bmp)
        {
            try
            {
                long totalLum = 0;
                int count = 0;
                int stepX = Math.Max(1, bmp.Width / 12);
                int stepY = Math.Max(1, bmp.Height / 12);

                for (int x = 0; x < bmp.Width; x += stepX)
                {
                    Color top = bmp.GetPixel(x, 0);
                    Color bot = bmp.GetPixel(x, bmp.Height - 1);
                    totalLum += (int)(0.299 * top.R + 0.587 * top.G + 0.114 * top.B);
                    totalLum += (int)(0.299 * bot.R + 0.587 * bot.G + 0.114 * bot.B);
                    count += 2;
                }

                for (int y = 0; y < bmp.Height; y += stepY)
                {
                    Color left = bmp.GetPixel(0, y);
                    Color right = bmp.GetPixel(bmp.Width - 1, y);
                    totalLum += (int)(0.299 * left.R + 0.587 * left.G + 0.114 * left.B);
                    totalLum += (int)(0.299 * right.R + 0.587 * right.G + 0.114 * right.B);
                    count += 2;
                }

                if (count == 0) return false;
                double avgLum = (double)totalLum / count;
                return avgLum < 128.0;
            }
            catch
            {
                return false;
            }
        }
    }
}
