using Microsoft.EntityFrameworkCore;
using Tgs.RouletteServices;
using Tgs.RouletteServices.Repositories.Entities;
using Tgs.SerialNumberGenerator;

namespace Tgs.RouletteService.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly TgsContext _dbContext;
	private readonly ISerialNumberGenerator _serialNumberGenerator;

	public TgsRepository(TgsContext dbContext, ISerialNumberGenerator serialNumberGenerator)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public async ValueTask<RouletteSignInRecord?> GetRouletteSignInRecordAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date,
		CancellationToken cancellationToken = default)
	{
		var record = await _dbContext.RouletteSignInEvents
			.SingleOrDefaultAsync(
				rs => rs.PromotionId == promotionId && rs.PlayerCode == playerCode && rs.Date == date,
				cancellationToken).ConfigureAwait(false);

		return record is null
			? null
			: new RouletteSignInRecord(record.Id, record.PlayerCode, record.Date, record.ItemIndex);
	}

	public async ValueTask AddRouletteRecordAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date,
		int itemIndex,
		CancellationToken cancellationToken = default)
	{
		var player = await _dbContext.Players
			.FindAsync(new object?[] { playerCode }, cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
			return;

		var id = await _serialNumberGenerator
			.GenerateAsync()
			.ConfigureAwait(false);

		var insertRecord = new RouletteSignInEvent
		{
			Id = id,
			PlayerCode = playerCode,
			BrandId = player.BrandId,
			PromotionId = promotionId,
			ItemIndex = itemIndex,
			Date = date
		};

		_ = await _dbContext.RouletteSignInEvents
			.AddAsync(insertRecord, cancellationToken)
			.ConfigureAwait(false);

		await _dbContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}
}
