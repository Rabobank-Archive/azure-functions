using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Functions.Routing
{
    [ExcludeFromCodeCoverage]
    public static class IWebJobsRouterExtensions
    {
        public static List<Route> GetRoutes(this IWebJobsRouter router)
        {
            var type = typeof(WebJobsRouter);
            var fields = type.GetRuntimeFields();
            var field = fields.FirstOrDefault(f => f.Name == "_functionRoutes");
            var functionRoutes = field.GetValue(router);
            var routeCollection = (RouteCollection)functionRoutes;
            var routes = GetRoutes(routeCollection);

            return routes;
        }

        static List<Route> GetRoutes(RouteCollection collection)
        {
            var routes = new List<Route>();
            for (var i = 0; i < collection.Count; i++)
            {
                if (collection[i] is RouteCollection nestedCollection)
                {
                    routes.AddRange(GetRoutes(nestedCollection));
                    continue;
                }
                routes.Add((Route)collection[i]);
            }
            return routes;
        }
    }
}
