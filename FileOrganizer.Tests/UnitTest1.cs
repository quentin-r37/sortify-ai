using MimeTypes;

namespace FileOrganizer.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            MimeTypeMap.GetMimeType("aa.pdf");
        }
    }


}