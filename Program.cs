using System.Security.Cryptography;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi.Models;
using TNG.Shared.Lib.Mongo.Master;
using TNG.Shared.Lib;
using TNG.Shared.Lib.Communications.Email;
using TNG.Shared.Lib.Mongo.Base;
using TNG.Shared.Lib.Settings;
using TNG.Shared.Lib.Intefaces;
public class Program
{
    public static void Main(String[] args)
    {

        
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddControllers();
        configureServices(builder);
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        // if (app.Environment.IsDevelopment())
        // {
             app.UseSwagger();
             app.UseSwaggerUI();
        // }

        app.UseHttpsRedirection();
        app.MapControllers();
        app.Run();
    }

    private static void configureServices(WebApplicationBuilder builder)
    {

        builder.Services.AddSingleton<IMongoClient, MongoClient>(
          _ => new MongoClient(builder.Configuration.GetConnectionString("DefaultConnection"))
      );
        builder.Services.AddScoped<IMongoConfigurationService, MongoConfigurationService>(
            _ => new MongoConfigurationService(builder.Configuration.GetConnectionString("Database"), MongoOperationsMode.UNRESTRICTED)
        );
        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.IMongoLayer, MongoLayer>();

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.ITNGUtiltityLib, TNGUtilityLib>();
        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.ILogger, TNG.Shared.Lib.Logger>();

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.IRestLayer, TNG.Shared.Lib.RestLayer>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.IEmailer, Emailer>(
      _ => new TNG.Shared.Lib.Communications.Email.Emailer(builder.Configuration.GetSection("ConnectionEmail").Get<EMailSettings>()));

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.IAuthenticationService, AuthenticationService>(
        _ => new TNG.Shared.Lib.AuthenticationService(builder.Configuration.GetSection("Cryptography").Get<CryptoSettings>()));

        builder.Services.AddScoped<TNG.Shared.Lib.Intefaces.IS3Layer, S3Layer>(

       _ => new TNG.Shared.Lib.S3Layer(builder.Configuration.GetSection("ConnectionsS3").Get<S3LayerSettings>()));
    }
}
