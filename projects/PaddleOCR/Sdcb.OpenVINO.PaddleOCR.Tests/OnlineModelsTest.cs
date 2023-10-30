using OpenCvSharp;
using Sdcb.OpenVINO.PaddleOCR.Models.Online;
using Sdcb.OpenVINO.PaddleOCR.Models;
using Xunit.Abstractions;
using System.Diagnostics;

namespace Sdcb.OpenVINO.PaddleOCR.Tests;

public class OnlineModelsTest
{
    private readonly ITestOutputHelper _console;

    public OnlineModelsTest(ITestOutputHelper console)
    {
        _console = console;
    }

    [Fact]
    public async Task FastCheckOCR()
    {
        FullOcrModel model = await OnlineFullModels.EnglishV3.DownloadAsync();

        // from: https://visualstudio.microsoft.com/wp-content/uploads/2021/11/Home-page-extension-visual-updated.png
        byte[] sampleImageData = File.ReadAllBytes(@"./samples/vsext.png");

        using (PaddleOcrAll all = new(model)
        {
            AllowRotateDetection = true,
            Enable180Classification = false,
        })
        {
            // Load local file by following code:
            // using (Mat src2 = Cv2.ImRead(@"C:\test.jpg"))
            using (Mat src = Cv2.ImDecode(sampleImageData, ImreadModes.Color))
            {
                Stopwatch sw = Stopwatch.StartNew();
                PaddleOcrResult result = all.Run(src);
                _console.WriteLine($"elapsed={sw.ElapsedMilliseconds} ms");
                _console.WriteLine("Detected all texts: \n" + result.Text);
                foreach (PaddleOcrResultRegion region in result.Regions)
                {
                    _console.WriteLine($"Text: {region.Text}, Score: {region.Score}, RectCenter: {region.Rect.Center}, RectSize: {region.Rect.Size}, Angle: {region.Rect.Angle}");
                }
            }
        }
    }

    [Fact]
    public async Task NoRotateTest()
    {
        FullOcrModel model = await OnlineFullModels.EnglishV3.DownloadAsync();

        byte[] sampleImageData = File.ReadAllBytes(@"./samples/vsext.png");

        using (PaddleOcrAll all = new(model)
        {
            AllowRotateDetection = false,
            Enable180Classification = false,
        })
        {
            using (Mat src = Cv2.ImDecode(sampleImageData, ImreadModes.Color))
            {
                PaddleOcrResult result = all.Run(src);
                _console.WriteLine(result.Text);
                Assert.Contains("IDE", result.Text);
            }
        }
    }

    [Fact]
    public async Task FastCheckOCRWith180Cls()
    {
        FullOcrModel model = await OnlineFullModels.EnglishV4.DownloadAsync();

        // from: https://visualstudio.microsoft.com/wp-content/uploads/2021/11/Home-page-extension-visual-updated.png
        byte[] sampleImageData = File.ReadAllBytes(@"./samples/vsext.png");

        using (PaddleOcrAll all = new(model)
        {
            AllowRotateDetection = true,
            Enable180Classification = true,
        })
        {
            // Load local file by following code:
            // using (Mat src2 = Cv2.ImRead(@"C:\test.jpg"))
            using (Mat src = Cv2.ImDecode(sampleImageData, ImreadModes.Color))
            {
                Stopwatch sw = Stopwatch.StartNew();
                PaddleOcrResult result = all.Run(src);
                _console.WriteLine($"elapsed={sw.ElapsedMilliseconds} ms");
                _console.WriteLine("Detected all texts: \n" + result.Text);

                foreach (PaddleOcrResultRegion region in result.Regions)
                {
                    _console.WriteLine($"Text: {region.Text}, Score: {region.Score}, RectCenter: {region.Rect.Center}, RectSize: {region.Rect.Size}, Angle: {region.Rect.Angle}");
                }
            }
        }
    }

    [Fact]
    public async Task GPUFastCheckOCR()
    {
        FullOcrModel model = await OnlineFullModels.EnglishV3.DownloadAsync();

        // from: https://visualstudio.microsoft.com/wp-content/uploads/2021/11/Home-page-extension-visual-updated.png
        byte[] sampleImageData = File.ReadAllBytes(@"./samples/vsext.png");
        using (PaddleOcrAll all = new(model, new PaddleOcrOptions(new DeviceOptions("GPU")))
        {
            AllowRotateDetection = true,
            Enable180Classification = true,
        })
        {
            // Load local file by following code:
            // using (Mat src2 = Cv2.ImRead(@"C:\test.jpg"))
            using (Mat src = Cv2.ImDecode(sampleImageData, ImreadModes.Color))
            {
                Stopwatch sw = Stopwatch.StartNew();
                PaddleOcrResult result = all.Run(src);
                _console.WriteLine($"elapsed={sw.ElapsedMilliseconds} ms");
                _console.WriteLine("Detected all texts: \n" + result.Text);
                foreach (PaddleOcrResultRegion region in result.Regions)
                {
                    _console.WriteLine($"Text: {region.Text}, Score: {region.Score}, RectCenter: {region.Rect.Center}, RectSize:    {region.Rect.Size}, Angle: {region.Rect.Angle}");
                }
            }
        }
    }

    [Fact]
    public async Task V4FastCheckOCR()
    {
        OnlineFullModels onlineModels = new(
            OnlineDetectionModel.ChineseV4, OnlineClassificationModel.ChineseMobileV2, LocalDictOnlineRecognizationModel.EnglishV4);
        FullOcrModel model = await onlineModels.DownloadAsync();

        // from: https://visualstudio.microsoft.com/wp-content/uploads/2021/11/Home-page-extension-visual-updated.png
        byte[] sampleImageData = File.ReadAllBytes(@"./samples/vsext.png");

        using (PaddleOcrAll all = new(model)
        {
            AllowRotateDetection = true,
            Enable180Classification = false,
        })
        {
            // Load local file by following code:
            // using (Mat src2 = Cv2.ImRead(@"C:\test.jpg"))
            using (Mat src = Cv2.ImDecode(sampleImageData, ImreadModes.Color))
            {
                PaddleOcrResult result = null!;
                Stopwatch sw = Stopwatch.StartNew();
                result = all.Run(src);

                _console.WriteLine($"elapsed={sw.ElapsedMilliseconds} ms");
                _console.WriteLine("Detected all texts: \n" + result.Text);
            }
        }
    }

    [Fact(Skip = "Too slow")]
    public async Task V4ServerTest()
    {
        OnlineFullModels onlineModels = OnlineFullModels.ChineseServerV4;
        FullOcrModel model = await onlineModels.DownloadAsync();

        // from: https://visualstudio.microsoft.com/wp-content/uploads/2021/11/Home-page-extension-visual-updated.png
        byte[] sampleImageData = File.ReadAllBytes(@"./samples/vsext.png");

        using (PaddleOcrAll all = new(model)
        {
            AllowRotateDetection = true,
            Enable180Classification = false,
        })
        {
            // Load local file by following code:
            // using (Mat src2 = Cv2.ImRead(@"C:\test.jpg"))
            using (Mat src = Cv2.ImDecode(sampleImageData, ImreadModes.Color))
            {
                PaddleOcrResult result = null!;
                Stopwatch sw = Stopwatch.StartNew();
                result = all.Run(src);

                _console.WriteLine($"elapsed={sw.ElapsedMilliseconds} ms");
                _console.WriteLine("Detected all texts: \n" + result.Text);
            }
        }
    }
}