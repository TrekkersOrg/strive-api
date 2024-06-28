using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace strive_api.Models
{
    public class MongoDB_GetUser_Response
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("username")]
        public string? Username { get; set; }

        [BsonElement("password")]
        public string? Password { get; set; }
    }
}
