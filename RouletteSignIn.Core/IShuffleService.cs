namespace Tgs.RouletteServices;

public interface IShuffleService
{
	ValueTask<int[]> ShuffleAsync(Guid promotionId, IEnumerable<RouletteItem> items);
}
