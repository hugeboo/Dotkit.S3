using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3.IntegrationTests
{
    public class S3FileInfoTests
    {
        [Fact]
        public void CreateAndDeleteTextFile()
        {
            var service = Utils.CreateEnumerateTestDirs();

            var text00 = service.GetFileAsync("EnumerateTests\\text00.txt").Result;
            Assert.True(text00.Exists);
            Assert.True(text00.LastModifiedTime > DateTime.MinValue);
            Assert.True(text00.Length > 0);
            text00.DeleteAsync().Wait();
            Assert.False(text00.Exists);
            Assert.False(text00.LastModifiedTime > DateTime.MinValue);
            Assert.False(text00.Length > 0);

            var text11 = service.GetFileAsync("EnumerateTests\\Level11\\text11.txt").Result;
            Assert.True(text11.Exists);
            Assert.True(text11.LastModifiedTime > DateTime.MinValue);
            Assert.True(text11.Length > 0);
            text11.DeleteAsync().Wait();
            Assert.False(text11.Exists);
            Assert.False(text11.LastModifiedTime > DateTime.MinValue);
            Assert.False(text11.Length > 0);

            var root = service.GetDirectoryAsync("EnumerateTests").Result;
            root.DeleteAsync(true).Wait();
            Assert.False(root.Exists);
        }

        [Fact]
        public void UploadAndDownloadFile()
        {
            var size = 10 * 1024 * 1024;

            var tempFileName = Path.GetTempFileName();
            var s = new String('d', size);
            File.WriteAllText(tempFileName, s);

            var service = Utils.CreateEnumerateTestDirs();
            var fi = service.GetFileAsync("EnumerateTests\\test_file.txt").Result;

            fi.UploadFileAsync(tempFileName).Wait();
            Assert.True(fi.Exists);
            Assert.Equal(size, fi.Length);
            File.Delete(tempFileName);

            tempFileName = Path.GetTempFileName();
            bool ok = fi.DownloadAsync(tempFileName).Result;
            Assert.True(ok);
            var lfi = new FileInfo(tempFileName);
            Assert.True(lfi.Exists);
            Assert.Equal(size, lfi.Length);
            File.Delete(tempFileName);

            var root = service.GetDirectoryAsync("EnumerateTests").Result;
            root.DeleteAsync(true).Wait();
            Assert.False(root.Exists);
        }
    }
}
