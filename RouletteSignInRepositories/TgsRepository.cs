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

	public async ValueTask<Guid> AddRouletteRecordAsync(
		Guid promotionId,
		string playerCode,
		Guid brandId,
		DateOnly date,
		int itemIndex,
		decimal bonus,
		CancellationToken cancellationToken = default)
	{
		var id = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var insertRecord = new RouletteSignInEvent
		{
			Id = id,
			PlayerCode = playerCode,
			BrandId = brandId,
			PromotionId = promotionId,
			ItemIndex = itemIndex,
			Date = date,
			Bonus = bonus
		};

		_ = await _dbContext.RouletteSignInEvents
			.AddAsync(insertRecord, cancellationToken)
			.ConfigureAwait(false);

		_ = await _dbContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);

		return id;
	}
}
