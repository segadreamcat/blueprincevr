param([string]$Job,[switch]$CheckOnly,[string]$InputWave)
$ErrorActionPreference='Stop'
$runtime=Join-Path $PSScriptRoot 'Whisper'
$cli=Join-Path $runtime 'whisper-cli.exe'
$model=Join-Path $runtime 'ggml-small.en.bin'
try {
 if(!(Test-Path -LiteralPath $cli) -or !(Test-Path -LiteralPath $model)){throw 'Whisper runtime/model missing. Install the complete 2.8.0 recorder update.'}
 Add-Type -Path (Join-Path $PSScriptRoot 'MemoCapture.cs')
 if($CheckOnly){ & $cli --help; if($LASTEXITCODE -ne 0){throw 'Whisper executable failed'}; Write-Output 'PASS: capture helper compiled and Whisper CLI loaded. Microphone not opened.'; exit 0 }
 if([string]::IsNullOrWhiteSpace($Job)){throw 'Missing recording job path.'}
 if($InputWave){Copy-Item -LiteralPath $InputWave -Destination ($Job+'.wav')}else{[MemoCapture]::Record($Job)}
 [IO.File]::WriteAllText($Job+'.processing','transcribing')
 $ErrorActionPreference='Continue'
 & $cli -m $model -f ($Job+'.wav') -l en -t 4 -ng -otxt -of ($Job+'.whisper') -nt 2> ($Job+'.engine.log') | Out-Null
 $code=$LASTEXITCODE
 $ErrorActionPreference='Stop'
 if($code -ne 0 -or !(Test-Path -LiteralPath ($Job+'.whisper.txt'))){throw 'Whisper could not transcribe this recording.'}
 $result=[IO.File]::ReadAllText($Job+'.whisper.txt').Trim()
 [IO.File]::WriteAllText($Job+'.text',$result)
}catch{
 if($Job){[IO.File]::WriteAllText($Job+'.error',$_.Exception.GetBaseException().Message)}
 if($CheckOnly){Write-Error $_}
 exit 1
}finally{
 if($Job){foreach($suffix in @('.wav','.whisper.txt','.engine.log')){if(Test-Path -LiteralPath ($Job+$suffix)){Remove-Item -LiteralPath ($Job+$suffix) -Force}}}
}
