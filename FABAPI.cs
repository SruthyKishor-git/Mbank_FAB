using mBank_FAB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using iSuite.Lib.Logger;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Net.Http.Headers;
using iSuite.Lib.DBCon;
using System.Text;
using Newtonsoft.Json.Linq;

namespace mBank_FAB
{
    public class FABAPI
    {
        public APIConfig apiconfig = new APIConfig();
        DBConSql dBCon = new DBConSql();
                
        public async Task<JObject> GetChequeImages(string fabpayreference, string referencecode)
        {

            StatusUpdateResponse response = new StatusUpdateResponse();
            ResponseStatus objstat = new ResponseStatus();
            JObject resObj = new JObject();
            try
            {
                statusInquiryRequest req = new statusInquiryRequest();
                ApplicationArea apparea = new ApplicationArea();
                statusInquiryDataArea datarea = new statusInquiryDataArea();
                statusInquiry objinq = new statusInquiry();
                apparea.countryOfOrigin = "AE";
                apparea.creationDateTime = null;
                apparea.interfaceId = null;
                apparea.requiredExecutionDate = null;
                apparea.senderId = "MBK";
                apparea.transactionDateTime = DateTime.Now;
                apparea.transactionId = new Guid();
                datarea.fabepayReference = fabpayreference;
                datarea.referenceCode = referencecode;
                objinq.statusCode = "CKI";
                objinq.statusDesc = "Cheque Image Access";
                req.applicationArea = apparea;
                datarea.statusInquiry = objinq;
                req.dataArea = datarea;       
                
                string posturl = apiconfig.fetchChequeImageUrl;
                string jsonReq = Newtonsoft.Json.JsonConvert.SerializeObject(req);
                HttpClient client = new HttpClient();
                
                var responseMsg = await client.SendAsync(posturl,jsonReq,HttpMethod.Post);
                var responseString = responseMsg.Content.ReadAsStringAsync().Result;
               
               Log.Logger("", "", Log._App_Mbank_FAB, "FABAPI.cs", "GetChequeImages", Log._Log, "REQUEST is " + Newtonsoft.Json.JsonConvert.SerializeObject(jsonReq));
                    var jObjResp = new JObject();
                    if (responseString != null)
                        jObjResp = JObject.Parse(responseString);
                    var responseStatus = "";
                    if (jObjResp["responseStatus"] != null)
                        responseStatus = jObjResp["responseStatus"]["status"].ToString();
                    int i = 0;
                    Log.Logger("", "", Log._App_Mbank_FAB, "FABcontroller.cs", "GetChequeImages", Log._Log, "RESPONSE is " + Newtonsoft.Json.JsonConvert.SerializeObject(responseString));
                    if (responseMsg.IsSuccessStatusCode)
                    {
                        if (responseStatus == "SUCCESS")
                        {
                             var baseFrontImage= jObjResp["dataArea"]["chequeImageFrontend"].ToString();
                             var baseBackImage= jObjResp["dataArea"]["chequeImageBackend"].ToString();
                             i=dBCon.UpdateChequeTxnDetails(fabpayreference, baseFrontImage, baseBackImage);
                           
                             if (i > 0)
                             {
                            resObj.Add("fabpayreference", fabpayreference);
                            resObj.Add("chequeImageFrontend", baseFrontImage);
                            resObj.Add("chequeImageBackend", baseBackImage);
                                //response.fabpayreference = fabpayreference;
                                //response.referenceCode = referencecode;
                                //objstat.status = "000";
                                //objstat.statusMessage = "SUCCESS";
                                //objstat.errorDetails = null;
                                //response.response = objstat;
                             }
                            else
                            {
                            resObj.Add("fabpayreference", fabpayreference);
                            resObj.Add("status", "IDP007");
                            resObj.Add("statusMessage", "ERROR");
                            resObj.Add("errorDetails", "something went wrong,Please try again");
                            //response.fabpayreference = fabpayreference;
                            //    response.referenceCode = referencecode;
                            //    objstat.status = "IDP007";
                            //    objstat.statusMessage = "ERROR";
                            //    objstat.errorDetails = "something went wrong,Please try again";
                            //    response.response = objstat;
                            }
                    }
                        else
                        {
                        resObj.Add("fabpayreference", fabpayreference);
                        resObj.Add("status", responseStatus);
                        resObj.Add("statusMessage", "ERROR");
                        resObj.Add("errorDetails", jObjResp["responseStatus"]["errorDetails"].ToString());
                        //response.fabpayreference = fabpayreference;
                        //    response.referenceCode = referencecode;
                        //    objstat.status = responseStatus;
                        //    objstat.statusMessage = "ERROR";
                        //    objstat.errorDetails = jObjResp["responseStatus"]["errorDetails"].ToString();
                            response.response = objstat;
                        }
                    }
                    else
                    {
                    resObj.Add("fabpayreference", fabpayreference);
                    resObj.Add("status", responseStatus);
                    resObj.Add("statusMessage", "ERROR");
                    resObj.Add("errorDetails", jObjResp["message"].ToString());

                    //response.fabpayreference = fabpayreference;
                    //    response.referenceCode = referencecode;
                    //    objstat.status = responseStatus;
                    //    objstat.statusMessage = "ERROR";
                    //    objstat.errorDetails = jObjResp["message"].ToString();
                    //    response.response = objstat;

                    }

                
            }
            catch (Exception ex)
            {
                Log.Logger("", "", Log._App_Mbank_FAB, "FABAPI.cs", "GetChequeImages", Log._Exception, ex.Message);
                //response.fabpayreference = fabpayreference;
                //response.referenceCode = referencecode;
                //objstat.status = "IDP009";
                //objstat.statusMessage = "ERROR";
                //objstat.errorDetails = ex.Message;
                //response.response = objstat;
                resObj.Add("fabpayreference", fabpayreference);
                resObj.Add("status", "IDP009");
                resObj.Add("statusMessage", "ERROR");
                resObj.Add("errorDetails", ex.Message+" "+ex.StackTrace);
            }
            return resObj;

        }
    }
}