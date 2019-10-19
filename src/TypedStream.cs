using System.Net.Http;
using System.Threading.Tasks;
using System.IO;

namespace OsmSharp.IO.API
{
    public class TypedStream
    {
        public Stream Stream;
        public string FileName;
        public System.Net.Http.Headers.MediaTypeHeaderValue ContentType;

        internal static async Task<TypedStream> Create(HttpContent content)
        {
            return new TypedStream
            {
                FileName = content.Headers.ContentDisposition?.FileName.Trim('"'),
                ContentType = content.Headers.ContentType,
                Stream = await content.ReadAsStreamAsync()
            };
        }
    }
}

