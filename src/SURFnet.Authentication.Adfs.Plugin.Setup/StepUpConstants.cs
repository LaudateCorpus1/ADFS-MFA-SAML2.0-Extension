﻿/*
* Copyright 2017 SURFnet bv, The Netherlands
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

namespace SURFnet.Authentication.Adfs.Plugin.Setup
{
    /// <summary>
    /// Class Constants.
    /// </summary>
    public static class StepUpConstants
    {
        public static class InternalNames
        {
            public const string SecondFactorEndpoint = "SecondFactorEndpoint";

            public const string EntityId = "entityId";

            public const string SigningCertificateThumbprint = "findValue";

            public const string SecondCertificate = "Certificate";
        }

        /// <summary>
        /// Class FriendlyNames. These values Must match with the property names in the JSON file
        /// </summary>
        public static class FriendlyNames
        {
            public const string SecondFactorEndpoint = "StepupGatewaySSOLocation";

            public const string EntityId = "StepupGatewayEntityID";

            public const string SigningCertificateThumbprint = "StepupGatewaySigningCertificate";

            public const string SecondCertificate = "StepupGatewaySigningCertificate2";
        }
    }
}