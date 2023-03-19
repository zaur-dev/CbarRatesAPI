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

            if (latestDoc.Date.Date < DateTime.Now.Date)
            {
                await GetRatesAndSaveToDbAsync(latestDoc.Date.AddDays(1));
                await UpdateRatesAsync();
            }
        }

        public async Task UpdateRatesAsync(DateTime startDate)
        {
            if (startDate.Date > DateTime.Now.Date)
            {
                throw new Exception("\"startDate\" cannot be in future");
            }
            else
            {
                await GetRatesAndSaveToDbAsync(startDate.Date);
                if (startDate.Date < DateTime.Now.Date)
                {
                    await UpdateRatesAsync(startDate.Date.AddDays(1));
                }
            }
        }

        public async Task UpdateRatesAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate.Date > endDate.Date)
            {
                throw new Exception("\"endDate\" cannot be earlier than \"starDate\"");
            }
            else
            {
                await GetRatesAndSaveToDbAsync(startDate.Date);
                if (startDate.Date < endDate.Date)
                {
                    await UpdateRatesAsync(startDate.Date.AddDays(1), endDate);
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
