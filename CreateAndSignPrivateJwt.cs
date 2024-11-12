using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using System.Web.UI;
using Grpc.Core;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using iSuite.Lib.Logger;
using System.Configuration;

namespace mBank_FAB
{
    public class CreateAndSignPrivateJwt
    {
        public string GetJWTToken()
        {
            var signedToken = "";
            try
            {
                String strURLalias = ConfigurationManager.AppSettings["Token_alias"].ToString();
                String strURLpassword = ConfigurationManager.AppSettings["Token_Password"].ToString();
                String strURLkeyStorePath = ConfigurationManager.AppSettings["Token_pfs_path"].ToString();
                string alias = strURLalias; //unique name for key
                string password = strURLpassword; // pfx password
                string keyStorePath = strURLkeyStorePath;  //HttpContext.Current.Server.MapPath("~/App_Data/mbankisuituat_new.pfx"); //pfx file

                // Load the certificate from the specified path
                X509Certificate2 cert = new X509Certificate2(keyStorePath, password, X509KeyStorageFlags.MachineKeySet);

                // Get the RSA private key from the certificate
                RSA privateKey = cert.GetRSAPrivateKey();

                // Initialize the token handler
                var tokenHandler = new JwtSecurityTokenHandler();

                // Create an RSA security key
                var key = new RsaSecurityKey(privateKey);

                // Set up signing credentials
                var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
                {
                    CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
                };

                // Define the token descriptor with claims, audience, issuer, etc.
                //var tokenDescriptor = new SecurityTokenDescriptor
                //{
                //    Subject = new ClaimsIdentity(new[]
                //    {
                        
                //          new Claim(JwtRegisteredClaimNames.Sub, "77161e02-4498-4ee3-961e-0244984ee333"),
                //             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                //         }),
                    

                //    Audience = "https://idp-uat.bankfab.com/eb2b/oauth/token",
                //    Issuer = "77161e02-4498-4ee3-961e-0244984ee333",
                //    IssuedAt = DateTime.UtcNow,
                //    Expires = DateTime.UtcNow.AddSeconds(30),
                //    SigningCredentials = signingCredentials
                //};
                var payload = new JwtPayload
                {
                    //{ "sub", "77161e02-4498-4ee3-961e-0244984ee333" },// UAT
                    { "sub", "8cc4135c-f25f-47d3-8413-5cf25f17d309" },
                //   { "aud", "https://idp-uat.bankfab.com/eb2b/oauth/token" }, // UAT
                    { "aud", "https://auth.bankfab.com/eb2b/oauth/token" },                  
                     { "iss", "https://api-proxy.mbankuae.local" },
                  //  { "iss", "https://isuite.mbank.ae:8444/" },
                   { "iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                   { "exp", DateTimeOffset.UtcNow.AddSeconds(30).ToUnixTimeSeconds() },
                   { "jti", Guid.NewGuid().ToString() }
                };
                var header = new JwtHeader(signingCredentials)
                    {
                        { "kid", alias }  // Set the Key ID in the header
                    };

                var token = new JwtSecurityToken(header, payload);

                //var token = tokenHandler.CreateToken(tokenDescriptor);
                signedToken = tokenHandler.WriteToken(token); //Client Assertion Token
                
                // Prints the token ready to be sent to the Authentication Service!
            }
            catch (Exception ex)
            {
                signedToken= "Error creating JWT: " + ex.Message;
                Log.Logger("", "", Log._App_Mbank_FAB, "CreateAndSignPrivateJwt.cs", "GetJWTToken", Log._Exception, signedToken);
                
            }
            Log.Logger("", "", Log._App_Mbank_FAB, "CreateAndSignPrivateJwt.cs", "GetJWTToken Created :", "", signedToken);
            return signedToken;
        
        }

        public async Task<string> GetAccessToken()
        {
            APIConfig config = new APIConfig();
            string uri = config.accessTokenUrl;
            var responseFromServer = "";
            try
            {
                string jwtToken = GetJWTToken();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback +=
                (sender, certificate, chain, sslPolicyErrors) => true;

                var handler = new HttpClientHandler();
                handler.SslProtocols = SslProtocols.Tls12;

                var client = new System.Net.Http.HttpClient(handler);
                client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                var content = new FormUrlEncodedContent(new[]
    {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
        //    new KeyValuePair<string, string>("client_id", "77161e02-4498-4ee3-961e-0244984ee333"),// UAT
            new KeyValuePair<string, string>("client_id", "8cc4135c-f25f-47d3-8413-5cf25f17d309"),
            new KeyValuePair<string, string>("client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
            new KeyValuePair<string, string>("client_assertion", jwtToken)
            });

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
                Log.Logger("", "", "Before Request", "", "GetAccessToken","", "uri: " + uri);
                var responseMsg = await client.PostAsync(uri, content);
                Log.Logger("", "", "Before Request", "", "GetAccessToken", "", "response msg: " + responseMsg);
                responseFromServer = responseMsg.Content.ReadAsStringAsync().Result;
                Log.Logger("", "", "Before Request", "", "GetAccessToken", "", "responseFromServer msg: " + responseFromServer);
            }
            catch(Exception ex)
            {
                Log.Logger("", "", Log._App_Mbank_FAB, "CreateAndSignPrivateJwt.cs", "GetAccessToken", Log._Exception, "Error creating access token - "+ex.Message+" "+ex.InnerException);
            }
            return responseFromServer;
        }


    }
    
}

