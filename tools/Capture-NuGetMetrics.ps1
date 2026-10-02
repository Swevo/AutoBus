param(
    [string]$OutputPath = "docs\METRICS-SNAPSHOT.tsv"
)

$packages = @(
    @{ Id = "Swevo.AutoBus"; Version = "1.2.0"; Baseline = 616 },
    @{ Id = "Swevo.AutoBus.RabbitMQ"; Version = "1.0.2"; Baseline = 488 },
    @{ Id = "Swevo.AutoBus.Analyzers"; Version = "1.1.0"; Baseline = 0 }
)

$rows = foreach ($package in $packages) {
    $uri = "https://azuresearch-usnc.nuget.org/query?q=packageid:$($package.Id)&prerelease=true&take=1"
    $response = Invoke-RestMethod -Uri $uri
    $entry = $response.data | Where-Object { $_.id -eq $package.Id } | Select-Object -First 1
    if ($null -eq $entry) {
        $entry = $response.data[0]
    }

    $currentDownloads = [int]$entry.totalDownloads
    $delta = $currentDownloads - [int]$package.Baseline

    [PSCustomObject]@{
        Date = (Get-Date).ToString("yyyy-MM-dd")
        Package = $package.Id
        Version = $package.Version
        BaselineDownloads = $package.Baseline
        CurrentDownloads = $currentDownloads
        Delta = $delta
    }
}

$rows | Sort-Object Package | Export-Csv -Path $OutputPath -Delimiter "`t" -NoTypeInformation
Write-Output "Wrote metrics snapshot to $OutputPath"
