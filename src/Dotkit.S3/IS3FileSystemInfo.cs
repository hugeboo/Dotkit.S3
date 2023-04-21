using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3
{
    /// <summary>
    /// IS3FileSystemInfo: Common interface for both S3FileInfo and S3DirectoryInfo
    /// </summary>
    public interface IS3FileSystemInfo
    {
        /// <summary>
        /// Returns true if the item exists in S3
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Returns the extension of the item
        /// </summary>
        string Extension { get; }

        /// <summary>
        /// Returns the fully qualified path to the item
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Returns the last modified time for this item from S3 in local timezone
        /// </summary>
        DateTime LastModifiedTime { get; }

        /// <summary>
        /// Returns the name of the item without parent information
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns the key of the item
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Indicates what type of item this object represents
        /// </summary>
        FileSystemType Type { get; }

        /// <summary>
        /// Returns the ETag of the item
        /// </summary>
        string? ETag { get; }

        /// <summary>
        /// Deletes this item from S3
        /// </summary>
        Task DeleteAsync();
    }
}
