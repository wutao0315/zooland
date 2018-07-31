namespace csharp RpcContractThrift
namespace java com.hello.service.thrift

/**
 * HelloResult实体
 */
struct HelloResult
{
	1: string Name; 
	2: string Gender;	
	3: string Head;	  
}

/**
 * thrift HelloService
 */
service HelloService
{
	/**
	 * CallNameVoid
	 */
	string CallNameVoid();
	/**
	 * CallName
	 */
	void CallName(1:string name);
	/**
	 * CallVoid
	 */
	void CallVoid();
	/**
	 * Hello
	 */
	string Hello(1:string name);
	/**
	 * SayHello
	 */
	HelloResult SayHello(1:string name);
	/**
	 * ShowHello
	 */
	string ShowHello(1:HelloResult hello);
}