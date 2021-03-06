using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Test.Automation.Api
{
    /// <summary>
    /// Represents an abstract base class for calling JSON REST APIs.
    /// </summary>
    public abstract class ApiClientBase
    {
        private HttpClient _client;

        /// <summary>
        /// Creates an instance of ApiClientBase class using the URI and the credentials of the logged on user.
        /// Supports NTLM, Kerberos, and Negotiate authentication.
        /// </summary>
        /// <param name="uri">The base URI of the API.</param>
        public ApiClientBase(string uri)
        {
            if (!ApiHelper.TryGetUri(uri, out var baseUri))
            {
                throw new ArgumentException($"Invalid base URI string: {uri}");
            }

            _client = ApiHelper.CreateHttpClient(baseUri, ApiHelper.CreateHttpClientHandler());
        }

        /// <summary>
        /// Creates an instance of the ApiClientBase class using the URI and impersonated Network Credential passed in.
        /// Supports NTLM, Kerberos, and Negotiate authentication.
        /// </summary>
        /// <param name="uri">The base URI of the API.</param>
        /// <param name="credential">NetworkCredential used for impersonation.</param>
        public ApiClientBase(string uri, NetworkCredential credential)
        {
            if (!ApiHelper.TryGetUri(uri, out var baseUri))
            {
                throw new ArgumentException($"Invalid base URI string: {uri}");
            }

            _client = ApiHelper.CreateHttpClient(baseUri, ApiHelper.CreateHttpClientHandler(credential));
        }

        /// <summary>
        /// Creates an instance of the ApiClientBase class using the URI and the Credential Cache passed in.
        /// Supports Basic, Digest, NTLM, Kerberos, and Negotiate authentication.
        /// </summary>
        /// <param name="uri">The base URI of the API.</param>
        /// <param name="credentialCache">CredentialCache used for authentication.</param>
        public ApiClientBase(string uri, CredentialCache credentialCache)
        {
            if (!ApiHelper.TryGetUri(uri, out var baseUri))
            {
                throw new ArgumentException($"Invalid base URI string: {uri}");
            }

            _client = ApiHelper.CreateHttpClient(baseUri, ApiHelper.CreateHttpClientHandler(credentialCache));
        }

        /// <summary>
        /// Sends an HTTP request as an asynchronous operation.
        /// Deserializes the JSON response content to the generic parameter type.
        /// </summary>
        /// <typeparam name="T">The generic parameter type used to deserialize the JSON.</typeparam>
        /// <param name="request">The HttpRequestMessage to the API method.</param>
        /// <param name="converters">An (optional) custom JSON converter (if required to de-serialize your POCO.)</param>
        /// <returns>JSON deserialized as the generic type.</returns>
        public async Task<T> ExecuteAsync<T>(HttpRequestMessage request, params JsonConverter[] converters) where T : new()
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions
            try
            {
                // Send an HTTP request.
                using (var responseMessage = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    // Serialize the HTTP content.
                    var rawJson = await responseMessage.Content.ReadAsStringAsync();

                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        // Warn if not success status code and output the error message.
                        WarnIfNotSuccess(request, responseMessage.ReasonPhrase, rawJson);
                    }
                    else if (Debugger.IsAttached)
                    {
                        var byteArr = await responseMessage.Content.ReadAsByteArrayAsync();

                        // Attach the response content as HTML file to the test output when debugging.
                        AttachContentToTestOutput(request, responseMessage.StatusCode.ToString(), byteArr);
                    }

                    // Ensure the response Status Code is Success (2xx).
                    responseMessage.EnsureSuccessStatusCode();

                    // Convert the raw JSON to the generic type.
                    return JsonConvert.DeserializeObject<T>(rawJson, converters);
                }
            }
            catch (HttpRequestException reqEx)
            {
                Console.WriteLine($"HTTP EXCEPTION: {reqEx.Message}");
                if (reqEx.InnerException != null)
                {
                    Console.WriteLine($"INNER EXCEPTION: {reqEx.InnerException.Message}");

                    if (reqEx.InnerException.InnerException != null)
                    {
                        Console.WriteLine($"INNER INNER EX: {reqEx.InnerException.InnerException.Message}");
                    }
                }
                Console.WriteLine($"STACKTRACE: {reqEx.StackTrace}");

                return default(T);
            }
            finally
            {
                request.Dispose();
            }
        }

        /// <summary>
        /// Sends an HTTP request as an asynchronous operation.
        /// Returns the HttpResponseMessage for APIs that only return the HttpStatusCode.
        /// Callers must dispose of the HttpResponeMessage returned.
        /// </summary>
        /// <param name="request">The HttpRequestMessage to the API method.</param>
        /// <returns>The HttpResponseMessage from the API method.</returns>
        public async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage request)
        {
            try
            {
                using (var responseMessage = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        // Serialize the HTTP content.
                        var rawJson = await responseMessage.Content.ReadAsStringAsync();

                        // Warn if not success status code and output the error message.
                        WarnIfNotSuccess(request, responseMessage.ReasonPhrase, rawJson);
                    }
                    else if (Debugger.IsAttached)
                    {
                        var byteArr = await responseMessage.Content.ReadAsByteArrayAsync();

                        // Attach the response content as HTML file to the test output when debugging.
                        AttachContentToTestOutput(request, responseMessage.StatusCode.ToString(), byteArr);
                    }

                    return responseMessage;
                }
            }
            finally
            {
                request.Dispose();
            }
        }

        #region PRIVATE METHODS

        private static void WarnIfNotSuccess(HttpRequestMessage request, string reasonPhrase, string rawJaon)
        {
            Console.WriteLine($"API: ({request.Method.Method}) {request.RequestUri.OriginalString}");
            Console.WriteLine($"RESPONSE STATUS: {reasonPhrase} : {rawJaon}");
        }

        private static void AttachContentToTestOutput(HttpRequestMessage request, string statusCode, byte[]  byteArr)
        {
            // Save content to a file instead of test output window for debugging.
            if (byteArr.Length > 0)
            {
                // file name: bin\Debug\api_foo_bar_OK.html
                var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "DEBUG", $"{request.Method.Method}_{RemoveInvalidFileNameChars(request.RequestUri.AbsolutePath)}_{statusCode}.html");
                File.WriteAllBytes(path, byteArr);
                TestContext.AddTestAttachment(path);
            }
        }

        private static string RemoveInvalidFileNameChars(string apiPath)
        {
            // Remove initial slash char from the API path so the filename starts with a letter.
            // Replace all invalid filename chars (i.e. '/') with an underscore.
            // Ex: /api/foo/bar ==> api_foo_bar
            return string.Join("_", apiPath.Remove(0, 1).Split(Path.GetInvalidFileNameChars()));
        }
        
        #endregion
    }
}

