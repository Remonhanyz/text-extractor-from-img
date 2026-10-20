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

            // 1. Detect if image has a dark background (common in dark themes / code editors / terminals)
            bool isDark = IsDarkBackground(src);

            // 2. Convert to grayscale array (with inversion if dark background)
            byte[,] gray = new byte[w, h];
            BitmapData srcData = src.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* ptr = (byte*)srcData.Scan0;
                int stride = srcData.Stride;
                for (int y = 0; y < h; y++)
                {
                    byte* row = ptr + y * stride;
                    for (int x = 0; x < w; x++)
                    {
                        int b = row[x * 4];
                        int g = row[x * 4 + 1];
                        int r = row[x * 4 + 2];
                        int lum = (int)(0.299 * r + 0.587 * g + 0.114 * b);
                        if (isDark) lum = 255 - lum;
                        gray[x, y] = (byte)lum;
                    }
                }
            }
            src.UnlockBits(srcData);

            // 3. Fast integral image for local background estimation (Sauvola-based document cleaning)
            // Removes page bleed-through, spine shadows, and uneven lighting on printed documents
            int radius = Math.Max(12, Math.Min(24, Math.Min(w, h) / 25));
            long[,] integral = new long[w + 1, h + 1];
            for (int y = 0; y < h; y++)
            {
                long sum = 0;
                for (int x = 0; x < w; x++)
                {
                    sum += gray[x, y];
                    integral[x + 1, y + 1] = integral[x + 1, y] + sum;
                }
            }

            // 4. Adaptive binarization to temp bitmap
            Bitmap tempBmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData dstData = tempBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)dstData.Scan0;
                int stride = dstData.Stride;

                for (int y = 0; y < h; y++)
                {
                    byte* row = ptr + y * stride;
                    int y1 = Math.Max(0, y - radius);
                    int y2 = Math.Min(h - 1, y + radius);

                    for (int x = 0; x < w; x++)
                    {
                        int x1 = Math.Max(0, x - radius);
                        int x2 = Math.Min(w - 1, x + radius);

                        int count = (x2 - x1 + 1) * (y2 - y1 + 1);
                        long sum = integral[x2 + 1, y2 + 1] - integral[x1, y2 + 1] - integral[x2 + 1, y1] + integral[x1, y1];
                        int mean = (int)(sum / count);

                        int val = gray[x, y];
                        byte outByte = 255; // White background

                        // Mask out right spine dark artifact if near right edge on book scans
                        bool isExtremeSpine = (x > w - 8);
                        if (!isExtremeSpine && val < (mean - 15))
                        {
                            outByte = 0; // Black text
                        }

                        row[x * 4] = outByte;
                        row[x * 4 + 1] = outByte;
                        row[x * 4 + 2] = outByte;
                        row[x * 4 + 3] = 255;
                    }
                }
            }
            tempBmp.UnlockBits(dstData);

            // 5. High-quality upscale & clean margin padding (prevents character boundary clipping)
            float scale = 1.6f;
            if (h < 200) scale = 2.2f; // Small screen snips get higher scaling
            int pad = 24;

            int targetW = (int)(w * scale) + pad * 2;
            int targetH = (int)(h * scale) + pad * 2;

            Bitmap output = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(output))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                g.DrawImage(tempBmp, new Rectangle(pad, pad, (int)(w * scale), (int)(h * scale)),
                    0, 0, w, h, GraphicsUnit.Pixel);
            }
            tempBmp.Dispose();

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
