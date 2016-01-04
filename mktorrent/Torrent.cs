using OSS.NBEncode.Entities;
using OSS.NBEncode.Transforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace mktorrent
{
    struct TorrentFileStruct
    {
        public string Path;
        public long Length;
    }

    struct InfoStruct
    {
        public string Name;
        public long PieceLength;
        public byte[] Pieces;
        public int PrivateTorrent;
        public TorrentFileStruct[] torrentFiles;

        public BByteString GetName() {
            return new BByteString(Name);
        }

        public BInteger GetPieceLength() {
            return new BInteger(PieceLength);
        }

        public BByteString GetPieces()
        {
            return new BByteString() { Value = Pieces };
        }

        public BInteger GetPrivateTorrent()
        {
            return new BInteger(PrivateTorrent);
        }

        public BList GetTorrentFiles()
        {
            List<IBObject> files = new List<IBObject>();
            foreach (TorrentFileStruct torrentFile in torrentFiles)
            {
                List<BByteString> fileInfoName = new List<BByteString>();
                foreach (string subPath in torrentFile.Path.Split('\\'))
                {
                    fileInfoName.Add(new BByteString(subPath));
                }
                BDictionary infoFile = new BDictionary();
                infoFile.Value.Add(new BByteString("path"), new BList() { Value = fileInfoName.ToArray() });
                infoFile.Value.Add(new BByteString("length"), new BInteger(torrentFile.Length));
                files.Add(infoFile);
            }
            return new BList() { Value = files.ToArray() };
        }
    }

    struct TorrentStruct
    {
        public string FileName;
        public string Announce;
        public InfoStruct Info;

        public BByteString GetAnnounce() {
            return new BByteString(Announce);
        }

        public BDictionary GetInfo()
        {
            BDictionary info = new BDictionary();
            info.Value.Add(new BByteString("files"), Info.GetTorrentFiles());
            info.Value.Add(new BByteString("pieces"), Info.GetPieces());
            info.Value.Add(new BByteString("name"), Info.GetName());
            info.Value.Add(new BByteString("piece length"), Info.GetPieceLength());
            info.Value.Add(new BByteString("private"), Info.GetPrivateTorrent());
            info.Value.Add(new BByteString("source"), new BByteString(String.Format("{0} {1}",
                typeof(Torrent).Assembly.GetName().Name,
                typeof(Torrent).Assembly.GetName().Version)));
            return info;
        }
    }

    class Torrent
    {
        private MultiLangMsg messages;
        private TorrentStruct torrentSaved = new TorrentStruct();
        private string[] filesFilter = new string[] { "*.*" };
        
        public string TorrentFileName = "";
        public string DirectoryName = "";
        public string FileName = "";
        public int PiecesLength = 0;
        public bool PrivateTorrent = false;
        public bool MovieOnly = false;
        public string AnnounceURL = "";

        public bool TorrentPrepared = false;

        public Torrent(MultiLangMsg messages)
        {
            this.messages = messages;
#if (DEBUG)
            AnnounceURL = "http://announce.buran.pl/announce";
            AnnounceURL = "udp://thetracker.org/announce";
#endif
        }

        public void MakeTorrentData()
        {
            if (AnnounceURL.Equals("")) { return; }
            
            DirectoryInfo directoryInfo = null;
            MultipleFilesStream filesMFS = null;

            try
            {
                if (Directory.Exists(DirectoryName))
                {
                    directoryInfo = new DirectoryInfo(DirectoryName);
                    torrentSaved.Info.Name = directoryInfo.Name;
                    if (TorrentFileName.Equals(""))
                    {
                        torrentSaved.FileName = directoryInfo.FullName + '\\' + torrentSaved.Info.Name + ".torrent";
                    }
                    else
                    {
                        torrentSaved.FileName = directoryInfo.FullName + '\\' + TorrentFileName;
                    }

                    if (MovieOnly)
                    {
                        filesFilter = new string[] { "*.mkv", "*.mp4", "*.avi" };
                    }
                    filesMFS = new MultipleFilesStream(directoryInfo, filesFilter);
                }
                else if (File.Exists(FileName))
                {
                    FileInfo fileInfo = new FileInfo(FileName);
                    directoryInfo = new DirectoryInfo(fileInfo.DirectoryName);
                    torrentSaved.Info.Name = fileInfo.Name;
                    if (TorrentFileName.Equals(""))
                    {
                        torrentSaved.FileName = directoryInfo.FullName + '\\' + torrentSaved.Info.Name + ".torrent";
                    }
                    else
                    {
                        torrentSaved.FileName = directoryInfo.FullName + '\\' + TorrentFileName;
                    }
                    List<string> files = new List<string>();
                    files.Add(fileInfo.FullName);
                    filesMFS = new MultipleFilesStream(files);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new ArgumentException(messages.GetMessage("FilesNotFound"));
            }

            if (PiecesLength <= 0)
            {
                torrentSaved.Info.PieceLength = GetPiecesLength(filesMFS.Length);
            }
            else
            {
                torrentSaved.Info.PieceLength = (long)Math.Pow(2, PiecesLength);
            }
            Console.WriteLine("{0}: {1}", messages.GetMessage("PieceLength"), torrentSaved.Info.PieceLength);

            Console.WriteLine("\n{0}: {1}", messages.GetMessage("FilesFound"), filesMFS.GetFilesInfo().Count());
            if (filesMFS.GetFilesInfo().Count() > 0)
            {
                int i = 0;
                torrentSaved.Info.torrentFiles = new TorrentFileStruct[filesMFS.GetFilesInfo().Count()];
                foreach (FileInfo fileInfo in filesMFS.GetFilesInfo())
                {
                    if (directoryInfo.FullName == fileInfo.DirectoryName)
                    {
                        torrentSaved.Info.torrentFiles[i].Path = fileInfo.Name;
                    }
                    else
                    {
                        torrentSaved.Info.torrentFiles[i].Path =
                            fileInfo.DirectoryName.Replace(directoryInfo.FullName + "\\", "") + "\\" + fileInfo.Name;
                    }
                    torrentSaved.Info.torrentFiles[i].Length = fileInfo.Length;
                    Console.WriteLine("{0} [{1}]", torrentSaved.Info.torrentFiles[i].Path, fileInfo.Length);
                    i++;
                }
            }

            using (MemoryStream piecesMemoryStream = new MemoryStream())
            {
                using (BinaryReader inputStreamReader = new BinaryReader(filesMFS))
                {
                    byte[] readBuffer = new Byte[torrentSaved.Info.PieceLength];
                    long readCounter, totalReadCounter = 0, toRead = filesMFS.Length;
                    while ((readCounter = inputStreamReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                    {
                        byte[] lastBuffer = new byte[readCounter];
                        if (readCounter < readBuffer.Length)
                        {
                            Array.Copy(readBuffer, 0, lastBuffer, 0, readCounter);
                        }
                        else
                        {
                            lastBuffer = readBuffer;
                        }
                        byte[] sha1Hash = (new SHA1CryptoServiceProvider()).ComputeHash(lastBuffer);
                        piecesMemoryStream.Write(sha1Hash, 0, sha1Hash.Length);
                        totalReadCounter += readCounter;

                        Console.Write("\r{0}... {1} / {2} [{3:p}]", messages.GetMessage("Progress"), totalReadCounter,
                            toRead, (double)totalReadCounter / toRead);
                    }
                }
                torrentSaved.Info.Pieces = piecesMemoryStream.ToArray();
            }

            if (PrivateTorrent)
                torrentSaved.Info.PrivateTorrent = 1;

            torrentSaved.Announce = AnnounceURL;

            TorrentPrepared = true;
        }

        public void SaveTorrentFile()
        {
            if (!TorrentPrepared) { return; }

            BDictionary torrent = new BDictionary();
            torrent.Value.Add(new BByteString("announce"), torrentSaved.GetAnnounce());
            torrent.Value.Add(new BByteString("info"), torrentSaved.GetInfo());
            torrent.Value.Add(new BByteString("creation date"), new BInteger(TimeUnix()));
            
            using (FileStream outputFileStream = new FileStream(torrentSaved.FileName, FileMode.Create, FileAccess.Write))
            {
                BObjectTransform objTransform = new BObjectTransform();
                objTransform.EncodeObject(torrent, outputFileStream);
            }
            Console.WriteLine("\n{0} [{1}]\n", messages.GetMessage("TorrentCreated"), torrentSaved.FileName);
        }

        public void CheckTracker()
        {
            try
            {
                WebRequest request = WebRequest.Create(AnnounceURL);

                Console.WriteLine("{0}: {1}", messages.GetMessage("Tracker"), AnnounceURL);
                Console.Write("{0}, {1} ... : ", messages.GetMessage("TrackerResponse"), 
                    messages.GetMessage("TrackerWaiting"));

                request.Method = "HEAD";
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                
                Console.WriteLine("{0} {1}", ((HttpWebResponse)response).StatusDescription,
                    response.Headers.Get("Content-Type"), responseFromServer);
                
                reader.Close();
                response.Close();
            }
            catch (NotSupportedException) { }
            catch (UriFormatException ex)
            {
                Console.WriteLine(messages.GetMessage("TrackerError"));
                Console.WriteLine("{0}: {1}", messages.GetMessage("ErrorMessage"), ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: {1}", messages.GetMessage("ErrorMessage"), ex.Message);
            }

        }

        private long TimeUnix()
        {
            TimeSpan span = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)span.TotalSeconds;
        }

        private long GetPiecesLength(long filesLength)
        {
            if (filesLength > (long)2 * Math.Pow(2, 30))
            {
                return (long)Math.Pow(2, 22);
            }
            else if (filesLength > (long)1 * Math.Pow(2, 30))
            {
                return (long)Math.Pow(2, 22);
            }
            else if (filesLength > (long)512 * Math.Pow(2, 20))
            {
                return (long)Math.Pow(2, 19);
            }
            else if (filesLength > (long)350 * Math.Pow(2, 20))
            {
                return (long)Math.Pow(2, 18);
            }
            else if (filesLength > (long)150 * Math.Pow(2, 20))
            {
                return (long)Math.Pow(2, 17);
            }
            else if (filesLength > (long)50 * Math.Pow(2, 20))
            {
                return (long)Math.Pow(2, 16);
            }
            else
            {
                return (long)Math.Pow(2, 15);
            }
        }
    }
}
