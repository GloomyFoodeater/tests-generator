using TestsGenerator.Core.Dataflow;
using static System.Int32;

// General usage message.
if (args.Length == 0)
{
    var message = $"Invalid number of parameters({args.Length}).\n" +
                  "Syntax: <source file>{<source file>} " +
                  "[-d <output path>] " +
                  "[-r max reading tasks] " +
                  "[-p max processing tasks] " +
                  "[-w max writing tasks]\n" +
                  "There must be at least 1 source file.\n" +
                  "If error occured while reading/processing/writing a file, it will be skipped.\n" +
                  "Order of options is significant.";
    Console.Error.WriteLine(message);
    return;
}

// Initialize values of parsed arguments.
var sourceFiles = new List<string> { args[0] };
int maxReadingTasks = 0;
int maxProcessingTasks = 0;
int maxWritingTasks = 0;
var savePath = PipelineConfiguration.DefaultSavePath;

// Get all source files.
var i = 1;
for (; i < args.Length; i++)
{
    if (args[i].StartsWith("-"))
        break;
    sourceFiles.Add(args[i]);
}

// Parse options.
for (; i < args.Length; i++)
{
    if (args[i].StartsWith("-") && i + 1 < args.Length)
        switch (args[i])
        {
            // Directory option.
            case "-d":
                savePath = args[i + 1];
                break;
            // Reading tasks option.
            case "-r":
                TryParse(args[i + 1], out maxReadingTasks);
                break;
            // Processing tasks option.
            case "-p":
                TryParse(args[i + 1], out maxProcessingTasks);
                break;
            // Writing tasks option.
            case "-w":
                TryParse(args[i + 1], out maxWritingTasks);
                break;
            // Write tasks warning.
            default:
                Console.Error.WriteLine($"Unknown option '{args[i + 1]}'.");
                break;
        }
}

// Create directory.
if (!Directory.Exists(savePath))
    Directory.CreateDirectory(savePath);

// Create pipeline.
var configuration = new PipelineConfiguration
{
    MaxReadingTasks = maxReadingTasks > 0 ? maxReadingTasks : PipelineConfiguration.DefaultReadingTasks,
    MaxProcessingTasks = maxProcessingTasks > 0 ? maxProcessingTasks : PipelineConfiguration.DefaultProcessingTasks,
    MaxWritingTasks = maxWritingTasks > 0 ? maxWritingTasks : PipelineConfiguration.DefaultWritingTasks,
    SavePath = savePath
};
var pipeline = new Pipeline(configuration);
Console.WriteLine($"Pipeline configurations: " +
                  $"-d '{configuration.SavePath}' " +
                  $"-r {configuration.MaxReadingTasks} " +
                  $"-p {configuration.MaxProcessingTasks} " +
                  $"-w {configuration.MaxProcessingTasks}.");

// Start processing files.
var generatedAny = pipeline.PerformProcessing(sourceFiles).Result;
Console.WriteLine(generatedAny
    ? $"Tests generated in '{Path.GetFullPath(savePath + "\\")}'."
    : "No tests were generated.");