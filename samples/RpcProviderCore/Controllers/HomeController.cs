using Microsoft.AspNetCore.Mvc;


namespace RpcProviderCore.Controllers;

public class HomeController: ControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        return Ok("test");
    }
}
