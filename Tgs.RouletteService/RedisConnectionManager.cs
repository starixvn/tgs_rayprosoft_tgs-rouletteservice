using StackExchange.Redis;

namespace Tgs.RouletteService;

public class RedisConnectionManager : IDisposable
{
	private readonly ConnectionMultiplexer _connectionMultiplexer;
	private bool _disposedValue;

	public IDatabase Database { get; private set; }

	public RedisConnectionManager(string connectionString)
	{
		var connectionOptions = ConfigurationOptions.Parse(connectionString);

		_connectionMultiplexer = ConnectionMultiplexer.Connect(connectionOptions);

		Database = _connectionMultiplexer.GetDatabase();
	}

	public void Dispose()
	{
		// 請勿變更此程式碼。請將清除程式碼放入 'Dispose(bool disposing)' 方法
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_connectionMultiplexer?.Dispose();
			}

			_disposedValue = true;
		}
	}
}
