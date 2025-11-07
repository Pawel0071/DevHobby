using MongoDB.Driver;
using MongoDB.Bson;
using RPG.Domain.Interfaces;
using RPG.Infrastructure.Interfaces;

namespace RPG.Infrastructure.Repositories.Deprecated;

public class ExperienceProvider : IExperienceProvider
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly ILogger<ExperienceProvider> _logger;
    
    public Dictionary<int, int> ExperienceTable { get; set; }
        

    public ExperienceProvider(ILogger<ExperienceProvider> logger, IMongoDatabase database, Dictionary<int, int> experienceTable, string collectionName = "tables")
    {
        _logger = logger;
        ExperienceTable = experienceTable;
        _collection = database.GetCollection<BsonDocument>(collectionName);
        LoadExperienceTable();
    }

    private void LoadExperienceTable()
    {
        var doc = _collection.Find(Builders<BsonDocument>.Filter.Eq("_id", "experience_levels")).FirstOrDefault();
        if (doc == null || !doc.Contains("levels"))
            throw new InvalidOperationException("Nie znaleziono dokumentu 'experience_levels'.");

        var levelsDoc = doc["levels"].AsBsonDocument;
        ExperienceTable = levelsDoc.ToDictionary(
            kvp => int.Parse(kvp.Name),
            kvp => kvp.Value.AsInt32
        );
    }
    
    public int GetRequiredExperience(int level)
    {
        if (!ExperienceTable.TryGetValue(level, out var xp))
            throw new ArgumentOutOfRangeException(nameof(level), $"Brak danych dla poziomu {level}.");
        return xp;
    }

    public bool IsMaxLevel(int level) => !ExperienceTable.ContainsKey(level + 1);
}