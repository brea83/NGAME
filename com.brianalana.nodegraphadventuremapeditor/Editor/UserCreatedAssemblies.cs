using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace NGAME.Editor
{
    // based on discussion about getting only user defined assemblies in unity project https://discussions.unity.com/t/gather-only-user-defined-assemblies/866842/11
    public static class UserCreatedAssemblies
    {
        private static readonly HashSet<string> s_internalAssemblyNames = new()
        {
            "Bee.BeeDriver",
            "ExCSS.Unity",
            "Mono.Security",
            "mscorlib",
            "netstandard",
            "Newtonsoft.Json",
            "nunit.framework",
            "ReportGeneratorMerged",
            "Unrelated",
            "SyntaxTree.VisualStudio.Unity.Bridge",
            "SyntaxTree.VisualStudio.Unity.Messaging",
        };

        private static readonly HashSet<string> s_NgameAssemblyNames = new()
        {
            "NGAME",
            "NGAME.Editor",
            "NGAME_Runtime",
        };

        public static IEnumerable<Assembly> Get(this AppDomain appDomain)
        {
            foreach (var assembly in appDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                var assemblyName = assembly.GetName().Name;
                if (assemblyName.StartsWith("System") ||
                   assemblyName.StartsWith("Unity") ||
                   assemblyName.StartsWith("UnityEditor") ||
                   assemblyName.StartsWith("UnityEngine") ||
                   s_internalAssemblyNames.Contains(assemblyName))
                {
                    continue;
                }

                yield return assembly;
            }
        }

        public static IEnumerable<Assembly> GetWhereNgameRefrenced(this AppDomain appDomain)
        {
            Assembly[] assemblies = appDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                if(s_NgameAssemblyNames.Contains(assembly.GetName().Name))
                {
                    yield return assembly;
                }
                else
                {
                    var refrences = assembly.GetReferencedAssemblies();

                    foreach (AssemblyName name in refrences)
                    {
                        if (s_NgameAssemblyNames.Contains(name.Name))
                        {
                            yield return assembly;
                        }
                    }
                }
            }
        }
    }
}
