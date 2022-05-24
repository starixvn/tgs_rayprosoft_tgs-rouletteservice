using System.Reflection;
using Tgs.RouletteServices;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRouletteSignInEvents(this IServiceCollection services)
		=> services
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
			.AddSingleton<RouletteSerializer>()
			.AddSingleton<IShuffleService, ShuffleService>();
}
