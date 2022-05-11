namespace Tgs.RouletteServices.Clients;

public interface IRouletteSignInClient
{
	ValueTask<RouletteSignInRecord> RouletteAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date,
		CancellationToken cancellationToken = default);

	ValueTask<RouletteSignInRecord?> GetRouletteRecordAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date,
		CancellationToken cancellationToken = default);
}
