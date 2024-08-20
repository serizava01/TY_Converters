using NAudio.Lame;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace TY_Converters
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }



        private void ProcessVideo(string videoUrl)
        {
            try
            {
                var youtube = new YoutubeClient();
                var video = youtube.Videos.GetAsync(videoUrl).Result;
                string videoTitle = video.Title;
                string outputPath = null;

                // เรียกใช้งาน SaveFileDialog บน UI thread
                this.Invoke(new Action(() =>
                {
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        saveFileDialog.Filter = "MP3 Files (*.mp3)|*.mp3";
                        saveFileDialog.FileName = videoTitle + ".mp3";

                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            outputPath = saveFileDialog.FileName;
                        }
                    }
                }));

                if (!string.IsNullOrEmpty(outputPath))
                {
                    var streamManifest = youtube.Videos.Streams.GetManifestAsync(video.Id).Result;
                    var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                    if (streamInfo != null)
                    {
                        var stream = youtube.Videos.Streams.GetAsync(streamInfo).Result;
                        string tempFilePath = Path.GetTempFileName();

                        using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                        {
                            stream.CopyTo(fileStream);
                        }

                        using (var mp4 = new MediaFoundationReader(tempFilePath))
                        using (var mp3 = new LameMP3FileWriter(outputPath, mp4.WaveFormat, 128))
                        {
                            mp4.CopyTo(mp3);
                        }

                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing video: {videoUrl}\n{ex.Message}", "ข้อผิดพลาด", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            var urls = new List<string>
            { textBox1.Text,
            textBox2.Text,
            textBox3.Text,
            textBox4.Text,
            textBox5.Text
            }.Where(url => !String.IsNullOrEmpty(url)).ToList();
            if (!urls.Any())
            {
                MessageBox.Show("กรุณาใส่ Url ก่อนทำการแฟปลงไฟล์", "ข้อผิดพลาด",MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            videoUrls = urls;
            currentUrlIndex = 0;
            label1.Text = "กำลังทำงาน...";
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Value = 0;
            progressBar1.Maximum = 100;
            if(!bw_01.IsBusy)
            {
                bw_01.RunWorkerAsync();
            }


        }


        private void Main_Load(object sender, EventArgs e)
        {

        }
        
        private List<string> videoUrls;
        private int currentUrlIndex;
        private async void bw_01_DoWork(object sender, DoWorkEventArgs e)
        {
            while (currentUrlIndex < videoUrls.Count)
            {
                string videoUrl = videoUrls[currentUrlIndex];
                ProcessVideo(videoUrl);
                currentUrlIndex++;
                
                bw_01.ReportProgress((currentUrlIndex * 100) / videoUrls.Count);
            }

        }

        private void bw_01_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            label1.Text = "การทำงานเสร็จสิ้น!";
            progressBar1.Style = ProgressBarStyle.Blocks;
        }

        private void bw_01_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }
    }
}
