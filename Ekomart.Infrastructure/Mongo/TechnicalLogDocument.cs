using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Ekomart.Infrastructure.Mongo;

public class TechnicalLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? Path { get; set; }

    public string? UserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string? Details { get; set; }
}