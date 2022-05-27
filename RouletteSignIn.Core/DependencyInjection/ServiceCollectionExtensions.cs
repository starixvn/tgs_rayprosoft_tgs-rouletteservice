using System.Reflection;
using Tgs.RouletteServices;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRouletteSignInEvents(this IServiceCollection services)
		=> services
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
			.AddMemoryCache()
			.AddSingleton<RouletteSerializer>()
			.AddSingleton<IShuffleService>(sp => ActivatorUtilities.CreateInstance<MemoryStoreShuffleService>(
				   sp,
				   ActivatorUtilities.CreateInstance<ShuffleService>(sp)))
			.AddSingleton<IStatisticsService, StatisticsService>();
}
