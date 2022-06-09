namespace Tgs.RouletteServices;

public interface IStatisticsRepository
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<decimal> DecreaseTotalDistributeAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<decimal> GetTotalDistributeAmountAsync(Guid promotionId, CancellationToken cancellationToken = default);

	ValueTask<long> IncrementRouletteItemCountAsync(Guid promotionId, Guid itemId, DateOnly date, CancellationToken cancellationToken = default);

	ValueTask<long> DecreaseRouletteItemCountAsync(Guid promotionId, Guid itemId, DateOnly date, CancellationToken cancellationToken = default);
}
