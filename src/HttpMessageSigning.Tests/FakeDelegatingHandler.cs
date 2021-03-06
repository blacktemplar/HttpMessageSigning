using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dalion.HttpMessageSigning {
    public class FakeDelegatingHandler : DelegatingHandler {
        public FakeDelegatingHandler(HttpResponseMessage responseToReturn) {
            ResponseToReturn = responseToReturn;
        }
        
        public HttpResponseMessage ResponseToReturn { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            return Task.FromResult(ResponseToReturn);
        }
    }
}