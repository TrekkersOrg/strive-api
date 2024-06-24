using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using MongoDB.Driver;
using System.Text;
using strive_api.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

/// <summary>
/// Manages Strive Pinecone service.
/// </summary>
namespace strive_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class PineconeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _pineconeAPIKey;
        private readonly string _pineconeHost;

        public PineconeController(IConfiguration configuration)
        {
            _configuration = configuration;
            _pineconeAPIKey = _configuration["Pinecone:APIKey"];
            _pineconeHost = _configuration["Pinecone:Host"];
        }

        /// <summary>
        /// Retrieves all namespace details from the index.
        /// </summary>
        [HttpPost("indexDetails")]
        public async Task<ActionResult> IndexDetails()
        {
            // Call Pinecone API.
            using var httpClient = new HttpClient();
            var requestUri = _pineconeHost + "/describe_index_stats";
            httpClient.DefaultRequestHeaders.Add("Api-Key", _pineconeAPIKey);
            APIWrapper responseModel = new();
            var response = await httpClient.PostAsync(requestUri, null);
            if (response.IsSuccessStatusCode)
            {
                // Enumerate through Pinecone response and extract relevant details.
                var responseBodyString = await response.Content.ReadAsStringAsync();
                JsonDocument responseBodyJson = JsonDocument.Parse(responseBodyString);
                JsonElement responseBodyElement = responseBodyJson.RootElement;
                var namespacesDictionary = new Dictionary<string, Pinecone_IndexDetails_Response.NamespaceModel>();
                foreach (var namespaceProperty in responseBodyElement.GetProperty("namespaces").EnumerateObject())
                {
                    var namespaceModel = new Pinecone_IndexDetails_Response.NamespaceModel
                    {
                        VectorCount = namespaceProperty.Value.GetProperty("vectorCount").GetInt32()
                    };
                    namespacesDictionary.Add(namespaceProperty.Name, namespaceModel);
                }

                // Create and return response.
                Pinecone_IndexDetails_Response indexDetailsResponseModel = new()
                {
                    Namespaces = namespacesDictionary,
                    Dimension = responseBodyElement.GetProperty("dimension").GetInt32(),
                    IndexFullness = responseBodyElement.GetProperty("indexFullness").GetDouble(),
                    TotalVectorCount = responseBodyElement.GetProperty("totalVectorCount").GetInt32()
                };
                responseModel = CreateResponseModel(200, "Success", "Pinecone details retrieved successfully.", DateTime.Now, indexDetailsResponseModel);
                return Ok(responseModel);
            }
            else
            {
                // Return status code 500 for any unhandled errors.
                responseModel = CreateResponseModel((int)response.StatusCode, "Unexpected Error", "An unexpected error occurred, please refer to status code.", DateTime.Now);
                return StatusCode((int)response.StatusCode, responseModel);
            }
        }

        /// <summary>
        /// Deletes a Pinecone namespace.
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        [HttpPost("purgePinecone")]
        public async Task<ActionResult> PurgeNamespace([FromBody] Pinecone_PurgePinecone_Request requestBody)
        {
            // Initialize response models.
            APIWrapper responseModel;
            Pinecone_PurgePinecone_Response purgePineconeResponseModel = new();

            // Check if namespace field is empty.
            if (string.IsNullOrEmpty(requestBody.Namespace))
            {
                responseModel = CreateResponseModel(200, "Success", "The 'namespace' field is empty in the request body.", DateTime.Now);
                return Ok(responseModel);
            }

            // Call /indexDetails API to check namespace existence.
            ActionResult? pineconeDetailsResult = await IndexDetails();
            List<string> existingNamespaces = new();
            int namespaceVectorCount = 0;
            if ((pineconeDetailsResult is OkObjectResult okResult) && (okResult.Value != null) && (okResult.Value is APIWrapper apiDetails) && (apiDetails != null) && (apiDetails.Data is Pinecone_IndexDetails_Response pineconeDetails))
            {
                // Check if namespace exists.
                Dictionary<string, Pinecone_IndexDetails_Response.NamespaceModel>? namespaces = pineconeDetails.Namespaces;
                if (namespaces != null)
                {
                    List<string> namespaceKeys = new(namespaces.Keys);
                    foreach (var key in namespaceKeys)
                    {
                        existingNamespaces.Add(key);
                        if (key == requestBody.Namespace)
                        {
                            Pinecone_IndexDetails_Response.NamespaceModel? vectorCountModel = pineconeDetails?.Namespaces?.GetValueOrDefault(key);
                            if (vectorCountModel != null)
                            {
                                // Get the number of vectors in the namespace.
                                namespaceVectorCount = vectorCountModel.VectorCount;
                            }
                        }
                    }
                }
            }

            // Initialize Pinecone API.
            using var httpClient = new HttpClient();
            var requestUri = _pineconeHost + "/vectors/delete";
            var content = new StringContent(
                requestBody.ToJson(),
                Encoding.UTF8,
                "application/json"
            );
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Api-Key", _pineconeAPIKey);

            // Validate namespace existence.
            if (requestBody.Namespace != null && !existingNamespaces.Contains(requestBody.Namespace))
            {
                responseModel = CreateResponseModel(200, "Success", "Unable to find index: " + requestBody.Namespace, DateTime.Now);
                return Ok(responseModel);
            }

            // Call Pinecone API and return response.
            var response = await httpClient.PostAsync(requestUri, content);
            if (response.IsSuccessStatusCode)
            {
                purgePineconeResponseModel.Namespace = requestBody.Namespace;
                purgePineconeResponseModel.NumberOfVectorsDeleted = namespaceVectorCount;
                responseModel = CreateResponseModel(200, "OK", "Pinecone records deleted successfully.", DateTime.Now, purgePineconeResponseModel);
                return Ok(responseModel);
            }
            else
            {
                // Return status code 500 for any unhandled errors.
                responseModel = CreateResponseModel((int)response.StatusCode, "Unexpected Error", "An unexpected error occurred, please refer to status code.", DateTime.Now);
                return StatusCode((int)response.StatusCode, responseModel);
            }
        }

        /// <summary>
        /// Retrieves a record from a Pinecone namespace.
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        [HttpPost("getRecord")]
        public async Task<ActionResult> GetRecord([FromBody] Pinecone_GetRecord_Request requestBody)
        {
            // Initialize response models.
            APIWrapper responseModel = new();
            Pinecone_GetRecord_Response? getRecordResponseModel = new();

            // Set maximum ids to 100.
            const int maximumRequestLimit = 100;

            // Validate required fields.
            if (requestBody.Namespace == null || requestBody.Ids == null || requestBody.Ids.Any(string.IsNullOrEmpty) == true || requestBody.Ids.Any() == false)
            {
                responseModel = CreateResponseModel(200, "Success", "The 'namespace' and/or 'ids' field is missing or empty in the request body.", DateTime.Now);
                return Ok(responseModel);
            }

            // Validate the number of ids is within the limit.
            if (requestBody.Ids.Count > maximumRequestLimit)
            {
                responseModel = CreateResponseModel(200, "Success", "The number of IDs entered exceeded the limit of 100.", DateTime.Now);
            }

            // Call /indexDetails API and validate namespace existence.
            ActionResult? pineconeDetailsResult = await IndexDetails();
            List<string> existingNamespaces = new();
            if ((pineconeDetailsResult is OkObjectResult okResult) && (okResult.Value != null) && (okResult.Value is APIWrapper apiDetails) && (apiDetails != null) && (apiDetails.Data is Pinecone_IndexDetails_Response pineconeDetails))
            {
                Dictionary<string, Pinecone_IndexDetails_Response.NamespaceModel>? namespaces = pineconeDetails.Namespaces;
                if (namespaces != null)
                {
                    List<string> namespaceKeys = new(namespaces.Keys);
                    foreach (var key in namespaceKeys)
                    {
                        existingNamespaces.Add(key);
                    }
                }
            }

            // Build query string with list of ids and call Pinecone API.
            List<string> idList = requestBody.Ids;
            string idQueryString = BuildQueryString("ids", idList);
            using HttpClient httpClient = new();
            var requestUri = _pineconeHost + "/vectors/fetch?namespace=" + requestBody.Namespace + '&' + idQueryString;
            var content = new StringContent(
                requestBody.ToJson(),
                Encoding.UTF8,
                "application/json"
            );
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Api-Key", _pineconeAPIKey);
            if (requestBody.Namespace != null && !existingNamespaces.Contains(requestBody.Namespace))
            {
                responseModel = CreateResponseModel(200, "Success", "Unable to find index: " + string.Join(", ", requestBody.Namespace), DateTime.Now);
                return Ok(responseModel);
            }
            var response = await httpClient.GetAsync(requestUri);
            if (response.IsSuccessStatusCode)
            {
                // Iterate through Pinecone response and extract relevant details.
                var responseBodyString = await response.Content.ReadAsStringAsync();
                JsonDocument responseBodyJson = JsonDocument.Parse(responseBodyString);
                JsonElement responseBodyElement = responseBodyJson.RootElement;
                getRecordResponseModel.Namespace = requestBody.Namespace;
                foreach (var property in responseBodyElement.EnumerateObject())
                {
                    if (property.Name == "namespace")
                    {
                        getRecordResponseModel.Namespace = property.Value.GetString();
                    }
                    if (property.Name == "usage")
                    {
                        Pinecone_GetRecord_Response.UsageDetails usage = new();
                        foreach (var usageProperty in property.Value.EnumerateObject())
                        {
                            if (usageProperty.Name == "readUnits")
                            {
                                usage.ReadUnits = usageProperty.Value.GetInt32();
                            }
                        }
                        getRecordResponseModel.Usage = usage;
                    }
                    if (property.Name == "vectors")
                    {
                        Pinecone_GetRecord_Response.VectorsDetails vectorsDetails = new()
                        {
                            Vectors = new Dictionary<string, Pinecone_GetRecord_Response.VectorDetails>()
                        };
                    foreach (var vectorsDetailsProperty in property.Value.EnumerateObject())
                        {
                            Pinecone_GetRecord_Response.VectorDetails vectorDetails = new();
                            foreach (var vectorDetailsProperty in vectorsDetailsProperty.Value.EnumerateObject())
                            {
                                if (vectorDetailsProperty.Name == "id")
                                {
                                    vectorDetails.Id = vectorDetailsProperty.Value.GetString();
                                }
                                if (vectorDetailsProperty.Name == "values")
                                {
                                    vectorDetails.Values = new List<double>();
                                    foreach (var value in vectorDetailsProperty.Value.EnumerateArray())
                                    {
                                        vectorDetails.Values.Add(value.GetDouble());
                                    }
                                }
                            }
                            vectorsDetails.Vectors.Add(vectorsDetailsProperty.Name, vectorDetails);
                        }
                        // Validate the namespace has the desired record.
                        if (vectorsDetails.Vectors.Count == 0)
                        {
                            responseModel = CreateResponseModel(200, "Success", "Unable to find vectors: " + string.Join(", ", requestBody.Ids), DateTime.Now);
                            return Ok(responseModel);
                        }

                        // Check which entered vectors do not exist.
                        List<String> invalidVectors = new();
                        if (vectorsDetails.Vectors.Count > 0 && vectorsDetails.Vectors.Count < idList.Count)
                        {
                            foreach (string vector in idList)
                            {
                                if (vectorsDetails.Vectors.ContainsKey(vector) == false)
                                {
                                    invalidVectors.Add(vector);
                                }
                            }
                            // If there are invalid records, return the respective response.
                            if (invalidVectors.Count > 0)
                            {
                                getRecordResponseModel.Vectors = vectorsDetails;
                                responseModel = CreateResponseModel(200, "OK", "Some Pinecone records fetched successfully. The following records were not found: " + string.Join(", ", invalidVectors), DateTime.Now, getRecordResponseModel);
                                return Ok(responseModel);
                            }
                        }
                        getRecordResponseModel.Vectors = vectorsDetails;
                    }
                }
                responseModel = CreateResponseModel(200, "OK", "Pinecone records fetched successfully.", DateTime.Now, getRecordResponseModel);
                return Ok(responseModel);
            }
            else
            {
                // Return status code 500 for any unhandled errors.
                responseModel = CreateResponseModel((int)response.StatusCode, "Unexpected Error", "An unexpected error occurred, please refer to status code.", DateTime.Now);
                return StatusCode((int)response.StatusCode, responseModel);
            }
        }

        /// <summary>
        /// Creates the API wrapper for the response body.
        /// </summary>
        /// <param name="statusCode">The status code of the API response.</param>
        /// <param name="statusMessage">The status message of the API response.</param>
        /// <param name="statusMessageText">The more descriptive status message of the API response.</param>
        /// <param name="timestamp">The timestamp in which the API response was received.</param>
        /// <param name="data">Any request specific data returned from the API.</param>
        private static APIWrapper CreateResponseModel(int statusCode, string statusMessage, string statusMessageText, DateTime timestamp, object? data = null)
        {
            APIWrapper responseModel = new()
            {
                StatusCode = statusCode,
                StatusMessage = statusMessage,
                StatusMessageText = statusMessageText,
                Timestamp = timestamp
            };
            Type[] validResponseTypes = { 
                typeof(Pinecone_IndexDetails_Response), 
                typeof(Pinecone_GetRecord_Response),
                typeof(Pinecone_PurgePinecone_Response)
            };
            if (Array.Exists(validResponseTypes, t => t.IsInstanceOfType(data)))
            {
                responseModel.Data = data;
            }
            return responseModel;
        }

        private static string BuildQueryString(string key, List<string> values)
        {
            var encodedValues = values.Select(v => HttpUtility.UrlEncode(v));
            return $"{key}={string.Join("&" + key + "=", encodedValues)}";
        }
    }
}