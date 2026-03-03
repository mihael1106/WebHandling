using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Miki1106.WebHandling
{
    public class ErrorPage : ListenerResponse
    {
        private static Dictionary<int, Func<HttpListenerContext, string, Exception, ListenerResponse>> errorPages = new Dictionary<int, Func<HttpListenerContext, string, Exception, ListenerResponse>>();

        public static void AddListener(int statusCode, Func<HttpListenerContext, string, Exception, ListenerResponse> listener)
        {
            errorPages.Add(statusCode, listener);
        }

        private int statusCode;
        private string extraData;
        private Exception exception;

        public ErrorPage(int statusCode, string extraData = null, Exception exception = null)
        {
            this.statusCode = statusCode;
            this.extraData = extraData;
            this.exception = exception;
        }

        public override Stream GetResponse(HttpListenerContext context)
        {
            context.Response.StatusCode = statusCode;
            if (errorPages.ContainsKey(statusCode))
            {
                return errorPages[statusCode].Invoke(context, extraData, exception).GetResponse(context);
            }
            else
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(new ErrorPageBuilder().ErrorNumber(statusCode).ExtraData(extraData).DefaultDebugData(exception).Build()));
            }
        }
    }
}
