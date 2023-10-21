﻿using OpenCvSharp;
using Sdcb.OpenVINO.PaddleOCR.Models;
using Sdcb.OpenVINO.Extensions.OpenCvSharp4;
using System;
using System.Linq;

namespace Sdcb.OpenVINO.PaddleOCR;

/// <summary>
/// Implements a PaddleOCR classifier using a PaddlePredictor.
/// </summary>
public class PaddleOcrClassifier : IDisposable
{
    private readonly InferRequest _p;

    /// <summary>
    /// Rotation threshold value used to determine if the image should be rotated.
    /// </summary>
    public double RotateThreshold { get; init; } = 0.75;

    /// <summary>
    /// The OcrShape used for the model.
    /// </summary>
    public NCHW Shape { get; init; } = ClassificationModel.DefaultShape;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaddleOcrClassifier"/> class with a specified model and configuration.
    /// </summary>
    /// <param name="model">The <see cref="ClassificationModel"/> to use.</param>
    /// <param name="device">The device the inference request, pass null to using model's DefaultDevice.</param>
    public PaddleOcrClassifier(ClassificationModel model, DeviceOptions? device = null)
    {
        Shape = model.Shape;
        _p = model.CreateInferRequest(device, prePostProcessing: (m, ppp) =>
        {
            using PreProcessInputInfo ppii = ppp.Inputs.Primary;
            ppii.TensorInfo.Layout = Layout.NHWC;
            ppii.ModelInfo.Layout = Layout.NCHW;
        });
    }

    /// <summary>
    /// Releases all resources used by the <see cref="PaddleOcrClassifier"/> object.
    /// </summary>
    public void Dispose() => _p.Dispose();

    /// <summary>
    /// Determines whether the image should be rotated by 180 degrees based on the threshold value.
    /// </summary>
    /// <param name="src">The source image.</param>
    /// <returns>An instance of the <see cref="Ocr180DegreeClsResult"/> record struct, containing a bool indicating whether the image should be rotated and a float representing the confidence of the determination.</returns>
    /// <exception cref="ArgumentException">Thrown if the source image size is 0.</exception>
    /// <exception cref="NotSupportedException">Thrown if the source image has a channel count other than 3 or 1.</exception>
    public Ocr180DegreeClsResult ShouldRotate180(Mat src)
    {
        if (src.Empty())
        {
            throw new ArgumentException("src size should not be 0, wrong input picture provided?");
        }

        if (!(src.Channels() switch { 3 or 1 => true, _ => false }))
        {
            throw new NotSupportedException($"{nameof(src)} channel must be 3 or 1, provided {src.Channels()}.");
        }

        using Mat normalized = new();
        using (Mat resized = ResizePadding(src, Shape))
        {
            resized.ConvertTo(normalized, MatType.CV_32FC3, 2.0 / 255, -1);
        }

        using (Tensor input = normalized.AsTensor())
        {
            _p.Inputs.Primary = input;
            _p.Run();
        }

        using Tensor output = _p.Outputs.Primary;
        ReadOnlySpan<float> softmax = output.GetData<float>();
        return new Ocr180DegreeClsResult(softmax, RotateThreshold);
    }

    /// <summary>
    /// Determines whether each image in an array should be rotated by 180 degrees based on the threshold value.
    /// </summary>
    /// <param name="srcs">The array of source images.</param>
    /// <returns>An array of Ocr180DegreeClsResult instances, each containing a bool indicating whether the corresponding image should be rotated and a float representing the confidence of the determination.</returns>

    public Ocr180DegreeClsResult[] ShouldRotate180(Mat[] srcs)
    {
        return srcs.Select(ShouldRotate180).ToArray();
    }

    /// <summary>
    /// Processes the input image, and returns the resulting image.
    /// </summary>
    /// <param name="src">The source image.</param>
    /// <returns>The resulting image.</returns>
    /// <exception cref="ArgumentException">Thrown if the source image size is 0.</exception>
    /// <exception cref="NotSupportedException">Thrown if the source image has a channel count other than 3 or 1.</exception>
    public Mat Run(Mat src)
    {
        Ocr180DegreeClsResult res = ShouldRotate180(src);
        res.RotateIfShould(src);
        return src;
    }

    private static Mat ResizePadding(Mat src, NCHW shape)
    {
        Size srcSize = src.Size();
        double whRatio = 1.0 * shape.Width / shape.Height;
        using Mat roi = 1.0 * srcSize.Width / srcSize.Height > whRatio ?
            src[0, srcSize.Height, 0, (int)Math.Floor(1.0 * srcSize.Height * whRatio)] :
            src.WeakRef();

        double scaleRate = 1.0 * shape.Height / srcSize.Height;
        Mat resized = roi.Resize(new Size(Math.Floor(roi.Width * scaleRate), shape.Height));

        if (resized.Width < shape.Width)
        {
            Cv2.CopyMakeBorder(resized, resized, 0, 0, 0, shape.Width - resized.Width, BorderTypes.Constant, Scalar.Black);
        }
        return resized;
    }
}
