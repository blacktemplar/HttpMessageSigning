## HttpMessageSigning [<img src="https://dalion.eu/dalion128.png" align="right" width="48">](https://www.dalion.eu)

A C# implementation of the "Authorization" scheme of the IETF Internet-Draft [Signing HTTP Messages](https://tools.ietf.org/html/draft-cavage-http-signatures-12).

[![Build status](https://ci.appveyor.com/api/projects/status/d8fdl40nfj62ed1v?svg=true)](https://ci.appveyor.com/project/DavidLievrouw/httpmessagesigning) [![Coverage Status](https://coveralls.io/repos/github/DavidLievrouw/HttpMessageSigning/badge.svg?branch=master)](https://coveralls.io/github/DavidLievrouw/HttpMessageSigning?branch=master)

It contains:
  - HTTP request signing services.
  - HTTP request signature verification services.
  - Authentication middleware for ASP.NET Core applications.
  - Authentication middleware for OWIN applications.
  - Extensions for storing known clients in memory.
  - Extensions for storing known clients in MongoDb.

See [wiki](https://github.com/DavidLievrouw/HttpMessageSigning/wiki) for further details.

## Motivation
When communicating over the Internet using the HTTP protocol, it can be desirable for a server or client to authenticate the sender of a particular message.  It can also be desirable to ensure that the message was not tampered with during transit. The Signing HTTP Messages Internet-Draft describes a way for servers and clients to simultaneously add authentication and message integrity to HTTP messages by using a digital signature.

This repository is a C# implementation of that specification.

## NuGet

| Package | NuGet status |
| --- | --- |
| `Dalion.HttpMessageSigning.Signing` | [![NuGet Status](https://buildstats.info/nuget/Dalion.HttpMessageSigning.Signing)](https://www.nuget.org/packages/Dalion.HttpMessageSigning.Signing/) |
| `Dalion.HttpMessageSigning.Verification.AspNetCore` | [![NuGet Status](https://buildstats.info/nuget/Dalion.HttpMessageSigning.Verification.AspNetCore)](https://www.nuget.org/packages/Dalion.HttpMessageSigning.Verification.AspNetCore/) |
| `Dalion.HttpMessageSigning.Verification.Owin` | [![NuGet Status](https://buildstats.info/nuget/Dalion.HttpMessageSigning.Verification.Owin)](https://www.nuget.org/packages/Dalion.HttpMessageSigning.Verification.Owin/) |
| `Dalion.HttpMessageSigning.Verification.MongoDb` | [![NuGet Status](https://buildstats.info/nuget/Dalion.HttpMessageSigning.Verification.MongoDb)](https://www.nuget.org/packages/Dalion.HttpMessageSigning.Verification.MongoDb/) |

## Basics
When signing a request message, an _Authorization_ header is set in a http request. Using this header, the server can verify that it is sent by the known client, and that the content has not been tampered with.

The signing will result in a request header that will look like:

```
Authorization="SignedHttpRequest keyId=\"e0e8dcd638334c409e1b88daf821d135\",algorithm=\"hmac-sha256\",created=1584806516,expires=1584806576,headers=\"(request-target) dalion-app-id date digest\",nonce=\"38brRy8BLUajMbUqWumXPg\",signature=\"DUKQVjiirGMMaMOy9qIwKMro46R3BlLsvUQkw1/8sKQ=\""
```

When configured, a _RequestSignerFactory_ is registered in your composition root. Example usage:

```cs
public class SignRequestService {
    private readonly IHttpClient<SignRequestService> _httpClient;
    private readonly IRequestSignerFactory _requestSignerFactory;

...

    public async Task<HttpResponseMessage> SendSignedRequest(HttpRequestMessage request, CancellationToken cancellationToken) {
        var requestSigner = _requestSignerFactory.CreateFor(keyId: "f1ed1eff7ca4429abe1abbbe9ae6419a");
        await requestSigner.Sign(request);
        return await _httpClient.SendAsync(request, cancellationToken);
    }
}
```

And verification can be done server-side:

```cs
public class HttpRequestSignatureParser {
    private readonly IRequestSignatureVerifier _requestSignatureVerifier;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<HttpRequestSignatureParser> _logger;
    
...

    public async Task Verify(HttpRequest request) {
        var verificationResult = await _requestSignatureVerifier.VerifySignature(request);

        var httpContext = _httpContextAccessor.HttpContext;
        if (verificationResult is RequestSignatureVerificationResultFailure failure) {
            _logger.LogWarning(failure.SignatureVerificationException, "Request signature verification failed. See exception for details.");
            httpContext.Response.StatusCode = (int) HttpStatusCode.Unauthorized;
        }
        else if (verificationResult is RequestSignatureVerificationResultSuccess success) {
            _logger.LogInformation("Successfully verified signature for identity {0}.", success.Principal.Identity.Name);
            httpContext.User = success.Principal;
        }
    }
}
```

There is OWIN and ASP.NET Core middleware available too, for easy integration.

For more details and options, see the [Wiki](https://github.com/DavidLievrouw/HttpMessageSigning/wiki).

## Documentation

See [Wiki](https://github.com/DavidLievrouw/HttpMessageSigning/wiki).

## Support

If you've got value from any of the content which I have created, but pull requests are not your thing, then I would also very much appreciate your support by buying me a coffee.

<a href="https://www.buymeacoffee.com/DavidLievrouw" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: 41px !important;width: 174px !important;box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;-webkit-box-shadow: 0px 3px 2px 0px rgba(190, 190, 190, 0.5) !important;" ></a>
