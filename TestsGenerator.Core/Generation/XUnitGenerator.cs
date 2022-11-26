using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestsGenerator.Core.Exceptions;
using static TestsGenerator.Core.Generation.SyntaxUtils;

namespace TestsGenerator.Core.Generation;

public class XUnitGenerator : ITestGenerator
{
    private readonly TestBodyType _bodyType;

    public XUnitGenerator(TestBodyType bodyType = TestBodyType.Empty) => _bodyType = bodyType;

    public IEnumerable<TestsInfo> Generate(string source)
    {
        // Extract members from source unit.
        var unit = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();

        var hasErrors = unit
            .GetDiagnostics()
            .Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (hasErrors)
            throw new TestsGeneratorException($"Source unit has syntax errors");

        var classes = unit
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(@class => @class.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                             @class.Parent is BaseNamespaceDeclarationSyntax
                                 or CompilationUnitSyntax) // Only public outer classes
            .ToArray(); // Multiple enumerations below => immediate execution

        // Detect classes with same names.
        var hasDuplicates = classes.Length != classes.DistinctBy(@class => @class.Identifier.Text).Count();
        if (hasDuplicates)
            throw new TestsGeneratorException("Source unit contain classes with same names in same namespace");

        var usingDirectives = unit
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(usingDirective => usingDirective.StaticKeyword.IsMissing);

        if (!classes.Any())
            throw new TestsGeneratorException("Source unit does not contain any classes");

        Func<MethodDeclarationSyntax, BlockSyntax> generateBody = _bodyType switch
        {
            TestBodyType.Empty => _ => GetFailedTestBlock(),
            TestBodyType.Templated => GetTemplateTestBlock,
            _ => throw new TestsGeneratorException("Enumeration BodyType was invalid.")
        };

        // Generate sequentially methods, classes, namespaces, using directives and units
        // for each source class and create tests from units.
        return (from @class in classes
                let namespacesNames = GetFullNamespacesNamesFrom(@class)
                let testsMethods = GenerateTestsMethods(@class, generateBody)
                let testsClass = GenerateTestsClass(testsMethods, @class)
                let testsNamespace = GenerateTestsNamespace(testsClass, namespacesNames)
                let testsUsingDirectives = GenerateTestsUsingDirectives(usingDirectives, namespacesNames)
                let testsUnit = GenerateTestsUnit(testsUsingDirectives, testsNamespace)
                select new TestsInfo(
                    @class.Identifier.Text,
                    testsUnit.NormalizeWhitespace().ToFullString()))
            .ToArray(); // Immediate execution
    }
}