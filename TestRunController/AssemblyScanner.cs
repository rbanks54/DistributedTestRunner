using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestRunController
{
    public static class AssemblyScanner
    {
        public static IEnumerable<TestMetaData> ScanDll(string testDll)
        {
            AppDomain.CurrentDomain.AssemblyResolve += LoadFromSameFolderResolveEventHandler;

            var testMethods = new List<TestMetaData>();

            var currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var testAssembly = Assembly.LoadFile(Path.Combine(currentFolder,testDll));
            foreach (var type in testAssembly.GetTypes())
            {
                if (!type.IsClass) 
                    continue;
                
                var classAttributes = type.GetCustomAttributes(true);
                if (classAttributes.All(a => a.GetType().Name != "TestClassAttribute")) 
                    continue;
                
                foreach (var methodInfo in type.GetMethods())
                {
                    var methodAttributes = methodInfo.GetCustomAttributes(true);
                    if (methodAttributes.All(a => a.GetType().Name != "TestMethodAttribute"))
                        continue;
                    if (methodAttributes.Any(a => a.GetType().Name == "IgnoreAttribute" || a.GetType().Name == "JailAttribute"))
                        continue;
                    var metaData = new TestMetaData();
                    metaData.TestName = type.FullName + "." + methodInfo.Name; //fully qualified name
                    foreach (var methodAttribute in methodAttributes)
                    {
                        var attributeType = methodAttribute.GetType();
                        if (attributeType.Name != "TestCategoryAttribute")
                            continue;
                        var category = methodAttribute as TestCategoryAttribute;
                        if (category == null) continue;
                        foreach (var testCategory in category.TestCategories)
                        {
                            metaData.AddAttribute(testCategory);
                        }
                    }
                    testMethods.Add(metaData);
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve -= LoadFromSameFolderResolveEventHandler;
            return testMethods;
        }

        private static Assembly LoadFromSameFolderResolveEventHandler(object sender, ResolveEventArgs args)
        {
            var folderPath = Path.GetDirectoryName(args.RequestingAssembly.Location);
            var assemblyPath = Path.Combine(folderPath, args.Name.Substring(0, args.Name.IndexOf(',')) + ".dll");
            return Assembly.LoadFile(assemblyPath);
        }

    }
}
