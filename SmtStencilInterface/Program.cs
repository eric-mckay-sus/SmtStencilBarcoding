// <copyright file="Program.cs" company="Stanley Electric US Co. Inc.">
// Copyright (c) 2026 Stanley Electric US Co. Inc. Licensed under the MIT License.
// </copyright>

namespace SmtStencilInterface;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using InterProcessIO;
using SmtStencilInterface.Components;

/// <summary>
/// Hosts the application startup and configuration.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <param name="args">Command-line arguments supplied by the host.</param>
    public static void Main(string[] args)
    {
        try
        {
            GetConnectionString();
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContextFactory<SmtStencilingDbContext>(options =>
            options.UseSqlServer(GetConnectionString()));

        builder.Services.AddTransient<BlazorInputProvider>();
        builder.Services.AddTransient<IInputProvider>(sp => sp.GetRequiredService<BlazorInputProvider>());
        builder.Services.AddTransient<BlazorReporter>();
        builder.Services.AddTransient<IOutputProvider>(sp => sp.GetRequiredService<BlazorReporter>());

        builder.Services.AddBlazorBootstrap();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }

    /// <summary>
    /// Gets the connection string for the database whose credentials are stored in environment variables.
    /// </summary>
    /// <returns>A SQL Server connection string for access to the database.</returns>
    /// <throws>InvalidOperationException when there are missing environment variable(s).</throws>
    public static string GetConnectionString()
    {
        SqlConnectionStringBuilder builder = new ()
        {
            DataSource = GetRequired("DB_SERVER"),
            UserID = GetRequired("DB_USER"),
            Password = GetRequired("DB_PASS"),
            InitialCatalog = GetRequired("DB_NAME"),
            TrustServerCertificate = true,
        };
        return builder.ConnectionString;
    }

    /// <summary>
    /// Gets a required value from the environment.
    /// </summary>
    /// <param name="key">The key to get the environment variable.</param>
    /// <returns>The value associated with the key, or <see cref="InvalidOperationException"/> if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="key"/> cannot be found in the environment.</exception>
    private static string GetRequired(string key)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required environment variable '{key}' is missing for database connection.");
        }

        return value;
    }
}
