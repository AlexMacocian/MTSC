using System;

namespace MTSC.Common.Http.Attributes
{
    public sealed class FromRouteContextResourcesAttribute : Attribute
    {
        public string ResourceKey { get; }

        public FromRouteContextResourcesAttribute(string resourceKey)
        {
            this.ResourceKey = resourceKey;
        }
    }
}
