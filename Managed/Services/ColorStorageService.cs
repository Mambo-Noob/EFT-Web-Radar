using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using SkiaSharp;

namespace AncientMountain.Managed.Services
{
    public static class ColorStorageService
    {
        private static IJSRuntime _jsRuntime;

        public static void Init(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public static async Task SaveColor(string key, SKColor color)
        {
            await _jsRuntime.InvokeVoidAsync("saveSetting", key, color.ToString());
        }

        public static Dictionary<string, SKColor> LoadColors()
        {
            var colors = new Dictionary<string, SKColor>();

            // List of players that might have custom colors
            var keys = new string[] { "Big Pipe", "Birdeye", "Glukhar", "Kaban", "Killa", "Knight" };

            foreach (var key in keys)
            {
                var colorStr = _jsRuntime.InvokeAsync<string>("loadSetting", key).Result;
                if (!string.IsNullOrEmpty(colorStr) && SKColor.TryParse(colorStr, out var color))
                {
                    colors[key] = color;
                }
            }

            return colors;
        }
    }
}
