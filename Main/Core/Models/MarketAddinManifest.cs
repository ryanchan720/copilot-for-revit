using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Main.Core.Models
{
    // 此类表示位于插件共享目录中的插件清单以及提供相关操作方法
    // 根对象为两层动态 key 的字典：
    // {
    //   "HelloRevit-(5c7055)": {
    //     "HelloRevitCommand": { ... },
    //     "ChangeProjectStatusCommand": { ... }
    //   }
    // }
    public class MarketAddinManifest : Dictionary<string, Dictionary<string, AddinCommandDefinition>>
    {
        /// <summary>
        /// 从指定 JSON 文件反序列化为清单对象。
        /// </summary>
        /// <param name="filePath">JSON 文件路径。</param>
        /// <returns>反序列化得到的清单对象。</returns>
        public static MarketAddinManifest LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new MarketAddinManifest();
            }

            // 读取文件并使用 JsonConvert 反序列化为当前类型
            var json = File.ReadAllText(filePath);
            var manifest = JsonConvert.DeserializeObject<MarketAddinManifest>(json);
            return manifest ?? new MarketAddinManifest();
        }

        /// <summary>
        /// 将当前清单对象序列化为 JSON 并写入指定文件。
        /// </summary>
        /// <param name="filePath">输出 JSON 文件路径。</param>
        public void SaveToFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new System.ArgumentException("filePath is null or empty", nameof(filePath));
            }

            // 序列化为缩进的 JSON，便于人工检查
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 从特定格式的 Markdown 文档解析出插件清单。
        /// 默认顶层 key 使用 rootKey 作为分组名，例如一个插件包名称。
        /// </summary>
        /// <param name="filePath">Markdown 文件路径。</param>
        /// <param name="rootKey">顶层分组名称，使用文件夹名称，例如 "HelloRevit-(5c7055)"。</param>
        /// <returns>解析得到的清单对象。</returns>
        public static MarketAddinManifest LoadFromMarkdownFile(string filePath, string rootKey)
        {
            var markdown = File.ReadAllText(filePath);
            return ParseFromMarkdown(markdown, rootKey);
        }

        /// <summary>
        /// 将特定格式的 Markdown 字符串解析为 MarketAddinManifest。
        /// </summary>
        public static MarketAddinManifest ParseFromMarkdown(string markdown, string rootKey)
        {
            var manifest = new MarketAddinManifest();
            var commands = new Dictionary<string, AddinCommandDefinition>();

            // 使用简单的行解析算法处理结构化 Markdown
            var lines = markdown.Replace("\r\n", "\n").Split('\n');

            string currentCommandKey = null;
            AddinCommandDefinition current = null;
            bool inJsonBlock = false;
            var jsonLines = new System.Text.StringBuilder();

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();

                // 处理标题行，例如: ## HelloRevitCommand
                if (line.StartsWith("## "))
                {
                    // 保存上一个命令
                    if (currentCommandKey != null && current != null)
                    {
                        commands[currentCommandKey] = current;
                    }

                    currentCommandKey = line.Substring(3).Trim();
                    current = new AddinCommandDefinition();
                    inJsonBlock = false;
                    jsonLines.Length = 0;
                    continue;
                }

                if (current == null)
                {
                    // 还没有遇到第一个命令标题
                    continue;
                }

                // Markdown 代码块开始/结束
                if (line.StartsWith("```"))
                {
                    if (!inJsonBlock)
                    {
                        // 进入 JSON 代码块
                        inJsonBlock = true;
                        jsonLines.Length = 0;
                    }
                    else
                    {
                        // 退出 JSON 代码块
                        inJsonBlock = false;
                        var jsonText = jsonLines.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(jsonText) && !jsonText.Equals("无"))
                        {
                            try
                            {
                                current.ParameterSchema = JObject.Parse(jsonText);
                            }
                            catch (JsonException)
                            {
                                // JSON 解析失败时忽略 schema，避免影响其它字段
                                current.ParameterSchema = null;
                            }
                        }
                        jsonLines.Length = 0;
                    }
                    continue;
                }

                if (inJsonBlock)
                {
                    // 收集 JSON 代码块内的行
                    jsonLines.AppendLine(rawLine);
                    continue;
                }

                // 解析普通的列表行，例如: - 命令名称: 弹窗提示
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("- "))
                {
                    continue;
                }

                var content = trimmed.Substring(2).Trim();
                var separatorIndex = content.IndexOf(':');
                if (separatorIndex < 0)
                {
                    continue;
                }

                var label = content.Substring(0, separatorIndex).Trim();
                var value = content.Substring(separatorIndex + 1).Trim();

                // 将 Markdown 中常见的 "无" 视为 null
                if (string.Equals(value, "无", System.StringComparison.OrdinalIgnoreCase))
                {
                    value = null;
                }

                switch (label)
                {
                    case "命令名称":
                        current.Name = value;
                        break;
                    case "命令描述":
                        current.Description = value;
                        break;
                    case "命令参数":
                        // 实际 JSON 在后续代码块中解析，这里无需处理 "无" 以外的内容
                        if (string.Equals(value, "无", System.StringComparison.OrdinalIgnoreCase))
                        {
                            current.ParameterSchema = null;
                        }
                        break;
                    case "命令开发者":
                        current.Author = value;
                        break;
                    case "超时时长":
                        int timeout;
                        if (int.TryParse(ExtractDigits(value), out timeout))
                        {
                            current.Timeout = timeout;
                        }
                        break;
                    case "触发条件":
                        current.Trigger = value;
                        break;
                    case "结果返回":
                        current.Response = value;
                        break;
                    case "备注":
                        current.Remark = value;
                        break;
                }
            }

            // 收尾处理最后一个命令
            if (currentCommandKey != null && current != null)
            {
                commands[currentCommandKey] = current;
            }

            manifest[rootKey] = commands;
            return manifest;
        }

        private static string ExtractDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return Regex.Replace(text, "[^0-9]", string.Empty);
        }
    }

    /// <summary>
    /// 单个命令的定义结构，对应最内层的对象内容。
    /// </summary>
    public class AddinCommandDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        // 使用 JObject 表示 parameter_schema，可容纳任意 JSON Schema 结构
        [JsonProperty("parameter_schema")]
        public JObject ParameterSchema { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("timeout")]
        public int Timeout { get; set; }

        [JsonProperty("trigger")]
        public string Trigger { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("remark")]
        public string Remark { get; set; }
    }
}

// 示例结构
/*
{
    "HelloRevit-(5c7055)": {
        "HelloRevitCommand": {
            "name": "弹窗提示",
            "description": "在 Revit 中弹窗显示用户传递的内容",
            "parameter_schema": {
                "type": "object",
                "properties": {
                    "content": {
                        "type": "string",
                        "description": "用于显示弹窗的文本内容",
                        "required": false,
                        "example": "Hello, Revit!"
                    }
                }
            },
            "author": "张三",
            "timeout": 20,
            "trigger": "向 AI 发送\"弹窗测试\"或相近语义内容",
            "response": "执行成功将返回“弹窗显示成功”消息，失败将返回具体错误信息",
            "remark": null
        },
        "ChangeProjectStatusCommand": {
            "name": "修改项目信息",
            "description": "在 Revit 中修改项目信息中的 Project Status 属性",
            "parameter_schema": null,
            "author": "张三",
            "timeout": 20,
            "trigger": "向 AI 发送\"事务测试\"或相近语义内容",
            "response": "执行成功将返回“已修改项目状态信息为 xxx”消息，失败将返回具体错误信息",
            "remark": null
        }
    }
}
*/