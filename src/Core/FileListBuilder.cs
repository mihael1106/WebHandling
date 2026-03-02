using Miki1106.WebHandling.Properties;
using System.IO;
using System;
using System.Text;
using System.Net;
using System.Linq;

namespace Miki1106.WebHandling.Core
{
    public class FileListBuilder : ListenerResponse
    {
        private static readonly string template = Resources.static_base;

        private string httpPath;
        private string basePath;
        private string extraPath;
        private string fullPath;

        private bool showParentDirectory = false;
        private string[] directories = { };
        private string[] files = { };

        public FileListBuilder(string httpPath, string basePath, string extraPath)
        {
            if (basePath.Length > 0)
                if (basePath[basePath.Length - 1] == '/')
                    basePath = basePath.Remove(basePath.Length - 1);
            if (extraPath.StartsWith("/"))
                extraPath = extraPath.Substring(1);

            this.httpPath = httpPath;
            this.basePath = basePath;
            this.extraPath = extraPath;
            fullPath = Path.Combine(basePath, extraPath);
        }

        public byte[] Build()
        {
            string html = template.Replace("{local_path}", Path.Combine(httpPath, extraPath));

            string list = "";
            foreach (string str in directories)
            {
                DirectoryInfo info = new DirectoryInfo(str);
                list += GenDir(info);
            }
            foreach (string str in files)
            {
                FileInfo fileInfo = new FileInfo(str);
                list += GenFile(fileInfo);
            }
            return Encoding.UTF8.GetBytes(html.Replace("{list}", list).Replace("{display}", showParentDirectory ? "block" : "none"));
        }

        public override Stream GetResponse(HttpListenerContext context)
        {
            context.Response.ContentType = "text/html";
            return new MemoryStream(Build());
        }

        public FileListBuilder SetDefault()
        {

            directories = Directory.GetDirectories(fullPath).OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToArray();
            files = Directory.GetFiles(fullPath).OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToArray();
            showParentDirectory = extraPath != "";
            return this;
        }

        public FileListBuilder SetDirectories(string[] directories)
        {
            this.directories = directories;
            return this;
        }

        public FileListBuilder SetFiles(string[] files)
        {
            this.files = files;
            return this;
        }

        public FileListBuilder ShowParentDirectory(bool showParentDirectory)
        {
            this.showParentDirectory = showParentDirectory;
            return this;
        }

        private string GenDir(DirectoryInfo info)
        {
            string name = info.Name;
            long created = ((DateTimeOffset)info.LastWriteTime).ToUnixTimeSeconds();
            return Resources.item_folder.Replace("{path}", Path.Combine(httpPath, extraPath, name))
                .Replace("{name}", name)
                .Replace("{created}", created.ToString())
                .Replace("{created_str}", info.LastWriteTime.ToString("dd/MM/yy, H:mm:ss"));
        }

        private string GenFile(FileInfo info)
        {
            string name = info.Name;
            long created = ((DateTimeOffset)info.LastWriteTime).ToUnixTimeSeconds();
            string item = Resources.item_file.Replace("{path}", Path.Combine(httpPath, extraPath, name))
                .Replace("{name}", name)
                .Replace("{created}", created.ToString())
                .Replace("{created_str}", info.LastWriteTime.ToString("dd/MM/yy, H:mm:ss"))
                .Replace("{size}", info.Length.ToString());

            string[] sizes = { "B", "kB", "MB", "GB", "TB" };
            double len = info.Length;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            string result = string.Format("{0:0.##} {1}", len, sizes[order]);
            return item.Replace("{size_str}", result);
        }
    }
}
