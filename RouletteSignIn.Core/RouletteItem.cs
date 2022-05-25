using System.Text.Json.Serialization;

namespace Tgs.RouletteServices;
public record RouletteItem(
	Guid Id,
	string Title,
	decimal Bonus,
	decimal WinningProbability,
	decimal TurnoverRate,
	int DailyWinningLimit,
	bool IsDefault = false);
