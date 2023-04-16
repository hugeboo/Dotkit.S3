using System.IO;

namespace Dotkit.S3.IntegrationTests
{
    public class S3DirectoryTests
    {
        [Fact]
        public void CreateDirectory()
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
        public void RecursiveDeleteDirectory()
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

        // Тестовая структура директорий:
        //
        // EnumerateTests
        // |
        // +-Level11
        // | |
        // | +-Level21
        // | +-Level22
        // |
        // +-Level12
        //   |
        //   +-Level23
        //     |
        //     +-Level31
        //     +-Level32

        [Fact]
        public void EnumerateDirectories()
        {
            var service = CreateEnumerateTestDirs();

            var root = service.GetDirectoryInfoAsync("EnumerateTests").Result;

            // 1
            var lst1 = root.GetDirectories().Result;
            Assert.Equal(2, lst1.Count);

            // 2
            var level12 = root.GetSubDirectoryInfoAsync("Level12").Result;
            var lst2 = level12.GetDirectories().Result;
            Assert.Single(lst2);

            // 3
            var level23 = service.GetDirectoryInfoAsync("EnumerateTests\\Level12\\Level23").Result;
            var lst3 = level23.GetDirectories().Result;
            Assert.Equal(2, lst3.Count);

            // 3 - Check delete
            level23.DeleteAsync(true).Wait();
            lst3 = level23.GetDirectories().Result;
            Assert.Empty(lst3);

            root.DeleteAsync(true).Wait();
            Assert.False(root.Exists);
        }

        private static IS3Service CreateEnumerateTestDirs()
        {
            var config = new S3Configuration();
            var service = config.CreateService();

            var root = service.GetDirectoryInfoAsync("EnumerateTests").Result.CreateAsync().Result;

            var level11 = root.GetSubDirectoryInfoAsync("Level11").Result.CreateAsync().Result;
            var level21 = level11.GetSubDirectoryInfoAsync("Level21").Result.CreateAsync().Result;
            var level22 = level11.GetSubDirectoryInfoAsync("Level22").Result.CreateAsync().Result;

            var level12 = root.GetSubDirectoryInfoAsync("Level12").Result.CreateAsync().Result;
            var level23 = level12.GetSubDirectoryInfoAsync("Level23").Result.CreateAsync().Result;
            var level31 = level23.GetSubDirectoryInfoAsync("Level31").Result.CreateAsync().Result;
            var level32 = level23.GetSubDirectoryInfoAsync("Level32").Result.CreateAsync().Result;

            return service;
        }
    }
}