using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Reflection;

namespace mktorrent
{
    class Program
    {
        MultiLangMsg messages = new MultiLangMsg();

        private bool Run(string[] args)
        {
            Torrent torrent = new Torrent(messages);
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "-l":
                            torrent.PiecesLength = int.Parse(args[i + 1]);
                            i++;
                            Console.WriteLine("{0}: 2^{1}", messages.GetMessage("PieceLength"), torrent.PiecesLength);
                            break;
                        case "-p":
                            torrent.PrivateTorrent = true;
                            Console.WriteLine("{0}: {1}", messages.GetMessage("PrivateFlag"),
                                torrent.PrivateTorrent ? messages.GetMessage("StrTrue") : messages.GetMessage("StrFalse"));
                            break;
                        case "-movieonly":
                            torrent.MovieOnly = true;
                            Console.WriteLine(messages.GetMessage("MovieOnly"));
                            break;
                        case "-a":
                            torrent.AnnounceURL = args[i + 1];
                            i++;
                            Console.WriteLine("{0}: {1}", messages.GetMessage("Tracker"), torrent.AnnounceURL);
                            break;
                        case "-o":
                            torrent.TorrentFileName = args[i + 1];
                            i++;
                            Console.WriteLine("{0}: {1}", messages.GetMessage("FileName"), torrent.TorrentFileName);
                            break;
                        default:
                            if (args[i][args[i].Length - 1].Equals('"'))
                            {
                                args[i] = args[i].Substring(0, args[i].Length - 1);
                            }
                            if (args[i][args[i].Length - 1].Equals('\\'))
                            {
                                args[i] = args[i].Substring(0, args[i].Length - 1);
                            }
                            if (Directory.Exists(args[i]))
                            {
                                torrent.DirectoryName = args[i];
                                Console.WriteLine("{0}: {1}", messages.GetMessage("Folder"), torrent.DirectoryName);
                            }
                            else if (File.Exists(args[i]))
                            {
                                torrent.FileName = args[i];
                                Console.WriteLine("{0}: {1}", messages.GetMessage("File"), torrent.FileName);
                            }
                            break;
                    }
                }
                if ((!torrent.DirectoryName.Equals("")) || (!torrent.FileName.Equals("")))
                {
                    torrent.MakeTorrentData();
                    torrent.SaveTorrentFile();
                    torrent.CheckTracker();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: {1}", messages.GetMessage("ErrorMessage"), ex.Message);
            }
            return torrent.TorrentPrepared;
        }

        static void Main(string[] args)
        {
            Program program = new Program();
            if ((args.Length == 0) || (!program.Run(args)))
            {
                Console.WriteLine("\n{0}",program.messages.GetMessage("Info01"));
                Console.WriteLine(program.messages.GetMessage("Info02"));
                Console.WriteLine("          {0}", program.messages.GetMessage("Info03"));
                Console.WriteLine("          {0}", program.messages.GetMessage("Info04"));
                Console.WriteLine();
                Console.WriteLine(program.messages.GetMessage("Info05"));
                Console.WriteLine(program.messages.GetMessage("Info06"));
                Console.WriteLine("\"files/[..]\" - {0}", program.messages.GetMessage("Info07"));
                Console.WriteLine("               {0}", program.messages.GetMessage("Info08"));
                Console.WriteLine("-l 21        - {0}", program.messages.GetMessage("Info09"));
                Console.WriteLine("               {0}", program.messages.GetMessage("Info10"));
                Console.WriteLine("-p           - {0}", program.messages.GetMessage("Info11"));
                Console.WriteLine("               {0}", program.messages.GetMessage("Info12"));
                Console.WriteLine("-movieonly   - {0}", program.messages.GetMessage("Info13"));
                Console.WriteLine("               {0}", program.messages.GetMessage("Info14"));
                Console.WriteLine("-a http://[..]  - {0}", program.messages.GetMessage("Info15"));
                Console.WriteLine("-o file.torrent - {0}", program.messages.GetMessage("Info16"));
            }
        }
    }
}
