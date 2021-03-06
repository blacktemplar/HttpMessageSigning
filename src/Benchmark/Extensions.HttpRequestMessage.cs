using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dalion.HttpMessageSigning;
using Microsoft.AspNetCore.Http;

namespace Benchmark {
    public static partial class Extensions {
        public static async Task<HttpRequest> ToServerSideHttpRequest(this HttpRequestMessage clientRequest) {
            if (clientRequest == null) return null;

            var request = new DefaultHttpContext().Request;
            request.Method = clientRequest.Method.Method;
            request.Scheme = clientRequest.RequestUri.IsAbsoluteUri ? clientRequest.RequestUri.Scheme : null;
            request.Host = clientRequest.RequestUri.IsAbsoluteUri ? new HostString(clientRequest.RequestUri.Host, clientRequest.RequestUri.Port) : new HostString();
            request.Path = clientRequest.RequestUri.IsAbsoluteUri ? clientRequest.RequestUri.AbsolutePath : clientRequest.RequestUri.OriginalString.Split('?')[0];
            request.Headers["Authorization"] = clientRequest.Headers.Authorization.Scheme + " " + clientRequest.Headers.Authorization.Parameter;

            var bodyTask = clientRequest.Content?.ReadAsStreamAsync();
            if (bodyTask != null) request.Body = await bodyTask;

            if (clientRequest.Headers.Contains("Dalion-App-Id")) {
                request.Headers.Add("Dalion-App-Id", clientRequest.Headers.GetValues("Dalion-App-Id").ToArray());
            }

            if (clientRequest.Headers.Contains(HeaderName.PredefinedHeaderNames.Digest)) {
                request.Headers.Add(HeaderName.PredefinedHeaderNames.Digest, clientRequest.Headers.GetValues(HeaderName.PredefinedHeaderNames.Digest).ToArray());
            }

            if (clientRequest.Headers.Contains(HeaderName.PredefinedHeaderNames.Date)) {
                request.Headers.Add(HeaderName.PredefinedHeaderNames.Date, clientRequest.Headers.GetValues(HeaderName.PredefinedHeaderNames.Date).ToArray());
            }

            return request;
        }
    }
}