using System;
using System.Threading.Tasks;
using AncientMountain.Managed.Data;
using Microsoft.JSInterop;

namespace AncientMountain.Managed.Services
{
    public class LootFilterService
    {
        private readonly IJSRuntime _js;

        public LootFilterService(IJSRuntime js)
        {
            _js = js;
        }

        public int LootPriceFilter { get; set; } = 50000;
        public int ImportantLootPriceFilter { get; set; } = 200000;
        public bool ShowFood { get; set; } = true;
        public bool ShowMeds { get; set; } = true;
        public bool ShowBackpacks { get; set; } = true;
        public string LootSearchQuery { get; set; } = "";

        public bool MatchesFilter(WebRadarLoot loot)
        {
            // 🚀 Always show Food, Meds, and Backpacks
            if ((ShowFood && loot.IsFood) || (ShowMeds && loot.IsMeds) || (ShowBackpacks && loot.IsBackpack))
                return true;

            // ✅ Apply filtering for other loot
            return loot.Price >= LootPriceFilter &&
                   (string.IsNullOrEmpty(LootSearchQuery) || loot.ShortName.Contains(LootSearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsImportantLoot(WebRadarLoot loot) => loot.Price >= ImportantLootPriceFilter;

        // ✅ Load settings when the service initializes
        public async Task LoadSettingsAsync()
        {
            LootPriceFilter = await LoadSetting("LootPriceFilter", LootPriceFilter);
            ImportantLootPriceFilter = await LoadSetting("ImportantLootPriceFilter", ImportantLootPriceFilter);
            ShowFood = await LoadSetting("ShowFood", ShowFood);
            ShowMeds = await LoadSetting("ShowMeds", ShowMeds);
            ShowBackpacks = await LoadSetting("ShowBackpacks", ShowBackpacks);
            LootSearchQuery = await LoadSetting("LootSearchQuery", LootSearchQuery);
        }

        // ✅ Save settings when values change
        public async Task SaveSettingsAsync()
        {
            await SaveSetting("LootPriceFilter", LootPriceFilter);
            await SaveSetting("ImportantLootPriceFilter", ImportantLootPriceFilter);
            await SaveSetting("ShowFood", ShowFood);
            await SaveSetting("ShowMeds", ShowMeds);
            await SaveSetting("ShowBackpacks", ShowBackpacks);
            await SaveSetting("LootSearchQuery", LootSearchQuery);
        }

        // 🔹 Save a setting in localStorage
        private async Task SaveSetting(string key, object value)
        {
            await _js.InvokeVoidAsync("localStorage.setItem", key, value.ToString());
        }

        // 🔹 Load a setting from localStorage
        private async Task<int> LoadSetting(string key, int defaultValue)
        {
            var result = await _js.InvokeAsync<string>("localStorage.getItem", key);
            return int.TryParse(result, out int parsedValue) ? parsedValue : defaultValue;
        }

        private async Task<bool> LoadSetting(string key, bool defaultValue)
        {
            var result = await _js.InvokeAsync<string>("localStorage.getItem", key);
            return bool.TryParse(result, out bool parsedValue) ? parsedValue : defaultValue;
        }

        private async Task<string> LoadSetting(string key, string defaultValue)
        {
            var result = await _js.InvokeAsync<string>("localStorage.getItem", key);
            return string.IsNullOrEmpty(result) ? defaultValue : result;
        }
    }
}
