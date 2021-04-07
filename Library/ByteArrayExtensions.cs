using System.Text;

public static class ByteArrayExtensions
{
    public static string ToDebugString(this byte[] bytes)
    {
        return ToDebugString(bytes,0,bytes.Length);
    }

    public static string ToDebugString(this byte[] bytes, int offset, int size)
    {
        if(bytes == null) return string.Empty;
        var builder = new StringBuilder();
        for (int i = offset; i < offset + size; i++)
        {
            builder.AppendFormat("{0:X2}", bytes[i]);
        }
        return builder.ToString();
    }
}
