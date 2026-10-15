# Read installed SAPI and OneCore voices without changing the Windows registry.
function Get-DingousVoices {
 $catalog=@()
 foreach($registryPath in @('HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech\Voices','HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices')){
  try {
   $category=New-Object -ComObject SAPI.SpObjectTokenCategory
   $category.SetId($registryPath,$false)
   foreach($token in $category.EnumerateTokens()){
    $catalog += [pscustomobject]@{Name=$token.GetAttribute('Name');Gender=$token.GetAttribute('Gender');Language=$token.GetAttribute('Language');Token=$token}
   }
  }catch {Write-Verbose "Voice category unavailable: $registryPath"}
 }
 return $catalog | Sort-Object Name -Unique
}
