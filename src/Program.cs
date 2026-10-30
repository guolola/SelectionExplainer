using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SelectionExplainer {
    internal static class Program {
        [STAThread] static void Main(string[] args) {
            bool created;
            using (var mutex = new Mutex(true, "Local\\SelectionExplainer.Desktop.v1", out created)) {
                if (!created) { MessageBox.Show("划词解释已经运行，请在系统托盘中打开设置。", "划词解释"); return; }
                Native.SetProcessDPIAware(); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SelectionExplainer");
                if (args.Length == 2 && args[0] == "--data-dir") directory = Path.GetFullPath(args[1]);
                try { using (var app = new AppContext(new ConfigStore(directory))) Application.Run(app); }
                catch (Exception error) { MessageBox.Show("无法启动：" + error.Message, "划词解释", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }
    internal sealed class AppContext : ApplicationContext {
        readonly Form dispatcher = new Form();
        readonly ConfigStore store;
        Config config;
        readonly NotifyIcon tray;
        readonly ToolStripMenuItem toggle;
        readonly ResultForm result = new ResultForm();
        readonly SelectionMonitor monitor;
        readonly System.Windows.Forms.Timer debounce = new System.Windows.Forms.Timer();
        readonly System.Windows.Forms.Timer render = new System.Windows.Forms.Timer();
        CancellationTokenSource request;
        readonly StringBuilder output = new StringBuilder();
        readonly object outputLock = new object();
        long revision;
        bool settingsOpen, disposed;
        Point point;
        string lastText;
        Point lastPoint;
        int targetProcess;
        readonly int ownProcess = Process.GetCurrentProcess().Id;
        public AppContext(ConfigStore configStore) {
            store = configStore;
            config = store.Load();
            IntPtr unused = dispatcher.Handle;
            toggle = new ToolStripMenuItem();
            var menu = new ContextMenuStrip(); menu.Items.Add(toggle);
            menu.Items.Add("模型与设置…", null, delegate { OpenSettings(); });
            menu.Items.Add("退出", null, delegate { ExitThread(); });
            tray = new NotifyIcon { Icon = SystemIcons.Information, Text = "划词解释", ContextMenuStrip = menu, Visible = true };
            tray.DoubleClick += delegate { OpenSettings(); };
            toggle.Click += delegate { config.Enabled = !config.Enabled; Cancel(); result.Hide(); UpdateTray(); };
            UpdateTray();
            result.Dismissed += Cancel;
            result.RetryRequested += delegate { if (!String.IsNullOrWhiteSpace(lastText)) Explain(lastText, lastPoint); };
            result.SettingsRequested += OpenSettings;
            debounce.Tick += delegate { debounce.Stop(); Capture(); };
            render.Interval = 100;
            render.Tick += delegate { lock (outputLock) result.SetAnswer(output.ToString()); };
            monitor = new SelectionMonitor();
            monitor.MousePressed += delegate { Post(delegate { if (!IsOwnWindowUnderMouse() && !result.Pinned) { Cancel(); result.Hide(); } }); };
            monitor.MouseReleased += delegate(Point location, bool selectionGesture) {
                if (selectionGesture) Post(delegate { Schedule(location); });
            };
            if (String.IsNullOrWhiteSpace(config.GetKey()) && !config.Endpoint().IsLoopback) Post(OpenSettings);
        }
        bool IsOwnWindowUnderMouse() { return result.Visible && result.Bounds.Contains(Cursor.Position) || settingsOpen; }
        void Post(Action action) { if (!disposed && !dispatcher.IsDisposed) { try { dispatcher.BeginInvoke(action); } catch (InvalidOperationException) { } } }
        void UpdateTray() { toggle.Text = config.Enabled ? "暂停自动解释" : "恢复自动解释"; tray.Text = config.Enabled ? "划词解释 · 已启用" : "划词解释 · 已暂停"; }
        void Cancel() { revision++; debounce.Stop(); render.Stop(); if (request != null) { request.Cancel(); request.Dispose(); request = null; } }
        void Schedule(Point location) {
            if (!config.Enabled || settingsOpen || result.Visible && result.Bounds.Contains(location)) return;
            int process = Native.ForegroundProcess();
            if (process == 0 || process == ownProcess) return;
            try {
                using (var app = Process.GetProcessById(process)) {
                    foreach (string excluded in (config.ExcludedApps ?? "").Split(','))
                        if (app.ProcessName.Equals(excluded.Trim().Replace(".exe", ""), StringComparison.OrdinalIgnoreCase)) return;
                }
            } catch { return; }
            Cancel(); point = location; targetProcess = process;
            debounce.Interval = config.DelayMs; debounce.Start();
        }
        void Capture() {
            long version = revision; int process = targetProcess; Point location = point;
            if (Native.ForegroundProcess() != process) return;
            SelectionReader.ReadAsync(location, process, config.MaxChars, delegate(string text) {
                Post(delegate {
                    if (revision != version || !config.Enabled || settingsOpen || Native.ForegroundProcess() != process || String.IsNullOrWhiteSpace(text)) return;
                    if (text.Length > config.MaxChars) { result.Begin("选区过长", location); result.SetStatus("请缩小选区，或在设置中调整字数上限。"); return; }
                    Explain(text, location);
                });
            });
        }
        async void Explain(string text, Point location) {
            Cancel(); long version = revision;
            lastText = text; lastPoint = location;
            if (String.IsNullOrWhiteSpace(config.GetKey()) && !config.Endpoint().IsLoopback) { OpenSettings(); return; }
            var current = new CancellationTokenSource(); request = current;
            lock (outputLock) output.Clear();
            result.Begin(text, location); render.Start();
            var elapsed = Stopwatch.StartNew();
            try {
                await ModelClient.Explain(config, text, delegate(string delta) { lock (outputLock) { if (revision == version) output.Append(delta); } }, current.Token);
                if (revision == version) {
                    render.Stop(); lock (outputLock) result.SetAnswer(output.ToString());
                    result.SetStatus("已完成 · " + elapsed.Elapsed.TotalSeconds.ToString("0.0") + " 秒");
                }
            } catch (OperationCanceledException) { }
            catch (Exception error) { if (revision == version) result.SetStatus(error.Message); }
            finally {
                if (revision == version) { render.Stop(); lock (outputLock) result.SetAnswer(output.ToString()); }
                if (ReferenceEquals(request, current)) { request = null; current.Dispose(); }
            }
        }
        void OpenSettings() {
            if (settingsOpen) return;
            Cancel(); result.Hide(); settingsOpen = true;
            try {
                using (var form = new SettingsForm(config)) {
                    if (form.ShowDialog() == DialogResult.OK) {
                        try { store.Save(form.Value); config = form.Value; UpdateTray(); }
                        catch (Exception) { MessageBox.Show("设置保存失败，请检查配置目录权限。原有设置保持不变。", "划词解释"); }
                    }
                }
            } finally { settingsOpen = false; }
        }
        protected override void ExitThreadCore() {
            Cancel(); disposed = true; monitor.Dispose(); tray.Visible = false; tray.Dispose();
            debounce.Dispose(); render.Dispose(); result.Dispose(); dispatcher.Dispose(); base.ExitThreadCore();
        }
    }
}
