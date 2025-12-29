using OpusToMp3.Api.Models;
using OpusToMp3.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AudioConverterService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok());

app.MapPost("/convert", (ConvertRequest request, AudioConverterService converter) =>
{
    try
    {
        var mp3Base64 = converter.ConvertOpusToMp3(request.OpusBase64);
        return Results.Ok(new ConvertResponse(mp3Base64));
    }
    catch (FormatException)
    {
        return Results.BadRequest("Invalid base64 input");
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

app.Run();

public partial class Program;
