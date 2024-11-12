using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using iSuite.Lib.Logger;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using iSuite.Lib.DBCon;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Authentication;
using System.Net.Http.Headers;
using mBank_FAB;
using System.Net.Mime;
/// <summary>
/// Summary description for HttpClient
/// </summary>
public class HttpClient
{
    public String PostJson(string uri, String postData)
    {
        string responseFromServer = "";
        try
        {
            //Log.MessageLog("JSON REQUEST MESSAGE ::-" + Log.GetMaskedMessage(postData));

            // string postData = JsonConvert.SerializeObject(InputData);
            byte[] bytes = Encoding.UTF8.GetBytes(postData);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentLength = bytes.Length;
            httpWebRequest.ContentType = "application/json";
            //httpWebRequest.Accept = "application/json";
            //httpWebRequest.SendChunked = true;
            // httpWebRequest.TransferEncoding = "gzip,deflate";

            //httpWebRequest.UserAgent = "Apache-HttpClient/4.1.1 (java 1.5)";

            //Adding Authentication
            String AuthUserName = ConfigurationManager.AppSettings["MiddlewareAuthUserName"].ToString();
            String AuthPassword = ConfigurationManager.AppSettings["MiddlewareAuthPasword"].ToString();
            //AuthPassword = objg.decrypt(AuthPassword);
            String AuthString = AuthUserName + ":" + AuthPassword;
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(AuthString));
            httpWebRequest.Headers.Add("Authorization", "Basic " + encoded);

            if (uri.Contains("https"))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }

            using (Stream requestStream = httpWebRequest.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Count());
            }
            var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            if (httpWebResponse.StatusCode != HttpStatusCode.OK)
            {
                string message = String.Format("POST failed. Received HTTP {0}", httpWebResponse.StatusCode);
                throw new ApplicationException(message);
            }

            using (Stream dataStream = httpWebResponse.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                responseFromServer = reader.ReadToEnd();
                // Display the content.            
            }
        }
        catch (Exception ex)
        {
            Log.MessageLog("Exception while processing request in PostJson in HttpClient.cs ::-" + Log.GetMaskedMessage(ex.Message+" "+ex.StackTrace));
            responseFromServer = "[{\"RspHdr\":{\"RspHdrVer\":\"3.0.0\",\"RtnCde\":\"9001\",\"MsgUuid\":\"\",\"Lcl\":null,\"ServPrmtrsLst\":{\"ServPrmtrs\":{\"FeId\":\"123\",\"ServId\":\"getCustCardAccDetails\",\"ServVer\":\"1.0.0\",\"ApplId\":\"CTX\"}},\"MsgLst\":{\"msg\":{\"MsgCde\":\"E002\",\"Typ\":null,\"MsgLcl\":null,\"Txt\":\"Exception while processing request " + ex.Message + "\"}}},\"GetCustCardAccDetailsOso\":{\"Customer\":null,\"CardList\":null}}]";
            return responseFromServer;
        }
        //Log.MessageLog("JSON RESPONSE MESSAGE ::-" + Log.GetMaskedMessage(responseFromServer));
        return responseFromServer;
    }

    public string RestClientCall(string uri, string method, string postdata, string MessageName, string Token, Dictionary<string, object> parameters, Dictionary<string, object> queryParameters, string RequestBody)
    {
        string responseFromServer = "";
        try
        {
            RestClient client = new RestClient(uri);
            RestRequest request;
            if (method == "GET")
                request = new RestRequest(Method.GET);
            else if (method == "PATCH")
                request = new RestRequest("",Method.PATCH);
            else
                request = new RestRequest("",Method.POST);

            DBConSql dbcon = new DBConSql();
            DataTable dt = new DataTable();
            dt = dbcon.GetJsonHeaderDetails(MessageName);
            foreach (DataRow row in dt.Rows)
            {
                if (row["HeaderField"].ToString().Trim() == "Authorization")
                {
                    request.AddHeader(row["HeaderField"].ToString(), row["HeaderFieldValue"].ToString() + " " + Token);
                }
                else
                {
                    request.AddHeader(row["HeaderField"].ToString(), row["HeaderFieldValue"].ToString());
                }
            }
            if (parameters != null)
            {
                foreach (var key in parameters.Keys)
                {
                    request.AddParameter(key, parameters[key]);
                }
            }
            if (queryParameters != null)
            {
                foreach (var key in queryParameters.Keys)
                {
                    request.AddQueryParameter(key, queryParameters[key].ToString());
                }
            }
            if (RequestBody != "")
            {

                request.AddParameter("application / json", RequestBody, ParameterType.RequestBody);
            }
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = (snder, cert, chain, error) => true;
            if (MessageName == "GetCustomerDetails")
            {
                request.AddHeader("userId", "1");
                request.AddHeader("entity", "GBUAEALMOU");
                request.AddHeader("languageCode", "1");
            }

            IRestResponse restResponse = client.Execute(request);
            string trackingLog = RestRequestTrackingHelper.Log(uri, request, restResponse);
            Log.MessageLog(trackingLog);
            if (restResponse.StatusCode != HttpStatusCode.OK && restResponse.StatusCode != HttpStatusCode.Created)
            {
                responseFromServer = "";
                Log.MessageLog("JSON RESPONSE MESSAGE ::-" + restResponse.ErrorMessage);
            }
            else
            {
                responseFromServer = restResponse.Content;
            }
        }
        catch (Exception ex)
        {
            Log.MessageLog("JSON RESPONSE MESSAGE ::-" + ex.Message);
        }
        return responseFromServer;
    }
    public string CreateSignature(string data)
    {
        string secret = "d3hKfnKsgD9pLmmMPQgySPX3e3WFQh3v";
        byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
        HMACSHA256 hmac = new HMACSHA256(secretBytes);
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] hashBytes = hmac.ComputeHash(dataBytes);
        string signature = Convert.ToBase64String(hashBytes);
        Console.WriteLine("HmacSHA256 is: " + signature);
        hmac.Dispose(); // Explicit disposal
        return signature;
    }
    public string RestClientCallQRCancel(string uri, string method, string postdata, string MessageName, string Token, Dictionary<string, object> parameters, Dictionary<string, object> queryParameters, string RequestBody)
    {
        string responseFromServer = "";
        try
        {
            RestClient client = new RestClient(uri);
            RestRequest request;
            if (method == "GET")
                request = new RestRequest(Method.GET);
            else if (method == "PATCH")
                request = new RestRequest(Method.PATCH);
            else
                request = new RestRequest(Method.POST);

            DBConSql dbcon = new DBConSql();
            DataTable dt = new DataTable();
            dt = dbcon.GetJsonHeaderDetails(MessageName);
            foreach (DataRow row in dt.Rows)
            {
                if (row["HeaderField"].ToString().Trim() == "Authorization")
                {
                    request.AddHeader(row["HeaderField"].ToString(), row["HeaderFieldValue"].ToString() + " " + Token);
                }
                else
                {
                    request.AddHeader(row["HeaderField"].ToString(), row["HeaderFieldValue"].ToString());
                }
            }
            request.AddHeader("x-client-id", "amb_ext");
            var reqToSign = "https://api-uat.mbankuae.com/amb/";
            var signa = CreateSignature(RequestBody);

            request.AddHeader("x-foo-signature", signa);



            if (parameters != null)
            {
                foreach (var key in parameters.Keys)
                {
                    request.AddParameter(key, parameters[key]);
                }
            }
            if (queryParameters != null)
            {
                foreach (var key in queryParameters.Keys)
                {
                    request.AddQueryParameter(key, queryParameters[key].ToString());
                }
            }
            if (RequestBody != "")
            {

                request.AddParameter("application/json", RequestBody, ParameterType.RequestBody);
            }
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = (snder, cert, chain, error) => true;

            IRestResponse restResponse = client.Execute(request);
            string trackingLog = RestRequestTrackingHelper.Log(uri, request, restResponse);
            Log.MessageLog(trackingLog);
            if (restResponse.StatusCode != HttpStatusCode.OK && restResponse.StatusCode != HttpStatusCode.Created)
            {
                responseFromServer = "";
                Log.MessageLog("JSON RESPONSE MESSAGE ::-" + restResponse.ErrorMessage);
            }
            else
            {
                responseFromServer = restResponse.Content;
            }
        }
        catch (Exception ex)
        {
            Log.MessageLog("JSON RESPONSE MESSAGE ::-" + ex.Message);
        }
        return responseFromServer;
    }

    public string RestClientCallgetCustomerDetails(string uri, string method, string postdata, string MessageName, string Token, Dictionary<string, object> parameters, Dictionary<string, object> queryParameters, string RequestBody, String InputJson, String TransactionID, String TerminalID, String ServerName)
    {
        string responseFromServer = "";
        try
        {
            RestClient client = new RestClient(uri);
            RestRequest request;

            if (method == "GET")
                request = new RestRequest(Method.GET);
            else if (method == "PATCH")
                request = new RestRequest(Method.PATCH);
            else
                request = new RestRequest(Method.POST);

            request.Timeout = 2000;

            DBConSql dbcon = new DBConSql();
            DataTable dt = new DataTable();
            dt = dbcon.GetJsonHeaderDetails(MessageName);
            foreach (DataRow row in dt.Rows)
            {
                if (row["HeaderField"].ToString().Trim() == "Authorization")
                {
                    request.AddHeader(row["HeaderField"].ToString(), row["HeaderFieldValue"].ToString() + " " + Token);
                }
                else
                {
                    request.AddHeader(row["HeaderField"].ToString(), row["HeaderFieldValue"].ToString());
                }

            }
            if (parameters != null)
            {
                foreach (var key in parameters.Keys)
                {
                    request.AddParameter(key, parameters[key]);
                }
            }
            if (queryParameters != null)
            {
                foreach (var key in queryParameters.Keys)
                {
                    request.AddQueryParameter(key, queryParameters[key].ToString());
                }
            }
            if (RequestBody != "")
            {

                request.AddParameter("application / json", RequestBody, ParameterType.RequestBody);
            }
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = (snder, cert, chain, error) => true;

            IRestResponse restResponse = new RestResponse();

            restResponse = client.Execute(request);



            string trackingLog = RestRequestTrackingHelper.Log(uri, request, restResponse);
            Log.MessageLog(trackingLog);



            if (restResponse.StatusCode != HttpStatusCode.OK && restResponse.StatusCode != HttpStatusCode.Created)
            {
                responseFromServer = "";
                Log.MessageLog("JSON RESPONSE MESSAGE ::-" + restResponse.ErrorMessage);



                if (MessageName == "GetCustomerDetails")
                {
                    Log.MessageLog("Calling second API GetCustomerDetails2  ");
                    mBank_FAB.DepositPosting cashDeposit = new mBank_FAB.DepositPosting();
                    cashDeposit.GetCustomerDetails2(InputJson, TransactionID, TerminalID, ServerName);

                }
            }
            else
            {

                responseFromServer = restResponse.Content;




            }
        }
        catch (Exception ex)
        {
            Log.MessageLog("JSON RESPONSE MESSAGE ::-" + ex.Message);
        }
        return responseFromServer;
    }


    public string RestClientCall_1(string uri, string postData, string MessageName, string Token)
    {
        string responseFromServer = "";
        try
        {

            //RestClient client = new RestClient("https://TCS/token/");
            //RestClient client = new RestClient("https://TCS/ALMR/CardServices/Enquiry/V1/CardDetailsEnquiry");
            //RestClient client = new RestClient("https://TCS/crmdynamics/");
            RestClient client = new RestClient("https://TCS/gbprest/accountManagement/account/balanceList");
            RestRequest request = new RestRequest(Method.GET);
            //string postdata = "{\"NISrvRequest\": {\"request_card_details\": {\"header\": {\"msg_id\": \"756013\",\"msg_type\": \"ENQUIRY\",\"msg_function\": \"REQ_CARD_DETAILS\",\"src_application\": \"ALMR\",\"target_application\": \"DCMS\",\"timestamp\": \"2020-08-10T10:49:02.366+04:00\",\"tracking_id\": \"756013\",\"bank_id\": \"ALMR\"},\"body\": {\"card_number\": \"5467710533974373\"}}}}";
            string postdata = "";
            DBConSql dbcon = new DBConSql();
            DataTable dt = new DataTable();
            dt = dbcon.GetJsonHeaderDetails(MessageName);
            foreach (DataRow row in dt.Rows)
            {
                if (row["HeaderField"].ToString() == "Authorization")
                {
                    request.AddHeader(row["HeaderField"].ToString(), row["HeaderFieldValue"].ToString() + Token);
                }
                else
                {
                    request.AddHeader(row["HeaderField"].ToString(), row["HeaderFieldValue"].ToString());
                }
            }
            byte[] bytes = Encoding.UTF8.GetBytes(postdata);
            string cnt = bytes.Count().ToString();
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Accept-Encoding", "gzip, deflate, br");
            //request.AddHeader("Content-Type", "application/json");
            //request.AddHeader("Host", "TCS");

            request.AddHeader("userId", "1");
            request.AddHeader("entity", "GBUAEALMOU");
            request.AddHeader("languageCode", "1");

            request.AddHeader("Authorization", "Bearer 1311a02c0e5dd9f2d556fcfe6b20c02d");

            //request.AddParameter("client_id", "XAjFOMmAueBUvhpHXLpziTwI");
            //request.AddParameter("client_secret", "-WPpBEheTMUT-qujaDIAWSimtMRjXvWPSMjM");
            //request.AddParameter("grant_type", "client_credentials");

            //request.AddQueryParameter("key", "git_cifnumber");
            //request.AddQueryParameter("value", "300100573");
            //request.AddQueryParameter("$select", "firstname");

            request.AddQueryParameter("CustomerID", "100005");

            //request.AddParameter("application/json", ParameterType.);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = (snder, cert, chain, error) => true;
            IRestResponse restResponse = client.Execute(request);

            if (restResponse.StatusCode != HttpStatusCode.OK)
            {

            }
            else
            {
                responseFromServer = restResponse.Content;
            }
        }
        catch (Exception ex)
        {
            responseFromServer = "{\"code\":" + ex.Message + ",\"data\": " + postData + "}";
        }
        return responseFromServer;
    }

    public String RegisterEIDA(string uri, String postData)
    {
        string responseFromServer = "";
        String StateCode = "";
        String StateCodeDesc = "";
        try
        {
            Log.Logging("JSON REQUEST MESSAGE ::-" + Log.GetMaskedMessage(postData));
            // string postData = JsonConvert.SerializeObject(InputData);
            byte[] bytes = Encoding.UTF8.GetBytes(postData);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentLength = bytes.Length;
            httpWebRequest.ContentType = "application/json";
            //httpWebRequest.Accept = "application/json";
            //httpWebRequest.SendChunked = true;
            // httpWebRequest.TransferEncoding = "gzip,deflate";

            //httpWebRequest.UserAgent = "Apache-HttpClient/4.1.1 (java 1.5)";

            //Adding Authentication
            String AuthUserName = ConfigurationManager.AppSettings["EIDAUserName"].ToString();
            String AuthPassword = ConfigurationManager.AppSettings["EIDAPassword"].ToString();

            iSuite.Lib.Global.g objg = new iSuite.Lib.Global.g();
            AuthUserName = objg.DecryptString(AuthUserName);
            AuthPassword = objg.DecryptString(AuthPassword);

            String AuthString = AuthUserName + ":" + AuthPassword;
            Log.Logging("eida auth String=" + AuthString);
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(AuthString));
            httpWebRequest.Headers.Add("Authorization", "Basic " + encoded);

            if (uri.Contains("https"))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }

            using (Stream requestStream = httpWebRequest.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Count());
            }
            var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            StateCode = httpWebResponse.StatusCode.ToString();
            StateCodeDesc = httpWebResponse.StatusDescription.ToString();
            //if (httpWebResponse.StatusCode != HttpStatusCode.OK)
            //{
            //    string message = String.Format("POST failed. Received HTTP {0}", httpWebResponse.StatusCode);
            //    throw new ApplicationException(message);
            //}
            if (httpWebResponse.StatusCode == HttpStatusCode.OK)
            {
                using (Stream dataStream = httpWebResponse.GetResponseStream())
                {
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    responseFromServer = reader.ReadToEnd();
                    // Display the content.            
                }
            }

        }
        catch (WebException webex)
        {
            using (var stream = webex.Response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                responseFromServer = reader.ReadToEnd();
            }

        }
        catch (Exception ex)
        {
            responseFromServer = "{\"code\":" + ex.Message + ",\"data\": " + postData + "}";
        }
        Log.Logging("JSON RESPONSE MESSAGE ::-" + Log.GetMaskedMessage(responseFromServer));
        return responseFromServer;
    }

    public static string Base64Encode(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return System.Convert.ToBase64String(plainTextBytes);
    }

    public async Task<HttpResponseMessage> SendAsync(string uri, String postData,HttpMethod httpMethod)
    {
        HttpResponseMessage responseMsg=new HttpResponseMessage();
        CreateAndSignPrivateJwt objToken = new CreateAndSignPrivateJwt();
        var responseFromServer = "";
        try
        {
            //Log.MessageLog("JSON REQUEST MESSAGE ::-" + Log.GetMaskedMessage(postData));
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback +=
            (sender, certificate, chain, sslPolicyErrors) => true;

            var handler = new HttpClientHandler();
            handler.SslProtocols = SslProtocols.Tls12;

            var client = new System.Net.Http.HttpClient(handler);
            client.DefaultRequestHeaders
                  .Accept
                  .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var accesstoken =JObject.Parse( await objToken.GetAccessToken());
            
            client.DefaultRequestHeaders.Authorization =
             new AuthenticationHeaderValue("Bearer", accesstoken["access_token"].ToString());
            //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            //request.RequestUri = new Uri(uri);
            //request.Content = new StringContent(postData, Encoding.UTF8, "application/json");
            if (httpMethod == HttpMethod.Post)
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(uri),
                    Content = new StringContent(
                                postData,
                                Encoding.UTF8,
                                "application/json"), 
                };
                responseMsg = await client.PostAsync(uri, request.Content);
            }
                
            else
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(uri),
                    Content = new StringContent(
                               postData,
                               Encoding.UTF8,
                               "application/json"), 
                };
               
                responseMsg = await client.SendAsync(request);
            }
                
            responseFromServer = responseMsg.Content.ReadAsStringAsync().Result;
        }
        catch (Exception ex)
        {
            responseFromServer = "Exception while processing request " + ex.Message ;
            Log.MessageLog("JSON RESPONSE MESSAGE ::-" + Log.GetMaskedMessage(responseFromServer));
        }
        //Log.MessageLog("JSON RESPONSE MESSAGE ::-" + Log.GetMaskedMessage(responseFromServer));
        return responseMsg;
    }


}
public static class RestRequestTrackingHelper
{
    public static string Log(string baseUrl,
                      IRestRequest request,
                      IRestResponse response)
    {

        //Get the values of the parameters passed to the API
        string parameters = string.Join(", ", request.Parameters.Select(x => x.Name.ToString() + "=" + ((x.Value == null) ? "NULL" : x.Value)).ToArray());

        //Set up the information message with the URL, 
        //the status code, and the parameters.
        string info = "Request URL: " + baseUrl
                      + request.Resource + Environment.NewLine + "status code: "
                      + response.StatusCode + Environment.NewLine + "parameters: "
                      + parameters + Environment.NewLine + "Response: " + response.Content + Environment.NewLine;


        return info;

    }
}