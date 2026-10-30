using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SelectionExplainer {
    internal static class Theme {
        internal static readonly Color Ink = Color.FromArgb(35, 42, 54);
        internal static readonly Color Muted = Color.FromArgb(119, 127, 143);
        internal static readonly Color Line = Color.FromArgb(232, 235, 240);
        internal static readonly Color Soft = Color.FromArgb(247, 248, 250);
        internal static readonly Color Accent = Color.FromArgb(91, 81, 214);
        internal static GraphicsPath Round(RectangleF bounds, float radius) {
            var path = new GraphicsPath(); float d = radius * 2;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path;
        }
    }
    internal sealed class ActionButton : Button {
        internal string Symbol;
        internal bool Active;
        bool hovered;
        internal ActionButton(string symbol, string text) {
            Symbol = symbol; Text = text; FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            BackColor = Color.White; ForeColor = Theme.Muted; Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 9F); Size = new Size(String.IsNullOrEmpty(text) ? 30 : 72, 30);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            MouseEnter += delegate { hovered = true; Invalidate(); };
            MouseLeave += delegate { hovered = false; Invalidate(); };
        }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(Parent == null ? Color.White : Parent.BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (hovered || Active || Focused) using (var path = Theme.Round(new RectangleF(0, 0, Width - 1, Height - 1), 6))
                using (var brush = new SolidBrush(Active ? Color.FromArgb(239, 237, 255) : Theme.Soft)) e.Graphics.FillPath(brush, path);
            Color color = !Enabled ? Color.FromArgb(197, 201, 209) : Active ? Theme.Accent : hovered ? Theme.Ink : ForeColor;
            float x = String.IsNullOrEmpty(Text) ? (Width - 16) / 2F : 9, y = (Height - 16) / 2F;
            using (var pen = new Pen(color, 1.45F)) {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round; pen.LineJoin = LineJoin.Round;
                var state = e.Graphics.Save(); e.Graphics.TranslateTransform(x, y);
                switch (Symbol) {
                    case "close": e.Graphics.DrawLine(pen, 4, 4, 12, 12); e.Graphics.DrawLine(pen, 12, 4, 4, 12); break;
                    case "copy": e.Graphics.DrawRectangle(pen, 5, 5, 8, 9); e.Graphics.DrawLines(pen, new[] { new Point(3, 11), new Point(2, 11), new Point(2, 2), new Point(10, 2), new Point(10, 3) }); break;
                    case "retry": e.Graphics.DrawArc(pen, 2, 2, 12, 12, 30, 290); e.Graphics.DrawLines(pen, new[] { new Point(9, 2), new Point(13, 2), new Point(13, 6) }); break;
                    case "pin": e.Graphics.DrawPolygon(pen, new[] { new Point(5, 2), new Point(11, 2), new Point(10, 7), new Point(13, 10), new Point(3, 10), new Point(6, 7) }); e.Graphics.DrawLine(pen, 8, 10, 8, 15); break;
                    case "settings": e.Graphics.DrawEllipse(pen, 3, 3, 10, 10); e.Graphics.DrawEllipse(pen, 6, 6, 4, 4); for (int i = 0; i < 8; i++) { double a = i * Math.PI / 4; e.Graphics.DrawLine(pen, 8 + (float)Math.Cos(a) * 5, 8 + (float)Math.Sin(a) * 5, 8 + (float)Math.Cos(a) * 7, 8 + (float)Math.Sin(a) * 7); } break;
                    case "check": e.Graphics.DrawLines(pen, new[] { new Point(3, 8), new Point(6, 11), new Point(13, 4) }); break;
                }
                e.Graphics.Restore(state);
            }
            if (!String.IsNullOrEmpty(Text)) TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(31, 0, Width - 32, Height), color, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }
    internal sealed class QuotePanel : Panel {
        internal QuotePanel() { BackColor = Theme.Soft; DoubleBuffered = true; }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e); using (var pen = new Pen(Color.FromArgb(210, 207, 239), 3)) e.Graphics.DrawLine(pen, 0, 10, 0, Height - 10);
        }
    }
    public static class AnswerFormat {
        public static string Escape(string text) {
            var output = new StringBuilder();
            foreach (char c in text) {
                if (c == '\\' || c == '{' || c == '}') output.Append('\\').Append(c);
                else if (c > 127) output.Append("\\u").Append((short)c).Append('?');
                else if (c != '\r') output.Append(c);
            }
            return output.ToString();
        }
        static string Inline(string text) {
            var output = new StringBuilder(); int last = 0;
            foreach (Match match in Regex.Matches(text, @"\*\*(.+?)\*\*|`([^`]+)`")) {
                output.Append(Escape(text.Substring(last, match.Index - last)));
                output.Append(match.Groups[1].Success ? "{\\b " + Escape(match.Groups[1].Value) + "}" : "{\\f1\\fs20\\cf2 " + Escape(match.Groups[2].Value) + "}");
                last = match.Index + match.Length;
            }
            return output.Append(Escape(text.Substring(last))).ToString();
        }
        public static string ToRtf(string text) {
            var output = new StringBuilder(@"{\rtf1\ansi\ansicpg936\deff0{\fonttbl{\f0\fnil\fcharset134 Microsoft YaHei UI;}{\f1\fmodern\fcharset0 Consolas;}}{\colortbl ;\red35\green42\blue54;\red91\green81\blue214;\red247\green248\blue250;}\viewkind4\uc1\lang2052\langfe2052\f0\af0\fs22\cf1 ");
            bool code = false;
            foreach (string raw in (text ?? "").Replace("\r\n", "\n").Split('\n')) {
                string line = raw.TrimEnd();
                if (line.TrimStart().StartsWith("```")) { code = !code; continue; }
                if (code) { output.Append("\\pard\\f1\\fs20\\li140\\sa70\\highlight3 ").Append(Escape(line)).Append("\\highlight0\\par "); continue; }
                if (String.IsNullOrWhiteSpace(line)) continue;
                output.Append("\\pard\\f0\\af0\\fs22\\li0\\sa100\\sl280\\slmult1 ");
                var heading = Regex.Match(line, @"^#{1,6}\s+(.+)$");
                if (heading.Success) output.Append("\\b\\fs24 ").Append(Inline(heading.Groups[1].Value)).Append("\\b0\\fs22");
                else if (Regex.IsMatch(line, @"^\s*[-*]\s+")) output.Append("\\li200\\fi-160 ").Append(Escape("• ")).Append(Inline(Regex.Replace(line, @"^\s*[-*]\s+", "")));
                else output.Append(Inline(line));
                output.Append("\\par ");
            }
            return output.Append('}').ToString();
        }
    }
}
