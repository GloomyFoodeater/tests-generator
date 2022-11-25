using System.Threading.Tasks.Dataflow;
using TestsGenerator.Core.Generation;

namespace TestsGenerator.Core.Dataflow;

public class Pipeline
{
    private bool _generatedAny = false;

    private readonly PipelineConfiguration _configuration;

    public Pipeline(PipelineConfiguration configuration) => _configuration = configuration;

    public async Task<bool> PerformProcessing(IEnumerable<string> filePaths)
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
        return _generatedAny;
    }

    private async Task<string> ReadContent(string filePath)
    {
        // Generator must return empty collection from empty string.
        var result = string.Empty;
        try
        {
            using var streamReader = new StreamReader(filePath);
            result = await streamReader.ReadToEndAsync();
        }
        catch
        {
            // Ignore.
        }

        return result;
    }

    private IEnumerable<TestsInfo> GenerateTests(string fileContent)
    {
        var tests = Array.Empty<TestsInfo>();
        try
        {
            tests = _configuration.TestsGenerator // Generator must be thread safe
                .Generate(fileContent)
                .ToArray(); // Immediate execution
        }
        catch
        {
            // Ignore.
        }

        return tests;
    }

    private async Task WriteTests(TestsInfo testsInfo)
    {
        try
        {
            var testsPath = $"{_configuration.SavePath}\\{testsInfo.Name}Tests.cs";
            await using var streamWriter = new StreamWriter(testsPath);
            await streamWriter.WriteAsync(testsInfo.Content);
            _generatedAny = true;
        }
        catch
        {
            // Ignore.
        }
    }
}