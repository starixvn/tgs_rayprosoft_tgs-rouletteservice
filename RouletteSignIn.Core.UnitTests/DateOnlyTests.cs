using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RouletteSignIn.Core.UnitTests;
[TestClass]
public class DateOnlyTests
{
	[TestMethod]
	public void DateOnly_Format()
	{
		var date = DateOnly.FromDateTime(new DateTime(2014, 1, 5));

		var expected = "20140105";

		var actual = date.ToString("yyyyMMdd");

		Assert.AreEqual(expected, actual);
	}
}