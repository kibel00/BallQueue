using BallQueue.Services;
using BallQueue.Data;
using BallQueue.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BallQueue;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        SQLitePCL.Batteries_V2.Init();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "basketball.db");
        builder.Services.AddDbContext<BasketballDbContext>(options => 
            options.UseSqlite($"Data Source={databasePath}"));

        builder.Services.AddScoped<BasketballQueueService>();
        builder.Services.AddScoped<BasketballRepository>();
        builder.Services.AddScoped<BasketballViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BasketballDbContext>();
        dbContext.Database.EnsureCreated();
        dbContext.RemoveLegacyCircularTeamForeignKeys();

        return app;
    }
}
