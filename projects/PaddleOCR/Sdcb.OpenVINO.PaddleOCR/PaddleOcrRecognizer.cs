﻿using OpenCvSharp;
using Sdcb.OpenVINO.Natives;
using Sdcb.OpenVINO.Extensions.OpenCvSharp4;
using Sdcb.OpenVINO.PaddleOCR.Models;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Sdcb.OpenVINO.PaddleOCR;

/// <summary>
/// Class for recognizing OCR using PaddleOCR models.
/// </summary>
public class PaddleOcrRecognizer : IDisposable
{
    private readonly InferRequest _p;

    /// <summary>
    /// Recognization model being used for OCR.
    /// </summary>
    public RecognizationModel Model { get; init; }

    /// <summary>
    /// Gets the width of the static shape input for the OCR model. This property is particularly useful in cases 
    /// where the model input shape is not dynamic or it's desired to have a constant shape input. If `null`, 
    /// the shape is considered to be dynamic. The value is automatically rounded to the nearest upper multiple of 32 
    /// following the instance creation based on the provided `staticShapeWidth` value in the constructor.
    /// </summary>
    public int? StaticShapeWidth { get; }

    /// <summary>
    /// Gets a value indicating whether the input shape for the OCR model is dynamic or not.
    /// If this property returns `true`, it means that the model can accept input of various shapes.
    /// If it returns `false`, it indicates that the model requires a fixed input shape defined by the <see cref="StaticShapeWidth"/> property.
    /// </summary>
    public bool IsDynamic => StaticShapeWidth is null;

    /// <summary>
    /// Constructor for creating a new instance of the <see cref="PaddleOcrRecognizer"/> class using a specified model and a callback configuration.
    /// </summary>
    /// <param name="model">The RecognizationModel object.</param>
    /// <param name="deviceOptions">The device of the inference request, pass null to using model's <see cref="BaseModel.DefaultDeviceOptions"/>.</param>
    /// <param name="staticShapeWidth">The width of the static shape for the OCR model. If provided, this value must be a positive integer. 
    /// The value will be rounded to the nearest upper multiple of 32. This parameter is useful for models that require a fixed shape input.
    /// Pass `null` if the model supports dynamic input shape.
    /// </param>
    public PaddleOcrRecognizer(RecognizationModel model, DeviceOptions? deviceOptions = null, int? staticShapeWidth = null)
    {
        Model = model;
        StaticShapeWidth = staticShapeWidth.HasValue ? (int)(32 * Math.Ceiling(1.0 * staticShapeWidth.Value / 32)) : null;

        _p = model.CreateInferRequest(deviceOptions, prePostProcessing: (m, ppp) =>
        {
            using PreProcessInputInfo ppii = ppp.Inputs.Primary;
            ppii.TensorInfo.Layout = Layout.NHWC;
            ppii.ModelInfo.Layout = Layout.NCHW;
        }, afterBuildModel: m =>
        {
            if (StaticShapeWidth.HasValue)
            {
                m.ReshapePrimaryInput(new PartialShape(new Dimension(1, 128), 48, StaticShapeWidth.Value, 3));
            }
        });
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="PaddleOcrRecognizer"/> class.
    /// </summary>
    public void Dispose() => _p.Dispose();

    /// <summary>
    /// Run OCR recognition on multiple images in batches.
    /// </summary>
    /// <param name="srcs">Array of images for OCR recognition.</param>
    /// <param name="batchSize">Size of the batch to run OCR recognition on.</param>
    /// <returns>Array of <see cref="PaddleOcrRecognizerResult"/> instances corresponding to OCR recognition results of the images.</returns>
    public PaddleOcrRecognizerResult[] Run(Mat[] srcs, int batchSize = 0)
    {
        if (srcs.Length == 0)
        {
            return new PaddleOcrRecognizerResult[0];
        }

        int chooseBatchSize = batchSize != 0 ? batchSize : Math.Min(8, Environment.ProcessorCount);
        PaddleOcrRecognizerResult[] allResult = new PaddleOcrRecognizerResult[srcs.Length];

        return srcs
            .Select((x, i) => (mat: x, i, ratio: 1.0 * x.Width / x.Height))
            .OrderBy(x => x.ratio)
            .Chunk(chooseBatchSize)
            .Select(x => (result: RunMulti(x.Select(x => x.mat).ToArray()), ids: x.Select(x => x.i).ToArray()))
            .SelectMany(x => x.result.Zip(x.ids, (result, i) => (result, i)))
            .OrderBy(x => x.i)
            .Select(x => x.result)
            .ToArray();
    }

    /// <summary>
    /// Run OCR recognition on a single image.
    /// </summary>
    /// <param name="src">Image for OCR recognition.</param>
    /// <returns><see cref="PaddleOcrRecognizerResult"/> instance corresponding to OCR recognition result of the image.</returns>
    public PaddleOcrRecognizerResult Run(Mat src) => RunMulti(new[] { src }).Single();

    private unsafe PaddleOcrRecognizerResult[] RunMulti(Mat[] srcs)
    {
        if (srcs.Length == 0)
        {
            return new PaddleOcrRecognizerResult[0];
        }

        for (int i = 0; i < srcs.Length; ++i)
        {
            Mat src = srcs[i];
            if (src.Empty())
            {
                throw new ArgumentException($"src[{i}] size should not be 0, wrong input picture provided?");
            }
        }

        int modelHeight = Model.Shape.Height;
        int maxWidth = StaticShapeWidth ?? (int)Math.Ceiling(srcs.Max(src =>
        {
            Size size = src.Size();
            double width = 1.0 * size.Width / size.Height * modelHeight;
            double padded = 32 * Math.Ceiling(1.0 * width / 32);
            return padded;
        }));

        Mat[] normalizeds = null!;
        Mat final = new();
        try
        {
            normalizeds = srcs
                .Select(src =>
                {
                    using Mat channel3 = src.Channels() switch
                    {
                        4 => src.CvtColor(ColorConversionCodes.RGBA2RGB),
                        1 => src.CvtColor(ColorConversionCodes.GRAY2RGB),
                        3 => src.WeakRef(),
                        var x => throw new Exception($"Unexpect src channel: {x}, allow: (1/3/4)")
                    };
                    return ResizePadding(channel3, modelHeight, maxWidth);                    
                })
                .ToArray();
            using Mat combined = CombineMats(normalizeds, modelHeight, maxWidth);
            combined.ConvertTo(final, MatType.CV_32FC3, 2.0 / 255, -1.0);            
        }
        finally
        {
            foreach (Mat normalized in normalizeds)
            {
                normalized.Dispose();
            }
        }

        using (Tensor input = final.StackedAsTensor(srcs.Length))
        {
            _p.Inputs.Primary = input;
            _p.Run();
        }

        using (Tensor output = _p.Outputs.Primary)
        {
            Span<float> data = output.GetData<float>();
            IntPtr dataPtr = output.DangerousGetDataPtr();
            Shape shape = output.Shape;

            int labelCount = shape[2];
            int charCount = shape[1];

            return Enumerable.Range(0, shape[0])
                .Select(i =>
                {
                    StringBuilder sb = new();
                    int lastIndex = 0;
                    float score = 0;
                    for (int n = 0; n < charCount; ++n)
                    {
                        using Mat mat = new(1, labelCount, MatType.CV_32FC1, dataPtr + (n + i * charCount) * labelCount * sizeof(float));
                        int[] maxIdx = new int[2];
                        mat.MinMaxIdx(out double _, out double maxVal, new int[0], maxIdx);

                        if (maxIdx[1] > 0 && (!(n > 0 && maxIdx[1] == lastIndex)))
                        {
                            score += (float)maxVal;
                            sb.Append(Model.GetLabelByIndex(maxIdx[1]));
                        }
                        lastIndex = maxIdx[1];
                    }

                    return new PaddleOcrRecognizerResult(sb.ToString(), score / sb.Length);
                })
                .ToArray();
        }
    }

    private static Mat ResizePadding(Mat src, int modelHeight, int targetWidth)
    {
        // Calculate scaling factor
        double scale = Math.Min((double)modelHeight / src.Height, (double)targetWidth / src.Width);

        // Resize image
        Mat resized = new();
        Cv2.Resize(src, resized, new Size(), scale, scale);

        // Compute padding for height and width
        int padTop = Math.Max(0, (modelHeight - resized.Height) / 2);

        if (padTop > 0)
        {
            // Add padding. If padding needs to be added to top and bottom we divide it equally,
            // but if there is an odd number we add the extra pixel to the bottom.
            Mat result = new();
            int remainder = (modelHeight - resized.Height) % 2;
            Cv2.CopyMakeBorder(resized, result, padTop, padTop + remainder, 0, 0, BorderTypes.Constant, Scalar.Black);
            resized.Dispose();
            return result;
        }
        else
        {
            return resized;
        }
    }

    static Mat CombineMats(Mat[] srcs, int height, int width)
    {
        // 创建一个空的Mat，它的大小等于所有输入Mat的加总
        MatType matType = srcs[0].Type();
        Mat combinedMat = new(height * srcs.Length, width, matType, Scalar.Black);
        for (int i = 0; i < srcs.Length; i++)
        {
            // 将源Mat的数据复制到目标Mat的正确位置
            Mat src = srcs[i];
            using Mat dest = combinedMat[i * height, (i + 1) * height, 0, src.Width];
            srcs[i].CopyTo(dest);
        }
        return combinedMat;
    }
}
