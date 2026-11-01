using DataBaseControl;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        app.UseHttpsRedirection();

        if (!File.Exists("TourFirm.db"))
        {
            var db = new DataBase();
            await db.GenerateDataBase();
        }

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseCors("AllowAll");

        app.MapControllers();

        app.Run();
    }
}