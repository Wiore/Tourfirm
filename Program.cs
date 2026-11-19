using DataBaseControl;
using Microsoft.AspNetCore.Identity.Data;

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

        if (!File.Exists("TourFirm.db"))
        {
            var db = new DataBase();
            await db.GenerateDataBase();
            await db.InsToUser(new Users.NewUserData() { Login = "employer", Email="employer@gmail.com", NickName="admin", Password="password", Phone="+79524405011"} ,1);
        }


        app.UseCors("AllowAll");
        app.UseHttpsRedirection();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapControllers();


        app.Run();
    }
}