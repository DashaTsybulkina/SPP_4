using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGeneratorLib.structure
{
    public class TestClassStructure
    {
        public string TestClassName { get; }
        public string TestClassData { get; }

        public TestClassStructure(string className, string classData)
        {
            TestClassName = className;
            TestClassData = classData;
        }
    }
}
