using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Miki1106.WebHandling.Core
{
    public static class Utils
    {

        public static async Task CopyStream(Stream source, Stream target, long bytesToCopy, Action<long> updateSent, int bufferSize = 1048576)
        {
            byte[] buffer = new byte[bufferSize];
            while (bytesToCopy > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, bytesToCopy);
                int bytesRead = await source.ReadAsync(buffer, 0, toRead).ConfigureAwait(false);
                if (bytesRead <= 0)
                    break;
                try
                {
                    await target.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                }
                catch { break; }
                bytesToCopy -= bytesRead;
                updateSent?.Invoke(bytesRead);
            }
        }

        public static async Task Send(HttpListenerContext context, ListenerResponse response, Action<long> updateSent, Action<int> updateStatusCode, int bufferSize = 131072)
        {
            if (response == null)
            {
                int statusCode = 0;
                long totalSent = 0;
                new ErrorPageBuilder().ErrorNumber(500).DefaultDebugData(new NullReferenceException("Something went very wrong. The listener response is null.")).Send(context, ref statusCode, ref totalSent);
                updateStatusCode?.Invoke(statusCode);
                updateSent?.Invoke(totalSent);
                return;
            }

            Stream resultStream = response.GetResponse(context);
            if (resultStream == null)
            {
                int statusCode = 0;
                long totalSent = 0;
                new ErrorPageBuilder().ErrorNumber(500).DefaultDebugData(new NullReferenceException("Something went very wrong. The result stream is null.")).Send(context, ref statusCode, ref totalSent);
                updateStatusCode?.Invoke(statusCode);
                updateSent?.Invoke(totalSent);
                return;
            }

            using (resultStream)
            {
                if (resultStream.CanSeek)
                    resultStream.Seek(response.startAt, SeekOrigin.Begin);
                long size = response.sendCount < 0 ? resultStream.Length - resultStream.Position : response.sendCount;

                updateStatusCode?.Invoke(context.Response.StatusCode);
                context.Response.ContentLength64 = size;
                await CopyStream(resultStream, context.Response.OutputStream, size, add => updateSent?.Invoke(add), bufferSize).ConfigureAwait(false);
            }
        }
    }
}
