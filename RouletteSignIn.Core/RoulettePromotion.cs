namespace Tgs.RouletteServices;
internal record RoulettePromotion(
	Guid Id,
	decimal? PromotionLimitAmount,
	RouletteSettings CampaignDetail);
