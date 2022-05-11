using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.MessageQueue;
using Tgs.RouletteService.MessageQueue;
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
