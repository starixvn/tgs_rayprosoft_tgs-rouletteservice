using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.RouletteServices.ApplicationServices;
using Tgs.ServiceUtility;

namespace RouletteSignIn.Core.UnitTests;

[TestClass]
public class DistributeBonusTests
{
	[TestMethod]
	public async Task Distribute_Bonus()
	{
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeGetDistributeService = Substitute.For<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>>();

		var sut = new DistributeBonusCommandService(
			fakePromotionServiceClient,
			fakeGetDistributeService);

		var distributeDate = DateOnly.FromDateTime(DateTime.Now);
		var command = new DistributeBonusCommand(
			Guid.NewGuid(),
			"player1",
			distributeDate,
			10M,
			100M);

		var planId = Guid.NewGuid();
		_ = fakeGetDistributeService
			.GetAsync(Arg.Any<GetPromotionDistributePlanQuery>())
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

	[TestMethod]
	public async Task Get_Promotion_Distribute_Plan()
	{
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new GetPromotionDistributePlanQueryService(fakePromotionServiceClient);

		var query = new GetPromotionDistributePlanQuery(Guid.NewGuid(), 12345L);

		_ = await sut.GetAsync(query);

		_ = fakePromotionServiceClient.Received(1)
			.GetPromotionDistributePlanAsync(
				Arg.Is(query.PromotionId),
				Arg.Is(query.ConditionHash));
	}
}
