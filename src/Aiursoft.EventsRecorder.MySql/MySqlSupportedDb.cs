using System.Diagnostics.CodeAnalysis;
using Aiursoft.DbTools;
using Aiursoft.DbTools.MySql;
using Aiursoft.EventsRecorder.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.EventsRecorder.MySql;

[ExcludeFromCodeCoverage]
public class MySqlSupportedDb(bool allowCache, bool splitQuery) : SupportedDatabaseType<TemplateDbContext>
{
    public override string DbType => "MySql";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurMySqlWithCache<MySqlContext>(
            connectionString,
            splitQuery: splitQuery,
            allowCache: allowCache);
    }

    public override TemplateDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<MySqlContext>();
    }
}
