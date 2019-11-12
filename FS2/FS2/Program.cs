using System;
using System.IO;
using System.Security.Permissions;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Json;
using System.Text;

namespace MultiWatcher
// ConsoleApplication, which monitors TXT-files in multiple folders. 
// Inspired by:
// http://msdn.microsoft.com/en-us/library/system.io.filesystemeventargs(v=vs.100).aspx

{
    public class Watchers
    {
        private static string hostName = ConfigurationManager.AppSettings["HostName"].ToString();

        public static void Main()
        {
            Run();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public static void Run()
        {
            string[] args = System.Environment.GetCommandLineArgs();

            string SD = ConfigurationManager.AppSettings["SearchDrive"].ToString();

            string[] DriveList = SD.Split('|');

            foreach(var item in DriveList)
            {
                Watch(item);
            }

            // Wait for the user to quit the program.
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;
        }
        private static void Watch(string watch_folder)
        {
            // Create a new FileSystemWatcher and set its properties.
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = watch_folder;

            // watcher가 하위 폴더까지 검색할수있는 권한 설정
            watcher.IncludeSubdirectories = true;

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName;

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
            
            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        private static bool ExceptionWord(FileSystemEventArgs e)
        {
            string ExceptString = ConfigurationManager.AppSettings["ExceptionWord"].ToString();

            string[] WordList = ExceptString.Split('|');

            foreach (var item in WordList)
            {
                if (e.FullPath.ToLower().IndexOf(item) != -1) return false;
            }

            return true;
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {

            if (ExceptionWord(e) == false) return;

            string log = string.Format("[보안] {3} - File: {0} {1} {2}", e.FullPath, e.ChangeType, DateTime.Now, hostName);
            Console.WriteLine(log);
            
            Log(log);

            //SMS 전송
            SendSms(log);
        }

       
        private static void OnRenamed(object source, RenamedEventArgs e)
        {

            if (ExceptionWord(e) == false) return;

            string log = string.Format("[보안] {3} - File: {0} renamed to {1} DATA : {2}", e.OldFullPath, e.FullPath, DateTime.Now, hostName);
            
            Console.WriteLine(log);
            
            Log(log);

            //SMS 전송
            SendSms(log);
        }


        #region Log
        private static void Log(string str)
        {
            string FilePath = @"D:\FileWatcherLog\Log.txt";
            string DirPath = @"D:\FileWatcherLog";

            DirectoryInfo di = new DirectoryInfo(DirPath);
            FileInfo fi = new FileInfo(FilePath);

            try
            {
                if (di.Exists != true) Directory.CreateDirectory(DirPath);

                if (fi.Exists != true)
                {
                    using (StreamWriter sw = new StreamWriter(FilePath))
                    {

                        sw.WriteLine(str);
                        sw.Close();
                    }
                }
                else
                {
                    using (StreamWriter sw = File.AppendText(FilePath))
                    {

                        sw.WriteLine(str);
                        sw.Close();
                    }
                }
            }
            catch (Exception e)
            {
                
            }
        }
        #endregion

        #region SMS
        private static void SendSms(string msg)
        {
            // 디버그 모드시에 SMS 전송 안됨
            string isDebug = ConfigurationManager.AppSettings["Debug"].ToString();
            if (isDebug.Equals("Y")) return;

            string Phone = ConfigurationManager.AppSettings["PhoneNum"].ToString();

            string[] PhoneList = Phone.Split('|');

            foreach (var item in PhoneList)
            {
                //Sms(msg, item);
            }
        }

        private static void Sms(string msg,string phone)
        {

            string SMSKey = ConfigurationManager.AppSettings["SMSKey"].ToString();
            string PartCode = ConfigurationManager.AppSettings["PartCode"].ToString();
            string SMSUrl = ConfigurationManager.AppSettings["SMSUrl"].ToString();
            string CallBackNum = ConfigurationManager.AppSettings["CallBackNum"].ToString();

            //json 값 생성 
            JsonObjectCollection res = new JsonObjectCollection();
            res.Add(new JsonStringValue("callbackNo", CallBackNum));
            res.Add(new JsonStringValue("message", msg));
            res.Add(new JsonStringValue("phoneNo", phone));
            res.Add(new JsonStringValue("reservationNo", ""));
            res.Add(new JsonStringValue("subject", "[보안] 파일 변경 감지"));
            res.Add(new JsonStringValue("systemCD", PartCode));


            // 전송할 uri
            WebRequest request = WebRequest.Create(SMSUrl);

            // post 전송할 헤드 설정
            request.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(res.ToString());
            request.ContentType = " text/json";
            // 서버 키 입렵
            request.Headers.Add("openapikey", SMSKey);
            request.ContentLength = byteArray.Length;

            // stream으로 변환한뒤 json 전송
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            WebResponse response = request.GetResponse();
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            Console.WriteLine(responseFromServer);
            reader.Close();
            dataStream.Close();
            response.Close();

        }
        #endregion
    }
}