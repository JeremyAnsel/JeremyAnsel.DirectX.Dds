# JeremyAnsel.DirectX.Dds

[![Build status](https://ci.appveyor.com/api/projects/status/82tbhgrqyrxx0igv/branch/master?svg=true)](https://ci.appveyor.com/project/JeremyAnsel/jeremyansel-directx-dds/branch/master)
[![NuGet Version](https://buildstats.info/nuget/JeremyAnsel.DirectX.Dds)](https://www.nuget.org/packages/JeremyAnsel.DirectX.Dds)
![License](https://img.shields.io/github/license/JeremyAnsel/JeremyAnsel.DirectX.Dds)

JeremyAnsel.DirectX.Dds is a .Net library to handle DirectX .dds files.

Description     | Value
----------------|----------------
License         | [The MIT License (MIT)](https://github.com/JeremyAnsel/JeremyAnsel.DirectX.Dds/blob/master/LICENSE.txt)
Documentation   | http://jeremyansel.github.io/JeremyAnsel.DirectX.Dds
Source code     | https://github.com/JeremyAnsel/JeremyAnsel.DirectX.Dds
Nuget           | https://www.nuget.org/packages/JeremyAnsel.DirectX.Dds
Build           | https://ci.appveyor.com/project/JeremyAnsel/jeremyansel-directx-dds/branch/master

# Usage

```csharp
DdsDirectX.CreateTexture(
	fileName,
	d3dDevice,
	d3dDeviceContext,
	out D3D11ShaderResourceView textureView);
```
