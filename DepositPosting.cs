using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iSuite.Lib.Logger;
using iSuite.Lib.Global;
//using mBank_FAB.iConnectAPIService;
using mBank_FAB.Models;
using Newtonsoft.Json.Linq;
using iSuite.Lib.DBCon;
using System.Configuration;
using Newtonsoft.Json;
using System.IO;
using System.Data;
using System.Net.Http;
using System.Xml;
using System.Globalization;

namespace mBank_FAB
{
    public class DepositPosting
    {
        public String SIMULATOR;
        public String MWMessagesPath;
        public  String ServerName ;
        //public string strURL = ConfigurationManager.AppSettings["MiddlewareURL"].ToString();
        APIConfig config = new APIConfig();
        DBConSql dbcon = new DBConSql();
        public DepositPosting()
        {
            SIMULATOR = ConfigurationManager.AppSettings["SIMULATOR"].ToString();
            MWMessagesPath = ConfigurationManager.AppSettings["MWMessages"].ToString();
            ServerName = System.Net.Dns.GetHostName();
        }
        public class CardRequest
        {
            public String pan { get; set; }
            public String srcId { get; set; }

            public String rqstHdrVer { get; set; }
            public String applId { get; set; }
            public String feId { get; set; }
            public String servId { get; set; }
            public String servVer { get; set; }
        }
        public String CashDeposit(String InputJson, String TransactionID, String TerminalID)
        {
            
            Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting", "CashDeposit", Log._Log, "In get CashDeposit Message");
            String RequestMessage = "";
            String ReturnMessage = "";
            String ReturnCode = "";
            String ReturnDesc = "";
            String TxnStatus_ID = "0";
            String TxnStatus = "";
            String ErrorInfo = "";
            String fabpayReference = "";
            JObject jreq = JObject.Parse(InputJson);
            // String ChannelId = jreq["ChannelId"].ToString();
            String AccountNumber = jreq["AccountNumber"].ToString();//"1000081101001";
            String CIFNumber =  jreq["CIFNumber"].ToString();//"100008110";
            String ServiceType = jreq["ServiceType"].ToString();
           String CardNumber = "";
            String CustomerSegment = jreq["Segment"].ToString();
            String FITNumber = "";
            fabpayReference = jreq["fabpayReference"].ToString();

            if (CardNumber.Length >= 7)
            {
                FITNumber = CardNumber.Substring(0, 6);
            }
            String SeqNumber = "00001";
            String Language = "";
            //String Location = jreq["Location"].ToString();
            String TxnAmount = jreq["TxnAmount"].ToString();
            string PaymentMode = "CASH";
            //string TotalNotes = jreq["TotalNotes"].ToString();
            //string NoteDetails = jreq["NoteDetails"].ToString();
            String EntryMode = "CARDLESS";
            //String SeqNumber = jreq["SeqNumber"].ToString();
            String accountTitle = jreq["CustomerName"].ToString();
            String MobileNumber = jreq["MobileNumber"].ToString();
            String MessageName = jreq["MessageName"].ToString();
            String AccountCreated = "";
            string externalReferenceNum= jreq["externalReferenceNum"].ToString();
            if (jreq["AccountCreatedOn"] != null)
            {
                AccountCreated = jreq["AccountCreatedOn"].ToString();
                try
                {
                    AccountCreated = Convert.ToDateTime(AccountCreated).ToString("dd-MM-yyyy HH:mm:ss.fff");
                }
                catch
                {
                    AccountCreated = "";
                }
            }
            JObject response = jreq;
            g objg = new g();
            

            try
            {

                String strURL = ConfigurationManager.AppSettings["MiddlewareURL"].ToString();
                HttpClient httpClient = new HttpClient();
              
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "CashDepositPosting", MessageName, Log._Log, "Post Data=" + Log.GetMaskedMessage(RequestMessage));

                RequestMessage = "<accountBookingFromBsnsCrspndsAcntToCustomerAccountRequest>" +
                                "<cbcMoAccount>" +
                                "<customerId>" + CIFNumber + "</customerId>" +
                                "<customerAccountNumber>" + AccountNumber + "</customerAccountNumber>" +
                                "<remarks>Cash deposit on Terminal(" + TerminalID + ") - SEQNO(" + SeqNumber + ")</remarks>" +
                                "<businessCorrespondencId>" + TerminalID + "</businessCorrespondencId>" +
                                "<transactioAmount>" + TxnAmount + "</transactioAmount>" +
                                "<externalReferenceNum>" + externalReferenceNum + "</externalReferenceNum>" +
                                //"<terminalType>0</terminalType>"+
                                "</cbcMoAccount>" +
                                "</accountBookingFromBsnsCrspndsAcntToCustomerAccountRequest>";

                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                var Queryparameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");

                if (SIMULATOR == "TRUE")
                {
                    ReturnMessage = File.ReadAllText(MWMessagesPath + MessageName + "_Response.xml");
                }
                else
                {
                    string token = dbcon.CheckTokenExpiry(TransactionID, TerminalID);
                    strURL += "gbprest/referenceData/accountBookingToCustomerAccount";

                    ReturnMessage = httpClient.RestClientCall(strURL, "POST", "", MessageName, token, parameters, Queryparameters, RequestMessage);
                }

                //response["MsgFormat"] = "RESPONSE";
                JObject jRoot = new JObject();
                if (ReturnMessage != "" && ReturnMessage != null)
                {
                    jRoot = JObject.Parse(ReturnMessage);
                }


                ResponseCode res = dbcon.GetResponseDesc(ReturnCode, ReturnDesc, Language);

                //JObject jResponse = (JObject)jRoot["NISrvResponse"]["response_card_details"]["exception_details"];
                //JObject jResponse = (JObject)jRoot["arg1"];
                JObject jResponse = (JObject)jRoot["cbcMoDummyRs"];


                if (jResponse!=null && jResponse["status"] != null && jResponse["status"].ToString() != "" && jResponse["status"].ToString() == "1")
                {
                    
                    TxnStatus_ID = "1";
                    TxnStatus = "SUCCESS";
                    ReturnCode = "0";
                    ReturnDesc = "Success";
                    //SendiDepositEmail(InputJson, TransactionID, TerminalID, ServerName, "Cash Deposit");
                    CallClari5Api(TransactionID, TerminalID, "1", AccountNumber, CIFNumber, accountTitle, TxnAmount, "BALMAEAA", "C", AccountCreated, "1", "1", "1", "", "", "", "FT_ACCOUNTTXN", "", "");

                }
                else
                {
                    ReturnCode = "IDP001";
                    TxnStatus_ID = "3";
                    TxnStatus = "Suspected";
                    ReturnDesc = objg.ParseXML(ReturnMessage, "ResDesc");
                    //SendiDepositServiceEmail(InputJson, TransactionID, TerminalID, ServerName, "CASH_ACCOUNT_FAIL", ReturnDesc);

                }
            }
            catch (Exception ex)
            {
                ReturnCode = "IDP001";
                TxnStatus_ID = "3";
                TxnStatus = "Suspected";
                ReturnDesc = ex.Message;
                //SendiDepositServiceEmail(InputJson, TransactionID, TerminalID, ServerName, "CASH_ACCOUNT_FAIL", ReturnDesc);
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectAPI", "CashDeposit", Log._Exception, ex.StackTrace);
            }
            response.Add("ReturnCode", ReturnCode);
            response.Add("ReturnDesc", ReturnDesc);
            response.Add("ErrorInfo", ErrorInfo);
            dbcon.TxnMessageDetailsInsert(TransactionID, TerminalID, ServerName, ReturnCode, ReturnDesc, "ICONNECT", "CashDeposit", RequestMessage, ReturnMessage, 2);
            FITNumber = "";
            String CardData = "";
            if (EntryMode == "CARD")
            {
                FITNumber = CardNumber.Substring(0, 6);
                CardData = "CardOwner='ONUS',CardNumber='" + objg.EncryptCardData(CardNumber) + "',FITNumber='" + FITNumber + "',";
            }

            try

            {
                String StrSql = "UPDATE TxnMessageQueue SET " + CardData + "TxnStatus='" + TxnStatus + "',TxnAmount1Cur='AED',TxnAmount1=" + TxnAmount + ", AccountNumber1='" + AccountNumber +
                          "',PaymentMode='" + PaymentMode +
                         "',EntryMode='" + EntryMode +
                          //"',NoteDetails='" + NoteDetails + "',TotalNotes='" + TotalNotes +
                          "',CustomerSegment='" + CustomerSegment + "',AccountTitle1='" + accountTitle + "', ReturnCode='" + ReturnCode + "',MobileNumber='" + MobileNumber + "', ReturnDesc='" + ReturnDesc + "',MessageName='CashDeposit',TxnStatus_ID='" + TxnStatus_ID + "' WHERE Transaction_ID='" + TransactionID + "' ";
                dbcon.UpdateTxnQue(StrSql, TransactionID);
            }
            catch (Exception ex)
            {
                ReturnCode = "IDP001";
                ReturnDesc = ex.Message;
                dbcon.UpdateTxnQue("UPDATE TxnMessageQueue SET ReturnCode='" + ReturnCode + "',ReturnDesc='" + ReturnDesc + "',MessageName='CashDeposit',TxnStatus_ID='3',TxnStatus='Suspected' WHERE Transaction_ID='" + TransactionID + "' ", TransactionID);
                //SendiDepositServiceEmail(InputJson, TransactionID, TerminalID, ServerName, "CASH_ACCOUNT_FAIL", ReturnDesc);
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectAPI", "CashDeposit", Log._Exception, ex.StackTrace);
            }
            //dbcon.TerminalTxnUpdate(TerminalID, TotalNotes, "CASH", TxnAmount, SeqNumber, ReturnCode);

            return response.ToString();

        }

        public String ChequeDeposit(String InputJson, String TransactionID, String TerminalID)
        {
            Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting.cs", "ChequeDeposit", Log._Log, "In get ChequeDepositPayment Message");

            JObject jreq = JObject.Parse(InputJson);
            //String ChannelId = jreq["ChannelId"].ToString();
            String AccountNumber = jreq["AccountNumber"].ToString();
            String AccountTitle = "";
            String CIFNumber = jreq["CIFNumber"].ToString();
            String ServiceType = jreq["ServiceType"].ToString();
            String CardNumber = "";
            String fabpayReference = jreq["fabpayReference"].ToString();
            String FITNumber = "";
            if (CardNumber != "" && CardNumber.Length > 6)
            {
                FITNumber = CardNumber.Substring(0, 6);
            }
            String Language = "en";
            //String Location = jreq["Location"].ToString();
            //String TxnAmount = jreq["TxnAmount"].ToString(); Commented by sathish for value not populated
            String TxnAmount = jreq["TxnAmount"].ToString();
            string PaymentMode = "CHEQUE";
            string TotalNotes = "0";
            //string NoteDetails = jreq["NoteDetails"].ToString();
            String EntryMode ="CARDLESS";
            String ReturnCode = "0";
            String ReturnDesc ="SUCCESS";
            String SeqNumber = jreq["SeqNo"].ToString();

            String MobileNumber = jreq["MobileNumber"].ToString();
            g objg = new g();
            JObject response = jreq;
            DBConSql dbcon = new DBConSql();
            try
            {
                FITNumber = "";
                String CardData = "";
                if (EntryMode == "CARD")
                {
                    FITNumber = CardNumber.Substring(0, 6);
                    CardData = "CardOwner='ONUS',CardNumber='" + objg.EncryptCardData(CardNumber) + "',FITNumber='" + FITNumber + "',";
                }


                String StrSql = "UPDATE TxnMessageQueue SET " + CardData + "TxnStatus='',TxnAmount1Cur='AED',TxnAmount1=" + "0" + ", AccountNumber1='" + AccountNumber +
                    "',PaymentMode='" + PaymentMode + "',ServiceType='" + ServiceType + "',EntryMode='" + EntryMode + "',NoteDetails='" + "" + "',TotalNotes='" + TotalNotes + "',AccountTitle1='" + AccountTitle + "', CIFNumber='" + CIFNumber +
                    "',MobileNumber='" + MobileNumber + "', ReturnCode='" + ReturnCode + "',ReturnDesc='" + ReturnDesc + "',MessageName='ChequeDeposit',TxnStatus_ID=1 WHERE Transaction_ID='" + TransactionID + "' ";
                dbcon.UpdateTxnQue(StrSql, TransactionID);
                dbcon.TerminalTxnUpdate(TerminalID, TotalNotes, "CHEQUE", TxnAmount, SeqNumber, ReturnCode);
                //Insert Cheque Images 
                String MICR = jreq["MICR"].ToString();
                string ChequeNumber = jreq["ChequeNumber"].ToString();
                string ChequeRoutingNumber = jreq["ChequeRoutingNumber"].ToString();
                string ChequeAccountNumber = jreq["ChequeAccountNumber"].ToString();
                string SeqNo = jreq["SeqNo"].ToString();
                string FrontImageName = jreq["FrontImageName"].ToString();
                double amt = 0;
                String StrInsert = "INSERT INTO txnChequeMessages (Transaction_ID,SLNO,ImageName,MICR,ChequeAmount,RoutingNo,ChequeNo,TargetAccountno)" +
                                           " VALUES('" + TransactionID + "','" + SeqNo + "','" + FrontImageName + "','" + MICR + "','" + amt + "','" + ChequeRoutingNumber + "','" + ChequeNumber + "','" + ChequeAccountNumber + "')";
                dbcon.UpdateTxnQue(StrInsert, TransactionID);
                String StrFABInsert = "INSERT INTO FABtxnChequeMessages (Transaction_ID,SLNO,ImageName,MICR,ChequeAmount,RoutingNo,ChequeNo,TargetAccountno,fabReferenceNum,CreatedDate,isImageAvailable)" +
                                           " VALUES('" + TransactionID + "','" + SeqNo + "','" + FrontImageName + "','" + MICR + "','" + amt + "','" + ChequeRoutingNumber + "','" + ChequeNumber + "','" + ChequeAccountNumber +"','"+ fabpayReference+"','"+DateTime.Now+ "','"+0+"')";
                dbcon.UpdateTxnQue(StrFABInsert, TransactionID);

                if (MobileNumber != "")
                {

                    SendCustomerChequeSMS(InputJson, TransactionID, TerminalID, ServerName, "", ChequeNumber);
                }

                ReturnCode = "0";
                ReturnDesc = "SUCCESS";

                response.Add("ReturnCode", ReturnCode);
                response.Add("ReturnDesc", ReturnDesc);
                response.Add("ErrorInfo", "");

                EndTransaction(InputJson, TerminalID, ServerName);
            }
            catch (Exception ex)
            {

                ReturnCode = "IDP001";
                ReturnDesc = ex.Message;
                Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "iConnectAPI", "ChequeDepositPayment", Log._Exception, ex.StackTrace);
            }

            return response.ToString();
            //return jreq.ToString();
        }
        public String SendCustomerChequeSMS(String InputJson, String TransactionID, String TerminalID, String ServerName, String SMSFormat, String ChequeNo)
        {
            Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting", "SendCustomerSMS", Log._Log, "In getCIF Message");
            String ReturnCode = "";
            String ReturnDesc = "";
            String ErrorInfo = "";
            JObject jreq = JObject.Parse(InputJson);
            JObject response = jreq;
            DBConSql dbcon = new DBConSql();
            CardRequest cardRequest = new CardRequest();
            String ReturnMessage = "";
            String CustomerMobile = jreq["MobileNumber"].ToString().Replace("+", "");
            //CustomerMobile = ConfigurationManager.AppSettings["CUSTOMERSMSTEST"].ToString();
            string ServiceType = jreq["ServiceType"].ToString();

            try
            {
                string token = dbcon.CheckTokenExpiry(TransactionID, TerminalID);

                string MessageName = "SendCustomerSMS";//jreq["MessageName"].ToString();
                string Language = jreq["Language"].ToString();

                DataTable dtSMS = new DataTable();

                dtSMS = dbcon.GetSMSFormat(ServiceType);
                if (dtSMS.Rows.Count > 0)
                {
                    string SMS = dtSMS.Rows[0]["SMSText"].ToString();
                    if (ServiceType == "CHEQUE_ACCOUNT")
                    {
                        string AccountNumber = jreq["AccountNumber"].ToString();
                        //string Balance = jreq["Balance"].ToString();
                        SMS = SMS.Replace("#ACC#", AccountNumber);
                        SMS = SMS.Replace("#CHQ#", ChequeNo);
                    }

                    String strURL = ConfigurationManager.AppSettings["MiddlewareURL"].ToString();
                    strURL = strURL + "campaigns/submissions/sms/nb";
                    string RequestMessage = "{\"desc\": \"This is the description for campaign\",\"campaignName\": \"test campaign\"," +
                            "\"msgCategory\": \"4.6\"," +
                            "\"contentType\": \"3.2\"," +
                            "\"senderAddr\": \"MBANK\"," +
                            "\"dndCategory\": \"Campaign\"," +
                            "\"priority\": 1," +
                            "\"clientTxnId\": " + TransactionID + "," +
                            "\"recipient\": \"" + CustomerMobile + "\"," +
                            "\"dr\" : \"1\"," +
                            "\"msg\": \"" + SMS + "\"" +
                    "}";

                    var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                    var Queryparameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                    HttpClient httpClient = new HttpClient();
                    Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting", "SendCustomerSMS", Log._Log, "Post Data=" + Log.GetMaskedMessage(RequestMessage));
                    if (SIMULATOR == "TRUE")
                    {
                        ReturnMessage = File.ReadAllText(MWMessagesPath + MessageName + "_Response.xml");
                    }
                    else
                    {
                        ReturnMessage = httpClient.RestClientCall(strURL, "", "", MessageName, token, parameters, Queryparameters, RequestMessage);
                    }
                    JObject jRoot = JObject.Parse(ReturnMessage);

                    try
                    {
                        ResponseCode res = dbcon.GetResponseDesc(ReturnCode, ReturnDesc, Language);
                        ErrorInfo = res.ErrorInfo;
                        response["MsgFormat"] = "RESPONSE";
                        //RETURN CODE SHOULD BE COME FROM MIDDLEWARE
                        ReturnCode = "000";
                        if (ReturnCode == "000")
                        {
                            JArray Beneficiaries = new JArray();
                            ReturnCode = "0";
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting", "SendCustomerSMS", Log._Exception, ex.StackTrace);
                    }
                }
                else
                {
                    ReturnCode = "9001";
                    ReturnDesc = "";
                    Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting", "SendCustomerSMS", Log._Exception, "Invalid Service name");
                }

            }
            catch (Exception ex)
            {
                ReturnCode = "9001";
                ReturnDesc = ex.Message;
                Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting", "SendCustomerSMS", Log._Exception, ex.StackTrace);
            }
            //response.Add("ReturnCode", ReturnCode);
            //response.Add("ReturnDesc", ReturnDesc);
            //response.Add("ErrorInfo", ErrorInfo);
            return response.ToString();
        }

        public String SendCustomerSMS(String InputJson, String TransactionID, String TerminalID, String ServerName, String SMSFormat)
        {
            Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting", "SendCustomerSMS", Log._Log, "In getCIF Message");
            String ReturnCode = "";
            String ReturnDesc = "";
            String ErrorInfo = "";
            JObject jreq = JObject.Parse(InputJson);
            JObject response = jreq;
            DBConSql dbcon = new DBConSql();
            CardRequest cardRequest = new CardRequest();
            String ReturnMessage = "";
            String CustomerMobile = "";
            try { CustomerMobile = jreq["Mobile"].ToString(); } catch (Exception) { }

            CustomerMobile = ConfigurationManager.AppSettings["CUSTOMERSMSTEST"].ToString();
            string ServiceType = jreq["ServiceType"].ToString();

            try
            {
                //string token = dbcon.CheckTokenExpiry(TransactionID, TerminalID);
                string token = GenerateBANKSMSTOKEN();
                string MessageName = "SendCustomerSMS";//jreq["MessageName"].ToString();
                string Language = jreq["Language"].ToString();

                DataTable dtSMS = new DataTable();

                dtSMS = dbcon.GetSMSFormat(ServiceType);
                if (dtSMS.Rows.Count > 0)
                {
                    string SMS = dtSMS.Rows[0]["SMSText"].ToString();
                    if (ServiceType == "BALANCEENQUIRY")
                    {
                        string AccountNumber = jreq["AccountNumber"].ToString();
                        string Balance = jreq["Balance"].ToString();
                        SMS = SMS.Replace("#ACC#", AccountNumber);
                        SMS = SMS.Replace("#BALANCE#", Balance);
                    }
                    if (ServiceType == "EMIRATESID_UPDATE")
                    {
                        string REQNumber = jreq["ReqNumber"].ToString();
                        string MachineType = jreq["MachineType"].ToString();
                        SMS = SMS.Replace("#REQNUMBER#", REQNumber);
                        SMS = SMS.Replace("#ATM/MFK#", MachineType);
                        SMS = SMS.Replace("#REQDATE#", DateTime.Now.ToString("dd/MMM/yyyy"));
                    }
                    //String strURL = ConfigurationManager.AppSettings["MiddlewareURL"].ToString();
                    String strURL = ConfigurationManager.AppSettings["BANKSMSURL"].ToString();
                    strURL = strURL + "/campaigns/submissions/sms/nb";
                    string RequestMessage = "{\"msgCategory\": \"4.5\"," +
                            "\"contentType\": \"3.2\"," +
                            "\"senderAddr\": \"MBANKAlert\"," +
                            "\"dndCategory\": \"\"," +
                            "\"priority\": 1," +
                            "\"clientTxnId\": " + TransactionID + "," +
                            "\"recipient\": \"" + CustomerMobile + "\"," +
                            "\"dr\" : \"1\"," +
                            "\"msg\": \"" + SMS + "\"" +
                    "}";

                    var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                    var Queryparameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                    HttpClient httpClient = new HttpClient();
                    Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "SendCustomerSMS", Log._Log, "Post Data=" + Log.GetMaskedMessage(RequestMessage));
                    if (SIMULATOR == "TRUE")
                    {
                        ReturnMessage = File.ReadAllText(MWMessagesPath + MessageName + "_Response.xml");
                    }
                    else
                    {
                        ReturnMessage = SendCustomerSMS(RequestMessage, TransactionID, TerminalID, ServerName, SMSFormat);  //httpClient.RestClientCall(strURL, "", "", MessageName, token, parameters, Queryparameters, RequestMessage);
                    }
                    JObject jRoot = JObject.Parse(ReturnMessage);

                    try
                    {
                        ResponseCode res = dbcon.GetResponseDesc(ReturnCode, ReturnDesc, Language);
                        ErrorInfo = res.ErrorInfo;
                        response["MsgFormat"] = "RESPONSE";
                        //RETURN CODE SHOULD BE COME FROM MIDDLEWARE
                        ReturnCode = "000";
                        if (ReturnCode == "000")
                        {
                            JArray Beneficiaries = new JArray();
                            ReturnCode = "0";
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "SendCustomerSMS", Log._Exception, ex.StackTrace);
                    }
                }
                else
                {
                    ReturnCode = "9001";
                    ReturnDesc = "";
                    Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "SendCustomerSMS", Log._Exception, "Invalid Service name");
                }

            }
            catch (Exception ex)
            {
                ReturnCode = "9001";
                ReturnDesc = ex.Message;
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "SendCustomerSMS", Log._Exception, ex.StackTrace);
            }
            response.Add("ReturnCode", ReturnCode);
            response.Add("ReturnDesc", ReturnDesc);
            response.Add("ErrorInfo", ErrorInfo);
            return response.ToString();
        }
        public string GenerateBANKSMSTOKEN()
        {
            String Token = "";
            String ReturnMessage = "";
            String MessageName = "GetSMS_Token";
            String username = ConfigurationManager.AppSettings["BANKSMSUSERNAME"].ToString();
            String password = ConfigurationManager.AppSettings["BANKSMSPASSWORD"].ToString();

            try
            {

                String strURL = ConfigurationManager.AppSettings["BANKSMSURL"].ToString();
                strURL = strURL + "/marvel/login/user";

                string RequestMessage = "{\"username\": \"" + username + "\",\"password\": \"" + password + "\"}";

                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                var Queryparameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                HttpClient httpClient = new HttpClient();
                ReturnMessage = httpClient.RestClientCall(strURL, "", "", MessageName, Token, parameters, Queryparameters, RequestMessage);

                JObject jRoot = JObject.Parse(ReturnMessage);
                Log.Logger("", "", Log._App_iconnectApi, "iConnectMsg", "GenerateSMStoken", Log._Log, "Response Data=" + ReturnMessage);
                try
                {
                    JObject jres = JObject.Parse(ReturnMessage);
                    string ReturnCode = jres["decodedValue"]["status"].ToString();
                    if (ReturnCode == "1")
                    {
                        Token = jres["token"].ToString();
                    }
                    else
                    {
                        Token = "";
                    }

                }
                catch (Exception ex)
                {
                    Log.Logger("", "", Log._App_iconnectApi, "iConnectMsg", "GenerateSMStoken", Log._Log, "Error=" + ex.Message.ToString());
                }

            }
            catch (Exception ex)
            {
                Log.Logger("", "", Log._App_iconnectApi, "iConnectAPI.cs", "GenerateBANKSMSTOKEN", Log._Exception, ex.StackTrace);
                Token = "";
            }
            return Token;
        }

        public JObject ValidateWallet(string InputJson, String TransactionID, String TerminalID)
        {

            string strURL = config.validateWalletUrl; ;
            
            string MessageName = "ValidateWallet";
            string token = dbcon.CheckTokenExpiry(TransactionID, TerminalID);
            var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
            var Queryparameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
            HttpClient httpClient = new HttpClient();
            Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting.cs", "ValidateWallet", Log._Log, "Request Data=" + Log.GetMaskedMessage(InputJson));
            string ReturnMessage = "";
            if (SIMULATOR == "TRUE")
            {
                 ReturnMessage = File.ReadAllText(MWMessagesPath + MessageName + "_Response.xml");
            }
            else
            {
                 ReturnMessage = httpClient.RestClientCall(strURL, "", "", MessageName, token, parameters, Queryparameters, InputJson);
            }
            JObject jRoot = JObject.Parse(ReturnMessage);
            JObject ac = new JObject();
            JArray jArray = new JArray();
            JObject response = new JObject();
            string ReturnCode = "";
            string ReturnDesc = ""; 
            try
            {
                JObject jres = JObject.Parse(ReturnMessage);
                if(jres["response"]["exist"] != null)
                    ReturnCode = jres["response"]["exist"].ToString();
                else
                {
                    ReturnCode = jres["response"]["status"].ToString();
                    response.Add("ReturnCode", ReturnCode);
                    response.Add("ReturnDesc", ReturnDesc);
                    response.Add("TotalAccount", jArray.Count);
                    response.Add("ACCOUNT_LST", jArray);
                    return response;
                }
                   

                if (ReturnCode == "True")
                {
                    ReturnCode = "0";
                     ReturnDesc = jres["response"].ToString();
                    ac.Add("CIF", jres["response"]["cif"].ToString());
                    ac.Add("AccountNumber", jres["response"]["virtual_iban"].ToString());
                    ac.Add("AccountTitle", jres["response"]["holder_name"].ToString());
                    String AccountType = "Wallet";

                    ac.Add("AccountType", AccountType);
                    ac.Add("Currency", "AED");
                    ac.Add("iban", jres["response"]["virtual_iban"].ToString());
                    ac.Add("AccountStatus", jres["response"]["wallet_status"].ToString());
                    ac.Add("AccountCategory", "Wallet");
                    String availableBalance = jres["response"]["wallet_balance"].ToString();
                    try
                    {
                        availableBalance = string.Format("{0:N2}", Convert.ToDouble(availableBalance));
                        ac.Add("AccountBalance", availableBalance);
                    }
                    catch (Exception ex) {

                    }

                    jArray.Add(ac);
                }
                else
                {

                     ReturnDesc = jres["response"]["exist"].ToString();
                }
            }
            catch (Exception ex)
            {
                ReturnCode = "9001";
                ReturnDesc = ex.Message;
                Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting.cs", "ValidateWallet", Log._Exception, "exception is "+ex.StackTrace);
            }
            response.Add("ReturnCode", ReturnCode);
            response.Add("ReturnDesc", ReturnDesc);
            response.Add("TotalAccount", jArray.Count);
            response.Add("ACCOUNT_LST", jArray);
            Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting.cs", "ValidateWallet", Log._Log, "RESPONSE IS "+JsonConvert.SerializeObject(response));
            return response;
        }


        public string CashWalletPosting(String InputJson, String TransactionID, String TerminalID, String ServerName)
        {
            Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "DepositPosting", "CashWalletPosting", Log._Log, "In CashWalletPosting Message");
            String ReturnCode = "";
            String ReturnDesc = "";
            String ErrorInfo = "";
            JObject jreq = JObject.Parse(InputJson);
            JObject response = jreq;
            DBConSql dbcon = new DBConSql();

            
           // String ChannelId = jreq["ChannelId"].ToString();
           // String AccountNumber = jreq["AccountNumber"].ToString();
            String AccountTitle = jreq["AccountTitle"].ToString();
            String CIFNumber = jreq["CIFNumber"].ToString();
            //String CardNumber = jreq["CardNumber"].ToString();
            //String FITNumber = "";
            //if (CardNumber != "" && CardNumber.Length >= 6)
            //{
            //    FITNumber = CardNumber.Substring(0, 6);
            //}
            String Location = jreq["Location"].ToString();
            //String TxnAmount = jreq["TxnAmount"].ToString();
            string WalletOption = jreq["WalletOption"].ToString();

            //String SeqNumber = jreq["SeqNumber"].ToString();



            String ReturnMessage = "";
            String cid = TerminalID + "_" + TransactionID;
            String iban = jreq["virtual_iban"].ToString();
            String amount = jreq["TxnAmount"].ToString();
            String Wsource = "FABATM";
            String source_ref = TerminalID + "_" + TransactionID;
            string externalReferenceNum = jreq["externalReferenceNum"].ToString();
            // String source_note = "top up my wallet via iban";
            String source_note = "cash deposit to mWallet via " + TerminalID + " @" + Location;
            //iban="AE320973002130670000123";

            //string ServiceType = jreq["ServiceType"].ToString();

            try
            {
                string token = dbcon.CheckTokenExpiry(TransactionID, TerminalID);

                string MessageName = "CashWalletDeposit";
                string Language = jreq["Language"].ToString();


                /*{
                    "cid": "4555555",
                    "iban": "AE320973002130670000123",
                    "amount": 15,
                    "source": "TCS-VIBAN",
                    "source_ref": "US010055989710000000673",
                    "source_note": "top up my wallet via iban"
                } */

                String strURL = config.cashDepositWalletUrl;
                
                string RequestMessage = "{\"cid\": \"" + cid + "\"," +
                        "\"iban\": \"" + iban + "\"," +
                        "\"amount\": \"" + amount + "\"," +
                        "\"source\": \"" + Wsource + "\"," +
                        "\"source_ref\": \"" + source_ref + "\"," +
                        "\"source_note\": \"" + source_note + "\"" +
                        "\"externalReferenceNum\": \"" + externalReferenceNum + "\"" +
                "}";

                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                var Queryparameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                HttpClient httpClient = new HttpClient();
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "DepositPosting", "CashWalletDeposit", Log._Log, "REQUEST is" + Log.GetMaskedMessage(RequestMessage));
                if (SIMULATOR == "TRUE")
                {
                    ReturnMessage = File.ReadAllText(MWMessagesPath + MessageName + "_Response.xml");
                }
                else
                {
                    ReturnMessage = httpClient.RestClientCall(strURL, "", "", MessageName, token, parameters, Queryparameters, RequestMessage);
                }
                JObject jRoot = JObject.Parse(ReturnMessage);
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "DepositPosting", "CashWalletDeposit", Log._Log, "RESPONSE is" + ReturnMessage);

                try
                {
                    JObject jres = JObject.Parse(ReturnMessage);

                    ReturnCode = jres["response"]["success"].ToString();
                    Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "CashWalletDeposit", Log._Log, "Response Code=" + ReturnCode);
                    string txnstatus = "";
                    if (ReturnCode == "True")
                    {
                        ReturnCode = "0";
                        ReturnDesc = "Success";
                        //response.Add("cif", jres["response"]["cif"].ToString());
                        //response.Add("virtual_iban", jres["response"]["virtual_iban"].ToString());
                        //response.Add("wallet_iban", jres["response"]["wallet_iban"].ToString());
                        txnstatus = "Success";
                        Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "CashWalletDeposit", Log._Log, "Response =" + ReturnCode + "  " + ReturnDesc);
                    }
                    else
                    {
                        txnstatus = "Failure";
                        ReturnDesc = "Failure";
                        //ReturnDesc = jres["response"]["success"].ToString();
                    }
                    string StrSqlIn = "insert into txnwalletdeposit values ('" + DateTime.Now.Date.ToString("dd-MMM-yyyy hh:mm:ss") + "','" + TransactionID + "','" + AccountTitle + "','" + CIFNumber + "','" + iban + "','" + iban + "','1','" + amount + "','Y','300102479','Y','','" + jres["response"].ToString() + "','" + WalletOption + "')";
                    dbcon.UpdateTxnQue(StrSqlIn, TransactionID);

                    //dbcon.TerminalTxnUpdate(TerminalID, TotalNotes, "CashWalletDeposit", amount, SeqNumber, ReturnCode);
                }
                catch (Exception ex)
                {
                    Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "CashWalletFundtransfer", Log._Exception, ex.StackTrace);
                }


            }
            catch (Exception ex)
            {
                ReturnCode = "9001";
                ReturnDesc = ex.Message;
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "CashWalletFundtransfer", Log._Exception, ex.StackTrace);
            }
            //response.Add("ReturnCode", ReturnCode);
            //response.Add("ReturnDesc", ReturnDesc);
            //response.Add("ErrorInfo", ErrorInfo);
            return ReturnDesc.ToString();
        }
        public String GetCustomerDetails2(String InputJson, String TransactionID, String TerminalID, String ServerName)
        {

            Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectAPI", "GetCustomerDetails", Log._Log, "In get GetCustomerDetails Message");
            String RequestMessage = "";
            String ReturnMessage = "";
            String ReturnCode = "";
            String ReturnDesc = "";
            String ErrorInfo = "";
            JObject jreq = JObject.Parse(InputJson);
            String ChannelId = jreq["ChannelId"].ToString();
            String CIFNumber = jreq["CIFNumber"].ToString();
            String MessageName = jreq["MessageName"].ToString();
            String CardNumber = jreq["CardNumber"].ToString();
            String MobileNumber = "";
            String EmailId = "";

            String Language = "En";
            String CustomerName = "";
            JObject response = jreq;
            g objg = new g();
            DBConSql dbcon = new DBConSql();
            try
            {

                String strURL = ConfigurationManager.AppSettings["MiddlewareURL"].ToString();

                strURL = strURL + "crmdynamics/contacts";
                RequestMessage = "{\"key\":\"git_cifnumber\",\"value\":\"" + CIFNumber + "\"}";
                //RequestMessage = "{\"key\":\"git_cifnumber\",\"value\":\"" + CIFNumber + "\",\"$select\":\"firstname,lastname,statecode,git_cifnumber,mobilephone,emailaddress1,fullname,governmentid,gits_tpin,gits_tpinstatus,git_nationalidexpirydate\"}";


                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                var Queryparameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(RequestMessage);

                HttpClient httpClient = new HttpClient();

                if (SIMULATOR == "TRUE")
                {
                    //ReturnMessage = "{\"@odata.context\":\"https://almaryah-crm-uat.crm15.dynamics.com/api/data/v9.2/$metadata#contacts(firstname,lastname,git_cifnumber,mobilephone,emailaddress1,governmentid,gits_tpin,gits_tpinstatus)/$entity\",\"@odata.etag\":\"W/26257009\",\"firstname\":\"AJAY\",\"lastname\":\"KUMAR\",\"git_cifnumber\":\"500600700\",\"mobilephone\":\"2695\",\"emailaddress1\":\"valkya27+41@gmail.com\",\"governmentid\":\"784198082516257\",\"gits_tpin\":null,\"gits_tpinstatus\":null,\"contactid\":\"9ce0e123-f3e9-eb11-bacb-0022480cf5aa\"}";
                    ReturnMessage = File.ReadAllText(MWMessagesPath + MessageName + "_Response.xml");
                }
                else
                {
                    string token = dbcon.CheckTokenExpiry(TransactionID, TerminalID);
                    ReturnMessage = httpClient.RestClientCallgetCustomerDetails(strURL, "", "", MessageName, token, parameters, Queryparameters, "", InputJson, TransactionID, TerminalID, ServerName);
                }
                //
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectAPI", "GetCustomerDetails", Log._Log, "In get GetCustomerDetails Message details" + ReturnMessage);

                JObject jRoot = JObject.Parse(ReturnMessage);
                ReturnCode = "000";
                ReturnDesc = "Success";
                response["MsgFormat"] = "RESPONSE";
                if (ReturnCode == "000")
                {
                    ReturnCode = "0";
                    ReturnDesc = "SUCCESS";
                    //CustomerType,CustomerName_En,CustomerName_Ar,MobileNumber,CustomerStatus,dateOfBirth

                    string CustomerName_En = jRoot["firstname"].ToString() + " " + jRoot["lastname"].ToString();
                    response.Add("CustomerName_En", CustomerName_En);

                    MobileNumber = jRoot["mobilephone"].ToString();
                    EmailId = jRoot["emailaddress1"].ToString();
                    response.Add("MobileNumber", MobileNumber);

                    response.Add("EmailAddress", EmailId);


                    response.Add("EmiratesID", jRoot["governmentid"].ToString());
                    //response.Add("EmiratesID", "784199070641941");

                    response.Add("gits_tpin", jRoot["gits_tpin"].ToString());
                    response.Add("gits_tpinstatus", jRoot["gits_tpinstatus"].ToString());
                    response.Add("contactid", jRoot["contactid"].ToString());
                    response.Add("statecode", jRoot["statecode"].ToString());
                    response.Add("FullName", jRoot["fullname"].ToString());
                    //response.Add("EIDexpiryDate", jRoot["git_nationalidexpirydate"].ToString());
                    String EIDAExpiryDate = jRoot["msfsi_idexpirydate"].ToString();
                    try
                    {
                        if (EIDAExpiryDate == null || EIDAExpiryDate == "")
                            EIDAExpiryDate = DateTime.Now.AddDays(2).ToString("yyyy-MM-dd");

                        string expirynationaliddate = jRoot["git_nationalidexpirydate"].ToString();
                        if (Convert.ToDateTime(expirynationaliddate) >= Convert.ToDateTime(EIDAExpiryDate))
                        {
                            EIDAExpiryDate = expirynationaliddate;
                        }
                    }
                    catch (Exception ex) { }


                    //EIDAEXPIRYDATE = "01-01-2023";
                    response.Add("EIDexpiryDate", EIDAExpiryDate);
                    try
                    {
                        response.Add("git_residenceapartmentnumber", jRoot["git_residenceapartmentnumber"].ToString());
                        response.Add("gits_nearestlandmark", jRoot["gits_nearestlandmark"].ToString());
                        response.Add("gits_area", jRoot["gits_area"].ToString());
                        response.Add("gits_street", jRoot["gits_street"].ToString());
                        response.Add("gits_poboxnumber", jRoot["gits_poboxnumber"].ToString());
                        response.Add("gits_pobox", jRoot["gits_pobox"].ToString());

                    }
                    catch (Exception ex) { }

                    string segmentvalue = "";
                    try
                    {

                        segmentvalue = jRoot["gits_defaultsegment"]["gits_segmentid"].ToString();
                    }
                    catch (Exception ex) { }
                    response.Add("Segment", segmentvalue);
                    String DOB = "";//objg.ParseXML(ReturnMessage, "dateOfBirth");
                    String Age = "0";
                    DateTime dtDateTime = DateTime.Now.Date;
                    try
                    {

                        if (DOB != "")
                        {
                            dtDateTime = Convert.ToDateTime(DOB);
                            Age = ((System.DateTime.Now.Subtract(dtDateTime)).Days / 365).ToString();
                            response.Add("CustomerAge", Age);
                        }
                        else
                        {
                            response.Add("CustomerAge", "22");
                        }


                        dtDateTime = Convert.ToDateTime(EIDAExpiryDate);
                        String _EidExpiryindays = ((dtDateTime.Subtract(System.DateTime.Now)).Days).ToString();

                        double _age = Convert.ToDouble(_EidExpiryindays);
                        String EXPIRY_DAYS = dbcon.GetUtilityConfigValue("EMIRATESID_UPDATE", "EXPIRY_DAYS");


                        if (_age < -90)
                        {
                            response.Add("EIDAExpirycheck", "UPDATEONLY");
                            response.Add("ShowEIDAPopUp", "YES");
                        }
                        else if (_age >= -90)
                        {
                            response.Add("EIDAExpirycheck", "YES");
                            if (_age >= Convert.ToDouble(EXPIRY_DAYS))
                            {
                                response.Add("ShowEIDAPopUp", "NO");
                                //response.Add("ShowEIDAPopUp", "YES");
                            }
                            else
                            {
                                response.Add("ShowEIDAPopUp", "YES");
                            }
                        }
                        else
                        {
                            response.Add("EIDAExpirycheck", "NO");
                        }
                        string stmntcharges = "0";
                        DataTable dtcharges = dbcon.GetCharges(CIFNumber, segmentvalue);
                        if (dtcharges != null && dtcharges.Rows.Count > 0)
                        {
                            stmntcharges = dtcharges.Rows[0]["chargeamount"].ToString();

                        }
                        response.Add("StmtCharges", stmntcharges);
                    }
                    catch (Exception ex) { }
                }
            }
            catch (Exception ex)
            {
                ReturnCode = "IDP9002";
                ReturnDesc = "Customer Details Not Found Try Again Later or Contact Customer Care";
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectAPI", "GetCustomerDetails", Log._Exception, ex.StackTrace);
            }
            string strUpdate = " Update TxnMessageQueue set CIFNUMBER= '" + CIFNumber + "',EmailId= '" + EmailId + "',MobileNumber = '" + MobileNumber + "' , CARDNUMBER= '" + CardNumber + "', MessageName = 'GetCustomerDetails' Where Transaction_ID = '" + TransactionID + "' ";
            dbcon.UpdateTxnQue(strUpdate, TransactionID);

            response.Add("ReturnCode", ReturnCode);
            response.Add("ReturnDesc", ReturnDesc);
            response.Add("ErrorInfo", ErrorInfo);
            dbcon.TxnMessageDetailsInsert(TransactionID, TerminalID, ServerName, ReturnCode, ReturnDesc, "ICONNECT", "GetCustomerDetails", RequestMessage, ReturnMessage, 2);
            return response.ToString();
        }

        public String GetToken(string TransactionID, string TerminalID)
        {
            Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "GetToken", Log._Log, "Get Token");

            DBConSql dbcon = new DBConSql();

            string RequestMessage = "";
            string ReturnMessage = "";
            string Token = "";

            try
            {
                String strURL = ConfigurationManager.AppSettings["MiddlewareURL"].ToString();
                String MessageName = "GetToken";
                strURL = strURL + "token/";
                //  RequestMessage = "{\"client_id\":\"XAjFOMmAueBUvhpHXLpziTwI\",\"client_secret\":\"-WPpBEheTMUT-qujaDIAWSimtMRjXvWPSMjM\",\"grant_type\":\"client_credentials\"}";
                    RequestMessage = "{\"client_id\":\"YYgViMyDldBcRobvOtTHoVBx\",\"client_secret\":\"sNYPefkWuYzSrNGrMBUNNheFuDoHPaZyAWkQ\",\"grant_type\":\"client_credentials\"}";
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(RequestMessage);
                var Queryparameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");

                HttpClient httpClient = new HttpClient();

                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "GetToken", Log._Log, "Post Data=" + Log.GetMaskedMessage(RequestMessage));

                if (SIMULATOR == "TRUE")
                {
                    ReturnMessage = "{\"access_token\":\"5dfe1cd072cb4e27fb44ef551f375efc\",\"refresh_token_expires_in\":\"7200\",\"refresh_token\":\"0517ad8b3627e5599f52ebfa6f88d39f\",\"expires_in\":3600}";
                }
                else
                {
                    int access_token_expiry;
                    string refresh_token = "";
                    int refresh_token_expiry;

                    ReturnMessage = httpClient.RestClientCall(strURL, "", "", MessageName, "", parameters, Queryparameters, "");
                    JObject jRoot = JObject.Parse(ReturnMessage);

                    try
                    {
                        if (ReturnMessage != "")
                        {
                            Token = GetJsonValue(jRoot, "$.access_token");
                            access_token_expiry = Convert.ToInt32(GetJsonValue(jRoot, "$.expires_in"));
                            refresh_token = GetJsonValue(jRoot, "$.refresh_token");
                            refresh_token_expiry = Convert.ToInt32(GetJsonValue(jRoot, "$.refresh_token_expires_in"));
                            dbcon.UpdateTokenDetails(Token, access_token_expiry, refresh_token, refresh_token_expiry);
                        }
                        else
                        {
                            Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "GetToken", Log._Log, "No Response from Middleware");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "GetToken", Log._Exception, ex.StackTrace);
                    }

                }

            }
            catch (Exception ex)
            {
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", "GetToken", Log._Exception, ex.StackTrace);
            }
            return Token;
        }

        private String GetJsonValue(JObject jsonObj, String path)
        {
            String retVal = "";
            try
            {
                JValue jValue = (JValue)jsonObj.SelectToken(path);
                retVal = jValue.Value.ToString();
            }
            catch (Exception ex)
            {
                Log.Logger("", "", Log._App_iconnectApi, "iConnectAPI", "GetJsonValue", Log._Exception, ex.StackTrace);
                Log.Logger("", "", Log._App_iconnectApi, "iConnectAPI", "GetJsonValue", Log._Exception, "Path=" + path + " Message=" + jsonObj.ToString());
            }

            return retVal;
        }

        public void CallClari5Api(string TransactionID, string TerminalID, string TxnType, string AccountNumber, string CIFNumber, string AccountTitle, string txnAmount, string Bankcode, string CounterPartTranType, string AccountCreatedOn, string Part_tran_type, string Cust_const, string schm_type, string payee_id, string counter_party_bank_name, string counter_party_acct_num, string txnType, string MobNumber, string CardNumber)
        {
            DBConSql d = new DBConSql();
            string TerminalIP = d.getTerminalIPAddress(TerminalID);

            string MessageName = "Clari5FT";
            string RequestMessage = "";
            DBConSql dbcon = new DBConSql();
            string token = dbcon.CheckTokenExpiry(TransactionID, TerminalID);
            String strURL = ConfigurationManager.AppSettings["MiddlewareURL"].ToString();
            //strURL += "hes/rest/core/message/enqueue";
            strURL = "https://TCS/mq/rest/cxq/load/put?q=HOST&event_id=MBS30092021120856987&entity_id=000000380&event_ts=1528873088";
            HttpClient httpClient = new HttpClient();
            //Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectMsg", MessageName, Log._Log, "Post Data=" + Log.GetMaskedMessage(RequestMessage));

            string Datetimenow = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss.fff");


            //DateTime tn = new DateTime(2023,2,1,6,1,1);

            //string Datetimenow =  tn.ToString("dd-MM-yyyy HH:mm:ss.fff");
            if (txnType == "FT_ACCOUNTTXN")

            {
                RequestMessage = "{\"eventtype\":\"ft\",\"eventsubtype\":\"accounttxn\"," +
                                    "\"event_name\":\"ft_accounttxn\",\"eventts\":\"" + Datetimenow + "\"" +
                                    ",\"msgBody\":\"{ 'source':'ATM','event_id':'" + TransactionID + "','host_id':'F','sys_time':'" + Datetimenow + "','channel':'DC','account_id':'" + AccountNumber + "'," +
                                    "'cust_id':'" + CIFNumber + "','schm_type':'" + schm_type + "','schm_code':'','place_holder':'','acct_name':'" + AccountTitle + "','acct_branch_id':'','acct_ownership':''" +
                                    ",'acctopendate':'','avl_bal':'','clr_bal_amt':'','un_clr_bal_amt':'','eff_avl_bal':'','tran_type':'" + TxnType + "','tran_sub_type':''" +
                                    ",'tran_category':'','tran_id':'" + TransactionID + "','tran_date':'" + Datetimenow + "','pstd_date':'" + Datetimenow + "','value_date':'','part_tran_srl_num':'','tran_amt':'" + txnAmount + "'" +
                                    ",'tran_crncy_code':'AED','ref_tran_amt':'" + txnAmount + "','ref_tran_crncy':'AED','tran_particular':'','tran_rmks':'','ref_num':'" + TransactionID + "','instrmnt_type':''" +
                                    ",'instrmnt_num':'','instrmnt_date':'','ip_address':'" + TerminalIP + "','bank_code':'" + Bankcode + "','pstd_flg':'','online_batch':'','pstd_user_id':'','hdrmkrs':''" +
                                    ",'entry_user':'','acct_occp_code':'','mode_oprn_code':'','branch_id_desc':'','emp_id':'','work_class':'','tran_branch_id':'','country_code':'UAE'" +
                                    ",'cust_const':'" + Cust_const + "','payee_id':'" + payee_id + "','bin':'','txn_br_city_code':'','home_br_city_code':'','cust_multi_acct':'','status':'','counter_party_bank_name':'" + counter_party_bank_name + "'" +
                                    ",'counter_party_bank_swift_code':'','counter_party_acct_num':'" + counter_party_acct_num + "','counter_party_name':'','counter_party_country_code':'" + CounterPartTranType + "', 'part_tran_type':'" + Part_tran_type + "','counter_party_cust_id':''" +
                                    ",'is_cpty_bank_cust':'','last_tran_date':'','acct_activation_date':'','acct_open_date':'" + AccountCreatedOn + "','pin_code':'','mobile_no':''}\"}";

            }
            else
            {
                RequestMessage = "{\"eventtype\":\"nft\",\"eventsubtype\":\"regen\",\"event_name\":\"nft_regen\",\"eventts\":\"" + Datetimenow + "\",\"msgBody\":\"{" +
                    "'source':" +
                    "'ATM','event_id':'" + TransactionID + "','host_id':'F','sys_time':'" + Datetimenow + "','channel': 'DC','cust_id':'" + CIFNumber + "','user_id':'" + CIFNumber + "','user_type':'','device_id':'" + TerminalID + "','ip_address':'" + TerminalIP + "','ip_country':'','ip_city':'','succ_fail_flg':'','error_code':'','error_desc':'','regen_type':'PIN','country_code':'IN','mobile_no':'" + MobNumber + "','tran_date':'" + Datetimenow + "'" +
                    ",'app_id':'','cust_card_id':'','mask_card_no':'" + CardNumber.Substring(CardNumber.Length - 4).PadLeft(CardNumber.Length, '*') + "'}\"}";
            }


            var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
            var Queryparameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
            string ReturnMessage = httpClient.RestClientCall(strURL, "POST", "", MessageName, token, parameters, Queryparameters, RequestMessage);




        }

        public String EndTransaction(String inputJson, String TerminalID, String ServerName)
        {
            JObject jres = JObject.Parse(inputJson);
            String TransactionID = jres["TransactionID"].ToString();
            //String TxnClosedBy = jres["TxnClosedBy"].ToString();

            Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectAPI.cs", "EndTransaction", Log._Log, "in EndTransaction");
            String returnMessage = "";

            DBConSql dbcon = new DBConSql();
            jres["MsgFormat"] = "RESPONSE";
            try
            {
                String strUpdate = " Update TxnMessageQueue set  TxnClosedBy='" + "" + "', " +
                " TxnEndDate=GetDate(),TimeTaken= DateDiff( SECOND ,txndate, GetDate()) " +
                " where Transaction_ID = '" + TransactionID + "'";
                dbcon.UpdateTxnQue(strUpdate, TransactionID);
                // DB INSERT END          
                dbcon.MoveTransaction(TerminalID, TransactionID);
                if (inputJson.ToString().Contains("ReturnCode"))
                {
                    jres["ReturnCode"] = "0";
                    jres["ReturnDesc"] = "SUCCESS";
                    jres["ErrorInfo"] = "SUCCESS";
                }
                else
                {
                    jres.Add("ReturnCode", "0");
                    jres.Add("ReturnDesc", "SUCCESS");
                    jres.Add("ErrorInfo", "SUCCESS");
                }
                returnMessage = jres.ToString();
            }
            catch (Exception ex)
            {
                Log.Logger("", TerminalID, Log._App_iconnectApi, "iConnectAPI.cs", "EndTransaction", Log._Exception, ex.StackTrace);
            }
            return returnMessage;
        }

        public string GetCustomerDetailsNew(String InputJson, String TransactionID, String TerminalID, String ServerName)
        {
            Log.Logger(TransactionID, TerminalID, Log._App_Mbank_FAB, "DepositPosting", "GetCustomerDetailsNew", Log._Log, "In GetCustomerDetailsNew Message");
            String RequestMessage = "";
            String ReturnMessage = "";
            String ReturnCode = "";
            String ReturnDesc = "";
            String ErrorInfo = "";
            JObject jreq = JObject.Parse(InputJson);
            String CardNumber = jreq["CardNumber"].ToString();
            String CIFNumber = jreq["CIFNumber"].ToString();
            String channelName = jreq["ChannelId"].ToString();
            String MessageName = jreq["MessageName"].ToString();
            String pageNum = jreq["pageNum"].ToString();
            String pageSize = jreq["pageSize"].ToString();
            String MobileNumber = "";
            String EmailId = "";

            String Language = "En";
            JObject response = jreq;
            g objg = new g();
            DBConSql dbcon = new DBConSql();
            try
            {

                String strURL = ConfigurationManager.AppSettings["TCSCustomerDetailsURL"].ToString();

                //strURL = HttpUtility.UrlPathEncode(strURL + "crmdynamics/contacts?$select=firstname,statecode,lastname,createdon,git_cifnumber,mobilephone,emailaddress1,fullname,governmentid,gits_tpin,gits_tpinstatus,git_nationalidexpirydate&$filter=git_cifnumber eq '" + CIFNumber+ "'&$expand=gits_defaultsegment($select=gits_name,gits_segmentid),msfsi_contact_msfsi_financialproduct_Customer($select=createdon)");
                //strURL = HttpUtility.UrlPathEncode(strURL + "crmdynamics/contacts?$select=firstname,statecode,lastname,createdon,git_cifnumber,mobilephone,emailaddress1,fullname,governmentid,gits_tpin,gits_tpinstatus,msfsi_idexpirydate,git_nationalidexpirydate&$filter=git_cifnumber eq '" + CIFNumber + "'&$expand=gits_defaultsegment($select=gits_name,gits_segmentid),msfsi_contact_msfsi_financialproduct_Customer($select=createdon)");
                strURL = HttpUtility.UrlPathEncode(strURL + @"?pageNum="+pageNum+"&pageSize="+pageSize+"&CustomerID="+CIFNumber);

                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>("");
                var Queryparameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(RequestMessage);

                HttpClient httpClient = new HttpClient();

                if (SIMULATOR == "TRUE")
                {
                    ReturnMessage = "{\"@odata.context\":\"https://almaryah-crm-uat.crm15.dynamics.com/api/data/v9.2/$metadata#contacts(firstname,lastname,git_cifnumber,mobilephone,emailaddress1,governmentid,gits_tpin,gits_tpinstatus)/$entity\",\"@odata.etag\":\"W/26257009\",\"firstname\":\"AJAY\",\"lastname\":\"KUMAR\",\"git_cifnumber\":\"500600700\",\"mobilephone\":\"2695\",\"emailaddress1\":\"valkya27+41@gmail.com\",\"governmentid\":\"784198082516257\",\"gits_tpin\":null,\"gits_tpinstatus\":null,\"contactid\":\"9ce0e123-f3e9-eb11-bacb-0022480cf5aa\"}";
                }
                else
                {
                    string token = dbcon.CheckTokenExpiry(TransactionID, TerminalID);
                    ReturnMessage = httpClient.RestClientCall(strURL, "GET", "", MessageName, token, parameters, Queryparameters, "");
                }
                //
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectAPI", "GetCustomerDetails", Log._Log, "In get GetCustomerDetails Message details" + ReturnMessage);

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(ReturnMessage);

                // Convert the XmlDocument to JSON string
                string jsonString = JsonConvert.SerializeXmlNode(xmlDoc);
                JObject jRoot = JObject.Parse(jsonString);
                ReturnCode = "000";
                ReturnDesc = "Success";
                response["MsgFormat"] = "RESPONSE";
                if (ReturnCode == "000")
                {
                    ReturnCode = "0";
                    ReturnDesc = "SUCCESS";
                    //CustomerType,CustomerName_En,CustomerName_Ar,MobileNumber,CustomerStatus,dateOfBirth

                    string CustomerName_En = jRoot["getBPDetailsResponse"]["customerDetails"]["customerName"].ToString();
                    response.Add("CustomerName_En", CustomerName_En);

                    MobileNumber = jRoot["getBPDetailsResponse"]["contactDetails"]["nationalNumber"].ToString();
                    EmailId = jRoot["getBPDetailsResponse"]["contactDetails"]["primaryEmailId"].ToString();
                    response.Add("MobileNumber", MobileNumber);
                    response.Add("EmailAddress", EmailId);
                    var EIDAExpiryDate = "";
                    //if (channelName.Contains("MBANK_CORPORATE"))
                    //{
                    //    response.Add("EmiratesID", "1234567890");

                    //    EIDAExpiryDate = new DateTime().ToString("yyyyMMdd");
                    //    response.Add("EmiratesExpiryDate", EIDAExpiryDate);
                    //}
                    //else
                    //{
                    //    response.Add("EmiratesID", jRoot["getBPDetailsResponse"]["identificationDetails"]["identificationDetails"]["identifierValue"].ToString());

                    //    EIDAExpiryDate = jRoot["getBPDetailsResponse"]["identificationDetails"]["identificationDetails"]["validTill"].ToString();
                    //    response.Add("EmiratesExpiryDate", EIDAExpiryDate);
                    //}
                    response.Add("EmiratesID", "1234567890");

                    EIDAExpiryDate = new DateTime().ToString("yyyyMMdd");
                    response.Add("EmiratesExpiryDate", EIDAExpiryDate);
                    
                    
                    response.Add("FullName", jRoot["getBPDetailsResponse"]["customerDetails"]["customerName"].ToString());
                    string segmentvalue = "";
                    //if (jRoot["value"][0]["gits_defaultsegment"] != null && jRoot["value"][0]["gits_defaultsegment"]["gits_segmentid"] != null)
                    //{
                        response.Add("Segment", jRoot["getBPDetailsResponse"]["customerDetails"]["customerSegment"].ToString());
                        segmentvalue = jRoot["getBPDetailsResponse"]["customerDetails"]["customerSegment"].ToString();
                   // }
                    //else
                    //{
                    //    response.Add("Segment", "");
                    //    segmentvalue = "0";
                    //}

                    String DOB = "";//objg.ParseXML(ReturnMessage, "dateOfBirth");
                    String Age = "0";
                    DateTime dtDateTime = DateTime.Now.Date;
                    try
                    {
                        if (DOB != "")
                        {
                            dtDateTime = Convert.ToDateTime(DOB);
                            Age = ((System.DateTime.Now.Subtract(dtDateTime)).Days / 365).ToString();
                            response.Add("CustomerAge", Age);
                        }
                        else
                        {
                            response.Add("CustomerAge", "22");
                        }

                        //String EIDAExpiryDate = jRoot["getBPDetailsResponse"]["identificationDetails"]["identificationDetails"]["validTill"].ToString();
                        try
                        {
                            if (EIDAExpiryDate == null || EIDAExpiryDate == "")
                                EIDAExpiryDate = "2023-01-01";
                        }
                        catch (Exception ex) { }
                        dtDateTime= DateTime.ParseExact(EIDAExpiryDate, "yyyyMMdd", CultureInfo.CurrentCulture);
                        //dtDateTime = Convert.ToDateTime(EIDAExpiryDate);
                        String _EidExpiryindays = ((dtDateTime.Subtract(System.DateTime.Now)).Days).ToString();

                        double _age = Convert.ToDouble(_EidExpiryindays);
                        String EXPIRY_DAYS = dbcon.GetUtilityConfigValue("EMIRATESID_UPDATE", "EXPIRY_DAYS");
                        if (_age < -90)
                        {
                            response.Add("EIDAExpirycheck", "UPDATEONLY");
                            response.Add("ShowEIDAPopUp", "YES");
                        }
                        else if (_age >= -90)
                        {
                            response.Add("EIDAExpirycheck", "YES");
                            if (_age >= Convert.ToDouble(EXPIRY_DAYS))
                            {
                                response.Add("ShowEIDAPopUp", "NO");
                            }
                            else
                            {
                                response.Add("ShowEIDAPopUp", "YES");
                            }
                        }

                        string stmntcharges = "0";
                        DataTable dtcharges = dbcon.GetCharges(CIFNumber, segmentvalue);
                        if (dtcharges != null && dtcharges.Rows.Count > 0)
                        {
                            stmntcharges = dtcharges.Rows[0]["chargeamount"].ToString();

                        }
                        response.Add("StmtCharges", stmntcharges);
                    }
                    catch (Exception ex) { }
                }
            }
            catch (Exception ex)
            {
                ReturnCode = "IDP9002";
                ReturnDesc = "Customer Details Not Found Try Again or Contact Customer Care";
                Log.Logger(TransactionID, TerminalID, Log._App_iconnectApi, "iConnectAPI", "GetCustomerDetails", Log._Exception, ex.Message+" "+ ex.StackTrace);
            }
            //string strUpdate = " Update TxnMessageQueue set CIFNUMBER= '" + CIFNumber + "',EmailId= '" + EmailId + "',MobileNumber = '" + MobileNumber + "' , CARDNUMBER= '" + objg.EncryptCardData(CardNumber) + "', MessageName = 'GetCustomerDetails' Where Transaction_ID = '" + TransactionID + "' ";
            //dbcon.UpdateTxnQue(strUpdate, TransactionID);

            response.Add("ReturnCode", ReturnCode);
            response.Add("ReturnDesc", ReturnDesc);
            response.Add("ErrorInfo", ErrorInfo);
            dbcon.TxnMessageDetailsInsert(TransactionID, TerminalID, ServerName, ReturnCode, ReturnDesc, "ICONNECT", "GetCustomerDetails", RequestMessage, ReturnMessage, 2);
            return response.ToString();
        }
    }
}