﻿/// <summary>
/// Represents an authenticated Strive user.
/// </summary>
namespace strive_api.Models
{
    public class User
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
