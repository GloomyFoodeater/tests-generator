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
        var unit = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        var hasErrors = unit
            .GetDiagnostics()
            .Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (hasErrors)
            throw new SourceSyntaxException($"Source unit has syntax errors");

        var classes = unit
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(@class => @class.Modifiers.Any(SyntaxKind.PublicKeyword)) // Only public classes
            .ToArray(); // Multiple enumerations below => immediate execution

        var usingDirectives = unit
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.StaticKeyword.IsMissing);

        if (!classes.Any())
            throw new SourceSyntaxException("Source unit does not contain any classes");

        // Generate sequentially methods, classes, namespaces, using directives and units
        // for each source class and create tests from units.
        return from @class in classes
            let namespacesNames = GetFullNamespacesNamesFrom(@class)
            let testsMethods = GenerateTestsMethods(@class, _ => GetFailedTestBlock())
            let testsClass = GenerateTestsClass(testsMethods, @class)
            let testsNamespace = GenerateTestsNamespace(testsClass, namespacesNames)
            let testsUsingDirectives = GenerateTestsUsingDirectives(usingDirectives, namespacesNames)
            let testsUnit = GenerateTestsUnit(testsUsingDirectives, testsNamespace)
            select new TestsInfo(
                $"{namespacesNames[^1]}.{@class.Identifier}",
                testsUnit.NormalizeWhitespace().ToFullString());
    }
}