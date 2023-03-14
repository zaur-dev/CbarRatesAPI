using FxCore.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(FxFunctions.Startup))]

namespace FxFunctions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<DailyFxRepository>();
            builder.Services.AddSingleton<CBARService>();
            builder.Services.AddSingleton<CurrencyConverter>();
            builder.Services.AddSingleton<RatesUpdater>();
        }
    }
}
