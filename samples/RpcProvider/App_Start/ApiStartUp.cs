using Owin;
using Spring.Context.Support;
using Spring.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;

namespace RpcProvider
{
    public class ApiStartUp
    {

        public void Configuration(IAppBuilder appBuilder)
        {

            // Web API 路由

            // Configure Web API for self-host. 
            var config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = RouteParameter.Optional }
            );
            //跨域配置
            config.EnableCors(new EnableCorsAttribute("*", "*", "*"));
            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional }
            //);
            config.Formatters.XmlFormatter.SupportedMediaTypes.Clear();
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/json"));

            var springResolver = new SpringWebApiDependencyResolver(ContextRegistry.GetContext());
            config.DependencyResolver = springResolver;

            appBuilder.UseWebApi(config);
        }


    }
}
