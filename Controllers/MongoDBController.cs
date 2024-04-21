using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using MongoDB.Driver;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text;
using MongoDB.Bson;
using strive_api.Models;

namespace strive_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class MongoDBController : ControllerBase
    {

        private readonly ILogger<MongoDBController> _logger;
        private string _dbConnectionString;
        private IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public MongoDBController(ILogger<MongoDBController> logger, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _configuration = configuration;
            _dbConnectionString = configuration["MongoDB:ConnectionString"];
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("PostCollection")]
        public IActionResult PostCollection([FromQuery] string collectionName)
        {
            MongoClient client = new MongoClient(_dbConnectionString);
            IMongoDatabase database = client.GetDatabase("Test");
            database.CreateCollection(collectionName);
            MongoDB_PostCollection_Response responseData = new MongoDB_PostCollection_Response
            {
                CollectionName = collectionName,
            };
            APIWrapper response = createResponseModel(200, "Success", "Collection created successfully.", DateTime.Now, responseData);
            return Ok(response);
        }

        [HttpPost("DeleteCollection")]
        public IActionResult DeleteCollection([FromQuery] string collectionName)
        {
            MongoClient client = new MongoClient(_dbConnectionString);
            IMongoDatabase database = client.GetDatabase("Test");
            database.DropCollection(collectionName);
            APIWrapper response = createResponseModel(200, "Success", "Collection deleted successfully.", DateTime.Now, null);
            return Ok(response);
        }

        [HttpPost("UploadDocument")]
        public IActionResult UploadDocument(IFormFile targetFile, string collectionName)
        {
            var filePath = Path.Combine(_webHostEnvironment.ContentRootPath, Path.GetRandomFileName());
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                targetFile.CopyTo(stream);
            }
            string extractedText = ExtractTextFromPdf(filePath);
            System.IO.File.Delete(filePath);
            MongoClient client = new MongoClient(_dbConnectionString);
            IMongoDatabase database = client.GetDatabase("Test");
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(collectionName);
            var document = new BsonDocument
            {
                { "file_name", targetFile.FileName },
                { "content", extractedText }
            };
            collection.InsertOne(document);
            MongoDB_UploadDocument_Response responseData = new MongoDB_UploadDocument_Response
            {
                CollectionName = collectionName,
                FileName = targetFile.FileName
            };
            APIWrapper response = createResponseModel(200, "Success", "Document uploaded successfully.", DateTime.Now, responseData);
            return Ok(response);
        }

        private string ExtractTextFromPdf(string filePath)
        {
            StringBuilder text = new StringBuilder();
            using (PdfReader reader = new PdfReader(filePath))
            {
                using (PdfDocument pdfDoc = new PdfDocument(reader))
                {
                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        text.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
                    }
                }
            }
            return text.ToString();
        }

        /// <summary>
        /// Initializes response body with APIResponseBodyWrapperModel
        /// type.
        /// </summary>
        /// <param name="statusCode" type="int"></param>
        /// <param name="statusMessage" type="string"></param>
        /// <param name="statusMessageText" type="string"></param>
        /// <param name="timestamp" type="DateTime"></param>
        /// <param name="data" type="Object"></param>
        /// <returns type="APIResponseBodyWrapperModel"></returns>
        private static APIWrapper createResponseModel(int statusCode, string statusMessage, string statusMessageText, DateTime timestamp, object? data = null)
        {
            APIWrapper responseModel = new();
            responseModel.StatusCode = statusCode;
            responseModel.StatusMessage = statusMessage;
            responseModel.StatusMessageText = statusMessageText;
            responseModel.Timestamp = timestamp;
            Type[] validResponseTypes =
            {
                typeof(MongoDB_PostCollection_Response),
                typeof(MongoDB_UploadDocument_Response)
            };
            if (Array.Exists(validResponseTypes, t => t.IsInstanceOfType(data)))
            {
                responseModel.Data = data;
            }
            return responseModel;
        }
    }
}