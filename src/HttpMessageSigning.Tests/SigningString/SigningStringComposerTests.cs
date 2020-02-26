using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace Dalion.HttpMessageSigning.SigningString {
    public class SigningStringComposerTests {
        private readonly IHeaderAppenderFactory _headerAppenderFactory;
        private readonly SigningStringComposer _sut;

        public SigningStringComposerTests() {
            FakeFactory.Create(out _headerAppenderFactory);
            _sut = new SigningStringComposer(_headerAppenderFactory);
        }

        public class Compose : SigningStringComposerTests {
            private readonly HttpRequestMessage _httpRequest;
            private readonly SigningSettings _settings;
            private readonly IHeaderAppender _headerAppender;
            private readonly DateTimeOffset _timeOfComposing;

            public Compose() {
                _timeOfComposing = new DateTimeOffset(2020, 2, 24, 11, 20, 14, TimeSpan.FromHours(1));
                _httpRequest = new HttpRequestMessage {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("http://dalion.eu/api/resource/id1")
                };
                _settings = new SigningSettings {
                    Expires = TimeSpan.FromMinutes(5),
                    KeyId = new KeyId("client1"),
                    SignatureAlgorithm = new HMACSignatureAlgorithm("s3cr3t", HashAlgorithmName.SHA512),
                    Headers = new[] {
                        HeaderName.PredefinedHeaderNames.RequestTarget,
                        HeaderName.PredefinedHeaderNames.Date,
                        HeaderName.PredefinedHeaderNames.Expires,
                        new HeaderName("dalion_app_id")
                    },
                    DigestHashAlgorithm = HashAlgorithmName.SHA256
                };

                FakeFactory.Create(out _headerAppender);
                A.CallTo(() => _headerAppenderFactory.Create(_httpRequest, _settings, _timeOfComposing))
                    .Returns(_headerAppender);
            }

            [Fact]
            public void GivenNullRequest_ThrowsArgumentNullException() {
                Action act = () => _sut.Compose(null, _settings, _timeOfComposing);
                act.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void GivenNullSettings_ThrowsArgumentNullException() {
                Action act = () => _sut.Compose(_httpRequest, null, _timeOfComposing);
                act.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void GivenInvalidSettings_ThrowsValidationException() {
                _settings.KeyId = KeyId.Empty; // Make invalid
                Action act = () => _sut.Compose(_httpRequest, _settings, _timeOfComposing);
                act.Should().Throw<ValidationException>();
            }

            [Fact]
            public void WhenHeadersDoesNotContainRequestTarget_PrependsRequestTargetToHeaders() {
                SigningSettings interceptedSettings = null;
                A.CallTo(() => _headerAppenderFactory.Create(_httpRequest, _settings, _timeOfComposing))
                    .Invokes(call => interceptedSettings = call.GetArgument<SigningSettings>(1))
                    .Returns(_headerAppender);

                _settings.Headers = Array.Empty<HeaderName>();
                _sut.Compose(_httpRequest, _settings, _timeOfComposing);

                interceptedSettings.Headers.ElementAt(0).Should().Be(HeaderName.PredefinedHeaderNames.RequestTarget);
            }
            
            [Fact]
            public void WhenAlgorithmIsRSAOrHMACOrECDSA_AndHeadersDoesNotContainDate_PrependsDateToHeaders_ButAfterRequestTargetHeader() {
                SigningSettings interceptedSettings = null;
                A.CallTo(() => _headerAppenderFactory.Create(_httpRequest, _settings, _timeOfComposing))
                    .Invokes(call => interceptedSettings = call.GetArgument<SigningSettings>(1))
                    .Returns(_headerAppender);

                _settings.Headers = Array.Empty<HeaderName>();
                _sut.Compose(_httpRequest, _settings, _timeOfComposing);

                interceptedSettings.Headers.ElementAt(0).Should().Be(HeaderName.PredefinedHeaderNames.RequestTarget);
                interceptedSettings.Headers.ElementAt(1).Should().Be(HeaderName.PredefinedHeaderNames.Date);
            }    
            
            [Fact]
            public void WhenAlgorithmIsNotRSAOrHMACOrECDSA_AndHeadersDoesNotContainCreated_PrependsCreatedToHeaders_ButAfterRequestTargetHeader() {
                _settings.SignatureAlgorithm = new NotSupportedSignatureAlgorithm();
                
                SigningSettings interceptedSettings = null;
                A.CallTo(() => _headerAppenderFactory.Create(_httpRequest, _settings, _timeOfComposing))
                    .Invokes(call => interceptedSettings = call.GetArgument<SigningSettings>(1))
                    .Returns(_headerAppender);

                _settings.Headers = Array.Empty<HeaderName>();
                _sut.Compose(_httpRequest, _settings, _timeOfComposing);

                interceptedSettings.Headers.ElementAt(0).Should().Be(HeaderName.PredefinedHeaderNames.RequestTarget);
                interceptedSettings.Headers.ElementAt(1).Should().Be(HeaderName.PredefinedHeaderNames.Created);
            }

            [Fact]
            public void WhenHeadersDoesNotContainDigest_AndDigestIsOff_DoesNotAddDigestHeader() {
                _settings.DigestHashAlgorithm = default;
                
                SigningSettings interceptedSettings = null;
                A.CallTo(() => _headerAppenderFactory.Create(_httpRequest, _settings, _timeOfComposing))
                    .Invokes(call => interceptedSettings = call.GetArgument<SigningSettings>(1))
                    .Returns(_headerAppender);

                _settings.Headers = Array.Empty<HeaderName>();
                _sut.Compose(_httpRequest, _settings, _timeOfComposing);

                interceptedSettings.Headers.Should().NotContain(_ => _.Value == HeaderName.PredefinedHeaderNames.Digest);
            }
            
            [Fact]
            public void WhenHeadersDoesNotContainDigest_AndDigestIsOn_AddsDigestHeader() {
                _settings.DigestHashAlgorithm = HashAlgorithmName.SHA384;
                
                SigningSettings interceptedSettings = null;
                A.CallTo(() => _headerAppenderFactory.Create(_httpRequest, _settings, _timeOfComposing))
                    .Invokes(call => interceptedSettings = call.GetArgument<SigningSettings>(1))
                    .Returns(_headerAppender);

                _settings.Headers = Array.Empty<HeaderName>();
                _sut.Compose(_httpRequest, _settings, _timeOfComposing);

                interceptedSettings.Headers.Should().Contain(_ => _.Value == HeaderName.PredefinedHeaderNames.Digest);
            }
            
            [Fact]
            public void WhenHeadersContainsDigest_AndDigestIsOn_DoesNotAddDigestHeaderAgain() {
                _settings.Headers = new[] {
                    HeaderName.PredefinedHeaderNames.RequestTarget,
                    HeaderName.PredefinedHeaderNames.Date,
                    HeaderName.PredefinedHeaderNames.Digest,
                    HeaderName.PredefinedHeaderNames.Expires,
                    new HeaderName("dalion_app_id")
                };
                _settings.DigestHashAlgorithm = HashAlgorithmName.SHA384;
                
                SigningSettings interceptedSettings = null;
                A.CallTo(() => _headerAppenderFactory.Create(_httpRequest, _settings, _timeOfComposing))
                    .Invokes(call => interceptedSettings = call.GetArgument<SigningSettings>(1))
                    .Returns(_headerAppender);

                _sut.Compose(_httpRequest, _settings, _timeOfComposing);

                interceptedSettings.Headers.Should().Contain(_ => _.Value == HeaderName.PredefinedHeaderNames.Digest);
                interceptedSettings.Headers.Count(_ => _.Value == HeaderName.PredefinedHeaderNames.Digest).Should().Be(1);
            }
            
            [Theory]
            [InlineData("GET")]
            [InlineData("TRACE")]
            [InlineData("HEAD")]
            [InlineData("DELETE")]
            public void WhenHeadersDoesNotContainDigest_AndDigestIsOn_ButMethodDoesNotHaveBody_DoesNotAddDigestHeader(string method) {
                _settings.DigestHashAlgorithm = HashAlgorithmName.SHA384;
                _httpRequest.Method = new HttpMethod(method);
                
                SigningSettings interceptedSettings = null;
                A.CallTo(() => _headerAppenderFactory.Create(_httpRequest, _settings, _timeOfComposing))
                    .Invokes(call => interceptedSettings = call.GetArgument<SigningSettings>(1))
                    .Returns(_headerAppender);

                _settings.Headers = Array.Empty<HeaderName>();
                _sut.Compose(_httpRequest, _settings, _timeOfComposing);

                interceptedSettings.Headers.Should().NotContain(_ => _.Value == HeaderName.PredefinedHeaderNames.Digest);
            }
            
            [Fact]
            public void ExcludesEmptyHeaderNames() {
                _settings.Headers = new[] {
                    HeaderName.PredefinedHeaderNames.RequestTarget,
                    HeaderName.Empty, 
                    HeaderName.PredefinedHeaderNames.Date,
                    HeaderName.PredefinedHeaderNames.Expires,
                    HeaderName.Empty, 
                    new HeaderName("dalion_app_id")
                };
                
                A.CallTo(() => _headerAppender.BuildStringToAppend(A<HeaderName>._))
                    .ReturnsLazily(call => call.GetArgument<HeaderName>(0) + ",");

                var actual = _sut.Compose(_httpRequest, _settings, _timeOfComposing);

                var expected = "(request-target),date,(expires),dalion_app_id,digest,";
                actual.Should().Be(expected);
            }
            
            [Fact]
            public void ComposesStringOutOfAllRequestedHeaders() {
                A.CallTo(() => _headerAppender.BuildStringToAppend(A<HeaderName>._))
                    .ReturnsLazily(call => "\n" + call.GetArgument<HeaderName>(0) + ",");

                var actual = _sut.Compose(_httpRequest, _settings, _timeOfComposing);

                var expected = "(request-target),\ndate,\n(expires),\ndalion_app_id,\ndigest,";
                actual.Should().Be(expected);
            }
        }
    }
}