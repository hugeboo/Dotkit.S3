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
using Amazon.S3.Transfer;
using Amazon.Runtime.Internal.Util;
using System.IO;

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
        private readonly S3DirectoryInfo _directory;

        public string Key { get; private set; }

        public bool Exists { get; private set; }

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

        public string FullName => $"{_bucketName}:\\{Key}";

        public DateTime LastModifiedTime { get; private set; }

        public string Name
        {
            get
            {
                int num = Key.LastIndexOf('\\');
                return Key.Substring(num + 1);
            }
        }

        public FileSystemType Type => FileSystemType.File;

        /// <summary>
        /// Returns the parent S3DirectoryInfo
        /// </summary>
        public S3DirectoryInfo Directory => _directory;

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
            Key = key;

            string dirKey = string.Empty;
            int num = key.LastIndexOf('\\');
            if (num >= 0)
            {
                dirKey = key.Substring(0, num);
            }
            _directory = new S3DirectoryInfo(s3Client, bucket, dirKey);
        }

        public override string ToString() => Key;

        /// <summary>
        /// Актуалзировать информацию (Exists, LastModifiedTime, Length) из S3 хранилища
        /// </summary>
        internal async Task UpdateFromRemoteAsync()
        {
            try
            {
                try
                {
                    var request0 = new GetObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = S3Helper.EncodeKey(Key)
                    };
                    var response0 = await _s3Client.GetObjectAsync(request0).ConfigureAwait(false);
                }
                catch(string.Equals(ex.ErrorCode, "NotFound"))
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        //...
                    }
                }

                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = S3Helper.EncodeKey(Key),
                    MaxKeys = 1
                };
                var response = await _s3Client.ListObjectsV2Async(request).ConfigureAwait(false);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK && response.S3Objects.Count == 1 &&
                    response.S3Objects[0].Key == S3Helper.EncodeKey(Key))
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
                        Key = S3Helper.EncodeKey(Key)
                    };
                    var metaResponse = await _s3Client.GetObjectMetadataAsync(getObjectMetadataRequest).ConfigureAwait(false);
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
            Exists = true;
            LastModifiedTime = obj.LastModified;
            Length = obj.Size;
        }

        /// <summary>
        /// Создание (загрузка) файла в S3 хранилище из локального входного потока
        /// </summary>
        /// <param name="stream">Входной поток</param>
        /// <returns>Сссылка на себя</returns>
        public async Task<S3FileInfo> CreateAsync(Stream stream)
        {
            using var fileTransferUtility = new TransferUtility(_s3Client);

            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                BucketName = _bucketName,
                InputStream = stream,
                AutoCloseStream = false,
                StorageClass = S3StorageClass.StandardInfrequentAccess,
                PartSize = 6291456, // 6 MB.
                Key = S3Helper.EncodeKey(Key),
                CannedACL = S3CannedACL.PublicRead
            };
            //fileTransferUtilityRequest.Metadata.Add("param1", "Value1");
            //fileTransferUtilityRequest.Metadata.Add("param2", "Value2");

            await fileTransferUtility.UploadAsync(fileTransferUtilityRequest).ConfigureAwait(false);

            await UpdateFromRemoteAsync();

            return this;
        }

        /// <summary>
        /// Создание (загрузка) текстового файла в S3 хранилище
        /// </summary>
        /// <param name="text">Текст - содержимое файла</param>
        /// <returns>Сссылка на себя</returns>
        public async Task<S3FileInfo> CreateTextAsync(string text)
        {
            using var memStream = new MemoryStream();
            byte[] data = Encoding.UTF8.GetBytes(text); 
            memStream.Write(data, 0, data.Length); 
            memStream.Position = 0;

            await CreateAsync(memStream);

            return this;
        }

        /// <summary>
        /// Загрузка локального файла в S3 хранилище
        /// </summary>
        /// <param name="localFilePath">Полный путь к локальному файлу</param>
        /// <returns>Сссылка на себя</returns>
        public async Task<S3FileInfo> UploadFileAsync(string localFilePath)
        {
            using var fileTransferUtility = new TransferUtility(_s3Client);

            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                BucketName = _bucketName,
                FilePath = localFilePath,
                StorageClass = S3StorageClass.StandardInfrequentAccess,
                PartSize = 6291456, // 6 MB.
                Key = S3Helper.EncodeKey(Key),
                CannedACL = S3CannedACL.PublicRead
            };
            //fileTransferUtilityRequest.Metadata.Add("param1", "Value1");
            //fileTransferUtilityRequest.Metadata.Add("param2", "Value2");

            await fileTransferUtility.UploadAsync(fileTransferUtilityRequest).ConfigureAwait(false);

            await UpdateFromRemoteAsync();

            return this;
        }

        /// <summary>
        /// Загрузить файл из S3 хранилища в локальную ФС
        /// </summary>
        /// <param name="localFilePath">Полный локальный путь</param>
        /// <returns>Признак успеха операции</returns>
        public async Task<bool> DownloadAsync(string localFilePath)
        {
            using var fileTransferUtility = new TransferUtility(_s3Client);

            var fileTransferUtilityRequest = new TransferUtilityDownloadRequest
            {
                BucketName = _bucketName,
                Key = S3Helper.EncodeKey(Key),
                FilePath = localFilePath
            };

            await fileTransferUtility.DownloadAsync(fileTransferUtilityRequest).ConfigureAwait(false);

            return File.Exists(localFilePath);
        }

        /// <summary>
        /// Открыть удаленный файл в S3 хранилище на чтение.
        /// Метод экспериментальный!
        /// </summary>
        /// <remarks>
        /// Возвращаемый поток после чтения необходимо закрыть (Dispose)
        /// </remarks>
        /// <returns>Открытый поток на чтение</returns>
        public async Task<Stream> OpenReadAsync()
        {
            GetObjectRequest getObjectRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = S3Helper.EncodeKey(Key)
            };
            GetObjectResponse getObjectResponse = await _s3Client.GetObjectAsync(getObjectRequest).ConfigureAwait(false);
            return getObjectResponse.ResponseStream;
        }

        public async Task DeleteAsync()
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = S3Helper.EncodeKey(Key)
            };
            await _s3Client.DeleteObjectAsync(deleteObjectRequest).ConfigureAwait(false);
            await UpdateFromRemoteAsync();
        }
    }
}
