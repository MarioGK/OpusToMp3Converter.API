using OpusToMp3.Api.Models;
using OpusToMp3.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AudioConverterService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());

app.MapPost("/convert", async (ConvertRequest request, AudioConverterService converter, ILogger<Program> logger) =>
{
    logger.LogInformation("Received convert request");
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        logger.LogInformation("Processing conversion...");
        var mp3Base64 = await converter.ConvertOpusToMp3Async(request.OpusBase64);

        stopwatch.Stop();
        logger.LogInformation("Conversion completed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

        return Results.Ok(new ConvertResponse(mp3Base64));
    }
    catch (FormatException)
    {
        stopwatch.Stop();
        logger.LogWarning("Conversion failed: Invalid base64 input. Time elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        return Results.BadRequest("Invalid base64 input");
    }
    catch (ArgumentException ex)
    {
        stopwatch.Stop();
        logger.LogWarning("Conversion failed: {Message}. Time elapsed: {ElapsedMs}ms", ex.Message, stopwatch.ElapsedMilliseconds);
        return Results.BadRequest(ex.Message);
    }
});

app.Run();

public partial class Program;
