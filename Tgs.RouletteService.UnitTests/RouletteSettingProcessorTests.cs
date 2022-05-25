using ExpectedObjects;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tgs.Promotion.Clients.Protos;
using Tgs.RouletteService.MessageQueue;
using Tgs.RouletteServices;

namespace Tgs.RouletteService.UnitTests;

[TestClass]
public class RouletteSettingProcessorTests
{
	[TestMethod]
	public async Task Setting_Request_Will_Response()
	{
		var items = new[] {
			new RouletteItem(
				Guid.NewGuid(),
				"送8元",
				8M,
				1M,
				2M,
				0,
				true),
			new RouletteItem(
				Guid.NewGuid(),
				"送18元",
				18M,
				0.3M,
				10M,
				50,
				false),
			new RouletteItem(
				Guid.NewGuid(),
				"送28元",
				18M,
				0.3M,
				10M,
				50,
				false)
		};
		var settings = new RouletteSettings(items);
		var serializer = new RouletteSerializer();

		var request = new CampaignDetailRequest
		{
			PromotionId = Guid.NewGuid(),
			Settings = serializer.Serialize(settings)
		};

		var sut = new RouletteSettingProcessor(serializer);

		var expectedSettings = new RouletteSettings(new[] {
			items[0] with { WinningProbability = .4M },
			items[1],
			items[2]
		});

		var response = await sut.HandleAsync(request.ToByteArray());

		var actual = CampaignDetailResponse.Parser.ParseFrom(response.Span);

		var actualSettings = serializer.Deserialize<RouletteSettings>(actual.CampaignDetail);

		Assert.IsNotNull(actualSettings);
		expectedSettings
			.ToExpectedObject(ctx => ctx
				.MemberShouldMatch(
					settings => settings.Items,
					items => items.ToExpectedObject(
						mc => mc.IgnoreEqualsOverride()))
				.IgnoreEqualsOverride())
			.ShouldMatch(actualSettings);
		Assert.AreEqual(true, actualSettings.Items.First().IsDefault);
		Assert.AreEqual(1, actualSettings.Items.Count(item => item.IsDefault));
		Assert.AreEqual(1M, actualSettings.Items.Sum(item => item.WinningProbability));
	}

	[TestMethod]
	public async Task Multiple_Default_Will_Set_First_Match_One_Only()
	{
		var items = new[] {
			new RouletteItem(
				Guid.NewGuid(),
				"送8元",
				8M,
				.4M,
				2M,
				0,
				false),
			new RouletteItem(
				Guid.NewGuid(),
				"送18元",
				18M,
				0.3M,
				10M,
				50,
				true),
			new RouletteItem(
				Guid.NewGuid(),
				"送28元",
				18M,
				0.3M,
				10M,
				50,
				true)
		};
		var settings = new RouletteSettings(items);
		var serializer = new RouletteSerializer();

		var request = new CampaignDetailRequest
		{
			PromotionId = Guid.NewGuid(),
			Settings = serializer.Serialize(settings)
		};

		var sut = new RouletteSettingProcessor(serializer);

		var expectedSettings = new RouletteSettings(new[] {
			items[1],
			items[0],
			items[2] with {IsDefault = false}
		});

		var response = await sut.HandleAsync(request.ToByteArray());

		var actual = CampaignDetailResponse.Parser.ParseFrom(response.Span);

		var actualSettings = serializer.Deserialize<RouletteSettings>(actual.CampaignDetail);

		Assert.IsNotNull(actualSettings);
		expectedSettings
			.ToExpectedObject(ctx => ctx
				.MemberShouldMatch(
					settings => settings.Items,
					items => items.ToExpectedObject(
						mc => mc.IgnoreEqualsOverride()))
				.IgnoreEqualsOverride())
			.ShouldMatch(actualSettings);
		Assert.AreEqual(true, actualSettings.Items.First().IsDefault);
		Assert.AreEqual(1, actualSettings.Items.Count(item => item.IsDefault));
		Assert.AreEqual(1M, actualSettings.Items.Sum(item => item.WinningProbability));
	}

	[TestMethod]
	public async Task Without_Default_Will_Set_First_Item_Default()
	{
		var items = new[] {
			new RouletteItem(
				Guid.NewGuid(),
				"送8元",
				8M,
				.4M,
				2M,
				0,
				false),
			new RouletteItem(
				Guid.NewGuid(),
				"送18元",
				18M,
				0.3M,
				10M,
				50,
				false),
			new RouletteItem(
				Guid.NewGuid(),
				"送28元",
				18M,
				0.3M,
				10M,
				50,
				false)
		};
		var settings = new RouletteSettings(items);
		var serializer = new RouletteSerializer();

		var request = new CampaignDetailRequest
		{
			PromotionId = Guid.NewGuid(),
			Settings = serializer.Serialize(settings)
		};

		var sut = new RouletteSettingProcessor(serializer);

		var expectedSettings = new RouletteSettings(new[] {
			items[0] with{IsDefault = true},
			items[1],
			items[2]
		});

		var response = await sut.HandleAsync(request.ToByteArray());

		var actual = CampaignDetailResponse.Parser.ParseFrom(response.Span);

		var actualSettings = serializer.Deserialize<RouletteSettings>(actual.CampaignDetail);

		Assert.IsNotNull(actualSettings);
		expectedSettings
			.ToExpectedObject(ctx => ctx
				.MemberShouldMatch(
					settings => settings.Items,
					items => items.ToExpectedObject(
						mc => mc.IgnoreEqualsOverride()))
				.IgnoreEqualsOverride())
			.ShouldMatch(actualSettings);
		Assert.AreEqual(true, actualSettings.Items.First().IsDefault);
		Assert.AreEqual(1, actualSettings.Items.Count(item => item.IsDefault));
		Assert.AreEqual(1M, actualSettings.Items.Sum(item => item.WinningProbability));
	}

	[TestMethod]
	public async Task All_Item_Without_Default_WinningProbability_Summary_Must_Less_Than_Or_Equal_To_One()
	{
		var items = new[] {
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
				0.3M,
				10M,
				50,
				false),
			new RouletteItem(
				Guid.NewGuid(),
				"送28元",
				18M,
				0.8M,
				10M,
				50,
				false)
		};
		var settings = new RouletteSettings(items);
		var serializer = new RouletteSerializer();

		var request = new CampaignDetailRequest
		{
			PromotionId = Guid.NewGuid(),
			Settings = serializer.Serialize(settings)
		};

		var sut = new RouletteSettingProcessor(serializer);


		await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await sut.HandleAsync(request.ToByteArray()));
	}

	[TestMethod]
	public async Task Must_Have_One_More_Roulette_Items()
	{
		var settings = new RouletteSettings(Array.Empty<RouletteItem>());
		var serializer = new RouletteSerializer();

		var request = new CampaignDetailRequest
		{
			PromotionId = Guid.NewGuid(),
			Settings = serializer.Serialize(settings)
		};

		var sut = new RouletteSettingProcessor(serializer);

		await Assert.ThrowsExceptionAsync<InvalidOperationException>(
			async () => await sut.HandleAsync(request.ToByteArray()));
	}
}
