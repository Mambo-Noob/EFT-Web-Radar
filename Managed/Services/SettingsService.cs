using Microsoft.JSInterop;
using System.Threading.Tasks;

public class SettingsService
{
    private readonly IJSRuntime _js;

    public SettingsService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SaveSettingAsync(string key, object value)
    {
        await _js.InvokeVoidAsync("saveSetting", key, value);
    }

    public async Task<T> LoadSettingAsync<T>(string key)
    {
        return await _js.InvokeAsync<T>("loadSetting", key);
    }
}
