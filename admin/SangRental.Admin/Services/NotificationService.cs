using Microsoft.JSInterop;

namespace SangRental.Admin.Services;

// Invoke only after interactive rendering, never during prerender.
public sealed class NotificationService(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? module;
    private readonly SemaphoreSlim gate = new(1, 1);
    public string Locale { get; set; } = "vi";
    private async Task<T> Invoke<T>(string method, object payload)
    {
        await gate.WaitAsync();
        try { module ??= await js.InvokeAsync<IJSObjectReference>("import", "./notifications.js"); }
        finally { gate.Release(); }
        return await module.InvokeAsync<T>("invoke", method, payload, Locale);
    }
    public Task Success(string message) => Invoke<object?>("success", new { message });
    public Task Error(string message) => Invoke<object?>("error", new { message });
    public Task Warning(string message) => Invoke<object?>("warning", new { message });
    public Task Info(string message) => Invoke<object?>("info", new { message });
    public Task<bool> Confirm(string message) => Invoke<bool>("confirm", new { message });
    public Task<int?> Loading(string message) => Invoke<int?>("loading", new { message });
    public Task CloseLoading(int token) => Invoke<object?>("closeLoading", new { token });
    public Task Toast(string message, string type = "info") => Invoke<object?>("toast", new { message, type });
    public async ValueTask DisposeAsync()
    {
        if (module is null) return;
        try { await module.DisposeAsync(); }
        catch (JSDisconnectedException) { /* Circuit disconnected; no browser calls remain possible. */ }
    }
}
