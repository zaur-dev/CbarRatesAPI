using FxCore.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FxCore.Services
{
    public class CBARService
    {
        private readonly ILogger<CBARService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public CBARService(IHttpClientFactory httpClientFactory, ILogger<CBARService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<DailyFx> GetRates(DateOnly date)
        {
            string urlDate = $"{date.Day:D2}.{date.Month:D2}.{date.Year}";
            string url = $"https://www.cbar.az/currencies/{urlDate}.xml";
            CbarDailyFx rates;
            HttpResponseMessage result;
            var client = _httpClientFactory.CreateClient();

            try
            {
                result = client.GetAsync(url).GetAwaiter().GetResult(); //Service returns latest rates if no rates for given date
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CBAR rates retrieval failed:{ex.Message}");
                throw;
            }

            using (var stream = result.Content.ReadAsStream())
            {
                try
                {
                    var serializer = new XmlSerializer(typeof(CbarDailyFx));
                    rates = (CbarDailyFx)serializer.Deserialize(stream);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Deserialization failed:{ex.Message}");
                    throw;
                }
            }

            return ConvertToDailyFx(rates, date);
        }

        private DailyFx ConvertToDailyFx(CbarDailyFx cbarDailyFx, DateOnly date)
        {
            var dailyFx = new DailyFx { Rates = new List<Rate>() };
            dailyFx.Base = "AZN";
            dailyFx.Date = date.ToDateTime(TimeOnly.MinValue);
            dailyFx.CbarDate = DateTime.ParseExact(cbarDailyFx.Date.ToString(), "dd.MM.yyyy", CultureInfo.InvariantCulture).Date;

            var list = cbarDailyFx.AssetTypes.First(x => x.Type == "Xarici valyutalar");
            dailyFx.Rates.AddRange(list.Currencies.Select(currency => new Rate
            {
                Code = currency.Code,
                Nominal = int.Parse(currency.Nominal),
                Value = currency.Value
            }));
            return dailyFx;
        }
    }
}
