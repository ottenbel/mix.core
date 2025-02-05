﻿using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Mix.Heart.NetCore.Attributes;
using Mix.Heart.NetCore.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Mix.Heart.Providers
{
    public class GenericTypeControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public Assembly Assembly { get; set; }
        public Type BaseType { get; set; }
        public GenericTypeControllerFeatureProvider(Assembly assembly, Type baseType = null)
        {
            this.Assembly = assembly;
            BaseType = baseType != null ? baseType : typeof(BaseRestApiController<,,>);
        }
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            var candidates = Assembly.GetExportedTypes().Where(x => x.GetCustomAttributes<GeneratedControllerAttribute>().Any());

            foreach (var candidate in candidates)
            {
                if (candidate.BaseType.IsGenericType)
                {
                    feature.Controllers.Add(
                        BaseType.MakeGenericType(
                            candidate.BaseType.GenericTypeArguments[0], candidate.BaseType.GenericTypeArguments[1], candidate)
                            .GetTypeInfo()
                    );
                }
            }
        }
    }
}
