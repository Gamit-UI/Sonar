namespace Sonar.Extensions;

internal static class StringExtensions
{
    extension(string fileName)
    {
        public string CleanFileName()
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(oldValue: c.ToString(), newValue: "_"));
        }

        public string CleanFilePath()
        {
            return Path.GetInvalidPathChars().Aggregate(fileName, (current, c) => current.Replace(oldValue: c.ToString(), newValue: string.Empty));
        }
    }
}