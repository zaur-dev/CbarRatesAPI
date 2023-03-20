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
            //var dateFilter = Builders<DailyFx>.Filter.Eq("Date", date.ToString());
            //var rates = await  _dailyFXCollection.Find(dateFilter).FirstAsync();

            //var olderDateFilter = Builders<DailyFx>.Filter.Lte("Date", date.AddDays(-1).ToString());
            //var olderRateslist = await _dailyFXCollection.Find(olderDateFilter).ToListAsync();
            //var prevDayRates = olderRateslist.OrderByDescending(x => x.Date).First(x => x.Date == x.CbarDate && x.CbarDate == rates.CbarDate.AddDays(-1)) ;

            var rates = await Task.Run(() => _dailyFXCollection.AsQueryable().First(x => x.Date == date));

            var prevDayRates = await Task.Run(() => _dailyFXCollection.AsQueryable()
                                                                      .Where(x => x.Date <= date.AddDays(-1))
                                                                      .OrderByDescending(x => x.Date)
                                                                      .First(x => x.Date == x.CbarDate && x.CbarDate <= rates.CbarDate.AddDays(-1)));

            if (rates != null)
            {
                foreach (var item in rates.Rates)
                {
                    var prevVal = prevDayRates.Rates.First(r => r.Code == item.Code).Value;
                    item.Difference = item.Value - prevVal;
                }
                return rates;
            }
            else
            {
                return null;
            }
        }

        public async Task<DailyFx?> GetLatestDocumentAsync()
        {
            var latest = await Task.Run(() => _dailyFXCollection.AsQueryable().OrderByDescending(x => x.Date).First());
            
            var prevDayRates = await Task.Run(() => _dailyFXCollection.AsQueryable()
                                                                      .Where(x => x.Date <= latest.Date.AddDays(-1))
                                                                      .OrderByDescending(x => x.Date)
                                                                      .First(x => x.Date == x.CbarDate && x.CbarDate <= latest.CbarDate.AddDays(-1)));
            if (latest != null)
            {
                foreach (var item in latest.Rates)
                {
                    var prevVal = prevDayRates.Rates.First(r => r.Code == item.Code).Value;
                    item.Difference = item.Value - prevVal;
                }
                return latest;
            }
            else
            {
                return null;
            }        
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
