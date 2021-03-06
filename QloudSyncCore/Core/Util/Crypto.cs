using System;
using System.Text;
using System.Security.Cryptography;
using GreenQloud.Model;
using System.IO;

namespace GreenQloud
{
    public class Crypto
    {
        public static string GetHMACbase64(string secretkey, string url, bool urlEncode)
        {
            byte[] key = new Byte[64];
            string b64 = null;
            key = Encoding.UTF8.GetBytes(secretkey);
            HMACSHA1 myhash1 = new HMACSHA1(key);
            byte[] urlbytes = Encoding.UTF8.GetBytes(url);         
            byte[] hashValue = myhash1.ComputeHash(urlbytes);
            b64 = Convert.ToBase64String(hashValue);
            
            if (urlEncode)            
                return new UrlEncode().Encode(b64);
            else                            
                return b64;
        }

        public static string Getbase64(string value)
        {
            byte[] key = new Byte[64];
            key = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(key);
        }

        public string md5hash (RepositoryItem item) {
            if (item.IsFolder) {
                return "d41d8cd98f00b204e9800998ecf8427e";//default folder etag for zero bytes inode generated from s3.
            } 
            return md5hash (item.LocalAbsolutePath);
        }
        public string md5hash (string input)
        {
            string md5hash;
            try {
                FileStream fs = System.IO.File.Open (input, FileMode.Open);
                md5hash = BitConverter.ToString (new MD5CryptoServiceProvider().ComputeHash (fs)).Replace ("-", string.Empty).ToLower ();
                fs.Close ();
            } catch {
                md5hash = string.Empty;
            }
            return md5hash;
        }
    }
}

