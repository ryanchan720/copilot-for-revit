using Main.CommandSet.Commands;
using Main.Core.Abstractions;
using Main.Core.Models;
using Main.Core.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Main.Core.Services.Mcp
{
    /// <summary>
    /// MCP Server，基于 HTTP+SSE 传输层（协议版本 2024-11-05）。
    /// 端点：
    ///   GET  /sse           建立 SSE 长连接，服务端推送 endpoint 事件
    ///   POST /messages      接收 JSON-RPC 请求，响应通过 SSE 连接推送
    /// </summary>
    public class McpService
    {
        private static McpService _instance;

        private readonly ILogger<McpService> _logger;
        private HttpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private const int Port = 18181;

        // sessionId → 活跃的 SSE 会话
        private readonly ConcurrentDictionary<string, McpSession> _sessions =
            new ConcurrentDictionary<string, McpSession>();

        // addin 文件夹路径 → 该文件夹 README.md 的解析结果（类名 → 命令信息）
        private readonly ConcurrentDictionary<string, Dictionary<string, McpCommandInfo>> _readmeCache =
            new ConcurrentDictionary<string, Dictionary<string, McpCommandInfo>>();

        public McpService(ILogger<McpService> logger)
        {
            _logger = logger;
            _instance = this;
        }

        public static void Initialize(ILogger<McpService> logger)
        {
            if (_instance == null)
                _instance = new McpService(logger);
        }

        public static McpService Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("McpService is not initialized. Call McpService.Initialize(...) first.");
                return _instance;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  生命周期
        // ─────────────────────────────────────────────────────────────────────

        public void Start()
        {
            if (_isRunning) return;
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{Port}/");
                _listener.Start();
                _isRunning = true;
                _logger.LogInfo($"MCP 服务已启动，端口: {Port}");

                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "McpListenerThread"
                };
                _listenerThread.Start();
            }
            catch (Exception ex)
            {
                _isRunning = false;
                _logger.LogError($"MCP 服务启动失败: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            try { _listener?.Stop(); } catch { }
            _listener = null;
            _logger.LogInfo("MCP 服务已停止");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HTTP 监听循环
        // ─────────────────────────────────────────────────────────────────────

        private void ListenLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var context = _listener.GetContext();
                    var path = context.Request.Url.AbsolutePath.TrimEnd('/');

                    // SSE 连接需要独立线程维持长连接
                    if (context.Request.HttpMethod == "GET" && path == "/sse")
                    {
                        var thread = new Thread(() => HandleSseConnection(context))
                        {
                            IsBackground = true,
                            Name = "McpSseThread"
                        };
                        thread.Start();
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                    }
                }
                catch (HttpListenerException) when (!_isRunning)
                {
                    break;
                }
                catch (Exception ex) when (_isRunning)
                {
                    _logger.LogError($"MCP 监听循环异常: {ex.Message}");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HTTP 请求路由
        // ─────────────────────────────────────────────────────────────────────

        private void HandleRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;
            AddCorsHeaders(res);

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            var path = req.Url.AbsolutePath.TrimEnd('/');

            if (req.HttpMethod == "POST" && path == "/messages")
                HandleMessagePost(context);
            else
            {
                res.StatusCode = 404;
                res.Close();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SSE 连接处理
        // ─────────────────────────────────────────────────────────────────────

        private void HandleSseConnection(HttpListenerContext context)
        {
            var res = context.Response;
            AddCorsHeaders(res);
            res.ContentType = "text/event-stream";
            res.Headers["Cache-Control"] = "no-cache";
            res.Headers["Connection"] = "keep-alive";
            res.StatusCode = 200;

            var sessionId = Guid.NewGuid().ToString("N");
            var session = new McpSession(sessionId, res.OutputStream);
            _sessions[sessionId] = session;

            _logger.LogInfo($"MCP 客户端已连接，sessionId: {sessionId}");

            // 通知客户端其消息发送地址
            session.SendEvent("endpoint", $"/messages?sessionId={sessionId}");

            // 心跳维持连接，直到客户端断开
            try
            {
                while (_isRunning && session.IsAlive)
                {
                    Thread.Sleep(20000);
                    if (session.IsAlive)
                        session.SendRaw(": heartbeat\n\n");
                }
            }
            finally
            {
                _sessions.TryRemove(sessionId, out _);
                _logger.LogInfo($"MCP 客户端已断开，sessionId: {sessionId}");
                try { res.Close(); } catch { }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  消息 POST 处理
        // ─────────────────────────────────────────────────────────────────────

        private void HandleMessagePost(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            var sessionId = req.QueryString["sessionId"];
            McpSession session;
            if (string.IsNullOrEmpty(sessionId) || !_sessions.TryGetValue(sessionId, out session))
            {
                res.StatusCode = 400;
                WriteText(res, "Invalid or missing sessionId");
                return;
            }

            string body;
            using (var reader = new StreamReader(req.InputStream, Encoding.UTF8))
                body = reader.ReadToEnd();

            // 立即返回 202，不阻塞客户端的 HTTP 请求
            res.StatusCode = 202;
            res.Close();

            // 在后台处理请求并通过 SSE 推送响应
            try
            {
                var responseJson = ProcessRequest(body);
                if (responseJson != null)
                    session.SendEvent("message", responseJson);
            }
            catch (Exception ex)
            {
                _logger.LogError($"MCP 处理请求异常: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  JSON-RPC 请求分发
        // ─────────────────────────────────────────────────────────────────────

        private string ProcessRequest(string json)
        {
            McpJsonRpcRequest request;
            try
            {
                request = JsonConvert.DeserializeObject<McpJsonRpcRequest>(json);
                if (request == null)
                    return BuildError(null, McpErrorCodes.InvalidRequest, "Invalid Request");
            }
            catch
            {
                return BuildError(null, McpErrorCodes.ParseError, "Parse error");
            }

            // 通知类消息（无 id）不需要响应
            if (request.Id == null)
            {
                _logger.LogInfo($"MCP 通知: {request.Method}");
                return null;
            }

            _logger.LogInfo($"MCP 请求: {request.Method} (id={request.Id})");

            switch (request.Method)
            {
                case "initialize":   return HandleInitialize(request);
                case "tools/list":   return HandleToolsList(request);
                case "tools/call":   return HandleToolsCall(request);
                default:
                    return BuildError(request.Id, McpErrorCodes.MethodNotFound,
                        $"Method not found: {request.Method}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MCP 方法实现
        // ─────────────────────────────────────────────────────────────────────

        private string HandleInitialize(McpJsonRpcRequest request)
        {
            var result = JObject.FromObject(new McpInitializeResult
            {
                ProtocolVersion = "2024-11-05",
                ServerInfo = new McpServerInfo { Name = "revit-copilot", Version = "1.0.0" },
                Capabilities = new McpCapabilities { Tools = new JObject() }
            });
            return BuildResult(request.Id, result);
        }

        private string HandleToolsList(McpJsonRpcRequest request)
        {
            var toolsArray = new JArray();
            var registry = AppServices.AddinRegistry;

            if (registry != null)
            {
                foreach (var addin in registry.RegisteredAddins)
                {
                    foreach (var node in addin.ItemList)
                    {
                        var command = node as Command;
                        if (command == null) continue;

                        // 从 README.md 读取完整描述（schema 除外）和 inputSchema
                        var cmdInfo = GetCommandInfo(command);
                        if (!IsCommandCompatibleWithCurrentContext(cmdInfo))
                            continue;

                        var description = cmdInfo?.FullDescription
                            ?? (!string.IsNullOrEmpty(addin.Description)
                                ? addin.Description
                                : $"Revit command: {command.Name}");

                        toolsArray.Add(JObject.FromObject(new McpTool
                        {
                            Name = command.Name,
                            Description = description,
                            InputSchema = cmdInfo?.InputSchema ?? BuildDefaultInputSchema()
                        }));
                    }
                }
            }

            return BuildResult(request.Id, new JObject { ["tools"] = toolsArray });
        }

        private string HandleToolsCall(McpJsonRpcRequest request)
        {
            var p = request.Params as JObject;
            var toolName = p?["name"]?.ToString();
            var arguments = (p?["arguments"] as JObject) ?? new JObject();

            if (string.IsNullOrEmpty(toolName))
                return BuildError(request.Id, McpErrorCodes.InvalidParams, "Missing 'name' in params");

            var registry = AppServices.AddinRegistry;
            if (registry == null)
                return BuildError(request.Id, McpErrorCodes.InternalError, "AddinRegistry not initialized");

            Command command;
            if (!registry.TryGetCommand(toolName, out command) &&
                !registry.TryGetCommandFromMarket(toolName, out command))
            {
                return BuildError(request.Id, McpErrorCodes.MethodNotFound, $"Tool not found: {toolName}");
            }

            var cmdInfo = GetCommandInfo(command);
            if (!IsCommandCompatibleWithCurrentContext(cmdInfo))
            {
                return BuildError(request.Id, McpErrorCodes.MethodNotFound,
                    $"Tool not available for current Revit version/language: {toolName}");
            }

            try
            {
                ExecuteCommand.AssemblyPath = command.AssemblyPath;
                ExecuteCommand.FullClassName = command.FullClassName;
                _logger.LogInfo($"MCP 执行工具: {toolName}");

                var result = Command.Execute(arguments);
                var text = result != null ? JsonConvert.SerializeObject(result) : "null";

                var callResult = new JObject
                {
                    ["content"] = new JArray
                    {
                        new JObject { ["type"] = "text", ["text"] = text }
                    }
                };
                return BuildResult(request.Id, callResult);
            }
            catch (Exception ex)
            {
                var errResult = new JObject
                {
                    ["isError"] = true,
                    ["content"] = new JArray
                    {
                        new JObject { ["type"] = "text", ["text"] = ex.Message }
                    }
                };
                return BuildResult(request.Id, errResult);
            }
        }

        /// <summary>
        /// 从 command 所在 addin 文件夹的 README.md 中查找该命令的 MCP 元信息。
        /// 结果按文件夹路径缓存，避免重复 IO。
        /// </summary>
        private McpCommandInfo GetCommandInfo(Command command)
        {
            if (string.IsNullOrEmpty(command.AssemblyPath)) return null;

            var folder = Path.GetDirectoryName(command.AssemblyPath);
            if (string.IsNullOrEmpty(folder)) return null;

            var parsed = GetCachedReadme(folder);

            // FullClassName 形如 Namespace.ClassName，取最后一段与 README ## 标题匹配
            var simpleClass = GetSimpleClassName(command.FullClassName);
            McpCommandInfo info;
            return parsed.TryGetValue(simpleClass, out info) ? info : null;
        }

        private Dictionary<string, McpCommandInfo> GetCachedReadme(string folder)
        {
            return _readmeCache.GetOrAdd(folder, f =>
                ReadmeParser.Parse(Path.Combine(f, "README.md")));
        }

        private static string GetSimpleClassName(string fullClassName)
        {
            if (string.IsNullOrEmpty(fullClassName)) return string.Empty;
            var lastDot = fullClassName.LastIndexOf('.');
            return lastDot >= 0 ? fullClassName.Substring(lastDot + 1) : fullClassName;
        }

        private static bool IsCommandCompatibleWithCurrentContext(McpCommandInfo info)
        {
            if (info == null)
                return true;

            var app = RevitService.App;
            if (app == null)
                return true;

            var versionOk = true;
            if (info.CompatibleVersions != null && info.CompatibleVersions.Count > 0 && !string.IsNullOrEmpty(app.VersionNumber))
            {
                int currentVersion;
                if (int.TryParse(app.VersionNumber, out currentVersion))
                    versionOk = info.IsCompatibleWith(currentVersion);
            }

            var languageOk = true;
            if (info.TargetLanguages != null && info.TargetLanguages.Count > 0)
                languageOk = info.IsLanguageCompatibleWith(app.Language.ToString());

            return versionOk && languageOk;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  辅助方法
        // ─────────────────────────────────────────────────────────────────────

        private static JObject BuildDefaultInputSchema()
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject(),
                ["additionalProperties"] = true
            };
        }

        private static string BuildResult(JToken id, JToken result)
        {
            return JsonConvert.SerializeObject(new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = result
            });
        }

        private static string BuildError(JToken id, int code, string message)
        {
            return JsonConvert.SerializeObject(new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["error"] = new JObject
                {
                    ["code"] = code,
                    ["message"] = message
                }
            });
        }

        private static void AddCorsHeaders(HttpListenerResponse res)
        {
            res.Headers["Access-Control-Allow-Origin"] = "*";
            res.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            res.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        }

        private static void WriteText(HttpListenerResponse res, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.Close();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SSE 会话：负责线程安全地向客户端推送事件
    // ─────────────────────────────────────────────────────────────────────────

    internal class McpSession
    {
        private readonly Stream _stream;
        private readonly object _writeLock = new object();

        public string SessionId { get; }
        public bool IsAlive { get; private set; } = true;

        public McpSession(string sessionId, Stream stream)
        {
            SessionId = sessionId;
            _stream = stream;
        }

        public void SendEvent(string eventType, string data)
        {
            SendRaw($"event: {eventType}\ndata: {data}\n\n");
        }

        public void SendRaw(string text)
        {
            lock (_writeLock)
            {
                if (!IsAlive) return;
                try
                {
                    var bytes = Encoding.UTF8.GetBytes(text);
                    _stream.Write(bytes, 0, bytes.Length);
                    _stream.Flush();
                }
                catch
                {
                    IsAlive = false;
                }
            }
        }
    }
}
