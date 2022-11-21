using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestsGenerator.Core.Generation;

[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
public class Generator : ITestGenerator
{
    public TestsInfo Generate(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);

        // Extract all essential syntax members from source unit
        var sourceUnit = tree.GetCompilationUnitRoot();
        var sourceUseDirectives = sourceUnit.DescendantNodes().OfType<UsingDirectiveSyntax>();
        var sourceClass = ExtractSourceClass(sourceUnit);
        var sourceNamespace = sourceClass.Parent as BaseNamespaceDeclarationSyntax;
        var sourceMethods = ExtractSourceMethods(sourceClass);

        // Generate unit test
        var testsMethods = GenerateTestsMethods(sourceMethods);
        var testsClass = GenerateTestsClass(testsMethods, sourceClass);
        var testsNamespace = GenerateTestsNamespace(testsClass, sourceNamespace!);
        var testsUseDirectives = GenerateTestsUsingDirectives(sourceNamespace!, sourceUseDirectives);
        var testsUnit = GenerateTestsUnit(testsUseDirectives, testsNamespace);

        // Return test with identical name as source class
        return new TestsInfo(sourceClass.Identifier.Text + "Tests", testsUnit.NormalizeWhitespace().ToFullString());
    }

    // Methods to get syntax nodes from source unit
    private static ClassDeclarationSyntax ExtractSourceClass(CompilationUnitSyntax root)
    {
        throw new NotImplementedException();
    }

    private static IEnumerable<MethodDeclarationSyntax> ExtractSourceMethods(ClassDeclarationSyntax root)
    {
        throw new NotImplementedException();
    }

    // Methods to generate syntax nodes into test unit
    private static IEnumerable<MethodDeclarationSyntax> GenerateTestsMethods(
        IEnumerable<MethodDeclarationSyntax> sourceMethods)
    {
        throw new NotImplementedException();
    }

    private static ClassDeclarationSyntax GenerateTestsClass(
        IEnumerable<MethodDeclarationSyntax> testsMethods,
        ClassDeclarationSyntax sourceClass)
    {
        throw new NotImplementedException();
    }

    private static FileScopedNamespaceDeclarationSyntax GenerateTestsNamespace(
        ClassDeclarationSyntax testsClass,
        BaseNamespaceDeclarationSyntax sourceNamespace)
    {
        throw new NotImplementedException();
    }

    private static IEnumerable<UsingDirectiveSyntax> GenerateTestsUsingDirectives(
        BaseNamespaceDeclarationSyntax sourceNamespace,
        IEnumerable<UsingDirectiveSyntax> sourceUseDirectives)
    {
        throw new NotImplementedException();
    }

    private static CompilationUnitSyntax GenerateTestsUnit(
        IEnumerable<UsingDirectiveSyntax> testsUseDirectives,
        FileScopedNamespaceDeclarationSyntax testsNamespace)
    {
        throw new NotImplementedException();
    }
}