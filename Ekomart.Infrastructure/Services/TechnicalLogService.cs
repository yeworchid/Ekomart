using Ekomart.Application.Interfaces;
using Ekomart.Infrastructure.Mongo;
using Ekomart.Infrastructure.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Ekomart.Infrastructure.Services;

public class TechnicalLogService : ITechnicalLogService
{
    private const string DefaultDatabaseName = "ekomart_technical";
    private const string CollectionName = "technical_logs";

    private readonly IMongoCollection<TechnicalLogDocument> _logs;

    public TechnicalLogService(
        IMongoClient mongoClient,
        IOptions<MongoOptions> options)
    {
        var databaseName = string.IsNullOrWhiteSpace(options.Value.DatabaseName)
            ? DefaultDatabaseName
            : options.Value.DatabaseName;

        _logs = mongoClient
            .GetDatabase(databaseName)
            .GetCollection<TechnicalLogDocument>(CollectionName);
    }

    public Task LogAsync(
        string level,
        string message,
        string? path = null,
        string? userId = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        var document = new TechnicalLogDocument
        {
            Level = level,
            Message = message,
            Path = path,
            UserId = userId,
            Details = details
        };

        return _logs.InsertOneAsync(document, cancellationToken: cancellationToken);
    }
}
