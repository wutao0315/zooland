using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Zooyard.Rpc.NettyImpl;

public static class SerializeExtensions
{
    public static byte[] Serialize(this object obj)
    {
        try
        {
            IFormatter formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, obj);
            stream.Position = 0;
            var buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            stream.Flush();
            stream.Close();
            return buffer;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw ex;
        }
    }
    public static T Desrialize<T>(this byte[] buffer)
    {
        try
        {
            var obj = default(T);
            IFormatter formatter = new BinaryFormatter();
            var stream = new MemoryStream(buffer);
            obj = (T)formatter.Deserialize(stream);
            stream.Flush();
            stream.Close();
            return obj;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw ex;
        }
    }
}
