using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace RpcProviderCore.Controllers
{
    public class HomeController: ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("test");
        }
    }
}