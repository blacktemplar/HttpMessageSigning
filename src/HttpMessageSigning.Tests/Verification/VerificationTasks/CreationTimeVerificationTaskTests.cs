using System;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace Dalion.HttpMessageSigning.Verification.VerificationTasks {
    public class CreationTimeVerificationTaskTests {
        private readonly CreationTimeVerificationTask _sut;
        private readonly ISystemClock _systemClock;

        public CreationTimeVerificationTaskTests() {
            FakeFactory.Create(out _systemClock);
            _sut = new CreationTimeVerificationTask(_systemClock);
        }

        public class Verify : CreationTimeVerificationTaskTests {
            private readonly HttpRequestForSigning _signedRequest;
            private readonly Client _client;
            private readonly Signature _signature;
            private readonly Func<HttpRequestForSigning, Signature, Client, Task<SignatureVerificationFailure>> _method;
            private readonly DateTimeOffset _now;

            public Verify() {
                _signature = (Signature) TestModels.Signature.Clone();
                _signedRequest = (HttpRequestForSigning) TestModels.Request.Clone();
                _client = (Client) TestModels.Client.Clone();
                _method = (request, signature, client) => _sut.Verify(request, signature, client);

                _now = _signature.Created.Value.AddSeconds(3);
                A.CallTo(() => _systemClock.UtcNow).Returns(_now);
            }

            [Fact]
            public async Task WhenSignatureDoesNotSpecifyACreationTime_ReturnsSignatureVerificationFailure() {
                _signature.Created = null;

                var actual = await _method(_signedRequest, _signature, _client);

                actual.Should().NotBeNull().And.BeAssignableTo<SignatureVerificationFailure>()
                    .Which.Code.Should().Be("INVALID_CREATED_HEADER");
            }

            [Fact]
            public async Task WhenSignatureCreationTimeIsInTheFuture_ReturnsSignatureVerificationFailure() {
                _signature.Created = _now.AddSeconds(1);

                var actual = await _method(_signedRequest, _signature, _client);

                actual.Should().NotBeNull().And.BeAssignableTo<SignatureVerificationFailure>()
                    .Which.Code.Should().Be("INVALID_CREATED_HEADER");
            }

            [Fact]
            public async Task WhenSignatureCreationTimeIsNow_ReturnsNull() {
                _signature.Created = _now;

                var actual = await _method(_signedRequest, _signature, _client);

                actual.Should().BeNull();
            }

            [Fact]
            public async Task WhenSignatureCreationTimeIsInThePast_ReturnsNull() {
                _signature.Created = _now.AddSeconds(-1);

                var actual = await _method(_signedRequest, _signature, _client);

                actual.Should().BeNull();
            }
        }
    }
}