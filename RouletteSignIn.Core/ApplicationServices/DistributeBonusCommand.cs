namespace Tgs.RouletteServices.ApplicationServices;
public record DistributeBonusCommand(
	Guid PromotionId,
	string PlayerCode,
	DateOnly DistributeDate,
	decimal BonusAmount,
	decimal Bucket);
