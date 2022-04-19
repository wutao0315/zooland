namespace RpcProviderCore;

public interface IHelloRepository
{
    string SayHello();

}
public class HelloRepository: IHelloRepository
{
    public string SayHello() 
    {
        return "Hello Repository";
    }
}
