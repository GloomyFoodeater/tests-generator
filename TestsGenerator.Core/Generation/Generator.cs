using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TestsGenerator.Core.Exceptions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace TestsGenerator.Core.Generation;

[SuppressMessage("ReSharper", "SuggestBaseTypeForParameter")]
public class Generator : ITestGenerator
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
            var sourceUsingDirectives = sourceUnit.DescendantNodes().OfType<UsingDirectiveSyntax>();
            var testsUsingDirectives = GenerateTestsUsingDirectives(sourceUsingDirectives, sourceNamespaceName);

            // Generate unit
            var testsUnit = GenerateTestsUnit(testsUsingDirectives, testsNamespace);

            var testsName = sourceClass.Identifier.Text + "Tests";
            var testsContent = testsUnit.NormalizeWhitespace().ToFullString();
            testsInfos.Add(new(testsName, testsContent));
        }

        return testsInfos;
    }

    private SyntaxList<UsingDirectiveSyntax> GenerateTestsUsingDirectives(
        IEnumerable<UsingDirectiveSyntax> sourceUsingDirectives, NameSyntax sourceNamespaceName)
    {
        // Outer namespaces first
        var sourceNamespaceNames = new Stack<NameSyntax>();

        // Get all nested namespaces full names by qualified name syntax 
        var current = sourceNamespaceName;
        while (current is QualifiedNameSyntax complexName)
        {
            sourceNamespaceNames.Push(current);
            current = complexName.Left;
        }

        sourceNamespaceNames.Push(current);

        // Return list of namespaces
        return new SyntaxList<UsingDirectiveSyntax>()
            .Add(UsingDirective(ParseName("System")))
            .Add(UsingDirective(ParseName("System.Collections.Generic")))
            .Add(UsingDirective(ParseName("System.Linq")))
            .Add(UsingDirective(ParseName("System.Text")))
            .Add(UsingDirective(ParseName("XUnit")))
            .AddRange(sourceUsingDirectives) // Namespaces from source unit & nested namespaces
            .AddRange(sourceNamespaceNames.Select(UsingDirective)); // Outer & own namespaces of source class;
    }

    private FileScopedNamespaceDeclarationSyntax GenerateTestsNamespace(ClassDeclarationSyntax testsClass,
        NameSyntax sourceNamespaceName)
    {
        // Append test postfix to found name
        var name = QualifiedName(sourceNamespaceName, IdentifierName("Tests"));

        return FileScopedNamespaceDeclaration(name).AddMembers(testsClass);
    }

    private NameSyntax ExtractSourceNamespaceFullName(ClassDeclarationSyntax sourceClass)
    {
        NameSyntax GetFullNameFrom(NamespaceDeclarationSyntax @namespace)
        {
            // Collect identifiers of namespaces in reverse order
            var stack = new Stack<string>();
            for (var current = @namespace; current != null; current = current.Parent as NamespaceDeclarationSyntax)
                stack.Push(current.Name.ToString());

            return ParseName(string.Join(".", stack));
        }

        // Return full name of source class namespace
        return sourceClass.Parent switch
        {
            // Class can be either in =1 file scoped namespace or in >=1 nested namespaces
            FileScopedNamespaceDeclarationSyntax fileNamespace => fileNamespace.Name,
            NamespaceDeclarationSyntax @namespace => GetFullNameFrom(@namespace),
            _ => throw new SyntaxException("Source class was not in namespace")
        };
    }

    private MethodDeclarationSyntax[] GenerateTestsMethods(ClassDeclarationSyntax sourceClass)
    {
        return (from sourceMember in sourceClass.Members
                let sourceMethod = sourceMember as MethodDeclarationSyntax
                where sourceMethod != null && sourceMethod.Modifiers.Any(SyntaxKind.PublicKeyword)
                let attributes = SingletonList(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                IdentifierName("Fact")))))
                let modifiers = TokenList(Token(SyntaxKind.PublicKeyword))
                let identifier = Identifier(sourceMethod.Identifier.Text + "Test")
                let returnType = PredefinedType(Token(SyntaxKind.VoidKeyword))
                let body = Block(
                    ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(
                                    Identifier("Assert")),
                                IdentifierName("Fail")),
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            Literal("autogenerated"))))))))
                select MethodDeclaration(returnType, identifier)
                    .WithModifiers(modifiers)
                    .WithAttributeLists(attributes)
                    .WithBody(body))
            .ToArray();
    }

    [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
    private ClassDeclarationSyntax GenerateTestsClass(
        MethodDeclarationSyntax[] testsMethods,
        ClassDeclarationSyntax sourceClass)
    {
        var name = sourceClass.Identifier.Text + "Tests";
        var modifiers = TokenList(Token(SyntaxKind.PublicKeyword));
        return ClassDeclaration(name)
            .WithModifiers(modifiers)
            .AddMembers(testsMethods);
    }

    private static FileScopedNamespaceDeclarationSyntax GenerateTestsNamespace(
        ClassDeclarationSyntax testsClass,
        ClassDeclarationSyntax sourceClass)
    {
        NameSyntax GetFullNameFrom(NamespaceDeclarationSyntax @namespace)
        {
            // Collect identifiers of namespaces in reverse order
            var stack = new Stack<string>();
            for (var current = @namespace; current != null; current = current.Parent as NamespaceDeclarationSyntax)
                stack.Push(current.Name.ToString());

            return ParseName(string.Join(".", stack));
        }

        // Get full name of source class namespace
        var name = sourceClass.Parent switch
        {
            // Class can be either in =1 file scoped namespace or in >=1 nested namespaces
            FileScopedNamespaceDeclarationSyntax fileNamespace => fileNamespace.Name,
            NamespaceDeclarationSyntax @namespace => GetFullNameFrom(@namespace),
            _ => throw new SyntaxException("Source class was not in namespace")
        };

        // Append test postfix to found name
        name = QualifiedName(name, IdentifierName("Tests"));

        return FileScopedNamespaceDeclaration(name).AddMembers(testsClass);
    }

    private IEnumerable<UsingDirectiveSyntax> GenerateTestsUsingDirectives(CompilationUnitSyntax sourceUnit)
    {
        var usingDirectives = new SyntaxList<UsingDirectiveSyntax>()
            .Add(UsingDirective(ParseName("System")))
            .Add(UsingDirective(ParseName("System.Collections.Generic")))
            .Add(UsingDirective(ParseName("System.Linq")))
            .Add(UsingDirective(ParseName("System.Text")))
            .Add(UsingDirective(ParseName("XUnit")))
            .AddRange(sourceUnit.DescendantNodes().OfType<UsingDirectiveSyntax>());
        return usingDirectives;
    }

    private CompilationUnitSyntax GenerateTestsUnit(SyntaxList<UsingDirectiveSyntax> testsUsingDirectives,
        FileScopedNamespaceDeclarationSyntax testsNamespace)
    {
        return CompilationUnit()
            .WithUsings(testsUsingDirectives)
            .WithMembers(new SyntaxList<MemberDeclarationSyntax>(testsNamespace));
    }
}