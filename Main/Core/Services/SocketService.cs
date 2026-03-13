using Autodesk.Revit.UI;
using Main.CommandSet.Commands;
using Main.Core.Abstractions;
using Main.Core.Models;
using Main.Core; // for AppServices
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Main.Core.Services
{
    public class SocketService
    {
        private static SocketService _instance;
        private readonly ILogger<SocketService> _logger;
        private TcpListener _listener;
        private Thread _listenerThread;
        private bool _isRunning;
        private int _port = 18180;
        private UIApplication _uiApp;
        private readonly List<Thread> _clientThreads = new List<Thread>();
        private readonly List<TcpClient> _clients = new List<TcpClient>();

        public SocketService(ILogger<SocketService> logger)
        {
            _logger = logger;
            _instance = this;
        }

        public static void Initialize(ILogger<SocketService> logger)
        {
            if (_instance == null)
            {
                _instance = new SocketService(logger);
            }
        }

        public static SocketService Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("SocketService is not initialized. Call SocketService.Initialize(...) first.");
                return _instance;
            }
        }

        public bool IsRunning => _isRunning;

        public int Port
        {
            get => _port;
            set => _port = value;
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _logger.LogInfo("Socket 服务准备启动...");
                _isRunning = true;
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _logger.LogInfo($"Socket 服务已启动，监听端口: {_port}");

                _listenerThread = new Thread(ListenForClients)
                {
                    IsBackground = true
                };
                _listenerThread.Start();
            }
            catch (Exception ex)
            {
                _isRunning = false;
                _logger.LogError($"Socket 服务启动失败: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _logger.LogInfo("Socket 服务正在停止...");
                _isRunning = false;

                // 关闭所有客户端连接
                lock (_clients)
                {
                    foreach (var client in _clients)
                        client.Close();
                    _clients.Clear();
                }

                // 等待所有客户端线程退出
                lock (_clientThreads)
                {
                    foreach (var thread in _clientThreads)
                        if (thread.IsAlive) thread.Join(1000);
                    _clientThreads.Clear();
                }

                _listener?.Stop();
                _listener = null;

                if (_listenerThread != null && _listenerThread.IsAlive)
                {
                    _listenerThread.Join(1000);
                }
                _logger.LogInfo("Socket 服务已停止");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Socket 服务停止异常: {ex.Message}");
            }
        }

        private void ListenForClients()
        {
            try
            {
                _logger.LogInfo("Socket 服务监听线程已启动");
                while (_isRunning)
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    _logger.LogInfo("检测到新客户端连接");
                    lock (_clients) { _clients.Add(client); }
                    Thread clientThread = new Thread(HandleClientCommunication)
                    {
                        IsBackground = true
                    };
                    lock (_clientThreads) { _clientThreads.Add(clientThread); }
                    clientThread.Start(client);
                }
            }
            catch (SocketException ex)
            {
                _logger.LogError($"Socket 异常: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Socket 服务监听线程异常: {ex.Message}");
            }
        }

        private void HandleClientCommunication(object clientObj)
        {
            TcpClient tcpClient = (TcpClient)clientObj;
            NetworkStream stream = tcpClient.GetStream();

            try
            {
                byte[] buffer = new byte[8192];

                while (_isRunning && tcpClient.Connected)
                {
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                    }
                    catch (IOException)
                    {
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    System.Diagnostics.Trace.WriteLine($"收到消息: {message}");

                    string response = ProcessJsonRPCRequest(message);

                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                }
            }
            catch (Exception)
            {
                // log
            }
            finally
            {
                tcpClient.Close();
                lock (_clients) { _clients.Remove(tcpClient); }
                lock (_clientThreads) { _clientThreads.Remove(Thread.CurrentThread); }
            }
        }

        private string ProcessJsonRPCRequest(string requestJson)
        {
            JsonRPCRequest request;

            try
            {
                // 解析JSON-RPC请求
                request = JsonConvert.DeserializeObject<JsonRPCRequest>(requestJson);

                // 验证请求格式是否有效
                if (request == null || !request.IsValid())
                {
                    return CreateErrorResponse(
                        null,
                        JsonRPCErrorCodes.InvalidRequest,
                        "Invalid JSON-RPC request"
                    );
                }

                var registry = AppServices.AddinRegistry;
                if (registry == null)
                {
                    return CreateErrorResponse(
                        request.Id,
                        JsonRPCErrorCodes.InternalError,
                        "AddinRegistry is not initialized."
                    );
                }

                // 查找命令
                Command command;
                if (!registry.TryGetCommand(request.Method, out command))
                {
                    // 屏蔽市场查找，后续完善
                    //if (!registry.TryGetCommandFromMarket(request.Method, out command))
                    //{
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.MethodNotFound,
                        $"Method '{request.Method}' not found in local registry or market");
                    //}
                }

                // 执行命令
                try
                {
                    if (command.AssemblyPath == string.Empty || command.FullClassName == string.Empty)
                    {
                        throw new Exception($"Command '{command.Name}' infomation is not complete.");
                    }
                    ExecuteCommand.AssemblyPath = command.AssemblyPath;
                    ExecuteCommand.FullClassName = command.FullClassName;
                    _logger.LogInfo("执行命令...");
                    object result = Command.Execute(request.GetParamsObject());

                    return CreateSuccessResponse(request.Id, result);
                }
                catch (Exception ex)
                {
                    return CreateErrorResponse(request.Id, JsonRPCErrorCodes.InternalError, ex.Message);
                }
            }
            catch (JsonException)
            {
                // JSON解析错误
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.ParseError,
                    "Invalid JSON"
                );
            }
            catch (Exception ex)
            {
                // 处理请求时的其他错误
                return CreateErrorResponse(
                    null,
                    JsonRPCErrorCodes.InternalError,
                    $"Internal error: {ex.Message}"
                );
            }
        }

        private string CreateSuccessResponse(string id, object result)
        {
            var response = new JsonRPCSuccessResponse
            {
                Id = id,
                Result = result is JToken jToken ? jToken : JToken.FromObject(result)
            };

            return response.ToJson();
        }

        private string CreateErrorResponse(string id, int code, string message, object data = null)
        {
            var response = new JsonRPCErrorResponse
            {
                Id = id,
                Error = new JsonRPCError
                {
                    Code = code,
                    Message = message,
                    Data = data != null ? JToken.FromObject(data) : null
                }
            };

            return response.ToJson();
        }

        public void SendMessageToClient(TcpClient client, string message)
        {
            if (client == null || !client.Connected)
                return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                NetworkStream stream = client.GetStream();
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError($"发送消息到客户端失败: {ex.Message}");
                try { client.Close(); } catch { }
                lock (_clients) { _clients.Remove(client); }
            }
        }
    }
}
