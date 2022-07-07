using StackExchange.Redis;
using Tgs.RouletteServices;

namespace Tgs.RouletteService.Repositories;

internal class StatisticsRepository : IStatisticsRepository
{
	private static readonly string _Prefix = "Tgs:Roulette";
	private static readonly decimal _ExchangeRate = 1_000_000M;

	private readonly IDatabase _redisDatabase;

	public StatisticsRepository(IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
	}

	public async ValueTask<decimal> IncrementTotalDistributeAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default)
	{
		var returnAmount = await _redisDatabase
			.StringIncrementAsync($"{_Prefix}:Total:{promotionId}", (long)Math.Floor(amount * _ExchangeRate))
			.ConfigureAwait(false);

		return returnAmount / _ExchangeRate;
	}

	public async ValueTask<decimal> DecreaseTotalDistributeAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default)
	{
		var returnAmount = await _redisDatabase
			.StringDecrementAsync($"{_Prefix}:Total:{promotionId}", (long)Math.Floor(amount * _ExchangeRate))
			.ConfigureAwait(false);

		return returnAmount / _ExchangeRate;
	}

	public async ValueTask<decimal> GetTotalDistributeAmountAsync(Guid promotionId, CancellationToken cancellationToken = default)
	{
		var returnAmount = await _redisDatabase
			.StringGetAsync($"{_Prefix}:Total:{promotionId}")
			.ConfigureAwait(false);

		return (long)returnAmount / _ExchangeRate;
	}

	public async ValueTask<long> IncrementRouletteItemCountAsync(Guid promotionId, Guid itemId, DateOnly date, CancellationToken cancellationToken = default)
	{
		return await _redisDatabase
			.StringIncrementAsync($"{_Prefix}:Roulette:{promotionId}:{itemId}:{date:yyyyMMdd}")
			.ConfigureAwait(false);
	}

	public async ValueTask<long> DecreaseRouletteItemCountAsync(Guid promotionId, Guid itemId, DateOnly date, CancellationToken cancellationToken = default)
	{
		return await _redisDatabase
			.StringDecrementAsync($"{_Prefix}:Roulette:{promotionId}:{itemId}:{date:yyyyMMdd}")
			.ConfigureAwait(false);
	}
}
