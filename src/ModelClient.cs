using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SelectionExplainer {
    public static class ModelClient {
        public static string Delta(string json) {
            var root = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
            if (root == null) return "";
            if (root.ContainsKey("error")) throw new InvalidDataException("模型服务返回了错误，请检查模型和额度。");
            object choicesObject;
            if (!root.TryGetValue("choices", out choicesObject)) return "";
            var choices = choicesObject as object[];
            if (choices == null || choices.Length == 0) return "";
            var choice = choices[0] as Dictionary<string, object>;
            object messageObject;
            if (choice == null || (!choice.TryGetValue("delta", out messageObject) && !choice.TryGetValue("message", out messageObject))) return "";
            var message = messageObject as Dictionary<string, object>;
            object content;
            return message != null && message.TryGetValue("content", out content) ? content as string ?? "" : "";
        }
        public static async Task Explain(Config config, string text, Action<string> append, CancellationToken token) {
            config.Validate();
            if (String.IsNullOrWhiteSpace(text) || text.Length > config.MaxChars) throw new ArgumentException("所选文本为空或超过长度上限。");
            var request = (HttpWebRequest)WebRequest.Create(config.Endpoint());
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            request.Accept = "text/event-stream";
            request.AllowAutoRedirect = false;
            request.Timeout = 90000;
            request.ReadWriteTimeout = 90000;
            string key = config.GetKey();
            if (!String.IsNullOrWhiteSpace(key)) request.Headers["Authorization"] = "Bearer " + key;
            var payload = new { model = config.Model, stream = true, max_tokens = 800,
                messages = new[] { new { role = "system", content = config.Prompt }, new { role = "user", content = text } } };
            byte[] bytes = Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(payload));
            request.ContentLength = bytes.Length;
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                timeout.CancelAfter(90000);
                using (timeout.Token.Register(request.Abort)) {
                    try {
                        using (var stream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                            await stream.WriteAsync(bytes, 0, bytes.Length, timeout.Token).ConfigureAwait(false);
                        using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false)) {
                            if ((int)response.StatusCode >= 300) throw new InvalidDataException("接口发生重定向，请填写最终 API 基础地址。");
                            using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
                                if ((response.ContentType ?? "").IndexOf("text/event-stream", StringComparison.OrdinalIgnoreCase) < 0) {
                                    var body = new StringBuilder();
                                    var buffer = new char[2048];
                                    int read;
                                    while ((read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0) {
                                        timeout.Token.ThrowIfCancellationRequested();
                                        body.Append(buffer, 0, read);
                                        if (body.Length > 131072) throw new InvalidDataException("接口响应过大。");
                                    }
                                    string result = Delta(body.ToString());
                                    if (String.IsNullOrWhiteSpace(result)) throw new InvalidDataException("模型未返回解释内容。");
                                    append(result);
                                    return;
                                }
                                string line;
                                int total = 0;
                                bool received = false;
                                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null) {
                                    timeout.Token.ThrowIfCancellationRequested();
                                    total += line.Length;
                                    if (total > 262144) throw new InvalidDataException("接口响应过大。");
                                    if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
                                    string data = line.Substring(5).Trim();
                                    if (data == "[DONE]") break;
                                    if (data.Length == 0) continue;
                                    string delta = Delta(data);
                                    if (delta.Length > 0) { received = true; append(delta); }
                                }
                                if (!received) throw new InvalidDataException("模型未返回解释内容，请检查接口是否支持流式输出。");
                            }
                        }
                    } catch (WebException error) {
                        if (token.IsCancellationRequested) throw new OperationCanceledException(token);
                        if (timeout.IsCancellationRequested) throw new TimeoutException("请求超过 90 秒，请检查网络或更换模型。");
                        using (var response = error.Response as HttpWebResponse) {
                            if (response != null) {
                                int status = (int)response.StatusCode;
                                throw new InvalidOperationException(status == 401 || status == 403 ? "认证失败，请检查 API Key 和接口权限。" :
                                    status == 429 ? "请求受限或额度不足，请稍后重试。" : "模型接口返回 HTTP " + status + "，请检查地址和模型名称。");
                            }
                        }
                        throw new InvalidOperationException("无法连接模型服务，请检查 API 地址、网络和代理。");
                    }
                }
            }
        }
    }
}
