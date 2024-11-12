using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace mBank_FAB
{
    public class APIConfig
    {
        public static string FABKONGUrl= ConfigurationManager.AppSettings["FABKONGUrl"].ToString();
        public string accessTokenUrl = ConfigurationManager.AppSettings["FABIDPUrl"].ToString();
        public static string walletURL = ConfigurationManager.AppSettings["MiddlewareURL"].ToString();
        //mention the FAB api url
        public string createDepositSlipUrl= FABKONGUrl+ "createdepositslip";
        public string updatedepositstatusUrl = FABKONGUrl + "deposits/statusupdate";
        public string statusinquiryUrl = FABKONGUrl + "deposits/statusinquiry";
        public string fetchChequeImageUrl = "https://api-proxy.mbankuae.local:8443/accountdeposit/plgn/mbank/fetchChequeImage";
        public string validateWalletUrl = walletURL + "wallet/ext/third-party/validate";
        public string cashDepositWalletUrl = walletURL + "wallet/ext/third-party/credit";



    }
}