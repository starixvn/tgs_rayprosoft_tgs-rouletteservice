using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.RouletteServices;
using Tgs.RouletteServices.ApplicationServices;

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
	public async Task Player_Roulette()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new RouletteCommandService(fakeTgsRepository);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1",
			DateOnly.FromDateTime(DateTime.Now));

		await sut.ExecuteAsync(command);

		_ = fakeTgsRepository.Received(1)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Is(command.Date),
				Arg.Any<int>());
	}

	[TestMethod]
	public async Task If_Already_Roulette_Should_Not_Roulette_Again()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new RouletteCommandService(fakeTgsRepository);

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
}
