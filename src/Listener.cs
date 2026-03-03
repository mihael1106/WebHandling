using System.Net;
using System.Threading.Tasks;
using System;
using Miki1106.WebHandling.Core;
using System.Threading;
using static Miki1106.WebHandling.Core.Utils;

namespace Miki1106.WebHandling
{
    public static class Listener
    {

        private static bool running = false;

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

        static Listener()
        {
            if (webListener == null)
            {
                webListener = new HttpListener();
                webListener.Prefixes.Add($"http://*:{_port}/");
            }
        }

        public static void Start()
        {
            if (running) return;

            listenerThreads = new Thread[_listenerThreads];
            for (int i = 0; i < _listenerThreads; i++)
            {
                listenerThreads[i] = new Thread(Web)
                {
                    IsBackground = false,
                    Name = "Web Listener thread #" + i
                };
                listenerThreads[i].Start();
            }
            running = true;
        }

        private static void Web()
        {
            if (!webListener.IsListening)
                webListener.Start();
            while (webListener.IsListening)
            {
                try
                {
                    try
                    {
                        HttpListenerContext context = webListener.GetContext();
                        _ = Task.Run(async () =>
                        {
                            long start = DateTime.Now.Ticks;
                            long totalSent = 0;
                            long totalReceived = context.Request.ContentLength64;
                            int statusCode = 500;
                            string method = context.Request.HttpMethod;
                            IPEndPoint iPEndPoint = context.Request.RemoteEndPoint;
                            bool responseStarted = false;
                            string path = Uri.UnescapeDataString(context.Request.Url.AbsolutePath);
                            try
                            {
                                ListenerResponse response = Router.FindRoute(path, context);
                                responseStarted = true;
                                await Send(context, response, add => totalSent += add, status => statusCode = status).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                if (WebHandler.debug)
                                    Console.WriteLine(ex.ToString());

                                if (!responseStarted)
                                    await Send(context, new ErrorPage(500, null, ex), add => totalSent += add, status => statusCode = status).ConfigureAwait(false);
                                else
                                    context.Response.Abort();
                            }
                            finally
                            {
                                try
                                {
                                    RequestInfo info = new RequestInfo(statusCode, totalSent, totalReceived, method, path, iPEndPoint, DateTime.Now.Ticks - start);
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
                    catch (HttpListenerException ex) when (ex.ErrorCode == 995) { }
                }
                catch (Exception ex)
                {
                    if (WebHandler.debug)
                        Console.WriteLine(ex.ToString());
                    else
                        Console.WriteLine(ex.Message);
                }
            }
        }

        public static void Join()
        {
            if (listenerThreads == null)
                return;

            for (int i = 0; i < _listenerThreads; i++)
            {
                listenerThreads[i].Join();
            }
        }

        public static void Stop()
        {
            webListener.Stop();
            Join();
            running = false;
        }
    }
}
