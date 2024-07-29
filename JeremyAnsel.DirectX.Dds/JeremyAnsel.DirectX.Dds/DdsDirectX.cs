using JeremyAnsel.DirectX.D3D11;
using JeremyAnsel.DirectX.Dxgi;
using JeremyAnsel.Media.Dds;
using System.Diagnostics.CodeAnalysis;

namespace JeremyAnsel.DirectX.Dds
{
    public static class DdsDirectX
    {
        public static void CreateTexture(
            string fileName,
            D3D11Device device,
            D3D11DeviceContext context,
            out D3D11ShaderResourceView textureView)
        {
            DdsFile dds = DdsFile.FromFile(fileName);
            CreateTexture(dds, device, context, out textureView);
        }

        public static void CreateTexture(
            string fileName,
            D3D11Device device,
            D3D11DeviceContext context,
            out D3D11Resource texture,
            out D3D11ShaderResourceView textureView)
        {
            DdsFile dds = DdsFile.FromFile(fileName);
            CreateTexture(dds, device, context, out texture, out textureView);
        }

        public static void CreateTexture(
            Stream stream,
            D3D11Device device,
            D3D11DeviceContext context,
            out D3D11ShaderResourceView textureView)
        {
            DdsFile dds = DdsFile.FromStream(stream);
            CreateTexture(dds, device, context, out textureView);
        }

        public static void CreateTexture(
            Stream stream,
            D3D11Device device,
            D3D11DeviceContext context,
            out D3D11Resource texture,
            out D3D11ShaderResourceView textureView)
        {
            DdsFile dds = DdsFile.FromStream(stream);
            CreateTexture(dds, device, context, out texture, out textureView);
        }

        public static void CreateTexture(
            DdsFile dds,
            D3D11Device device,
            D3D11DeviceContext context,
            out D3D11ShaderResourceView textureView)
        {
            CreateTexture(dds, device, context, 0, out D3D11Resource texture, out textureView, out _);
            D3D11Utils.DisposeAndNull(ref texture);
        }

        public static void CreateTexture(
            DdsFile dds,
            D3D11Device device,
            D3D11DeviceContext context,
            out D3D11Resource texture,
            out D3D11ShaderResourceView textureView)
        {
            CreateTexture(dds, device, context, 0, out texture, out textureView, out _);
        }

        public static void CreateTexture(
            DdsFile dds,
            D3D11Device device,
            D3D11DeviceContext context,
            int maxSize,
            out D3D11ShaderResourceView textureView)
        {
            CreateTexture(dds, device, context, maxSize, out D3D11Resource texture, out textureView, out _);
            D3D11Utils.DisposeAndNull(ref texture);
        }

        public static void CreateTexture(
            DdsFile dds,
            D3D11Device device,
            D3D11DeviceContext context,
            int maxSize,
            out D3D11Resource texture,
            out D3D11ShaderResourceView textureView,
            out DdsAlphaMode alphaMode)
        {
            CreateTexture(
                dds,
                device,
                context,
                maxSize,
                D3D11Usage.Default,
                D3D11BindOptions.ShaderResource,
                D3D11CpuAccessOptions.None,
                D3D11ResourceMiscOptions.None,
                false,
                out texture,
                out textureView,
                out alphaMode);
        }

        public static void CreateTexture(
            DdsFile dds,
            D3D11Device device,
            D3D11DeviceContext context,
            int maxSize,
            D3D11Usage usage,
            D3D11BindOptions bindOptions,
            D3D11CpuAccessOptions cpuAccessOptions,
            D3D11ResourceMiscOptions miscOptions,
            bool forceSRGB,
            out D3D11Resource texture,
            out D3D11ShaderResourceView textureView,
            out DdsAlphaMode alphaMode)
        {
            if (dds == null)
            {
                throw new ArgumentNullException(nameof(dds));
            }

            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            CreateTextureFromDDS(
                device,
                context,
                dds,
                dds.Data,
                maxSize,
                usage,
                bindOptions,
                cpuAccessOptions,
                miscOptions,
                forceSRGB,
                out texture,
                out textureView);

            alphaMode = dds.AlphaMode;
        }

        private static bool FillInitData(
            int width,
            int height,
            int depth,
            int mipCount,
            int arraySize,
            DxgiFormat format,
            int maxSize,
            byte[] bitData,
            out int twidth,
            out int theight,
            out int tdepth,
            out int skipMip,
            out D3D11SubResourceData[] initData)
        {
            skipMip = 0;
            twidth = 0;
            theight = 0;
            tdepth = 0;
            initData = new D3D11SubResourceData[mipCount * arraySize];

            int pSrcBits = 0;
            int index = 0;

            for (int j = 0; j < arraySize; j++)
            {
                int w = width;
                int h = height;
                int d = depth;

                for (int i = 0; i < mipCount; i++)
                {
                    DdsHelpers.GetSurfaceInfo(w, h, (DdsFormat)format, out int NumBytes, out int RowBytes, out _);

                    if ((mipCount <= 1) || maxSize == 0 || (w <= maxSize && h <= maxSize && d <= maxSize))
                    {
                        if (twidth == 0)
                        {
                            twidth = w;
                            theight = h;
                            tdepth = d;
                        }

                        int dataLength = NumBytes * d;
                        var data = new byte[dataLength];
                        Array.Copy(bitData, pSrcBits, data, 0, dataLength);

                        initData[index] = new D3D11SubResourceData(data, (uint)RowBytes, (uint)NumBytes);
                        index++;
                    }
                    else if (j == 0)
                    {
                        // Count number of skipped mipmaps (first item only)
                        skipMip++;
                    }

                    pSrcBits += NumBytes * d;

                    w >>= 1;
                    h >>= 1;
                    d >>= 1;

                    if (w == 0)
                    {
                        w = 1;
                    }

                    if (h == 0)
                    {
                        h = 1;
                    }

                    if (d == 0)
                    {
                        d = 1;
                    }
                }
            }

            return index > 0;
        }

        private static void CreateD3DResources(
            D3D11Device device,
            D3D11ResourceDimension resDim,
            int width,
            int height,
            int depth,
            int mipCount,
            int arraySize,
            DxgiFormat format,
            D3D11Usage usage,
            D3D11BindOptions bindFlags,
            D3D11CpuAccessOptions cpuAccessFlags,
            D3D11ResourceMiscOptions miscFlags,
            bool forceSRGB,
            bool isCubeMap,
            D3D11SubResourceData[] initData,
            out D3D11Resource texture,
            out D3D11ShaderResourceView textureView)
        {
            texture = null;
            textureView = null;

            if (forceSRGB)
            {
                format = (DxgiFormat)DdsHelpers.MakeSrgb((DdsFormat)format);
            }

            switch (resDim)
            {
                case D3D11ResourceDimension.Texture1D:
                    {
                        var desc = new D3D11Texture1DDesc(
                            format,
                            (uint)width,
                            (uint)arraySize,
                            (uint)mipCount,
                            bindFlags,
                            usage,
                            cpuAccessFlags,
                            miscFlags & ~D3D11ResourceMiscOptions.TextureCube);

                        texture = device.CreateTexture1D(desc, initData);

                        try
                        {
                            var SRVDesc = new D3D11ShaderResourceViewDesc
                            {
                                Format = format
                            };

                            if (arraySize > 1)
                            {
                                SRVDesc.ViewDimension = D3D11SrvDimension.Texture1DArray;
                                SRVDesc.Texture1DArray = new D3D11Texture1DArraySrv
                                {
                                    MipLevels = (mipCount == 0) ? uint.MaxValue : desc.MipLevels,
                                    ArraySize = (uint)arraySize
                                };
                            }
                            else
                            {
                                SRVDesc.ViewDimension = D3D11SrvDimension.Texture1D;
                                SRVDesc.Texture1D = new D3D11Texture1DSrv
                                {
                                    MipLevels = (mipCount == 0) ? uint.MaxValue : desc.MipLevels
                                };
                            }

                            textureView = device.CreateShaderResourceView(texture, SRVDesc);
                        }
                        catch
                        {
                            D3D11Utils.DisposeAndNull(ref texture);
                            throw;
                        }

                        break;
                    }

                case D3D11ResourceDimension.Texture2D:
                    {
                        var desc = new D3D11Texture2DDesc(
                            format,
                            (uint)width,
                            (uint)height,
                            (uint)arraySize,
                            (uint)mipCount,
                            bindFlags,
                            usage,
                            cpuAccessFlags,
                            1,
                            0);

                        if (isCubeMap)
                        {
                            desc.MiscOptions = miscFlags | D3D11ResourceMiscOptions.TextureCube;
                        }
                        else
                        {
                            desc.MiscOptions = miscFlags & ~D3D11ResourceMiscOptions.TextureCube;
                        }

                        if (format == DxgiFormat.BC1UNorm || format == DxgiFormat.BC2UNorm || format == DxgiFormat.BC3UNorm)
                        {
                            if ((width & 3) != 0 || (height & 3) != 0)
                            {
                                desc.Width = (uint)(width + 3) & ~3U;
                                desc.Height = (uint)(height + 3) & ~3U;
                            }
                        }

                        texture = device.CreateTexture2D(desc, initData);

                        try
                        {
                            var SRVDesc = new D3D11ShaderResourceViewDesc
                            {
                                Format = format
                            };

                            if (isCubeMap)
                            {
                                if (arraySize > 6)
                                {
                                    SRVDesc.ViewDimension = D3D11SrvDimension.TextureCubeArray;
                                    SRVDesc.TextureCubeArray = new D3D11TextureCubeArraySrv
                                    {
                                        MipLevels = (mipCount == 0) ? uint.MaxValue : desc.MipLevels,
                                        // Earlier we set arraySize to (NumCubes * 6)
                                        NumCubes = (uint)arraySize / 6
                                    };
                                }
                                else
                                {
                                    SRVDesc.ViewDimension = D3D11SrvDimension.TextureCube;
                                    SRVDesc.TextureCube = new D3D11TextureCubeSrv
                                    {
                                        MipLevels = (mipCount == 0) ? uint.MaxValue : desc.MipLevels,
                                    };
                                }
                            }
                            else if (arraySize > 1)
                            {
                                SRVDesc.ViewDimension = D3D11SrvDimension.Texture2DArray;
                                SRVDesc.Texture2DArray = new D3D11Texture2DArraySrv
                                {
                                    MipLevels = (mipCount == 0) ? uint.MaxValue : desc.MipLevels,
                                    ArraySize = (uint)arraySize
                                };
                            }
                            else
                            {
                                SRVDesc.ViewDimension = D3D11SrvDimension.Texture2D;
                                SRVDesc.Texture2D = new D3D11Texture2DSrv
                                {
                                    MipLevels = (mipCount == 0) ? uint.MaxValue : desc.MipLevels,
                                };
                            }

                            textureView = device.CreateShaderResourceView(texture, SRVDesc);
                        }
                        catch
                        {
                            D3D11Utils.DisposeAndNull(ref texture);
                            throw;
                        }

                        break;
                    }

                case D3D11ResourceDimension.Texture3D:
                    {
                        var desc = new D3D11Texture3DDesc(
                            format,
                            (uint)width,
                            (uint)height,
                            (uint)depth,
                            (uint)mipCount,
                            bindFlags,
                            usage,
                            cpuAccessFlags,
                            miscFlags & ~D3D11ResourceMiscOptions.TextureCube);

                        texture = device.CreateTexture3D(desc, initData);

                        try
                        {
                            var SRVDesc = new D3D11ShaderResourceViewDesc
                            {
                                Format = format,
                                ViewDimension = D3D11SrvDimension.Texture3D,
                                Texture3D = new D3D11Texture3DSrv
                                {
                                    MipLevels = (mipCount == 0) ? uint.MaxValue : desc.MipLevels,
                                }
                            };

                            textureView = device.CreateShaderResourceView(texture, SRVDesc);
                        }
                        catch
                        {
                            D3D11Utils.DisposeAndNull(ref texture);
                            throw;
                        }

                        break;
                    }
            }
        }

        private static bool IsBitMask(DdsPixelFormat ddpf, uint r, uint g, uint b, uint a)
        {
            return ddpf.RedBitMask == r && ddpf.GreenBitMask == g && ddpf.BlueBitMask == b && ddpf.AlphaBitMask == a;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        private static void CreateTextureFromDDS(
            D3D11Device device,
            D3D11DeviceContext context,
            DdsFile dds,
            byte[] bitData,
            int maxSize,
            D3D11Usage usage,
            D3D11BindOptions bindOptions,
            D3D11CpuAccessOptions cpuAccessOptions,
            D3D11ResourceMiscOptions miscOptions,
            bool forceSRGB,
            out D3D11Resource texture,
            out D3D11ShaderResourceView textureView)
        {
            int width = dds.Width;
            int height = dds.Height;
            int depth = dds.Depth;

            D3D11ResourceDimension resDim = (D3D11ResourceDimension)dds.ResourceDimension;
            int arraySize = Math.Max(1, dds.ArraySize);
            DxgiFormat format = (DxgiFormat)dds.Format;
            bool isCubeMap = false;

            if (dds.Format == DdsFormat.Unknown)
            {
                if (dds.PixelFormat.RgbBitCount == 32)
                {
                    if (IsBitMask(dds.PixelFormat, 0x000000ff, 0x0000ff00, 0x00ff0000, 0x00000000))
                    {
                        format = DxgiFormat.B8G8R8X8UNorm;
                        int length = bitData.Length / 4;
                        var bytes = new byte[length * 4];

                        for (int i = 0; i < length; i++)
                        {
                            bytes[i * 4 + 0] = bitData[i * 4 + 2];
                            bytes[i * 4 + 1] = bitData[i * 4 + 1];
                            bytes[i * 4 + 2] = bitData[i * 4 + 0];
                        }

                        bitData = bytes;
                    }
                }
                else if (dds.PixelFormat.RgbBitCount == 24)
                {
                    if (IsBitMask(dds.PixelFormat, 0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000))
                    {
                        format = DxgiFormat.B8G8R8X8UNorm;
                        int length = bitData.Length / 3;
                        var bytes = new byte[length * 4];

                        for (int i = 0; i < length; i++)
                        {
                            bytes[i * 4 + 0] = bitData[i * 3 + 0];
                            bytes[i * 4 + 1] = bitData[i * 3 + 1];
                            bytes[i * 4 + 2] = bitData[i * 3 + 2];
                        }

                        bitData = bytes;
                    }
                }
            }

            int mipCount = Math.Max(1, dds.MipmapCount);

            switch (format)
            {
                case DxgiFormat.AI44:
                case DxgiFormat.IA44:
                case DxgiFormat.P8:
                case DxgiFormat.A8P8:
                    throw new NotSupportedException(format.ToString() + " format is not supported.");

                default:
                    if (DdsHelpers.GetBitsPerPixel((DdsFormat)format) == 0)
                    {
                        throw new NotSupportedException(format.ToString() + " format is not supported.");
                    }

                    break;
            }

            switch (resDim)
            {
                case D3D11ResourceDimension.Texture1D:
                    // D3DX writes 1D textures with a fixed Height of 1
                    if ((dds.Options & DdsOptions.Height) != 0 && height != 1)
                    {
                        throw new InvalidDataException();
                    }

                    height = 1;
                    depth = 1;
                    break;

                case D3D11ResourceDimension.Texture2D:
                    if ((dds.ResourceMiscOptions & DdsResourceMiscOptions.TextureCube) != 0)
                    {
                        arraySize *= 6;
                        isCubeMap = true;
                    }

                    depth = 1;
                    break;

                case D3D11ResourceDimension.Texture3D:
                    if ((dds.Options & DdsOptions.Depth) == 0)
                    {
                        throw new InvalidDataException();
                    }

                    if (arraySize > 1)
                    {
                        throw new NotSupportedException();
                    }
                    break;

                default:
                    if ((dds.Options & DdsOptions.Depth) != 0)
                    {
                        resDim = D3D11ResourceDimension.Texture3D;
                    }
                    else
                    {
                        if ((dds.Caps2 & DdsAdditionalCaps.CubeMap) != 0)
                        {
                            // We require all six faces to be defined
                            if ((dds.Caps2 & DdsAdditionalCaps.CubeMapAllFaces) != DdsAdditionalCaps.CubeMapAllFaces)
                            {
                                throw new NotSupportedException();
                            }

                            arraySize = 6;
                            isCubeMap = true;
                        }

                        depth = 1;
                        resDim = D3D11ResourceDimension.Texture2D;

                        // Note there's no way for a legacy Direct3D 9 DDS to express a '1D' texture
                    }

                    break;
            }

            if ((miscOptions & D3D11ResourceMiscOptions.TextureCube) != 0
                && resDim == D3D11ResourceDimension.Texture2D
                && (arraySize % 6 == 0))
            {
                isCubeMap = true;
            }

            // Bound sizes (for security purposes we don't trust DDS file metadata larger than the D3D 11.x hardware requirements)
            if (mipCount > D3D11Constants.ReqMipLevels)
            {
                throw new NotSupportedException();
            }

            switch (resDim)
            {
                case D3D11ResourceDimension.Texture1D:
                    if (arraySize > D3D11Constants.ReqTexture1DArrayAxisDimension
                        || width > D3D11Constants.ReqTexture1DDimension)
                    {
                        throw new NotSupportedException();
                    }

                    break;

                case D3D11ResourceDimension.Texture2D:
                    if (isCubeMap)
                    {
                        // This is the right bound because we set arraySize to (NumCubes*6) above
                        if (arraySize > D3D11Constants.ReqTexture2DArrayAxisDimension
                            || width > D3D11Constants.ReqTextureCubeDimension
                            || height > D3D11Constants.ReqTextureCubeDimension)
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else if (arraySize > D3D11Constants.ReqTexture2DArrayAxisDimension
                        || width > D3D11Constants.ReqTexture2DDimension
                        || height > D3D11Constants.ReqTexture2DDimension)
                    {
                        throw new NotSupportedException();
                    }

                    break;

                case D3D11ResourceDimension.Texture3D:
                    if (arraySize > 1
                        || width > D3D11Constants.ReqTexture3DDimension
                        || height > D3D11Constants.ReqTexture3DDimension
                        || depth > D3D11Constants.ReqTexture3DDimension)
                    {
                        throw new NotSupportedException();
                    }

                    break;

                default:
                    throw new NotSupportedException();
            }

            bool autogen = false;

            if (mipCount == 1)
            {
                // See if format is supported for auto-gen mipmaps (varies by feature level)
                if (!device.CheckFormatSupport(format, out D3D11FormatSupport fmtSupport)
                    && (fmtSupport & D3D11FormatSupport.MipAutogen) != 0)
                {
                    // 10level9 feature levels do not support auto-gen mipgen for volume textures
                    if (resDim != D3D11ResourceDimension.Texture3D
                        || device.FeatureLevel >= D3D11FeatureLevel.FeatureLevel100)
                    {
                        autogen = true;
                    }
                }
            }

            if (autogen)
            {
                // Create texture with auto-generated mipmaps
                CreateD3DResources(
                    device,
                    resDim,
                    width,
                    height,
                    depth,
                    0,
                    arraySize,
                    format,
                    usage,
                    bindOptions | D3D11BindOptions.RenderTarget,
                    cpuAccessOptions,
                    miscOptions | D3D11ResourceMiscOptions.GenerateMips,
                    forceSRGB,
                    isCubeMap,
                    null,
                    out texture,
                    out textureView);

                try
                {
                    DdsHelpers.GetSurfaceInfo(width, height, (DdsFormat)format, out int numBytes, out int rowBytes, out int numRows);

                    if (numBytes > bitData.Length)
                    {
                        throw new EndOfStreamException();
                    }

                    D3D11ShaderResourceViewDesc desc = textureView.Description;
                    uint mipLevels = 1;

                    switch (desc.ViewDimension)
                    {
                        case D3D11SrvDimension.Texture1D:
                            mipLevels = desc.Texture1D.MipLevels;
                            break;

                        case D3D11SrvDimension.Texture1DArray:
                            mipLevels = desc.Texture1DArray.MipLevels;
                            break;

                        case D3D11SrvDimension.Texture2D:
                            mipLevels = desc.Texture2D.MipLevels;
                            break;

                        case D3D11SrvDimension.Texture2DArray:
                            mipLevels = desc.Texture2DArray.MipLevels;
                            break;

                        case D3D11SrvDimension.TextureCube:
                            mipLevels = desc.TextureCube.MipLevels;
                            break;

                        case D3D11SrvDimension.TextureCubeArray:
                            mipLevels = desc.TextureCubeArray.MipLevels;
                            break;

                        case D3D11SrvDimension.Texture3D:
                            mipLevels = desc.Texture3D.MipLevels;
                            break;

                        default:
                            throw new InvalidDataException();
                    }

                    if (arraySize > 1)
                    {
                        int pSrcBits = 0;

                        for (uint item = 0; item < (uint)arraySize; item++)
                        {
                            if (pSrcBits + numBytes > bitData.Length)
                            {
                                throw new EndOfStreamException();
                            }

                            var data = new byte[numBytes];
                            Array.Copy(bitData, pSrcBits, data, 0, numBytes);

                            uint res = D3D11Utils.CalcSubresource(0, item, mipLevels);
                            context.UpdateSubresource(texture, res, null, data, (uint)rowBytes, (uint)numBytes);

                            pSrcBits += numBytes;
                        }
                    }
                    else
                    {
                        context.UpdateSubresource(texture, 0, null, bitData, (uint)rowBytes, (uint)numBytes);
                    }

                    context.GenerateMips(textureView);
                }
                catch
                {
                    D3D11Utils.DisposeAndNull(ref textureView);
                    D3D11Utils.DisposeAndNull(ref texture);
                    throw;
                }
            }
            else
            {
                // Create the texture

                if (!FillInitData(
                    width,
                    height,
                    depth,
                    mipCount,
                    arraySize,
                    format,
                    maxSize,
                    bitData,
                    out int twidth,
                    out int theight,
                    out int tdepth,
                    out int skipMip,
                    out D3D11SubResourceData[] initData))
                {
                    throw new InvalidDataException();
                }

                try
                {
                    CreateD3DResources(
                        device,
                        resDim,
                        twidth,
                        theight,
                        tdepth,
                        mipCount - skipMip,
                        arraySize,
                        format,
                        usage,
                        bindOptions,
                        cpuAccessOptions,
                        miscOptions,
                        forceSRGB,
                        isCubeMap,
                        initData,
                        out texture,
                        out textureView);
                }
                catch
                {
                    if (maxSize == 0 && mipCount > 1)
                    {
                        // Retry with a maxsize determined by feature level
                        switch (device.FeatureLevel)
                        {
                            case D3D11FeatureLevel.FeatureLevel91:
                            case D3D11FeatureLevel.FeatureLevel92:
                                if (isCubeMap)
                                {
                                    maxSize = (int)D3D11Constants.FeatureLevel91ReqTextureCubeDimension;
                                }
                                else
                                {
                                    maxSize = resDim == D3D11ResourceDimension.Texture3D
                                        ? (int)D3D11Constants.FeatureLevel91ReqTexture3DDimension
                                        : (int)D3D11Constants.FeatureLevel91ReqTexture2DDimension;
                                }

                                break;

                            case D3D11FeatureLevel.FeatureLevel93:
                                maxSize = resDim == D3D11ResourceDimension.Texture3D
                                    ? (int)D3D11Constants.FeatureLevel91ReqTexture3DDimension
                                    : (int)D3D11Constants.FeatureLevel93ReqTexture2DDimension;
                                break;

                            case D3D11FeatureLevel.FeatureLevel100:
                            case D3D11FeatureLevel.FeatureLevel101:
                                maxSize = resDim == D3D11ResourceDimension.Texture3D
                                    ? (int)D3D11Constants.D3D10ReqTexture3DDimension
                                    : (int)D3D11Constants.D3D10ReqTexture2DDimension;
                                break;

                            default:
                                maxSize = resDim == D3D11ResourceDimension.Texture3D
                                    ? (int)D3D11Constants.ReqTexture3DDimension
                                    : (int)D3D11Constants.ReqTexture2DDimension;
                                break;
                        }

                        if (!FillInitData(
                            width,
                            height,
                            depth,
                            mipCount,
                            arraySize,
                            format,
                            maxSize,
                            bitData,
                            out twidth,
                            out theight,
                            out tdepth,
                            out skipMip,
                            out initData))
                        {
                            throw new InvalidDataException();
                        }

                        CreateD3DResources(
                            device,
                            resDim,
                            twidth,
                            theight,
                            tdepth,
                            mipCount - skipMip,
                            arraySize,
                            format,
                            usage,
                            bindOptions,
                            cpuAccessOptions,
                            miscOptions,
                            forceSRGB,
                            isCubeMap,
                            initData,
                            out texture,
                            out textureView);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
