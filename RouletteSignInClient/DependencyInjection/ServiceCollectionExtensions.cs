using Tgs.RouletteServices.Clients;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRouletteSignInClient(this IServiceCollection services)
		=> services.AddTransient<IRouletteSignInClient, RouletteSignInClient>();
}
