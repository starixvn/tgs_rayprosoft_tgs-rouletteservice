using Tgs.ServiceUtility;

namespace Tgs.RouletteServices.ApplicationServices;

internal class GetRouletteRecordQueryService : IGetService<GetRouletteRecordQuery, RouletteSignInRecord?>
{
	private readonly ITgsRepository _tgsRepository;

	public GetRouletteRecordQueryService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public ValueTask<RouletteSignInRecord?> GetAsync(GetRouletteRecordQuery query, CancellationToken cancellationToken = default)
		=> _tgsRepository.GetRouletteSignInRecordAsync(
			query.PromotionId,
			query.PlayerCode,
			query.Date,
			cancellationToken);
}
