using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using GroniningenOnbeperkt.NetCore.Openstreetmap.TagEditor.Models;

namespace GroniningenOnbeperkt.NetCore.Openstreetmap.TagEditor
{
    internal class OpenStreetMapAccessUrl
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>
        /// Get HTTP requestMessage with OAuth 1.0a protocol for access to the OpenStreetMap API 
        /// </summary>
        /// <param name="accessData"></param>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static HttpRequestMessage getHttpRequestMessage(OpenStreetMapAccessModel accessData, string url, HttpMethod method)
        {
            var nonce = Guid.NewGuid().ToString("N");

            var authorizationParts = new SortedDictionary<string, string>
            {
                { "oauth_consumer_key", accessData.AppKey },
                { "oauth_nonce", nonce },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_token", accessData.AccessToken },
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
            canonicalRequestBuilder.Append(method.Method);
            canonicalRequestBuilder.Append("&");
            canonicalRequestBuilder.Append(Uri.EscapeDataString(url));
            canonicalRequestBuilder.Append("&");
            canonicalRequestBuilder.Append(Uri.EscapeDataString(parameterString));

            var signature = ComputeSignature(accessData.AppSecret, accessData.AccessTokenSecret, canonicalRequestBuilder.ToString());
            authorizationParts.Add("oauth_signature", signature);

            var authorizationHeaderBuilder = new StringBuilder();
            authorizationHeaderBuilder.Append("OAuth ");
            foreach (var authorizationPart in authorizationParts)
            {
                authorizationHeaderBuilder.AppendFormat(
                    "{0}=\"{1}\", ", authorizationPart.Key, Uri.EscapeDataString(authorizationPart.Value));
            }
            authorizationHeaderBuilder.Length = authorizationHeaderBuilder.Length - 2;

            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());

            return request;
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
    }
}
