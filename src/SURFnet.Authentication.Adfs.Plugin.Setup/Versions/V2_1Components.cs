﻿using SURFnet.Authentication.Adfs.Plugin.Setup.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SURFnet.Authentication.Adfs.Plugin.Setup.Versions
{
    public static class V2_1Components
    {
        public static readonly AdapterComponent V2_1_17Adapter = new V2_1_17AdapterImp();

        public static readonly AdapterComponent V2_1_18Adapter = new V2_1_18AdapterImp();

        public static readonly StepupComponent[] V2_1_17Components = new StepupComponent[]
        {
            new Sustainsys2_3_Component()
            {
                Assemblies = ComponentAssemblies.Sustain2_3AssemblySpec,
                ConfigFilename = SetupConstants.SustainCfgFilename
            },

            new Log4netV2_0_8Component("log4net V2.0.8.0"),

            new StepupComponent("Newtonsoft v12.0.3")
            {
                Assemblies = ComponentAssemblies.Newtonsoft12_0_3AssemblySpec,
                ConfigFilename = null
            }
        };

        public static readonly StepupComponent[] V2_1_18Components = new StepupComponent[]
        {
            new Sustainsys2_3MdComponent()
            {
                Assemblies = ComponentAssemblies.Sustain2_3AssemblySpec,
                ConfigFilename = SetupConstants.SustainCfgFilename
            },

            new Log4netV2_0_8Component("log4net V2.0.8.0"),

            new StepupComponent("Newtonsoft v12.0.3")
            {
                Assemblies = ComponentAssemblies.Newtonsoft12_0_3AssemblySpec,
                ConfigFilename = null
            }
        };
    }
}
