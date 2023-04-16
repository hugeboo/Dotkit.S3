using Amazon.Runtime.Internal;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3
{
    /// <summary>
    /// Операции с файлом в рамках бакета S3 хранилища.
    /// <inheritdoc/>
    /// </summary>
    public sealed class S3FileInfo : IS3FileSystemInfo
    {
        private readonly string _bucketName;
        private readonly IAmazonS3 _s3Client;
        private readonly string _key;
        private readonly S3DirectoryInfo _directory;

        public bool Exists {get; private set;}

        public string Extension
        {
            get
            {
                int num = Name.LastIndexOf('.');
                if (num == -1 || Name.Length <= num + 1)
                {
                    return string.Empty;
                }

                return Name.Substring(num + 1);
            }
        }

        public string FullName => $"{_bucketName}:\\{_key}";

        public DateTime LastModifiedTime { get; private set; }

        public string Name
        {
            get
            {
                int num = _key.LastIndexOf('\\');
                return _key.Substring(num + 1);
            }
        }

        public FileSystemType Type => FileSystemType.File;

        /// <summary>
        /// Returns the parent S3DirectoryInfo
        /// </summary>
        public S3DirectoryInfo Directory => Directory;

        /// <summary>
        /// The full name of parent directory
        /// </summary>
        public string DirectoryName => Directory.FullName;

        /// <summary>
        /// Returns the content length of the file
        /// </summary>
        public long Length { get; private set; }

        /// <summary>
        /// Конструктор класса файла
        /// </summary>
        /// <param name="s3Client">Клиент для связи с S3 хранилищем</param>
        /// <param name="bucket">Имя бакета</param>
        /// <param name="key">Путь к директории</param>
        internal S3FileInfo(IAmazonS3 s3Client, string bucket, string key)
        {
            if (string.IsNullOrEmpty(key) || string.Equals(key, "\\"))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key.EndsWith("\\", StringComparison.Ordinal))
            {
                throw new ArgumentException("key is a directory name");
            }

            _s3Client = s3Client;
            _bucketName = bucket;
            _key = key;

            string dirKey = null!;
            int num = key.LastIndexOf('\\');
            if (num >= 0)
            {
                dirKey = key.Substring(0, num);
            }
            else
            {
                new ArgumentException("key contains bad directory");
            }
            _directory = new S3DirectoryInfo(s3Client, bucket, dirKey);
        }

        /// <summary>
        /// Актуалзировать информацию (Exists, LastModifiedTime, Length) из S3 хранилища
        /// </summary>
        internal async Task InitFromRemoteAsync()
        {
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = S3Helper.EncodeKey(_key),
                    MaxKeys = 1
                };
                var response = await _s3Client.ListObjectsV2Async(request);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK && response.S3Objects.Count == 1)
                {
                    Exists = true;
                    LastModifiedTime = response.S3Objects[0].LastModified;
                }
                else
                {
                    Exists = false;
                    LastModifiedTime = DateTime.MinValue;
                }

                Length = 0L;
                if (Exists)
                {
                    var getObjectMetadataRequest = new GetObjectMetadataRequest
                    {
                        BucketName = _bucketName,
                        Key = S3Helper.EncodeKey(_key)
                    };
                    var metaResponse = await _s3Client.GetObjectMetadataAsync(getObjectMetadataRequest);
                    Length = metaResponse.ContentLength;
                }
            }
            catch (AmazonS3Exception ex)
            {
                if (string.Equals(ex.ErrorCode, "NotFound"))
                {
                    Exists = false;
                    LastModifiedTime = DateTime.MinValue;
                    Length = 0L;
                }
                throw;
            }
        }

        /// <summary>
        /// Актуалзировать информацию (Exists, LastModifiedTime, Length) из объекта S3 хранилища
        /// </summary>
        internal void InitFromRemoteObject(S3Object obj)
        {
            LastModifiedTime = obj.LastModified;
            Length = obj.Size;
        }

        public Task DeleteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
