using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Main.Core.Abstractions
{
    public interface ILogger
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
    }

    // 泛型 Logger 接口，自动带上类名
    public interface ILogger<T> : ILogger { }
}
