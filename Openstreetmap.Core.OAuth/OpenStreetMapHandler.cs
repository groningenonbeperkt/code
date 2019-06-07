// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System.Xml.Linq;
using System.Linq;

namespace Microsoft.AspNetCore.Authentication.OpenStreetMap
{
    internal class OpenStreetMapHandler : RemoteAuthenticationHandler<OpenStreetMapOptions>
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string StateCookie = "__OpenStreetMapState";

        private const string RequestTokenEndpoint = "http://www.openstreetmap.org/oauth/request_token";
        private const string AccessTokenEndpoint = "http://www.openstreetmap.org/oauth/access_token";
        private const string AuthenticationEndpoint = "http://www.openstreetmap.org/oauth/authorize?oauth_token=";
        private const string RequestUserDetailsEndpoint = "http://api.openstreetmap.org/api/0.6/user/details";

        private const string RequestTokenDevEndpoint = "https://master.apis.dev.openstreetmap.org/oauth/request_token";
        private const string AuthenticationDevEndpoint = "https://master.apis.dev.openstreetmap.org/oauth/authorize?oauth_token=";
        private const string AccessTokenDevEndpoint = "https://master.apis.dev.openstreetmap.org/oauth/access_token";
        private const string RequestUserDetailsDevEndpoint = "https://master.apis.dev.openstreetmap.org/api/0.6/user/details";

        private readonly HttpClient _httpClient;

        public OpenStreetMapHandler(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        protected override async Task<AuthenticateResult> HandleRemoteAuthenticateAsync()
        {
            AuthenticationProperties properties = null;
            var query = Request.Query;
            var protectedRequestToken = Request.Cookies[StateCookie];

            var requestToken = Options.StateDataFormat.Unprotect(protectedRequestToken);

            if (requestToken == null)
            {
                return AuthenticateResult.Fail("Invalid state cookie.");
            }

            properties = requestToken.Properties;

            // REVIEW: see which of these are really errors

            var returnedToken = query["oauth_token"];
            if (StringValues.IsNullOrEmpty(returnedToken))
            {
                return AuthenticateResult.Fail("Missing oauth_token");
            }

            if (!string.Equals(returnedToken, requestToken.Token, StringComparison.Ordinal))
            {
                return AuthenticateResult.Fail("Unmatched token");
            }

            var oauthVerifier = query["oauth_verifier"];
            if (StringValues.IsNullOrEmpty(oauthVerifier))
            {
                return AuthenticateResult.Fail("Missing or blank oauth_verifier");
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps
            };

            Response.Cookies.Delete(StateCookie, cookieOptions);

            var accessToken = await ObtainAccessTokenAsync(requestToken, oauthVerifier);

            var identity = new ClaimsIdentity(new List<Claim>(), Options.ClaimsIssuer);

            JObject user = null;
            if (Options.RetrieveUserDetails)
            {
                user = await RetrieveUserDetailsAsync(accessToken, identity);
            }

            if (Options.SaveTokens)
            {
                properties.StoreTokens(new [] {
                    new AuthenticationToken { Name = "access_token", Value = accessToken.Token },
                    new AuthenticationToken { Name = "access_token_secret", Value = accessToken.TokenSecret }
                });

                
            }

            return AuthenticateResult.Success(await CreateTicketAsync(identity, properties, accessToken, user));
        }

        protected virtual async Task<AuthenticationTicket> CreateTicketAsync(
            ClaimsIdentity identity, AuthenticationProperties properties, AccessToken token, JObject user)
        {
            var context = new OpenStreetMapCreatingTicketContext(Context, Options, token.UserId, token.ScreenName, token.Token, token.TokenSecret, user)
            {
                Principal = new ClaimsPrincipal(identity),
                Properties = properties
            };

            await Options.Events.CreatingTicket(context);

            if (context.Principal?.Identity == null)
            {
                return null;
            }

            return new AuthenticationTicket(context.Principal, context.Properties, Options.AuthenticationScheme);
        }

        protected override async Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var properties = new AuthenticationProperties(context.Properties)
            {
                ExpiresUtc = Options.SystemClock.UtcNow.Add(Options.RemoteAuthenticationTimeout)
            };

            if (string.IsNullOrEmpty(properties.RedirectUri))
            {
                properties.RedirectUri = CurrentUri;
            }

            // If CallbackConfirmed is false, this will throw
            var requestToken = await ObtainRequestTokenAsync(BuildRedirectUri(Options.CallbackPath), properties);
            var resource_url = "";
            if(Options.UseDevelopmentUrls)
            {
                resource_url = AuthenticationDevEndpoint;
            }
            else
            {
                resource_url = AuthenticationEndpoint;
            }

            var openStreetmapAuthenticationEndpoint = resource_url + requestToken.Token;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps
            };

            Response.Cookies.Append(StateCookie, Options.StateDataFormat.Protect(requestToken), cookieOptions);

            var redirectContext = new OpenStreetMapRedirectToAuthorizationEndpointContext(
                Context, Options,
                properties, openStreetmapAuthenticationEndpoint);
            await Options.Events.RedirectToAuthorizationEndpoint(redirectContext);
            return true;
        }

        private async Task<RequestToken> ObtainRequestTokenAsync(string callBackUri, AuthenticationProperties properties)
        {
            Logger.ObtainRequestToken();

            var nonce = Guid.NewGuid().ToString("N");

            var authorizationParts = new SortedDictionary<string, string>
            {
                { "oauth_callback", callBackUri },
                { "oauth_consumer_key", Options.ConsumerKey },
                { "oauth_nonce", nonce },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", GenerateTimeStamp() },
                { "oauth_version", "1.0" }
            };

            var parameterBuilder = new StringBuilder();
            foreach (var authorizationKey in authorizationParts)
            {
                parameterBuilder.AppendFormat("{0}={1}&", UrlEncoder.Encode(authorizationKey.Key), UrlEncoder.Encode(authorizationKey.Value));
            }
            parameterBuilder.Length--;
            var parameterString = parameterBuilder.ToString();

            var resource_url = "";
            if(Options.UseDevelopmentUrls)
            {
                resource_url = RequestTokenDevEndpoint;
            }
            else
            {
                resource_url = RequestTokenEndpoint;
            }

            var canonicalizedRequestBuilder = new StringBuilder();
            canonicalizedRequestBuilder.Append(HttpMethod.Post.Method);
            canonicalizedRequestBuilder.Append("&");
            canonicalizedRequestBuilder.Append(UrlEncoder.Encode(resource_url));
            canonicalizedRequestBuilder.Append("&");
            canonicalizedRequestBuilder.Append(UrlEncoder.Encode(parameterString));

            var signature = ComputeSignature(Options.ConsumerSecret, null, canonicalizedRequestBuilder.ToString());
            authorizationParts.Add("oauth_signature", signature);

            var authorizationHeaderBuilder = new StringBuilder();
            authorizationHeaderBuilder.Append("OAuth ");
            foreach (var authorizationPart in authorizationParts)
            {
                authorizationHeaderBuilder.AppendFormat(
                    "{0}=\"{1}\", ", authorizationPart.Key, UrlEncoder.Encode(authorizationPart.Value));
            }
            authorizationHeaderBuilder.Length = authorizationHeaderBuilder.Length - 2;

            var request = new HttpRequestMessage(HttpMethod.Post, resource_url);
            request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());

            var response = await _httpClient.SendAsync(request, Context.RequestAborted);
            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();

            var responseParameters = new FormCollection(new FormReader(responseText).ReadForm());
            if (!string.Equals(responseParameters["oauth_callback_confirmed"], "true", StringComparison.Ordinal))
            {
                throw new Exception("Twitter oauth_callback_confirmed is not true.");
            }

            return new RequestToken { Token = Uri.UnescapeDataString(responseParameters["oauth_token"]), TokenSecret = Uri.UnescapeDataString(responseParameters["oauth_token_secret"]), CallbackConfirmed = true, Properties = properties };
        }

        private async Task<AccessToken> ObtainAccessTokenAsync(RequestToken token, string verifier)
        {
            // https://dev.twitter.com/docs/api/1/post/oauth/access_token

            Logger.ObtainAccessToken();

            var nonce = Guid.NewGuid().ToString("N");

            var authorizationParts = new SortedDictionary<string, string>
            {
                { "oauth_consumer_key", Options.ConsumerKey },
                { "oauth_nonce", nonce },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_token", token.Token },
                { "oauth_timestamp", GenerateTimeStamp() },
                { "oauth_verifier", verifier },
                { "oauth_version", "1.0" },
            };

            var parameterBuilder = new StringBuilder();
            foreach (var authorizationKey in authorizationParts)
            {
                parameterBuilder.AppendFormat("{0}={1}&", UrlEncoder.Encode(authorizationKey.Key), UrlEncoder.Encode(authorizationKey.Value));
            }
            parameterBuilder.Length--;
            var parameterString = parameterBuilder.ToString();

            var resource_url = "";
            if(Options.UseDevelopmentUrls)
            {
                resource_url = AccessTokenDevEndpoint;
            }
            else
            {
                resource_url = AccessTokenEndpoint;
            }

            var canonicalizedRequestBuilder = new StringBuilder();
            canonicalizedRequestBuilder.Append(HttpMethod.Post.Method);
            canonicalizedRequestBuilder.Append("&");
            canonicalizedRequestBuilder.Append(UrlEncoder.Encode(resource_url));
            canonicalizedRequestBuilder.Append("&");
            canonicalizedRequestBuilder.Append(UrlEncoder.Encode(parameterString));

            var signature = ComputeSignature(Options.ConsumerSecret, token.TokenSecret, canonicalizedRequestBuilder.ToString());
            authorizationParts.Add("oauth_signature", signature);
            authorizationParts.Remove("oauth_verifier");

            var authorizationHeaderBuilder = new StringBuilder();
            authorizationHeaderBuilder.Append("OAuth ");
            foreach (var authorizationPart in authorizationParts)
            {
                authorizationHeaderBuilder.AppendFormat(
                    "{0}=\"{1}\", ", authorizationPart.Key, UrlEncoder.Encode(authorizationPart.Value));
            }
            authorizationHeaderBuilder.Length = authorizationHeaderBuilder.Length - 2;

            var request = new HttpRequestMessage(HttpMethod.Post, resource_url);
            request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());

            var formPairs = new Dictionary<string, string>()
            {
                { "oauth_verifier", verifier },
            };

            request.Content = new FormUrlEncodedContent(formPairs);

            var response = await _httpClient.SendAsync(request, Context.RequestAborted);

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("AccessToken request failed with a status code of " + response.StatusCode);
                response.EnsureSuccessStatusCode(); // throw
            }

            var responseText = await response.Content.ReadAsStringAsync();
            var responseParameters = new FormCollection(new FormReader(responseText).ReadForm());

            return new AccessToken
            {
                Token = Uri.UnescapeDataString(responseParameters["oauth_token"]),
                TokenSecret = Uri.UnescapeDataString(responseParameters["oauth_token_secret"]),
            };
        }

        // https://dev.twitter.com/rest/reference/get/account/verify_credentials
        private async Task<JObject> RetrieveUserDetailsAsync(AccessToken accessToken, ClaimsIdentity identity)
        {
            Logger.RetrieveUserDetails();

            var nonce = Guid.NewGuid().ToString("N");

            var authorizationParts = new SortedDictionary<string, string>
                        {
                            { "oauth_consumer_key", Options.ConsumerKey },
                            { "oauth_nonce", nonce },
                            { "oauth_signature_method", "HMAC-SHA1" },
                            { "oauth_timestamp", GenerateTimeStamp() },
                            { "oauth_token", accessToken.Token },
                            { "oauth_version", "1.0" }
                        };

            var parameterBuilder = new StringBuilder();
            foreach (var authorizationKey in authorizationParts)
            {
                parameterBuilder.AppendFormat("{0}={1}&", UrlEncoder.Encode(authorizationKey.Key), UrlEncoder.Encode(authorizationKey.Value));
            }
            parameterBuilder.Length--;
            var parameterString = parameterBuilder.ToString();

            var resource_url = "";
            if(Options.UseDevelopmentUrls)
            {
                resource_url = RequestUserDetailsDevEndpoint;
            }
            else
            {
                resource_url = RequestUserDetailsEndpoint;
            }

            var resource_query = "include_email=true";
            var canonicalizedRequestBuilder = new StringBuilder();
            canonicalizedRequestBuilder.Append(HttpMethod.Get.Method);
            canonicalizedRequestBuilder.Append("&");
            canonicalizedRequestBuilder.Append(UrlEncoder.Encode(resource_url));
            canonicalizedRequestBuilder.Append("&");
            canonicalizedRequestBuilder.Append(UrlEncoder.Encode(resource_query));
            canonicalizedRequestBuilder.Append("%26");
            canonicalizedRequestBuilder.Append(UrlEncoder.Encode(parameterString));

            var signature = ComputeSignature(Options.ConsumerSecret, accessToken.TokenSecret, canonicalizedRequestBuilder.ToString());
            authorizationParts.Add("oauth_signature", signature);

            var authorizationHeaderBuilder = new StringBuilder();
            authorizationHeaderBuilder.Append("OAuth ");
            foreach (var authorizationPart in authorizationParts)
            {
                authorizationHeaderBuilder.AppendFormat(
                    "{0}=\"{1}\", ", authorizationPart.Key, UrlEncoder.Encode(authorizationPart.Value));
            }
            authorizationHeaderBuilder.Length = authorizationHeaderBuilder.Length - 2;

            var request = new HttpRequestMessage(HttpMethod.Get, resource_url + "?include_email=true");
            request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());

            var response = await _httpClient.SendAsync(request, Context.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("Email request failed with a status code of " + response.StatusCode);
                response.EnsureSuccessStatusCode(); // throw
            }
            var responseText = await response.Content.ReadAsStringAsync();

            XDocument xdocUserDetails = XDocument.Parse(responseText);

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, xdocUserDetails.Elements().Select(x => x.Element("user").Attribute("id").Value).First(), ClaimValueTypes.String, Options.ClaimsIssuer));
            identity.AddClaim(new Claim(ClaimTypes.Name, xdocUserDetails.Elements().Select(x => x.Element("user").Attribute("display_name").Value).First(), ClaimValueTypes.String, Options.ClaimsIssuer));
            identity.AddClaim(new Claim(OpenstreetmapClaimTypes.AccessToken, accessToken.Token, ClaimValueTypes.String, Options.ClaimsIssuer));
            identity.AddClaim(new Claim(OpenstreetmapClaimTypes.AccessTokenSecret, accessToken.TokenSecret, ClaimValueTypes.String, Options.ClaimsIssuer));

            return null;
        }

        private static string GenerateTimeStamp()
        {
            var secondsSinceUnixEpocStart = DateTime.UtcNow - Epoch;
            return Convert.ToInt64(secondsSinceUnixEpocStart.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        private string ComputeSignature(string consumerSecret, string tokenSecret, string signatureData)
        {
            using (var algorithm = new HMACSHA1())
            {
                algorithm.Key = Encoding.ASCII.GetBytes(
                    string.Format(CultureInfo.InvariantCulture,
                        "{0}&{1}",
                        UrlEncoder.Encode(consumerSecret),
                        string.IsNullOrEmpty(tokenSecret) ? string.Empty : UrlEncoder.Encode(tokenSecret)));
                var hash = algorithm.ComputeHash(Encoding.ASCII.GetBytes(signatureData));
                return Convert.ToBase64String(hash);
            }
        }
    }
}