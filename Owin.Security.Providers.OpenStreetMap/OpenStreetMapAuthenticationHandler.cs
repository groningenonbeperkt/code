using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Helpers;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using Owin.Security.Providers.OpenStreetMap.Messages;
using Owin.Security.Providers.OpenStreetMap.Provider;
using System.Net;
using System.Net.Http.Headers;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace Owin.Security.Providers.OpenStreetMap
{
    internal class OpenStreetMapAuthenticationHandler : AuthenticationHandler<OpenStreetMapAuthenticationOptions>
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const string StateCookie = "__OpenStreetMapState";
        private const string XmlSchemaString = "http://www.w3.org/2001/XMLSchema#string";
        private const string RequestTokenEndpoint = "https://api06.dev.openstreetmap.org/oauth/request_token";
        private const string AuthenticationEndpoint = "https://api06.dev.openstreetmap.org/oauth/authorize";
        private const string AccessTokenEndpoint = "https://api06.dev.openstreetmap.org/oauth/access_token";
        private const string RequestUserDetailsEndpoint = "https://api06.dev.openstreetmap.org/api/0.6/user/details";

        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public OpenStreetMapAuthenticationHandler(HttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _logger.WriteInformation("Authentication Handler initialized");
        }

        public override async Task<bool> InvokeAsync()
        {
            if (Options.CallbackPath.HasValue && Options.CallbackPath == Request.Path)
            {
                return await InvokeReturnPathAsync();
            }
            return false;
        }

        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            AuthenticationProperties properties = null;
            try
            {
                var query = Request.Query;
                var protectedRequestToken = Request.Cookies[StateCookie];

                var requestToken = Options.StateDataFormat.Unprotect(protectedRequestToken);

                if (requestToken == null)
                {
                    _logger.WriteWarning("Invalid state");
                    return null;
                }

                properties = requestToken.Properties;

                var returnedToken = query.Get("oauth_token");
                if (string.IsNullOrWhiteSpace(returnedToken))
                {
                    _logger.WriteWarning("Missing oauth_token");
                    return new AuthenticationTicket(null, properties);
                }

                if (returnedToken != requestToken.Token)
                {
                    _logger.WriteWarning("Unmatched token");
                    return new AuthenticationTicket(null, properties);
                }

                var oauthVerifier = query.Get("oauth_verifier");
                if (string.IsNullOrWhiteSpace(oauthVerifier))
                {
                    _logger.WriteWarning("Missing or blank oauth_verifier");
                    return new AuthenticationTicket(null, properties);
                }

                var accessToken = await ObtainAccessTokenAsync(Options.AppKey, Options.AppSecret, requestToken, oauthVerifier);

                var context = new OpenStreetMapAuthenticatedContext(Context, accessToken)
                {
                    Identity = new ClaimsIdentity(
                        Options.AuthenticationType,
                        ClaimsIdentity.DefaultNameClaimType,
                        ClaimsIdentity.DefaultRoleClaimType)
                };

                if (!string.IsNullOrEmpty(context.UserId))
                {
                    context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, context.UserId,
                        XmlSchemaString, Options.AuthenticationType));
                }

                if (!string.IsNullOrEmpty(context.UserName))
                {
                    var existingClaim = context.Identity.FindFirst(ClaimTypes.Name);
                    if (existingClaim != null)
                        context.Identity.RemoveClaim(existingClaim);

                    context.Identity.AddClaim(new Claim(ClaimTypes.Name, context.UserName,
                        XmlSchemaString, Options.AuthenticationType));
                }

                context.Properties = requestToken.Properties;

                Response.Cookies.Delete(StateCookie);

                await Options.Provider.Authenticated(context);

                return new AuthenticationTicket(context.Identity, context.Properties);
            }
            catch (Exception ex)
            {
                _logger.WriteError("Authentication failed", ex);
                return new AuthenticationTicket(null, properties);
            }
        }

        protected override async Task ApplyResponseChallengeAsync()
        {
            if (Response.StatusCode != 401)
            {
                return;
            }

            var challenge = Helper.LookupChallenge(Options.AuthenticationType, Options.AuthenticationMode);

            if (challenge != null)
            {
                var requestPrefix = Request.Scheme + "://" + Request.Host;
                var callBackUrl = requestPrefix + RequestPathBase + Options.CallbackPath;

                var extra = challenge.Properties;
                if (string.IsNullOrEmpty(extra.RedirectUri))
                {
                    extra.RedirectUri = requestPrefix + Request.PathBase + Request.Path + Request.QueryString;
                }

                var requestToken = await ObtainRequestTokenAsync(Options.AppKey, Options.AppSecret, callBackUrl, extra);

                if (requestToken.CallbackConfirmed)
                {
                    var OpenStreetMapAuthenticationEndpoint = AuthenticationEndpoint + "?oauth_token=" + requestToken.Token;

                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = Request.IsSecure
                    };

                    Response.StatusCode = 302;
                    Response.Cookies.Append(StateCookie, Options.StateDataFormat.Protect(requestToken), cookieOptions);
                    Response.Headers.Set("Location", OpenStreetMapAuthenticationEndpoint);
                }
                else
                {
                    _logger.WriteError("requestToken CallbackConfirmed!=true");
                }
            }
        }

        public async Task<bool> InvokeReturnPathAsync()
        {
            var model = await AuthenticateAsync();
            if (model == null)
            {
                _logger.WriteWarning("Invalid return state, unable to redirect.");
                Response.StatusCode = 500;
                return true;
            }

            var context = new OpenStreetMapReturnEndpointContext(Context, model)
            {
                SignInAsAuthenticationType = Options.SignInAsAuthenticationType,
                RedirectUri = model.Properties.RedirectUri
            };
            model.Properties.RedirectUri = null;

            await Options.Provider.ReturnEndpoint(context);

            if (context.SignInAsAuthenticationType != null && context.Identity != null)
            {
                var signInIdentity = context.Identity;
                if (!string.Equals(signInIdentity.AuthenticationType, context.SignInAsAuthenticationType, StringComparison.Ordinal))
                {
                    signInIdentity = new ClaimsIdentity(signInIdentity.Claims, context.SignInAsAuthenticationType, signInIdentity.NameClaimType, signInIdentity.RoleClaimType);
                }
                Context.Authentication.SignIn(context.Properties, signInIdentity);
            }

            if (context.IsRequestCompleted || context.RedirectUri == null) return context.IsRequestCompleted;
            if (context.Identity == null)
            {
                // add a redirect hint that sign-in failed in some way
                context.RedirectUri = WebUtilities.AddQueryString(context.RedirectUri, "error", "access_denied");
            }
            Response.Redirect(context.RedirectUri);
            context.RequestCompleted();

            return context.IsRequestCompleted;
        }

        private async Task<RequestToken> ObtainRequestTokenAsync(string appKey, string appSecret, string callBackUri, AuthenticationProperties properties)
        {
            _logger.WriteVerbose("ObtainRequestToken");

            var nonce = Guid.NewGuid().ToString("N");

            var authorizationParts = new SortedDictionary<string, string>
            {
                { "oauth_callback", callBackUri },
                { "oauth_consumer_key", appKey },
                { "oauth_nonce", nonce },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", GenerateTimeStamp() },
                { "oauth_version", "1.0" }
            };

            var parameterBuilder = new StringBuilder();
            foreach (var authorizationKey in authorizationParts)
            {
                parameterBuilder.AppendFormat("{0}={1}&", Uri.EscapeDataString(authorizationKey.Key), Uri.EscapeDataString(authorizationKey.Value));
            }
            parameterBuilder.Length--;
            var parameterString = parameterBuilder.ToString();

            var canonicalRequestBuilder = new StringBuilder();
            canonicalRequestBuilder.Append(HttpMethod.Post.Method);
            canonicalRequestBuilder.Append("&");
            canonicalRequestBuilder.Append(Uri.EscapeDataString(RequestTokenEndpoint));
            canonicalRequestBuilder.Append("&");
            canonicalRequestBuilder.Append(Uri.EscapeDataString(parameterString));

            var signature = ComputeSignature(appSecret, null, canonicalRequestBuilder.ToString());
            authorizationParts.Add("oauth_signature", signature);

            //--
            var authorizationHeaderBuilder = new StringBuilder();
            authorizationHeaderBuilder.Append("OAuth ");
            foreach (var authorizationPart in authorizationParts)
            {
                authorizationHeaderBuilder.AppendFormat(
                    "{0}=\"{1}\", ", authorizationPart.Key, Uri.EscapeDataString(authorizationPart.Value));
            }
            authorizationHeaderBuilder.Length = authorizationHeaderBuilder.Length - 2;

            var request = new HttpRequestMessage(HttpMethod.Post, RequestTokenEndpoint);
            request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());

            var response = await _httpClient.SendAsync(request, Request.CallCancelled);
            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();

            var responseParameters = WebHelpers.ParseForm(responseText);
            if (string.Equals(responseParameters["oauth_callback_confirmed"], "true", StringComparison.InvariantCulture))
            {
                return new RequestToken { Token = Uri.UnescapeDataString(responseParameters["oauth_token"]), TokenSecret = Uri.UnescapeDataString(responseParameters["oauth_token_secret"]), CallbackConfirmed = true, Properties = properties };
            }

            return new RequestToken();
        }

        private async Task<AccessToken> ObtainAccessTokenAsync(string appKey, string appSecret, RequestToken token, string verifier)
        {
            _logger.WriteVerbose("ObtainAccessToken");

            var nonce = Guid.NewGuid().ToString("N");

            var authorizationParts = new SortedDictionary<string, string>
            {
                { "oauth_consumer_key", appKey },
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
                parameterBuilder.AppendFormat("{0}={1}&", Uri.EscapeDataString(authorizationKey.Key), Uri.EscapeDataString(authorizationKey.Value));
            }
            parameterBuilder.Length--;
            var parameterString = parameterBuilder.ToString();

            var canonicalRequestBuilder = new StringBuilder();
            canonicalRequestBuilder.Append(HttpMethod.Post.Method);
            canonicalRequestBuilder.Append("&");
            canonicalRequestBuilder.Append(Uri.EscapeDataString(AccessTokenEndpoint));
            canonicalRequestBuilder.Append("&");
            canonicalRequestBuilder.Append(Uri.EscapeDataString(parameterString));

            var signature = ComputeSignature(appSecret, token.TokenSecret, canonicalRequestBuilder.ToString());
            authorizationParts.Add("oauth_signature", signature);

            var authorizationHeaderBuilder = new StringBuilder();
            authorizationHeaderBuilder.Append("OAuth ");
            foreach (var authorizationPart in authorizationParts)
            {
                authorizationHeaderBuilder.AppendFormat(
                    "{0}=\"{1}\", ", authorizationPart.Key, Uri.EscapeDataString(authorizationPart.Value));
            }
            authorizationHeaderBuilder.Length = authorizationHeaderBuilder.Length - 2;

            var request = new HttpRequestMessage(HttpMethod.Post, AccessTokenEndpoint);
            request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());

            var formPairs = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string, string>("oauth_verifier", verifier)
            };

            request.Content = new FormUrlEncodedContent(formPairs);

            var response = await _httpClient.SendAsync(request, Request.CallCancelled);

            if (!response.IsSuccessStatusCode)
            {
                _logger.WriteError("AccessToken request failed with a status code of " + response.StatusCode);
                response.EnsureSuccessStatusCode(); // throw
            }

            var responseText = await response.Content.ReadAsStringAsync();
            var responseParameters = WebHelpers.ParseForm(responseText);

            var Token = Uri.UnescapeDataString(responseParameters["oauth_token"]);
            var TokenSecret = Uri.UnescapeDataString(responseParameters["oauth_token_secret"]);

            return await ObtainAccessTokenWithUserDetailsAsync(Options.AppKey, Options.AppSecret, Token, TokenSecret);
        }

        private async Task<AccessToken> ObtainAccessTokenWithUserDetailsAsync(string appKey, string appSecret, string token, string tokenSecret)
        {
            _logger.WriteVerbose("ObtainAccessTokenWithUserDetailsAsync");

            var nonce = Guid.NewGuid().ToString("N");

            var authorizationParts = new SortedDictionary<string, string>
            {
                { "oauth_consumer_key", appKey },
                { "oauth_nonce", nonce },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_token", token },
                { "oauth_timestamp", GenerateTimeStamp() },
                { "oauth_version", "1.0" },
            };

            var parameterBuilder = new StringBuilder();
            foreach (var authorizationKey in authorizationParts)
            {
                parameterBuilder.AppendFormat("{0}={1}&", Uri.EscapeDataString(authorizationKey.Key), Uri.EscapeDataString(authorizationKey.Value));
            }
            parameterBuilder.Length--;
            var parameterString = parameterBuilder.ToString();

            var canonicalRequestBuilder = new StringBuilder();
            canonicalRequestBuilder.Append(HttpMethod.Get.Method);
            canonicalRequestBuilder.Append("&");
            canonicalRequestBuilder.Append(Uri.EscapeDataString(RequestUserDetailsEndpoint));
            canonicalRequestBuilder.Append("&");
            canonicalRequestBuilder.Append(Uri.EscapeDataString(parameterString));

            var signature = ComputeSignature(appSecret, tokenSecret, canonicalRequestBuilder.ToString());
            authorizationParts.Add("oauth_signature", signature);

            var authorizationHeaderBuilder = new StringBuilder();
            authorizationHeaderBuilder.Append("OAuth ");
            foreach (var authorizationPart in authorizationParts)
            {
                authorizationHeaderBuilder.AppendFormat(
                    "{0}=\"{1}\", ", authorizationPart.Key, Uri.EscapeDataString(authorizationPart.Value));
            }
            authorizationHeaderBuilder.Length = authorizationHeaderBuilder.Length - 2;

            var request = new HttpRequestMessage(HttpMethod.Get, RequestUserDetailsEndpoint);
            request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());
            request.Headers.Add("Accept", "application/xml");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.WriteError("User details request failed with a status code of " + response.StatusCode);
                response.EnsureSuccessStatusCode();
            }

            var responseText = await response.Content.ReadAsStringAsync();
            XDocument xdocUserDetails = XDocument.Parse(responseText);

           // string test = await getApiResult(appKey, appSecret, token, tokenSecret);

            return new AccessToken
            {
                UserId = xdocUserDetails.Elements().Select(x => x.Element("user").Attribute("id").Value).First(),
                UserName = xdocUserDetails.Elements().Select(x => x.Element("user").Attribute("display_name").Value).First(),
                Token = token,
                TokenSecret = tokenSecret,
            };

        }

        private static string GenerateTimeStamp()
        {
            var secondsSinceUnixEpochStart = DateTime.UtcNow - Epoch;
            return Convert.ToInt64(secondsSinceUnixEpochStart.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        private static string ComputeSignature(string appSecret, string tokenSecret, string signatureData)
        {
            using (var algorithm = new HMACSHA1())
            {
                algorithm.Key = Encoding.ASCII.GetBytes(
                    string.Format(CultureInfo.InvariantCulture,
                        "{0}&{1}",
                        Uri.EscapeDataString(appSecret),
                        string.IsNullOrEmpty(tokenSecret) ? string.Empty : Uri.EscapeDataString(tokenSecret)));
                var hash = algorithm.ComputeHash(Encoding.ASCII.GetBytes(signatureData));
                return Convert.ToBase64String(hash);
            }
        }


        //onderstaande code is om de api te testen
        //    private async Task<String> getApiResult(string appKey, string appSecret, string token, string tokenSecret)
        //    {
        //        string way = "4303058092";

        //        var request = new HttpRequestMessage(HttpMethod.Get, "https://master.apis.dev.openstreetmap.org/api/0.6/way/"+way);
        //        request.Headers.Add("Accept", "application/xml");

        //        var response = await _httpClient.SendAsync(request);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            _logger.WriteError("User details request failed with a status code of " + response.StatusCode);
        //            response.EnsureSuccessStatusCode();
        //        }

        //        var responseText = await response.Content.ReadAsStringAsync();
        //        XDocument xdocWayDetails = XDocument.Parse(responseText);

        //        var version = xdocWayDetails.Elements().Select(x => x.Element("way").Attribute("version").Value).Single();
        //      //  var test  = xdocWayDetails.Elements().Select(x => x.Element("way")).Elements();
        //       // xdocWayDetails.Descendants("tag").Where(x => (String)x.Attribute("k") == "wheelchair").Select(x => x).Single().Value = "No";



        //        string url = "https://api06.dev.openstreetmap.org/api/0.6/changeset/create";
        //        string xmlCreateTest = "<osm><changeset version=\"0.6\" generator=\"tfe\"><tag k=\"created_by\" v=\"tfe\"/>" +
        //                               "<tag k=\"comment\" v=\"surface gewijzigd\"/></changeset></osm>";

        //        var createRequest = getHttpRequestMessageWithOauth(appKey, appSecret, token, tokenSecret, url, HttpMethod.Put);
        //        createRequest.Content = new StringContent(xmlCreateTest, Encoding.UTF8, "text/xml");

        //        var createResponse = await _httpClient.SendAsync(createRequest);

        //        if (!createResponse.IsSuccessStatusCode)
        //        {
        //            _logger.WriteError("User details request failed with a status code of " + createResponse.StatusCode);
        //            createResponse.EnsureSuccessStatusCode();
        //        }

        //        var changesetId = await createResponse.Content.ReadAsStringAsync();


        //        /////
        //        string url2 = "https://api06.dev.openstreetmap.org/api/0.6/changeset/" + changesetId + "/upload";
        //        string xmlUploadTest = "<osmChange version=\"0.6\" generator=\"tfe\"><modify>" +
        //            "<way id=\""+ way + "\" version=\""+ version + "\" changeset=\"" + changesetId + "\">" +
        //                               "<nd ref=\"4306399267\"/>" +
        //                               "<nd ref=\"4306399268\"/>" +
        //                               "<tag k=\"highway\" v=\"residential\"/><tag k=\"name\" v=\"testtname\"/>" +
        //                               "<tag k=\"wheelchair:description:nl\" v=\"YEAH dit stukje is zeker niet rolstoelvriendelijk\"/>" +
        //            "</way></modify></osmChange>";

        //        var uploadRequest = getHttpRequestMessageWithOauth(appKey, appSecret, token, tokenSecret, url2, HttpMethod.Post);
        //      //  uploadRequest.Headers.Add("Accept", "text/xml");
        //        uploadRequest.Content = new StringContent(xmlUploadTest, Encoding.UTF8, "application/xml");

        //        var uploadResponse = await _httpClient.SendAsync(uploadRequest);

        //        if (!uploadResponse.IsSuccessStatusCode)
        //        {
        //           // _logger.WriteError("User details request failed with a status code of " + uploadResponse.StatusCode);
        //            //uploadResponse.EnsureSuccessStatusCode();
        //        }

        //        var result = await uploadResponse.Content.ReadAsStringAsync();


        //        ////
        //        string url3 = "https://api06.dev.openstreetmap.org/api/0.6/changeset/" + changesetId + "/close";
        //        var closeRequest = getHttpRequestMessageWithOauth(appKey, appSecret, token, tokenSecret, url3, HttpMethod.Put);

        //        var closeResponse = await _httpClient.SendAsync(closeRequest);

        //        if (!closeResponse.IsSuccessStatusCode)
        //        {
        //            _logger.WriteError("User details request failed with a status code of " + closeResponse.StatusCode);
        //            closeResponse.EnsureSuccessStatusCode();
        //        }

        //        return " ";
        //        //XDocument xdocUserDetails = XDocument.Parse(responseText);

        //    }

        //    private HttpRequestMessage getHttpRequestMessageWithOauth(string appKey, string appSecret, string token, string tokenSecret, string url, HttpMethod method )
        //    {
        //        var nonce = Guid.NewGuid().ToString("N");

        //        var authorizationParts = new SortedDictionary<string, string>
        //        {
        //            { "oauth_consumer_key", appKey },
        //            { "oauth_nonce", nonce },
        //            { "oauth_signature_method", "HMAC-SHA1" },
        //            { "oauth_token", token },
        //            { "oauth_timestamp", GenerateTimeStamp() },
        //            { "oauth_version", "1.0" },
        //        };

        //        var parameterBuilder = new StringBuilder();
        //        foreach (var authorizationKey in authorizationParts)
        //        {
        //            parameterBuilder.AppendFormat("{0}={1}&", Uri.EscapeDataString(authorizationKey.Key), Uri.EscapeDataString(authorizationKey.Value));
        //        }
        //        parameterBuilder.Length--;
        //        var parameterString = parameterBuilder.ToString();

        //        var canonicalRequestBuilder = new StringBuilder();
        //        canonicalRequestBuilder.Append(method.Method);
        //        canonicalRequestBuilder.Append("&");
        //        canonicalRequestBuilder.Append(Uri.EscapeDataString(url));
        //        canonicalRequestBuilder.Append("&");
        //        canonicalRequestBuilder.Append(Uri.EscapeDataString(parameterString));

        //        var signature = ComputeSignature(appSecret, tokenSecret, canonicalRequestBuilder.ToString());
        //        authorizationParts.Add("oauth_signature", signature);

        //        var authorizationHeaderBuilder = new StringBuilder();
        //        authorizationHeaderBuilder.Append("OAuth ");
        //        foreach (var authorizationPart in authorizationParts)
        //        {
        //            authorizationHeaderBuilder.AppendFormat(
        //                "{0}=\"{1}\", ", authorizationPart.Key, Uri.EscapeDataString(authorizationPart.Value));
        //        }
        //        authorizationHeaderBuilder.Length = authorizationHeaderBuilder.Length - 2;

        //        var request = new HttpRequestMessage(method, url);

        //        request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());

        //        return request;
        //    }
    }
}
