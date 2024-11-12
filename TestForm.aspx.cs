using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using mBank_FAB.Controllers;
using mBank_FAB.Models;
using Newtonsoft.Json.Linq;

namespace mBank_FAB
{
    public partial class TestForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }
        protected async void btnSendrequest_Click(object sender, EventArgs e)
        {
            refNum.Text = refNum.Text + "";
            expirydate.Text = expirydate.Text + "";
            errmsg.Text = "";
            tdrefnum.Visible = true;
            lblaccnum.Visible = true;
            txtAcNum.Visible = true;
            lblAmount.Visible = true;
            txtAmount.Visible = true;
            lblService.Visible = true;
            dpdServiceType.Visible = true;
            if (txtAcNum.Text != "" || txtAmount.Text != "")
            {
                var AccountNumber = txtAcNum.Text;
                var Amount = txtAmount.Text;
                var ServiceType = dpdServiceType.SelectedValue;

                //dynamic inputParams = new System.Dynamic.ExpandoObject();
                //inputParams.AccountNumber = AccountNumber;
                //inputParams.TxnAmount = Amount;
                //inputParams.ServiceType = ServiceType;
                //inputParams.channel = "transview";
                customerDetailsRequest objReq = new customerDetailsRequest();
                objReq.AccountNumber = AccountNumber;
                objReq.TxnAmount = Amount;
                objReq.ServiceType = ServiceType;
                objReq.ChannelName = "transview";
                iConnectController iConnectController = new iConnectController();
                var response = await iConnectController.GetCustomerDetailsWithTxnLimit(objReq);
                if (response != null && response["status"].ToString() == "000")
                {
                    refNum.Text = "";
                    expirydate.Text = "";
                    refNum.Text = "Reference#  " + response["fabpayreference"].ToString();
                    expirydate.Text = "Expiry Date  " + response["epayExpiry"].ToString();
                    dailyLimit.Text = "Daily Available Limit " + response["DailyAvilabeLimit"].ToString();
                    mnthlyLimit.Text = "Monthly Availabe Limit " + response["MonthlyAvailabeLimit"].ToString();
                    refNum.Visible = true;
                    expirydate.Visible = true;
                    dailyLimit.Visible = true;
                    mnthlyLimit.Visible = true;
                }
                else
                {
                    errmsg.Text = response["errorDetails"].ToString();
                    errmsg.Visible = true;

                }
            }
            else
            {
                Response.Write("<script>alert('Please Provide Input')</script>");
            }
        }
        protected void btnCancelrequest_Click(object sender, EventArgs e)
        {
            tdrefnum.Visible = false;
            lblaccnum.Visible = false;
            txtAcNum.Visible = false;
            lblAmount.Visible = false;
            txtAmount.Visible = false;
            lblService.Visible = false;

            lblRefNum.Visible = true;
            txtRefnum.Visible = true;
            btnCancel.Visible = true;
            lblAcnum.Visible = true;
            txtAccnum.Visible = true;
            succmsg.Text = "";
            errmsg.Text = "";
        }
        protected async void btnCancel_Click(object sender, EventArgs e)
        {

            dynamic inputParams = new System.Dynamic.ExpandoObject();
            inputParams.AccountNumber = txtAccnum.Text;
            inputParams.fabPayReference = txtRefnum.Text;
            //inputParams.statuscode = "CNL";
            //inputParams.statusdesc = "cancel the deposit request";
            iConnectController iConnectController = new iConnectController();
            var response = await iConnectController.CancelDepositSlipRequest(inputParams);
            if (response.response != null && response.response.status == "000")
            {
                succmsg.Text = "Cancel request processed successfully";
                succmsg.Visible = true;
            }
                
            else
            {
                errmsg.Text = response.response.errorDetails.ToString();
                errmsg.Visible = true;
            }
                


        }

        protected async void btnToken_Click(object sender, EventArgs e)
        {
            CreateAndSignPrivateJwt jwt = new CreateAndSignPrivateJwt();
            var accesstoken = JObject.Parse(await jwt.GetAccessToken());
            var token= accesstoken["access_token"].ToString();
            ResultLabel.Visible = true;
            ResultLabel.Text = token;
        }
    }
}