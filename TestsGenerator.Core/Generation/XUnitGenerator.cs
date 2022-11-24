using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestsGenerator.Core.Exceptions;
using static TestsGenerator.Core.Generation.SyntaxUtils;

namespace TestsGenerator.Core.Generation;

public class XUnitGenerator : ITestGenerator
{
    public IEnumerable<TestsInfo> Generate(string source)
    {
        // Extract members from source unit.
        var sourceUnit = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        
        var hasErrors = sourceUnit
            .GetDiagnostics()
            .Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (hasErrors)
            throw new SourceSyntaxException($"Source unit has syntax errors");

        var sourceClasses = sourceUnit
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(@class => @class.Modifiers.Any(SyntaxKind.PublicKeyword)) // Only public classes
            .ToArray();

        var sourceUsingDirectives = sourceUnit
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>();

        if (!sourceClasses.Any())
            throw new SourceSyntaxException("Source unit does not contain any classes");

        // Generate sequentially methods, classes, namespaces, using directives and units
        // for each source class and append new test created with them in result collection.
        return from sourceClass in sourceClasses
            let sourceNamespaceName = ExtractSourceNamespaceFullName(sourceClass)
            let testsMethods = GenerateTestsMethods(sourceClass, _ => GetFailedTestBlock())
            let testsClass = GenerateTestsClass(testsMethods, sourceClass)
            let testsNamespace = GenerateTestsNamespace(testsClass, sourceNamespaceName)
            let testsUsingDirectives = GenerateTestsUsingDirectives(sourceUsingDirectives, sourceNamespaceName)
            let testsUnit = GenerateTestsUnit(testsUsingDirectives, testsNamespace)
            select new TestsInfo(
                sourceClass.Identifier.Text,
                testsUnit.NormalizeWhitespace().ToFullString());
    }
}