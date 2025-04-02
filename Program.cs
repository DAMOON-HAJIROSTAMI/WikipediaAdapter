// Wikipedia Adapter API for Search Provider
using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
HttpClient http = new HttpClient();

app.MapGet("/suggest", async (HttpContext context) =>
{
    var query = context.Request.Query["qry"].ToString();

    if (string.IsNullOrWhiteSpace(query))
    {
        return Results.Json(
            new { Suggestions = Array.Empty<object>() },
            new JsonSerializerOptions { PropertyNamingPolicy = null } // 🛠 Preserve casing
        );
    }

    var url = $"https://en.wikipedia.org/w/api.php?action=opensearch&format=json&search={Uri.EscapeDataString(query)}";
    var response = await http.GetStringAsync(url);

    using var json = JsonDocument.Parse(response);
    var root = json.RootElement;

    if (root.GetArrayLength() < 4)
    {
        return Results.Json(
            new { Suggestions = Array.Empty<object>() },
            new JsonSerializerOptions { PropertyNamingPolicy = null }
        );
    }

    var titles = root[1];
    var links = root[3];
    var suggestions = new List<object>();

    for (int i = 0; i < titles.GetArrayLength(); i++)
    {
        var title = titles[i].GetString();
        var link = links[i].GetString();

        if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(link))
        {
            suggestions.Add(new
            {
                Attributes = new
                {
                    url = link,
                    query = title,
                    previewPaneUrl = link
                },
                Text = title
            });
        }
    }

    return Results.Json(
        new { Suggestions = suggestions },
        new JsonSerializerOptions { PropertyNamingPolicy = null } // ✅ Critical fix
    );
});

app.Run("http://0.0.0.0:5000");
