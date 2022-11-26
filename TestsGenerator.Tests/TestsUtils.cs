using System.Reflection;

namespace TestsGenerator.Tests;

internal static class TestsUtils
{
    public static string ReadProgramUnit(string sourceUnitName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream($"TestsGenerator.Tests.TestData.{sourceUnitName}.txt");
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }

}