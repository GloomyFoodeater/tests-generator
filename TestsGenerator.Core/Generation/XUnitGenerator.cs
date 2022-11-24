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
        var tree = CSharpSyntaxTree.ParseText(source);
        var sourceUnit = tree.GetCompilationUnitRoot();
        var sourceClasses = sourceUnit
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(@class => @class.Modifiers.Any(SyntaxKind.PublicKeyword))
            .ToArray();
        var sourceUsingDirectives = sourceUnit
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .ToArray();

        if (!sourceClasses.Any())
            throw new ClassCountException("Source does not contain any classes");

        var testsInfos = new List<TestsInfo>();
        foreach (var sourceClass in sourceClasses)
        {
            // Generate methods
            var testsMethods = GenerateTestsMethods(sourceClass);

            // Generate class
            var testsClass = GenerateTestsClass(testsMethods, sourceClass);

            // Generate namespace
            var sourceNamespaceName = ExtractSourceNamespaceFullName(sourceClass);
            var testsNamespace = GenerateTestsNamespace(testsClass, sourceNamespaceName);

            // Generate use directives
            var testsUsingDirectives = GenerateTestsUsingDirectives(sourceUsingDirectives, sourceNamespaceName);

            // Generate unit
            var testsUnit = GenerateTestsUnit(testsUsingDirectives, testsNamespace);

            var testsName = sourceClass.Identifier.Text + "Tests";
            var testsContent = testsUnit.NormalizeWhitespace().ToFullString();
            testsInfos.Add(new(testsName, testsContent));
        }

        return testsInfos;
    }
}