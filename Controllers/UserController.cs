using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using strive_api.Models;
using System.Data;

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

        [HttpGet("GetUser")]
        public async Task<IActionResult> GetUser([FromQuery] string username)
        {
            APIWrapper response;
            try
            {
                using SqlConnection connection = new(_connectionString);
                using SqlCommand command = new("GetUserByUsername", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserName", username);
                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
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
                response = CreateResponseModel(200, "Success", "The user could not be found.", DateTime.Now, null);
                return Ok(response);
            }
            catch (Exception ex)
            {
                response = CreateResponseModel(500, "Internal Server Error", ex.ToString(), DateTime.Now, null);
                return StatusCode(500, response);
            }
        }

        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                using SqlConnection connection = new(_connectionString);
                using SqlCommand command = new("GetAllUsers", connection);
                command.CommandType = CommandType.StoredProcedure;
                await connection.OpenAsync();
                List<User> users = new();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
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
                APIWrapper response = CreateResponseModel(500, "Internal Server Error", ex.ToString(), DateTime.Now, null);
                return StatusCode(500, response);
            }
        }

        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromQuery] string username)
        {
            try
            {
                using SqlConnection connection = new(_connectionString);
                using SqlCommand command = new("DeleteUserByUserName", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@UserName", username);
                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected < 0)
                {
                    APIWrapper response = CreateResponseModel(200, "Success", "The user was deleted successfully.", DateTime.Now, null);
                    return Ok(response);
                }
                else
                {
                    APIWrapper response = CreateResponseModel(200, "Success", "The user could not be found.", DateTime.Now, null);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                APIWrapper response = CreateResponseModel(500, "Internal Server Error", ex.ToString(), DateTime.Now, null);
                return StatusCode(500, response);
            }
        }

        private static APIWrapper CreateResponseModel(int statusCode, string statusMessage, string statusMessageText, DateTime timestamp, object? data = null)
        {
            APIWrapper apiWrapper = new();
            APIWrapper responseModel = apiWrapper;
            responseModel.StatusCode = statusCode;
            responseModel.StatusMessage = statusMessage;
            responseModel.StatusMessageText = statusMessageText;
            responseModel.Timestamp = timestamp;
            if (data is List<User>)
            {
                responseModel.Data = data;
            }
            else if (data is User)
            {
                responseModel.Data = data;
            }
            else
            {
                responseModel.Data = "";
            }
            return responseModel;
        }
    }
}