using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YetAnotherPacketParser;

namespace YetAnotherPacketParserTests
{
    [TestClass]
    public class MagicWordDetectorTests
    {
        [TestMethod]
        public async Task IsZipFileSuccess()
        {
            // Base 64 encoded zip file with an empty HTML document
            const string zipWithHtmlString = "UEsDBBQAAAAIAAmmRVN3dFe2EwAAABoAAAAOAAAAZW1wdHlIdG1sLmh0bWyzySjJzbGzScpPqbSz0YdSYDEAUEsBAj8AFAAAAAgACaZFU3d0V7YTAAAAGgAAAA4AJAAAAAAAAAAgAAAAAAAAAGVtcHR5SHRtbC5odG1sCgAgAAAAAAABABgARu6F/2S61wFoW8n/ZLrXAcibZ/VkutcBUEsFBgAAAAABAAEAYAAAAD8AAAAAAA==";
            byte[] zipBytes = Convert.FromBase64String(zipWithHtmlString);
            using (Stream stream = new MemoryStream(zipBytes))
            {
                Tuple<bool, Stream> result = await MagicWordDetector.IsZipFile(stream);
                Assert.IsTrue(result.Item1);
                Assert.AreEqual(stream.Length, result.Item2.Length, "Stream length changed");

                byte[] resultBytes = new byte[stream.Length];
                Assert.AreEqual(stream.Length, result.Item2.Read(resultBytes), "Unexpected number of bytes read back");
                CollectionAssert.AreEqual(zipBytes, resultBytes, "Zip was modified");
            }
        }

        [TestMethod]
        public async Task IsZipFileFailsOnHtml()
        {
            const string html = "<html><body><p>1. Here is a question</p><br>Answer: Answer</br></body></html>";
            byte[] fileBytes = Encoding.UTF8.GetBytes(html);
            using (Stream stream = new MemoryStream(fileBytes))
            {
                Tuple<bool, Stream> result = await MagicWordDetector.IsZipFile(stream);
                Assert.IsFalse(result.Item1);
                Assert.AreEqual(stream.Length, result.Item2.Length, "Stream length changed");

                byte[] resultBytes = new byte[stream.Length];
                Assert.AreEqual(stream.Length, result.Item2.Read(resultBytes), "Unexpected number of bytes read back");
                CollectionAssert.AreEqual(fileBytes, resultBytes, "Stream was modified");
            }
        }
    }
}
