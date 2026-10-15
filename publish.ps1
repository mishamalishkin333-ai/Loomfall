# Publish "Loomfall" (Tkach Yarusov) to GitHub Pages in one run.
# ASCII only on purpose: Windows PowerShell 5.1 misreads UTF-8 without BOM.
#
#   powershell -ExecutionPolicy Bypass -File publish.ps1 -DryRun
#   powershell -ExecutionPolicy Bypass -File publish.ps1 -Repo loomfall
#
# One-time setup:  winget install --id GitHub.cli   then   gh auth login
param(
    [string]$Repo = 'loomfall',
    [string]$Owner = '',
    [switch]$DryRun
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

function Step([string]$text) { Write-Host "== $text" -ForegroundColor Cyan }

Step 'Environment'
$git = (Get-Command git -ErrorAction SilentlyContinue).Source
$gh = (Get-Command gh -ErrorAction SilentlyContinue).Source
"git: $git"
"gh:  $gh"
if (-not $gh) {
    Write-Host 'gh not found. Install it: winget install --id GitHub.cli' -ForegroundColor Yellow
}

if ($gh -and -not $Owner) {
    $Owner = (& $gh api user --jq .login 2>$null | Out-String).Trim()
    if (-not $Owner) {
        Write-Host 'Not signed in yet. Run: gh auth login' -ForegroundColor Yellow
    }
}

Step 'Already built'
$docs = Join-Path $root 'docs'
$size = [math]::Round(((Get-ChildItem $docs -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 2)
"docs archive: $size MB"
"index.html at root: $(Test-Path (Join-Path $docs 'index.html'))"
"privacy page:       $(Test-Path (Join-Path $docs 'privacy.html'))"
"commits: $((& $git rev-list --count HEAD))"

$account = if ($Owner) { $Owner } else { 'YOUR-NAME' }

if ($DryRun) {
    Step 'Dry run: nothing is published'
    "Would create: repository $account/$Repo (branch main), Pages from /docs"
    "Game URL:    https://$account.github.io/$Repo/"
    "Privacy URL: https://$account.github.io/$Repo/privacy.html"
    exit 0
}

if (-not $gh -or -not $Owner) {
    Write-Host 'Cannot publish without gh and a signed-in account. See steps above.' -ForegroundColor Yellow
    exit 1
}

Step 'Creating the repository and pushing the code'
& $gh repo create "$Owner/$Repo" --public --source . --push --description 'Loomfall - Unity WebGL game'

Step 'Enabling GitHub Pages (branch main, folder /docs)'
& $gh api --method POST "repos/$Owner/$Repo/pages" -f 'source[branch]=main' -f 'source[path]=/docs'

$app = "https://$Owner.github.io/$Repo/"
$privacy = "https://$Owner.github.io/$Repo/privacy.html"

Step 'Links for the VK card'
"App URL:     $app"
"Privacy URL: $privacy"

Step 'Waiting for the first deploy (about a minute)'
Start-Sleep -Seconds 45
foreach ($url in @($app, $privacy)) {
    try {
        $response = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 30
        "$url -> $($response.StatusCode)"
    }
    catch {
        "$url -> not answering yet, check again in a minute"
    }
}
