using FxCore.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FxCore.Services
{
    public class DailyFxRepository
    {
        private readonly IMongoCollection<DailyFx> _dailyFXCollection;

        public DailyFxRepository()
        {
            MongoClient client = new MongoClient(Environment.GetEnvironmentVariable("MongoDBConnectionString"));
            IMongoDatabase database = client.GetDatabase(Environment.GetEnvironmentVariable("DatabaseName"));
            _dailyFXCollection = database.GetCollection<DailyFx>("DailyFX");
        }

        public async Task<bool> AnyAsync(DateTime date)
        {
            var filter = Builders<DailyFx>.Filter.Eq("Date", date.ToString());

            return await _dailyFXCollection.Find(filter).AnyAsync();
        }

        public async Task<List<DailyFx>> GetAllAsync()
        {
            return await _dailyFXCollection.Find(new BsonDocument()).ToListAsync();
        }

        public async Task<DailyFx?> GetByDateAsync(DateTime date)
        {
            var filter = Builders<DailyFx>.Filter.Eq("Date", date.ToString());

            var rates = _dailyFXCollection.Find(filter);
            return await rates.AnyAsync() ? await rates.FirstAsync() : null;
        }

        public async Task<DailyFx> GetLatestDocumentAsync()
        {
            return await Task.Run(() => _dailyFXCollection.AsQueryable().OrderByDescending(x => x.Date).First());
        }

        public async Task CreateOrUpdateAsync(DailyFx document)
        {
            var filter = Builders<DailyFx>.Filter.Eq("Date", document.Date.ToString());
            var currentDoc = await _dailyFXCollection.Find(filter).ToListAsync();

            if (currentDoc.Any())
            {
                document.Id = currentDoc.First().Id;
            }

            await _dailyFXCollection.ReplaceOneAsync(filter, document, new ReplaceOptions() { IsUpsert = true });
        }

        public async Task DeleteAllAsync()
        {
            await _dailyFXCollection.DeleteManyAsync("{}");
        }
    }
}
