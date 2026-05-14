using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace CBreaseWebApp1.Services
{
    public class DraftStorageService
    {
        private readonly IJSRuntime _js;
        private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        public DraftStorageService(IJSRuntime js)
        {
            _js = js;
        }

        public async Task SaveAsync<T>(string key, T data)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await _js.InvokeVoidAsync("draftStorage.save", key, json);
        }

        public async Task<T?> LoadAsync<T>(string key)
        {
            var json = await _js.InvokeAsync<string?>("draftStorage.load", key);

            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task RemoveAsync(string key)
        {
            await _js.InvokeVoidAsync("draftStorage.remove", key);
        }
    }
}