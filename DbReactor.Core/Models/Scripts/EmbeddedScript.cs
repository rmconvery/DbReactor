using DbReactor.Core.Abstractions;
using DbReactor.Core.Utilities;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace DbReactor.Core.Models.Scripts
{
    public class EmbeddedScript : IScript
    {
        public string Name { get; }
        public string Script { get; }
        public string Hash { get; }

        public EmbeddedScript(Assembly assembly, string resourceName)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (string.IsNullOrWhiteSpace(resourceName)) throw new ArgumentNullException(nameof(resourceName));

            Name = resourceName;
            Script = ReadResource(assembly, resourceName);

            Hash = HashUtility.GenerateHash(resourceName + Script);
        }


        private string ReadResource(Assembly assembly, string resourceName)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    return null;
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

    }
}
