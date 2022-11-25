using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using TestsGenerator.Core.Generation;

namespace TestsGenerator.Core.Dataflow;

public class Pipeline
{
    private readonly PipelineConfiguration _configuration;

    public Pipeline(PipelineConfiguration configuration) => _configuration = configuration;

    public async Task PerformProcessing(IEnumerable<string> filePaths)
    {
        // Create pipeline blocks.
        var readingBlock = new TransformBlock<string, string>(
            ReadContent,
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _configuration.MaxReadingTasks });

        var processingBlock = new TransformManyBlock<string, TestsInfo>(
            GenerateTests,
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _configuration.MaxProcessingTasks });

        var writingBlock = new ActionBlock<TestsInfo>(
            WriteTests,
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _configuration.MaxWritingTasks });

        // Connect pipeline blocks into one network.
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        readingBlock.LinkTo(processingBlock, linkOptions);
        processingBlock.LinkTo(writingBlock, linkOptions);

        // Post files into network queue.
        foreach (var filePath in filePaths)
            readingBlock.Post(filePath);

        // Wait last block completion.
        readingBlock.Complete();
        await writingBlock.Completion;
    }

    private async Task<string> ReadContent(string filePath)
    {
        using var streamReader = new StreamReader(filePath);
        var result = await streamReader.ReadToEndAsync();
        return result;
    }

    private IEnumerable<TestsInfo> GenerateTests(string fileContent)
    {
        return _configuration.TestsGenerator // Generator must be thread safe
            .Generate(fileContent)
            .ToArray(); // Immediate execution
    }

    private async Task WriteTests(TestsInfo testsInfo)
    {
        var testsPath = $"{_configuration.SavePath}\\{testsInfo.Name}Tests.cs";
        await using var streamWriter = new StreamWriter(testsPath);
        await streamWriter.WriteAsync(testsInfo.Content);
    }
}