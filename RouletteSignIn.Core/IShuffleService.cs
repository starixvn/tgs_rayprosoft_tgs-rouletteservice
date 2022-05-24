namespace Tgs.RouletteServices;

public interface IShuffleService
{
	int[] Shuffle(Guid promotionId, IEnumerable<RouletteItem> items);
}
