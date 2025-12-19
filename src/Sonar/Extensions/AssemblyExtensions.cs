using System.Reflection;

namespace Sonar.Extensions;

internal static class AssemblyExtensions
{
    extension(Assembly assembly)
    {
        public string GetVersion()
        {
            try
            {
                return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
            }
            catch (Exception)
            {
                // Silent
            }

            return $"{new Version(0, 0, 0).ToString(fieldCount: 3)}";
        }

        private string? GetResourceKey(string embeddedFile)
        {
            return assembly.GetManifestResourceNames().FirstOrDefault(x => x.Contains(embeddedFile, StringComparison.OrdinalIgnoreCase));
        }

        public Stream ReadFromEmbeddedResource(string resourceName)
        {
            var key = GetResourceKey(assembly, resourceName);
            if (key == null)
            {
                throw new ArgumentException($"Resource name '{resourceName}' not found in assembly '{assembly.FullName}'");
            }

            var stream = assembly.GetManifestResourceStream(key);
            {
                if (stream == null)
                {
                    throw new ArgumentException($"Resource name '{resourceName}' not found");
                }
            }

            return stream;
        }
    }
}