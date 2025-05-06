using Miki1106.WebHandling.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Miki1106.WebHandling
{
    public class WebHandler
    {
        private readonly HttpListener webListener;
        public static bool debug = false;

        private static HttpListener staticListener;
        private static Thread staticThread;

        private readonly Dictionary<string, Func<HttpListenerContext, Stream>> streamListeners;
        private readonly Dictionary<string, Func<HttpListenerContext, byte[]>> byteListeners;
        private readonly string prefix;

        public WebHandler(string prefix) : this(prefix, 80, false)
        {
        }

        public WebHandler(string prefix, ushort port) : this(prefix, port, false)
        {
        }

        public WebHandler(string prefix, bool isPrivate) : this(prefix, 80, isPrivate)
        {
        }


        public WebHandler(string prefix, ushort port, bool isPrivate)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "static");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string access = isPrivate ? "localhost" : "*";

            streamListeners = new Dictionary<string, Func<HttpListenerContext, Stream>>();
            byteListeners = new Dictionary<string, Func<HttpListenerContext, byte[]>>();

            webListener = new HttpListener();
            if (staticListener == null)
            {
                staticListener = new HttpListener();
                staticListener.Prefixes.Add($"http://{access}:{port}/static/");
            }

            if (prefix == "")
            {
                webListener.Prefixes.Add($"http://{access}:{port}/{prefix}");
                this.prefix = "/";
            }
            else
            {
                if (prefix[prefix.Length - 1] == '/')
                {
                    if (prefix[0] == '/')
                        prefix = prefix.Substring(1);
                    webListener.Prefixes.Add($"http://{access}:{port}/{prefix}");
                    this.prefix = "/" + prefix;
                }
                else
                {
                    if (prefix[0] == '/')
                        prefix = prefix.Substring(1);
                    webListener.Prefixes.Add($"http://{access}:{port}/{prefix}/");
                    this.prefix = "/" + prefix + "/";
                }
            }

            staticListener.Start();

            if (staticThread == null || staticThread.ThreadState != System.Threading.ThreadState.Running)
            {
                staticThread = new Thread(StaticWeb);
                staticThread.Start();
            }

            webListener.Start();
            Thread thread = new Thread(Web)
            {
                IsBackground = true
            };
            thread.Start();
        }

        internal string GetPath(string path)
        {
            if (path.Length != 0)
                if (path[0] == '/')
                    path = path.Substring(1);
            string final = prefix + path;
            if (final.Length >= 2)
                if (final[final.Length - 1] == '/')
                    final = final.Remove(final.Length - 1, 1);
            return final;
        }

        public void AddListener(string path, Func<HttpListenerContext, Stream> listener)
        {
            streamListeners.Add(GetPath(path), listener);
        }

        public void AddListener(string path, Func<HttpListenerContext, byte[]> listener)
        {
            byteListeners.Add(GetPath(path), listener);
        }


        public void RemoveListener(string path)
        {
            if (streamListeners.ContainsKey(prefix + path))
                streamListeners.Remove(prefix + path);
            if (byteListeners.ContainsKey(prefix + path))
                byteListeners.Remove(prefix + path);
        }

        static private void StaticWeb()
        {
            while (staticListener.IsListening)
            {
                try
                {
                    HttpListenerContext context = staticListener.GetContext();
                    new Thread(() =>
                    {
                        string requestPath = context.Request.Url.AbsolutePath;
                        if (requestPath[0] == '/')
                            requestPath = requestPath.Substring(1);
                        if(debug)
                            Console.WriteLine($"Got request for static path \"{requestPath}\"");
                        requestPath = Uri.UnescapeDataString(requestPath);
                        string fullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), requestPath));

                        if (!fullPath.StartsWith(Path.Combine(Directory.GetCurrentDirectory(), "static"), StringComparison.OrdinalIgnoreCase))
                        {
                            context.Response.StatusCode = 403;
                            return;
                        }

                        Stream response = null;
                        try
                        {
                            if (File.Exists(requestPath))
                            {
                                switch (Path.GetExtension(requestPath).ToLower())
                                {
                                    case ".png":
                                        context.Response.AddHeader("Content-Type", "image/png");
                                        break;
                                    case ".gif":
                                        context.Response.AddHeader("Content-Type", "image/gif");
                                        break;
                                    case ".jpg":
                                        context.Response.AddHeader("Content-Type", "image/jpeg");
                                        break;
                                    case ".jepg":
                                        context.Response.AddHeader("Content-Type", "image/jpeg");
                                        break;
                                    case ".ico":
                                        context.Response.AddHeader("Content-Type", "image/x-icon");
                                        break;
                                    case ".mp4":
                                        context.Response.AddHeader("Content-Type", "video/mp4");
                                        break;
                                    case ".mov":
                                        context.Response.AddHeader("Content-Type", "video/mp4");
                                        break;
                                    default:
                                        Console.WriteLine("Couldnt find extension for type " + Path.GetExtension(requestPath).ToLower());
                                        break;
                                }

                                response = File.OpenRead(requestPath);
                            }
                            else if (Directory.Exists(requestPath))
                            {
                                if (requestPath[requestPath.Length - 1] == '/')
                                    requestPath = requestPath.Remove(requestPath.Length - 1);
                                string html = Resources.static_base.Replace("{local_path}", requestPath);

                                requestPath += "/";

                                string list = "";
                                foreach (string str in Directory.GetDirectories(requestPath))
                                {
                                    DirectoryInfo info = new DirectoryInfo(str);
                                    list += GenDir(requestPath, info);
                                }
                                foreach (string str in Directory.GetFiles(requestPath))
                                {
                                    FileInfo fileInfo = new FileInfo(str);
                                    list += GenFile(requestPath, fileInfo);
                                }
                                html = html.Replace("{list}", list).Replace("{display}", requestPath != "static/" ? "block" : "none");

                                response = new MemoryStream(Encoding.UTF8.GetBytes(html));
                            }
                            else
                            {
                                Console.WriteLine($"[{DateTime.Now}] [{context.Request.RemoteEndPoint.Address}] Path \"{requestPath}\" does not exist.");
                                context.Response.StatusCode = 404;
                                response = new MemoryStream(Encoding.UTF8.GetBytes(new ErrorPageBuilder().ErrorNumber(404).ExtraData($"<br>Path \"{requestPath}\" does not exist").Build()));
                            }
                            using (response)
                            {
                                context.Response.ContentLength64 = response.Length;
                                response.CopyTo(context.Response.OutputStream);
                            }
                        }
                        catch (Exception ex)
                        {
                            string debugData = $"<br>Exception occured: {WebUtility.HtmlEncode(ex.ToString())}<br><br>Time: {DateTime.Now:dd.MM.yyyy. HH:mm:ss}<br><h2>Call stack:</h2> <pre><code><div class=\"code-box\">{WebUtility.HtmlEncode(GetStackTrace())}</div></code></pre>";
                            new ErrorPageBuilder().ErrorNumber(500).DebugData(debugData).Send(context);
                            response?.Close();
                            if (debug)
                                Console.WriteLine(ex.ToString());
                            else
                                Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            context.Response.Close();
                        }
                    })
                    {
                        IsBackground = true
                    }.Start();
                }
                catch (Exception e)
                {
                    if (debug)
                        Console.WriteLine(e.ToString());
                    else
                        Console.WriteLine(e.Message);
                }
            }
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

        private void Web()
        {
            while (webListener.IsListening)
            {
                try
                {
                    HttpListenerContext context = webListener.GetContext();
                    new Thread(() =>
                    {
                        string path = context.Request.Url.AbsolutePath;
                        Stream stream = null;
                        try
                        {
                            if (path == "/throw" && debug)
                            {
                                throw new Exception("A debug exception has been thrown");
                            }
                            else if (streamListeners.ContainsKey(path))
                            {
                                stream = streamListeners[path]?.Invoke(context);
                                stream?.CopyTo(context.Response.OutputStream, 65536);
                                stream.Flush();
                                context.Response.OutputStream.Flush();
                                stream.Close();
                                context.Response.Close();
                            }
                            else if (byteListeners.ContainsKey(path))
                            {
                                byte[] response = byteListeners[path]?.Invoke(context);
                                context.Response.OutputStream.Write(response, 0, response.Length);
                                context.Response.Close();
                            }
                            else
                            {
                                if (debug)
                                    Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Path \"{path}\" does not exist.");
                                new ErrorPageBuilder().ErrorNumber(404).ExtraData($"<br>Path \"{path}\" does not exist.").Send(context);
                            }
                        }
                        catch (Exception ex)
                        {
                            stream?.Close();
                            if (debug)
                                Console.WriteLine(ex.ToString());
                            string debugData = $"<br>Exception occured: {WebUtility.HtmlEncode(ex.ToString())}<br><br>Time: {DateTime.Now:dd.MM.yyyy. HH:mm:ss}<br><h2>Call stack:</h2> <pre><code><div class=\"code-box\">{WebUtility.HtmlEncode(GetStackTrace())}</div></code></pre>";
                            new ErrorPageBuilder().ErrorNumber(500).DebugData(debugData).Send(context);
                        }
                    })
                    {
                        IsBackground = true
                    }.Start();
                }
                catch (Exception ex)
                {
                    if (debug)
                        Console.WriteLine(ex.ToString());
                    else
                        Console.WriteLine(ex.Message);
                }
            }
        }

        public static string GetStackTrace()
        {
            StackTrace frame = new StackTrace(true);
            return frame.ToString();
        }

        public void Stop()
        {
            webListener.Stop();
            staticListener.Stop();
        }
    }
}