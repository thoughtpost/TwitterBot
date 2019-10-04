using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Thoughtpost;
using Microsoft.Extensions.Configuration;

using Thoughtpost.Twitter.Models;

namespace Thoughtpost.Twitter
{
    public class TwitterApp
    {
        public TwitterApp(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; set; }
        public string OAuthConsumerKey
        {
            get
            {
                return Configuration.GetSection("Twitter")["ConsumerKey"];
            }
        }

        public string OAuthConsumerSecret
        {
            get
            {
                return Configuration.GetSection("Twitter")["ConsumerSecret"];
            }
        }

        public string AdminUserAccessToken
        {
            get
            {
                return Configuration.GetSection("Twitter")["AdminUserAccessToken"];
            }
        }

        public string AdminUserAccessTokenSecret
        {
            get
            {
                return Configuration.GetSection("Twitter")["AdminUserAccessTokenSecret"];
            }
        }

        public string WebhookUrl
        {
            get
            {
                return Configuration.GetSection("Twitter")["WebhookUrl"];
            }
        }

        public string Environment
        {
            get
            {
                return Configuration.GetSection("Twitter")["Environment"];
            }
        }

        public string AppName
        {
            get
            {
                return Configuration.GetSection("Twitter")["AppName"];
            }
        }

        public string UserAccessToken { get; set; }
        public string UserAccessTokenSecret { get; set; }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public async Task<byte[]> GetBytes(string url)
        {
            string authHeader = GenerateAuthorizationHeader(url, "GET");

            HttpResponseMessage response;
            using (HttpClient client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization",
                    authHeader);

                response = await client.GetAsync(url);
            }

            byte[] buffer = await response.Content.ReadAsByteArrayAsync();

            return buffer;
        }

        public async Task<string> Get( string url )
        {
            string authHeader = GenerateAuthorizationHeader(url, "GET");

            HttpResponseMessage response;
            using (HttpClient client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization",
                    authHeader);

                response = await client.GetAsync(url);
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();

            return jsonResponse;
        }

        public async Task<string> GetApi(string url)
        {
            string authHeader = await GetBearerToken();

            string bearer = "Bearer " + authHeader;

            HttpWebRequest authRequest = (HttpWebRequest)WebRequest.Create(url);
            authRequest.Headers.Add("Authorization", bearer);
            authRequest.Method = "GET";
            authRequest.UserAgent = "OAuth gem v0.4.4";
            authRequest.Host = "api.twitter.com";
            authRequest.ServicePoint.Expect100Continue = false;
            authRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            WebResponse authResponse = authRequest.GetResponse();

            string resp = "";
            Stream s = authResponse.GetResponseStream();
            StreamReader sr = new StreamReader(s);

            resp = sr.ReadToEnd();

            authResponse.Close();

            return resp;
        }

        public async Task SendTweet(string message)
        {
            string oAuthUrl = "https://api.twitter.com/1.1/statuses/update.json";
            string authHeader = GenerateAuthorizationHeaderPlus(message, oAuthUrl, "POST");
            string postBody = "status=" + Uri.EscapeDataString(message);

            HttpWebRequest authRequest = (HttpWebRequest)WebRequest.Create(oAuthUrl);
            authRequest.Headers.Add("Authorization", authHeader);
            authRequest.Method = "POST";
            authRequest.UserAgent = "OAuth gem v0.4.4";
            authRequest.Host = "api.twitter.com";
            authRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            authRequest.ServicePoint.Expect100Continue = false;
            authRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (Stream stream = authRequest.GetRequestStream())
            {
                byte[] content = Encoding.UTF8.GetBytes(postBody);
                stream.Write(content, 0, content.Length);
            }

            WebResponse authResponse = await authRequest.GetResponseAsync();
            string jsonResponse = new StreamReader(authResponse.GetResponseStream()).ReadToEnd();

            authResponse.Close();
        }


        public async Task SendDirectMessage(string recipientId, string message)
        {
            MessageData md = new MessageData() { Text = message };

            await SendDirectMessage(recipientId, md);
        }

        public async Task SendDirectMessage(string recipientId, MessageData message_data)
        {
            string json = JsonConvert.SerializeObject(message_data);

            string oAuthUrl = "https://api.twitter.com/1.1/direct_messages/events/new.json";
            string authHeader = GenerateAuthorizationHeader(oAuthUrl, "POST");
            string postBody = "{\"event\": { \"type\": \"message_create\", " +
                "\"message_create\": { \"target\": { \"recipient_id\": \"" + recipientId +
                "\" }, \"message_data\": " + json + "  } } }";

            HttpResponseMessage response;
            using (HttpClient client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization",
                    authHeader);

                response = await client.PostAsync(oAuthUrl, new StringContent(postBody,
                    Encoding.UTF8, "application/json"));
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
        }

#pragma warning disable 1998
        public async Task SendTyping(string recipientId)
        {
            string oAuthUrl = "https://api.twitter.com/1.1/direct_messages/indicate_typing.json";
            string authHeader = GenerateAuthorizationHeaderPlus("recipient_id", recipientId, oAuthUrl,
                "POST");
            string postBody = "recipient_id=" + Uri.EscapeDataString(recipientId);

            HttpWebRequest authRequest = (HttpWebRequest)WebRequest.Create(oAuthUrl);
            authRequest.Headers.Add("Authorization", authHeader);
            authRequest.Method = "POST";
            authRequest.UserAgent = "OAuth gem v0.4.4";
            authRequest.Host = "api.twitter.com";
            authRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            authRequest.ServicePoint.Expect100Continue = false;
            authRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (Stream stream = authRequest.GetRequestStream())
            {
                byte[] content = Encoding.UTF8.GetBytes(postBody);
                stream.Write(content, 0, content.Length);
            }

            WebResponse authResponse = authRequest.GetResponse();
            string jsonResponse = new StreamReader(authResponse.GetResponseStream()).ReadToEnd();

            authResponse.Close();
        }
#pragma warning restore 1998

        public async Task<string> SetWebhook(string hook)
        {
            string oAuthUrl = "https://api.twitter.com/1.1/account_activity/all/" + 
                this.Environment + "/webhooks.json?url=" + Uri.EscapeUriString(hook);
            string authHeader2 = Build(HttpMethod.Post, oAuthUrl);

            HttpResponseMessage response;
            using (HttpClient client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization",
                    authHeader2);

                response = await client.PostAsync(oAuthUrl, new StringContent(""));
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();

            return jsonResponse;
        }


        public async Task<string> DeleteWebhook(string hookid)
        {
            string oAuthUrl = "https://api.twitter.com/1.1/account_activity/all/" + 
                this.Environment + "/webhooks/" + hookid + ".json";
            string authHeader2 = Build(HttpMethod.Delete, oAuthUrl);
            authHeader2 = GenerateAuthorizationHeader(oAuthUrl, "DELETE");

            HttpResponseMessage response;
            using (HttpClient client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization",
                    authHeader2);

                response = await client.DeleteAsync(oAuthUrl);
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();

            return jsonResponse;
        }
        public async Task<string> SubscribeToWebhook()
        {
            string oAuthUrl =  oAuthUrl = "https://api.twitter.com/1.1/account_activity/all/" + 
                this.Environment + "/subscriptions.json";
            string authHeader2 = GenerateAuthorizationHeader(oAuthUrl);

            HttpResponseMessage response;
            using (HttpClient client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization",
                    authHeader2);

                response = await client.PostAsync(oAuthUrl, new StringContent(""));
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();

            return jsonResponse;
        }

        public async Task<string> UnsubscribeToWebhook(string id)
        {
            string oAuthUrl = "https://api.twitter.com/1.1/account_activity/all/" + 
                this.Environment + "/subscriptions.json";
            string authHeader2 = GenerateAuthorizationHeader(oAuthUrl, "DELETE");

            HttpResponseMessage response;
            using (HttpClient client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization",
                    authHeader2);

                response = await client.DeleteAsync(oAuthUrl);
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();

            return jsonResponse;
        }

        public static async Task<HttpClient> CreateHttpClient(string consumerKey, string consumerSecret)
        {
            var bearerToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(consumerKey + ":" + consumerSecret));
            string url = "https://api.twitter.com/oauth2/token";


            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Basic " + bearerToken);

            var resp = await client.PostAsync(url, new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded")).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var result = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            var jObj = JObject.Parse(result);

            if (jObj["token_type"].ToString() != "bearer") throw new Exception("Invalid Response From Twitter/OAuth");

            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + jObj["access_token"]);
            return client;
        }

        public async Task<string> GetBearerToken()
        {

            var baseUri = new Uri("https://api.twitter.com/");
            var encodedConsumerKey = HttpUtility.UrlEncode(OAuthConsumerKey);
            var encodedConsumerKeySecret = HttpUtility.UrlEncode(OAuthConsumerSecret);
            var encodedPair = Base64Encode(String.Format("{0}:{1}", encodedConsumerKey, encodedConsumerKeySecret));

            var requestToken = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(baseUri, "oauth2/token"),
                Content = new StringContent("grant_type=client_credentials")
            };

            requestToken.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded") { CharSet = "UTF-8" };
            requestToken.Headers.TryAddWithoutValidation("Authorization", String.Format("Basic {0}", encodedPair));

            HttpClient client = new HttpClient();
            var bearerResult = await client.SendAsync(requestToken);
            var bearerData = await bearerResult.Content.ReadAsStringAsync();
            var bearerToken = JObject.Parse(bearerData)["access_token"].ToString();

            return bearerToken;
        }

        public async Task<string> GetWebhook()
        {
            string oAuthUrl = "https://api.twitter.com/1.1/account_activity/all/" + 
                this.Environment + "/webhooks.json";
            string resp = await GetApi(oAuthUrl);
            return resp;
        }

        public async Task<string> GetWebhookSubscriptions()
        {
            string oAuthUrl = "https://api.twitter.com/1.1/account_activity/all/" + 
                this.Environment + "/subscriptions/list.json";
            string resp = await GetApi(oAuthUrl);
            return resp;
        }

        public string GenerateAuthorizationHeaderPlus(string status, string oAuthUrl, string verb)
        {
            string signatureMethod = "HMAC-SHA1";
            string version = "1.0";
            string nonce = GenerateNonce();
            double timestamp = ConvertToUnixTimestamp(DateTime.Now);
            string dst = string.Empty;

            dst = string.Empty;
            dst += "OAuth ";
            dst += string.Format("oauth_consumer_key=\"{0}\", ", Uri.EscapeDataString(OAuthConsumerKey));
            dst += string.Format("oauth_nonce=\"{0}\", ", Uri.EscapeDataString(nonce));
            dst += string.Format("oauth_signature=\"{0}\", ", Uri.EscapeDataString(
                GenerateOauthSignature(status, nonce, timestamp.ToString(), oAuthUrl, verb)));
            dst += string.Format("oauth_signature_method=\"{0}\", ", Uri.EscapeDataString(signatureMethod));
            dst += string.Format("oauth_timestamp=\"{0}\", ", timestamp);
            dst += string.Format("oauth_token=\"{0}\", ", Uri.EscapeDataString(UserAccessToken));
            dst += string.Format("oauth_version=\"{0}\"", Uri.EscapeDataString(version));
            return dst;
        }

        public string GenerateAuthorizationHeaderPlus(string param, string value, string oAuthUrl,
            string verb)
        {
            string signatureMethod = "HMAC-SHA1";
            string version = "1.0";
            string nonce = GenerateNonce();
            double timestamp = ConvertToUnixTimestamp(DateTime.Now);
            string dst = string.Empty;

            dst = string.Empty;
            dst += "OAuth ";
            dst += string.Format("oauth_consumer_key=\"{0}\", ", Uri.EscapeDataString(OAuthConsumerKey));
            dst += string.Format("oauth_nonce=\"{0}\", ", Uri.EscapeDataString(nonce));
            dst += string.Format("oauth_signature=\"{0}\", ", Uri.EscapeDataString(
                GenerateOauthSignature(param, value, nonce, timestamp.ToString(), oAuthUrl, verb)));
            dst += string.Format("oauth_signature_method=\"{0}\", ", Uri.EscapeDataString(signatureMethod));
            dst += string.Format("oauth_timestamp=\"{0}\", ", timestamp);
            dst += string.Format("oauth_token=\"{0}\", ", Uri.EscapeDataString(UserAccessToken));
            dst += string.Format("oauth_version=\"{0}\"", Uri.EscapeDataString(version));
            return dst;
        }


        public string GenerateAuthorizationHeader(string oAuthUrl)
        {
            return GenerateAuthorizationHeader(oAuthUrl, "POST");
        }

        public string GenerateAuthorizationHeader(string oAuthUrl, string verb)
        {
            string signatureMethod = "HMAC-SHA1";
            string version = "1.0";
            string nonce = GenerateNonce();
            double timestamp = ConvertToUnixTimestamp(DateTime.Now);
            string dst = string.Empty;

            dst = string.Empty;
            dst += "OAuth ";
            dst += string.Format("oauth_consumer_key=\"{0}\", ", Uri.EscapeDataString(OAuthConsumerKey));
            dst += string.Format("oauth_nonce=\"{0}\", ", Uri.EscapeDataString(nonce));
            dst += string.Format("oauth_signature=\"{0}\", ", Uri.EscapeDataString(
                GenerateOauthSignature(nonce, timestamp.ToString(), oAuthUrl, verb)));
            dst += string.Format("oauth_signature_method=\"{0}\", ", Uri.EscapeDataString(signatureMethod));
            dst += string.Format("oauth_timestamp=\"{0}\", ", timestamp);
            dst += string.Format("oauth_token=\"{0}\", ", Uri.EscapeDataString(UserAccessToken));
            dst += string.Format("oauth_version=\"{0}\"", Uri.EscapeDataString(version));
            return dst;
        }

        public string GenerateOauthSignature(string status, string nonce, string timestamp,
            string oAuthUrl, string verb)
        {
            string signatureMethod = "HMAC-SHA1";
            string version = "1.0";
            string result = string.Empty;
            string dst = string.Empty;

            dst += string.Format("oauth_consumer_key={0}&", Uri.EscapeDataString(OAuthConsumerKey));
            dst += string.Format("oauth_nonce={0}&", Uri.EscapeDataString(nonce));
            dst += string.Format("oauth_signature_method={0}&", Uri.EscapeDataString(signatureMethod));
            dst += string.Format("oauth_timestamp={0}&", timestamp);
            dst += string.Format("oauth_token={0}&", Uri.EscapeDataString(UserAccessToken));
            dst += string.Format("oauth_version={0}", Uri.EscapeDataString(version));
            if ( string.IsNullOrEmpty(status) == false )
            {
                dst += string.Format("&status={0}", Uri.EscapeDataString(status));
            }

            string signingKey = string.Empty;
            signingKey = string.Format("{0}&{1}", Uri.EscapeDataString(OAuthConsumerSecret), Uri.EscapeDataString(UserAccessTokenSecret));

            //result += "POST&";
            result += verb + "&";
            result += Uri.EscapeDataString(oAuthUrl);
            result += "&";
            result += Uri.EscapeDataString(dst);

            HMACSHA1 hmac = new HMACSHA1();
            hmac.Key = Encoding.UTF8.GetBytes(signingKey);

            byte[] databuff = System.Text.Encoding.UTF8.GetBytes(result);
            byte[] hashbytes = hmac.ComputeHash(databuff);

            return Convert.ToBase64String(hashbytes);
        }


        public string GenerateOauthSignature(string param, 
            string value, string nonce, string timestamp,
            string oAuthUrl, string verb)
        {
            string signatureMethod = "HMAC-SHA1";
            string version = "1.0";
            string result = string.Empty;
            string dst = string.Empty;

            dst += string.Format("oauth_consumer_key={0}&", Uri.EscapeDataString(OAuthConsumerKey));
            dst += string.Format("oauth_nonce={0}&", Uri.EscapeDataString(nonce));
            dst += string.Format("oauth_signature_method={0}&", Uri.EscapeDataString(signatureMethod));
            dst += string.Format("oauth_timestamp={0}&", timestamp);
            dst += string.Format("oauth_token={0}&", Uri.EscapeDataString(UserAccessToken));
            dst += string.Format("oauth_version={0}", Uri.EscapeDataString(version));
            if (string.IsNullOrEmpty(value) == false)
            {
                dst += string.Format("&" + param + "={0}", Uri.EscapeDataString(value));
            }

            string signingKey = string.Empty;
            signingKey = string.Format("{0}&{1}", Uri.EscapeDataString(OAuthConsumerSecret), Uri.EscapeDataString(UserAccessTokenSecret));

            //result += "POST&";
            result += verb + "&";
            result += Uri.EscapeDataString(oAuthUrl);
            result += "&";
            result += Uri.EscapeDataString(dst);

            HMACSHA1 hmac = new HMACSHA1();
            hmac.Key = Encoding.UTF8.GetBytes(signingKey);

            byte[] databuff = System.Text.Encoding.UTF8.GetBytes(result);
            byte[] hashbytes = hmac.ComputeHash(databuff);

            return Convert.ToBase64String(hashbytes);
        }

        public string GenerateOauthSignature(string nonce, string timestamp,
            string oAuthUrl, string verb)
        {
            string signatureMethod = "HMAC-SHA1";
            string version = "1.0";
            string result = string.Empty;
            string dst = string.Empty;

            dst += string.Format("oauth_consumer_key={0}&", Uri.EscapeDataString(OAuthConsumerKey));
            dst += string.Format("oauth_nonce={0}&", Uri.EscapeDataString(nonce));
            dst += string.Format("oauth_signature_method={0}&", Uri.EscapeDataString(signatureMethod));
            dst += string.Format("oauth_timestamp={0}&", timestamp);
            dst += string.Format("oauth_token={0}&", Uri.EscapeDataString(UserAccessToken));
            dst += string.Format("oauth_version={0}", Uri.EscapeDataString(version));

            string signingKey = string.Empty;
            signingKey = string.Format("{0}&{1}", Uri.EscapeDataString(OAuthConsumerSecret), Uri.EscapeDataString(UserAccessTokenSecret));

            result += verb + "&";
            //result += "POST&";
            result += Uri.EscapeDataString(oAuthUrl);
            result += "&";
            result += Uri.EscapeDataString(dst);

            HMACSHA1 hmac = new HMACSHA1();
            hmac.Key = Encoding.UTF8.GetBytes(signingKey);

            byte[] databuff = System.Text.Encoding.UTF8.GetBytes(result);
            byte[] hashbytes = hmac.ComputeHash(databuff);

            return Convert.ToBase64String(hashbytes);
        }

        public static string GenerateNonce()
        {
            string nonce = string.Empty;
            var rand = new Random();
            int next = 0;
            for (var i = 0; i < 32; i++)
            {
                next = rand.Next(65, 90);
                char c = Convert.ToChar(next);
                nonce += c;
            }

            return nonce;
        }

        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }
        public string AcceptChallenge(string crcToken)
        {

            byte[] hashKeyArray = Encoding.UTF8.GetBytes(OAuthConsumerSecret);
            byte[] crcTokenArray = Encoding.UTF8.GetBytes(crcToken);

            HMACSHA256 hmacSHA256Alog = new HMACSHA256(hashKeyArray);

            byte[] computedHash = hmacSHA256Alog.ComputeHash(crcTokenArray);

            string challengeToken = $"sha256={Convert.ToBase64String(computedHash)}";

            CRCResponseToken responseToken = new CRCResponseToken()
            {
                Token = challengeToken
            };

            string jsonResponse = JsonConvert.SerializeObject(responseToken);

            return jsonResponse;
        }

        public string Build(HttpMethod method, string requestUrl)
        {
            if (!Uri.TryCreate(requestUrl, UriKind.RelativeOrAbsolute, out Uri resourceUri))
            {
                throw new Exception("Invalid Resource Url format.");
            }

            string oauthVersion = "1.0";
            string oauthSignatureMethod = "HMAC-SHA1";

            // It could be any random string..
            string oauthNonce = DateTime.Now.Ticks.ToString();

            double epochTimeStamp = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

            string oauthTimestamp = Convert.ToInt64(epochTimeStamp).ToString();

            Dictionary<string, string> signatureParams = new Dictionary<string, string>();
            signatureParams.Add("oauth_consumer_key", OAuthConsumerKey);
            signatureParams.Add("oauth_nonce", oauthNonce);
            signatureParams.Add("oauth_signature_method", oauthSignatureMethod);
            signatureParams.Add("oauth_timestamp", oauthTimestamp);
            signatureParams.Add("oauth_token", UserAccessToken);
            signatureParams.Add("oauth_version", oauthVersion);

            Dictionary<string, string> qParams = resourceUri.GetParams();
            foreach (KeyValuePair<string, string> qp in qParams)
            {
                signatureParams.Add(qp.Key, qp.Value);
            }

            string baseString = string.Join("&", signatureParams.OrderBy(kpv => kpv.Key).Select(kpv => $"{kpv.Key}={kpv.Value}"));

            string resourceUrl = requestUrl.Contains("?") ? requestUrl.Substring(0, requestUrl.IndexOf("?")) : requestUrl;
            baseString = string.Concat(method.Method.ToUpper(), "&", Uri.EscapeDataString(resourceUrl), "&", Uri.EscapeDataString(baseString));

            string oauthSignatureKey = string.Concat(Uri.EscapeDataString(OAuthConsumerSecret), "&", Uri.EscapeDataString(UserAccessTokenSecret));

            string oauthSignature;
            using (HMACSHA1 hasher = new HMACSHA1(Encoding.ASCII.GetBytes(oauthSignatureKey)))
            {
                oauthSignature = Convert.ToBase64String(hasher.ComputeHash(Encoding.ASCII.GetBytes(baseString)));
            }

            string headerFormat = "OAuth oauth_nonce=\"{0}\", oauth_signature_method=\"{1}\", " +
                               "oauth_timestamp=\"{2}\", oauth_consumer_key=\"{3}\", " +
                               "oauth_token=\"{4}\", oauth_signature=\"{5}\", " +
                               "oauth_version=\"{6}\"";

            string authHeader = string.Format(headerFormat,
                                    Uri.EscapeDataString(oauthNonce),
                                    Uri.EscapeDataString(oauthSignatureMethod),
                                    Uri.EscapeDataString(oauthTimestamp),
                                    Uri.EscapeDataString(OAuthConsumerKey),
                                    Uri.EscapeDataString(AdminUserAccessToken),
                                    Uri.EscapeDataString(oauthSignature),
                                    Uri.EscapeDataString(oauthVersion));

            return authHeader;
        }

        internal class CRCResponseToken
        {
            [JsonProperty("response_token")]
            public string Token { get; set; }
        }

    }


}
