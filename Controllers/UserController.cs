using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using strive_api.Models;
using System.Data;

/// <summary>
/// Manages Strive users.
/// </summary>
namespace strive_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly string _connectionString;

        public UserController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Retrieves a user from the database.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        [HttpGet("GetUser")]
        public async Task<IActionResult> GetUser([FromQuery] string username)
        {
            APIWrapper response;
            try
            {
                // Establish database connection and initialize stored procedure command.
                using SqlConnection connection = new(_connectionString);
                using SqlCommand command = new("GetUserByUsername", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserName", username);
                await connection.OpenAsync();

                // Execute command.
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // If user exists, create user instance and return status code 200.
                    User user = new()
                    {
                        Id = reader.GetString("Id"),
                        Username = reader.GetString("UserName"),
                        FirstName = reader.GetString("FirstName"),
                        LastName = reader.GetString("LastName"),
                        Email = reader.GetString("Email"),
                        EmailConfirmed = reader.GetBoolean("EmailConfirmed")
                    };
                    response = CreateResponseModel(200, "Success", "The user was retrieved successfully.", DateTime.Now, user);
                    return Ok(response);
                }

                // If no user exists, return status code 200.
                response = CreateResponseModel(200, "Success", "The user could not be found.", DateTime.Now, null);
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Return status code 500 for any unhandled errors.
                response = CreateResponseModel(500, "Internal Server Error", ex.ToString(), DateTime.Now, null);
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Retrieves all users from the database.
        /// </summary>
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                // Establish database connection and initialize stored procedure command.
                using SqlConnection connection = new(_connectionString);
                using SqlCommand command = new("GetAllUsers", connection);
                command.CommandType = CommandType.StoredProcedure;
                await connection.OpenAsync();
                List<User> users = new();

                // Execute command.
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // If user exists, create user instance and return status code 200.
                    User user = new()
                    {
                        Id = reader.GetString("Id"),
                        Username = reader.GetString("UserName"),
                        Email = reader.GetString("Email"),
                        EmailConfirmed = reader.GetBoolean("EmailConfirmed")
                    };
                    users.Add(user);
                }
                APIWrapper response = CreateResponseModel(200, "Success", "Users retrieved successfully.", DateTime.Now, users);
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Return status code 500 for any unhandled errors.
                APIWrapper response = CreateResponseModel(500, "Internal Server Error", ex.ToString(), DateTime.Now, null);
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Deletes a user from the database.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromQuery] string username)
        {
            try
            {
                // Establish database connection and initialize stored procedure command.
                using SqlConnection connection = new(_connectionString);
                using SqlCommand command = new("DeleteUserByUserName", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserName", username);
                await connection.OpenAsync();

                // Execute command.
                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected < 0)
                {
                    // If user exists, create user instance and return status code 200.
                    APIWrapper response = CreateResponseModel(200, "Success", "The user was deleted successfully.", DateTime.Now, null);
                    return Ok(response);
                }
                else
                {
                    // If no user exists, return status code 200.
                    APIWrapper response = CreateResponseModel(200, "Success", "The user could not be found.", DateTime.Now, null);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                // Return status code 500 for any unhandled errors.
                APIWrapper response = CreateResponseModel(500, "Internal Server Error", ex.ToString(), DateTime.Now, null);
                return StatusCode(500, response);
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
                Timestamp = timestamp,
            };
            Type[] validResponseTypes = {
                typeof(List<User>),
                typeof(User)
            };
            if (Array.Exists(validResponseTypes, t => t.IsInstanceOfType(data)))
            {
                responseModel.Data = data;
            }
            return responseModel;
        }
    }
}