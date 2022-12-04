using TestGeneratorLib;

namespace TestsGenerator
{
    internal static class MainApp
    {
        public static void Main()
        {
            const string testPath = @"F:\\spp\\tests";
            var collection = new List<string>
            {
                @"F:\\spp\\fouth\\TestGenerator\\TestGenerator\\testClasses\\Class1.cs",
                @"F:\\spp\\fouth\\TestGenerator\\TestGenerator\\testClasses\\Class2.cs",
                @"F:\\spp\\fouth\\TestGenerator\\TestGenerator\\testClasses\\Class3.cs",
            };
            Config config = new Config(1, 1, 1);
            var generator = new NUnitTestGenerator(config);
            try
            {
                var task = generator.GenerateCLasses(collection, testPath);
                task?.Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Finish");
            Console.ReadLine();
        }
    }
}