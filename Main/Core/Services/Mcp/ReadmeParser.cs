using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Main.Core.Services.Mcp
{
    /// <summary>
    /// 解析插件 README.md，提取每个命令的 MCP 所需元信息。
    /// README 格式约定：每个 ## 二级标题块代表一个命令。
    /// </summary>
    internal static class ReadmeParser
    {
        public static Dictionary<string, McpCommandInfo> Parse(string readmePath)
        {
            var result = new Dictionary<string, McpCommandInfo>(System.StringComparer.OrdinalIgnoreCase);

            if (!File.Exists(readmePath)) return result;

            string content;
            try { content = File.ReadAllText(readmePath, Encoding.UTF8); }
            catch { return result; }

            // 按 ## 二级标题拆分，每块对应一个命令
            var sections = Regex.Split(content, @"(?=^## )", RegexOptions.Multiline);

            foreach (var section in sections)
            {
                var trimmed = section.TrimStart();
                if (!trimmed.StartsWith("## ")) continue;

                var info = ParseSection(trimmed);
                if (info != null)
                    result[info.ClassName] = info;
            }

            return result;
        }

        private static McpCommandInfo ParseSection(string section)
        {
            var info = new McpCommandInfo { FullContent = section.Trim() };

            var lines = section.Split('\n');
            if (lines.Length == 0) return null;

            // 第一行：## ClassName（作为 ClassName 的初始值）
            var header = lines[0].Trim();
            if (!header.StartsWith("## ")) return null;
            info.ClassName = header.Substring(3).Trim();

            foreach (var line in lines)
            {
                var t = line.Trim();
                string val;

                if ((val = ExtractField(t, "兼容版本")) != null)   { info.CompatibleVersions = ParseCompatibleVersions(val); continue; }
                if ((val = ExtractField(t, "目标语言")) != null)   { info.TargetLanguages = ParseCommaSeparatedValues(val); continue; }
                if ((val = ExtractField(t, "命令名称")) != null)   { info.DisplayName = val;       continue; }
                if ((val = ExtractField(t, "命令描述")) != null)   { info.Brief = val;             continue; }
                if ((val = ExtractField(t, "命令类名")) != null)   { info.ClassName = val;         continue; }
                if ((val = ExtractField(t, "触发条件")) != null)   { info.TriggerCondition = val;  continue; }
            }

            info.InputSchema = ExtractJsonSchema(section);
            info.FullDescription = ExtractDescriptionWithoutSchema(section.Trim());

            // 至少需要有命令描述才视为有效
            if (string.IsNullOrEmpty(info.Brief)) return null;

            return info;
        }

        /// <summary>兼容半角冒号（:）和全角冒号（：）。</summary>
        private static string ExtractField(string line, string fieldName)
        {
            var prefixAscii = $"- {fieldName}:";
            var prefixCjk   = $"- {fieldName}：";
            var plainAscii  = $"{fieldName}:";
            var plainCjk    = $"{fieldName}：";

            if (line.StartsWith(prefixAscii))
                return line.Substring(prefixAscii.Length).Trim();
            if (line.StartsWith(prefixCjk))
                return line.Substring(prefixCjk.Length).Trim();
            if (line.StartsWith(plainAscii))
                return line.Substring(plainAscii.Length).Trim();
            if (line.StartsWith(plainCjk))
                return line.Substring(plainCjk.Length).Trim();
            return null;
        }

        private static JObject ExtractJsonSchema(string section)
        {
            var match = Regex.Match(section, @"```json\s*([\s\S]*?)\s*```");
            if (!match.Success) return null;

            try { return JObject.Parse(match.Groups[1].Value); }
            catch { return null; }
        }

        /// <summary>移除 ```json...``` 代码块后的剩余文本，作为工具的完整 description。</summary>
        private static string ExtractDescriptionWithoutSchema(string section)
        {
            var result = Regex.Replace(section, @"```json[\s\S]*?```", string.Empty);
            result = Regex.Replace(result, @"\n{3,}", "\n\n");
            return result.Trim();
        }

        private static List<int> ParseCompatibleVersions(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var versions = new List<int>();
            var seen = new HashSet<int>();

            var normalized = raw.Replace('，', ',');
            var parts = normalized.Split(',');
            foreach (var part in parts)
            {
                var token = Regex.Replace(part ?? string.Empty, @"\s+", string.Empty);
                if (string.IsNullOrEmpty(token)) continue;

                int version;
                if (int.TryParse(token, out version) && seen.Add(version))
                {
                    versions.Add(version);
                }
            }

            return versions.Count > 0 ? versions : null;
        }

        private static List<string> ParseCommaSeparatedValues(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var result = new List<string>();
            var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            var normalized = raw.Replace('，', ',');
            var parts = normalized.Split(',');
            foreach (var part in parts)
            {
                var token = Regex.Replace(part ?? string.Empty, @"\s+", string.Empty);
                if (string.IsNullOrEmpty(token)) continue;

                if (seen.Add(token))
                    result.Add(token);
            }

            return result.Count > 0 ? result : null;
        }
    }
}
