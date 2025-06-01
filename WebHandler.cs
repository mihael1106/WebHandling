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

        private static string _staticPath = "static";
        public static string StaticPath
        {
            get => _staticPath;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                {
                    throw new ArgumentException("The path contains invalid characters or is empty.", nameof(value));
                }

                if (!Directory.Exists(value))
                    Directory.CreateDirectory(value);
                _staticPath = value;
            }
        }

        private static HttpListener staticListener;
        private static Thread staticThread;

        private readonly Dictionary<string, Func<HttpListenerContext, ListenerResponse>> listeners;
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
            string access = isPrivate ? "localhost" : "*";

            listeners = new Dictionary<string, Func<HttpListenerContext, ListenerResponse>>();

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

        public void AddListener(string path, Func<HttpListenerContext, ListenerResponse> listener)
        {
            listeners.Add(GetPath(path), listener);
        }


        public void RemoveListener(string path)
        {
            if (listeners.ContainsKey(prefix + path))
                listeners.Remove(prefix + path);
        }

        static private void StaticWeb()
        {
            while (staticListener.IsListening)
            {
                try
                {
                    HttpListenerContext context = staticListener.GetContext();
                    new Thread(async () =>
                    {
                        string requestPath = Uri.UnescapeDataString(context.Request.Url.AbsolutePath);
                        if (requestPath[0] == '/')
                            requestPath = requestPath.Substring(1);

                        if (debug)
                            Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Got request for static path \"{requestPath}\"");

                        string fullPath = requestPath.Substring(6);                      // removes the initial static, for eg. static/some_dir/file.txt to /some_dir/file.tx
                        if(fullPath.Length >= 1)
                            if (fullPath[0] == '/')
                                fullPath = fullPath.Substring(1);                        // removes the / at the begining (if any), for eg. /some_dir/file.txt to some_dir/file.txt
                        fullPath = Path.GetFullPath(Path.Combine(StaticPath, fullPath)); // puts the static folder and gets the absolute path, eg. some_dir/file.txt to D:/Server/static/some_dir/file.txt

                        if (!fullPath.StartsWith(Path.GetFullPath(StaticPath), StringComparison.OrdinalIgnoreCase))
                        {
                            new ErrorPageBuilder().ErrorNumber(403).ExtraData("<br>Invalid request").Send(context);
                            return;
                        }

                        bool responseStarted = false;
                        try
                        {
                            Stream response;
                            if (File.Exists(fullPath))
                            {
                                string mimeType = MimeTypes.GetMimeType(Path.GetExtension(fullPath));
                                if (debug)
                                    Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Found mime type: {mimeType}");
                                context.Response.ContentType = mimeType;

                                response = File.OpenRead(fullPath);

                                string rangeHeader = context.Request.Headers["Range"];
                                if (rangeHeader != null)
                                {
                                    long fileLength = response.Length;

                                    string[] range = rangeHeader.Substring(6).Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                                    long start = long.Parse(range[0]);
                                    if (range.Length == 2)
                                        Console.WriteLine(range[1] + rangeHeader);
                                    long end = range.Length == 2 ? long.Parse(range[1]) : fileLength - 1;

                                    long partialLength = end - start + 1;
                                    response.Seek(start, SeekOrigin.Begin);
                                    context.Response.StatusCode = 206;
                                    context.Response.AddHeader("Content-Range", $"bytes {start}-{end}/{fileLength}");
                                    context.Response.ContentLength64 = partialLength;

                                    responseStarted = true;
                                    CopyStream(response, context.Response.OutputStream, partialLength);
                                    response.Close();
                                    context.Response.Close();
                                    return;
                                }
                            }
                            else if (Directory.Exists(fullPath))
                            {
                                response = new MemoryStream(new FileListBuilder(requestPath).SetDefault().Build());
                            }
                            else
                            {
                                Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Path \"{requestPath}\" does not exist.");
                                context.Response.StatusCode = 404;
                                response = new MemoryStream(Encoding.UTF8.GetBytes(new ErrorPageBuilder().ErrorNumber(404).ExtraData($"<br>Path \"{requestPath}\" does not exist").Build()));
                            }
                            using (response)
                            {
                                context.Response.ContentLength64 = response.Length;
                                await response.CopyToAsync(context.Response.OutputStream, 1048576);
                            }
                        }
                        catch (HttpListenerException ex) when (ex.ErrorCode == 64)
                        {
                        }
                        catch (IOException ex)
                        {
                            if (!responseStarted)
                                new ErrorPageBuilder().ErrorNumber(500).DefaultDebugData(ex).Send(context);
                            else
                                context.Response.Abort();

                            if (debug)
                                Console.WriteLine($"IO error during file transfer: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            if (!responseStarted)
                                new ErrorPageBuilder().ErrorNumber(500).DefaultDebugData(ex).Send(context);
                            else
                                context.Response.Abort();

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

        private static void CopyStream(Stream source, Stream target, long bytesToCopy)
        {
            byte[] buffer = new byte[65536];
            while (bytesToCopy > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, bytesToCopy);
                int bytesRead = source.Read(buffer, 0, toRead);
                if (bytesRead <= 0)
                    break;

                target.Write(buffer, 0, bytesRead);
                bytesToCopy -= bytesRead;
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
                        bool responseStarted = false;
                        try
                        {
                            if (path == "/throw" && debug)
                            {
                                throw new Exception("A debug exception has been thrown");
                            }
                            else if (listeners.ContainsKey(path))
                            {
                                if (debug)
                                    Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Found listener for \"{path}\"");

                                Stream resultStream = listeners[path]?.Invoke(context).GetResponse(context);

                                using (resultStream)
                                {
                                    if (resultStream.CanSeek)
                                        resultStream.Seek(0, SeekOrigin.Begin);
                                    context.Response.ContentLength64 = resultStream.Length;
                                    responseStarted = true;
                                    resultStream.CopyTo(context.Response.OutputStream, 65536);
                                }

                                context.Response.OutputStream.Flush();
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
                            if (debug)
                                Console.WriteLine(ex.ToString());

                            if (!responseStarted)
                                new ErrorPageBuilder().ErrorNumber(500).DefaultDebugData(ex).Send(context);
                            else
                                context.Response.Abort();
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