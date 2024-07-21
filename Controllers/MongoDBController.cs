using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using MongoDB.Driver;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;
using MongoDB.Bson;
using strive_api.Models;
using Microsoft.AspNetCore.Cors;
using System.Collections;
using MongoDB.Bson.Serialization;

/// <summary>
/// Manages Strive MongoDB service.
/// </summary>
namespace strive_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class MongoDBController : ControllerBase
    {

        private readonly string _dbConnectionString;
        private readonly string _databaseName;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public MongoDBController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _dbConnectionString = configuration["MongoDB:ConnectionString"];
            _databaseName = configuration["MongoDB:DatabaseName"];
            _webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// Creates a MongoDB collection.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        [HttpPost("PostCollection")]
        public IActionResult PostCollection([FromQuery] string collectionName)
        {
            // Establish connection to MongoDB database and create collection.
            MongoClient client = new(_dbConnectionString);
            IMongoDatabase database = client.GetDatabase(_databaseName);
            database.CreateCollection(collectionName);
            MongoDB_PostCollection_Response responseData = new()
            {
                CollectionName = collectionName,
            };
            APIWrapper response = CreateResponseModel(200, "Success", "Collection created successfully.", DateTime.Now, responseData);
            return Ok(response);
        }

        /// <summary>
        /// Get User from MongoDB
        /// </summary>
        [HttpGet("GetUser")]
        public IActionResult GetUser([FromQuery] string username)
        {
            // Initialize response models.
            APIWrapper response = new();
            MongoDB_GetUser_Response responseData = new();
            try
            {
                // Establish MongoDB connection to collection.
                MongoClient client = new(_dbConnectionString);
                IMongoDatabase database = client.GetDatabase(_databaseName);
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("Users");

                // Search for the document.
                var filter = Builders<BsonDocument>.Filter.Eq("username", username);
                var document = collection.Find(filter).FirstOrDefault();

                // Respective to the file existence, return appropriate response.
                if (document != null)
                {
                    var user = BsonSerializer.Deserialize<MongoDB_GetUser_Response>(document);
                    responseData.Username = user.Username;
                    responseData.Password = user.Password;
                    response = CreateResponseModel(200, "Success", "User found successfully.", DateTime.Now, responseData);
                    return Ok(response);
                }
                else
                {
                    response = CreateResponseModel(200, "Success", "User does not exist.", DateTime.Now);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                // Return status code 500 for any unhandled errors.
                response = CreateResponseModel((int)response.StatusCode, "Unexpected Error", ex.Message, DateTime.Now);
                return StatusCode((int)response.StatusCode, response);
            }
        }

        /// <summary>
        /// Deletes a MongoDB collection.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        [HttpPost("DeleteCollection")]
        public IActionResult DeleteCollection([FromQuery] string collectionName)
        {
            // Establish connection to MongoDB database and delete collection.
            MongoClient client = new(_dbConnectionString);
            IMongoDatabase database = client.GetDatabase(_databaseName);
            database.DropCollection(collectionName);
            APIWrapper response = CreateResponseModel(200, "Success", "Collection deleted successfully.", DateTime.Now, null);
            return Ok(response);
        }

        /// <summary>
        /// Deletes a MongoDB document.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="fileName">The name of the document.</param>
        [HttpPost("DeleteDocument")]
        public async Task<IActionResult> DeleteDocument([FromQuery] string collectionName, string versionName)
        {
            MongoClient client = new(_dbConnectionString);
            IMongoDatabase database = client.GetDatabase(_databaseName);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(collectionName);
            var filter = Builders<BsonDocument>.Filter.Eq("version_name", versionName);
            await collection.DeleteOneAsync(filter);
            APIWrapper response = CreateResponseModel(200, "Success", "Document deleted successfully.", DateTime.Now, null);
            return Ok(response);
        }

        /// <summary>
        /// Uploads a document to a MongoDB collection.
        /// </summary>
        /// <param name="targetFile">The file.</param>
        /// <param name="collectionName">The name of the collection.</param>
        [HttpPost("UploadDocument")]
        [EnableCors("AllowAll")]
        public IActionResult UploadDocument(IFormFile targetFile, string collectionName)
        {
            // Create a file path using web host and copy target file to this path.
            var filePath = Path.Combine(_webHostEnvironment.ContentRootPath, Path.GetRandomFileName());
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                targetFile.CopyTo(stream);
            }

            // Extract the text from the PDF and remove the created file path
            string extractedText = ExtractTextFromPdf(filePath);
            System.IO.File.Delete(filePath);

            // Establish connection to MongoDB collection.
            MongoClient client = new(_dbConnectionString);
            IMongoDatabase database = client.GetDatabase(_databaseName);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(collectionName);

            // Construct entry and insert into collection.
            var document = new BsonDocument
            {
                { "file_name", targetFile.FileName },
                { "content", extractedText }
            };
            collection.InsertOne(document);

            // Return the API response.
            MongoDB_UploadDocument_Response responseData = new()
            {
                CollectionName = collectionName,
                FileName = targetFile.FileName
            };
            APIWrapper response = CreateResponseModel(200, "Success", "Document uploaded successfully.", DateTime.Now, responseData);
            return Ok(response);
        }

        /// <summary>
        /// Add a risk assessment to a document.
        /// </summary>
        /// <param name="request">The request body.</param>
        [HttpPost("AddRiskAssessment")]
        [EnableCors("AllowAll")]
        public IActionResult AddRiskAssessment(MongoDB_AddRiskAssessment_Request request)
        {
            string? requestNamespace = request.Namespace;
            string? fileName = request.File_Name;
            string? requestVersionName = request.VersionName;
            int riskAssessmentScore = request.riskAssessmentScore;
            int financialScore = request.financialScore;
            int financialSystemQueryScore = request.financialSystemQueryScore;
            int financialKeywordsScore = request.financialKeywordsScore;
            int financialXgbScore = request.financialXgbScore;
            int reputationalScore = request.reputationalScore;
            int reputationalSystemQueryScore = request.reputationalSystemQueryScore;
            int reputationalKeywordsScore = request.reputationalKeywordsScore;
            int reputationalXgbScore = request.reputationalXgbScore;
            int regulatoryScore = request.regulatoryScore;
            int regulatorySystemQueryScore = request.regulatorySystemQueryScore;
            int regulatoryKeywordsScore = request.regulatoryKeywordsScore;
            int regulatoryXgbScore = request.regulatoryXgbScore;
            int operationalScore = request.operationalScore;
            int operationalSystemQueryScore = request.operationalSystemQueryScore;
            int operationalKeywordsScore = request.operationalKeywordsScore;
            int operationalXgbScore = request.operationalXgbScore;

            MongoClient client = new(_dbConnectionString);
            IMongoDatabase database = client.GetDatabase(_databaseName);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(requestNamespace);

            BsonDocument riskAssessment = new BsonDocument
            {
                { "score", request.riskAssessmentScore },
                { "financial", new BsonDocument
                    {
                        { "score", request.financialScore },
                        { "system_query", request.financialSystemQueryScore },
                        { "keywords", request.financialKeywordsScore },
                        { "xgb", request.financialXgbScore }
                    }
                },
                { "reputational", new BsonDocument
                    {
                        { "score", request.reputationalScore },
                        { "system_query", request.reputationalSystemQueryScore },
                        { "keywords", request.reputationalKeywordsScore },
                        { "xgb", request.reputationalXgbScore }
                    }
                },
                { "regulatory", new BsonDocument
                    {
                        { "score", request.regulatoryScore },
                        { "system_query", request.regulatorySystemQueryScore },
                        { "keywords", request.regulatoryKeywordsScore },
                        { "xgb", request.regulatoryXgbScore }
                    }
                },
                { "operational", new BsonDocument
                    {
                        { "score", request.operationalScore },
                        { "system_query", request.operationalSystemQueryScore },
                        { "keywords", request.operationalKeywordsScore },
                        { "xgb", request.operationalXgbScore }
                    }
                }
            };

            var filter = Builders<BsonDocument>.Filter.Eq("file_name", fileName);

            var update = Builders<BsonDocument>.Update
                .Set("version_name", request.VersionName)
                .Set("risk_assessment", riskAssessment);

            var result = collection.UpdateOne(filter, update);
            if (result.ModifiedCount > 0)
            {
                return Ok("Risk assessment updated successfully");
            }
            else
            {
                return NotFound("Document not found or no changes made");
            }
        }

        /// <summary>
        /// Gets a document from a MongoDB collection.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="collectionName">The name of the collection.</param>
        [HttpGet("GetDocument")]
        [EnableCors("AllowAll")]
        public IActionResult GetDocument([FromQuery] string fileName, string collectionName)
        {
            // Initialize response models.
            APIWrapper response = new();
            MongoDB_GetDocument_Response responseData = new();
            try
            {
                // Establish MongoDB connection to collection.
                MongoClient client = new(_dbConnectionString);
                IMongoDatabase database = client.GetDatabase(_databaseName);
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(collectionName);

                // Search for the document.
                var filter = Builders<BsonDocument>.Filter.Eq("file_name", fileName);
                var document = collection.Find(filter).FirstOrDefault();

                // Respective to the file existence, return appropriate response.
                if (document != null)
                {
                    responseData.FileExists = true;
                    responseData.FileName = fileName;
                    responseData.CollectionName = collectionName;
                    response = CreateResponseModel(200, "Success", $"{fileName} exists in {collectionName} collection.", DateTime.Now, responseData);
                    return Ok(response);
                }
                else
                {
                    responseData.FileExists = false;
                    responseData.FileName = fileName;
                    responseData.CollectionName = collectionName;
                    response = CreateResponseModel(200, "Success", $"{fileName} exists in {collectionName} collection.", DateTime.Now, responseData);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                // Return status code 500 for any unhandled errors.
                response = CreateResponseModel((int)response.StatusCode, "Unexpected Error", ex.Message, DateTime.Now);
                return StatusCode((int)response.StatusCode, response);
            }
        }

        /// <summary>
        /// Extracts the text from a PDF into a string data type.
        /// </summary>
        /// <param name="filePath">The file path of the PDF.</param>
        private static string ExtractTextFromPdf(string filePath)
        {
            StringBuilder text = new();
            using (PdfReader reader = new(filePath)) 
            using (PdfDocument pdfDoc = new(reader))
            {
                for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    text.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
                }
            }
            return text.ToString();
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
                Timestamp = timestamp,
            };
            Type[] validResponseTypes =
            {
                typeof(MongoDB_PostCollection_Response),
                typeof(MongoDB_UploadDocument_Response),
                typeof(MongoDB_GetDocument_Response),
                typeof(MongoDB_GetUser_Response)
            };
            if (Array.Exists(validResponseTypes, t => t.IsInstanceOfType(data)))
            {
                responseModel.Data = data;
            }
            return responseModel;
        }
    }
}