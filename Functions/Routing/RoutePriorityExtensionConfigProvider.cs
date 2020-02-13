using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host.Config;

namespace Functions.Routing
{
    public class RoutePriorityExtensionConfigProvider : IExtensionConfigProvider
    {
        IApplicationLifetime _applicationLifetime;
        IWebJobsRouter _router;

        public RoutePriorityExtensionConfigProvider(IApplicationLifetime applicationLifetime, IWebJobsRouter router)
        {
            _applicationLifetime = applicationLifetime;
            _router = router;

            _applicationLifetime.ApplicationStarted.Register(() =>
            {
                ReorderRoutes();
            });
        }

        public void Initialize(ExtensionConfigContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }
        }

        public void ReorderRoutes()
        {
            var unorderedRoutes = _router.GetRoutes();
            var routePrecedence = Comparer<Route>.Create(RouteComparison);
            var orderedRoutes = unorderedRoutes.OrderBy(id => id, routePrecedence);
            var orderedCollection = new RouteCollection();
            foreach (var route in orderedRoutes)
            {
                orderedCollection.Add(route);
            }
            _router.ClearRoutes();
            _router.AddFunctionRoutes(orderedCollection, null);
        }

        static int RouteComparison(Route x, Route y)
        {
            var xTemplate = x.ParsedTemplate;
            var yTemplate = y.ParsedTemplate;

            for (var i = 0; i < xTemplate.Segments.Count; i++)
            {
                if (yTemplate.Segments.Count <= i)
                {
                    return -1;
                }

                var xSegment = xTemplate.Segments[i].Parts[0];
                var ySegment = yTemplate.Segments[i].Parts[0];
                if (!xSegment.IsParameter && ySegment.IsParameter)
                {
                    return -1;
                }
                if (xSegment.IsParameter && !ySegment.IsParameter)
                {
                    return 1;
                }

                if (xSegment.IsParameter)
                {
                    if (xSegment.InlineConstraints.Count() > ySegment.InlineConstraints.Count())
                    {
                        return -1;
                    }
                    else if (xSegment.InlineConstraints.Count() < ySegment.InlineConstraints.Count())
                    {
                        return 1;
                    }
                }
                else
                {
                    var comparison = string.Compare(xSegment.Text, ySegment.Text, StringComparison.OrdinalIgnoreCase);
                    if (comparison != 0)
                    {
                        return comparison;
                    }
                }
            }
            if (yTemplate.Segments.Count > xTemplate.Segments.Count)
            {
                return 1;
            }
            return 0;
        }
    }
}