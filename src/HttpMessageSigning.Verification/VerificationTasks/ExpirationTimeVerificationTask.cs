using System;

namespace Dalion.HttpMessageSigning.Verification.VerificationTasks {
    internal class ExpirationTimeVerificationTask : VerificationTask {
        private readonly ISystemClock _systemClock;
        
        public ExpirationTimeVerificationTask(ISystemClock systemClock) {
            _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        }

        public override SignatureVerificationFailure VerifySync(HttpRequestForSigning signedRequest, Signature signature, Client client) {
            if (!signature.Expires.HasValue) {
                return SignatureVerificationFailure.HeaderMissing($"The signature does not contain a value for the {nameof(signature.Expires)} property, but it is required.");
            }
            
            if (signature.Expires.Value < _systemClock.UtcNow) {
                return SignatureVerificationFailure.SignatureExpired("The signature is expired.");
            }
            
            return null;
        }
    }
}