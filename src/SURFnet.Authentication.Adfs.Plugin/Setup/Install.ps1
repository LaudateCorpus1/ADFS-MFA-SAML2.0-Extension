﻿###########################################################################
# Copyright 2017 SURFnet bv, The Netherlands
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
###########################################################################

# Global settings
$ErrorActionPreference = "Stop"
$configurationFile = ".\SURFnet.Authentication.ADFS.MFA.Plugin.Environments.json"

# Global initialization
. .\Functions.ps1
. .\Configuration.ps1
Import-Module .\PowerShellModule\SURFnetMFA
Clear-Host
$error.Clear()
$date = Get-Date -f "yyyyMMdd.HHmmss"
$installDir = $PSScriptRoot
$configDir = "$PSScriptroot\Config"
$certificateDir = "$PSScriptroot\Certificates"
Start-Transcript "Log/Install-SurfnetMfaPlugin.$date.log"

function GetUserSettings() {
	# Read the configuration file and iterate the options
	$environments = GetEnvironments $configurationFile

	# Ask the user which environment should be used
	$environment = SelectEnvironment $environments

	# Set the defaults
	$Default_ActiveDirectoryUserIdAttribute = "employeeNumber"
	
	# Ask for remaining installation parameters
	Write-WarningMessage "1. Enter the value of the schacHomeOrganization attribute of your institution (the one that your institution provides to SURFconext)."
	Write-WarningMessage " Must be the same value that is used in the urn:mace:terena.org:attribute-def:schacHomeOrganization attribute your institution sends to SURFconext."
	$schacHomeOrganization = AskRequiredQuestion "SchacHomeOrganization"
	
	# TODO: Remove this question when it has been removed from the rest of the source code
	Write-WarningMessage "2. Enter the name of the Active Directory (AD) that contains the useraccounts used by the ADFS MFA extension. E.g. 'example.org'."
	$activeDirectoryName = AskRequiredQuestion "ActiveDirectoryName [OBSOLETE]"
	
	Write-WarningMessage "3. Enter the name of the attribute in AD containing the userid known by SURFsecureID."
	Write-WarningMessage " The result must be same value that was used in the urn:mace:dir:attribute-def:uid attribute during authentication to SURFconext."
	$activeDirectoryUserIdAttribute = Read-Host "ActiveDirectoryUserIdAttribute? (default is $($Default_ActiveDirectoryUserIdAttribute))"
	if ($activeDirectoryUserIdAttribute.Length -eq 0) { $activeDirectoryUserIdAttribute = $Default_ActiveDirectoryUserIdAttribute }
	
	# TODO: Set the default to https://hostname/stepup-mfa
	Write-WarningMessage "4. Enter the EntityID of your Service Provider (SP)." 
	Write-WarningMessage " This is the entityid used by the ADFS MFA extenstion to communicatie with SURFsecureID. Choose a value in the form of an URL or URN."
	$serviceProviderEntityId = AskRequiredQuestion "Service provider EntityId"
	
	Write-WarningMessage "5. Optionally, select (if present) a .pfx file containing the X.509 certificate and RSA private key which will be used to sign the authentication request to the SFO Endpoint" 
	Write-WarningMessage " When using an existing X.509 certificate, please register the certificate with SURFsecureID." 
	Write-WarningMessage " When not present, an X.509 certificate and private key will be generated by the install script, and written as a .pfx file to the installation folder. Please register this certificate with SURFsecureID." 
	Write-WarningMessage " Caution: In case of a multi server farm, use the same signing certificate"
	$serviceProviderSigningCertificate = BrowseForFile "Do you want to select a service provider SigningCertificate?" $installDir 'PFX Certificates (*.pfx)|*.pfx'
	
	Write-Host ""
	Write-Host ""
	Write-Host ""
	Write-Host ""
	
	# Show the user the signing certificate will be auto-generated
	$signingCertificate = $serviceProviderSigningCertificate
	if ($signingCertificate.Length -eq 0) {
		$signingCertificate = "auto-generate"
	}

	Write-GoodMessage "===================================== Installation Configuration Summary =========================================="
	Write-WarningMessage "Installation settings"
	Write-GoodMessage  "Location of SFO endpoint from SURFsecureID Gateway        : $($environment.SecondFactorEndpoint)"
	Write-GoodMessage  "Minimum LoA for authentication requests                   : $($environment.MinimalLoa)"
	Write-GoodMessage  "schacHomeOrganization attribute of your institution       : $schacHomeOrganization"
	Write-GoodMessage  "AD containing the useraccounts                            : $activeDirectoryName"
	Write-GoodMessage  "AD userid attribute                                       : $activeDirectoryUserIdAttribute"
	Write-GoodMessage  "SAML EntityID of the ADFS MFA extension                   : $serviceProviderEntityId"
	Write-GoodMessage  ".pfx file with the extension's X.509 cert and RSA keypair : $signingCertificate"
	Write-GoodMessage  "SAML EntityID of the SURFsecureID Gateway                 : $($environment.EntityId)"
	Write-GoodMessage  ".crt file with X.509 cert of the SURFsecureID Gateway     : $($environment.Certificate)"
	Write-GoodMessage "==================================================================================================================="
	
	Write-Host ""
	Write-Host ""

	if ((Read-Host "Continue the installation with these settings? Y/N") -ne "Y") {
		return $false
	}

	return @{
		MinimalLoa                         = $environment.MinimalLoa;
		SecondFactorEndpoint               = $environment.SecondFactorEndpoint;
		IdentityProvider_EntityId          = $environment.EntityId;
		IdentityProvider_Certificate       = $environment.Certificate;
		SchacHomeOrganization              = $schacHomeOrganization;
		ActiveDirectoryName                = $activeDirectoryName;
		ActiveDirectoryUserIdAttribute     = $activeDirectoryUserIdAttribute;
		ServiceProvider_EntityId           = $serviceProviderEntityId;
		ServiceProvider_SigningCertificate = $serviceProviderSigningCertificate;
		AutoGenerateSigningCertificate     = $serviceProviderSigningCertificate.Length -eq 0;
	}
}

function CheckIfRunningAsAdministrator {
	if (([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator") -ne $true) {
		throw "Cannot run script. Please run this script in administrator mode."
	}
}

function GetAdfsServiceAccountName {
	# Get the ADFS service
	$adfssrv = Get-WmiObject win32_service | Where-Object { $_.name -eq "adfssrv" }

	# Check if it is present
	if (!$adfssrv) {
		throw "No AD FS service found on this server. Please run this script locally at the target AD FS server."
	}

	# Return the start name of the adfs service
	return $adfssrv.StartName
}

try {
	CheckIfRunningAsAdministrator
	$adfsServiceAccountName = GetAdfsServiceAccountName
	if ($settings = GetUserSettings) {
		Copy-Log4NetConfiguration -ConfigDir $configDir

		if ($settings.AutoGenerateSigningCertificate) {
			$signingCertificate = New-SigningCertificate
		}
		else {
			$signingCertificate = Import-SigningCertificate `
				-CertificateFile $settings.ServiceProvider_SigningCertificate `
				-CertificateDir $certificateDir
		}

		Install-SigningCertificate `
			-AccountName $adfsServiceAccountName `
			-Certificate $signingCertificate

		$sfoCertificateThumbprint = Import-SfoCertificate `
			-CertificateDir $certificateDir `
			-CertificateFile $settings.IdentityProvider_Certificate

		Add-EventLogForMfaPlugin

		Install-AuthProvider `
			-InstallDir $installDir `
			-ProviderName ADFS.SCSA `
			-AssemblyName "SURFnet.Authentication.Adfs.Plugin.dll" `
			-TypeName "SURFnet.Authentication.Adfs.Plugin.Adapter"

		Update-ADFSConfiguration `
			-ConfigDir $configDir `
			-ServiceProviderEntityId $settings.ServiceProvider_EntityId `
			-IdentityProviderEntityId $settings.IdentityProvider_EntityId `
			-SecondFactorEndpoint $settings.SecondFactorEndpoint `
			-MinimalLoa $settings.MinimalLoa `
			-schacHomeOrganization $settings.SchacHomeOrganization `
			-ActiveDirectoryName $settings.ActiveDirectoryName `
			-ActiveDirectoryUserIdAttribute $settings.ActiveDirectoryUserIdAttribute `
			-sfoCertificateThumbprint $sfoCertificateThumbprint `
			-ServiceProviderCertificateThumbprint $signingCertificate.Thumbprint            

		if ($settings.AutoGenerateSigningCertificate) {
			$exportCertificateTo = "$certificateDir\" + $signingCertificate.DnsNameList[0].Unicode + ".pfx"
			$pwd = Export-SigningCertificate `
				-CertificateThumbprint $signingCertificate.Thumbprint `
				-ExportTo $exportCertificateTo

			Write-Host ""
			Write-Host ""
			Write-Host ""

			Write-SigningCertificate `
				-Certificate $signingCertificate `
				-EntityId $settings.ServiceProvider_EntityId `
				-Password $pwd
		}
	}
}
catch {
	Write-ErrorMessage $_.Exception.Message
}

Stop-Transcript
