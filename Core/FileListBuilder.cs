using Miki1106.WebHandling.Properties;
using System.IO;
using System;
using System.Text;

namespace Miki1106.WebHandling.Core
{
    internal class FileListBuilder
    {
        private static readonly string template = Resources.static_base;
        private string path;
        private bool showParentDirectory = false;
        private string[] directories = { };
        private string[] files = { };

        public FileListBuilder(string path)
        {
            if (path[path.Length - 1] == '/')
                path = path.Remove(path.Length - 1);
            this.path = path;
        }

        public byte[] Build()
        {
            string html = template.Replace("{local_path}", path);

            path += "/";

            string list = "";
            foreach (string str in directories)
            {
                DirectoryInfo info = new DirectoryInfo(str);
                list += GenDir(path, info);
            }
            foreach (string str in files)
            {
                FileInfo fileInfo = new FileInfo(str);
                list += GenFile(path, fileInfo);
            }
            return Encoding.UTF8.GetBytes(html.Replace("{list}", list).Replace("{display}", showParentDirectory ? "block" : "none"));
        }

        public FileListBuilder SetDefault()
        {
            string noSlash = path.Substring(6);
            if(noSlash.StartsWith("/"))
                noSlash = noSlash.Substring(1);
            directories = Directory.GetDirectories(Path.GetFullPath(Path.Combine(StaticHandler.StaticPath, noSlash)));
            files = Directory.GetFiles(Path.GetFullPath(Path.Combine(StaticHandler.StaticPath, noSlash)));
            showParentDirectory = path != "static";
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

        private static string GenDir(string requestPath, DirectoryInfo info)
        {
            string name = info.Name;
            long created = ((DateTimeOffset)info.LastWriteTime).ToUnixTimeSeconds();
            return Resources.item_folder.Replace("{path}", requestPath + name)
                .Replace("{name}", name)
                .Replace("{created}", created.ToString())
                .Replace("{created_str}", info.LastWriteTime.ToString("dd/MM/yy, H:mm:ss"));
        }

        private static string GenFile(string requestPath, FileInfo info)
        {
            string name = info.Name;
            long created = ((DateTimeOffset)info.LastWriteTime).ToUnixTimeSeconds();
            string item = Resources.item_file.Replace("{path}", requestPath + name)
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
