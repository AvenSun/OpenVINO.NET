﻿using Sdcb.OpenVINO.Natives;
using System;

namespace Sdcb.OpenVINO;

using static Sdcb.OpenVINO.Natives.NativeMethods;

/// <summary>
/// Represents the model loaded from an OpenVINO.
/// </summary>
public class Model : CppPtrObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Model"/> class.
    /// </summary>
    /// <param name="handle">The handle to the native resource.</param>
    /// <param name="owned">If set to <c>true</c> the instance owns the handle.</param>
    public unsafe Model(IntPtr handle, bool owned = true) : base(handle, owned)
    {
        Inputs = new InputPortIndexer((ov_model*)Handle);
        Outputs = new OutputPortIndexer((ov_model*)Handle);
    }

    /// <summary>
    /// Provides an indexer over the input nodes in the model.
    /// </summary>
    public unsafe IPortIndexer Inputs { get; }

    /// <summary>
    /// Provides an indexer over the output nodes in the model.
    /// </summary>
    public unsafe IPortIndexer Outputs { get; }

    /// <summary>
    /// Gets the friendly name of the model.
    /// </summary>
    /// <returns>The friendly name of the model as a string.</returns>
    public unsafe string FriendlyName
    {
        get
        {
            ThrowIfDisposed();

            byte* friendlyName;
            OpenVINOException.ThrowIfFailed(ov_model_get_friendly_name((ov_model*)Handle, &friendlyName));

            return StringUtils.UTF8PtrToString((IntPtr)friendlyName)!;
        }
    }

    /// <inheritdoc/>
    protected unsafe override void ReleaseCore()
    {
        ov_model_free((ov_model*)Handle);
    }
}
