using FxCore.Models;
using Microsoft.Extensions.Logging;

namespace FxCore.Services
{
    public class RatesUpdater
    {
        private readonly DailyFxRepository _dailyFxRepo;
        private readonly CBARService _cbarService;
        private readonly ILogger<RatesUpdater> _logger;

        public RatesUpdater(DailyFxRepository dailyFxRepo, CBARService cbarService, ILogger<RatesUpdater> logger)
        {
            _dailyFxRepo = dailyFxRepo;
            _cbarService = cbarService;
            _logger = logger;
        }

        public async Task UpdateRatesAsync()
        {
            var latestDoc = await _dailyFxRepo.GetLatestDocumentAsync();
            if (latestDoc != null)
            {
                while (latestDoc.Date.Date < DateTime.Now.Date)
                {
                    GetRatesAndSaveToDbAsync(latestDoc.Date.AddDays(1));
                    latestDoc = await _dailyFxRepo.GetLatestDocumentAsync();
                }
            }
            else
            {
                throw new Exception("DB is empty");
            }
        }

        public async Task UpdateRatesAsync(DateTime startDate)
        {
            if (startDate.Date > DateTime.Now.Date)
            {
                throw new ArgumentException("\"startDate\" cannot be in future");
            }
            else
            {
                while (startDate.Date < DateTime.Now.Date)
                {
                    GetRatesAndSaveToDbAsync(startDate.Date);
                    startDate = startDate.AddDays(1);
                }
            }
        }

        public async Task UpdateRatesAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                throw new ArgumentException("\"endDate\" cannot be earlier than \"starDate\"");
            }
            else
            {
                while (startDate.Date < endDate.Date)
                {
                    GetRatesAndSaveToDbAsync(startDate.Date);
                    startDate = startDate.AddDays(1);
                }
            }


        }

        private async Task GetRatesAndSaveToDbAsync(DateTime date)
        {
            if (!_dailyFxRepo.AnyAsync(date).GetAwaiter().GetResult())
            {
                var cbarRates = await _cbarService.GetRates(DateOnly.FromDateTime(date));
                await _dailyFxRepo.CreateOrUpdateAsync(cbarRates);
            }
            else
            {
                _logger.LogInformation($"Already have rates for {date.Date}");
            }
        }
    }
}
