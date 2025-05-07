using Miki1106.WebHandling.Core;
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
        private static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {".png", "image/png"},
            {".gif", "image/gif"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".ico", "image/x-icon"},
            {".bmp", "image/bmp"},
            {".webp", "image/webp"},
            {".svg", "image/svg+xml"},

            {".mp3", "audio/mpeg"},
            {".mp4", "video/mp4"},
            {".avi", "video/x-msvideo"},
            {".mov", "video/quicktime"},

            {".txt", "text/plain"},
            {".html", "text/html"},
            {".htm", "text/html"},
            {".css", "text/css"},

            {".js", "application/javascript"},
            {".json", "application/json"},
            {".xml", "application/xml"},

            {".pdf", "application/pdf"},
            {".zip", "application/zip"},
            {".rar", "application/x-rar-compressed"},
            {".7z", "application/x-7z-compressed"},

            {".doc", "application/msword"},
            {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
            {".xls", "application/vnd.ms-excel"},
            {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            {".ppt", "application/vnd.ms-powerpoint"},
            {".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation"},
        };

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
                        if (debug)
                            Console.WriteLine($"Got request for static path \"{requestPath}\"");
                        requestPath = Uri.UnescapeDataString(requestPath);
                        string fullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), requestPath));

                        if (!fullPath.StartsWith(Path.Combine(Directory.GetCurrentDirectory(), "static"), StringComparison.OrdinalIgnoreCase))
                        {
                            new ErrorPageBuilder().ErrorNumber(403).ExtraData("<br>Invalid request").Send(context);
                            return;
                        }

                        Stream response = null;
                        try
                        {
                            if (File.Exists(requestPath))
                            {
                                context.Response.ContentType = MimeTypes.TryGetValue(Path.GetExtension(requestPath).ToLower(), out string mime) ? mime : "application/octet-stream";
                                response = File.OpenRead(requestPath);
                            }
                            else if (Directory.Exists(requestPath))
                            {
                                response = new MemoryStream(new FileListBuilder(requestPath).SetDefault().Build());
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
                            new ErrorPageBuilder().ErrorNumber(500).DefaultDebugData(ex).Send(context);
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
                                context.Response.StatusCode = 404;
                                new ErrorPageBuilder().ErrorNumber(404).ExtraData($"<br>Path \"{path}\" does not exist.").Send(context);
                            }
                        }
                        catch (Exception ex)
                        {
                            stream?.Close();
                            if (debug)
                                Console.WriteLine(ex.ToString());
                            new ErrorPageBuilder().ErrorNumber(500).DefaultDebugData(ex).Send(context);
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