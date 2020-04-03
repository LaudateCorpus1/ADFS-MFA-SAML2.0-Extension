﻿using SURFnet.Authentication.Adfs.Plugin.Setup.Models;
using SURFnet.Authentication.Adfs.Plugin.Setup.Services;
using SURFnet.Authentication.Adfs.Plugin.Setup.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SURFnet.Authentication.Adfs.Plugin.Setup.Versions
{
    public class Sustainsys_2_3_Component : StepupComponent
    {
        public Sustainsys_2_3_Component() : base("Sustainsys.Saml2 v2.3")
        {
            ConfigFilename = SetupConstants.SustainCfgFilename;
        }


        private static readonly string[] ConfigParameters =
        {
            ConfigSettings.SPEntityId,
            ConfigSettings.IdPEntityId,
            ConfigSettings.IdPSigningCertificate   
        };

        public override List<Setting> ReadConfiguration()
        {
            LogService.Log.Info($"Reading Settings from {ConfigFilename} for {ComponentName}.");

            var settings = ExctractSustainsyConfig();
            if ( settings == null )
            {
                LogService.WriteFatal($"  Reading settings from {ConfigFilename} for '{ComponentName}' failed.");
            }

            return settings;
        }

        public override int WriteConfiguration(List<Setting> allsettings)
        {
            int rc = 0;

            LogService.Log.Info($"  Writing settings of {ComponentName} configuration to {ConfigFilename}");

            if ( false == ConfigurationFileService.ReplaceInXmlCfgFile(ConfigFilename, ConfigParameters, allsettings) )
            {
                LogService.WriteFatal($"Content problem(s) in {ConfigFilename} for component: {ComponentName}");
                rc = -1;
            }

            return rc;
        }


        // private code
        private List<Setting> ExctractSustainsyConfig()
        {
            List<Setting> settings = new List<Setting>();

            string sustainsysCfgPath = FileService.OurDirCombine(FileDirectory.AdfsDir, SetupConstants.SustainCfgFilename);
            var sustainsysConfig = XDocument.Load(sustainsysCfgPath);

            //var nameAttribute = XName.Get("name");

            var sustainsysSection = sustainsysConfig.Descendants(XName.Get(SetupConstants.XmlElementName.SustainsysSaml2Section)).FirstOrDefault();

            ConfigSettings.SPEntityID.FoundCfgValue = sustainsysSection?.Attribute(XName.Get(SetupConstants.XmlAttribName.EntityId))?.Value;
            settings.Add(ConfigSettings.SPEntityID);

            var identityProvider = sustainsysSection?.Descendants(XName.Get("add")).FirstOrDefault();
            var certificate = identityProvider?.Descendants(XName.Get(SetupConstants.XmlElementName.SustainIdPSigningCert)).FirstOrDefault();
            ConfigSettings.IdPSigningThumbPrint_1_Setting.FoundCfgValue = certificate?.Attribute(XName.Get(SetupConstants.XmlAttribName.CertFindValue))?.Value;
            settings.Add(ConfigSettings.IdPSigningThumbPrint_1_Setting);

            ConfigSettings.IdPEntityID.FoundCfgValue = identityProvider?.Attribute(XName.Get(SetupConstants.XmlAttribName.EntityId))?.Value;
            settings.Add(ConfigSettings.IdPEntityID);

            return settings;
        }

    }
}
