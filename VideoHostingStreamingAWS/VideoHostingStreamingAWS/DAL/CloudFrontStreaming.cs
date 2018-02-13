using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml;

namespace VideoHostingStreamingAWS.DAL
{
    /// <summary>
    /// Used to manage signed URL access to CloudFront
    /// From Amazon CloudFront Developer Guide, API Version 2016-09-29, article "Create a URL Signature Using C# and the .NET Framework"
    /// http://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/CreateSignatureInCSharp.html
    /// </summary>
    public class CloudFrontStreaming
    {
        /// <summary>
        /// Gets a signed URL from a private S3 bucket using CloudFront
        /// </summary>
        /// <returns></returns>
        public static string getSignedURL(string urlString)
        {
            string URL = string.Empty;

            //NEED TO REPLACE ALL OF THESE WITH YOUR POLICIES/KEYS
            string pathToPolicyStmnt = HttpContext.Current.Server.MapPath("~/DAL/PolicyStatement.json");
            string pathToPrivateKey = HttpContext.Current.Server.MapPath("~/DAL/PrivateKey.pem");
            string privateKeyId = ConfigurationManager.AppSettings["AWSCloudFrontPrivateKeyID"];

            StreamReader txtReader = new StreamReader(pathToPrivateKey);
            DateTime expiresOn = DateTime.Now.AddHours(24); //expires after 24 hours - modify as needed
            
            URL = Amazon.CloudFront.AmazonCloudFrontUrlSigner.GetCannedSignedURL(urlString, txtReader, privateKeyId, expiresOn);
            
            return URL;
        }

        /// <summary>
        /// Creates a URL Safe string
        /// </summary>
        /// <param name="bytes">Byte array to convert to Base64 string</param>
        /// <returns></returns>
        public static string ToUrlSafeBase64String(byte[] bytes)
        {
            return System.Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('=', '_')
                .Replace('/', '~');
        }

        /// <summary>
        /// Creates a new signed URL
        /// </summary>
        /// <param name="urlString"></param>
        /// <param name="durationUnits"></param>
        /// <param name="durationNumber"></param>
        /// <param name="pathToPolicyStmnt"></param>
        /// <param name="pathToPrivateKey"></param>
        /// <param name="privateKeyId"></param>
        /// <returns></returns>
        private static string CreateCannedPrivateURL(string urlString,
            string durationUnits, string durationNumber, string pathToPolicyStmnt,
            string pathToPrivateKey, string privateKeyId)
        {
            // args[] 0-thisMethod, 1-resourceUrl, 2-seconds-minutes-hours-days 
            // to expiration, 3-numberOfPreviousUnits, 4-pathToPolicyStmnt, 
            // 5-pathToPrivateKey, 6-PrivateKeyId

            TimeSpan timeSpanInterval = GetDuration(durationUnits, durationNumber);

            // Create the policy statement.
            string strPolicy = CreatePolicyStatement(pathToPolicyStmnt,
                urlString,
                DateTime.Now,
                DateTime.Now.Add(timeSpanInterval),
                "0.0.0.0/0");
            if ("Error!" == strPolicy) return "Invalid time frame." +
                "Start time cannot be greater than end time.";

            // Copy the expiration time defined by policy statement.
            string strExpiration = CopyExpirationTimeFromPolicy(strPolicy);

            // Read the policy into a byte buffer.
            byte[] bufferPolicy = Encoding.ASCII.GetBytes(strPolicy);

            // Initialize the SHA1CryptoServiceProvider object and hash the policy data.
            using (SHA1CryptoServiceProvider
                cryptoSHA1 = new SHA1CryptoServiceProvider())
            {
                bufferPolicy = cryptoSHA1.ComputeHash(bufferPolicy);

                // Initialize the RSACryptoServiceProvider object.
                RSACryptoServiceProvider providerRSA = new RSACryptoServiceProvider();
                XmlDocument xmlPrivateKey = new XmlDocument();

                // Load PrivateKey.xml, which you created by converting your 
                // .pem file to the XML format that the .NET framework uses.  
                // Several tools are available. 
                xmlPrivateKey.Load(pathToPrivateKey);

                // Format the RSACryptoServiceProvider providerRSA and 
                // create the signature.
                providerRSA.FromXmlString(xmlPrivateKey.InnerXml);
                RSAPKCS1SignatureFormatter rsaFormatter =
                    new RSAPKCS1SignatureFormatter(providerRSA);
                rsaFormatter.SetHashAlgorithm("SHA1");
                byte[] signedPolicyHash = rsaFormatter.CreateSignature(bufferPolicy);

                // Convert the signed policy to URL-safe base64 encoding and 
                // replace unsafe characters + = / with the safe characters - _ ~
                string strSignedPolicy = ToUrlSafeBase64String(signedPolicyHash);

                // Concatenate the URL, the timestamp, the signature, 
                // and the key pair ID to form the signed URL.
                return urlString +
                    "?Expires=" +
                    strExpiration +
                    "&Signature=" +
                    strSignedPolicy +
                    "&Key-Pair-Id=" +
                    privateKeyId;
            }
        }

        /// <summary>
        /// Creates a new policy statement for signed URLs
        /// </summary>
        /// <param name="policyStmnt"></param>
        /// <param name="resourceUrl"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        private static string CreatePolicyStatement(string policyStmnt, string resourceUrl,
                        DateTime startTime, DateTime endTime, string ipAddress)
        {
            // Create the policy statement.
            FileStream streamPolicy = new FileStream(policyStmnt, FileMode.Open, FileAccess.Read);
            using (StreamReader reader = new StreamReader(streamPolicy))
            {
                string strPolicy = reader.ReadToEnd();

                TimeSpan startTimeSpanFromNow = (startTime - DateTime.Now);
                TimeSpan endTimeSpanFromNow = (endTime - DateTime.Now);
                TimeSpan intervalStart =
                   (DateTime.UtcNow.Add(startTimeSpanFromNow)) -
                   new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                TimeSpan intervalEnd =
                   (DateTime.UtcNow.Add(endTimeSpanFromNow)) -
                   new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                int startTimestamp = (int)intervalStart.TotalSeconds; // START_TIME
                int endTimestamp = (int)intervalEnd.TotalSeconds;  // END_TIME

                if (startTimestamp > endTimestamp)
                    return "Error!";

                // Replace variables in the policy statement.
                strPolicy = strPolicy.Replace("RESOURCE", resourceUrl);
                strPolicy = strPolicy.Replace("START_TIME", startTimestamp.ToString());
                strPolicy = strPolicy.Replace("END_TIME", endTimestamp.ToString());
                strPolicy = strPolicy.Replace("IP_ADDRESS", ipAddress);
                strPolicy = strPolicy.Replace("EXPIRES", endTimestamp.ToString());
                return strPolicy;
            }
        }

        /// <summary>
        /// Gest the duration of the TTL for the signed URL
        /// </summary>
        /// <param name="units"></param>
        /// <param name="numUnits"></param>
        /// <returns></returns>
        private static TimeSpan GetDuration(string units, string numUnits)
        {
            TimeSpan timeSpanInterval = new TimeSpan();
            switch (units)
            {
                case "seconds":
                    timeSpanInterval = new TimeSpan(0, 0, 0, int.Parse(numUnits));
                    break;
                case "minutes":
                    timeSpanInterval = new TimeSpan(0, 0, int.Parse(numUnits), 0);
                    break;
                case "hours":
                    timeSpanInterval = new TimeSpan(0, int.Parse(numUnits), 0, 0);
                    break;
                case "days":
                    timeSpanInterval = new TimeSpan(int.Parse(numUnits), 0, 0, 0);
                    break;
                default:
                    Console.WriteLine("Invalid time units;" +
                       "use seconds, minutes, hours, or days");
                    break;
            }
            return timeSpanInterval;
        }

        /// <summary>
        /// Gets the duration of the TTL for the specified units (seconds, minutes, hours, or days)
        /// </summary>
        /// <param name="durationUnits">The units to create, as seconds, minutes, hours, or days</param>
        /// <param name="startIntervalFromNow"></param>
        /// <returns></returns>
        private static TimeSpan GetDurationByUnits(string durationUnits,
           string startIntervalFromNow)
        {
            switch (durationUnits)
            {
                case "seconds":
                    return new TimeSpan(0, 0, int.Parse(startIntervalFromNow));
                case "minutes":
                    return new TimeSpan(0, int.Parse(startIntervalFromNow), 0);
                case "hours":
                    return new TimeSpan(int.Parse(startIntervalFromNow), 0, 0);
                case "days":
                    return new TimeSpan(int.Parse(startIntervalFromNow), 0, 0, 0);
                default:
                    return new TimeSpan(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Copies the expiration time from the provided policy statement
        /// </summary>
        /// <param name="policyStatement"></param>
        /// <returns></returns>
        private static string CopyExpirationTimeFromPolicy(string policyStatement)
        {
            int startExpiration = policyStatement.IndexOf("EpochTime");
            string strExpirationRough = policyStatement.Substring(startExpiration +
               "EpochTime".Length);
            char[] digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

            List<char> listDigits = new List<char>(digits);
            StringBuilder buildExpiration = new StringBuilder(20);

            foreach (char c in strExpirationRough)
            {
                if (listDigits.Contains(c))
                    buildExpiration.Append(c);
            }
            return buildExpiration.ToString();
        }
    }
}