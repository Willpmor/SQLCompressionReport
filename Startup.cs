// Startup.cs
using Microsoft.OpenApi.Models;
using SQLCompressionReport.Interfaces;
using SQLCompressionReport.Services;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();

        // Adicione o Swagger
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "SQLCompressionReport API", Version = "v1" });
        });

        services.AddScoped<ITableCompressionService, TableCompressionService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // Ative o middleware do Swagger
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "SQLCompressionReport API v1");
            c.RoutePrefix = "swagger"; // Define o Swagger UI no endpoint /swagger/index.html
        });

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}