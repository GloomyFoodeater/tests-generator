namespace TestsGenerator.Core.Generation;

public interface ITestGenerator
{
    public IEnumerable<TestsInfo> Generate(string source);
}