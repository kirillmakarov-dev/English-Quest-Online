# Applies Build Size Reduction texture/FBX import settings via .meta YAML (run when Unity is closed, then reopen to reimport).
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..\..\..")).Path
)

$ErrorActionPreference = "Stop"

function Update-TextureMetaFile {
    param(
        [string]$MetaPath,
        [int]$MaxSize,
        [bool]$IsNormal = $false,
        [bool]$IsSprite = $false,
        [bool]$IsSkybox = $false,
        [bool]$UseCrunch = $false
    )

    if (-not (Test-Path $MetaPath)) { return $false }
    $content = Get-Content -Raw -LiteralPath $MetaPath
    if ($content -notmatch "TextureImporter:") { return $false }

    $original = $content

    $content = $content -replace "isReadable: 1", "isReadable: 0"
    if ($IsSprite) {
        $content = $content -replace "enableMipMap: 1", "enableMipMap: 0"
    }
    if ($IsSkybox) {
        $content = $content -replace "streamingMipmaps: 0", "streamingMipmaps: 1"
        $content = $content -replace "textureShape: 1", "textureShape: 2"
    }
    if ($IsNormal) {
        $content = $content -replace "textureType: 0", "textureType: 1"
        $content = $content -replace "sRGBTexture: 1", "sRGBTexture: 0"
    }

    # Cap oversized platform max sizes
    $content = $content -replace "maxTextureSize: 8192", "maxTextureSize: $MaxSize"
    if ($MaxSize -le 2048) {
        $content = $content -replace "maxTextureSize: 4096", "maxTextureSize: $MaxSize"
    }

    # Enable compression where disabled
    $content = $content -replace "textureCompression: 0", "textureCompression: 1"

    if ($UseCrunch) {
        $content = $content -replace "crunchedCompression: 0", "crunchedCompression: 1"
    }

    if ($content -ne $original) {
        Set-Content -LiteralPath $MetaPath -Value $content -NoNewline
        return $true
    }
    return $false
}

function Update-FbxMetaFile {
    param([string]$MetaPath)
    if (-not (Test-Path $MetaPath)) { return $false }
    $content = Get-Content -Raw -LiteralPath $MetaPath
    if ($content -notmatch "ModelImporter:") { return $false }
    $original = $content
    $content = $content -replace "animationCompression: 1", "animationCompression: 3"
    $content = $content -replace "meshCompression: 0", "meshCompression: 2"
    $content = $content -replace "importCameras: 1", "importCameras: 0"
    $content = $content -replace "importLights: 1", "importLights: 0"
    if ($content -ne $original) {
        Set-Content -LiteralPath $MetaPath -Value $content -NoNewline
        return $true
    }
    return $false
}

function Test-NormalMapName {
    param([string]$Name)
    $n = $Name.ToLowerInvariant()
    return ($n -match "_n\.(png|tga|psd|tif)$") -or ($n -match "normal") -or ($n -match "_n_")
}

function Test-MaskMapName {
    param([string]$Name)
    $n = $Name.ToLowerInvariant()
    return ($n -match "metallic") -or ($n -match "metsmooth") -or ($n -match "_mso") -or ($n -match "_as\.") -or ($n -match "smoothness") -or ($n -match "emit")
}

$stats = @{ Skybox = 0; UI = 0; Env = 0; Fbx = 0 }

# Skyboxes
$skyFolder = Join-Path $ProjectRoot "Assets\_ThirdParty\8K Skybox Pack Free\Skyboxes\Texture"
Get-ChildItem -LiteralPath $skyFolder -Filter "*.meta" -File | ForEach-Object {
    if ($_.DirectoryName -match "Materials") { return }
    if (Update-TextureMetaFile -MetaPath $_.FullName -MaxSize 2048 -IsSkybox $true) { $stats.Skybox++ }
}
foreach ($extra in @(
    "Assets\_ThirdParty\Yanshi - Stylized Rocky Island Environment\Textures\sky\sky_cloud.psd.meta",
    "Assets\Polyart\PolyartStudio\DreamscapeCastle\Textures\Sky\T_SkyboxCastle_C.png.meta",
    "Assets\_ThirdParty\Polytope Studio\Lowpoly_Environments\Sources\Textures\PT_Skybox_Texture_01.png.meta"
)) {
    $p = Join-Path $ProjectRoot $extra
    if (Update-TextureMetaFile -MetaPath $p -MaxSize 2048 -IsSkybox $true) { $stats.Skybox++ }
}

# UI sprites
$spritesRoot = Join-Path $ProjectRoot "Assets\_OurAssets\Art\Sprites"
Get-ChildItem -LiteralPath $spritesRoot -Filter "*.meta" -Recurse -File | ForEach-Object {
    if (Update-TextureMetaFile -MetaPath $_.FullName -MaxSize 1024 -IsSprite $true -UseCrunch $true) { $stats.UI++ }
}

# Environment packs
$envFolders = @(
    "Assets\_ThirdParty\@PaulosCreations\RunesAndPortals",
    "Assets\Polyart\PolyartStudio\DreamscapeCastle\Textures",
    "Assets\_ThirdParty\Yanshi - Stylized Rocky Island Environment\Textures",
    "Assets\_ThirdParty\Sci-Fi Tomb\Textures"
)
foreach ($rel in $envFolders) {
    $folder = Join-Path $ProjectRoot $rel
    if (-not (Test-Path $folder)) { continue }
    Get-ChildItem -LiteralPath $folder -Filter "*.meta" -Recurse -File | ForEach-Object {
        $base = $_.BaseName
        $isNormal = Test-NormalMapName $base
        $isMask = Test-MaskMapName $base
        $max = if ($isMask) { 1024 } elseif ($isNormal) { 2048 } else { 2048 }
        $crunch = -not ($isNormal -or $isMask)
        if (Update-TextureMetaFile -MetaPath $_.FullName -MaxSize $max -IsNormal $isNormal -UseCrunch $crunch) { $stats.Env++ }
    }
}

# Animal FBX
foreach ($rel in @(
    "Assets\_ThirdParty\UnRealProject\Animals\owl.FBX.meta",
    "Assets\_ThirdParty\UnRealProject\Animals\lion.FBX.meta"
)) {
    $p = Join-Path $ProjectRoot $rel
    if (Update-FbxMetaFile -MetaPath $p) { $stats.Fbx++ }
}

Write-Output "Texture meta optimizations applied:"
Write-Output "  Skybox: $($stats.Skybox)"
Write-Output "  UI: $($stats.UI)"
Write-Output "  Environment: $($stats.Env)"
Write-Output "  FBX: $($stats.Fbx)"
Write-Output "Reopen Unity (or focus project) to trigger reimport."
