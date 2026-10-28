using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SteamIdleManager
{
    internal sealed class Palette
    {
        public Color Background, Surface, Input, Text, Muted, Border, Accent, AccentText, Selection, Error;
        public static Palette Create(bool dark)
        {
            return dark ? new Palette {
                Background = Color.FromArgb(15, 19, 27), Surface = Color.FromArgb(23, 29, 39),
                Input = Color.FromArgb(17, 23, 32), Text = Color.FromArgb(235, 240, 248),
                Muted = Color.FromArgb(155, 170, 190), Border = Color.FromArgb(47, 59, 75),
                Accent = Color.FromArgb(111, 225, 199), AccentText = Color.FromArgb(14, 42, 36),
                Selection = Color.FromArgb(35, 65, 64), Error = Color.FromArgb(255, 159, 159)
            } : new Palette {
                Background = Color.FromArgb(242, 245, 249), Surface = Color.White,
                Input = Color.FromArgb(247, 249, 252), Text = Color.FromArgb(25, 36, 49),
                Muted = Color.FromArgb(84, 101, 122), Border = Color.FromArgb(211, 220, 231),
                Accent = Color.FromArgb(14, 114, 95), AccentText = Color.White,
                Selection = Color.FromArgb(218, 242, 235), Error = Color.FromArgb(174, 42, 42)
            };
        }
    }
    internal static class Shapes
    {
        internal static GraphicsPath Round(Rectangle rectangle, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, diameter, diameter, 180, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Top, diameter, diameter, 270, 90);
            path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
    internal sealed class Card : Panel
    {
        public Color Fill, Stroke;
        public Card() { DoubleBuffered = true; BackColor = Color.Transparent; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = Shapes.Round(new Rectangle(0, 0, Width - 1, Height - 1), Math.Max(8, DeviceDpi / 12)))
            using (var fill = new SolidBrush(Fill))
            using (var pen = new Pen(Stroke)) { e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(pen, path); }
        }
    }
    internal sealed class ModernButton : Button
    {
        public Color Fill, Ink, Stroke;
        private bool hover;
        public ModernButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var card = Parent as Card;
            e.Graphics.Clear(card != null ? card.Fill : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var background = Enabled && hover ? ControlPaint.Light(Fill, .12f) : Fill;
            using (var path = Shapes.Round(new Rectangle(1, 1, Width - 3, Height - 3), Math.Max(6, DeviceDpi / 16)))
            using (var fill = new SolidBrush(background))
            using (var pen = new Pen(Stroke)) { e.Graphics.FillPath(fill, path); e.Graphics.DrawPath(pen, path); }
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Enabled ? Ink : SystemColors.GrayText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -6, -6), Ink, background);
        }
    }
}
