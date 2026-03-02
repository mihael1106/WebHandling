using System.Net;

namespace Miki1106.WebHandling.Core
{
    public sealed class RequestInfo
    {
        public int StatusCode { get; }
        public long DataSent { get; }
        public long DataReceived { get; }
        public string Method { get; }
        public string Path { get; }
        public IPEndPoint RemoteAddress { get; }
        public long TimeTook { get; }

        internal RequestInfo(int statusCode, long dataSent, long dataReceived, string method, string path, IPEndPoint remoteAddress, long timeTook)
        {
            StatusCode = statusCode;
            DataSent = dataSent;
            DataReceived = dataReceived;
            Method = method;
            Path = path;
            RemoteAddress = remoteAddress;
            TimeTook = timeTook;
        }
    }
}
