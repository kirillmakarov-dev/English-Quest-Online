param(
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.3f1\Editor\Unity.exe",
    [string]$ProjectPath = "C:\Portfolio Projects\English-Quest-Online\English Quest online",
    [string]$RepositoryRoot = "C:\Portfolio Projects\English-Quest-Online",
    [switch]$OpenLogWhenFinished
)

$ErrorActionPreference = "Stop"

$testNames = @(
    "EnglishQuest.Tests.QuestSystem.QuestLineRegistrarTests.SoloFirstFlow_ActualMvpRegistryAsset_AllowsOnePlayerToUnlockAdaBenNoraSequentially",
    "EnglishQuest.Tests.PortfolioDemoValidatorAnalyzerTests.ActualPortfolioDemoScene_PassesStaticSoloFirstValidation",
    "EnglishQuest.Tests.PortfolioDemoValidatorAnalyzerTests.ActualPortfolioDemoScene_UsesOptionalMultiplayerControlsCopy",
    "EnglishQuest.Tests.PortfolioDemoValidatorAnalyzerTests.ActualQuestLineAssets_PreserveAdaBenNoraSoloChain",
    "EnglishQuest.Tests.PortfolioDemoValidatorAnalyzerTests.ActualQuestDefinitionAssets_PreserveSoloFirstLessonBindings",
    "EnglishQuest.Tests.PortfolioDemoValidatorAnalyzerTests.ActualCommittedQuestRegistry_PassesValidatorForSoloFirstMvpFlow",
    "EnglishQuest.Tests.PortfolioDemoValidatorAnalyzerTests.ActualOpenWorldNetworkSessionProfile_PassesValidatorForTwoPlayerSharedSlice",
    "EnglishQuest.Tests.PortfolioDemoValidatorAnalyzerTests.ActualShowcaseDocs_PassSoloFirstDocumentationValidation"
)

$logPath = Join-Path $RepositoryRoot "unity-solo-first-editmode.log"
$resultsPath = Join-Path $RepositoryRoot "unity-solo-first-editmode-results.xml"
$testFilter = [string]::Join(";", $testNames)

if (-not (Test-Path $UnityPath)) {
    throw "Unity executable was not found: $UnityPath"
}

if (-not (Test-Path $ProjectPath)) {
    throw "Unity project was not found: $ProjectPath"
}

$openProjectEditors = Get-Process Unity -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowTitle -like "*English Quest online*" }

if ($openProjectEditors) {
    Write-Host "Cannot run solo-first Unity batchmode tests while the English Quest project is open in the Unity Editor." -ForegroundColor Yellow
    Write-Host "Close the Unity window for this project, then run this script again." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Detected Unity editor instance(s):"
    $openProjectEditors | ForEach-Object {
        Write-Host ("- PID {0}: {1}" -f $_.Id, $_.MainWindowTitle)
    }

    exit 2
}

$arguments = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-runTests",
    "-testPlatform", "editmode",
    "-testFilter", $testFilter,
    "-testResults", $resultsPath,
    "-logFile", $logPath,
    "-quit"
)

Write-Host "Running targeted solo-first Unity Edit Mode tests..."
Write-Host "Unity: $UnityPath"
Write-Host "Project: $ProjectPath"
Write-Host "Results: $resultsPath"
Write-Host "Log: $logPath"
Write-Host "Coverage:"
Write-Host "- actual solo-first quest unlock chain"
Write-Host "- committed PortfolioDemo scene wiring"
Write-Host "- committed optional multiplayer HUD copy"
Write-Host "- committed quest-line and quest-definition assets"
Write-Host "- committed MVP quest registry"
Write-Host "- committed Open World network session profile"
Write-Host "- reviewer-facing solo-first docs"
Write-Host ""

& $UnityPath $arguments
$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "Unity exit code: $exitCode"

if ($OpenLogWhenFinished -and (Test-Path $logPath)) {
    Start-Process notepad.exe $logPath | Out-Null
}

exit $exitCode
