using TestsGenerator.Core.Generation;

namespace TestsGenerator.Core.Dataflow;

public record PipelineConfiguration
{
    public const int DefaultReadingTasks = 5;
    public const int DefaultProcessingTasks = 5;
    public const int DefaultWritingTasks = 5;
    public const string DefaultSavePath = ".";
    public static readonly XUnitGenerator DefaultGenerator = new(); // XUnitGenerator is immutable

    public int MaxReadingTasks { get; init; } = DefaultReadingTasks;
    public int MaxProcessingTasks { get; init; } = DefaultProcessingTasks;
    public int MaxWritingTasks { get; init; } = DefaultWritingTasks;
    public string SavePath { get; init; } = DefaultSavePath;
    public ITestGenerator TestsGenerator { get; init; } = DefaultGenerator;
}