using Amazon.S3;
using Amazon.S3.Model;
using System.Configuration;
using System.IO;
using System.Web.Mvc;
using VideoHostingStreamingAWS.Models;

namespace VideoHostingStreamingAWS.Controllers
{
    public class HomeController : Controller
    {
        //IAM Access key for service user
        private string accessID = ConfigurationManager.AppSettings["AWSS3AccessID"];
        private string privateAccessKey = ConfigurationManager.AppSettings["AWSS3PrivateAccessKey"];
        
        /// <summary>
        /// Used as the main home page/launch point to other pages
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Used for getting information direct from a public S3 bucket
        /// </summary>
        /// <returns></returns>
        public ActionResult S3Public()
        {
            HomeViewModel model = new HomeViewModel();

            //pull File from Bucket - fill in with full path of bucket and filename
            model.URL = "https://s3.amazonaws.com/BUCKETNAME/FILENAME.mp4";

            return View(model);
        }

        /// <summary>
        /// Pulls data from a private S3 bucket using IAM user credentials
        /// </summary>
        /// <returns></returns>
        public ActionResult S3Private()
        {
            HomeViewModel model = new HomeViewModel();

            //pull from a private S3 bucket - using an IAM user role
            model.URL = "https://s3.amazonaws.com/BUCKETNAME/FILENAME.mp4";

            //file to grab
            string keyName = "FILENAME.mp4";
            string dest = Path.Combine(HttpContext.Server.MapPath("~/OPTIONAL_FOLDER/"), keyName);

            //create new S3 Client to grab file
            using (AmazonS3Client s3client = new AmazonS3Client(accessID, privateAccessKey))
            {
                using (GetObjectResponse myObjStr = s3client.GetObject("BUCKETNAME", keyName))
                {
                    if (!System.IO.File.Exists(dest))
                    {
                        myObjStr.WriteResponseStreamToFile(dest);
                    }
                }
            }

            //show the newly grabbed video the user - must use relative path
            if (System.IO.File.Exists(dest))
            {
                model.URL = "../OPTIONAL_FOLDER/" + keyName;
            }
            return View(model);
        }

        /// <summary>
        /// Used for CloudFront when using signed URLs
        /// </summary>
        /// <returns></returns>
        public ActionResult CloudFrontPrivate()
        {
            HomeViewModel model = new HomeViewModel();

            //int startTimeSecs = 10; //the number of seconds into the video that should start playing
            //int endTimeSecs = 20; //the end time of the video snippet to play
            string urlString = ConfigurationManager.AppSettings["AWSCloudFrontURL"] + "FILENAME.mp4"; 

            string signedURL = DAL.CloudFrontStreaming.getSignedURL(urlString);
            //if you need to change the start and end time
            //string URL = String.Concat(signedURL, "#t=",startTimeSecs,",",endTimeSecs);
            model.URL = signedURL;
            model.SourceType = "video/mp4"; //MP4 format
                             //"application/x-mpegURL"; //HLS format

            return View(model);
        }

        /// <summary>
        /// Using a public CloudFront distro with a private S3 bucket
        /// </summary>
        /// <returns></returns>
        public ActionResult CloudFrontPublic()
        {
            HomeViewModel model = new HomeViewModel();

            //File to display
            model.URL = ConfigurationManager.AppSettings["AWSCloudFrontURL"] + "FILENAME.mp4";
            
            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "About";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Contact Information";

            return View();
        }
    }
}