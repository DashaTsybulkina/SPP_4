using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TestGeneratorLib.structure
{
    public class ClassInfo
    {
        public string NamespaceName { get; }
        public string ClassName { get; }
        public List<MethodInfo> Methods { get; }

        public ClassInfo(string namespaceName, string className, List<MethodInfo> methods)
        {
            NamespaceName = namespaceName;
            ClassName = className;
            Methods = methods;
        }
    }
}
