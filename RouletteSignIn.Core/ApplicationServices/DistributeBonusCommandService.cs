using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.RouletteServices.ApplicationServices;
internal class DistributeBonusCommandService : ICommandService<DistributeBonusCommand>
{
	private readonly IPromotionServiceClient _promotionServiceClient;

	public DistributeBonusCommandService(IPromotionServiceClient promotionServiceClient)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
	}
	public async ValueTask ExecuteAsync(DistributeBonusCommand command, CancellationToken cancellationToken = default)
	{
		var plan = await _promotionServiceClient.GetPromotionDistributePlanAsync(
			command.PromotionId,
			command.DistributeDate.DayNumber,
			cancellationToken).ConfigureAwait(false);

		_ = await _promotionServiceClient.DistributePromotionBonusAsync(
			plan.Id,
			command.PlayerCode,
			command.BonusAmount,
			command.Bucket,
			null,
			cancellationToken).ConfigureAwait(false);
	}
}
