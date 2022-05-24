namespace Tgs.RouletteServices;

public interface IStatisticsService
{
	ValueTask<decimal> GetTotalDistributeAmountAsync(Guid promotionId, CancellationToken cancellationToken = default);

	ValueTask<decimal> IncrementTotalDistributeAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default);

	ValueTask<decimal> DecreaseTotalDistributeAmountAsync(Guid promotionId, decimal amount, CancellationToken cancellationToken = default);
}
