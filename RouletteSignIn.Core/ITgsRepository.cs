namespace Tgs.RouletteServices;

public interface ITgsRepository
{
	ValueTask<RouletteSignInRecord?> GetRouletteSignInRecordAsync(Guid promotionId, string playerCode, DateOnly date, CancellationToken cancellationToken = default);

	ValueTask<Guid> AddRouletteRecordAsync(
		Guid promotionId,
		string playerCode,
		Guid brandId,
		DateOnly date,
		int itemIndex,
		decimal bonus,
		CancellationToken cancellationToken = default);
}
