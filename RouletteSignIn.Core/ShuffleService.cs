namespace Tgs.RouletteServices;

internal class ShuffleService : IShuffleService
{
	private static readonly Random _Random = new();

	public ValueTask<int[]> ShuffleAsync(Guid promotionId, IEnumerable<RouletteItem> items)
	{
		var proportions = items
			.Select(item => Math.Floor(item.WinningProbability * 10000M))
			.ToArray();
		var gcdResult = Gcd(proportions.Where(n => n > 0M));

		var minProportions = proportions
			.Select(p => p / gcdResult)
			.ToArray();

		var deckSize = (int)minProportions.Sum();

		var deck = minProportions
			.Select((proportion, index) => (Proportion: proportion, Index: index))
			.Aggregate(new List<int>(deckSize), (seed, t) =>
			{
				seed.AddRange(Enumerable.Repeat(t.Index, (int)t.Proportion));
				return seed;
			});

		for (var i = 0; i < 2; i++)
			deck = Shuffle(deck);

		return ValueTask.FromResult(deck.ToArray());
	}

	private static List<int> Shuffle(IEnumerable<int> deck)
	{
		var originDeck = deck.ToList();
		var newDeck = new List<int>(originDeck.Count);

		var total = originDeck.Count;
		for (var i = 0; i < total; i++)
		{
			var index = _Random.Next(0, originDeck.Count);

			newDeck.Add(originDeck[index]);
			originDeck.RemoveAt(index);
		}

		return newDeck;
	}

	private static decimal Gcd(IEnumerable<decimal> numbers)
		=> numbers.Aggregate(Gcd);

	private static decimal Gcd(decimal a, decimal b)
		=> b is 0 ? a : Gcd(b, a % b);
}
