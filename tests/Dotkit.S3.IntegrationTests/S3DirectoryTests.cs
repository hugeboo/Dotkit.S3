using System.IO;

namespace Dotkit.S3.IntegrationTests
{
    public class S3DirectoryTests
    {
        [Fact]
        public void CreateDirectory_Success()
        {
            var config = new S3Configuration();
            var service = config.CreateService();
            
            string path = @"high-level-folder";
            var di = service.GetDirectoryInfoAsync(path).Result;
            Assert.False(di.Exists);

            di.CreateAsync().Wait();
            Assert.True(di.Exists);
            Assert.True(di.LastModifiedTime > DateTime.MinValue);

            di.DeleteAsync().Wait();
            Assert.False(di.Exists);
        }

        [Fact]
        public void RecursiveDeleteDirectory_Success()
        {
            var config = new S3Configuration();
            var service = config.CreateService();

            string path = @"high-level-folder\1\2\3";
            var di = service.GetDirectoryInfoAsync(path).Result;

            di.CreateAsync().Wait();
            Assert.True(di.Exists);

            di = service.GetDirectoryInfoAsync("high-level-folder").Result;
            
            di.DeleteAsync().Wait();
            Assert.True(di.Exists);
            
            di.DeleteAsync(true).Wait();
            Assert.False(di.Exists);
        }
    }
}