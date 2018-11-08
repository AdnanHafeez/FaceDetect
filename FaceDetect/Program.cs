﻿using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Primitives;
using System;
using System.IO;
using System.Net;
using SixLabors.Primitives;
using System.Collections.Generic;
using SixLabors.ImageSharp.PixelFormats;

namespace FaceDetect
{
    class Program
    {
        private static string msg = "Please provide the API key as the first comman-line parameter, followed by the filename";

        static void Main(string[] args)
        {
            // Command Line arguments
            var apiKey = !string.IsNullOrWhiteSpace(args[0]) ? args[0] : throw new ArgumentException(msg, args[0]);
            var filename = File.Exists(args[1]) ? args[1] : throw new FileNotFoundException(msg, args[1]);

            // HTTP Request
            var region = "westcentralus";
            var target = new Uri($"https://{region}.api.cognitive.microsoft.com/face/v1.0/detect/?subscription-key={apiKey}");
            var httpPost = CreateHttpRequest(target, "POST", "application/octet-stream");
            
            // Load Image
            using (var fs = File.OpenRead(filename))
            {
                fs.CopyTo(httpPost.GetRequestStream());
            }
            
            // submit Image to HTTP Endpoint

            string data = GetResponse(httpPost);

            // Inspect JSON

            var rectangles = GetRectangles(data);


            // Draw rectangles on the image (copy)

            var img = Image.Load(filename);

            foreach(var rectangle in GetRectangles(data))
            {
                img.Mutate(a => a.DrawPolygon(Rgba32.HotPink, 20, rectangle));
            }

            var outputfilename = $"{Environment.CurrentDirectory}\\{Path.GetFileNameWithoutExtension(filename)}-2{Path.GetExtension(filename)}";

            SaveImage(img, outputfilename);
        }

        private static void SaveImage(Image<Rgba32> img, string outputfilename)
        {
            using (var fs = File.Create(outputfilename))
            {
                img.SaveAsJpeg(fs);
            }
        }

        private static IEnumerable<PointF[]> GetRectangles(string data)
        {
            var faces = JArray.Parse(data);

            foreach (var face in faces)
            {
                var id = (string)face["faceId"];
                var top = (int)face["faceRectangle"]["top"];
                var left = (int)face["faceRectangle"]["left"];
                var width = (int)face["faceRectangle"]["width"];
                var height = (int)face["faceRectangle"]["height"];

                var rectangle = new PointF[]
            {
                new PointF(left, top),
                new PointF(left + width, top),
                new PointF(left + width, top + height),
                new PointF(left, top + height)
            };

                yield return rectangle;

            }
            

        }

        private static string GetResponse(WebRequest httpPost)
        {
            using (var response = httpPost.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                return sr.ReadToEnd();
            }
        }

        private static HttpWebRequest CreateHttpRequest(Uri target, string method, string contentType)
        {
            var request = WebRequest.CreateHttp(target);
            request.Method = method;
            request.ContentType = contentType;
            return request;
        }
    }
}
