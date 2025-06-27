using Miki1106.WebHandling.Core;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static Miki1106.WebHandling.Core.Utils;

namespace Miki1106.WebHandling
{
    public static class StaticHandler
    {
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

        public static event EventHandler<RequestInfo> OnRequestFinished;

        private static HttpListener staticListener;
        private static Thread staticThread;

        internal static void Start()
        {
            if (staticListener == null)
            {
                staticListener = new HttpListener();
                staticListener.Prefixes.Add($"http://*:80/static/");
                staticListener.Start();
            }
            if (staticThread == null)
            {
                staticThread = new Thread(StaticWeb)
                {
                    Name = "Static Listener thread #0"
                };
                staticThread.Start();
            }
            else if (staticThread.ThreadState != ThreadState.Running)
            {
                staticThread.Start();
            }
        }

        internal static void Stop()
        {
            staticListener?.Stop();
            staticThread?.Join();
        }

        private async static void StaticWeb()
        {
            while (staticListener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await staticListener.GetContextAsync();
                    _ = Task.Run(async () =>
                    {
                        long start = DateTime.Now.Ticks;
                        string requestPath = Uri.UnescapeDataString(context.Request.Url.AbsolutePath);
                        if (requestPath[0] == '/')
                            requestPath = requestPath.Substring(1);

                        if (WebHandler.debug)
                            Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Got request for static path \"{requestPath}\"");

                        string fullPath = requestPath.Substring(6);                      // removes the initial static, for eg. static/some_dir/file.txt to /some_dir/file.tx
                        if (fullPath.Length >= 1)
                            if (fullPath[0] == '/')
                                fullPath = fullPath.Substring(1);                        // removes the / at the begining (if any), for eg. /some_dir/file.txt to some_dir/file.txt
                        fullPath = Path.GetFullPath(Path.Combine(StaticPath, fullPath)); // puts the static folder and gets the absolute path, eg. some_dir/file.txt to D:/Server/static/some_dir/file.txt

                        long totalSent = 0;
                        long totalReceived = context.Request.ContentLength64;
                        int statusCode = 500;
                        string method = context.Request.HttpMethod;
                        IPEndPoint iPEndPoint = context.Request.RemoteEndPoint;
                        if (!fullPath.StartsWith(Path.GetFullPath(StaticPath), StringComparison.OrdinalIgnoreCase))
                        {
                            await Send(context, new ErrorPage(403, "<br>Invalid request"), add => totalSent += add, status => statusCode = status, 1048576);
                            return;
                        }


                        bool responseStarted = false;
                        try
                        {
                            Stream response;
                            if (File.Exists(fullPath))
                            {
                                string mimeType = MimeTypes.GetMimeType(Path.GetExtension(fullPath));
                                if (WebHandler.debug)
                                    Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Found mime type: {mimeType}");
                                context.Response.ContentType = mimeType;

                                response = File.OpenRead(fullPath);

                                string rangeHeader = context.Request.Headers["Range"];
                                if (rangeHeader != null)
                                {
                                    responseStarted = true;
                                    await HandleRange(rangeHeader, response, context, add => totalSent += add, status => statusCode = status);
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
                                response = new ErrorPage(404, $"<br>Path \"{requestPath}\" does not exist").GetResponse();
                            }
                            using (response)
                            {
                                if (response.CanSeek)
                                    response.Seek(0, SeekOrigin.Begin);
                                context.Response.ContentLength64 = response.Length - response.Position;
                                responseStarted = true;
                                statusCode = context.Response.StatusCode;
                                await CopyStream(response, context.Response.OutputStream, response.Length - response.Position, add => totalSent += add);
                            }
                        }
                        catch (HttpListenerException ex) when (ex.ErrorCode == 64)
                        {
                        }
                        catch (Exception ex)
                        {
                            if (!responseStarted)
                                await Send(context, new ErrorPage(500, null, ex), add => totalSent += add, status => statusCode = status, 1048576);
                            else
                                context.Response.Abort();

                            if (WebHandler.debug)
                                Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            try
                            {
                                RequestInfo info = new RequestInfo(statusCode, totalSent, totalReceived, method, requestPath, iPEndPoint, DateTime.Now.Ticks - start);
                                OnRequestFinished?.Invoke(null, info);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Exception while calling OnRequestFinished: {ex.Message}");
                            }
                            if (context?.Response?.OutputStream?.CanWrite == true)
                            {
                                try { context.Response.Close(); } catch { }
                            }
                        }
                    });
                }
                catch (Exception e)
                {
                    if (WebHandler.debug)
                        Console.WriteLine(e.ToString());
                    else
                        Console.WriteLine(e.Message);
                }
            }
        }

        private static async Task HandleRange(string rangeHeader, Stream response, HttpListenerContext context, Action<long> updateSent, Action<int> updateStatus)
        {
            long fileLength = response.Length;

            string[] range = rangeHeader.Substring(6).Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            long start = long.Parse(range[0]);
            long end = range.Length == 2 ? long.Parse(range[1]) : fileLength - 1;

            long partialLength = end - start + 1;
            response.Seek(start, SeekOrigin.Begin);
            context.Response.StatusCode = 206;
            updateStatus?.Invoke(206);
            context.Response.AddHeader("Content-Range", $"bytes {start}-{end}/{fileLength}");
            context.Response.ContentLength64 = partialLength;

            await CopyStream(response, context.Response.OutputStream, partialLength, add => updateSent?.Invoke(add));
            response.Close();
        }
    }
}
