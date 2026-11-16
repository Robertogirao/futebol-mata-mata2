param([string]$Projeto)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$pacote = $PSScriptRoot
$origem = Join-Path $pacote 'Assets'
if ([string]::IsNullOrWhiteSpace($Projeto)) { $Projeto = Read-Host 'Cole o caminho da pasta do projeto NBA (a que contem Assets, Packages e ProjectSettings)' }
$Projeto = [IO.Path]::GetFullPath($Projeto.Trim().Trim('"'))
foreach ($pasta in @('Assets', 'Packages', 'ProjectSettings')) {
    if (!(Test-Path -LiteralPath (Join-Path $Projeto $pasta) -PathType Container)) { throw "Pasta de projeto invalida: falta $pasta." }
}
if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw 'Feche o editor Unity antes de instalar.' }
$assetsDestino = Join-Path $Projeto 'Assets'
if ($pacote.StartsWith($assetsDestino + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or $pacote -eq $assetsDestino) {
    throw 'Extraia o ZIP fora de Assets e execute o instalador novamente.'
}
$arquivos = @(Get-ChildItem -LiteralPath $origem -File -Recurse)
if ($arquivos.Count -lt 100) { throw 'Pacote incompleto. Extraia todo o ZIP antes de instalar.' }
$manifesto = Join-Path $pacote 'SHA256SUMS.txt'
foreach ($linha in (Get-Content -LiteralPath $manifesto)) {
    if ($linha -match '^([a-f0-9]{64})  (.+)$') {
        $esperado = $Matches[1]; $rel = $Matches[2]
        $caminho = Join-Path $pacote $rel
        if (!(Test-Path -LiteralPath $caminho -PathType Leaf) -or (Get-FileHash -LiteralPath $caminho -Algorithm SHA256).Hash -ne $esperado) {
            throw "Arquivo ausente ou alterado no pacote: $rel"
        }
    }
}
$backup = Join-Path $Projeto ('Backup_Basquete_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '_' + [Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $backup | Out-Null
$substituidos = New-Object 'System.Collections.Generic.List[string]'
$adicionados = New-Object 'System.Collections.Generic.List[string]'
$legados = New-Object 'System.Collections.Generic.List[string]'
try {
    # Preserve every file that will be overwritten before changing the project.
    foreach ($arquivo in $arquivos) {
        $rel = $arquivo.FullName.Substring($origem.Length).TrimStart([IO.Path]::DirectorySeparatorChar)
        $destino = Join-Path $assetsDestino $rel
        if (Test-Path -LiteralPath $destino -PathType Leaf) {
            $salvo = Join-Path (Join-Path $backup 'Assets') $rel
            New-Item -ItemType Directory -Path (Split-Path $salvo) -Force | Out-Null
            Copy-Item -LiteralPath $destino -Destination $salvo
            $substituidos.Add($rel)
        } else { $adicionados.Add($rel) }
    }
    foreach ($nome in @('BasqueteDiscos', 'BasqueteDiscos.meta')) {
        $antigo = Join-Path $assetsDestino $nome
        if (Test-Path -LiteralPath $antigo) {
            Move-Item -LiteralPath $antigo -Destination (Join-Path $backup $nome)
            $legados.Add($nome)
        }
    }
    foreach ($arquivo in $arquivos) {
        $rel = $arquivo.FullName.Substring($origem.Length).TrimStart([IO.Path]::DirectorySeparatorChar)
        $destino = Join-Path $assetsDestino $rel
        New-Item -ItemType Directory -Path (Split-Path $destino) -Force | Out-Null
        Copy-Item -LiteralPath $arquivo.FullName -Destination $destino -Force
    }
    $substituidos | Set-Content -LiteralPath (Join-Path $backup 'arquivos_substituidos.txt')
    $adicionados | Set-Content -LiteralPath (Join-Path $backup 'arquivos_adicionados.txt')
    Write-Host "Instalacao concluida. Backup preservado em: $backup"
    Write-Host 'Abra o Unity. Use Tools > Basquete > Abrir Mata-mata NBA (ou Abrir Liga NBA).'
    Write-Host 'A cena antiga MataMata_Discos nao e a nova cena de basquete.'
} catch {
    $falha = $_
    foreach ($rel in $substituidos) {
        $salvo = Join-Path (Join-Path $backup 'Assets') $rel
        if (Test-Path -LiteralPath $salvo) { Copy-Item -LiteralPath $salvo -Destination (Join-Path $assetsDestino $rel) -Force }
    }
    foreach ($rel in $adicionados) {
        $novo = Join-Path $assetsDestino $rel
        if (Test-Path -LiteralPath $novo -PathType Leaf) { Remove-Item -LiteralPath $novo }
    }
    foreach ($nome in $legados) { Move-Item -LiteralPath (Join-Path $backup $nome) -Destination (Join-Path $assetsDestino $nome) }
    throw $falha
}
