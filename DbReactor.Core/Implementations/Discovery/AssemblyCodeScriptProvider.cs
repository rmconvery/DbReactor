using DbReactor.Core.Abstractions;
using DbReactor.Core.Discovery;
using DbReactor.Core.Models.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DbReactor.Core.Implementations.Discovery
{
    /// <summary>
    /// Discovers code scripts from an assembly by scanning for ICodeScript implementations
    /// </summary>
    public class AssemblyCodeScriptProvider : IScriptProvider
    {
        private readonly Assembly _assembly;
        private readonly string _targetNamespace;

        public AssemblyCodeScriptProvider(Assembly assembly, string baseNamespace, string folderName)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _targetNamespace = string.IsNullOrEmpty(folderName) ? baseNamespace : $"{baseNamespace}.{folderName}";
        }

        public AssemblyCodeScriptProvider(Assembly assembly, string @namespace = null)
        {
            _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
            _targetNamespace = @namespace;
        }

        public IEnumerable<IScript> GetScripts()
        {
            var codeScriptTypes = _assembly.GetTypes()
                .Where(t => typeof(ICodeScript).IsAssignableFrom(t) 
                    && !t.IsInterface 
                    && !t.IsAbstract
                    && t.HasParameterlessConstructor())
                .Where(t => string.IsNullOrEmpty(_targetNamespace) || t.Namespace?.StartsWith(_targetNamespace) == true)
                .OrderBy(t => t.FullName);

            var scripts = new List<IScript>();
            
            foreach (var type in codeScriptTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as ICodeScript;
                    if (instance != null)
                    {
                        scripts.Add(new ManagedCodeScript(instance));
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other scripts
                    throw new InvalidOperationException($"Failed to create instance of code script '{type.FullName}': {ex.Message}", ex);
                }
            }

            return scripts.OrderBy(s => s.Name);
        }
    }

    /// <summary>
    /// Extension methods for Type reflection
    /// </summary>
    internal static class TypeExtensions
    {
        public static bool HasParameterlessConstructor(this Type type)
        {
            return type.GetConstructors().Any(c => c.GetParameters().Length == 0);
        }
    }
}