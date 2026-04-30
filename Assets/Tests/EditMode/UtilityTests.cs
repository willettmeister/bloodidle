using NUnit.Framework;

public class UtilityTests
{
    // FormatNumber

    [Test]
    public void FormatNumber_Small_FloorsFraction()
        => Assert.AreEqual("42", GameManager.FormatNumber(42.9));

    [Test]
    public void FormatNumber_Zero()
        => Assert.AreEqual("0", GameManager.FormatNumber(0));

    [Test]
    public void FormatNumber_ExactThousand_ReturnsK()
        => Assert.AreEqual("1.0K", GameManager.FormatNumber(1_000));

    [Test]
    public void FormatNumber_Thousands()
        => Assert.AreEqual("1.5K", GameManager.FormatNumber(1_500));

    [Test]
    public void FormatNumber_Millions()
        => Assert.AreEqual("2.3M", GameManager.FormatNumber(2_300_000));

    [Test]
    public void FormatNumber_Billions()
        => Assert.AreEqual("1.0B", GameManager.FormatNumber(1_000_000_000));

    // FormatHP

    [Test]
    public void FormatHP_WholeNumber()
        => Assert.AreEqual("50", GameManager.FormatHP(50f));

    [Test]
    public void FormatHP_FractionCeilsUp()
        => Assert.AreEqual("49", GameManager.FormatHP(48.1f));

    [Test]
    public void FormatHP_Zero()
        => Assert.AreEqual("0", GameManager.FormatHP(0f));

    [Test]
    public void FormatHP_SmallFractionRoundsToOne()
        => Assert.AreEqual("1", GameManager.FormatHP(0.1f));
}
