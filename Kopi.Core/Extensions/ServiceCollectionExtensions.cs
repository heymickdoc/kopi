using System.Reflection;
using Bogus;
using Kopi.Core.Interfaces;
using Kopi.Core.Models.Common;
using Kopi.Core.Services.Common;
using Kopi.Core.Services.Common.DataGeneration.Generators;
using Kopi.Core.Services.Matching.Matchers;
using Kopi.Core.Services.PostgreSQL.Target;
using Kopi.Core.Services.SQLServer.Target;
using Kopi.Core.Services.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace Kopi.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///  Registers the Kopi Core services, matchers, and generators
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddKopiCore(this IServiceCollection services)
    {
        // // 1. Register Core Engine Services
        // // These are the services shared by BOTH editions
        // services.AddScoped<TargetDbOrchestratorService>();
        // services.AddScoped<DataOrchestratorService>();
        // services.AddScoped<DataGeneratorService>(); // The "Switchboard"
        // services.AddScoped<DataInsertionService>();
        // services.AddSingleton<Faker>(); // Bogus instance
        // services.AddScoped<StandardTableDataStrategy>();
        // services.AddScoped<ITableDataStrategy, StandardTableDataStrategy>();
        //
        // // 2. Register Community Matchers & Generators
        // // Instead of listing them one by one, we can scan the assembly!
        // RegisterCommunityComponents(services);
        //
        // return services;
        
        services.AddScoped<DataOrchestratorService>();
        services.AddScoped<DataGeneratorService>(); 
        services.AddSingleton<Faker>(); 
        
        // Strategies (Shared)
        services.AddScoped<StandardTableDataStrategy>();
        services.AddScoped<ITableDataStrategy, StandardTableDataStrategy>();

        // 2. Register Community Matchers & Generators
        RegisterCommunityComponents(services);

        return services;
    }
    
    /// <summary>
    /// Registers the specific database implementation (SQL Server or Postgres) based on config.
    /// </summary>
    public static IServiceCollection AddKopiDatabaseProvider(this IServiceCollection services, KopiConfig config)
    {
        if (config.DatabaseType == DatabaseType.PostgreSQL)
        {
            // Register Postgres Implementations
            services.AddSingleton<ITargetDbOrchestratorService, PostgresTargetDbOrchestratorService>();
            services.AddSingleton<IDataInsertionService, PostgresDataInsertionService>();
        }
        else
        {
            // Register SQL Server Implementations
            services.AddSingleton<ITargetDbOrchestratorService, SqlServerTargetDbOrchestratorService>();
            services.AddSingleton<IDataInsertionService, SqlServerDataInsertionService>();
        }

        return services;
    }
    
    private static void RegisterCommunityComponents(IServiceCollection services)
    {
        var coreAssembly = Assembly.GetExecutingAssembly();

        // Auto-Register all IDataGenerator implementations in Core
        var generators = coreAssembly.GetTypes()
            .Where(t => typeof(IDataGenerator).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var gen in generators)
        {
            services.AddSingleton(typeof(IDataGenerator), gen);
        }

        // Auto-Register all IColumnMatcher implementations in Core
        var matchers = coreAssembly.GetTypes()
            .Where(t => typeof(IColumnMatcher).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var matcher in matchers)
        {
            services.AddSingleton(typeof(IColumnMatcher), matcher);
        }
    }
}