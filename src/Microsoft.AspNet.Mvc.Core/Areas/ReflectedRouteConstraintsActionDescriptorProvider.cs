﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class ReflectedRouteConstraintsActionDescriptorProvider : IActionDescriptorProvider
    {
        public int Order
        {
            get { return 100; }
        }

        public void Invoke([NotNull]ActionDescriptorProviderContext context, Action callNext)
        {
            var removalConstraints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Iterate all the Reflected Action Descriptor providers and add area or other route constraints
            if (context.Results != null)
            {
                foreach (var actionDescriptor in context.Results.OfType<ReflectedActionDescriptor>())
                {
                    var routeConstraints = actionDescriptor.
                                           ControllerDescriptor.
                                           ControllerTypeInfo.
                                           GetCustomAttributes<RouteConstraintAttribute>().
                                           ToArray();

                    foreach (var routeConstraint in routeConstraints)
                    {
                        if (routeConstraint.PreventNonAttributedActions)
                        {
                            removalConstraints.Add(routeConstraint.RouteKey);
                        }

                        // TODO: Do we throw when there are duplicates, skip (current code below), or silently add duplicate (and probably fail at run time).
                        if (!ContainsKey(actionDescriptor, routeConstraint.RouteKey))
                        {
                            actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(
                                routeConstraint.RouteKey, routeConstraint.RouteValue));
                        }
                    }
                }

                foreach (var actionDescriptor in context.Results.OfType<ReflectedActionDescriptor>())
                {
                    foreach (var key in removalConstraints)
                    {
                        if (!ContainsKey(actionDescriptor, key))
                        {
                            actionDescriptor.RouteConstraints.Add(new RouteDataActionConstraint(key, RouteKeyHandling.DenyKey));
                        }
                    }
                }
            }

            callNext();
        }

        private bool ContainsKey(ActionDescriptor actionDescript, string routeKey)
        {
            return actionDescript.RouteConstraints.Any(rc => string.Equals(rc.RouteKey, routeKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}