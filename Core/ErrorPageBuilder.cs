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
            string exception = $"<br>Exception occured: {WebUtility.HtmlEncode(ex?.ToString())}<br>";
            debugData = $"{(ex != null ? exception : "")}<br>Time: {DateTime.Now:dd.MM.yyyy. HH:mm:ss}<br><h2>Call stack:</h2> <pre><code><div class=\"code-box\">{WebUtility.HtmlEncode(WebHandler.GetStackTrace())}</div></code></pre>";

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

        public void Send(HttpListenerContext context)
        {
            try
            {
                context.Response.StatusCode = errorNum;
                byte[] response = Encoding.UTF8.GetBytes(Build());
                context.Response.OutputStream.Write(response, 0, response.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                if (WebHandler.debug)
                    Console.WriteLine(ex.ToString());
                else
                    Console.WriteLine("This shouldnt happen: " + ex.Message);
            }
        }
    }
}
