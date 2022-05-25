namespace Tgs.RouletteServices;

public interface ITgsRepository
{
	ValueTask<RouletteSignInRecord?> GetRouletteSignInRecordAsync(Guid promotionId, string playerCode, DateOnly date, CancellationToken cancellationToken = default);

	ValueTask AddRouletteRecordAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date,
		int itemIndex,
		decimal bonus,
		CancellationToken cancellationToken = default);
}
