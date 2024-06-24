using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using strive_api.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

/// <summary>
/// Manages Strive email service.
/// </summary>
namespace strive_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class EmailController : ControllerBase
    {
        private readonly string _sendGridAPIKey;

        public EmailController(IConfiguration configuration)
        {
            _sendGridAPIKey = configuration["SendGrid:Key"];
        }

        /// <summary>
        /// Sends an email to a user.
        /// </summary>
        /// <param name="requestBody">The respective request body.</param>
        [HttpPost("sendEmail")]
        public async Task<ActionResult> SendEmail(Email_SendEmail_Request requestBody)
        {
            // Initialize response models.
            APIWrapper responseModel;
            Email_SendEmail_Response sendEmailResponseModel = new();
            try
            {
                // Create SendGrid client and construct email.
                var client = new SendGridClient(_sendGridAPIKey);
                var emailMessage = new SendGridMessage
                {
                    From = new EmailAddress(requestBody.FromEmail, requestBody.FromName),
                    Subject = requestBody.Subject,
                    PlainTextContent = requestBody.Body,
                    HtmlContent = requestBody.Body
                };
                emailMessage.AddTo(new EmailAddress(requestBody.ToEmail, requestBody.ToName));

                // Send email.
                var response = await client.SendEmailAsync(emailMessage);

                // Return response depending on success of the API call.
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    sendEmailResponseModel.FromEmail = requestBody.FromEmail;
                    sendEmailResponseModel.FromName = requestBody.FromName;
                    sendEmailResponseModel.Subject = requestBody.Subject;
                    sendEmailResponseModel.Body = requestBody.Body;
                    sendEmailResponseModel.ToEmail = requestBody.ToEmail;
                    sendEmailResponseModel.ToName = requestBody.ToName;
                    responseModel = CreateResponseModel(200, "Success", "Email sent successfully.", DateTime.Now, sendEmailResponseModel);
                    return Ok(responseModel);
                }
                else
                {
                    responseModel = CreateResponseModel(200, "Success", "Email failed to send", DateTime.Now, response.ToString());
                    return Ok(responseModel);
                }
            }
            catch (Exception ex)
            {
                // Return status code 500 for any unhandled errors.
                return StatusCode(500, $"An error occurred while sending the email: {ex.Message}");
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