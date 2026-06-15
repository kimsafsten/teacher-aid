using TeacherAid.Api.Services;
using Xunit;

namespace TeacherAid.Tests;

public class SubmissionFileNameParserTests
{
    [Fact]
    public void StandardFormatParsesCorrectly()
    {
        var (name, courseId) = SubmissionFileNameParser.Parse("Anna_Svensson_SYS25D_Uppgift1.pdf");
        Assert.Equal("Anna Svensson", name);
        Assert.Equal("SYS25D", courseId);
    }

    [Fact]
    public void HyphenatedLastNameParsesCorrectly()
    {
        var (name, courseId) = SubmissionFileNameParser.Parse("Anna-Li_Eriksson_PRJ23A_1.docx");
        Assert.Equal("Anna-Li Eriksson", name);
        Assert.Equal("PRJ23A", courseId);
    }

    [Fact]
    public void MissingCourseIdDefaultsToUnknown()
    {
        var (name, courseId) = SubmissionFileNameParser.Parse("Anna_Svensson.pdf");
        Assert.Equal("Anna Svensson", name);
        Assert.Equal("okänd", courseId);
    }

    [Fact]
    public void OnlyOneSegmentReturnsAsName()
    {
        var (name, courseId) = SubmissionFileNameParser.Parse("Anna.pdf");
        Assert.Equal("Anna", name);
        Assert.Equal("okänd", courseId);
    }

    [Fact]
    public void ParseCourseIdExtractsSecondSegment()
    {
        var courseId = SubmissionFileNameParser.ParseCourseId("Uppgiftsbeskrivning_SYS25D.txt");
        Assert.Equal("SYS25D", courseId);
    }

    [Fact]
    public void ParseCourseIdReturnsNullWhenNoUnderscore()
    {
        var courseId = SubmissionFileNameParser.ParseCourseId("README.txt");
        Assert.Null(courseId);
    }
}
