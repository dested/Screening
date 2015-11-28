using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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

            rehydrate();


            write();
            var name = "fooc";

            //            write();

            Bitmap @base = getScreenshot();
            Directory.CreateDirectory(name);
            var d = writeImagePayload(@base);
            File.WriteAllBytes($"{name}/base.bp", d);

            readImagePayload(File.ReadAllBytes($"{name}/base.bp"));







        }

        private static void rehydrate()
        {
            var name = "fooc";
            var bmp = readImagePayload(File.ReadAllBytes($"{name}/base.bp"));
            bmp.Save($"{name}/base.bmp");
            int index = 1;

            Bitmap bmp2 = null;
            Bitmap bmp2diff = bmp;

            while (File.Exists($"{name}/diff {index++}.bp"))
            {
                bmp2 = readImagePayload(File.ReadAllBytes($"{name}/diff {index}.bp"));
                bmp2.Save($"{name}/diff {index}.bmp");
                bmp2diff = PixelDiffNoTransparent(bmp2, bmp2diff);
                bmp2diff.Save($"{name}/diff {index} a.bmp");
            }
/*

            var bmp2 = readImagePayload(File.ReadAllBytes($"{name}/diff 1.bp"));
            bmp2.Save($"{name}/diff 1.bmp");
            var bmp2diff = PixelDiffNoTransparent(bmp2, bmp);
            bmp2diff.Save($"{name}/diff 1 a.bmp");

            var bmp3 = readImagePayload(File.ReadAllBytes($"{name}/diff 2.bp"));
            bmp3.Save($"{name}/diff 2.bmp");
            var bmp3diff = PixelDiffNoTransparent(bmp3, bmp2diff);
            bmp3diff.Save($"{name}/diff 2 a.bmp");

            var bmp4 = readImagePayload(File.ReadAllBytes($"{name}/diff 3.bp"));
            bmp4.Save($"{name}/diff 3.bmp");
            var bmp4diff = PixelDiffNoTransparent(bmp4, bmp3diff);
            bmp4diff.Save($"{name}/diff 3 a.bmp");*/
        }

        private unsafe static Bitmap readImagePayload(byte[] readAllBytes)
        {
            fixed (byte* ps = &readAllBytes[0])
            {
                byte* bb = ps;
                var width = readLength(ref bb);
                var height = readLength(ref bb);

                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

                while (true)
                {
                    switch (readStatus(ref bb))
                    {
                        case 1:
                            {
                                var x = readLength(ref bb);
                                var y = readLength(ref bb);
                                var color = toColor(readColor(ref bb));
                                bmp.SetPixel(x, y, color);
                            }
                            break;
                        case 2:
                            {
                                var x = readLength(ref bb);
                                var y = readLength(ref bb);
                                var len = readLength(ref bb);
                                for (int i = 0; i < len; i++)
                                    bmp.SetPixel(x + i, y, toColor(readColor(ref bb)));
                            }
                            break;
                        case 3:
                            {
                                var x = readLength(ref bb);
                                var y = readLength(ref bb);
                                var len = readLength(ref bb);
                                var color = toColor(readColor(ref bb));
                                for (int i = 0; i < len; i++)
                                    bmp.SetPixel(x + i, y, color);
                            }
                            break;
                        case 255:
                            return bmp;
                    }

                }

            }


        }

        private static Color toColor(PixelData pixelData)
        {
            return Color.FromArgb(pixelData.red, pixelData.green, pixelData.blue);
        }


        private static void write()
        {
            Bitmap @base = getScreenshot();
            var name = "fooc";
            Directory.CreateDirectory(name);
            var d = writeImagePayload(@base);
            File.WriteAllBytes($"{name}/base.bp", d);
            Console.WriteLine("Base");
            Thread.Sleep(1000 * 5);
            int count = 0;
            while (true)
            {
                count++;
                Console.WriteLine($"diff {count} start {DateTime.Now.ToLongTimeString()}");
                var n = getScreenshot();
                var b = PixelDiffNoTransparent(@base, n);
                @base.Dispose();
                @base = n;

                d = writeImagePayload(b);
                b.Dispose();

                File.WriteAllBytes($"{name}/diff {count}.bp", d);
                Console.WriteLine($"diff {count} end {DateTime.Now.ToLongTimeString()}");

                GC.Collect();

                Thread.Sleep(1000 * 5);
            }
        }

        public static void Main2()
        {
            var @base = getScreenshot();
            int count = 0;
            var name = "fooc";
            Directory.CreateDirectory(name);
            @base.Save($"{name}/0.bmp");
            Thread.Sleep(1000 * 5);
            while (true)
            {
                count++;
                Console.WriteLine($"{count} Started ${DateTime.Now.ToLongTimeString()}");
                var n = getScreenshot();
                Console.WriteLine($"Screenshot ${DateTime.Now.ToLongTimeString()}");

                var b = PixelDiff(@base, n);
                Console.WriteLine($"Pixel Diff ${DateTime.Now.ToLongTimeString()}");

                //                var c = UnPixelDiff(@base, b);
                Console.WriteLine($"UnPixel Diff ${DateTime.Now.ToLongTimeString()}");
                Directory.CreateDirectory(name + "/" + count);

                //                c.Save($"{name}/{count}/2 splice.bmp");
                //                c.Dispose();


                var j = writeImagePayload(b);
                /*  var ds = buildBoxes(b);
                  Console.WriteLine($"Build Boxes Done ${DateTime.Now.ToLongTimeString()}");

                  using (ds)
                  {

                      foreach (var rectObject in ds.Rectangles)
                      {
                          rectObject.Bitmap.Save($"{name}/{count}/0 blocks {rectObject.Index}.bmp");
                      }
                      StringBuilder sb = new StringBuilder();
                      var serializeObject = JsonConvert.SerializeObject(ds);
                      File.WriteAllText($"{name}/{count}/blocks.json", serializeObject);
                  }*/

                //                b.Save($"{name}/{count}/0 diff.bmp");
                b.Dispose();
                @base.Dispose();
                @base = n;
                Console.WriteLine($"{count} Done");
                Thread.Sleep(1000 * 5);
            }

        }

        public class RectCollection : IDisposable
        {
            public RectCollection()
            {
                Rectangles = new List<RectObject>();

            }

            public List<RectObject> Rectangles { get; set; }
            public void Dispose()
            {
                foreach (var rectObject in Rectangles)
                {
                    rectObject.Bitmap.Dispose();
                }
            }
        }

        public class RectObject
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int Index { get; set; }
            public Bitmap Bitmap { protected internal get; set; }

            /*
                        public string Base64
                        {
                            get
                            {
                                Bitmap bImage = Bitmap;  //Your Bitmap Image
                                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                                bImage.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                                byte[] byteImage = ms.ToArray();
                                return Convert.ToBase64String(byteImage); //Get Base64
                            }
                        }
            */
        }

        private static RectCollection buildBoxes(Bitmap bitmap)
        {
            List<CRectangle> rt = new List<CRectangle>();

            UnsafeBitmap bmp = new UnsafeBitmap(bitmap);
            bmp.LockBitmap();
            var gapHeight = 40;

            for (int y = 0; y < bitmap.Height; y++)
            {
                List<CRectangle> rtc = new List<CRectangle>();

                foreach (var r in rt)
                {
                    if (r.Y + r.Height + gapHeight + 1 >= y)
                    {
                        rtc.Add(r);
                    }
                }


                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pix = bmp.GetPixel(x, y);
                    if (!(pix.red == 255 && pix.green == 255 && pix.blue == 255))
                    {


                        List<CRectangle> ins = new List<CRectangle>();
                        if (rtc.Count > 1)
                        {

                        }
                        foreach (var rectangle in rtc)
                        {
                            if (rectangle.ShouldContain(x, y))
                            {
                                if (rectangle.Contains(x, y))
                                {
                                    ins.Add(rectangle);
                                    //                                    Console.WriteLine($"rt: {rt.Count} rtc:{rtc.Count} contians");

                                }
                                else if (rectangle.Contains(x, y - 1))
                                {
                                    ins.Add(rectangle);
                                    rectangle.Height = rectangle.Height + 1;
                                    //                                    Console.WriteLine($"rt: {rt.Count} rtc:{rtc.Count} height");
                                }
                                else if (rectangle.Contains(x - 1, y))
                                {
                                    ins.Add(rectangle);
                                    rectangle.Width = rectangle.Width + 1;
                                    //                                    Console.WriteLine($"rt: {rt.Count} rtc:{rtc.Count} width");
                                }
                            }
                        }
                        if (ins.Count == 0)
                        {
                            var cRectangle = new CRectangle(x, y, gapHeight * 5, gapHeight);
                            rt.Add(cRectangle);
                            rtc.Add(cRectangle);
                            //                            Console.WriteLine($"rt: {rt.Count} rtc:{rtc.Count} new");
                        }
                        if (ins.Count > 1)
                        {
                            var ba = ins[0];

                            for (int index = ins.Count - 1; index >= 0; index--)
                            {
                                ba = CRectangle.Union(ba, ins[index]);
                                rt.Remove(ins[index]);
                                rtc.Remove(ins[index]);
                            }
                            rt.Add(ba);
                            rtc.Add(ba);
                            //                            Console.WriteLine($"rt: {rt.Count} rtc:{rtc.Count} union");

                        }


                    }
                }
            }
            Console.WriteLine($"{rt.Count} boxes");


            bmp.Dispose();

            byte[] bytes = new byte[10000000];
            bytes[0] = 0xff;
            RectCollection col = new RectCollection();

            for (int index = 0; index < rt.Count; index++)
            {
                var cRectangle = rt[index];
                var dc = CopyImage(bitmap, cRectangle);

                col.Rectangles.Add(new RectObject()
                {
                    X = cRectangle.X,
                    Y = cRectangle.Y,
                    Width = cRectangle.Width,
                    Height = cRectangle.Height,
                    Index = index,
                    Bitmap = dc
                });
            }

            return col;



            /*
                        Bitmap done = new Bitmap(bitmap.Width, bitmap.Height);
                        var random = new Random();
                        using (Graphics g = Graphics.FromImage(done))
                        {
                            foreach (var rectangle in rt)
                            {
                                g.FillRectangle(new SolidBrush(Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255))),

                                    rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
                            }

                        }
                        return done;
            */
        }

        private static byte[] writeImagePayload(Bitmap bitmap)
        {
            List<byte> bytes = new List<byte>(bitmap.Width * bitmap.Height);

            UnsafeBitmap bmp = new UnsafeBitmap(bitmap);
            bmp.LockBitmap();


            List<PixelData> consecutive = new List<PixelData>();
            var width = bitmap.Width;
            var height = bitmap.Height;
            writeLength(bytes, width);
            writeLength(bytes, height);

            for (int y = 0; y < height; y++)
            {
                Point start = new Point(-1, -1);
                Point startColor = new Point(-1, -1);
                PixelData pixLast = PixelData.Empty;
                int consecutiveColor = 0;
                for (int x = 0; x < width; x++)
                {
                    var pix = bmp.GetPixel(x, y);
                    if (!(pix.red == 255 && pix.green == 255 && pix.blue == 255))
                    {

                        if (pixLast.red == pix.red && pixLast.green == pix.green && pixLast.blue == pix.blue)
                        {
                            if (consecutive.Count > 0)
                            {
                                //                                consecutive.RemoveAt(consecutive.Count - 1);
                                writeOutConsecutive(consecutive, bytes, ref start);
                            }
                            if (startColor.X == -1)
                            {
                                startColor = new Point(x, y);
                            }
                            consecutiveColor++;
                        }
                        else
                        {

                            if (consecutiveColor > 0)
                            {
                                writeOutConsecutiveColor(ref consecutiveColor, pixLast, bytes, ref startColor);
                            }


                            consecutiveColor++;
                            if (startColor.X == -1)
                            {
                                startColor = new Point(x, y);
                            }
                            if (start.X == -1)
                            {
                                start = new Point(x, y);
                            }
                            consecutive.Add(pix);
                            pixLast = pix;
                        }
                    }
                    else
                    {
                        if (consecutiveColor > 1)
                        {
                            writeOutConsecutiveColor(ref consecutiveColor, pixLast, bytes, ref startColor);
                        }
                        else
                        {
                            writeOutConsecutive(consecutive, bytes, ref start);
                        }

                    }
                }
                if (consecutiveColor > 1)
                {
                    writeOutConsecutiveColor(ref consecutiveColor, pixLast, bytes, ref startColor);
                }
                else
                {
                    writeOutConsecutive(consecutive, bytes, ref start);
                }
            }
            writeStatus(bytes, 255);
            /*
                        writeLength(bytes, bitmap.Width);
                        writeLength(bytes, bitmap.Height);
                        writeXY(bytes, 484, 997);
                        writeColor(bytes, new PixelData(155, 255, 24));
                        writeColor(bytes, new PixelData(15, 55, 24));
                        writeColor(bytes, new PixelData(100, 100, 100));
                        writeLength(bytes, bitmap.Height);

                        var diffPayload = bytes.ToArray();



                        fixed (byte* ps = &diffPayload[0])
                        {
                            byte* bb = ps;

                            var m1 = readLength( ref bb);
                            var m2 = readLength(ref bb);
                            var m3x = readLength(ref bb);
                            var m3y = readLength(ref bb);
                            var col1 = readColor(ref bb);
                            var col2 = readColor(ref bb);
                            var col3 = readColor(ref bb);
                            var m3 = readLength(ref bb);
                        }

            */

            bmp.UnlockBitmap();
            return bytes.ToArray();

        }

        public static void writeOutConsecutive(List<PixelData> consecutive, List<byte> bytes, ref Point start)
        {
            if (consecutive.Count > 1)
            {
                writeStatus(bytes, 2);
                writeXY(bytes, start.X, start.Y);
                writeLength(bytes, consecutive.Count);
                foreach (var pixelData in consecutive)
                {
                    writeColor(bytes, pixelData);
                }
                consecutive.Clear();
                start = new Point(-1, -1);
            }
            if (consecutive.Count > 0)
            {
                writeStatus(bytes, 1);
                writeXY(bytes, start.X, start.Y);
                writeColor(bytes, consecutive[0]);
                consecutive.Clear();
                start = new Point(-1, -1);
            }

        }

        private static void writeOutConsecutiveColor(ref int consecutiveColor, PixelData pixLast, List<byte> bytes, ref Point start)
        {
            writeStatus(bytes, 3);
            writeXY(bytes, start.X, start.Y);
            writeLength(bytes, consecutiveColor);
            writeColor(bytes, pixLast);
            start = new Point(-1, -1);
            consecutiveColor = 0;

        }

        private static void writeXY(List<byte> bytes, int x, int y)
        {
            bytes.Add((byte)(x >> 8));
            bytes.Add((byte)(x));
            bytes.Add((byte)(y >> 8));
            bytes.Add((byte)(y));
        }
        private static void writeLength(List<byte> bytes, int length)
        {
            bytes.Add((byte)(length >> 8));
            bytes.Add((byte)(length));
        }
        private static void writeStatus(List<byte> bytes, byte status)
        {
            bytes.Add(status);
        }

        private static void writeColor(List<byte> bytes, PixelData pix)
        {
            bytes.Add(pix.blue);
            bytes.Add(pix.green);
            bytes.Add(pix.red);
        }


        private unsafe static int readLength(ref byte* bytes)
        {
            var length = *(bytes)++ << 8 | *(bytes++);
            return length;
        }
        private unsafe static byte readStatus(ref byte* bytes)
        {
            return *(bytes++);
        }

        private unsafe static PixelData readColor(ref byte* bytes)
        {
            return new PixelData(*(bytes++), *(bytes++), *(bytes++));
        }


        static public Bitmap CopyImage(Bitmap srcBitmap, CRectangle section)
        {
            // Create the new bitmap and associated graphics object
            Bitmap bmp = new Bitmap(section.Width, section.Height);
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(srcBitmap, new Rectangle(0, 0, section.Width, section.Height), new Rectangle(section.X, section.Y, section.Width, section.Height), GraphicsUnit.Pixel);

            // Clean up
            g.Dispose();

            // Return the bitmap
            return bmp;
        }

        private static Bitmap getScreenshot()
        {
            Bitmap bmpScreenCapture = new Bitmap(1920 * 2 * 3, 1080 * 2);
            {
                using (Graphics g = Graphics.FromImage(bmpScreenCapture))
                {
                    g.CopyFromScreen(-1920 * 2,
                                     0,
                                     0, 0,
                                     bmpScreenCapture.Size,
                                     CopyPixelOperation.SourceCopy);
                }
                return bmpScreenCapture;
            }
        }

        public static unsafe Bitmap PixelDiffNoTransparent(Bitmap a, Bitmap b)
        {
            Bitmap output = new Bitmap(a.Width, a.Height, PixelFormat.Format24bppRgb);
            Rectangle rect = new Rectangle(Point.Empty, a.Size);
            var aData = a.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var bData = b.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var outputData = output.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            {
                byte* aPtr = (byte*)aData.Scan0;
                byte* bPtr = (byte*)bData.Scan0;
                byte* outputPtr = (byte*)outputData.Scan0;
                int len = aData.Stride * aData.Height;
                for (int i = 0; i < len; i++)
                {
                    *outputPtr = (byte)~(*aPtr ^ *bPtr);

                    outputPtr++;
                    aPtr++;
                    bPtr++;
                }
            }
            output.UnlockBits(outputData);
            b.UnlockBits(bData);
            a.UnlockBits(aData);


            return output;
        }

        public static unsafe Bitmap PixelDiff(Bitmap a, Bitmap b)
        {
            Bitmap output = new Bitmap(a.Width, a.Height, PixelFormat.Format32bppRgb);
            Rectangle rect = new Rectangle(Point.Empty, a.Size);
            var aData = a.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            var bData = b.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            var outputData = output.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
            {
                byte* aPtr = (byte*)aData.Scan0;
                byte* bPtr = (byte*)bData.Scan0;
                byte* outputPtr = (byte*)outputData.Scan0;
                int len = aData.Stride * aData.Height;
                for (int i = 0; i < len; i++)
                {
                    // For alpha use the average of both images (otherwise pixels with the same alpha won't be visible)
                    if ((i + 1) % 4 == 0)
                        *outputPtr = (byte)((*aPtr + *bPtr) / 2);
                    else
                        *outputPtr = (byte)~(*aPtr ^ *bPtr);

                    outputPtr++;
                    aPtr++;
                    bPtr++;
                }
            }
            output.UnlockBits(outputData);
            b.UnlockBits(bData);
            a.UnlockBits(aData);


            return output;
        }


        public static unsafe Bitmap UnPixelDiff(Bitmap a, Bitmap b)
        {
            Bitmap output = new Bitmap(a.Width, a.Height, PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(Point.Empty, a.Size);
            var aData = a.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bData = b.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var outputData = output.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            {
                byte* aPtr = (byte*)aData.Scan0;
                byte* bPtr = (byte*)bData.Scan0;
                byte* outputPtr = (byte*)outputData.Scan0;
                int len = aData.Stride * aData.Height;
                for (int i = 0; i < len; i++)
                {
                    // For alpha use the average of both images (otherwise pixels with the same alpha won't be visible)
                    if ((i + 1) % 4 == 0)
                        *outputPtr = (byte)((*aPtr + *bPtr) / 2);
                    else
                        *outputPtr = (byte)~(*aPtr ^ *bPtr);

                    outputPtr++;
                    aPtr++;
                    bPtr++;
                }
            }
            output.UnlockBits(outputData);
            b.UnlockBits(bData);
            a.UnlockBits(aData);


            return output;
        }

    }






    public unsafe class UnsafeBitmap
    {
        Bitmap bitmap;

        // three elements used for MakeGreyUnsafe
        int width;
        BitmapData bitmapData = null;
        Byte* pBase = null;

        public UnsafeBitmap(Bitmap bitmap)
        {
            this.bitmap = new Bitmap(bitmap);
        }

        public UnsafeBitmap(int width, int height)
        {
            this.bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        }

        public void Dispose()
        {
            bitmap.Dispose();
        }

        public Bitmap Bitmap
        {
            get
            {
                return (bitmap);
            }
        }

        private Point PixelSize
        {
            get
            {
                GraphicsUnit unit = GraphicsUnit.Pixel;
                RectangleF bounds = bitmap.GetBounds(ref unit);

                return new Point((int)bounds.Width, (int)bounds.Height);
            }
        }

        public void LockBitmap()
        {
            GraphicsUnit unit = GraphicsUnit.Pixel;
            RectangleF boundsF = bitmap.GetBounds(ref unit);
            Rectangle bounds = new Rectangle((int)boundsF.X,
          (int)boundsF.Y,
          (int)boundsF.Width,
          (int)boundsF.Height);

            // Figure out the number of bytes in a row
            // This is rounded up to be a multiple of 4
            // bytes, since a scan line in an image must always be a multiple of 4 bytes
            // in length. 
            width = (int)boundsF.Width * sizeof(PixelData);
            if (width % 4 != 0)
            {
                width = 4 * (width / 4 + 1);
            }
            bitmapData =
          bitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            pBase = (Byte*)bitmapData.Scan0.ToPointer();
        }

        public PixelData GetPixel(int x, int y)
        {
            PixelData returnValue = *PixelAt(x, y);
            return returnValue;
        }

        public void SetPixel(int x, int y, PixelData colour)
        {
            PixelData* pixel = PixelAt(x, y);
            *pixel = colour;
        }

        public void UnlockBitmap()
        {
            bitmap.UnlockBits(bitmapData);
            bitmapData = null;
            pBase = null;
        }
        public PixelData* PixelAt(int x, int y)
        {
            return (PixelData*)(pBase + y * width + x * sizeof(PixelData));
        }
    }
    public struct PixelData
    {
        public PixelData(byte blue, byte green, byte red)
        {
            this.blue = blue;
            this.green = green;
            this.red = red;
        }

        public byte blue;
        public byte green;
        public byte red;
        public static PixelData Empty = new PixelData(0, 0, 0);
    }




    public class CRectangle
    {
        private int x;
        private int y;
        private int width;
        private int height;

        public int X
        {
            get { return x; }
            set { x = value; }
        }

        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        public int Width
        {
            get { return width; }
            set { width = value; }
        }

        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public CRectangle(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public bool Contains(int x, int y)
        {
            return this.x <= x &&
            x < this.x + this.width &&
            this.y <= y &&
            y < this.y + this.height;
        }

        public static CRectangle Union(CRectangle a, CRectangle b)
        {
            int x1 = Math.Min(a.X, b.X);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y1 = Math.Min(a.Y, b.Y);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new CRectangle(x1, y1, x2 - x1, y2 - y1);
        }

        public bool ShouldContain(int x, int y)
        {

            return this.x <= x &&
                   x < this.x + this.width + 1 &&
                   this.y <= y &&
                   y < this.y + this.height + 1;
        }
    }
}
