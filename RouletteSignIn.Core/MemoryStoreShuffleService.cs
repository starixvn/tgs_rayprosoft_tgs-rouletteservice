using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Tgs.RouletteServices;

internal class MemoryStoreShuffleService : IShuffleService
{
	private static readonly SHA1 _Hasher = SHA1.Create();
	private readonly IShuffleService _sourceService;
	private readonly IMemoryCache _memoryCache;
	private readonly RouletteSerializer _rouletteSerializer;

	public MemoryStoreShuffleService(IShuffleService sourceService, IMemoryCache memoryCache, RouletteSerializer rouletteSerializer)
	{
		_sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		_rouletteSerializer = rouletteSerializer ?? throw new ArgumentNullException(nameof(rouletteSerializer));
	}

	public async ValueTask<int[]> ShuffleAsync(Guid promotionId, IEnumerable<RouletteItem> items)
	{
		var json = _rouletteSerializer.Serialize(items);

		var hashCode = Convert.ToHexString(_Hasher.ComputeHash(Encoding.UTF8.GetBytes(json)));

		var cacheKey = $"{promotionId}:{hashCode}";

		return await _memoryCache
			.GetOrCreateAsync(
				cacheKey,
				async entry =>
				{
					entry.SetAbsoluteExpiration(DateTimeOffset.Now.AddHours(6D));

					return await _sourceService
						.ShuffleAsync(promotionId, items)
						.ConfigureAwait(false);
				})
			.ConfigureAwait(false);
	}
}
