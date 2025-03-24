using AncientMountain.Managed.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace AncientMountain
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Logging.SetMinimumLevel(LogLevel.None);
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddSingleton<SignalRService>();
            builder.Services.AddSingleton<RadarService>();
            builder.Services.AddSingleton<LootFilterService>();
            builder.Services.AddScoped<SettingsService>();

            await builder.Build().RunAsync();
        }
    }
}
