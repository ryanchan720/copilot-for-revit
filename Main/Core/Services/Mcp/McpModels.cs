using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Main.Core.Services.Mcp
{
    internal class McpJsonRpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; }

        [JsonProperty("id")]
        public JToken Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JToken Params { get; set; }
    }

    internal class McpInitializeResult
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("serverInfo")]
        public McpServerInfo ServerInfo { get; set; }

        [JsonProperty("capabilities")]
        public McpCapabilities Capabilities { get; set; }
    }

    internal class McpServerInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    internal class McpCapabilities
    {
        // 空对象表示支持 tools 能力
        [JsonProperty("tools")]
        public JObject Tools { get; set; }
    }

    internal class McpTool
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("inputSchema")]
        public JObject InputSchema { get; set; }
    }

    /// <summary>
    /// 从 README.md 解析出的单个命令的 MCP 元信息。
    /// </summary>
    internal class McpCommandInfo
    {
        private static readonly HashSet<string> ChineseAliases = new HashSet<string>(new[]
        {
            "chinese", "zh", "zhcn", "zhhans", "chinesesimplified", "simplifiedchinese"
        });

        private static readonly HashSet<string> EnglishAliases = new HashSet<string>(new[]
        {
            "english", "en", "enus", "enuk", "eng"
        });

        /// <summary>命令类名（用于查找映射）。</summary>
        public string ClassName { get; set; }

        /// <summary>命令显示名称，来自「命令名称」字段。</summary>
        public string DisplayName { get; set; }

        /// <summary>一句话简介，来自「命令描述」字段，用于 tools/list。</summary>
        public string Brief { get; set; }

        /// <summary>触发条件，来自「触发条件」字段，辅助 AI 判断调用时机。</summary>
        public string TriggerCondition { get; set; }

        /// <summary>README 原文中移除 JSON Schema 代码块后的完整描述文本，用于 tools/list。</summary>
        public string FullDescription { get; set; }

        /// <summary>README 原文块（含 Schema）。</summary>
        public string FullContent { get; set; }

        /// <summary>从「命令参数」代码块解析出的 JSON Schema。</summary>
        public JObject InputSchema { get; set; }

        /// <summary>目标语言列表（如 Chinese, English）。为空表示未指定。</summary>
        public List<string> TargetLanguages { get; set; }

        /// <summary>兼容 Revit 主版本号列表（如 2019, 2024）。为空表示未限制版本。</summary>
        public List<int> CompatibleVersions { get; set; }

        public bool IsCompatibleWith(int revitVersion)
        {
            return CompatibleVersions == null || CompatibleVersions.Count == 0 || CompatibleVersions.Contains(revitVersion);
        }

        public bool IsLanguageCompatibleWith(string currentLanguage)
        {
            if (TargetLanguages == null || TargetLanguages.Count == 0) return true;
            if (string.IsNullOrWhiteSpace(currentLanguage)) return true;

            var current = NormalizeLanguageToken(currentLanguage);
            foreach (var language in TargetLanguages)
            {
                var target = NormalizeLanguageToken(language);
                if (string.IsNullOrEmpty(target)) continue;

                if (target == current) return true;
                if (IsChineseAlias(target) && IsChineseAlias(current)) return true;
                if (IsEnglishAlias(target) && IsEnglishAlias(current)) return true;
            }

            return false;
        }

        private static bool IsChineseAlias(string token)
        {
            return ChineseAliases.Contains(token);
        }

        private static bool IsEnglishAlias(string token)
        {
            return EnglishAliases.Contains(token);
        }

        private static string NormalizeLanguageToken(string language)
        {
            if (string.IsNullOrWhiteSpace(language)) return string.Empty;

            var normalized = language.Trim().ToLowerInvariant();
            normalized = normalized.Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);

            if (normalized.Contains("chinese") || normalized.Contains("zh")) return "chinese";
            if (normalized.Contains("english") || normalized.Contains("en")) return "english";

            return normalized;
        }
    }

    internal static class McpErrorCodes
    {
        public const int ParseError     = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams  = -32602;
        public const int InternalError  = -32603;
    }
}
