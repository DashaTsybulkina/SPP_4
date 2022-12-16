using System.Reflection.PortableExecutable;
using TestGeneratorLib;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestForTestGenerator
{
    public class UnitTest1
    {
        private CompilationUnitSyntax root;

        [SetUp]
        public void Setup()
        {
            string pathToTests = Path.GetFullPath(@"..\..\..\testClasses");
            string outputPath = pathToTests + @"\GeneratedTests";

            List<string> pathes = new List<string>();
            pathes.Add(pathToTests + @"\Class1.cs");

            NUnitTestGenerator generator = new NUnitTestGenerator(new Config(3, 3, 3));
            try
            {
                var task = generator.GenerateCLasses(pathes, outputPath);
                task?.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            string sourceCode = File.ReadAllText(outputPath + @"\CLass1Test.cs");
            root = CSharpSyntaxTree.ParseText(sourceCode).GetCompilationUnitRoot();
        }

        [Test]
        public void CheckDirectives()
        {
            Assert.AreEqual("System", root.Usings[0].Name.ToString());
            Assert.AreEqual("System.Collections.Generic", root.Usings[1].Name.ToString());
            Assert.AreEqual("System.Linq", root.Usings[2].Name.ToString());
            Assert.That(root.Usings[3].Name.ToString(), Is.EqualTo("System.Text"));
            Assert.AreEqual("NUnit.Framework", root.Usings[4].Name.ToString());
        }

        [Test]
        public void AssertFailTest()
        {
            IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            int actual = methods.ElementAt<MethodDeclarationSyntax>(1).Body.Statements.OfType<ExpressionStatementSyntax>().Where((statement) => statement.ToString().Contains("Assert.Fail")).Count();
            Assert.AreEqual(1, actual);
        }

        [Test]
        public void TestMethodAttributes()
        {
            IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            Assert.AreEqual("SetUp", methods.ElementAt(0).AttributeLists[0].Attributes[0].Name.ToString());
            for (int i = 1; i < methods.Count(); i++)
            {
                Assert.AreEqual("Test", methods.ElementAt(i).AttributeLists[0].Attributes[0].Name.ToString());
            }

        }

        [Test]
        public void CheckMethods()
        {
            IEnumerable<MethodDeclarationSyntax> methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            Assert.IsTrue(methods.Count() > 1);
            Assert.AreEqual("SetUp", methods.ElementAt<MethodDeclarationSyntax>(0).Identifier.ToString());
        }

        [Test]
        public void CheckNamecpace()
        {
            IEnumerable<NamespaceDeclarationSyntax> namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
            Assert.AreEqual("TestForTestGenerator.Tests", namespaces.ElementAt<NamespaceDeclarationSyntax>(0).Name.ToString());
        }
    }
}