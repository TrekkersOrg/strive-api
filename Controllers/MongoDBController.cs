using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using MongoDB.Driver;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;
using MongoDB.Bson;
using strive_api.Models;
using Microsoft.AspNetCore.Cors;

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
                typeof (MongoDB_GetDocument_Response),
            };
            if (Array.Exists(validResponseTypes, t => t.IsInstanceOfType(data)))
            {
                responseModel.Data = data;
            }
            return responseModel;
        }
    }
}