using TestsGenerator.Core.Exceptions;
using TestsGenerator.Core.Generation;
using Xunit.Abstractions;
using static TestsGenerator.Tests.TestsUtils;

namespace TestsGenerator.Tests;

public class XUnitGeneratorTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XUnitGeneratorTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [InlineData("SameClasses")]
    [InlineData("ZeroClasses")]
    [InlineData("InvalidSyntax")]
    [Theory]
    public void InvalidUnit(string sourceUnitName)
    {
        var generator = new XUnitGenerator();
        var sourceUnit = ReadProgramUnit(sourceUnitName);
        
        Assert.Throws<TestsGeneratorException>(() => generator.Generate(sourceUnit));
    }

    [Fact]
    public void MissingNamespace()
    {
        var generator = new XUnitGenerator();
        
        var sourceUnit = ReadProgramUnit("MissingNamespace");
        Assert.Throws<TestsGeneratorException>(() => generator.Generate(sourceUnit));
    }

    [Fact]
    public void StaticUsingDirectives()
    {
        // Arrange
        var generator = new XUnitGenerator();
        var sourceUnit = ReadProgramUnit("StaticUsingDirectives");

        // Act
        var testsInfos = generator.Generate(sourceUnit);
        var array = testsInfos as TestsInfo[] ?? testsInfos.ToArray();

        // Assert
        Assert.DoesNotContain("using static", array.First().Content);

        _testOutputHelper.WriteLine(array.First().Content);
    }

    [Fact]
    public void SingleClass()
    {
        // Arrange
        var generator = new XUnitGenerator();
        var sourceUnit = ReadProgramUnit("SingleClass");
        var testsUnit = ReadProgramUnit("SingleClassTests");

        // Act
        var testsInfos = generator.Generate(sourceUnit);
        var array = testsInfos as TestsInfo[] ?? testsInfos.ToArray();

        // Assert
        Assert.Single(array);
        Assert.Equal("MyNamespace.MyClass", array.First().Name);
        Assert.Equal(testsUnit, array.First().Content);

        _testOutputHelper.WriteLine(array.First().Content);
    }

    [Fact]
    public void SmartClass()
    {
        // Arrange
        var generator = new XUnitGenerator(TestBodyType.Templated);
        var sourceUnit = ReadProgramUnit("SmartClass");
        var testsUnit = ReadProgramUnit("SmartClassTests");

        // Act
        var testsInfos = generator.Generate(sourceUnit);
        var array = testsInfos as TestsInfo[] ?? testsInfos.ToArray();

        // Assert
        Assert.Single(array);
        Assert.Equal("MyNamespace.MyClass", array.First().Name);
        Assert.Equal(testsUnit, array.First().Content);

        _testOutputHelper.WriteLine(array.First().Content);
    }

    [Fact]
    public void ManyClasses()
    {
        // Arrange
        var generator = new XUnitGenerator();
        var sourceUnit = ReadProgramUnit("ManyClasses");
        var expectedTests = new TestsInfo[]
        {
            new("MyNamespace.MyClass1", ReadProgramUnit("ManyClassesTests1")),
            new("MyNamespace.MyClass2", ReadProgramUnit("ManyClassesTests2")),
            new("MyNamespace.MyClass3", ReadProgramUnit("ManyClassesTests3"))
        };

        // Act
        var actualTests = generator
            .Generate(sourceUnit)
            .OrderBy(testsInfo => testsInfo.Name); // Sort to disregard processing order

        // Assert
        Assert.All(expectedTests.Zip(actualTests), tuple =>
        {
            // Both tests name and tests content must match.
            var (expected, actual) = tuple;
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Content, actual.Content);

            _testOutputHelper.WriteLine(actual.Content);
        });
    }
}