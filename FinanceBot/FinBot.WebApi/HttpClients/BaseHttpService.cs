using System.Net;
using System.Text.Json;
using FinBot.Domain.Utils;

namespace FinBot.WebApi.HttpClients;

public abstract class BaseHttpService(HttpClient httpClient, string basePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected async Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(basePath + path, ct);
            return await ReadResultAsync<T>(response, ct);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex.Message);
        }
    }

    protected async Task<Result<T>> PostAsync<T, TBody>(string path, TBody body, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(basePath + path, body, JsonOptions, ct);
            return await ReadResultAsync<T>(response, ct);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex.Message);
        }
    }

    protected async Task<Result> PostAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsync(basePath + path, content: null, ct);
            return await ReadResultAsync(response, ct);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    protected async Task<Result> PostAsync<TBody>(string path, TBody body, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(basePath + path, body, JsonOptions, ct);
            return await ReadResultAsync(response, ct);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    protected async Task<Result<T>> PostAsync<T>(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PostAsync(basePath + path, content: null, ct);
            return await ReadResultAsync<T>(response, ct);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex.Message);
        }
    }

    protected async Task<Result<T>> PatchAsync<T>(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PatchAsync(basePath + path, content: null, ct);
            return await ReadResultAsync<T>(response, ct);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex.Message);
        }
    }

    protected async Task<Result<T>> PatchAsync<T, TBody>(string path, TBody body, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.PatchAsJsonAsync(basePath + path, body, JsonOptions, ct);
            return await ReadResultAsync<T>(response, ct);
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex.Message);
        }
    }

    protected async Task<Result> DeleteAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync(basePath + path, ct);
            return await ReadResultAsync(response, ct);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private static async Task<Result<T>> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            return Result<T>.Success(data!);
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        return Result<T>.Failure(error, MapStatusCode(response.StatusCode));
    }

    private static async Task<Result> ReadResultAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return Result.Success();

        var error = await response.Content.ReadAsStringAsync(ct);
        return Result.Failure(error, MapStatusCode(response.StatusCode));
    }

    private static ErrorType MapStatusCode(HttpStatusCode code) => code switch
    {
        HttpStatusCode.NotFound => ErrorType.NotFound,
        HttpStatusCode.Unauthorized => ErrorType.Unauthorized,
        HttpStatusCode.BadRequest => ErrorType.Validation,
        HttpStatusCode.Conflict => ErrorType.Conflict,
        HttpStatusCode.Forbidden => ErrorType.Forbidden,
        _ => ErrorType.Exception
    };
}