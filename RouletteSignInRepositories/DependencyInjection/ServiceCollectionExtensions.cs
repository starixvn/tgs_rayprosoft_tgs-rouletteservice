using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tgs.RouletteServices;
using Tgs.RouletteServices.Repositories.Entities;

namespace Tgs.RouletteService.Repositories.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRouletteSignInEventRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> dbConfigure)
		=> services
			.AddDbContext<TgsContext>(dbConfigure)
			.AddTransient<ITgsRepository, TgsRepository>()
			.AddSerialNumberGenerator();
}
