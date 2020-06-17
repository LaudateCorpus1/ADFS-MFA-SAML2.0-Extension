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

namespace SURFnet.Authentication.Adfs.Plugin.Services
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Security.Claims;

    using Configuration;

    using log4net;

    using Microsoft.IdentityModel.Tokens.Saml2;

    using Models;

    using SURFnet.Authentication.Adfs.Plugin.Setup.Common.Exceptions;

    using Sustainsys.Saml2;
    using Sustainsys.Saml2.Configuration;
    using Sustainsys.Saml2.Saml2P;

    /// <summary>
    /// Creates the SAML assertions and processes the response.
    /// </summary>
    public class SamlService
    {
        /// <summary>
        /// Used for logging.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger("SAML Service");

        /// <summary>
        /// Creates the SAML authentication request with the correct name identifier.
        /// </summary>
        /// <param name="userid">The stepup userid as found in the active Directory.</param>
        /// <param name="authnRequestId">The AuthnRequest identifier.</param>
        /// <param name="ascUri">The asc URI.</param>
        /// <returns>
        /// The authentication request.
        /// </returns>
        public static Saml2AuthenticationSecondFactorRequest CreateAuthnRequest(string userid, string authnRequestId, Uri ascUri)
        {
            var stepupNameId = GetNameId(userid);  // TODO: This code should move up to the adapter, should not be in SAML code

            var nameIdentifier = new Saml2NameIdentifier(stepupNameId, new Uri("urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified"));
            Log.DebugFormat("Creating AuthnRequest for NameID '{0}'", stepupNameId);

            // This was testes in constructors!!!
            var samlConfiguration = Options.FromConfiguration;
            if (samlConfiguration == null)
            {
                throw new InvalidConfigurationException("ERROR_0002", "The SAML configuration could not be loaded");
            }

            // Should have been tested in constructors!
            var spConfiguration = samlConfiguration.SPOptions;
            if (spConfiguration == null)
            {
                throw new InvalidConfigurationException("ERROR_0002", "The service provider section of the SAML configuration could not be loaded");
            }

            var authnRequest = new Saml2AuthenticationSecondFactorRequest
            {
                DestinationUrl = Options.FromConfiguration.IdentityProviders.Default.SingleSignOnServiceUrl,
                AssertionConsumerServiceUrl = ascUri,
                Issuer = spConfiguration.EntityId,
                RequestedAuthnContext = new Saml2RequestedAuthnContext(StepUpConfig.Current.MinimalLoa, AuthnContextComparisonType.Minimum),
                Subject = new Saml2Subject(nameIdentifier),
            };
            authnRequest.SetId(authnRequestId);

            Log.DebugFormat("Created AuthnRequest for '{0}' with id '{1}'", userid, authnRequest.Id.Value);
            return authnRequest;
        }

        public static ClaimsIdentity VerifyResponseAndGetClaimsIdentity(Saml2Response samlResponse)
        {
            ClaimsIdentity cid = null;

            // The response is verified when the identities are retrieved.
            var responseIdentities = samlResponse.GetClaims(Options.FromConfiguration).ToList();

            if ( responseIdentities != null && responseIdentities.Count>0)
            {
                cid = responseIdentities[0];
                if ( responseIdentities.Count > 1 )
                {
                    // weird, but not fatal.
                    Log.WarnFormat("Using only the first ClaimsIdentity out of: {0}", responseIdentities.Count);
                }
            }
            else
            {
                Log.Debug("Saml2Response.GetClaims returned null or no ClaimsIdentities");
            }

            return cid;
        }

        /// <summary>
        /// Verifies the response and gets the first claim of the requested type from the response.
        /// </summary>
        /// <param name="samlResponse">The SAML response.</param>
        /// <param name="claimType">The type of claim to look for.</param>
        /// <returns>
        /// The authentication claim.
        /// </returns>
        public static Claim[] VerifyResponseAndGetAuthenticationClaim(Saml2Response samlResponse, string claimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationmethod")
        {
            // The response is verified when the claims are retrieved.
            var responseClaims = samlResponse.GetClaims(Options.FromConfiguration).ToList();

            // Get the first response claim where the type is right
            var authClaim = responseClaims
                .Select(claimsIdentity => claimsIdentity.Claims.FirstOrDefault(c =>
                    c.Type.Equals(claimType)))
                .FirstOrDefault(a => a != null);

            return authClaim == null ? Array.Empty<Claim>() : new[] { authClaim };
        }

        /// <summary>
        /// Gets the SURFConext identity provider from the configuration.
        /// </summary>
        /// <param name="serviceProviderConfiguration">The service provider configuration.</param>
        /// <returns>The SURFConext identity provider.</returns>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
        public static IdentityProvider GetIdentityProvider(Options serviceProviderConfiguration)
        {
            var providers = serviceProviderConfiguration.IdentityProviders.KnownIdentityProviders.ToList();
            if (providers.Count == 0)
            {
                throw new InvalidConfigurationException("ERROR_0002", "No identity providers found. Add the SURFConext identity provider before using Second Factor Authentication");
            }

            if (providers.Count > 1)
            {
                throw new InvalidConfigurationException("ERROR_0002", "Too many identity providers found. Add only the SURFConext identity provider");
            }

            return providers[0];
        }

        /// <summary>
        /// Gets the name identifier based in the identity claim.
        /// </summary>
        /// <param name="userid">The userid.</param>
        /// <returns>A name identifier.</returns>
        private static string GetNameId(string userid)
        {
            var nameid = $"urn:collab:person:{StepUpConfig.Current.SchacHomeOrganization}:{userid}";

            nameid = nameid.Replace('@', '_');
            return nameid;
        }
    }
}
