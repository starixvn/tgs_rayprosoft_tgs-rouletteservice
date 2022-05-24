using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.RouletteService.MessageQueue;
using Tgs.RouletteServices;
using Tgs.RouletteServices.Clients;
using Tgs.RouletteServices.Repositories.Entities;

namespace RouletteSignIn.InntegrationTests;

[TestClass]
public class RouletteSignInTests
{
	[TestMethod]
	public async Task Get_Roulette_Record()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();

		await using var web = new TestRouletteSignInApplication();

		var sut = new RouletteSignInClient(fakeMessageSender);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var date = DateOnly.FromDateTime(DateTime.Now);
		var playerCode = "tester";

		await using var serviceScope = web.Services.CreateAsyncScope();

		var dbContext = serviceScope.ServiceProvider.GetRequiredService<TgsContext>();

		_ = await dbContext.RouletteSignInEvents.AddAsync(new RouletteSignInEvent
		{
			Id = Guid.NewGuid(),
			BrandId = brandId,
			Date = date,
			PlayerCode = playerCode,
			ItemIndex = 1,
			PromotionId = promotionId,
			CreatedOn = DateTime.Now
		});

		_ = await dbContext.SaveChangesAsync();

		_ = fakeMessageSender.RequestAsync(
			Arg.Is("tgs.roulette.daily.query"),
			Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<RouletteQueryProcessor>(serviceScope.ServiceProvider);

				return processor.HandleAsync(data).Result;
			});

		var actual = await sut.GetRouletteRecordAsync(promotionId, playerCode, date);

		Assert.IsNotNull(actual);
		Assert.AreEqual(1, actual?.ItemIndex);
	}

	[TestMethod]
	public async Task Get_Roulette_Record_Not_Found_Will_Return_Nil()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();

		await using var web = new TestRouletteSignInApplication();

		var sut = new RouletteSignInClient(fakeMessageSender);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var date = DateOnly.FromDateTime(DateTime.Now);
		var playerCode = "tester";

		await using var serviceScope = web.Services.CreateAsyncScope();

		var dbContext = serviceScope.ServiceProvider.GetRequiredService<TgsContext>();

		_ = fakeMessageSender.RequestAsync(
			Arg.Is("tgs.roulette.daily.query"),
			Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<RouletteQueryProcessor>(serviceScope.ServiceProvider);

				return processor.HandleAsync(data).Result;
			});

		var actual = await sut.GetRouletteRecordAsync(promotionId, playerCode, date);

		Assert.IsNull(actual);
	}

	[TestMethod]
	public async Task Roulette()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		await using var web = new TestRouletteSignInApplication(builder => builder
			.ConfigureServices(services => services
				.AddSingleton(fakePromotionServiceClient)));

		var sut = new RouletteSignInClient(fakeMessageSender);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var date = DateOnly.FromDateTime(DateTime.Now);
		var playerCode = "tester";

		fakePromotionServiceClient.GetPromotionMainInfoAsync(Arg.Is(promotionId))
			.Returns(new PromotionMainInfo
			{
				Id = promotionId,
				EventDuring = (DateTime.UtcNow.AddDays(-10D), DateTime.UtcNow.AddDays(10D)),
				IsAutoSent = true,
				Name = "Test Roulette",
				PromotionLimitAmount = null,
				PromotionType = "Roulette"
			});

		fakePromotionServiceClient.GetPromotionRuleAsync<RouletteSettings>(Arg.Is(promotionId))
			.Returns(new RouletteSettings(
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
						false),
					new RouletteItem(
						Guid.NewGuid(),
						"送28元",
						28M,
						0.05M,
						20M,
						20,
						false),
					new RouletteItem(
						Guid.NewGuid(),
						"送88元",
						88M,
						0.04M,
						20M,
						10,
						false),
					new RouletteItem(
						Guid.NewGuid(),
						"送188元",
						188M,
						0.04M,
						20M,
						10,
						false),
					new RouletteItem(
						Guid.NewGuid(),
						"送1888元",
						1888M,
						0.01M,
						1M,
						1,
						false)
				}));

		await using var serviceScope = web.Services.CreateAsyncScope();

		var dbContext = serviceScope.ServiceProvider.GetRequiredService<TgsContext>();

		_ = await dbContext.Players.AddAsync(new Player
		{
			PlayerCode = playerCode,
			BrandId = brandId,
			Username = "Player",
			DefaultWalletCurrencyCode = "USD",
			Password = Array.Empty<byte>(),
			Salt = Array.Empty<byte>(),
			WithdrawPassword = Array.Empty<byte>(),
			WithdrawSalt = Array.Empty<byte>()
		});

		_ = fakeMessageSender.RequestAsync(
			Arg.Is("tgs.roulette.daily"),
			Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<RouletteSignInProcessor>(serviceScope.ServiceProvider);

				return processor.HandleAsync(data).Result;
			});

		var actual = await sut.RouletteAsync(promotionId, playerCode, date);

		Assert.IsNotNull(actual);
		Assert.AreNotEqual(Guid.Empty, actual.Id);
		Assert.IsTrue(actual.ItemIndex >= 0 && actual.ItemIndex < 6, "中獎項目需在範圍內");
	}

	[TestMethod]
	public async Task Already_Roulette_Will_Return_Exists_Data()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();

		await using var web = new TestRouletteSignInApplication();

		var sut = new RouletteSignInClient(fakeMessageSender);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var date = DateOnly.FromDateTime(DateTime.Now);
		var playerCode = "tester";

		await using var serviceScope = web.Services.CreateAsyncScope();

		var dbContext = serviceScope.ServiceProvider.GetRequiredService<TgsContext>();

		_ = await dbContext.Players.AddAsync(new Player
		{
			PlayerCode = playerCode,
			BrandId = brandId,
			Username = "Player",
			DefaultWalletCurrencyCode = "USD",
			Password = Array.Empty<byte>(),
			Salt = Array.Empty<byte>(),
			WithdrawPassword = Array.Empty<byte>(),
			WithdrawSalt = Array.Empty<byte>()
		});

		var existsSignInEvent = new RouletteSignInEvent
		{
			Id = Guid.NewGuid(),
			BrandId = brandId,
			Date = date,
			PlayerCode = playerCode,
			ItemIndex = 2,
			PromotionId = promotionId,
			CreatedOn = DateTime.Now
		};
		_ = await dbContext.RouletteSignInEvents.AddAsync(existsSignInEvent);

		_ = await dbContext.SaveChangesAsync();

		_ = fakeMessageSender.RequestAsync(
			Arg.Is("tgs.roulette.daily"),
			Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<RouletteSignInProcessor>(serviceScope.ServiceProvider);

				return processor.HandleAsync(data).Result;
			});

		var actual = await sut.RouletteAsync(promotionId, playerCode, date);

		Assert.IsNotNull(actual);
		Assert.AreEqual(existsSignInEvent.Id, actual.Id);
		Assert.AreEqual(existsSignInEvent.ItemIndex, actual.ItemIndex);
	}
}
