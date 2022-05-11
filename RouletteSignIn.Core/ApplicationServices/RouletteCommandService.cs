using System.Security.Cryptography;
using Tgs.ServiceUtility;

namespace Tgs.RouletteServices.ApplicationServices;

internal class RouletteCommandService : ICommandService<RouletteCommand>
{
	private readonly ITgsRepository _tgsRepository;

	public RouletteCommandService(ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public async ValueTask ExecuteAsync(RouletteCommand command, CancellationToken cancellationToken = default)
	{
		var currentRecord = await _tgsRepository.GetRouletteSignInRecordAsync(
			command.PromotionId,
			command.PlayerCode,
			command.Date,
			cancellationToken).ConfigureAwait(false);

		if (currentRecord is not null)
			return;

		var itemIndex = RandomNumberGenerator.GetInt32(0, 6);

		await _tgsRepository.AddRouletteRecordAsync(
			command.PromotionId,
			command.PlayerCode,
			command.Date,
			itemIndex,
			cancellationToken).ConfigureAwait(false);
	}
}
