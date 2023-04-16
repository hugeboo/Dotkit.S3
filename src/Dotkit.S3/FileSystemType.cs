using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotkit.S3
{
    /// <summary>
    /// Enumeration indicated whether a file system element is a file or directory
    /// </summary>
    public enum FileSystemType
    {
        /// <summary>
        /// Type is a directory
        /// </summary>
        Directory,

        /// <summary>
        /// Type is a file
        /// </summary>
        File
    }
}
