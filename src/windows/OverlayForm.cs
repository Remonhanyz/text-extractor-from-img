using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ArabicTextExtractor
{
    public class OverlayForm : Form
    {
        private Bitmap backgroundScreenshot;
        private Rectangle virtualBounds;
        private SnipperContext context;
        private Point startPoint;
        private Rectangle selectionRect;
        private bool isSelecting = false;

        public OverlayForm(Bitmap screenshot, Rectangle bounds, SnipperContext ctx)
        {
            backgroundScreenshot = screenshot;
            virtualBounds = bounds;
            context = ctx;

            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = bounds;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Cross;
            this.KeyPreview = true;

            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }
            };
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isSelecting = true;
                startPoint = e.Location;
                selectionRect = new Rectangle(e.Location, new Size(0, 0));
                Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                Close();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isSelecting)
            {
                int x = Math.Min(startPoint.X, e.X);
                int y = Math.Min(startPoint.Y, e.Y);
                int width = Math.Abs(startPoint.X - e.X);
                int height = Math.Abs(startPoint.Y - e.Y);
                selectionRect = new Rectangle(x, y, width, height);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (isSelecting && e.Button == MouseButtons.Left)
            {
                isSelecting = false;
                if (selectionRect.Width > 5 && selectionRect.Height > 5)
                {
                    Bitmap cropped = new Bitmap(selectionRect.Width, selectionRect.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(cropped))
                    {
                        g.DrawImage(backgroundScreenshot,
                            new Rectangle(0, 0, selectionRect.Width, selectionRect.Height),
                            selectionRect,
                            GraphicsUnit.Pixel);
                    }
                    this.Hide();
                    context.ProcessImage(cropped);
                }
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            // Draw full background screenshot
            g.DrawImage(backgroundScreenshot, 0, 0);

            // Darken entire screen with semi-transparent layer
            using (SolidBrush darkBrush = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
            {
                g.FillRectangle(darkBrush, 0, 0, Width, Height);
            }

            if (isSelecting && selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                // Clear selection cutout: show original bright image inside selection
                g.DrawImage(backgroundScreenshot, selectionRect, selectionRect, GraphicsUnit.Pixel);

                // Draw stylish blue border around selection
                using (Pen borderPen = new Pen(Color.FromArgb(0, 153, 255), 2))
                {
                    borderPen.Alignment = PenAlignment.Outset;
                    g.DrawRectangle(borderPen, selectionRect);
                }

                // Show dimension pill badge
                string dimText = string.Format("{0} x {1}", selectionRect.Width, selectionRect.Height);
                using (Font font = new Font("Segoe UI", 9, FontStyle.Bold))
                using (SolidBrush badgeBrush = new SolidBrush(Color.FromArgb(220, 0, 0, 0)))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    SizeF textSize = g.MeasureString(dimText, font);
                    int badgeX = selectionRect.Left;
                    int badgeY = selectionRect.Bottom + 6;
                    if (badgeY + textSize.Height > Height)
                    {
                        badgeY = selectionRect.Top - (int)textSize.Height - 8;
                    }
                    Rectangle badgeRect = new Rectangle(badgeX, badgeY, (int)textSize.Width + 10, (int)textSize.Height + 4);
                    g.FillRectangle(badgeBrush, badgeRect);
                    g.DrawString(dimText, font, textBrush, badgeX + 5, badgeY + 2);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && backgroundScreenshot != null)
            {
                backgroundScreenshot.Dispose();
                backgroundScreenshot = null;
            }
            base.Dispose(disposing);
        }
    }
}
