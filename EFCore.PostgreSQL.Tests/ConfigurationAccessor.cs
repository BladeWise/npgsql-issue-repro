namespace EFCore.PostgreSQL.Tests;

#region Namespaces
using Microsoft.Extensions.Configuration;
#endregion

public static class ConfigurationAccessor
{
    #region Static Properties
    public static IConfiguration Configuration { get; }
    #endregion

    static ConfigurationAccessor()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environmentName is null or "")
            environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        var builder = new ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json");
        if (environmentName is not (null or ""))
            builder.AddJsonFile($"appsettings.{environmentName}.json", true);
        builder.AddEnvironmentVariables()
               .AddUserSecrets(typeof(ConfigurationAccessor).Assembly);

        Configuration = builder.Build();
    }
}