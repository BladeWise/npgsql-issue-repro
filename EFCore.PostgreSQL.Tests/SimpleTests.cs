namespace EFCore.PostgreSQL.Tests;

#region Namespaces
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;
#endregion

[TestClass]
public class SimpleTests
{
    #region Static Fields
    private static PostgreSqlContainer? _postgreSql;

    private static ServiceProvider _serviceProvider = default!;
    #endregion

    #region Static Methods
    [ClassCleanup]
    public static async Task Cleanup()
    {
        var postgreSql = _postgreSql;
        if (postgreSql is not null)
            await postgreSql.DisposeAsync();
    }

    [ClassInitialize]
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Can be used in future tests.")]
    public static async Task Initialize(TestContext context)
    {
        var postgreSqlBuilder = new PostgreSqlBuilder("timescale/timescaledb-ha:pg16").WithWaitStrategy(Wait.ForUnixContainer()
                                                                                                            .UntilInternalTcpPortIsAvailable(5432));
        var postgreSql = postgreSqlBuilder.Build();
        await postgreSql.StartAsync(context.CancellationToken);
        _postgreSql = postgreSql;

        var connectionString = postgreSql.GetConnectionString();
        var dataSource = new NpgsqlDataSourceBuilder(connectionString).EnableDynamicJson()
                                                                      .EnableParameterLogging()
                                                                      .EnableRecordsAsTuples()
                                                                      .EnableUnmappedTypes()
                                                                      .Build();

        var services = new ServiceCollection();

        services.AddLogging(b => b.AddDebug())
                .AddSingleton(ConfigurationAccessor.Configuration)
                .AddDbContext<SampleDbContext>(b =>
                                               {
                                                   b.UseNpgsql(dataSource)
                                                    .EnableDetailedErrors()
                                                    .EnableSensitiveDataLogging();
                                               });

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var dbContext = sp.GetRequiredService<SampleDbContext>();
        await dbContext.Database.EnsureCreatedAsync(context.CancellationToken);
    }
    #endregion

    #region Properties
    public TestContext TestContext { get; set; } = default!;
    #endregion

    [TestMethod]
    public async Task QueryWithMultipleReferencesToNpgsqlParameterWorks()
    {
        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var dbContext = sp.GetRequiredService<SampleDbContext>();

        var v = new NpgsqlParameter<int>("value", 1);
        var values = await dbContext.Database.SqlQuery<int>($"""
                                                             SELECT 1
                                                             WHERE {v} >= 0 OR {v} <= 0
                                                             """)
                                    .ToListAsync();
        Assert.IsNotEmpty(values);
    }

    [TestMethod]
    public async Task QueryWithMultipleReferencesToNpgsqlParameterAsExplicitFormattableStringWorks()
    {
        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var dbContext = sp.GetRequiredService<SampleDbContext>();

        var v = new NpgsqlParameter<int>("value", 1);
        var values = await dbContext.Database.SqlQuery<int>(FormattableStringFactory.Create("""
                                                                                            SELECT 1
                                                                                            WHERE {0} >= 0 OR {0} <= 0
                                                                                            """,
                                                                                            v))
                                    .ToListAsync();
        Assert.IsNotEmpty(values);
    }

    [TestMethod]
    public async Task QueryWithMultipleReferencesToReferenceTypeParameterWorks()
    {
        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var dbContext = sp.GetRequiredService<SampleDbContext>();

        var v = "1";
        var values = await dbContext.Database.SqlQuery<int>($"""
                                                             SELECT 1
                                                             WHERE {v}::int >= 0 OR {v}::int <= 0
                                                             """)
                                    .ToListAsync();
        Assert.IsNotEmpty(values);
    }

    [TestMethod]
    public async Task QueryWithMultipleReferencesToValueTypeParameterWorks()
    {
        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var dbContext = sp.GetRequiredService<SampleDbContext>();

        var v = 1;
        var queryable = dbContext.Database.SqlQuery<int>($"""
                                                          SELECT 1
                                                          WHERE {v} >= 0 OR {v} <= 0
                                                          """);
        var values = await queryable.ToListAsync();
        Assert.IsNotEmpty(values);
    }

    [TestMethod]
    public async Task QueryWithSingleReferenceToNpgsqlParameterWorks()
    {
        using var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var dbContext = sp.GetRequiredService<SampleDbContext>();

        var v = new NpgsqlParameter<int>("value", 1);
        var values = await dbContext.Database.SqlQuery<int>($"""
                                                             SELECT 1
                                                             WHERE {v} >= 0
                                                             """)
                                    .ToListAsync();
        Assert.IsNotEmpty(values);
    }
}