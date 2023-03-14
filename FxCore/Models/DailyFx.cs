using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace FxCore.Models
{
    public class DailyFx
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string Id { get; set; }
        public string Base { get; set; }

        [BsonDateTimeOptions(DateOnly = true)]
        public DateTime Date { get; set; }

        [BsonDateTimeOptions(DateOnly = true)]
        public DateTime CbarDate { get; set; }
        public List<Rate> Rates { get; set; }
    }

    public class Rate
    {
        public string Code { get; set; }
        public int Nominal { get; set; }
        public decimal Value { get; set; }
    }
}
