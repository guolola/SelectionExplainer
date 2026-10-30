using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SelectionExplainer {
    public sealed class ResultForm : Form {
        readonly Label title = new Label(), source = new Label(), sourceTitle = new Label(), status = new Label(), hint = new Label();
        readonly RichTextBox answer = new RichTextBox();
        readonly QuotePanel quote = new QuotePanel();
        readonly ActionButton pin = new ActionButton("pin", ""), close = new ActionButton("close", "");
        readonly ActionButton copy = new ActionButton("copy", "复制"), retry = new ActionButton("retry", "重试"), settings = new ActionButton("settings", "");
        readonly ToolTip tips = new ToolTip();
        readonly Font bodyFont = new Font("Microsoft YaHei UI", 11);
        string latest = "";
        bool adjusting;
        float scale = 1;
        int contentHeight = 90;
        public bool Pinned { get { return pin.Active; } }
        public event Action Dismissed;
        public event Action RetryRequested;
        public event Action SettingsRequested;
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
        [StructLayout(LayoutKind.Sequential)] struct ScrollPoint { public int X, Y; }
        [DllImport("user32.dll", EntryPoint="SendMessage")] static extern IntPtr ScrollMessage(IntPtr window, int message, IntPtr wParam, ref ScrollPoint point);
        int P(int value) { return (int)Math.Round(value * scale); }
        public ResultForm() {
            Text = "划词解释"; BackColor = Color.White; ForeColor = Theme.Ink;
            Font = new Font("Microsoft YaHei UI", 10); FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false; TopMost = true; DoubleBuffered = true;
            using (var graphics = CreateGraphics()) scale = graphics.DpiX / 96F;
            Size = new Size(P(440), P(310)); MinimumSize = new Size(P(360), P(255));
            title.Text = "划词解释"; title.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);
            title.ForeColor = Theme.Ink; title.TextAlign = ContentAlignment.MiddleLeft; title.Cursor = Cursors.SizeAll;
            title.MouseDown += DragWindow;
            sourceTitle.Text = "选中的内容"; sourceTitle.Font = new Font(Font.FontFamily, 8.25F); sourceTitle.ForeColor = Theme.Muted;
            source.Font = new Font(Font.FontFamily, 9.25F); source.ForeColor = Color.FromArgb(94, 103, 120); source.AutoEllipsis = true;
            quote.Controls.Add(source); quote.Controls.Add(sourceTitle);
            answer.BorderStyle = BorderStyle.None; answer.ReadOnly = true; answer.BackColor = Color.White;
            answer.ForeColor = Theme.Ink; answer.Font = bodyFont; answer.DetectUrls = false;
            answer.ScrollBars = RichTextBoxScrollBars.Vertical; answer.AccessibleName = "解释内容";
            answer.ContentsResized += delegate(object sender, ContentsResizedEventArgs e) { contentHeight = e.NewRectangle.Height; FitHeight(); };
            status.Font = new Font(Font.FontFamily, 8.5F); status.ForeColor = Theme.Muted; status.TextAlign = ContentAlignment.MiddleLeft; status.AutoEllipsis = true;
            hint.Font = new Font(Font.FontFamily, 8F); hint.ForeColor = Theme.Muted; hint.TextAlign = ContentAlignment.MiddleRight; hint.Text = "点外部收起";
            pin.AccessibleName = "固定浮窗"; close.AccessibleName = "关闭浮窗"; settings.AccessibleName = "打开设置";
            tips.SetToolTip(pin, "固定后，点击外部不会关闭"); tips.SetToolTip(close, "关闭 · Esc"); tips.SetToolTip(settings, "模型与设置");
            pin.Click += delegate { pin.Active = !pin.Active; pin.Invalidate(); hint.Text = Pinned ? "已固定" : "点外部收起"; };
            close.Click += delegate { Close(); };
            copy.Click += delegate {
                try { if (!String.IsNullOrWhiteSpace(answer.Text)) { Clipboard.SetText(answer.Text.TrimEnd()); copy.Symbol = "check"; copy.Text = "已复制"; copy.Invalidate(); } }
                catch (ExternalException) { SetStatus("剪贴板正忙，请再试一次。"); }
            };
            retry.Click += delegate { if (RetryRequested != null) RetryRequested(); };
            settings.Click += delegate { if (SettingsRequested != null) SettingsRequested(); };
            Controls.AddRange(new Control[] { title, quote, answer, status, hint, pin, close, copy, retry, settings });
            KeyPreview = true; KeyDown += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape) Close(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; pin.Active = false; pin.Invalidate(); hint.Text = "点外部收起"; Hide(); if (Dismissed != null) Dismissed(); } };
            Arrange();
        }
        protected override CreateParams CreateParams { get { var value = base.CreateParams; value.ClassStyle |= 0x20000; return value; } }
        protected override bool ShowWithoutActivation { get { return true; } }
        void DragWindow(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero); } }
        protected override void WndProc(ref Message message) {
            base.WndProc(ref message);
            if (message.Msg == 0x84 && message.Result.ToInt32() == 1) {
                long value = message.LParam.ToInt64(); var point = PointToClient(new Point((short)(value & 0xffff), (short)((value >> 16) & 0xffff)));
                if (point.X > ClientSize.Width - P(13) && point.Y > ClientSize.Height - P(13)) message.Result = new IntPtr(17);
            }
        }
        protected override void OnResize(EventArgs e) { base.OnResize(e); Arrange(); }
        void Arrange() {
            if (title == null || Controls.Count == 0 || Width <= 0) return;
            int w = ClientSize.Width, h = ClientSize.Height;
            title.SetBounds(P(22), P(10), w - P(108), P(32));
            pin.SetBounds(w - P(83), P(11), P(30), P(30)); close.SetBounds(w - P(48), P(11), P(30), P(30));
            quote.SetBounds(P(20), P(54), w - P(40), P(74));
            sourceTitle.SetBounds(P(13), P(9), quote.Width - P(26), P(18));
            source.SetBounds(P(13), P(30), quote.Width - P(26), P(35));
            status.SetBounds(P(22), P(140), w - P(44), P(25));
            answer.SetBounds(P(22), P(171), w - P(44), Math.Max(P(28), h - P(230)));
            copy.SetBounds(P(14), h - P(42), P(84), P(30)); retry.SetBounds(P(100), h - P(42), P(72), P(30));
            settings.SetBounds(P(177), h - P(42), P(30), P(30)); hint.SetBounds(w - P(142), h - P(42), P(116), P(30));
            using (var path = Theme.Round(new RectangleF(0, 0, w, h), P(12))) { var previous = Region; Region = new Region(path); if (previous != null) previous.Dispose(); }
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e); e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = Theme.Round(new RectangleF(0.5F, 0.5F, Width - 1, Height - 1), P(12))) using (var pen = new Pen(Theme.Line)) e.Graphics.DrawPath(pen, path);
            using (var pen = new Pen(Theme.Line)) e.Graphics.DrawLine(pen, P(20), Height - P(51), Width - P(20), Height - P(51));
            using (var pen = new Pen(Color.FromArgb(207, 211, 220))) { e.Graphics.DrawLine(pen, Width - P(13), Height - P(6), Width - P(6), Height - P(13)); e.Graphics.DrawLine(pen, Width - P(9), Height - P(6), Width - P(6), Height - P(9)); }
        }
        void FitHeight() {
            if (adjusting || Pinned || contentHeight <= 0) return;
            adjusting = true;
            try { int target = Math.Max(P(285), Math.Min(P(580), contentHeight + P(238))); if (Math.Abs(Height - target) > P(16)) Height = target; }
            finally { adjusting = false; }
            if (Visible) ClampToScreen();
        }
        void ClampToScreen() { var area = Screen.FromPoint(Location).WorkingArea; Location = new Point(Math.Max(area.Left, Math.Min(Left, area.Right - Width)), Math.Max(area.Top, Math.Min(Top, area.Bottom - Height))); }
        public void Begin(string selected, Point point, bool show = true) {
            source.Text = selected.Replace("\r", "").Replace("\n", " ");
            latest = ""; contentHeight = P(60); answer.Clear(); answer.Font = bodyFont; answer.ForeColor = Theme.Ink;
            status.Text = "正在理解这段内容…"; status.ForeColor = Theme.Accent;
            copy.Enabled = false; copy.Symbol = "copy"; copy.Text = "复制"; retry.Enabled = false;
            if (!Pinned) Height = P(300);
            var area = Screen.FromPoint(point).WorkingArea;
            if (!Pinned) Location = new Point(Math.Max(area.Left, Math.Min(point.X + P(14), area.Right - Width)), Math.Max(area.Top, point.Y + Height + P(22) < area.Bottom ? point.Y + P(22) : point.Y - Height - P(16)));
            if (show) Show();
            Arrange();
        }
        public void SetAnswer(string text) {
            if (latest == text) return;
            int start = answer.SelectionStart, length = answer.SelectionLength; var scroll = new ScrollPoint();
            ScrollMessage(answer.Handle, 0x4DD, IntPtr.Zero, ref scroll);
            SendMessage(answer.Handle, 0xB, IntPtr.Zero, IntPtr.Zero);
            try {
                if (text.StartsWith(latest, StringComparison.Ordinal)) { answer.SelectionStart = answer.TextLength; answer.SelectionLength = 0; answer.AppendText(text.Substring(latest.Length)); }
                else answer.Text = text;
                latest = text;
                answer.Select(Math.Min(start, answer.TextLength), Math.Min(length, Math.Max(0, answer.TextLength - start)));
                ScrollMessage(answer.Handle, 0x4DE, IntPtr.Zero, ref scroll);
            } finally { SendMessage(answer.Handle, 0xB, new IntPtr(1), IntPtr.Zero); answer.Invalidate(); }
            copy.Enabled = text.Length > 0;
        }
        public void SetStatus(string value) {
            retry.Enabled = true;
            bool success = value.StartsWith("已完成", StringComparison.Ordinal);
            status.Text = success ? value.Replace("已完成", "解释") : value;
            status.ForeColor = success ? Theme.Muted : Color.FromArgb(166, 92, 53);
            tips.SetToolTip(status, value);
            if (success && latest.Length > 0) {
                var scroll = new ScrollPoint(); ScrollMessage(answer.Handle, 0x4DD, IntPtr.Zero, ref scroll);
                answer.Rtf = AnswerFormat.ToRtf(latest);
                ScrollMessage(answer.Handle, 0x4DE, IntPtr.Zero, ref scroll);
            }
        }
        protected override void Dispose(bool disposing) { if (disposing) { tips.Dispose(); bodyFont.Dispose(); } base.Dispose(disposing); }
    }
    public sealed class SettingsForm : Form {
        readonly TextBox url = new TextBox(), model = new TextBox(), key = new TextBox(), excluded = new TextBox(), prompt = new TextBox();
        readonly NumericUpDown delay = new NumericUpDown(), limit = new NumericUpDown();
        readonly CheckBox enabled = new CheckBox();
        public Config Value;
        public SettingsForm(Config config) {
            Text = "划词解释 · 设置"; ClientSize = new Size(550, 535); StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 10); BackColor = Color.White; ForeColor = Theme.Ink; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            var intro = new Label { Text = "让理解，跟上阅读。", Font = new Font(Font.FontFamily, 15, FontStyle.Bold), ForeColor = Theme.Ink, Location = new Point(20, 16), AutoSize = true };
            Controls.Add(intro);
            AddRow("API 基础地址", url, 58); url.Text = config.BaseUrl;
            AddRow("模型名称", model, 100); model.Text = config.Model;
            AddRow("API Key", key, 142); key.UseSystemPasswordChar = true; key.Text = config.GetKey();
            AddRow("触发延迟（毫秒）", delay, 184); delay.Minimum = 200; delay.Maximum = 3000; delay.Value = config.DelayMs;
            AddRow("选中文字数上限", limit, 226); limit.Minimum = 50; limit.Maximum = 20000; limit.Value = config.MaxChars;
            AddRow("排除的进程名", excluded, 268); excluded.Text = config.ExcludedApps;
            AddRow("解释指令", prompt, 310); prompt.Multiline = true; prompt.Height = 65; prompt.ScrollBars = ScrollBars.Vertical; prompt.Text = config.Prompt;
            enabled.Text = "启用选中后自动解释"; enabled.SetBounds(165, 387, 300, 28); enabled.Checked = config.Enabled; Controls.Add(enabled);
            var note = new Label { Text = "所选文本会发送给你配置的模型服务。密钥在本机加密保存。\n不保存选中文字或解释历史。进程名用英文逗号分隔。", Location = new Point(20, 423), Size = new Size(515, 48), ForeColor = Color.DimGray };
            Controls.Add(note);
            var save = new Button { Text = "保存设置", Location = new Point(355, 482), Size = new Size(170, 34), BackColor = Theme.Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            save.FlatAppearance.BorderSize = 0;
            save.Click += Save; Controls.Add(save); AcceptButton = save;
        }
        void AddRow(string label, Control control, int y) {
            Controls.Add(new Label { Text = label, Location = new Point(20, y + 3), Size = new Size(145, 28) });
            control.SetBounds(165, y, 360, 29); Controls.Add(control);
        }
        void Save(object sender, EventArgs args) {
            try {
                var value = new Config { BaseUrl = url.Text.Trim(), Model = model.Text.Trim(), DelayMs = (int)delay.Value,
                    MaxChars = (int)limit.Value, ExcludedApps = excluded.Text.Trim(), Prompt = prompt.Text.Trim(), Enabled = enabled.Checked };
                value.SetKey(key.Text.Trim()); value.Validate();
                if (String.IsNullOrWhiteSpace(value.GetKey()) && !value.Endpoint().IsLoopback) throw new ArgumentException("请填写 API Key；本机模型接口可留空。");
                Value = value; DialogResult = DialogResult.OK; Close();
            } catch (Exception error) { MessageBox.Show(this, error.Message, "检查设置", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
    }
}
