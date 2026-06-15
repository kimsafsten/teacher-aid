using TeacherAid.Api.Services;
using Xunit;

namespace TeacherAid.Tests;

public class TextChunkerTests
{
    [Fact]
    public void ShortTextReturnsSingleChunk()
    {
        var chunks = TextChunker.Chunk("Hello world.", chunkSize: 500);
        Assert.Single(chunks);
        Assert.Equal("Hello world.", chunks[0]);
    }

    [Fact]
    public void LongTextSplitsOnNewline()
    {
        // Two paragraphs that together exceed the chunk size should be split into two chunks
        var paragraph = new string('x', 300);
        var text = paragraph + "\n" + paragraph;
        var chunks = TextChunker.Chunk(text, chunkSize: 500);
        Assert.Equal(2, chunks.Count);
    }

    [Fact]
    public void EmptyStringReturnsEmptyList()
    {
        var chunks = TextChunker.Chunk(string.Empty);
        Assert.Empty(chunks);
    }

    [Fact]
    public void WhitespaceOnlyReturnsEmptyList()
    {
        var chunks = TextChunker.Chunk("   \n\n  ");
        Assert.Empty(chunks);
    }

    [Fact]
    public void OversizedSingleParagraphProducesOneChunkLargerThanLimit()
    {
        // A paragraph that exceeds chunkSize with no newlines must be kept intact
        var big = new string('x', 800);
        var chunks = TextChunker.Chunk(big, chunkSize: 500);
        Assert.Single(chunks);
        Assert.True(chunks[0].Length > 500);
    }
}
