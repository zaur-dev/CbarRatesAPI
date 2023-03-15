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
        
        //public void UpdateRates()
        //{
        //    var latestDoc = _dailyFxRepo.GetLatestDocumentAsync().GetAwaiter().GetResult();

        //    if (latestDoc.Date.Date < DateTime.Now.Date)
        //    {
        //        GetRatesAndSaveToDb(latestDoc.Date.AddDays(1));
        //        UpdateRates();
        //    }
        //}
        
        //public void UpdateRates(DateTime startDate)
        //{
        //    if (startDate.Date > DateTime.Now.Date)
        //    {
        //        throw new Exception("\"startDate\" cannot be in future");
        //    }
        //    else
        //    {
        //        GetRatesAndSaveToDb(startDate.Date);
        //        if (startDate.Date < DateTime.Now.Date)
        //        {
        //            UpdateRates(startDate.Date.AddDays(1));
        //        }
        //    }
        //}
        
        //public void UpdateRates(DateTime startDate, DateTime endDate)
        //{
        //    if (startDate.Date > endDate.Date)
        //    {
        //        throw new Exception("\"endDate\" cannot be earlier than \"starDate\"");
        //    }
        //    else
        //    {
        //        GetRatesAndSaveToDb(startDate.Date);
        //        if (startDate.Date < endDate.Date)
        //        {
        //            UpdateRates(startDate.Date.AddDays(1), endDate);
        //        }
        //    }
        //}
        
        //private void GetRatesAndSaveToDb(DateTime date)
        //{
        //    if (!_dailyFxRepo.AnyAsync(date).GetAwaiter().GetResult())
        //    {
        //        var cbarRates = _cbarService.GetRates(DateOnly.FromDateTime(date)).GetAwaiter().GetResult();
        //        _dailyFxRepo.CreateOrUpdateAsync(cbarRates).GetAwaiter().GetResult();
        //    }
        //    else
        //    {
        //        _logger.LogInformation($"Already have rates for {date.Date}");
        //    }
        //}
    }
}
