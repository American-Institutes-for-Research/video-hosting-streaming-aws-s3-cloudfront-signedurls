using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VideoHostingStreamingAWS.Models
{
    public class HomeViewModel
    {
        public bool UserLoggedIn { get; set; }
        public string URL { get; set; } //the link to the video for the src tag in the player
        public string SourceType { get; set; } //the type of video for the type tag in the player
        public string FileName { get; set; } //the file name of the video being uploaded
        public string FilePath { get; set; } //the file path for the video being uploaded
        
        /// <summary>
        /// Default constructor
        /// </summary>
        public HomeViewModel()
        {
            UserLoggedIn = false;
        }
    }
}