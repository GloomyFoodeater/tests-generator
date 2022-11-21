namespace TestsGenerator.Core.Generation;

public interface ITestGenerator
{
    public TestsInfo Generate(string source);
}