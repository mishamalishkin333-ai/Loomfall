# Publish "Loomfall" (Tkach Yarusov) to GitHub Pages in one run.
# ASCII only on purpose: Windows PowerShell 5.1 misreads UTF-8 without BOM.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File publish.ps1 -DryRun
#   powershell -NoProfile -ExecutionPolicy Bypass -File publish.ps1 -Repo loomfall
#
# -NoProfile matters: a broken PowerShell profile prints errors and returns a
# non-zero exit code of its own, which looks like a failure of this script.
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
"branch:  $((& $git rev-parse --abbrev-ref HEAD))"
"tracked files: $((& $git ls-files | Measure-Object -Line).Lines)"

# Placeholder identity means the commits will not be credited to your account.
$email = (& $git config user.email)
"commit author: $email"
if ($email -like '*example.com') {
    Write-Host 'Note: this is a placeholder address. To credit the commits to you:' -ForegroundColor Yellow
    Write-Host '  git config user.name "Your Name"; git config user.email "you@example.org"' -ForegroundColor Yellow
    Write-Host '  git commit --amend --reset-author --no-edit   (or leave it as is)' -ForegroundColor Yellow
}

$account = if ($Owner) { $Owner } else { 'YOUR-NAME' }

if ($DryRun) {
    Step 'Dry run: nothing is published'
    "Would create: repository $account/$Repo (branch main), Pages from /docs"
    "Game URL:    https://$account.github.io/$Repo/"
    "Privacy URL: https://$account.github.io/$Repo/privacy.html"
    exit 0
}

if (-not $gh -or -not $Owner) {
    Write-Host 'Cannot publish without gh and a signed-in account.' -ForegroundColor Yellow
    Step 'Manual route (no gh needed)'
    "1. Create an empty repository $account/$Repo on github.com (no README, no .gitignore)."
    '2. Then run:'
    '   cd <this folder>'
    "   git remote add origin https://github.com/$account/$Repo.git"
    '   git push -u origin main'
    '3. Settings -> Pages -> Source: Deploy from a branch; Branch: main; Folder: /docs; Save.'
    "4. After a minute the game is at https://$account.github.io/$Repo/"
    exit 1
}

Step 'Creating the repository and pushing the code'
& $gh repo create "$Owner/$Repo" --public --source . --push --description 'Loomfall - Unity WebGL game'

Step 'Enabling GitHub Pages (branch main, folder /docs)'
try {
    & $gh api --method POST "repos/$Owner/$Repo/pages" -f 'source[branch]=main' -f 'source[path]=/docs'
}
catch {
    Write-Host 'Pages was not enabled by this call - it is probably already on. Checking:' -ForegroundColor Yellow
    & $gh api "repos/$Owner/$Repo/pages" --jq '.html_url'
}

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
