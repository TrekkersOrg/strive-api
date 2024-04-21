using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using strive_api.Models;
using Microsoft.AspNetCore.Cors;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace strive_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class EmailController : ControllerBase
    {

        private readonly ILogger<EmailController> _logger;
        private IConfiguration _configuration;
        private readonly string _sendGridAPIKey;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public EmailController(ILogger<EmailController> logger, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _configuration = configuration;
            _sendGridAPIKey = configuration["SendGrid:Key"];
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("sendEmail")]
        [EnableCors("AllowAll")]
        public async Task<ActionResult> SendEmail(Email_SendEmail_Request requestBody)
        {
            APIWrapper responseModel = new();
            Email_SendEmail_Response sendEmailResponseModel = new();
            try
            {
                var client = new SendGridClient(_sendGridAPIKey);
                var emailMessage = new SendGridMessage
                {
                    From = new EmailAddress(requestBody.FromEmail, requestBody.FromName),
                    Subject = requestBody.Subject,
                    PlainTextContent = requestBody.Body,
                    HtmlContent = requestBody.Body
                };
                emailMessage.AddTo(new EmailAddress(requestBody.ToEmail, requestBody.ToName));
                var response = await client.SendEmailAsync(emailMessage);
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    sendEmailResponseModel.FromEmail = requestBody.FromEmail;
                    sendEmailResponseModel.FromName = requestBody.FromName;
                    sendEmailResponseModel.Subject = requestBody.Subject;
                    sendEmailResponseModel.Body = requestBody.Body;
                    sendEmailResponseModel.ToEmail = requestBody.ToEmail;
                    sendEmailResponseModel.ToName = requestBody.ToName;
                    responseModel = createResponseModel(200, "Success", "Email sent successfully.", DateTime.Now, sendEmailResponseModel);
                    return Ok(responseModel);
                }
                else
                {
                    responseModel = createResponseModel(200, "Success", "Email failed to send", DateTime.Now, response.ToString());
                    return Ok(responseModel);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while sending the email: {ex.Message}");
            }
        }


        private static APIWrapper createResponseModel(int statusCode, string statusMessage, string statusMessageText, DateTime timestamp, object? data = null)
        {
            APIWrapper responseModel = new();
            responseModel.StatusCode = statusCode;
            responseModel.StatusMessage = statusMessage;
            responseModel.StatusMessageText = statusMessageText;
            responseModel.Timestamp = timestamp;
            Type[] validResponseTypes =
            {
                typeof(Email_SendEmail_Response)
            };
            if (Array.Exists(validResponseTypes, t => t.IsInstanceOfType(data)))
            {
                responseModel.Data = data;
            }
            return responseModel;
        }
    }
}