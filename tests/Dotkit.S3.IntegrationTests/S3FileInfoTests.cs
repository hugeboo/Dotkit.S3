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
            Assert.True(text00.Length == 6);
            text00.DeleteAsync().Wait();
            Assert.False(text00.Exists);
            Assert.False(text00.LastModifiedTime > DateTime.MinValue);
            Assert.False(text00.Length > 0);

            var text111 = service.GetFileAsync("EnumerateTests\\Level11\\text11.txt").Result;
            var text112 = service.GetFileAsync("EnumerateTests\\Level11\\text11.txt.user").Result.CreateTextAsync("00").Result;
            
            Assert.True(text111.Exists);
            Assert.True(text111.LastModifiedTime > DateTime.MinValue);
            Assert.True(text111.Length == 6);
            text111.DeleteAsync().Wait();
            Assert.False(text111.Exists);
            Assert.False(text111.LastModifiedTime > DateTime.MinValue);
            Assert.False(text111.Length > 0);

            Assert.True(text112.Exists);
            Assert.True(text112.LastModifiedTime > DateTime.MinValue);
            Assert.True(text112.Length == 2);
            text112.DeleteAsync().Wait();
            Assert.False(text112.Exists);
            Assert.False(text112.LastModifiedTime > DateTime.MinValue);
            Assert.False(text112.Length > 0);

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
            bool ok = fi.DownloadAsync(tempFileName, true).Result;
            Assert.True(ok);
            var lfi = new FileInfo(tempFileName);
            Assert.True(lfi.Exists);
            Assert.Equal(size, lfi.Length);
            var tagLfi = new FileInfo($"{tempFileName}.etag");
            Assert.True(tagLfi.Exists);
            Assert.Equal(fi.ETag, File.ReadAllText($"{tempFileName}.etag"));
            File.Delete(tempFileName);
            File.Delete($"{tempFileName}.etag");

            var root = service.GetDirectoryAsync("EnumerateTests").Result;
            root.DeleteAsync(true).Wait();
            Assert.False(root.Exists);
        }
    }
}
