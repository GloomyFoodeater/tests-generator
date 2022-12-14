using TestsGenerator.Core.Exceptions;
using TestsGenerator.Core.Generation;
using Xunit.Abstractions;
using static TestsGenerator.Tests.TestsUtils;

namespace TestsGenerator.Tests;

public class XUnitGeneratorTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private const string TestsSourcesFolder = "TestsSources";
    private const string TestsExpectedFolder = "TestsExpected";
    
    public XUnitGeneratorTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

    [InlineData("SameClasses")]
    [InlineData("ZeroClasses")]
    [InlineData("InvalidSyntax")]
    [Theory]
    public void InvalidUnit(string sourceUnitName)
    {
        // Arrange
        var generator = new XUnitGenerator();
        var sourceUnit = ReadProgramUnit(TestsSourcesFolder,sourceUnitName);
        
        // Act & assert
        Assert.Throws<TestsGeneratorException>(() => generator.Generate(sourceUnit));
    }

    [Fact]
    public void MissingNamespace()
    {
        // Arrange
        var generator = new XUnitGenerator();
        var sourceUnit = ReadProgramUnit(TestsSourcesFolder, "MissingNamespace");
        
        // Act
        var tests = generator.Generate(sourceUnit).First();
        
        // Assert
        Assert.Equal("MyClass", tests.Name);
        Assert.Contains("namespace Tests;", tests.Content);
        
        _testOutputHelper.WriteLine(tests.Content);
    }

    [Fact]
    public void StaticUsingDirectives()
    {
        // Arrange
        var generator = new XUnitGenerator();
        var sourceUnit = ReadProgramUnit(TestsSourcesFolder, "StaticUsingDirectives");

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
        var sourceUnit = ReadProgramUnit(TestsSourcesFolder, "SingleClass");
        var testsUnit = ReadProgramUnit(TestsExpectedFolder, "SingleClassTests");

        // Act
        var testsInfos = generator.Generate(sourceUnit);
        var array = testsInfos as TestsInfo[] ?? testsInfos.ToArray();

        // Assert
        Assert.Single(array);
        Assert.Equal("MyClass", array[0].Name);
        Assert.Equal(testsUnit, array[0].Content);

        _testOutputHelper.WriteLine(array[0].Content);
    }

    [Fact]
    public void SmartClass()
    {
        // Arrange
        var generator = new XUnitGenerator(TestBodyType.Templated);
        var sourceUnit = ReadProgramUnit(TestsSourcesFolder, "SmartClass");
        var testsUnit = ReadProgramUnit(TestsExpectedFolder,"SmartClassTests");

        // Act
        var testsInfos = generator.Generate(sourceUnit); 
        var array = testsInfos as TestsInfo[] ?? testsInfos.ToArray();

        // Assert
        Assert.Single(array);
        Assert.Equal("MyClass", array[0].Name);
        // Assert.Equal(testsUnit, array[0].Content);

        _testOutputHelper.WriteLine(array[0].Content);
    }

    [Fact]
    public void ManyClasses()
    {
        // Arrange
        var generator = new XUnitGenerator();
        var sourceUnit = ReadProgramUnit(TestsSourcesFolder, "ManyClasses");
        var expectedTests = new TestsInfo[]
        {
            new("MyClass1", ReadProgramUnit(TestsExpectedFolder,"ManyClassesTests1")),
            new("MyClass2", ReadProgramUnit(TestsExpectedFolder,"ManyClassesTests2")),
            new("MyClass3", ReadProgramUnit(TestsExpectedFolder,"ManyClassesTests3"))
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