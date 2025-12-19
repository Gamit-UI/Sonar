namespace Sonar.Extensions;

internal static class ArrayExtensions
{
    private const byte Secret = 0xAA;

    public static void XorDecode(this byte[] buffer, int count)
    {
        for (var i = 0; i < count; i++)
            buffer[i] = (byte)(buffer[i] ^ Secret);
    }
}