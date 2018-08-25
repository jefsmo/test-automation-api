using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace Test.Automation.Api
{
    /// <summary>
    /// Represents the static ApiClientHelper class which contains static methods used by the ApiClientBase class.
    /// </summary>
    public static class ApiHelper
    {
        /**********************************************************************
         *  The default MaxResponseContentBufferSize value is 2 GB. 
         *  If app intends to download large amounts of data (50 megabytes or more),
         *  the app should steam those downloads and not use the default buffering.
        **********************************************************************/
        const long MAX_BUFFER_SIZE = 50 * 1024 * 1024L; // 50MB.

        /**********************************************************************
         *  The default Timeout value is 100,000 ms (100 seconds).
         *  To set an infinite timeout use Timeout.InfiniteTimeSpan property.
        **********************************************************************/
        const double DEFAULT_TIMEOUT = 100D;    // 100 seconds.

        /// <summary>
        /// Initializes a new instance of the HttpClient class using a specific handler.
        /// </summary>
        /// <param name="baseAddress">the base address of the Internet resource used when sending requests</param>
        /// <param name="handler">the HttpClient authentication handler</param>
        /// <returns>an HttpClient initialized with the base URI and authentication handler</returns>
        public static HttpClient CreateHttpClient(Uri baseAddress, HttpClientHandler handler)
        {
            var client = new HttpClient(handler, true)
            {
                BaseAddress = baseAddress,
                Timeout = TimeSpan.FromSeconds(DEFAULT_TIMEOUT),
                MaxResponseContentBufferSize = MAX_BUFFER_SIZE
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        /**********************************************************************
         *  Default NTLM authentication and Kerberos authentication use the Microsoft Windows NT user credentials 
         *  associated with the calling application to attempt authentication with the server. 
         *  
         *  When using non-default NTLM authentication, the application sets the authentication type to NTLM and 
         *  uses a NetworkCredential object to pass the user name, password, and domain to the host.
         *
         *  NT LAN Manager(NTLM) authentication is a challenge-response scheme that is a securer variation of Digest authentication.
         *  NTLM uses Windows credentials to transform the challenge data instead of the unencoded user name and password. 
         *  NTLM authentication requires multiple exchanges between the client and server. 
         *  The server and any intervening proxies must support persistent connections to successfully complete the authentication.
         *********************************************************************/

        /**********************************************************************
         * Basic Authentication using the Request Authorization Header
         * Example:
         *   string username = "myUserName";
         *   string password = "P4$$w0rd";
         *   var encoding = Encoding.GetEncoding("iso-8859-1").GetBytes(username + ":" + password));
         *   var credential = System.Convert.ToBase64String(encoding);
         *   request.Headers.Add("Authorization", "Basic " + credential);
        **********************************************************************/

        /// <summary>
        /// Creates an instance of a HttpClientHandler class using the credentials contained in the credential cache.
        /// Basic, Digest, NTLM, Kerberos, and Negotiate supported.
        /// </summary>
        /// <code>            
        /// var credentialCache = new CredentialCache
        /// {
        ///     { new Uri("http://myBaseUri"), "Digest", new NetworkCredential(userName, password, domain) },
        ///     { new Uri("http://myBaseUri"), "NTLM", new NetworkCredential(userName, password) },
        ///     { new Uri("http://myBaseUri"), "Kerberos", new NetworkCredential(userName, password) },
        ///     { new Uri("http://myBaseUri"), "Negotiate", new NetworkCredential(userName, password) }
        /// };
        /// </code>
        /// <param name="credentialCache">The cache of credentials HttpClient will use to try to authenticate with.</param>
        /// <returns>an HttpClientHandler with the desired authentication</returns>
        public static HttpClientHandler CreateHttpClientHandler(CredentialCache credentialCache)
        {

            return new HttpClientHandler
            {
                // Gets or sets authentication information used by this handler.
                Credentials = credentialCache,
                // Gets or sets a value that indicates whether the handler sends an Authorization header with the request.
                PreAuthenticate = true
            };
        }

        /// <summary>
        /// Creates an instance of a HttpClientHandler class using an impersonated NetworkCredential.
        /// NTLM, Negotiate, and Kerberos support only.
        /// </summary>
        /// <param name="credential">an optional NetworkCredential when using impersonation authentication</param>
        /// <returns>An HttpClientHandler with impersonated user authentication.</returns>
        public static HttpClientHandler CreateHttpClientHandler(NetworkCredential credential)
        {
            return new HttpClientHandler
            {
                // Gets or sets authentication information used by this handler.
                Credentials = credential,
                // Gets or sets a value that indicates whether the handler sends an Authorization header with the request.
                PreAuthenticate = true
            };
        }

        /// <summary>
        /// Creates an instance of a HttpClientHandler class using the credentials of the logged on user (UseDefaultCredentials = true).
        /// NTLM, Negotiate, and Kerberos support only.
        /// </summary>
        /// <returns>An HttpClientHandler with default user authentication.</returns>
        public static HttpClientHandler CreateHttpClientHandler()
        {
            // The DefaultCredentials property applies only to NTLM, negotiate, and Kerberos-based authentication.
            return new HttpClientHandler
            {
                // Gets or sets a value that controls whether default credentials are sent with requests by the handler.
                // TRUE: The HttpClientHandler object should be authenticated using the credentials of the currently logged on user.
                UseDefaultCredentials = true,
                // Gets or sets a value that indicates whether the handler sends an Authorization header with the request.
                PreAuthenticate = true
            };
        }

        /// <summary>
        /// Replaces the existing query string with a new query string.
        /// </summary>
        /// <param name="resourceString">the original URI resource string</param>
        /// <param name="newQueryString">the replacement query string</param>
        /// <returns>a new URI resource string</returns>
        public static string ReplaceQueryString(string resourceString, string newQueryString)
        {
            var result = resourceString;
            // Remove previous query string. 
            var questionMarkIndex = result.IndexOf("?", StringComparison.Ordinal);

            if (questionMarkIndex != -1)
            {
                result = result.Substring(0, questionMarkIndex);
            }

            return result + newQueryString;
        }

        /// <summary>
        /// Creates a new absolute URI using the specified string instance.
        /// </summary>
        /// <param name="uriString">the URI string instance</param>
        /// <param name="result">the URI created from the string instance</param>
        /// <returns>true if the URI was created successfully</returns>
        public static bool TryGetUri(string uriString, out Uri result)
        {
            if (!Uri.TryCreate(uriString.Trim(), UriKind.Absolute, out result))
            {
                return false;
            }

            if (result.Scheme != "http" && result.Scheme != "https")
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Creates a request Cancellation Token to override the default HttpClient Timeout.
        /// </summary>
        /// <param name="timeoutInSeconds"></param>
        /// <returns>a CancellationToken with the desired cancellation timeout value</returns>
        public static CancellationToken CreateCancellationToken(double timeoutInSeconds)
        {
            // Custom request timeout overrides the HttpClient default timeout.
            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutInSeconds));
            return cts.Token;
        }

        /// <summary>
        /// Serializes the response status code and response headers.
        /// </summary>
        /// <param name="response">the HttpResponseMessage to serialize</param>
        /// <returns>serialized status code and header collection data</returns>
        private static string SerializeResponseHeaders(HttpResponseMessage response)
        {
            var output = new StringBuilder();
            if (response != null)
            {
                // We cast the StatusCode to an int so we display the numeric value (e.g., "200") rather than the 
                // name of the enum (e.g., "OK") which would often be redundant with the ReasonPhrase. 
                output.AppendLine($"RESPONSE STATUS: {response.ReasonPhrase} ({((int)response.StatusCode)})");
                SerializeHeaderCollection(response.Headers, output);
                SerializeHeaderCollection(response.Content.Headers, output);
            }
            return output.ToString();
        }

        /// <summary>
        /// Serializes a header collection.
        /// </summary>
        /// <param name="headers">a header collection</param>
        /// <param name="output">the serialized header collection</param>
        private static void SerializeHeaderCollection(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers, StringBuilder output)
        {
            foreach (var header in headers)
            {
                output.AppendLine($"{header.Key}: {header.Value.Aggregate((x, next) => x + "; " + next)}");
            }
        }
    }
}
