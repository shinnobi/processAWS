using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LeanWork.IO.FileSystem;
using Newtonsoft.Json;

namespace ProcessAWS
{
    public partial class ProcessTP1 : Form
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        string SourceFolder = ConfigurationManager.AppSettings["SourceFolder"];
        string JsonFolder = ConfigurationManager.AppSettings["JsonFolder"];
        string DesFolder = ConfigurationManager.AppSettings["DesFolder"];
        ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();
        private bool isError = false;
        private DateTime beginError = new DateTime();
        int projectID = 3;

        public ProcessTP1()
        {
            InitializeComponent();
        }

        private void ProcessTP1_Load(object sender, EventArgs e)
        {
            //luc khoi dong thi lay lai file cu trong vong 2 tieng
            Thread t = new Thread(() => ProcessErrorWatcher(DateTime.Now.AddHours(-3))) { IsBackground = true };
            t.Start();

            // start watcher
            RunRecoveringWatcher();


            // xu ly queue
            Thread processQueue = new Thread(deQueue) { IsBackground = true };
            processQueue.Start();

            Thread CopyFile = new Thread(ThreadCopyFile) { IsBackground = true };
            CopyFile.Start();
        }
        public void RunRecoveringWatcher()
        {

            var watcher = new RecoveringFileSystemWatcher(SourceFolder);

            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.FileName
                                           | NotifyFilters.Size;
            watcher.Filter = "*.txt";
            watcher.All += (_, e) =>
            {
                if (isError == true)
                {
                    isError = false;
                    new Thread(() =>
                    {
                        ProcessErrorWatcher(beginError);
                    }).Start();

                }
                if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Renamed || e.ChangeType == WatcherChangeTypes.Created)
                {
                    //new Thread(() =>
                    //{
                    OnChange(e.FullPath);
                    //}).Start();


                }
            };

            watcher.Error += (_, e) =>
            {
                if (isError == false)
                {
                    beginError = DateTime.Now;
                    isError = true;
                }
                BeginInvoke((MethodInvoker)delegate
                {

                });

            };
            watcher.EnableRaisingEvents = true;
            watcher.OrderByOldestFirst = false;
        }

        public void OnChange(string path)
        {
            if (!Queue.Contains(path))
            {
                Queue.Enqueue(path);
            }
            //if(checkFileName(path)) {
            //    Queue.Enqueue(path);
            //}
            //else {
            //    log.Debug("file " + path + " không đúng định dạng");
            //    return;
            //}
        }
        //showlog
        public void ShowLog(string data)
        {
            if (rtb_Log.Lines.Count() > 1000)
                rtb_Log.Clear();

            rtb_Log.AppendText(data);
        }

        public void ProcessErrorWatcher(DateTime beginError)
        {
            Invoke((MethodInvoker)delegate
            {
                ShowLog(String.Format("Xử lý file cũ...\n"));
            });
            var lstMissingFiles = getMissingFiles(SourceFolder,beginError);
            foreach (var file in lstMissingFiles)
            {

                if (!Queue.Contains(file.FullName))
                {
                    //if(checkFileName(file.FullName)) {

                    //}
                    Queue.Enqueue(file.FullName);
                }
            }
        }
        //ham get file bi thieu luc network down

        public FileInfo[] getMissingFiles(string folder,DateTime beginError)
        {
            //var lstFileInfos = new List<FileInfo>();
            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            FileInfo[] lstFiles = dirInfo.EnumerateFiles("*.json", SearchOption.AllDirectories)
            .AsParallel()
            .Where(x => x.LastWriteTime >= beginError).ToArray();
            return lstFiles;
        }

        public void deQueue()
        {
            while (true)
            {
                if (Queue.Count != 0)
                {
                    string fn;
                    Queue.TryDequeue(out fn);
                    if (!IsFileLocked(fn))
                    {
                        ProcessOne(fn);
                    }
                    else
                    {
                        Queue.Enqueue(fn);
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        Thread Write2Rain10m;
        Thread Write2RObs;
        Thread Write2Rain19h;
        Thread Write2Rain24h;

        // ham xu ly 1 file
        public void ProcessOne(string path)
        {
            string StationNo;
            DateTime datetimeEnd;
            DateTime datetimeBegin;
            float rain24h;
            float rain19h;
            float value;
            float rainObs;
           


            try
            {
                string myString = System.IO.File.ReadAllText(path);
                myString = Regex.Replace(myString.Replace(System.Environment.NewLine, " "), " {2,}", " ");
                String[] data = myString.Split(' ');
                if (checkIfNull(path, data[0])) { return; };
                StationNo = data[0];
                var Obs = float.Parse(data[3]);
                if (data[2].Substring(8, 2) == "24")
                {

                    datetimeEnd = DateTime.ParseExact(data[2].Remove(8, 2).Insert(8, "00"), "yyyyMMddHHmmss", null).AddDays(1);
                }
                else
                {
                    datetimeEnd = DateTime.ParseExact(data[2], "yyyyMMddHHmmss", null);
                }

                datetimeBegin = datetimeEnd.AddMinutes(Obs * -1);
                //obj.PrjID = 7;
                rainObs = float.Parse(data[6]);
                if (checkIfNull(path, data[4])) { return; };
                rain24h = float.Parse(data[4]);
                if (checkIfNull(path, data[5])) { return; };
                rain19h = float.Parse(data[5]);
                if (checkIfNull(path, data[7])) { return; };




                var rain10minJsonFile = JsonFolder + "/" + projectID + "_Rain10m_" + datetimeEnd.ToString("yyyyMMddHH") + ".json";


                Write2Rain10m = new Thread(() =>
                {
                try {
                        var lstRain10m = Deserialize(rain10minJsonFile);
                        for (int i = 1; i <= Obs / 10; i++)
                        {
                            if (checkIfNull(path, data[i + 7])) { return; }
                            value = float.Parse(data[i + 7]) / 10;
                            var obj10m = createObj(StationNo, projectID, datetimeBegin.AddMinutes(10 * i), value);

                            if (!checkObjectExits(lstRain10m, obj10m))
                            {
                                lstRain10m.Add(obj10m);
                            }


                        }
                        Wirte2Json(rain10minJsonFile, lstRain10m, path);
                    }
                 catch (Exception ex)
                    {
                        Invoke((MethodInvoker)delegate
                        {
                            ShowLog(String.Format("Không đọc được file: " + path + "\n"));
                            log.Debug("file " + path + " không đúng định dạng " + ex.Message);
                        });
                        return;
                    }

                })
                { IsBackground = true };

                Write2Rain10m.Start();
                Write2Rain10m.Join();

                var rain24hJsonFile = JsonFolder + "/" + projectID + "_Rain24h_" + datetimeEnd.ToString("yyyyMMddHH") + ".json";
                var obj24h = createObj(StationNo, projectID, datetimeEnd, rain24h);
                
                var rain19hJsonFile = JsonFolder + "/" + projectID + "_Rain19h_" + datetimeEnd.ToString("yyyyMMddHH") + ".json";
                var obj19h = createObj(StationNo, projectID, datetimeEnd, rain19h);


                Write2Rain24h = new Thread(() => {
                    var lstRain24h = Deserialize(rain24hJsonFile);
                    if (!checkObjectExits(lstRain24h, obj24h))
                    {
                        lstRain24h.Add(obj24h);
                        Wirte2Json(rain24hJsonFile, lstRain24h, path);
                    }
                    
                    }) { IsBackground = true };
                Write2Rain24h.Start();
                Write2Rain24h.Join();
                Write2Rain19h = new Thread(() => {
                    var lstRain19h = Deserialize(rain19hJsonFile);
                    if (!checkObjectExits(lstRain19h, obj19h))
                    {
                        lstRain19h.Add(obj19h);
                        Wirte2Json(rain19hJsonFile, lstRain19h, path);
                    }
                }) { IsBackground = true };
                Write2Rain19h.Start();
                Write2Rain19h.Join();

                if (Obs != 10)
                {
                    var ObsJsonFile = JsonFolder + "/" + projectID + "_Rain1h_" + datetimeEnd.ToString("yyyyMMddHH") + ".json";
                    if(Obs !=60) {
                        ObsJsonFile =JsonFolder + "/" + projectID + "_Rain" + Obs.ToString() + "m_" + datetimeEnd.ToString("yyyyMMddHH") + ".json";
                    }
                    var objObs = createObj(StationNo, projectID, datetimeEnd, rainObs);

                    Write2RObs = new Thread(() =>
                    {
                        var lstrainObs = Deserialize(ObsJsonFile);
                        if (!checkObjectExits(lstrainObs, objObs))
                        {
                            lstrainObs.Add(objObs);
                            Wirte2Json(ObsJsonFile, lstrainObs, path);
                        }
                    }) { IsBackground = true };
                    Write2RObs.Start();
                    Write2RObs.Join();
                    //new Thread(() =>
                    //{
                    //    CopyFiles(ObsJsonFile, DesFolder);


                    //}).Start();
                }


                //new Thread(() =>
                //{
                //    CopyFiles(rain10minJsonFile, DesFolder);
                //    CopyFiles(rain24hJsonFile, DesFolder);
                //    CopyFiles(rain19hJsonFile, DesFolder);

                //}).Start();
            }

            catch (Exception ex)
            {
                Invoke((MethodInvoker)delegate
                {
                    ShowLog(String.Format("Không đọc được file: " + path + "\n"));
                    log.Debug("file " + path + " không đúng định dạng " + ex.Message);
                });
                return;
            }
            Invoke((MethodInvoker)delegate
            {
                ShowLog(String.Format("Đã xử lý xong file: " + path + "\n"));
            });



        }
        public void ThreadCopyFile() {
            while(true) {
                var lstMissingFiles = getMissingFiles(JsonFolder, DateTime.Now.AddHours(-2));
                foreach (var file in lstMissingFiles)
                {
                    CopyFiles(file.FullName, DesFolder);


                }
                Thread.Sleep(3 * 60 * 1000);
            }
          
          

        }

        public List<JSON> Deserialize (string jsonFile) {
            if (!File.Exists(jsonFile))
            {
                using (File.Create(jsonFile))
                {
                    return new List<JSON>();
                }
            }
            else {
                string jsonData = File.ReadAllText(jsonFile);
                var desirializedData = JsonConvert.DeserializeObject<List<JSON>>(jsonData);
                return desirializedData;
            }
            
        }


        public JSON createObj(string StationNo, int ProjectID, DateTime datatime, float Value)
        {
            var obj = new JSON();
            obj.StationNo = StationNo;
            obj.ProjectID = ProjectID;
            obj.Year = datatime.Year;
            obj.Month = datatime.Month;
            obj.Day = datatime.Day;
            obj.Hour = datatime.Hour;
            obj.Minute = datatime.Minute;
            obj.Seconds = datatime.Second;
            obj.Value = Value;
            obj.SearchDatetime = convertDateTime2Int(datatime.Year, datatime.Month, datatime.Day, datatime.Hour, datatime.Minute);
            return obj;
        }

        public bool checkIfNull(string path, string txt)
        {
            if (txt == null || txt == "")
            {
                log.Debug("file " + path + " không đúng định dạng");
                return true;
            }
            return false;
        }

     

        //ham kiem tra object ton tai hay khong 
        public bool checkObjectExits(List<JSON> desirializedData, JSON receiveObj)
        { 

                var myOrder = desirializedData.Any(x => x.StationNo == receiveObj.StationNo && x.SearchDatetime == receiveObj.SearchDatetime && x.Value == receiveObj.Value);
             
                return myOrder;

        }

        //ham ghi vao file json

        public void Wirte2Json(string jsonFile, List<JSON> lstJSON, string path)
        {
            try
            {
            if(lstJSON==null) { return; }
                //List<JSON> lstJSON = new List<JSON>();
            
                var jsonData = JsonConvert.SerializeObject(lstJSON, Formatting.Indented);
                using (var fs = TryCreateFileStream(jsonFile, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                               FileShare.None))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(jsonData);
                    fs.Write(bytes, 0, bytes.Length);

                }
                //File.WriteAllText(jsonFile, jsonData);


            }
            catch (Exception ex)
            {
                Invoke((MethodInvoker)delegate
                {
                    ShowLog(String.Format("Không ghi được xuống file JSON: " + path + "\n"));
                });

                if (!Queue.Contains(path))
                {

                    Queue.Enqueue(path);
                }
                Write2Rain10m.Abort();
                Write2Rain19h.Abort();
                Write2Rain24h.Abort();
                Write2RObs.Abort();
            }

        }





        // kiem tra file co bi chiem dung khong
        public bool IsFileLocked(string fn)
        {
            try
            {
                var fileInfo = new FileInfo(fn);
                using (fileInfo.OpenRead())
                {
                    //
                }
            }
            catch
            {
                return true;
            }
            return false;
        }


        private FileStream TryCreateFileStream(string filename, FileMode fileMode, FileAccess fileAccess,
          FileShare fileShare)
        {
            while (true)
            {
                try
                {
                    return new FileStream(filename, fileMode, fileAccess, fileShare);
                }
                catch (IOException)
                {
                    Thread.Sleep(1000);
                }
            }
        }
        public long convertDateTime2Int(int year, int month, int day, int hour, int minute)
        {


            return (long)100000000 * year + 1000000 * month + 10000 * day + 100 * hour + minute;
        }

        //gửi dữ liệu
        private void CopyFiles(string fileFullName, string desPath)
        {
  
                int i = 0;
                if (System.IO.Directory.Exists(desPath))
                {

                    var fileName = System.IO.Path.GetFileName(fileFullName);
                    var destFile = System.IO.Path.Combine(desPath, fileName);
                    while (i < 5)
                    {
                        try
                        {
                            if (IsFileLocked(fileFullName))
                            {
                                Thread.Sleep(2000);
                                i++;
                                continue;
                            }
                            else
                            {
                                File.Copy(fileFullName, destFile, true);

                                return;
                            }

                        }
                        catch (Exception ex)
                        {
                            Invoke((MethodInvoker)delegate
                            {
                                ShowLog(String.Format("File đang bị chiếm thử copy lại... \n"));
                            });
                            Thread.Sleep(2000);
                            i++;

                        }

                    }

                }
                else
                {
                    Invoke((MethodInvoker)delegate
                    {
                        ShowLog(String.Format("Thu mục Des Không tồn tại \n"));
                    });
                }


        }

    }
}
