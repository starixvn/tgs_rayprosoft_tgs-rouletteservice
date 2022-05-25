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

		fakeStatisticsRepository
			.IncrementTotalDistributeAmountAsync(Arg.Is(promotionId), Arg.Is(20M))
			.Returns(110M);

		var actual = await sut.IncrementTotalDistributeAmountAsync(promotionId, 100M, 20M, 10M);

		Assert.IsFalse(actual.IsOver);
		Assert.AreEqual(110M, actual.ResultAmount);
	}

	[TestMethod]
	public async Task IncrementTotalDistributeAmount_Over_Limit()
	{
		var fakeStatisticsRepository = Substitute.For<IStatisticsRepository>();

		var sut = new StatisticsService(fakeStatisticsRepository);

		var promotionId = Guid.NewGuid();

		fakeStatisticsRepository
			.IncrementTotalDistributeAmountAsync(Arg.Is(promotionId), Arg.Is(20M))
			.Returns(120M);

		fakeStatisticsRepository
			.DecreaseTotalDistributeAmountAsync(Arg.Is(promotionId), Arg.Is(20M - 10M))
			.Returns(110M);

		var actual = await sut.IncrementTotalDistributeAmountAsync(promotionId, 100M, 20M, 10M);

		Assert.IsTrue(actual.IsOver);
		Assert.AreEqual(110M, actual.ResultAmount);
	}
}