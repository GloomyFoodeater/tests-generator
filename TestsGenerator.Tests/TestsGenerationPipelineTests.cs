using TestsGenerator.Core.Dataflow;

namespace TestsGenerator.Tests;

public class TestsGenerationPipeline
{
    private readonly string _savePath =
        $"{Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? "."}\\TestsSources";

    [Fact]
    public void ZeroTestsGenerated()
    {
        // Arrange
        var configuration = new PipelineConfiguration { SavePath = _savePath };
        var pipeline = new TestGenerationPipeline(configuration);
        var fileNames = new[]
        {
            $"{_savePath}\\InvalidSyntax.txt",
            $"{_savePath}\\ZeroClasses.txt"
        };

        // Act
        var processedAny = pipeline.Process(fileNames);

        // Assert
        Assert.False(processedAny.Result);
    }

    [Fact]
    public void SomeTestsGenerated()
    {
        // Arrange
        var configuration = new PipelineConfiguration { SavePath = _savePath };
        var pipeline = new TestGenerationPipeline(configuration);
        var fileNames = new[]
        {
            $"{_savePath}\\InvalidSyntax.txt", // Invalid
            $"{_savePath}\\ManyClasses.txt",
            $"{_savePath}\\SingleClass.txt",
            $"{_savePath}\\Not-existent-path.what?", // Does not exist
            $"{_savePath}\\ManyClasses.txt" // Duplicate
        };

        // Act
        var processedAny = pipeline.Process(fileNames).Result;

        // Assert
        Assert.True(processedAny);
    }

    [Fact]
    public void AllTestsGenerated()
    {
        // Arrange
        var configuration = new PipelineConfiguration { SavePath = _savePath };
        var pipeline = new TestGenerationPipeline(configuration);
        var fileNames = new[]
        {
            $"{_savePath}\\ManyClasses.txt",
            $"{_savePath}\\SingleClass.txt",
            $"{_savePath}\\StaticUsingDirectives.txt"
        };

        // Act
        var processedAny = pipeline.Process(fileNames).Result;

        // Assert
        Assert.True(processedAny);
    }
}