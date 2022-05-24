namespace Tgs.RouletteServices;

public interface IStatisticsRepository
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<decimal> DecreaseTotalDistributeAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<decimal> GetTotalDistributeAmountAsync(Guid promotionId, CancellationToken cancellationToken = default);
}
