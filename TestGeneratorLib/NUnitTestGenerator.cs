using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks.Dataflow;
using TestGeneratorLib.structure;
using MethodInfo = TestGeneratorLib.structure.MethodInfo;

namespace TestGeneratorLib
{
    public class NUnitTestGenerator
    {

        Config config;
        private string DirPath;

        public NUnitTestGenerator(Config config)
        {
            this.config = config;
        }

        public Task GenerateCLasses(IEnumerable<string> classPaths, string testPath)
        {

            if (!Directory.Exists(testPath))
            {
                throw new FileNotFoundException("Path " + "\"" + testPath + "\" " + "Invalid ");
            }

            DirPath = testPath;

            var maxFilesToLoad = new ExecutionDataflowBlockOptions()
            { MaxDegreeOfParallelism = config.ReadingTasksCount };
            var maxTestToGenerate = new ExecutionDataflowBlockOptions()
            { MaxDegreeOfParallelism = config.ProcessingTasksCount };
            var maxFilesToWrite = new ExecutionDataflowBlockOptions()
            { MaxDegreeOfParallelism = config.WritingTasksCount };

            var loadClasses =
                new TransformBlock<string, string>(fileName => readAsync(fileName), maxFilesToLoad);
            var generateTests =
                new TransformBlock<string, List<TestClassStructure>>(sourceCode => GetTestFromText(sourceCode), maxTestToGenerate);
            var writeTests = new ActionBlock<List<TestClassStructure>>(generatedClass => writeAsync(generatedClass), maxFilesToWrite);


            var linkOption = new DataflowLinkOptions
            {
                PropagateCompletion = true
            };

            loadClasses.LinkTo(generateTests, linkOption);
            generateTests.LinkTo(writeTests, linkOption);

            foreach (var path in classPaths)
            {
                try
                {
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException("Path " + "\"" + path + "\" " + "Invalid ");
                    }

                    loadClasses.Post(path);
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            loadClasses.Complete();


            return writeTests.Completion;
        }


        private async Task<string> readAsync(string path)
        {
            using (StreamReader streamReader = new StreamReader(path))
            {
                return await streamReader.ReadToEndAsync();
            }
        }

        private async Task writeAsync(List<TestClassStructure> generatedCodes)
        {
            foreach (var generatedCode in generatedCodes) {
                string filePath = String.Format("{0}\\{1}", DirPath, generatedCode.TestClassName);
                using (StreamWriter streamWriter = new StreamWriter(filePath))
                {
                    await streamWriter.WriteAsync(generatedCode.TestClassData);
                }
            }
        }

        private List<TestClassStructure> GetTestFromText(string text)
        {
            var classes = GetClassesFromText(text);
            var tests = new List<TestClassStructure>();
            foreach (var classDeclaration in classes)
            {
                tests.Add(CreateTest(classDeclaration));
            }

            return tests; ;
        }

        private IEnumerable<ClassInfo> GetClassesFromText(string text)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(text);
            var root = syntaxTree.GetCompilationUnitRoot();
            List<ClassInfo> classes = new List<ClassInfo>();
            foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                string namespaceName = ((NamespaceDeclarationSyntax)classDeclaration.Parent).Name.ToString();
                string className = classDeclaration.Identifier.ValueText;
                classes.Add(new ClassInfo(namespaceName, className, GetMethods(classDeclaration)));
            }
            return classes;
        }

        private List<MethodInfo> GetMethods(ClassDeclarationSyntax classDeclaration)
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MethodDeclarationSyntax methodDeclaration in
                    classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().Where((methodDeclaration) =>
                    methodDeclaration.Modifiers.Any((modifier) =>
                    modifier.IsKind(SyntaxKind.PublicKeyword))))
            {
                string methodName = methodDeclaration.Identifier.ValueText;
                methods.Add(new MethodInfo(methodName));
            }

            return methods;
        }

        private TestClassStructure CreateTest(ClassInfo classInfo)
        {
            CompilationUnitSyntax compilationUnit = SyntaxFactory.CompilationUnit()
                .WithUsings(getUsingDirectives(classInfo))
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(GetNamespaceDeclaration(classInfo)
                         .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(SyntaxFactory.ClassDeclaration(classInfo.ClassName + "Tests")
                              .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                              .WithMembers(GetMembersDeclarations(classInfo))))
                        )
                     );


            string fileName = String.Format("{0}Test.cs", classInfo.ClassName);
            string fileData = compilationUnit.NormalizeWhitespace().ToFullString();

            return new TestClassStructure(fileName, fileData);
        }

        private SyntaxList<UsingDirectiveSyntax> getUsingDirectives(ClassInfo classInfo)
        {
            List<UsingDirectiveSyntax> usingDirectives = new List<UsingDirectiveSyntax>()
            {
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Collections.Generic")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Text")),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("NUnit.Framework"))
            };

            return new SyntaxList<UsingDirectiveSyntax>(usingDirectives);
        }

        private NamespaceDeclarationSyntax GetNamespaceDeclaration(ClassInfo classInfo)
        {
            NamespaceDeclarationSyntax namespaceDeclaration =
                 SyntaxFactory.NamespaceDeclaration(SyntaxFactory.QualifiedName(
                      SyntaxFactory.IdentifierName(classInfo.NamespaceName), SyntaxFactory.IdentifierName("Tests")));

            return namespaceDeclaration;
        }

        private SyntaxList<MemberDeclarationSyntax> GetMembersDeclarations(ClassInfo classInfo)
        {
            List<MemberDeclarationSyntax> methods = new List<MemberDeclarationSyntax>();

            methods.Add(getMethodDeclaration("SetUp", "SetUp", new List<StatementSyntax>()));
            foreach (MethodInfo method in classInfo.Methods)
            {
                List<StatementSyntax> bodyMembers = new List<StatementSyntax>();
                bodyMembers.Add(
                SyntaxFactory.ExpressionStatement(
                     SyntaxFactory.InvocationExpression(
                          GetAssertFail())
                               .WithArgumentList(GetMemberArgs())));
                methods.Add(getMethodDeclaration(method.Name + "Test", "Test", bodyMembers));
            }
            return new SyntaxList<MemberDeclarationSyntax>(methods);
        }


        private MemberDeclarationSyntax getMethodDeclaration(String methodName, String atribute, List<StatementSyntax> bodyMembers)
        {
            MethodDeclarationSyntax methodDeclaration = SyntaxFactory.MethodDeclaration(
                 SyntaxFactory.PredefinedType(
                      SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                           SyntaxFactory.Identifier(methodName))
                                .WithAttributeLists(
                                     SyntaxFactory.SingletonList<AttributeListSyntax>(
                           SyntaxFactory.AttributeList(
                                SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                                     SyntaxFactory.Attribute(
                                          SyntaxFactory.IdentifierName(atribute))))))
                           .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                           .WithBody(SyntaxFactory.Block(bodyMembers));

            return methodDeclaration;
        }

        private ArgumentListSyntax GetMemberArgs()
        {
            ArgumentListSyntax args = SyntaxFactory.ArgumentList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Argument(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal("Generated")))));

            return args;
        }

        private MemberAccessExpressionSyntax GetAssertFail()
        {
            MemberAccessExpressionSyntax assertFail = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Assert"),
                SyntaxFactory.IdentifierName("Fail"));

            return assertFail;
        }
    }
}