using StackExchange.Redis;
using Tgs.RouletteServices;

namespace Tgs.RouletteService.Repositories;

internal class StatisticsRepository : IStatisticsRepository
{
	private static readonly string _Prefix = "Tgs:Roulette:Total";
	private static readonly decimal _ExchangeRate = 1_000_000M;

	private readonly IDatabase _redisDatabase;

	public StatisticsRepository(IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
	}

	public async ValueTask<decimal> IncrementTotalDistributeAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default)
	{
		var returnAmount = await _redisDatabase
			.StringIncrementAsync($"{_Prefix}:{promotionId}", (long)Math.Floor(amount * _ExchangeRate))
			.ConfigureAwait(false);

		return returnAmount / _ExchangeRate;
	}

	public ValueTask<decimal> DecreaseTotalDistributeAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default)
		=> IncrementTotalDistributeAmountAsync(promotionId, amount, cancellationToken);

	public async ValueTask<decimal> GetTotalDistributeAmountAsync(Guid promotionId, CancellationToken cancellationToken = default)
	{
		var returnAmount = await _redisDatabase
			.StringGetAsync($"{_Prefix}:{promotionId}")
			.ConfigureAwait(false);

		return (long)returnAmount / _ExchangeRate;
	}
}
