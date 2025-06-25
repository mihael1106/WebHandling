using Miki1106.WebHandling.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Miki1106.WebHandling
{
    public class WebHandler
    {
        public static bool debug = false;

        private static int _listenerThreads = 4;
        public static int ListenerThreads
        {
            get => _listenerThreads;
            set
            {
                if (running)
                {
                    throw new InvalidOperationException("Cant change the amount of threads while running.");
                }
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Thread count cant be less than 1.");
                }
                _listenerThreads = value;
            }
        }

        private static ushort _port = 80;
        public static ushort Port
        {
            get => _port;
            set
            {
                if (running)
                {
                    throw new InvalidOperationException("Cant change the port while running.");
                }
                _port = value;
                if (webListener != null)
                {
                    webListener.Prefixes.Clear();
                    webListener.Prefixes.Add($"http://*:{value}/");
                }
            }
        }

        public static event EventHandler<RequestInfo> OnRequestFinished;

        private static HttpListener webListener;
        private static Thread[] listenerThreads;

        private static bool running = false;

        private static Dictionary<string, Func<HttpListenerContext, ListenerResponse>> listeners;
        private readonly string prefix;

        public WebHandler(string prefix)
        {
            this.prefix = GetPath(prefix);

            if (listeners == null)
                listeners = new Dictionary<string, Func<HttpListenerContext, ListenerResponse>>();

            if (webListener == null)
            {
                webListener = new HttpListener();
                webListener.Prefixes.Add($"http://*:{_port}/");
            }
        }

        public static void Start()
        {
            if (running) return;

            StaticHandler.Start();
            listenerThreads = new Thread[_listenerThreads];
            for (int i = 0; i < _listenerThreads; i++)
            {
                listenerThreads[i] = new Thread(Web)
                {
                    IsBackground = true,
                    Name = "Web Listener thread #" + i
                };
                listenerThreads[i].Start();
            }
            running = true;
        }

        internal string GetPath(string path)
        {
            if (path.Length != 0)
                if (path[0] == '/')
                    path = path.Substring(1);

            if (path.Length != 0)
                if (path[path.Length - 1] == '/')
                    path = path.Remove(path.Length - 1, 1);
            return path;
        }

        public void AddListener(string path, Func<HttpListenerContext, ListenerResponse> listener)
        {
            listeners.Add("/" + GetPath($"{prefix}/{GetPath(path)}"), listener);
            Console.WriteLine($"Added: {"/" + GetPath($"{prefix}/{GetPath(path)}")}");
        }


        public void RemoveListener(string path)
        {
            if (listeners.ContainsKey("/" + GetPath($"{prefix}/{GetPath(path)}")))
                listeners.Remove("/" + GetPath($"{prefix}/{GetPath(path)}"));
        }

        private async static void Web()
        {
            if (!webListener.IsListening)
                webListener.Start();
            while (webListener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await webListener.GetContextAsync();
                    _ = Task.Run(async () =>
                    {
                        long start = DateTime.Now.Ticks;
                        bool responseStarted = false;
                        try
                        {
                            string path = context.Request.Url.AbsolutePath;
                            if (path == "/throw" && debug)
                            {
                                throw new Exception("A debug exception has been thrown");
                            }
                            else if (listeners.ContainsKey(path))
                            {
                                if (debug)
                                    Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Found listener for \"{path}\"");

                                ListenerResponse listenerResponse = listeners[path]?.Invoke(context);
                                if (listenerResponse == null)
                                {
                                    new ErrorPageBuilder().ErrorNumber(500).DefaultDebugData(new NullReferenceException("Something went very wrong. The listener response is null.")).Send(context);
                                    return;
                                }

                                Stream resultStream = listenerResponse.GetResponse(context);
                                if (resultStream == null)
                                {
                                    responseStarted = true;
                                    new ErrorPageBuilder().ErrorNumber(500).DefaultDebugData(new NullReferenceException("Something went very wrong. The result stream is null.")).Send(context);
                                    return;
                                }

                                using (resultStream)
                                {
                                    if (resultStream.CanSeek)
                                        resultStream.Seek(0, SeekOrigin.Begin);
                                    context.Response.ContentLength64 = resultStream.Length - resultStream.Position;
                                    responseStarted = true;
                                    await resultStream.CopyToAsync(context.Response.OutputStream, 65536);
                                }

                                context.Response.OutputStream.Flush();
                            }
                            else
                            {
                                if (debug)
                                    Console.WriteLine($"[{context.Request.RemoteEndPoint.Address}] Path \"{path}\" does not exist.");

                                responseStarted = true;
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
                        finally
                        {
                            try
                            {
                                RequestInfo info = new RequestInfo(context.Response.StatusCode, context.Response.ContentLength64, context.Request.ContentLength64, context.Request.HttpMethod, context.Request.Url.AbsolutePath, context.Request.RemoteEndPoint, DateTime.Now.Ticks - start);
                                OnRequestFinished?.Invoke(null, info);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }

                            if (context?.Response?.OutputStream?.CanWrite == true)
                            {
                                try { context.Response.Close(); } catch { }
                            }
                        }
                    });
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

        public static void Stop()
        {
            webListener.Stop();
            for (int i = 0; i < _listenerThreads; i++)
            {
                listenerThreads[i].Join();
            }
            StaticHandler.Stop();
            running = false;
        }
    }
}