using System.IO;

namespace Dotkit.S3.IntegrationTests
{
    public class S3DirectoryTests
    {
        [Fact]
        public void CreateAndDeeletDirectory()
        {
            var config = new S3Configuration();
            var service = config.CreateService();
            
            string path = @"high-level-folder";
            var di = service.GetDirectoryAsync(path).Result;
            Assert.False(di.Exists);

            di.CreateAsync().Wait();
            Assert.True(di.Exists);
            Assert.True(di.LastModifiedTime > DateTime.MinValue);

            di.DeleteAsync().Wait();
            Assert.False(di.Exists);
        }

        [Fact]
        public void RecursiveDeleteDirectory()
        {
            var config = new S3Configuration();
            var service = config.CreateService();

            string path = @"high-level-folder\1\2\3";
            var di = service.GetDirectoryAsync(path).Result;

            di.CreateAsync().Wait();
            Assert.True(di.Exists);

            di = service.GetDirectoryAsync("high-level-folder").Result;
            
            di.DeleteAsync().Wait();
            Assert.True(di.Exists);
            
            di.DeleteAsync(true).Wait();
            Assert.False(di.Exists);
        }

        [Fact]
        public void EnumerateDirectories()
        {
            var service = Utils.CreateEnumerateTestDirs();

            var root = service.GetDirectoryAsync("EnumerateTests").Result;

            // 1
            var lst1 = root.GetDirectories().Result;
            Assert.Equal(2, lst1.Count);

            // 2
            var level12 = root.GetSubDirectoryAsync("Level12").Result;
            var lst2 = level12.GetDirectories().Result;
            Assert.Single(lst2);

            // 3
            var level23 = service.GetDirectoryAsync("EnumerateTests\\Level12\\Level23").Result;
            var lst3 = level23.GetDirectories().Result;
            Assert.Equal(2, lst3.Count);

            // 3 - Check delete
            level23.DeleteAsync(true).Wait();
            lst3 = level23.GetDirectories().Result;
            Assert.Empty(lst3);

            root.DeleteAsync(true).Wait();
            Assert.False(root.Exists);
        }
    }
}