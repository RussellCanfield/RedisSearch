using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using System.Text.Json;
using Redis_Search;
using Redis_Search.Services;
using Redis_Search.Models;
using Redis_Search_Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddStackExchangeRedisExtensions<NewtonsoftSerializer>(builder.Configuration.GetSection("Redis").Get<RedisConfiguration>());

builder.Services.AddSingleton(sp =>
{
    JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
    JsonSerializerOptions.Converters.Add(new StringConverter());

    return JsonSerializerOptions;
});

builder.Services.AddSingleton<Hasher>();

builder.Services.AddSingleton<ISearchService, SearchService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/search", async (
    HttpContext httpContext,
    JsonSerializerOptions jsonSerializerOptions,
    ISearchService searchService) =>
{
    SearchRequest? searchRequest = await JsonSerializer.DeserializeAsync<SearchRequest?>(
        httpContext.Request.Body,
        jsonSerializerOptions);

    return await searchService.Search(searchRequest);
})
.WithName("Search");

app.Run();