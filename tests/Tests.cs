using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;

namespace SelectionExplainer {
    public static class Tests {
        [StructLayout(LayoutKind.Sequential)] struct PrintRect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct PrintRange { public IntPtr Hdc, Target; public PrintRect Bounds, Page; public int First, Last; }
        [DllImport("user32.dll", EntryPoint="SendMessage")] static extern IntPtr PrintText(IntPtr window, int message, IntPtr wParam, ref PrintRange range);
        [DllImport("user32.dll", EntryPoint="SendMessage")] static extern IntPtr ClearPrint(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
        static void RenderText(System.Windows.Forms.RichTextBox text, System.Drawing.Bitmap bitmap) {
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap)) {
                float x = 1440F / graphics.DpiX, y = 1440F / graphics.DpiY;
                var hdc = graphics.GetHdc();
                try {
                    var range = new PrintRange { Hdc = hdc, Target = hdc, First = 0, Last = -1,
                        Bounds = new PrintRect { Left = (int)(text.Left * x), Top = (int)(text.Top * y), Right = (int)((text.Right - 18) * x), Bottom = (int)(text.Bottom * y) },
                        Page = new PrintRect { Right = (int)(bitmap.Width * x), Bottom = (int)(bitmap.Height * y) } };
                    PrintText(text.Handle, 0x439, new IntPtr(1), ref range);
                } finally { ClearPrint(text.Handle, 0x439, IntPtr.Zero, IntPtr.Zero); graphics.ReleaseHdc(hdc); }
            }
        }
        static int passed;
        static void Assert(bool value, string name) { if (!value) throw new Exception("FAIL: " + name); passed++; Console.WriteLine("PASS: " + name); }
        [STAThread] public static int Main(string[] args) {
            try {
                if (args.Length == 2 && args[0] == "--render") {
                    Native.SetProcessDPIAware(); System.Windows.Forms.Application.EnableVisualStyles();
                    using (var form = new ResultForm()) {
                        form.Begin("Good design is as little design as possible.", new System.Drawing.Point(100, 100), false);
                        form.SetAnswer("好的设计，是尽可能少的设计。\n\n这句话强调的是 **克制**：保留真正有用的部分，去掉让人分心的装饰。\n\n### 放在这个工具里\n- 选中文字，解释自然出现。\n- 内容清楚，操作留在需要的地方。\n- 读完收起，继续手头的事情。");
                        form.SetStatus("已完成 · 1.2 秒");
                        form.Location = new System.Drawing.Point(-30000, -30000);
                        form.Show(); System.Windows.Forms.Application.DoEvents();
                        using (var bitmap = new System.Drawing.Bitmap(form.Width, form.Height)) {
                            form.DrawToBitmap(bitmap, form.ClientRectangle);
                            foreach (System.Windows.Forms.Control control in form.Controls) {
                                var richText = control as System.Windows.Forms.RichTextBox;
                                if (richText != null) RenderText(richText, bitmap);
                            }
                            bitmap.Save(args[1], System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    return 0;
                }
                if (args.Length == 1 && args[0] == "--fixture") {
                    System.Windows.Forms.Application.EnableVisualStyles();
                    var form = new System.Windows.Forms.Form { Text = "Selection Explainer Test Fixture", Width = 650, Height = 180 };
                    var text = new System.Windows.Forms.TextBox { Text = "Selection explains the meaning of a highlighted sentence automatically.", Dock = System.Windows.Forms.DockStyle.Top, Font = new System.Drawing.Font("Segoe UI", 14) };
                    form.Controls.Add(text); System.Windows.Forms.Application.Run(form); return 0;
                }
                if (args.Length == 2 && args[0] == "--serve") { Serve(args[1]); return 0; }
                Run().GetAwaiter().GetResult(); Console.WriteLine("TOTAL: " + passed + " passed"); return 0;
            } catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }
        static async Task Run() {
            var config = new Config();
            Assert(config.Endpoint().AbsoluteUri == "https://api.deepseek.com/v1/chat/completions", "endpoint construction");
            config.BaseUrl = "http://example.com/v1";
            bool rejected = false; try { config.Validate(); } catch (ArgumentException) { rejected = true; }
            Assert(rejected, "remote plain HTTP rejected");
            config.BaseUrl = "https://example.com/v1/chat/completions";
            rejected = false; try { config.Validate(); } catch (ArgumentException) { rejected = true; }
            Assert(rejected, "duplicate completions path rejected");
            config = new Config(); config.SetKey("test-only-dummy-key");
            Assert(config.GetKey() == "test-only-dummy-key" && !config.EncryptedKey.Contains("dummy"), "DPAPI key roundtrip");
            string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config-test-" + Guid.NewGuid().ToString("N"));
            var store = new ConfigStore(directory); store.Save(config); config.Model = "changed"; store.Save(config);
            Assert(store.Load().Model == "changed" && store.Load().GetKey() == "test-only-dummy-key", "atomic settings replacement and roundtrip");
            Assert(!File.ReadAllText(Path.Combine(directory, "settings.json")).Contains("test-only-dummy-key"), "key not stored in plaintext");
            Assert(ModelClient.Delta("{\"choices\":[{\"delta\":{\"role\":\"assistant\"}}]}") == "", "role-only streaming delta");
            Assert(ModelClient.Delta("{\"choices\":[{\"delta\":{\"content\":\"中文解释\"}}]}") == "中文解释", "Chinese streaming delta");
            using (var view = new System.Windows.Forms.RichTextBox()) {
                view.Rtf = AnswerFormat.ToRtf("### 标题\n这是 **重点** 和 `code`。\n- 条目\n```csharp\nvar x = {1};\n```");
                Assert(view.Text.Contains("标题") && view.Text.Contains("重点") && !view.Text.Contains("**") && !view.Text.Contains("```"), "native rich text headings and emphasis");
                Assert(view.Text.Contains("var x = {1};") && view.Text.Contains("code"), "code block and RTF escaping");
                view.Rtf = AnswerFormat.ToRtf("中文 😀 {text} \\ path");
                Assert(view.Text.Contains("中文 😀 {text} \\ path"), "Unicode and RTF metacharacters survive formatting");
            }
            using (var form = new ResultForm()) {
                form.Begin("sample", new System.Drawing.Point(100, 100), false);
                form.SetAnswer("hello");
                System.Windows.Forms.RichTextBox text = null;
                foreach (System.Windows.Forms.Control control in form.Controls) if (control is System.Windows.Forms.RichTextBox) text = (System.Windows.Forms.RichTextBox)control;
                text.Select(1, 2); form.SetAnswer("hello world");
                Assert(text.SelectedText == "el", "stream append preserves reader selection");
                bool retryTriggered = false; form.RetryRequested += delegate { retryTriggered = true; };
                form.SetStatus("已完成 · 0.1 秒");
                foreach (System.Windows.Forms.Control control in form.Controls) {
                    var button = control as ActionButton;
                    if (button != null && (button.Symbol == "pin" || button.Symbol == "retry"))
                        typeof(System.Windows.Forms.Button).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(button, new object[] { EventArgs.Empty });
                }
                Assert(form.Pinned, "pin click handler toggles persistent state");
                Assert(retryTriggered, "retry click handler dispatches request");
            }
            SynchronizationContext.SetSynchronizationContext(null);
            using (var server = new MockServer()) {
                config = new Config { BaseUrl = server.Url + "v1", Model = "test-stream" };
                var output = new StringBuilder();
                await ModelClient.Explain(config, "selection sample", delegate(string s) { output.Append(s); }, CancellationToken.None);
                Assert(output.ToString() == "这是测试解释。", "HTTP streaming response assembled");
                Assert(server.LastBody.Contains("selection sample") && !server.LastBody.Contains("surrounding private context"), "only supplied selection sent");
                Assert(server.LastPath == "/v1/chat/completions", "request route correct");
                config.Model = "test-json"; output.Clear();
                await ModelClient.Explain(config, "test", delegate(string s) { output.Append(s); }, CancellationToken.None);
                Assert(output.ToString() == "JSON 解释", "non-stream JSON response fallback");
                config.Model = "test-auth";
                rejected = false; try { await ModelClient.Explain(config, "test", delegate { }, CancellationToken.None); }
                catch (InvalidOperationException e) { rejected = e.Message.Contains("认证失败"); }
                Assert(rejected, "HTTP 401 friendly error");
                config.Model = "test-slow";
                using (var cancel = new CancellationTokenSource(150)) {
                    bool cancelled = false; var watch = System.Diagnostics.Stopwatch.StartNew();
                    try { await ModelClient.Explain(config, "test", delegate { }, cancel.Token); } catch (OperationCanceledException) { cancelled = true; }
                    Assert(cancelled && watch.ElapsedMilliseconds < 2000, "in-flight request cancellation");
                }
                config.Model = "test-empty";
                rejected = false; try { await ModelClient.Explain(config, "test", delegate { }, CancellationToken.None); }
                catch (InvalidDataException) { rejected = true; }
                Assert(rejected, "empty model result reported");
                rejected = false; try { await ModelClient.Explain(config, new string('x', config.MaxChars + 1), delegate { }, CancellationToken.None); }
                catch (ArgumentException) { rejected = true; }
                Assert(rejected, "oversized selection not sent");
            }
        }
        static void Serve(string directory) {
            using (var server = new MockServer()) {
                new ConfigStore(directory).Save(new Config { BaseUrl = server.Url + "v1", Model = "test-stream" });
                Console.WriteLine("Mock server ready; only loopback requests; data directory configured.");
                Thread.Sleep(TimeSpan.FromMinutes(15));
            }
        }
        sealed class MockServer : IDisposable {
            readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            public string Url;
            public volatile string LastBody, LastPath;
            public MockServer() {
                listener.Start(); int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Url = "http://127.0.0.1:" + port + "/";
                Task.Run(async delegate {
                    while (true) {
                        try { var client = await listener.AcceptTcpClientAsync(); ThreadPool.QueueUserWorkItem(delegate { Respond(client); }); }
                        catch (SocketException) { break; } catch (ObjectDisposedException) { break; }
                    }
                });
            }
            void Respond(TcpClient client) {
                try {
                    using (client) {
                    var stream = client.GetStream();
                    var header = new StringBuilder();
                    while (!header.ToString().EndsWith("\r\n\r\n")) { int b = stream.ReadByte(); if (b < 0) return; header.Append((char)b); if (header.Length > 16384) return; }
                    string headers = header.ToString();
                    LastPath = headers.Split(' ')[1];
                    int length = 0;
                    foreach (string line in headers.Split(new[] { "\r\n" }, StringSplitOptions.None))
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) length = Int32.Parse(line.Substring(15).Trim());
                    if (headers.IndexOf("100-continue", StringComparison.OrdinalIgnoreCase) >= 0) Write(stream, "HTTP/1.1 100 Continue\r\n\r\n");
                    byte[] bytes = new byte[length]; int offset = 0;
                    while (offset < length) { int count = stream.Read(bytes, offset, length - offset); if (count == 0) return; offset += count; }
                    LastBody = Encoding.UTF8.GetString(bytes);
                    var body = new JavaScriptSerializer().DeserializeObject(LastBody) as System.Collections.Generic.Dictionary<string, object>;
                    string model = (string)body["model"];
                    if (model == "test-auth") { Write(stream, "HTTP/1.1 401 Unauthorized\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"); return; }
                    if (model == "test-slow") Thread.Sleep(2500);
                    Write(stream, "HTTP/1.1 200 OK\r\nConnection: close\r\nContent-Type: " + (model == "test-json" ? "application/json" : "text/event-stream") + "\r\n\r\n");
                    if (model == "test-json") Write(stream, "{\"choices\":[{\"message\":{\"content\":\"JSON 解释\"}}]}");
                    else {
                        if (model != "test-empty") foreach (string token in new[] { "这是", "测试", "解释。" }) {
                            Write(stream, "data: " + new JavaScriptSerializer().Serialize(new { choices = new[] { new { delta = new { content = token } } } }) + "\n\n");
                            Thread.Sleep(80);
                        }
                        Write(stream, "data: [DONE]\n\n");
                    }
                    }
                } catch { client.Close(); }
            }
            void Write(NetworkStream stream, string data) {
                byte[] bytes = Encoding.UTF8.GetBytes(data); stream.Write(bytes, 0, bytes.Length); stream.Flush();
            }
            public void Dispose() { listener.Stop(); }
        }
    }
}
