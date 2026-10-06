using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MerceditasStore.Services;

public static class SessionJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static T? GetJson<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<T>(json, Options);
    }

    public static void SetJson<T>(this ISession session, string key, T value)
    {
        var json = JsonSerializer.Serialize(value, Options);
        session.SetString(key, json);
    }
}

