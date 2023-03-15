using FxCore.Models;

namespace FxCore.Services
{
    public class CurrencyConverter
    {
        private readonly DailyFxRepository _dailyFxRepo;

        public CurrencyConverter(DailyFxRepository dailyFxRepo)
        {
            _dailyFxRepo = dailyFxRepo;
        }

        // TODO nominal
        // TODO add info about query
        public CurrencyConverterResult ConvertAsync(string from, string to, decimal amount, DateTime? date)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                throw new ArgumentException($"Arguments \"{nameof(from)}\" or \"{nameof(to)}\" can't be empty string");
            }

            if (amount <= 0)
            {
                throw new ArgumentException("Should be more than 0", nameof(amount));
            }
            
            DailyFx rates;
            if (date != null)
            {
                rates = _dailyFxRepo.GetByDateAsync((DateTime)date).GetAwaiter().GetResult();
            }
            else
            {
                rates = _dailyFxRepo.GetLatestDocumentAsync().GetAwaiter().GetResult();
            }

            decimal rate = from == rates.Base
                ? rates.Rates.First(x => x.Code == to).Value
                : rates.Rates.First(x => x.Code == from).Value / rates.Rates.First(x => x.Code == to).Value;

            return new CurrencyConverterResult
            {
                Date = rates.Date,
                CbarDate = rates.CbarDate,
                Rate = Math.Round(rate, 4),
                Result = Math.Round(rate * amount, 4)
            };
        }
    }
    public class CurrencyConverterResult
    {
        public DateTime Date { get; set; }
        public DateTime CbarDate { get; set; }
        public decimal Rate { get; set; }
        public decimal Result { get; set; }
    }
}
