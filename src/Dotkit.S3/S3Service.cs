using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    internal sealed class S3Service : IS3Service
    {
        private readonly string _bucketName;
        private readonly IAmazonS3 _s3Client;
        private readonly S3DirectoryInfo _root;

        public S3DirectoryInfo Root => _root;

        public S3Service(S3Configuration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            _bucketName = configuration.BucketName;
            var s3Config = new AmazonS3Config
            {
                ServiceURL = configuration.ServiceURL
            };
            _s3Client = new AmazonS3Client(configuration.AccessKeyId, configuration.SecretAccessKey, s3Config);
            _root = new S3DirectoryInfo(_s3Client, _bucketName, "", true);
        }

        public void Dispose()
        {
            _s3Client.Dispose();
        }

        public async Task<S3DirectoryInfo> GetDirectoryAsync(string key)
        {
            var di = new S3DirectoryInfo(_s3Client, _bucketName, key);
            await di.UpdateFromRemoteAsync();
            return di;
        }

        public async Task<S3FileInfo> GetFileAsync(string key)
        {
            var fi = new S3FileInfo(_s3Client, _bucketName, key);
            await fi.UpdateFromRemoteAsync();
            return fi;
        }
    }
}
