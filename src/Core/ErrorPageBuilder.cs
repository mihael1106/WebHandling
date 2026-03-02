using Miki1106.WebHandling.Properties;
using System;
using System.Net;
using System.Text;

namespace Miki1106.WebHandling
{
    public class ErrorPageBuilder
    {
        private static readonly string template = Resources.error_base;

        private string error = "An unknown error has occured";
        private int errorNum = 500;
        private string extraData = "";
        private string debugData = "";

        public ErrorPageBuilder()
        {

        }

        public ErrorPageBuilder ErrorNumber(int error)
        {
            this.error = $"ERROR {error}";
            this.errorNum = error;
            return this;
        }

        public ErrorPageBuilder ExtraData(string extraData)
        {
            this.extraData = extraData;
            return this;
        }

        public ErrorPageBuilder DefaultDebugData(Exception ex = null)
        {
            string exception = $"<br>Exception occured: <pre><code><div class=\"code-box\">{WebUtility.HtmlEncode(ex?.ToString())}</div></code></pre><br>";
            debugData = $"{(ex != null ? exception : "")}<br>Time: {DateTime.Now:dd.MM.yyyy. HH:mm:ss}";

            return this;
        }

        public ErrorPageBuilder DebugData(string debugData)
        {
            this.debugData = debugData;
            return this;
        }

        public string Build()
        {
            return template.Replace("{err}", WebUtility.HtmlEncode(error)).Replace("{extra_data}", extraData).Replace("{debug_data}", WebHandler.debug ? debugData : "");
        }

        public void Send(HttpListenerContext context, ref int statusCode, ref long totalSent)
        {
            try
            {
                context.Response.StatusCode = errorNum;
                statusCode = errorNum;
                byte[] response = Encoding.UTF8.GetBytes(Build());
                context.Response.ContentLength64 = response.Length;
                context.Response.ContentType = "text/html";
                context.Response.Headers.Remove("Content-Range");
                context.Response.OutputStream.Write(response, 0, response.Length);
                totalSent += response.LongLength;
                context.Response.OutputStream.Flush();
            }
            catch (Exception ex)
            {
                if (WebHandler.debug)
                    Console.WriteLine(ex.ToString());
                else
                    Console.WriteLine("This shouldnt happen: " + ex.Message);
            }
        }

        public void Send(HttpListenerContext context)
        {
            int temp = 0;
            long temp2 = 0;
            Send(context, ref temp, ref temp2);
        }
    }
}
