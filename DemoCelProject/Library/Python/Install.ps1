# Get the directory of the currently running script
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Check if the venv folder exists in the current directory
$venvPath = Join-Path $scriptDirectory "venv"
$requirementsFile = Join-Path $scriptDirectory "requirements.txt"

Remove-Item -Path $venvPath -Recurse

if (Test-Path $venvPath)
{
    Write-Host "Failed to delete existing virtual environment"
    exit 1
}

# Upgrade pip to latest version
python -m pip install --upgrade pip

# Create a new virtual environment if it doesn't exist
python -m venv $venvPath

# Activate the virtual environment
$activateScript = Join-Path $venvPath "Scripts\Activate.ps1"
if (Test-Path $activateScript)
{
    Write-Host "Activating the virtual environment..."
    & $activateScript
}
else
{
    Write-Host "Activate script not found. Please run Install.ps1 to recreate the virtual environment."
    exit 1
}

# Check if the requirements.txt file exists and install packages if present
if (Test-Path $requirementsFile)
{
    Write-Host "Installing packages from requirements.txt"
    python -m pip install -r $requirementsFile
}
else
{
    Write-Host "No requirements.txt file found. You can create one to specify the required packages."
}

# Check if the virtual environment is activated
$env:VIRTUAL_ENV

deactivate

Write-Host "Installation completed successfully"

