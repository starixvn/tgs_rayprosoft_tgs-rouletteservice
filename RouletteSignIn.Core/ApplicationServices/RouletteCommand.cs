namespace Tgs.RouletteServices.ApplicationServices;
public record RouletteCommand(Guid PromotionId, string PlayerCode, DateOnly Date);
