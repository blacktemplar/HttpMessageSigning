using System;
using System.Net.Http;
using System.Threading.Tasks;
using Dalion.HttpMessageSigning.SigningString;
using Microsoft.Extensions.Logging;

namespace Dalion.HttpMessageSigning.Signing {
    internal class SignatureCreator : ISignatureCreator {
        private readonly ISigningSettingsSanitizer _signingSettingsSanitizer;
        private readonly ISigningStringComposer _signingStringComposer;
        private readonly IBase64Converter _base64Converter;
        private readonly INonceGenerator _nonceGenerator;
        private readonly ILogger<SignatureCreator> _logger;

        public SignatureCreator(
            ISigningSettingsSanitizer signingSettingsSanitizer,
            ISigningStringComposer signingStringComposer,
            IBase64Converter base64Converter,
            INonceGenerator nonceGenerator,
            ILogger<SignatureCreator> logger = null) {
            _signingSettingsSanitizer = signingSettingsSanitizer ?? throw new ArgumentNullException(nameof(signingSettingsSanitizer));
            _signingStringComposer = signingStringComposer ?? throw new ArgumentNullException(nameof(signingStringComposer));
            _base64Converter = base64Converter ?? throw new ArgumentNullException(nameof(base64Converter));
            _nonceGenerator = nonceGenerator ?? throw new ArgumentNullException(nameof(nonceGenerator));
            _logger = logger;
        }

        public async Task<Signature> CreateSignature(HttpRequestMessage request, SigningSettings settings, DateTimeOffset timeOfSigning) {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            _signingSettingsSanitizer.SanitizeHeaderNamesToInclude(settings, request);
            
            settings.Validate();

            var nonce = settings.EnableNonce ? _nonceGenerator.GenerateNonce() : null;
            var requestForSigning = request.ToRequestForSigning();
            var signingString = _signingStringComposer.Compose(
                requestForSigning, 
                settings.SignatureAlgorithm.Name, 
                settings.Headers, 
                timeOfSigning, 
                settings.Expires, 
                nonce);

            var eventTask = settings.Events?.OnSigningStringComposed?.Invoke(request, signingString);
            if (eventTask != null) await eventTask;

            _logger?.LogDebug("Composed the following signing string for request signing: {0}", signingString);

            var signatureHash = settings.SignatureAlgorithm.ComputeHash(signingString);
            var signatureString = _base64Converter.ToBase64(signatureHash);

            _logger?.LogDebug("The base64 hash of the signature string for signing is '{0}'.", signatureString);

            var signature = new Signature {
                KeyId = settings.KeyId,
                Algorithm = $"{settings.SignatureAlgorithm.Name.ToLowerInvariant()}-{settings.SignatureAlgorithm.HashAlgorithm.ToString().ToLowerInvariant()}",
                Created = timeOfSigning,
                Expires = timeOfSigning.Add(settings.Expires),
                Headers = settings.Headers,
                Nonce = nonce,
                String = signatureString
            };

            return signature;
        }
    }
}