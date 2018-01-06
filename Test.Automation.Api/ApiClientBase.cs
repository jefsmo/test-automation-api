using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        /// <param name="request">The request message sent to the API.</param>
        /// <param name="converters">An (optional) custom JSON converter (if required to de-serialize your POCO.)</param>
        /// <returns>JSON deserialized as the generic type.</returns>
        public async Task<T> ExecuteAsync<T>(HttpRequestMessage request, params JsonConverter[] converters) where T : new()
        {
            var responseMessage = default(HttpResponseMessage);
            string response = null;

            // Call asynchronous network methods in a try/catch block to handle exceptions
            try
            {
                // Send an HTTP request.
                responseMessage = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                // Serialize the HTTP content.
                var rawJson = await responseMessage.Content.ReadAsStringAsync();

                // Save the entire response in case there are exceptions.
                response = ApiHelper.SerializeResponseData(responseMessage, rawJson);

                // Ensure the response Status Code is Success (2xx).
                responseMessage.EnsureSuccessStatusCode();

                // Write response to output window in a debug sesson.
                if (Debugger.IsAttached)
                {
                    ApiHelper.PrintResponse(response);
                }

                // Convert the raw JSON to the generic type.
                return JsonConvert.DeserializeObject<T>(rawJson, converters);
            }
            catch (Exception)
            {
                ApiHelper.PrintResponse(response);
                throw;
            }
            finally
            {
                request?.Dispose();
                responseMessage?.Dispose();
            }
        }
    }
}

