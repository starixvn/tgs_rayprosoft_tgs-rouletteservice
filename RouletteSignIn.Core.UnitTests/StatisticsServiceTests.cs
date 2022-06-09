using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.RouletteServices;

namespace RouletteSignIn.Core.UnitTests;

[TestClass]
public class StatisticsServiceTests
{
	[TestMethod]
	public async Task CheckOverTotalDistributeAmount_Without_Over_Should_Return_False()
	{
		var fakeStatisticsRepository = Substitute.For<IStatisticsRepository>();

		var sut = new StatisticsService(fakeStatisticsRepository);

		var promotionId = Guid.NewGuid();

		_ = fakeStatisticsRepository
			.GetTotalDistributeAmountAsync(Arg.Is(promotionId))
			.Returns(90M);

		var actual = await sut.CheckOverTotalDistributeAmountAsync(promotionId, 100M);

		Assert.IsFalse(actual);
	}

	[TestMethod]
	public async Task IncrementTotalDistributeAmount_Without_Over_Limit()
	{
		var fakeStatisticsRepository = Substitute.For<IStatisticsRepository>();

		var sut = new StatisticsService(fakeStatisticsRepository);

		var promotionId = Guid.NewGuid();

		_ = fakeStatisticsRepository
			.IncrementTotalDistributeAmountAsync(Arg.Is(promotionId), Arg.Is(20M))
			.Returns(110M);

		var (isOver, resultAmount) = await sut.IncrementTotalDistributeAmountAsync(promotionId, 100M, 20M, 10M);

		Assert.IsFalse(isOver);
		Assert.AreEqual(110M, resultAmount);
	}

	[TestMethod]
	public async Task IncrementTotalDistributeAmount_Over_Limit()
	{
		var fakeStatisticsRepository = Substitute.For<IStatisticsRepository>();

		var sut = new StatisticsService(fakeStatisticsRepository);

		var promotionId = Guid.NewGuid();

		_ = fakeStatisticsRepository
			.IncrementTotalDistributeAmountAsync(Arg.Is(promotionId), Arg.Is(20M))
			.Returns(120M);

		_ = fakeStatisticsRepository
			.DecreaseTotalDistributeAmountAsync(Arg.Is(promotionId), Arg.Is(20M - 10M))
			.Returns(110M);

		var (isOver, resultAmount) = await sut.IncrementTotalDistributeAmountAsync(promotionId, 100M, 20M, 10M);

		Assert.IsTrue(isOver);
		Assert.AreEqual(110M, resultAmount);
	}

	[TestMethod]
	public async Task IncrementTotalDistributeAmount_Over_Limit_But_DifferenceAmount_Is_Zero()
	{
		var fakeStatisticsRepository = Substitute.For<IStatisticsRepository>();

		var sut = new StatisticsService(fakeStatisticsRepository);

		var promotionId = Guid.NewGuid();

		_ = fakeStatisticsRepository
			.IncrementTotalDistributeAmountAsync(Arg.Is(promotionId), Arg.Is(20M))
			.Returns(120M);

		var (isOver, resultAmount) = await sut.IncrementTotalDistributeAmountAsync(promotionId, 100M, 20M, 20M);

		Assert.IsTrue(isOver);
		Assert.AreEqual(120M, resultAmount);

		_ = fakeStatisticsRepository.Received(0)
			.DecreaseTotalDistributeAmountAsync(
				Arg.Any<Guid>(),
				Arg.Any<decimal>());
	}

	[TestMethod]
	public async Task IncrementRouletteItemCount_Without_Over_Limit()
	{
		var fakeStatisticsRepository = Substitute.For<IStatisticsRepository>();

		var sut = new StatisticsService(fakeStatisticsRepository);

		var promotionId = Guid.NewGuid();
		var rouletteItemId = Guid.NewGuid();
		var date = DateOnly.FromDateTime(DateTime.Now);

		var actual = await sut.IncrementRouletteItemCountAsync(promotionId, rouletteItemId, date, 10);

		Assert.IsTrue(actual);

		_ = fakeStatisticsRepository.Received(1)
			.IncrementRouletteItemCountAsync(
				Arg.Is(promotionId),
				Arg.Is(rouletteItemId),
				Arg.Is(date));

		_ = fakeStatisticsRepository.Received(0)
			.DecreaseRouletteItemCountAsync(
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Any<DateOnly>());
	}

	[TestMethod]
	public async Task IncrementRouletteItemCount_Over_Limit()
	{
		var fakeStatisticsRepository = Substitute.For<IStatisticsRepository>();

		var sut = new StatisticsService(fakeStatisticsRepository);

		var promotionId = Guid.NewGuid();
		var rouletteItemId = Guid.NewGuid();
		var date = DateOnly.FromDateTime(DateTime.Now);

		_ = fakeStatisticsRepository
			.IncrementRouletteItemCountAsync(
				Arg.Is(promotionId),
				Arg.Is(rouletteItemId),
				Arg.Is(date))
			.Returns(11);

		var actual = await sut.IncrementRouletteItemCountAsync(promotionId, rouletteItemId, date, 10);

		Assert.IsFalse(actual);

		_ = fakeStatisticsRepository.Received(1)
			.IncrementRouletteItemCountAsync(
				Arg.Is(promotionId),
				Arg.Is(rouletteItemId),
				Arg.Is(date));

		_ = fakeStatisticsRepository.Received(1)
			.DecreaseRouletteItemCountAsync(
				Arg.Is(promotionId),
				Arg.Is(rouletteItemId),
				Arg.Is(date));
	}
}
