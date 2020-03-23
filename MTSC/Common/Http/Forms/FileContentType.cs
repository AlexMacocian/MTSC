namespace MTSC.Common.Http.Forms
{
    public class FileContentType : ContentTypeBase
    {
        public string FileName { get; private set; }

        public byte[] Data { get; private set; }

        public FileContentType(string contentType, string fileName, byte[] data) : base(contentType)
        {
            this.FileName = fileName;
            this.Data = data;
        }
    }
}
