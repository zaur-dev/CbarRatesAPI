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

        public bool Any(DateTime date)
        {
            var filter = Builders<DailyFx>.Filter.Eq("Date", date.ToString());

            return _dailyFXCollection.Find(filter).Any();
        }

        public List<DailyFx> GetAll()
        {
            return _dailyFXCollection.Find(new BsonDocument()).ToList();
        }

        public DailyFx GetByDate(DateTime date)
        {
            var filter = Builders<DailyFx>.Filter.Eq("Date", date.ToString());

            return  _dailyFXCollection.Find(filter).First();
        }

        public DailyFx GetLatestDocument()
        {
            return _dailyFXCollection.AsQueryable().OrderByDescending(x => x.Date).First();
        }

        public void CreateOrUpdate(DailyFx document)
        {
            var filter = Builders<DailyFx>.Filter.Eq("Date", document.Date.ToString());
            var currentDoc = _dailyFXCollection.Find(filter);

            if (currentDoc.Any())
            {
                document.Id = currentDoc.First().Id;
            }

            _dailyFXCollection.ReplaceOne(filter, document, new ReplaceOptions() { IsUpsert = true });
        }

        public void DeleteAll()
        {
            _dailyFXCollection.DeleteMany("{}");
        }
    }
}
