using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.RouletteServices.ApplicationServices;

namespace RouletteSignIn.Core.UnitTests;
[TestClass]
public class DistributeBonusTests
{
	[TestMethod]
	public async Task Distribute_Bonus()
	{
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new DistributeBonusCommandService(fakePromotionServiceClient);

		var distributeDate = DateOnly.FromDateTime(DateTime.Now);
		var command = new DistributeBonusCommand(
			Guid.NewGuid(),
			"player1",
			distributeDate,
			10M,
			100M);

		var planId = Guid.NewGuid();
		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(
			Arg.Is(command.PromotionId),
			Arg.Is<long>(distributeDate.DayNumber))
			.Returns(new PromotionDistributePlanInfo(
				planId,
				Guid.NewGuid(),
				command.PromotionId,
				distributeDate.DayNumber));

		await sut.ExecuteAsync(command);

		_ = fakePromotionServiceClient.Received(1)
			.DistributePromotionBonusAsync(
				Arg.Is(planId),
				Arg.Is(command.PlayerCode),
				Arg.Is(command.BonusAmount),
				Arg.Is(command.Bucket),
				Arg.Any<string>());
	}
}