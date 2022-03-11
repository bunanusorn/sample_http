using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net.NetworkInformation;

namespace sea_it_mes_log
{
    public static class MyComputer
    {
        public static string GetComputerNameAndIP()
        {
            try
            {
                string PCName = Environment.MachineName;
                IPAddress[] arrIP = Dns.GetHostAddresses(Environment.MachineName);
                string IP = "";
                for (int i = 0; i < arrIP.Length; i++)
                {
                    if (i == 0)
                    {
                        IP = arrIP[i].ToString();
                    }
                    else
                    {
                        IP = IP + ", " + arrIP[i].ToString();
                    }

                }

                return $"{PCName}, {IP}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }

    public static class KeyGenerator
    {
        internal static readonly char[] chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

        public static string GetUniqueKey(int size)
        {
            byte[] data = new byte[4 * size];
            using (var crypto = RandomNumberGenerator.Create())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                var rnd = BitConverter.ToUInt32(data, i * 4);
                var idx = rnd % chars.Length;

                result.Append(chars[idx]);
            }

            return result.ToString();
        }

        public static string GetUniqueKeyOriginal_BIASED(int size)
        {
            char[] chars =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[size];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);
            }
            StringBuilder result = new StringBuilder(size);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }
    }

    public static class MyTextFile
    {
        public static bool CreateText(string path, string text)
        {
            try
            {
                if (!File.Exists(path))
                {
                    TextWriter tw = new StreamWriter(path);
                    tw.WriteLine(text);
                    tw.WriteLine(DateTime.Now.ToString());
                    tw.WriteLine("---------------------");
                    tw.Close();
                }
                else if (File.Exists(path))
                {
                    using (var tw = new StreamWriter(path, true))
                    {

                        tw.WriteLine(text);
                        tw.WriteLine(DateTime.Now.ToString());
                        tw.WriteLine("---------------------");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool WriteToFile(string path, string text)
        {
            try
            {
                if (!File.Exists(path))
                {
                    TextWriter tw = new StreamWriter(path);
                    tw.WriteLine(text + Environment.NewLine + $"\n{DateTime.Now.ToString()}\n------\n");
                    tw.Close();
                }
                else if (File.Exists(path))
                {
                    string content = File.ReadAllText(path);
                    content = text + Environment.NewLine + $"\n{DateTime.Now.ToString()}\n------\n" + content;
                    File.WriteAllText(path, content);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

    public static class MyHttp
    {
        public static bool Get(string token_name, string token_key, string url, out string ReturnResult, out string ErrorMsg)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            if (token_name != "")
            {
                request.Headers[token_name] = token_key;
            }

            try
            {
                WebResponse response = request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                    ReturnResult = reader.ReadToEnd();
                }

                ErrorMsg = "";
                return true;
            }
            catch (Exception ex)
            {
                ErrorMsg = ex.Message;
                ReturnResult = "";
                return false;
            }
        }


        public static bool Post(string token_name, string token_key, string url, string data, out string result, out string msg)
        {
            try
            {
                byte[] arrdata = Encoding.ASCII.GetBytes($"{data}");

                WebRequest request = WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = arrdata.Length;
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(arrdata, 0, arrdata.Length);
                }


                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader sr99 = new StreamReader(stream))
                        {
                            result = sr99.ReadToEnd();
                        }
                    }
                }

                msg = "";
                return true;
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                result = ex.Message;
                return false;
            }
        }

        public static bool PingHost(string nameOrAddress, out string Status, out string pingTime, out string pingTTL, out string pingBytes, out string pingStatus)
        {

            Ping pingSender = new Ping();
            try
            {
                PingOptions options = new PingOptions();

                options.DontFragment = true;
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 10000;
                PingReply reply = pingSender.Send(nameOrAddress, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                {

                    Status = $"Status: {reply.Status}" +
                        $"\n - RoundTrip time (time): {reply.RoundtripTime}ms" +
                        $"\n - Time to live (TTL): {reply.Options.Ttl}" +
                        $"\n - Buffer size (bytes): {reply.Buffer.Length}";

                    pingTime = reply.RoundtripTime.ToString();
                    pingTTL = reply.Options.Ttl.ToString();
                    pingBytes = reply.Buffer.Length.ToString();
                    pingStatus = $"{reply.Status}";
                    return true;
                }
                else
                {
                    Status = $"Status: {reply.Status}";
                    pingTime = "";
                    pingTTL = "";
                    pingBytes = "";
                    pingStatus = $"{reply.Status}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                pingTime = "";
                pingTTL = "";
                pingBytes = "";
                Status = ex.Message;
                pingStatus = "";
                return false;
            }
            finally
            {
                if (pingSender != null)
                {
                    pingSender.Dispose();
                }
            }
        }
    }

}