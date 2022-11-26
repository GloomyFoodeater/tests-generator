using System.Reflection;
using TestsGenerator.Core.Exceptions;
using TestsGenerator.Core.Generation;

namespace TestsGenerator.Tests;

public class XUnitGeneratorTests
{
    private readonly XUnitGenerator _generator = new();

    private static string ReadProgramUnit(string sourceUnitName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream($"TestsGenerator.Tests.TestData.{sourceUnitName}.txt");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

    [InlineData("SameClasses")]
    [InlineData("ZeroClasses")]
    [InlineData("InvalidSyntax")]
    [Theory]
    public void InvalidUnit(string sourceUnitName)
    {
        var sourceUnit = ReadProgramUnit(sourceUnitName);
        Assert.Throws<TestsGeneratorException>(() => _generator.Generate(sourceUnit));
    }

    [Fact]
    public void MissingNamespace()
    {
        var sourceUnit = ReadProgramUnit("MissingNamespace");
        Assert.Throws<TestsGeneratorException>(() => _generator.Generate(sourceUnit));
    }

    [Fact]
    public void StaticUsingDirectives()
    {
        // Arrange
        var sourceUnit = ReadProgramUnit("StaticUsingDirectives");

        // Act
        var testsInfos = _generator.Generate(sourceUnit);

        // Assert
        Assert.DoesNotContain("using static", testsInfos.First().Content);
    }

    [Fact]
    public void SingleClass()
    {
        // Arrange
        var sourceUnit = ReadProgramUnit("SingleClass");
        var testsUnit = ReadProgramUnit("SingleClassTests");

        // Act
        var testsInfos = _generator.Generate(sourceUnit).ToArray();

        // Assert
        Assert.Single(testsInfos);
        Assert.Equal("MyNamespace.MyClass", testsInfos[0].Name);
        Assert.Equal(testsUnit, testsInfos[0].Content);
    }

    [Fact]
    public void ManyClasses()
    {
        // Arrange
        var sourceUnit = ReadProgramUnit("ManyClasses");
        var expectedTests = new TestsInfo[]
        {
            new("MyNamespace.MyClass1", ReadProgramUnit("ManyClassesTests1")),
            new("MyNamespace.MyClass2", ReadProgramUnit("ManyClassesTests2")),
            new("MyNamespace.MyClass3", ReadProgramUnit("ManyClassesTests3"))
        };

        // Act
        var actualTests = _generator
            .Generate(sourceUnit)
            .OrderBy(testsInfo => testsInfo.Name); // Sort to disregard processing order

        // Assert
        Assert.All(expectedTests.Zip(actualTests), tuple =>
        {
            // Both tests name and tests content must match.
            var (expected, actual) = tuple;
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Content, actual.Content);
        });
    }
}