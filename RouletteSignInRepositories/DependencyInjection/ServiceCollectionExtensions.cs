using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Tgs.RouletteServices;
using Tgs.RouletteServices.Repositories.Entities;

namespace Tgs.RouletteService.Repositories.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRouletteSignInEventRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> dbConfigure,
		Func<IServiceProvider, IDatabase> resolveRedisDatabse)
		=> services
			.AddDbContext<TgsContext>(dbConfigure)
			.AddTransient<ITgsRepository, TgsRepository>()
			.AddSingleton<IStatisticsRepository>(sp =>
			{
				var redis = resolveRedisDatabse(sp);

				return ActivatorUtilities.CreateInstance<StatisticsRepository>(sp, redis);
			})
			.AddSerialNumberGenerator();
}
