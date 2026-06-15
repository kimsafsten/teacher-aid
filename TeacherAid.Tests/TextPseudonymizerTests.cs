using TeacherAid.Api.Services;
using Xunit;

namespace TeacherAid.Tests;

public class TextPseudonymizerTests
{
    [Fact]
    public void FullNameIsReplaced()
    {
        var result = TextPseudonymizer.Pseudonymize("Anna Svensson wrote this.", "Anna Svensson");
        Assert.DoesNotContain("Anna", result);
        Assert.DoesNotContain("Svensson", result);
        Assert.Contains("[Student]", result);
    }

    [Fact]
    public void FirstNameAloneIsReplaced()
    {
        var result = TextPseudonymizer.Pseudonymize("Anna did a great job.", "Anna Svensson");
        Assert.DoesNotContain("Anna", result);
        Assert.Contains("[Student]", result);
    }

    [Fact]
    public void LastNameAloneIsReplaced()
    {
        var result = TextPseudonymizer.Pseudonymize("Svensson submitted the report.", "Anna Svensson");
        Assert.DoesNotContain("Svensson", result);
        Assert.Contains("[Student]", result);
    }

    [Fact]
    public void CaseInsensitiveReplacement()
    {
        var result = TextPseudonymizer.Pseudonymize("ANNA svensson wrote this.", "Anna Svensson");
        Assert.DoesNotContain("ANNA", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("svensson", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NameNotInTextReturnsUnchanged()
    {
        const string text = "This text has no names in it.";
        var result = TextPseudonymizer.Pseudonymize(text, "Anna Svensson");
        Assert.Equal(text, result);
    }

    [Fact]
    public void HyphenatedLastNameIsReplaced()
    {
        // "Li" is only 2 chars so it won't be individually matched — the full name will be
        var result = TextPseudonymizer.Pseudonymize("Anna-Li Eriksson submitted the work.", "Anna-Li Eriksson");
        Assert.DoesNotContain("Eriksson", result);
        Assert.Contains("[Student]", result);
    }

    [Fact]
    public void ShortNamePartIsNotReplacedStandalone()
    {
        // "Li" is ≤2 chars — the Length > 2 guard means it should not be individually replaced
        var result = TextPseudonymizer.Pseudonymize("The li content is fine.", "Li Svensson");
        // "li" should survive (it's too short to match individually)
        Assert.Contains("li", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmptyStudentNameReturnsTextUnchanged()
    {
        const string text = "Some submission content.";
        var result = TextPseudonymizer.Pseudonymize(text, "");
        Assert.Equal(text, result);
    }
}
