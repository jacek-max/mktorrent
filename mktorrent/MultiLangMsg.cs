using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace mktorrent
{
    class MultiLangMsg
    {
        private Dictionary<string, string> messages = new Dictionary<string, string>();

        public MultiLangMsg()
        {
            Assembly assembly = Assembly.Load("mktorrent");
            ResourceManager rm = new ResourceManager("mktorrent.Lang.langres", assembly);

            messages.Add("Info01", rm.GetString("Info01"));
            messages.Add("Info02", rm.GetString("Info02"));
            messages.Add("Info03", rm.GetString("Info03"));
            messages.Add("Info04", rm.GetString("Info04"));
            messages.Add("Info05", rm.GetString("Info05"));
            messages.Add("Info06", rm.GetString("Info06"));
            messages.Add("Info07", rm.GetString("Info07"));
            messages.Add("Info08", rm.GetString("Info08"));
            messages.Add("Info09", rm.GetString("Info09"));
            messages.Add("Info10", rm.GetString("Info10"));
            messages.Add("Info11", rm.GetString("Info11"));
            messages.Add("Info12", rm.GetString("Info12"));
            messages.Add("Info13", rm.GetString("Info13"));
            messages.Add("Info14", rm.GetString("Info14"));
            messages.Add("Info15", rm.GetString("Info15"));
            messages.Add("Info16", rm.GetString("Info16"));

            messages.Add("ErrorMessage", rm.GetString("ErrorMessage"));
            messages.Add("FilesFound", rm.GetString("FilesFound"));
            messages.Add("FilesNotFound", rm.GetString("FilesNotFound"));
            messages.Add("PieceLength", rm.GetString("PieceLength"));
            messages.Add("Progress", rm.GetString("Progress"));
            messages.Add("TorrentCreated", rm.GetString("TorrentCreated"));
            messages.Add("TrackerResponse", rm.GetString("TrackerResponse"));
            messages.Add("TrackerWaiting", rm.GetString("TrackerWaiting"));
            messages.Add("TrackerError", rm.GetString("TrackerError"));
            messages.Add("NoStreamsProvided", rm.GetString("NoStreamsProvided"));

            messages.Add("PrivateFlag", rm.GetString("PrivateFlag"));
            messages.Add("MovieOnly", rm.GetString("MovieOnly"));
            messages.Add("Tracker", rm.GetString("Tracker"));
            messages.Add("FileName", rm.GetString("FileName"));
            messages.Add("Folder", rm.GetString("Folder"));
            messages.Add("File", rm.GetString("File"));

            messages.Add("StrTrue", rm.GetString("StrTrue"));
            messages.Add("StrFalse", rm.GetString("StrFalse"));
        }

        public string GetMessage(string key)
        {
            return messages[key];
        }
    }
}
