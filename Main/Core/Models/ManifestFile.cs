using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Main.Core.Models
{
    internal class ManifestFile
    {
        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ManifestFile()
        {
            local = false;
            applications = new List<AddinItem>();
            commands = new List<AddinItem>();
        }

        /// <summary>
        /// 带文件名参数的构造函数
        /// </summary>
        /// <param name="fileName">清单文件名</param>
        public ManifestFile(string fileName) : this()
        {
            this.fileName = fileName;
            if (string.IsNullOrEmpty(filePath))
            {
                var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "AddIn");
                filePath = Path.Combine(path, this.fileName);
            }
        }

        /// <summary>
        /// 带local标志的构造函数
        /// </summary>
        /// <param name="local">是否为本地清单</param>
        public ManifestFile(bool local) : this()
        {
            this.local = local;
        }

        /// <summary>
        /// 从文件路径加载并解析清单文件
        /// </summary>
        public void Load()
        {
            xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            var documentElement = xmlDoc.DocumentElement;
            if (!documentElement.Name.Equals(ROOT_NODE))
            {
                throw new ArgumentException(INCORRECT_NODE);
            }
            if (documentElement.ChildNodes.Count == 0)
            {
                throw new ArgumentException(EMPTY_ADDIN);
            }
            applications.Clear();
            commands.Clear();
            foreach (var obj in documentElement.ChildNodes)
            {
                var xmlNode = (XmlNode)obj;
                if (!xmlNode.Name.Equals(ADDIN_NODE) || xmlNode.Attributes.Count != 1)
                {
                    throw new ArgumentException(INCORRECT_NODE);
                }
                var xmlAttribute = xmlNode.Attributes[0];
                if (xmlAttribute.Value.Equals(APPLICATION_NODE))
                {
                    ParseExternalApplications(xmlNode);
                }
                else
                {
                    if (!xmlAttribute.Value.Equals(COMMAND_NODE))
                    {
                        throw new ArgumentException(INCORRECT_NODE);
                    }
                    ParseExternalCommands(xmlNode);
                }
            }
        }

        /// <summary>
        /// 保存清单到当前文件路径
        /// </summary>
        public void Save()
        {
            SaveAs(filePath);
        }

        /// <summary>
        /// 将清单另存为到指定路径
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        public void SaveAs(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(FILENAME_NULL_OR_EMPTY);
            }
            if (!filePath.ToLower().EndsWith(DefaultSetting.FormatExAddin))
            {
                throw new ArgumentException(FILENAME_INCORRECT_WARNING + filePath);
            }
            var directoryName = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            var fileInfo = new FileInfo(filePath);
            xmlDoc = new XmlDocument();
            CreateXmlForManifest();
            if (File.Exists(filePath))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }
            TextWriter w = new StreamWriter(filePath, false, Encoding.UTF8);
            var xmlTextWriter = new XmlTextWriter(w);
            xmlTextWriter.Formatting = Formatting.Indented;
            xmlDoc.Save(xmlTextWriter);
            xmlTextWriter.Close();
            this.filePath = fileInfo.FullName;
            fileName = Path.GetFileName(fileInfo.FullName);
        }

        /// <summary>
        /// 获取或设置清单文件名
        /// </summary>
        public string FileName
        {
            get => fileName;
            set => fileName = value;
        }

        /// <summary>
        /// 获取或设置是否为本地清单
        /// </summary>
        public bool Local
        {
            get => local;
            set => local = value;
        }

        private string _vendorDescription;

        /// <summary>
        /// 获取或设置供应商描述
        /// </summary>
        public string VendorDescription
        {
            get => _vendorDescription;
            set => _vendorDescription = value;
        }

        /// <summary>
        /// 获取或设置清单文件的完整路径
        /// </summary>
        public string FilePath
        {
            get
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "AddIn");
                    filePath = Path.Combine(path, DefaultSetting.AimInternalName);
                }
                return filePath;
            }
            set => filePath = value;
        }

        /// <summary>
        /// 获取或设置应用程序列表
        /// </summary>
        public List<AddinItem> Applications
        {
            get => applications;
            set => applications = value;
        }

        /// <summary>
        /// 获取或设置命令列表
        /// </summary>
        public List<AddinItem> Commands
        {
            get => commands;
            set => commands = value;
        }

        /// <summary>
        /// 为清单内容创建XML文档结构
        /// </summary>
        /// <returns>构建的XML文档</returns>
        private XmlDocument CreateXmlForManifest()
        {
            var xmlNode = xmlDoc.AppendChild(xmlDoc.CreateElement(ROOT_NODE));
            foreach (var currentApp in applications)
            {
                var xmlElement = xmlDoc.CreateElement(ADDIN_NODE);
                xmlElement.SetAttribute(TYPE_ATTRIBUTE, APPLICATION_NODE);
                xmlNode.AppendChild(xmlElement);
                AddApplicationToXmlElement(xmlElement, currentApp);
                var xmlElement2 = xmlDoc.CreateElement(VENDORID);
                xmlElement2.InnerText = "ADSK";
                xmlElement.AppendChild(xmlElement2);
                xmlElement2 = xmlDoc.CreateElement(VENDORDESCRIPTION);
                xmlElement2.InnerText = "Autodesk, www.autodesk.com";
                xmlElement.AppendChild(xmlElement2);
            }
            foreach (var command in commands)
            {
                var xmlElement3 = xmlDoc.CreateElement(ADDIN_NODE);
                xmlElement3.SetAttribute(TYPE_ATTRIBUTE, COMMAND_NODE);
                xmlNode.AppendChild(xmlElement3);
                AddCommandToXmlElement(xmlElement3, command);
                var xmlElement4 = xmlDoc.CreateElement(VENDORID);
                xmlElement4.InnerText = "ADSK";
                xmlElement3.AppendChild(xmlElement4);
                xmlElement4 = xmlDoc.CreateElement(VENDORDESCRIPTION);
                if (VendorDescription == string.Empty) xmlElement4.InnerText = "Autodesk, www.autodesk.com";
                else xmlElement4.InnerText = VendorDescription;
                xmlElement3.AppendChild(xmlElement4);
            }
            return xmlDoc;
        }

        /// <summary>
        /// 将AddinItem的通用属性添加到XML元素中
        /// </summary>
        /// <param name="xmlEle">XML元素</param>
        /// <param name="addinItem">插件项</param>
        private void AddAddInItemToXmlElement(XmlElement xmlEle, AddinItem addinItem)
        {
            if (!string.IsNullOrEmpty(addinItem.AssemblyPath))
            {
                var xmlElement = xmlDoc.CreateElement(ASSEMBLY);
                if (local)
                {
                    xmlElement.InnerText = addinItem.AssemblyName;
                }
                else
                {
                    xmlElement.InnerText = addinItem.AssemblyPath;
                }
                xmlEle.AppendChild(xmlElement);
            }
            if (!string.IsNullOrEmpty(addinItem.ClientIdString))
            {
                var xmlElement2 = xmlDoc.CreateElement(CLIENTID);
                xmlElement2.InnerText = addinItem.ClientIdString;
                xmlEle.AppendChild(xmlElement2);
            }
            if (!string.IsNullOrEmpty(addinItem.FullClassName))
            {
                var xmlElement3 = xmlDoc.CreateElement(FULLCLASSNAME);
                xmlElement3.InnerText = addinItem.FullClassName;
                xmlEle.AppendChild(xmlElement3);
            }
        }

        /// <summary>
        /// 将应用程序插件项添加到XML元素中
        /// </summary>
        /// <param name="appEle">应用程序的XML元素</param>
        /// <param name="currentApp">当前的应用程序插件项</param>
        private void AddApplicationToXmlElement(XmlElement appEle, AddinItem currentApp)
        {
            if (!string.IsNullOrEmpty(currentApp.Name))
            {
                var xmlElement = xmlDoc.CreateElement(NAME_NODE);
                xmlElement.InnerText = currentApp.Name;
                appEle.AppendChild(xmlElement);
            }
            AddAddInItemToXmlElement(appEle, currentApp);
        }

        /// <summary>
        /// 将命令插件项添加到XML元素中
        /// </summary>
        /// <param name="commandEle">命令的XML元素</param>
        /// <param name="command">命令插件项</param>
        private void AddCommandToXmlElement(XmlElement commandEle, AddinItem command)
        {
            AddAddInItemToXmlElement(commandEle, command);
            XmlElement xmlElement;
            if (!string.IsNullOrEmpty(command.Name))
            {
                xmlElement = xmlDoc.CreateElement(TEXT);
                xmlElement.InnerText = command.Name;
                commandEle.AppendChild(xmlElement);
            }
            if (!string.IsNullOrEmpty(command.Description))
            {
                xmlElement = xmlDoc.CreateElement(DESCRIPTION);
                xmlElement.InnerText = command.Description;
                commandEle.AppendChild(xmlElement);
            }
            var text = command.VisibilityMode.ToString();
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Replace(",", " |");
            }
            xmlElement = xmlDoc.CreateElement(VISIBILITYMODE);
            xmlElement.InnerText = text;
            commandEle.AppendChild(xmlElement);
        }

        /// <summary>
        /// 解析XML节点以创建外部应用程序项
        /// </summary>
        /// <param name="nodeApplication">应用程序的XML节点</param>
        private void ParseExternalApplications(XmlNode nodeApplication)
        {
            var addinItem = new AddinItem(AddinType.Application);
            ParseApplicationItems(addinItem, nodeApplication);
            applications.Add(addinItem);
        }

        /// <summary>
        /// 解析XML节点以创建外部命令项
        /// </summary>
        /// <param name="nodeCommand">命令的XML节点</param>
        private void ParseExternalCommands(XmlNode nodeCommand)
        {
            var addinItem = new AddinItem(AddinType.Command);
            ParseCommandItems(addinItem, nodeCommand);
            commands.Add(addinItem);
        }

        /// <summary>
        /// 解析应用程序插件项的特定属性
        /// </summary>
        /// <param name="addinApp">应用程序插件项</param>
        /// <param name="nodeAddIn">插件的XML节点</param>
        private void ParseApplicationItems(AddinItem addinApp, XmlNode nodeAddIn)
        {
            ParseAddInItem(addinApp, nodeAddIn);
            var xmlElement = nodeAddIn[NAME_NODE];
            if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.InnerText))
            {
                addinApp.Name = xmlElement.InnerText;
            }
        }

        /// <summary>
        /// 解析命令插件项的特定属性
        /// </summary>
        /// <param name="command">命令插件项</param>
        /// <param name="nodeAddIn">插件的XML节点</param>
        private void ParseCommandItems(AddinItem command, XmlNode nodeAddIn)
        {
            ParseAddInItem(command, nodeAddIn);
            var xmlElement = nodeAddIn[TEXT];
            if (xmlElement != null)
            {
                command.Name = xmlElement.InnerText;
            }
            xmlElement = nodeAddIn[DESCRIPTION];
            if (xmlElement != null)
            {
                command.Description = xmlElement.InnerText;
            }
            xmlElement = nodeAddIn[VISIBILITYMODE];
            if (xmlElement != null && !string.IsNullOrEmpty(xmlElement.InnerText))
            {
                command.VisibilityMode = ParseVisibilityMode(xmlElement.InnerText);
            }
        }

        /// <summary>
        /// 解析AddinItem的通用属性
        /// </summary>
        /// <param name="addinItem">插件项</param>
        /// <param name="nodeAddIn">插件的XML节点</param>
        private void ParseAddInItem(AddinItem addinItem, XmlNode nodeAddIn)
        {
            var xmlElement = nodeAddIn[ASSEMBLY];
            if (xmlElement != null)
            {
                if (local)
                {
                    addinItem.AssemblyName = xmlElement.InnerText;
                }
                else
                {
                    addinItem.AssemblyPath = xmlElement.InnerText;
                }
            }
            xmlElement = nodeAddIn[CLIENTID];
            if (xmlElement != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(xmlElement.InnerText))
                    {
                        addinItem.ClientId = new Guid(xmlElement.InnerText);
                    }
                    else
                    {
                        addinItem.ClientId = Guid.Empty;
                    }
                }
                catch (Exception)
                {
                    addinItem.ClientId = Guid.Empty;
                    addinItem.ClientIdString = xmlElement.InnerText;
                }
            }
            xmlElement = nodeAddIn[FULLCLASSNAME];
            if (xmlElement != null)
            {
                addinItem.FullClassName = xmlElement.InnerText;
            }
        }

        /// <summary>
        /// 解析字符串格式的可见性模式
        /// </summary>
        /// <param name="visibilityModeString">可见性模式字符串</param>
        /// <returns>VisibilityMode枚举</returns>
        private VisibilityMode ParseVisibilityMode(string visibilityModeString)
        {
            var visibilityMode = VisibilityMode.AlwaysVisible;
            VisibilityMode result;
            try
            {
                var text = "|";
                var separator = text.ToCharArray();
                var array = visibilityModeString.Replace(" | ", "|").Split(separator);
                foreach (var value in array)
                {
                    var visibilityMode2 = (VisibilityMode)Enum.Parse(typeof(VisibilityMode), value);
                    visibilityMode |= visibilityMode2;
                }
                result = visibilityMode;
            }
            catch (Exception)
            {
                throw new ArgumentException(UNKNOW_VISIBILITYMODE);
            }
            return result;
        }

        /// <summary>
        /// 获取文件的完整路径
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件的完整路径</returns>
        private string getFullPath(string fileName)
        {
            FileInfo fileInfo = null;
            try
            {
                fileInfo = new FileInfo(fileName);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(fileName + Environment.NewLine + ex.ToString());
            }
            return fileInfo.FullName;
        }

        private string fileName;

        private bool local;

        private string filePath;

        private List<AddinItem> applications;

        private List<AddinItem> commands;

        private string ROOT_NODE = "RevitAddIns";

        private string ADDIN_NODE = "AddIn";

        private string APPLICATION_NODE = "Application";

        private string COMMAND_NODE = "Command";

        private string TYPE_ATTRIBUTE = "Type";

        private string INCORRECT_NODE = "incorrect node in addin file!";

        private string EMPTY_ADDIN = "empty addin file!";

        private string ASSEMBLY = "Assembly";

        private string CLIENTID = "ClientId";

        private string FULLCLASSNAME = "FullClassName";

        private string NAME_NODE = "Name";

        private string TEXT = "Text";

        private string DESCRIPTION = "Description";

        private string VENDORID = "VendorId";

        private string VENDORDESCRIPTION = "VendorDescription";

        private string VISIBILITYMODE = "VisibilityMode";

        private string UNKNOW_VISIBILITYMODE = "Unrecognizable VisibilityMode!";

        private string FILENAME_INCORRECT_WARNING = "File name is incorrect, not .addin file .";

        private string FILENAME_NULL_OR_EMPTY = "File name for RevitAddInManifest is null or empty";

        private XmlDocument xmlDoc;

    }
}
