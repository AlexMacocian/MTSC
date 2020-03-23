namespace MTSC.Common.Http.Forms
{
    public class TextContentType : ContentTypeBase
    {
        public string Value { get; private set; }

        public TextContentType(string contentType, string value) : base(contentType)
        {
            this.Value = value;
        }
    }
}
