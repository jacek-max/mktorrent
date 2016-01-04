using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace mktorrent
{
    public class FileCompletedEventArgs : EventArgs
    {
        public string FileName;
    }

    /// <summary>
    /// Stream class for reading multiple files from a directory as one stream.
    /// Especially handy when using the HL7InputStreamMessageIterator on a complete directory with messages.
    /// </summary>
    public class MultipleFilesStream : Stream
    {
        #region Private properties
        private List<FileInfo> filesInfo = new List<FileInfo>();
        private List<FileStream> fileStreams;
        private FileStream currentStream;
        private int currentStreamIndex;
        private long length = -1;
        private long position;
        private bool endReached;
        #endregion

        #region Public properties
        public List<FileInfo> GetFilesInfo()
        {
            return filesInfo;
        }

        public char? FileEndMarker
        {
            get;
            set;
        }
        #endregion

        #region Constructor
        public MultipleFilesStream(List<FileStream> streams)
        {
            Init(streams);
        }

        public MultipleFilesStream(List<string> files)
        {
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                filesInfo.Add(fileInfo);
            }
            Init(files.Select(file => File.Open(file, FileMode.Open, FileAccess.Read)).ToList());
        }

        public MultipleFilesStream(DirectoryInfo directory)
            : this(directory, new String[] {"*.*"})
        { }

        public MultipleFilesStream(DirectoryInfo directory, string[] searchPattern)
        {
            GetAllFiles(directory, searchPattern);
            
            Init(filesInfo.Select(file => file.Open(FileMode.Open)).ToList());
        }
        #endregion

        #region Private methods
        private void Init(List<FileStream> streams)
        {
            if (streams == null || streams.Count == 0)
            {
                MultiLangMsg messages = new MultiLangMsg();
                throw new ArgumentNullException(messages.GetMessage("NoStreamsProvided"));
            }
            fileStreams = streams;
            currentStreamIndex = 0;
            currentStream = fileStreams[currentStreamIndex];
        }

        private void GetAllFiles(DirectoryInfo directory, string[] searchPattern)
        {
            try
            {
                DirectoryInfo[] subDirectories = directory.GetDirectories();
                foreach (DirectoryInfo subDirectory in subDirectories)
                {
                    GetAllFiles(subDirectory, searchPattern);
                }
                foreach (string pattern in searchPattern)
                {
                    FileInfo[] files = directory.GetFiles(pattern);
                    filesInfo.AddRange(files);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Stream implementation
        public override void Flush()
        {
            if (currentStream != null)
                currentStream.Flush();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new System.InvalidOperationException("Stream is not seekable.");
        }

        public override void SetLength(long value)
        {
            this.length = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (endReached)
            {
                for (int i = offset; i < offset + count; i++)
                    buffer[i] = 0;

                return 0;
            }

            int result = 0;
            int buffPostion = offset;

            while (count > 0)
            {
                int bytesRead = currentStream.Read(buffer, buffPostion, count);
                result += bytesRead;
                buffPostion += bytesRead;
                position += bytesRead;

                if (bytesRead <= count)
                {
                    count -= bytesRead;
                }

                if (count > 0)
                {
                    // Add End marker
                    if (FileEndMarker != null)
                    {
                        buffer[buffPostion] = (byte)FileEndMarker;
                        buffPostion++;
                        count--;
                        result++;
                    }

                    if (currentStreamIndex >= fileStreams.Count - 1)
                    {
                        fileStreams[currentStreamIndex].Close();
                        RaiseCompletedEvent(fileStreams[currentStreamIndex].Name);
                        fileStreams[currentStreamIndex] = null;

                        endReached = true;
                        break;
                    }

                    fileStreams[currentStreamIndex].Close();
                    currentStream = fileStreams[++currentStreamIndex];
                    RaiseCompletedEvent(fileStreams[currentStreamIndex - 1].Name);
                    fileStreams[currentStreamIndex - 1] = null;
                }
            }

            return result;
        }

        public override long Length
        {
            get
            {
                if (length == -1)
                {
                    length = 0;
                    foreach (var stream in fileStreams)
                    {
                        length += stream.Length;
                    }
                }

                return length;
            }
        }

        public override long Position
        {
            get { return this.position; }
            set { throw new System.NotImplementedException(); }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.InvalidOperationException("Stream is not writable");
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }
        #endregion

        #region Private methods
        private void RaiseCompletedEvent(string path)
        {
            FileCompletedEventArgs args = new FileCompletedEventArgs();
            args.FileName = path;

            OnFileCompleted(args);
        }
        #endregion

        #region CompletedEvent
        protected virtual void OnFileCompleted(FileCompletedEventArgs e)
        {
            EventHandler<FileCompletedEventArgs> handler = FileCompleted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<FileCompletedEventArgs> FileCompleted;
        #endregion
    }
}