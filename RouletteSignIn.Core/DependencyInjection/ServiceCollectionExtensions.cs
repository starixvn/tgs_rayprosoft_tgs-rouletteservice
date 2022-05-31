using System.Reflection;
using Tgs.Promotion.Clients;
using Tgs.RouletteServices;
using Tgs.RouletteServices.ApplicationServices;
using Tgs.ServiceUtility;

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
			.AddSingleton<IStatisticsService, StatisticsService>()
			.AddTransient<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>, GetPromotionDistributePlanQueryService>()
			.Decorate<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>, MemoryCachedGetPromotionDistributePlanQueryService>();
}
