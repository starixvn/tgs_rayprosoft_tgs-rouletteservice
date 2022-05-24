using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tgs.RouletteServices;

namespace RouletteSignIn.Core.UnitTests;
[TestClass]
public class ShuffleTests
{
	[TestMethod]
	public void Shuffle_By_RouletteItems()
	{
		var sut = new ShuffleService();

		var rouletteItems = new[]{
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
				0.03M,
				20M,
				10,
				false),
			new RouletteItem(
				Guid.NewGuid(),
				"送188元",
				188M,
				0.02M,
				20M,
				10,
				false),
			new RouletteItem(
				Guid.NewGuid(),
				"送1888元",
				1888M,
				0M,
				1M,
				1,
				false)
		};

		var deck = sut.Shuffle(Guid.NewGuid(), rouletteItems);

		Assert.AreEqual(100, deck.Length);
	}
}