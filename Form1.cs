using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net.Mail;

namespace sea_it_mes_log
{
    public partial class Form1 : Form
    {
        private string http_response = "";

        //Hide close button
        private const int CP_NOCLOSE_BUTTON = 0x200;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                return myCp;
            }
        }

        private void InkTextboxSetText(TextBox textbox, string text)
        {
            textbox.Invoke((MethodInvoker)delegate
            {
                textbox.Text = text;
            });
        }

        //ตัวอย่างการใช้ Call API โดย HTTP
        private void RecordReport(MesLogTdcOnline retData)
        {
            bool statusTextFile = MyTextFile.WriteToFile($@"{txtTextfilePath.Text}{retData.factory}_MES_REPORT.txt", $"Detail : {retData.factory} {retData.line}, {retData.start_date}, {retData.total_time} s\n");
            bool statusPost = MyHttp.Post("", "", $"http://thbpoprodap-mes.delta.corp/mes_notify_prod/api/tdconline",
                $"id={retData.id}&function_name={retData.function_name}&line={retData.line}" +
                $"&factory={retData.factory}" +
                $"&error_detail={retData.error_detail}" +
                $"&start_date={retData.start_date}" +
                $"&total_time={retData.total_time}" +
                $"&sender={retData.sender}" +
                $"&ping_time={retData.ping_time}" +
                $"&ping_ttl={retData.ping_ttl}" +
                $"&ping_bytes={retData.ping_bytes}" +
                $"&description={retData.description}" +
                $"&ping_status={retData.ping_status}" +
                $"&test_result={retData.test_result}",
                out string postResult, out string postStatus);

            string msgTextFile = statusTextFile == false ? "TextFile...Error" : "TextFile...OK";
            string msgPost = statusPost == false ? "Post...Error" : "Post...OK";

            string Message = $"{DateTime.Now}{Environment.NewLine}{msgTextFile}{Environment.NewLine}{msgPost}{Environment.NewLine}{postResult}";
            InkTextboxSetText(txtResponse, Message);

        }


        public string CheckPing()
        {
            try
            {
                MesLogTdcOnline retData = new MesLogTdcOnline();
                Stopwatch stop_watch = new Stopwatch();
                bool testResult = true, limitTimeResult = true;
                string msgTestResult = "";
                string ip = "10.150.192.16";

                stop_watch.Start();
                retData.start_date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                testResult = MyHttp.PingHost(ip, out msgTestResult, out string time, out string TTL, out string bytes, out string status);
                stop_watch.Stop();

                TimeSpan record_time = stop_watch.Elapsed;

                double record_time_sec = record_time.Seconds;
                string tdc_delay_limit = txtLimitTime.Text;

                retData.factory = txtPlant.Text;
                retData.line = txtLine.Text;
                retData.sender = txtPC.Text;

                retData.ping_status = status;
                retData.ping_time = time;
                retData.ping_ttl = TTL;
                retData.ping_bytes = bytes;
                retData.description = msgTestResult;

                if (record_time_sec > double.Parse(tdc_delay_limit))
                {
                    limitTimeResult = false;
                }

                //if error send mqtt & update to db
                retData.id = DateTime.Now.ToString("yyyyMMddHHmmss") + retData.factory + retData.line + KeyGenerator.GetUniqueKey(5);
                retData.function_name = $"ping";
                retData.total_time = record_time.TotalSeconds.ToString("0.000");

                if (testResult == false || limitTimeResult == false)
                {
                    retData.error_detail = $"Network problem";
                    retData.test_result = "Abnormal";
                }
                else
                {
                    retData.test_result = "Normal";
                }
                RecordReport(retData);
                return $"Ping..{testResult}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string CheckTDCOnline()
        {
            try
            {
                MesLogTdcOnline retData = new MesLogTdcOnline();
                Stopwatch stop_watch = new Stopwatch();
                bool testResult = true, limitTimeResult = true;
                string ErrorMsg = "";

                retData.factory = txtPlant.Text;
                retData.line = txtLine.Text;
                retData.sender = txtPC.Text;

                //get detail of molist
                stop_watch.Start();
                retData.start_date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                testResult = TdcOnlineAction.GetMolist(retData.factory, retData.line, out string ReturnData, out ErrorMsg);
                stop_watch.Stop();
                TimeSpan record_time = stop_watch.Elapsed;

                double record_time_sec = record_time.TotalSeconds;
                string tdc_delay_limit = txtLimitTime.Text;

                if (record_time_sec > double.Parse(tdc_delay_limit))
                {
                    limitTimeResult = false;
                }

                //if error send mqtt & update to db
                retData.id = DateTime.Now.ToString("yyyyMMddHHmmss") + retData.factory + retData.line + KeyGenerator.GetUniqueKey(5);
                retData.function_name = "tdconline_get_molist";
                retData.total_time = record_time.TotalSeconds.ToString("0.000");

                if (testResult == false || limitTimeResult == false)
                {
                    if (testResult == false)
                    {
                        retData.error_detail = $"TDC Online, {ErrorMsg}";
                    }
                    else
                    {
                        retData.error_detail = $"TDC Online is slower than {tdc_delay_limit} seconds";
                    }
                    retData.test_result = "Abnormal";
                }
                else
                {
                    retData.test_result = "Normal";
                }

                RecordReport(retData);
                return $"TDC Online...{testResult}";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (backgroundWorker1.IsBusy == false)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            backgroundWorker1.ReportProgress(0);
            http_response = DateTime.Now.ToString() + Environment.NewLine + CheckPing() + Environment.NewLine
                + CheckTDCOnline();
            backgroundWorker1.ReportProgress(100);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            txtResponse.Text = http_response;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            txtResponse.Text = "Stop program";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            txtResponse.Text = "Start program";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            txtPlant.Text = System.Configuration.ConfigurationManager.AppSettings["factory"];
            txtLine.Text = System.Configuration.ConfigurationManager.AppSettings["line"];
            txtLimitTime.Text = System.Configuration.ConfigurationManager.AppSettings["tdc_delay_limit"];
            txtTextfilePath.Text = System.Configuration.ConfigurationManager.AppSettings["save_textfile_location"];
            txtPC.Text = MyComputer.GetComputerNameAndIP();

            txtResponse.Text = DateTime.Now.ToString() + Environment.NewLine + CheckPing() + Environment.NewLine
                + CheckTDCOnline();
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            timer1.Interval = int.Parse(System.Configuration.ConfigurationManager.AppSettings["sender_interval"]);
            timer1.Enabled = true;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // Set the WindowState to normal if the form is minimized.
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;

            // Activate the form.
            this.Activate();
        }

        private void btnTestIO_Click(object sender, EventArgs e)
        {
            RecordReport(new MesLogTdcOnline
            {
                id = DateTime.Now.ToString("yyyyMMddHHmmss") + KeyGenerator.GetUniqueKey(10),
                line = txtLine.Text,
                factory = txtPlant.Text,
                error_detail = "test_io_error_detail",
                function_name = "test_io_function_name",
                start_date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                total_time = "10",
                sender = txtPC.Text,
            });
        }

    }
}
