using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExpectedObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.RouletteServices;
using Tgs.RouletteServices.ApplicationServices;
using Tgs.ServiceUtility;

namespace RouletteSignIn.Core.UnitTests;

[TestClass]
public class RouletteTests
{
	[TestMethod]
	public async Task Get_Player_RouletteSignInRecord()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetRouletteRecordQueryService(fakeTgsRepository);

		var query = new GetRouletteRecordQuery(
			Guid.NewGuid(),
			"player1",
			DateOnly.FromDateTime(DateTime.Now));

		var signInRecord = new RouletteSignInRecord(Guid.NewGuid(), query.PlayerCode, query.Date, 1);
		_ = fakeTgsRepository.GetRouletteSignInRecordAsync(
			Arg.Is(query.PromotionId),
			Arg.Is(query.PlayerCode),
			Arg.Is(query.Date))
			.Returns(signInRecord);

		var actual = await sut.GetAsync(query);

		Assert.AreEqual(signInRecord, actual);
	}

	[TestMethod]
	public async Task Player_Without_Roulette_Get_Record_Should_Return_Nil()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new GetRouletteRecordQueryService(fakeTgsRepository);

		var query = new GetRouletteRecordQuery(
			Guid.NewGuid(),
			"player1",
			DateOnly.FromDateTime(DateTime.Now));

		var actual = await sut.GetAsync(query);

		Assert.IsNull(actual);
	}

	[TestMethod]
	public async Task Play_Roulette()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetRoulettePromotionService = Substitute.For<IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetRoulettePromotionService,
			fakeShuffleService,
			fakeStatisticsService);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1",
			DateOnly.FromDateTime(DateTime.Now));

		_ = fakeGetRoulettePromotionService.GetAsync(
			Arg.Is<GetRoulettePromotionSettingsQuery>(cmd => cmd.PromotionId == command.PromotionId))
			.Returns(new RoulettePromotion(
				command.PromotionId,
				null,
				new RouletteSettings(
					new[] {
						new RouletteItem(
							Guid.NewGuid(),
							"送8元",
							8M,
							0.8M,
							2M,
							0,
							true),
						new RouletteItem(
							Guid.NewGuid(),
							"送18元",
							18M,
							0.1M,
							10M,
							50,
							false)
					})));

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.PromotionId),
			Arg.Any<decimal?>(),
			Arg.Any<decimal>(),
			Arg.Any<decimal>())
			.Returns((false, 100M));

		_ = fakeStatisticsService.IncrementRouletteItemCountAsync(
			Arg.Is(command.PromotionId),
			Arg.Any<Guid>(),
			Arg.Any<DateOnly>(),
			Arg.Any<int?>())
			.Returns(true);

		_ = fakeShuffleService
			.Shuffle(Arg.Is(command.PromotionId), Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 0, 1 });

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(1)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Is(command.Date),
				Arg.Any<int>());

		_ = fakeStatisticsService.Received(1)
			.IncrementTotalDistributeAmountAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(default(decimal?)),
				Arg.Any<decimal>(),
				Arg.Is(8M));
	}

	[TestMethod]
	public async Task If_Already_Roulette_Should_Not_Roulette_Again()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetRoulettePromotionService = Substitute.For<IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetRoulettePromotionService,
			fakeShuffleService,
			fakeStatisticsService);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1",
			DateOnly.FromDateTime(DateTime.Now));

		_ = fakeTgsRepository.GetRouletteSignInRecordAsync(
			Arg.Is(command.PromotionId),
			Arg.Is(command.PlayerCode),
			Arg.Is(command.Date))
			.Returns(new RouletteSignInRecord(
				Guid.NewGuid(),
				command.PlayerCode,
				command.Date,
				2));

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(0)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<string>(),
				Arg.Any<DateOnly>(),
				Arg.Any<int>());
	}

	[TestMethod]
	public void If_Not_Found_Roulette_Promotion_Will_Throw_Exception()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetRoulettePromotionService = Substitute.For<IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetRoulettePromotionService,
			fakeShuffleService,
			fakeStatisticsService);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1",
			DateOnly.FromDateTime(DateTime.Now));

		_ = Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await sut.ExecuteAsync(command));
	}

	[TestMethod]
	public void If_Roulette_Promotion_No_Any_Item_Will_Throw_Exception()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetRoulettePromotionService = Substitute.For<IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetRoulettePromotionService,
			fakeShuffleService,
			fakeStatisticsService);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1",
			DateOnly.FromDateTime(DateTime.Now));

		_ = fakeGetRoulettePromotionService.GetAsync(
			Arg.Is<GetRoulettePromotionSettingsQuery>(cmd => cmd.PromotionId == command.PromotionId))
			.Returns(new RoulettePromotion(
				command.PromotionId,
				null,
				new RouletteSettings(
					Array.Empty<RouletteItem>())));

		_ = Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await sut.ExecuteAsync(command));
	}

	[TestMethod]
	public async Task If_Over_Total_Distribute_Amount_Will_Return_Default_Item()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetRoulettePromotionService = Substitute.For<IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetRoulettePromotionService,
			fakeShuffleService,
			fakeStatisticsService);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1",
			DateOnly.FromDateTime(DateTime.Now));

		_ = fakeGetRoulettePromotionService.GetAsync(
			Arg.Is<GetRoulettePromotionSettingsQuery>(cmd => cmd.PromotionId == command.PromotionId))
			.Returns(new RoulettePromotion(
				command.PromotionId,
				100M,
				new RouletteSettings(
					new[] {
						new RouletteItem(
							Guid.NewGuid(),
							"送8元",
							8M,
							0M,
							2M,
							0,
							true),
						new RouletteItem(
							Guid.NewGuid(),
							"送18元",
							18M,
							1M,
							10M,
							50,
							false)
					})));

		_ = fakeShuffleService
			.Shuffle(Arg.Is(command.PromotionId), Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 1, 1 });

		_ = fakeStatisticsService.CheckOverTotalDistributeAmountAsync(
			Arg.Is(command.PromotionId),
			Arg.Is(100M))
			.Returns(true);

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(1)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Is(command.Date),
				Arg.Is(0));

		_ = fakeStatisticsService.Received(1)
			.IncrementTotalDistributeAmountAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(default(decimal?)),
				Arg.Is(8M),
				Arg.Is(8M));
	}

	[TestMethod]
	public async Task If_Winning_Amount_Already_Over_Total_Distribute_Amount_Will_Change_To_Default_Item()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetRoulettePromotionService = Substitute.For<IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetRoulettePromotionService,
			fakeShuffleService,
			fakeStatisticsService);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1",
			DateOnly.FromDateTime(DateTime.Now));

		_ = fakeGetRoulettePromotionService.GetAsync(
			Arg.Is<GetRoulettePromotionSettingsQuery>(cmd => cmd.PromotionId == command.PromotionId))
			.Returns(new RoulettePromotion(
				command.PromotionId,
				100M,
				new RouletteSettings(
					new[] {
						new RouletteItem(
							Guid.NewGuid(),
							"送8元",
							8M,
							0M,
							2M,
							0,
							true),
						new RouletteItem(
							Guid.NewGuid(),
							"送18元",
							18M,
							1M,
							10M,
							50,
							false)
					})));

		_ = fakeStatisticsService.IncrementRouletteItemCountAsync(
			Arg.Is(command.PromotionId),
			Arg.Any<Guid>(),
			Arg.Any<DateOnly>(),
			Arg.Any<int?>())
			.Returns(true);

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.PromotionId),
			Arg.Is(100M),
			Arg.Is(18M),
			Arg.Is(8M))
			.Returns((true, 100M));

		_ = fakeShuffleService
			.Shuffle(Arg.Is(command.PromotionId), Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 1, 1 });

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(1)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Is(command.Date),
				Arg.Is(0));
	}

	[TestMethod]
	public async Task If_Winning_RouletteItem_Over_Daily_Limit_Will_Change_To_Default_Item()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetRoulettePromotionService = Substitute.For<IGetService<GetRoulettePromotionSettingsQuery, RoulettePromotion?>>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetRoulettePromotionService,
			fakeShuffleService,
			fakeStatisticsService);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1",
			DateOnly.FromDateTime(DateTime.Now));

		_ = fakeGetRoulettePromotionService.GetAsync(
			Arg.Is<GetRoulettePromotionSettingsQuery>(cmd => cmd.PromotionId == command.PromotionId))
			.Returns(new RoulettePromotion(
				command.PromotionId,
				100M,
				new RouletteSettings(
					new[] {
						new RouletteItem(
							Guid.NewGuid(),
							"送8元",
							8M,
							0M,
							2M,
							0,
							true),
						new RouletteItem(
							Guid.NewGuid(),
							"送18元",
							18M,
							1M,
							10M,
							50,
							false)
					})));

		_ = fakeStatisticsService.IncrementRouletteItemCountAsync(
			Arg.Is(command.PromotionId),
			Arg.Any<Guid>(),
			Arg.Any<DateOnly>(),
			Arg.Any<int?>())
			.Returns(false);

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.PromotionId),
			Arg.Is(100M),
			Arg.Is(18M),
			Arg.Is(8M))
			.Returns((false, 100M));

		_ = fakeShuffleService
			.Shuffle(Arg.Is(command.PromotionId), Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 1, 1 });

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(1)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Is(command.Date),
				Arg.Is(0));
	}

	[TestMethod]
	public async Task Get_RoulettePromotion_Info()
	{
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new GetRoulettePromotionSettingsQueryService(fakePromotionServiceClient);

		var query = new GetRoulettePromotionSettingsQuery(Guid.NewGuid());

		_ = fakePromotionServiceClient.GetPromotionMainInfoAsync(Arg.Is(query.PromotionId))
			.Returns(new PromotionMainInfo
			{
				Id = query.PromotionId,
				EventDuring = (DateTime.Now.AddDays(-10D), DateTime.Now),
				IsAutoSent = true,
				Name = "Test Roulette",
				PromotionLimitAmount = null,
				PromotionType = "Roulette"
			});

		var rouletteItemId = Guid.NewGuid();
		_ = fakePromotionServiceClient.GetPromotionRuleAsync<RouletteSettings>(Arg.Is(query.PromotionId))
			.Returns(new RouletteSettings(new[] {
				new RouletteItem(rouletteItemId, "Test1", 100M, 0.1M, 10M, 0, true)
			}));

		var expected = new RoulettePromotion(
			query.PromotionId,
			null,
			new RouletteSettings(new[] {
				new RouletteItem(rouletteItemId, "Test1", 100M, 0.1M, 10M, 0, true)
			}))
			.ToExpectedObject(ctx => ctx.IgnoreEqualsOverride());

		var actual = await sut.GetAsync(query);

		expected.ShouldEqual(actual);
	}
}
