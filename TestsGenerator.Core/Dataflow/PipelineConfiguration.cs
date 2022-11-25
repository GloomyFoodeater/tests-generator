using TestsGenerator.Core.Generation;

namespace TestsGenerator.Core.Dataflow;

public record PipelineConfiguration
{
    private const int DefaultReadingTasks = 5;
    private const int DefaultProcessingTasks = 5;
    private const int DefaultWritingTasks = 5;
    private const string DefaultSavePath = ".";
    private static readonly XUnitGenerator DefaultGenerator = new(); // XUnitGenerator is immutable

    public int MaxReadingTasks { get; init; } = DefaultReadingTasks;
    public int MaxProcessingTasks { get; init; } = DefaultProcessingTasks;
    public int MaxWritingTasks { get; init; } = DefaultWritingTasks;
    public string SavePath { get; init; } = DefaultSavePath;
    public ITestGenerator TestsGenerator { get; init; } = DefaultGenerator;
}