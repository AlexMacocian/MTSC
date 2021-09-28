using System;

namespace MTSC.Common.Http.Attributes
{
    public class FromUrlAttribute : Attribute
    {
        public string Placeholder { get; }

        public FromUrlAttribute(string placeholder)
        {
            this.Placeholder = placeholder;
        }
    }
}
