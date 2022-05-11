namespace Tgs.RouletteServices.ApplicationServices;
public record GetRouletteRecordQuery(Guid PromotionId, string PlayerCode, DateOnly Date);
