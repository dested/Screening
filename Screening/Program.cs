using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Screening
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("./config.json"));

            var c = new WebEye.Controls.WinForms.WebCameraControl.WebCameraControl();
            while (true)
            {

                Bitmap screenshot = null;
                Bitmap webcam = null;
                Bitmap img = null;
                try
                {
                    screenshot = getScreenshot(config);
                    if (config.hasWebcam)
                    {
                        try
                        {
                            var videoCaptureDevices = c.GetVideoCaptureDevices().ToArray();
                            c.StartCapture(videoCaptureDevices.First());
                            Thread.Sleep(2000);
                            webcam = c.GetCurrentImage();
                            Thread.Sleep(2000);
                            c.StopCapture();

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            webcam = null;
                        }
                    }
                    img = new Bitmap(screenshot.Width + (webcam?.Width ?? 0), screenshot.Height);
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(img))
                    {
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                        g.DrawImage(screenshot, 0, 0);
                        if (webcam != null)
                        {
                            g.DrawImage(webcam, screenshot.Width, 0);
                        }
                    }
                    img.Save($"{config.path}{DateTime.Now:MM-dd-yyyy HH-mm-ss}.png");


                    Console.WriteLine($"Saved c:\\junk\\screenshots\\{DateTime.Now:MM-dd-yyyy HH-mm-ss}.png");

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    try
                    {
                        if (config.hasWebcam)
                        {
                            c.StopCapture();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                img?.Dispose();
                webcam?.Dispose();
                screenshot?.Dispose();
                Thread.Sleep(1000 * 60 * 60 * config.timeoutMinutes);
            }
        }
        private static Bitmap getScreenshot(Config config)
        {
            Bitmap bmpScreenCapture = new Bitmap(config.resolutionWidth * config.numberOfMonitors, config.resolutionHeight);
            {
                using (Graphics g = Graphics.FromImage(bmpScreenCapture))
                {
                    var left = 0;
                    if (config.numberOfMonitors == 1)
                    {
                        left = 0;
                    }
                    else
                    {
                        left = -config.resolutionWidth;
                    }
                    g.CopyFromScreen(left,
                        0,
                        0, 0,
                        bmpScreenCapture.Size,
                        CopyPixelOperation.SourceCopy);
                }
                return bmpScreenCapture;
            }
        }

    }
    class Config
    {
        public int resolutionHeight { get; set; }
        public int resolutionWidth { get; set; }
        public int numberOfMonitors { get; set; }
        public string path { get; set; }
        public bool hasWebcam { get; set; }
        public int timeoutMinutes { get; set; }
    }
}
