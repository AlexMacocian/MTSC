using System;

namespace MTSC.Common.Http.Attributes
{
    public sealed class FromHeadersAttribute : Attribute
    {
        public string HeaderName { get; }

        public FromHeadersAttribute(string headerName)
        {
            this.HeaderName = headerName;
        }
    }
}
