namespace MTSC.Common.Http.Forms
{
    public abstract class ContentTypeBase
    {
        public string ContentType { get; private set; }

        public ContentTypeBase(string contentType)
        {
            this.ContentType = contentType;
        }
    }
}
