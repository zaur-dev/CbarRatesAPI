using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FxCore.Models;
using FxCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FxFunctions
{
    public class Functions
    {
        private readonly DailyFxRepository _dailyFxRepo;
        private readonly CBARService _cbarService;
        private readonly CurrencyConverter _converter;
        private readonly RatesUpdater _updater;

        public Functions(DailyFxRepository dailyFxRepo, CBARService cbarService, CurrencyConverter converter, RatesUpdater updater)
        {
            _dailyFxRepo = dailyFxRepo;
            _cbarService = cbarService;
            _converter = converter;
            _updater = updater;
        }

        [FunctionName("DailyUpdater")]
        public async Task DailyUpdater([TimerTrigger("%DevCronExpression%")] TimerInfo myTimer, ILogger logger) //prod "0 13,19 * * 1-5"  dev "*/300 * * * * *" 
        {
            var cbarRates = await _cbarService.GetRates(DateOnly.FromDateTime(DateTime.Now));

            if (await _dailyFxRepo.AnyAsync(DateTime.Now.Date))
            {
                var latestDoc = await _dailyFxRepo.GetLatestDocumentAsync();

                if (cbarRates.Date != latestDoc.Date || cbarRates.CbarDate != latestDoc.CbarDate)
                {
                    await _dailyFxRepo.CreateOrUpdateAsync(cbarRates);
                    logger.LogInformation($"Updating rates...");
                }
                else
                {
                    logger.LogInformation($"Rates are up to date");
                }
            }
            else
            {
                logger.LogInformation($"No rates for today");
                await _dailyFxRepo.CreateOrUpdateAsync(cbarRates);
                logger.LogInformation($"Updating rates...");
            }

            logger.LogInformation($"C# Timer trigger function executed at: {DateTimeOffset.Now}");
        }

        [FunctionName("GetLatestRates")]
        public async Task<ActionResult<DailyFx>> GetLatestRates([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "get/latest")] HttpRequest req,
                                                               ILogger logger)
        {
            return await _dailyFxRepo.GetLatestDocumentAsync();
        }

        [FunctionName("GetRatesToDate")]
        public async Task<ActionResult<DailyFx>> GetRatesToDate([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "get/{date}")] HttpRequest req,
                                                              string date,
                                                              ILogger logger)
        {
            if (DateTime.TryParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return await _dailyFxRepo.GetByDateAsync(parsedDate) ?? new ActionResult<DailyFx>(new NotFoundObjectResult($"Rates for {parsedDate.Date} not found"));
            }
            else
            {
                return new BadRequestObjectResult("Enter valid date in \"dd-MM-yyyy\" format");
            }
        }

        [FunctionName("Convert")]
        public async Task<ActionResult<CurrencyConverterResult>> Convert([HttpTrigger(AuthorizationLevel.Function, "get", Route = "convert")] HttpRequest req,
                                                                         ILogger logger)
        {
            string from = req.Query["from"];
            string to = req.Query["to"];
            var amountIsParsedSuccessfully = decimal.TryParse(req.Query["amount"],
                                                              NumberStyles.AllowDecimalPoint,
                                                              CultureInfo.InvariantCulture,
                                                              out decimal parsedAmount);

            var dateIsParsedSuccessfully = DateTime.TryParseExact(req.Query["date"],
                                                                  "dd-MM-yyyy",
                                                                  CultureInfo.InvariantCulture,
                                                                  DateTimeStyles.None,
                                                                  out DateTime parsedDate);

            if (dateIsParsedSuccessfully)
            {
                if (amountIsParsedSuccessfully)
                {
                    if (await _dailyFxRepo.AnyAsync(parsedDate))
                    {
                        return _converter.ConvertAsync(from.ToUpper(), to.ToUpper(), parsedAmount, parsedDate);
                    }
                    else
                    {
                        return new NotFoundObjectResult($"Rates for {parsedDate.Date} not found");
                    }
                }
                else
                {
                    return new BadRequestObjectResult("Amount is not valid");
                }
            }
            else
            {
                return new BadRequestObjectResult("Enter valid date in \"dd-MM-yyyy\" format");
            }
        }

        [FunctionName("UpdateRates")]
        public async Task<IActionResult> UpdateRates([HttpTrigger(AuthorizationLevel.Admin, "get", Route = "update")] HttpRequest req,
                                                                         ILogger logger)
        {
            await _updater.UpdateRatesAsync();

            return new OkObjectResult("Latest rates loaded");
        }

        [FunctionName("UpdateRatesFromDate")]
        public async Task<IActionResult> UpdateRatesFromDate([HttpTrigger(AuthorizationLevel.Admin, "get", Route = "update/{startDate}")] HttpRequest req,
                                                                         string startDate,
                                                                         ILogger logger)
        {
            var startDateParsedSuccessfully = DateTime.TryParseExact(startDate,
                                                                     "dd-MM-yyyy",
                                                                     CultureInfo.InvariantCulture,
                                                                     DateTimeStyles.None,
                                                                     out DateTime parsedStartDate);
            if (startDateParsedSuccessfully)
            {
                await _updater.UpdateRatesAsync(parsedStartDate);
                return new OkObjectResult($"Rates starting from {startDate} loaded");
            }
            else
            {
                return new BadRequestObjectResult("Enter valid start date in \"dd-MM-yyyy\" format");
            }
        }

        [FunctionName("UpdateRatesForPeriod")]
        public async Task<IActionResult> UpdateRatesForPeriod([HttpTrigger(AuthorizationLevel.Admin, "get", Route = "update/{startDate}/{endDate}")] HttpRequest req,
                                                                         string startDate,
                                                                         string endDate,
                                                                         ILogger logger)
        {
            var startDateParsedSuccessfully = DateTime.TryParseExact(startDate,
                                                                     "dd-MM-yyyy",
                                                                     CultureInfo.InvariantCulture,
                                                                     DateTimeStyles.None,
                                                                     out DateTime parsedStartDate);
            var endDateParsedSuccessfully = DateTime.TryParseExact(endDate,
                                                                     "dd-MM-yyyy",
                                                                     CultureInfo.InvariantCulture,
                                                                     DateTimeStyles.None,
                                                                     out DateTime parsedEndDate);
            if (startDateParsedSuccessfully && endDateParsedSuccessfully)
            {
                await _updater.UpdateRatesAsync(parsedStartDate, parsedEndDate);
                return new OkObjectResult($"Rates starting from {startDate} loaded");
            }
            else
            {
                return new BadRequestObjectResult("Enter valid start and end dates in \"dd-MM-yyyy\" format");
            }
        }

        [FunctionName("RemoveAllRates")]
        public async Task<IActionResult> RemoveAllRates([HttpTrigger(AuthorizationLevel.Admin, "get", Route = "remove/all")] HttpRequest req,
                                                                        ILogger logger)
        {
            await _dailyFxRepo.DeleteAllAsync();
            return new OkObjectResult($"All rates removed from DB");
        }
    }
}