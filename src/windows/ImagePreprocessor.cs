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

            int w = src.Width;
            int h = src.Height;

            // 1. Detect if image has a dark background (dark themes, terminals)
            bool isDark = IsDarkBackground(src);

            // 2. Intelligent scaling factor:
            // High upscale for small text snippets; 1:1 scale for high-res book pages / full screenshots
            float scale = 1.0f;
            if (h < 120 || w < 250) scale = 2.0f;
            else if (h < 300) scale = 1.4f;

            int pad = 24;
            int targetW = (int)(w * scale) + pad * 2;
            int targetH = (int)(h * scale) + pad * 2;

            // 3. Process pixel values preserving smooth anti-aliased gradients
            // If dark background: invert so background becomes white and text becomes dark
            // If light background: preserve natural colors with soft contrast boost
            Bitmap processed = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData srcData = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = processed.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* srcPtr = (byte*)srcData.Scan0;
                byte* dstPtr = (byte*)dstData.Scan0;
                int srcStride = srcData.Stride;
                int dstStride = dstData.Stride;

                for (int y = 0; y < h; y++)
                {
                    byte* sRow = srcPtr + y * srcStride;
                    byte* dRow = dstPtr + y * dstStride;

                    for (int x = 0; x < w; x++)
                    {
                        byte b = sRow[x * 4];
                        byte g = sRow[x * 4 + 1];
                        byte r = sRow[x * 4 + 2];

                        if (isDark)
                        {
                            // Invert colors smoothly for dark mode
                            b = (byte)(255 - b);
                            g = (byte)(255 - g);
                            r = (byte)(255 - r);
                        }

                        // Preserves natural RGB subpixels without destructive 1-bit thresholding!
                        dRow[x * 4] = b;
                        dRow[x * 4 + 1] = g;
                        dRow[x * 4 + 2] = r;
                        dRow[x * 4 + 3] = 255;
                    }
                }
            }
            src.UnlockBits(srcData);
            processed.UnlockBits(dstData);

            // 4. Composite with clean border padding and high-quality bicubic interpolation
            Bitmap output = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(output))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                g.DrawImage(processed, new Rectangle(pad, pad, (int)(w * scale), (int)(h * scale)),
                    0, 0, w, h, GraphicsUnit.Pixel);
            }
            processed.Dispose();

            return output;
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
