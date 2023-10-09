﻿using Sdcb.OpenVINO.PaddleOCR.Models.Details;
using System;
using System.Collections.Generic;

namespace Sdcb.OpenVINO.PaddleOCR.Models;

/// <summary>
/// The abstract base class for recognition model.
/// </summary>
public abstract class RecognizationModel : OcrBaseModel
{
    /// <summary>
    /// Constructor for initializing an instance of the <see cref="RecognizationModel"/> class.
    /// </summary>
    /// <param name="version">The version of recognition model.</param>
    public RecognizationModel(OCRModelVersion version) : base(version)
    {
    }

    /// <summary>
    /// Get the label by specifying the index.
    /// </summary>
    /// <param name="i">The index of the label.</param>
    /// <returns>The label with the specified index.</returns>
    public abstract string GetLabelByIndex(int i);

    /// <summary>
    /// Gets a label by its index.
    /// </summary>
    /// <param name="i">The index of the label.</param>
    /// <param name="labels">The labels to search for the index.</param>
    /// <returns>The label at the specified index.</returns>
    public static string GetLabelByIndex(int i, IReadOnlyList<string> labels)
    {
        return i switch
        {
            var x when x > 0 && x <= labels.Count => labels[x - 1],
            var x when x == labels.Count + 1 => " ",
            _ => throw new Exception($"Unable to GetLabelByIndex: index {i} out of range {labels.Count}, OCR model or labels not matched?"),
        };
    }

    /// <summary>
    /// Get the OcrShape of recognition model.
    /// </summary>
    public virtual NCHW Shape => Version switch
    {
        OCRModelVersion.V2 => new(-1, 3, 32, 320),
        OCRModelVersion.V3 => new(-1, 3, 48, 320),
        OCRModelVersion.V4 => new(-1, 3, 48, 320),
        _ => throw new ArgumentOutOfRangeException($"Unknown OCR model version: {Version}."),
    };

    /// <summary>
    /// Create the RecognizationModel object with the specified directory path, label path and model version.
    /// </summary>
    /// <param name="directoryPath">The directory path of recognition model.</param>
    /// <param name="labelPath">The label path of recognition model.</param>
    /// <param name="version">The version of recognition model.</param>
    /// <returns>The RecognizationModel object created with the specified directory path, label path and model version.</returns>
    public static RecognizationModel FromDirectory(string directoryPath, string labelPath, OCRModelVersion version) => new FileRecognizationModel(directoryPath, labelPath, version);
}
