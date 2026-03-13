using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Main.Core.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace Main.CommandSet.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ExecuteCommand : IExternalCommand
    {
        public static string AssemblyPath;
        public static string FullClassName;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                //1. Load the assembly from the specified path.
                Assembly assembly = Assembly.LoadFrom(AssemblyPath);

                //2. Find the specified type (class) within the loaded assembly.
                Type commandType = assembly.GetType(FullClassName);
                if (commandType == null)
                {
                    TaskDialog.Show("Error", $"Could not find the class '{FullClassName}' in the assembly '{AssemblyPath}'.");
                    return Result.Failed;
                }

                //3. Create an instance of the command class.
                object commandInstance = Activator.CreateInstance(commandType);
                if (!(commandInstance is IExternalCommand externalCommand))
                {
                    TaskDialog.Show("Error", $"The class '{FullClassName}' does not implement the IExternalCommand interface.");
                    return Result.Failed;
                }

                //4. Inject the parameters (if the Parameters property exists)
                InjectParametersIfPresent(commandInstance, commandType);

                //5. Execute the command.
                message = string.Empty;
                elements = new ElementSet();
                var result = externalCommand.Execute(commandData, ref message, elements);
                return result;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Command Execution Error", $"An exception occurred while trying to execute the command from '{AssemblyPath}'.\n\n{ex}");
                return Result.Failed;
            }
            finally
            {
                // 清理参数存储，避免后续命令污染
                CommandStorageService.ClearCommandParams();
            }

        }

        public static void InjectParametersIfPresent(object commandInstance, Type commandType)
        {
            try
            {
                var prop = commandType.GetProperty("Parameters", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (prop == null || !prop.CanWrite) return; // 没有属性或不可写则跳过

                JObject stored = CommandStorageService.GetCommandParams();
                if (stored == null) return; // 无存储参数

                var targetType = prop.PropertyType;
                object valueToAssign = null;

                if (targetType == typeof(JObject) || targetType == typeof(object))
                {
                    valueToAssign = stored;
                }
                else if (targetType == typeof(string))
                {
                    valueToAssign = stored.ToString();
                }
                else
                {
                    // 尝试转换为目标类型（需要公共的无参构造或JObject匹配）
                    try
                    {
                        valueToAssign = stored.ToObject(targetType);
                    }
                    catch
                    {
                        // 回退为字符串
                        valueToAssign = stored.ToString();
                    }
                }

                prop.SetValue(commandInstance, valueToAssign, null);
            }
            catch (Exception ex)
            {
                // 参数注入失败不阻止命令执行，只提示
                TaskDialog.Show("Parameter Injection Warning", $"Failed to inject parameters: {ex.Message}");
            }
        }
    }
}
