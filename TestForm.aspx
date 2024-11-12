<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TestForm.aspx.cs" Inherits="mBank_FAB.TestForm" Async="true"%>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <table class="auto-style1">
            <tr>
                <td id="tdrefnum" runat="server" visible="false">Generate Reference Number :</td>
                <td>
          
                </td>
            </tr>
              </table>
            <br />

         <asp:Label runat="server" visible="false" ID="lblaccnum">AccountNumber: </asp:Label>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <asp:TextBox ID="txtAcNum" runat="server" visible="false"  TextMode="SingleLine" ></asp:TextBox><br /><br />
        <asp:Label runat="server" visible="false" ID="lblAmount">Amount: </asp:Label>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <asp:TextBox ID="txtAmount" runat="server" visible="false" TextMode="SingleLine" ></asp:TextBox><br /><br />
            <asp:Label runat="server" visible="false" ID="lblService">ServiceType: </asp:Label>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <asp:DropDownList ID="dpdServiceType" runat="server" visible="false">
                <asp:ListItem Text="Cash" Value="CASH_ACCOUNT" ></asp:ListItem>
                <asp:ListItem Text="Cheque" Value="CHEQUE_ACCOUNT" ></asp:ListItem>
            </asp:DropDownList><br /><br />


        <asp:Button ID="btnSendrequest" runat="server" OnClick="btnSendrequest_Click" Text="Send Request" />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <asp:Button ID="btnCancelrequest" runat="server" OnClick="btnCancelrequest_Click" Text="Cancel Request" /><br /><br />
            <asp:Button ID="btnToken" runat="server" OnClick="btnToken_Click" Text="Generate token" /><br /><br />
            <asp:Label ID="ResultLabel" runat="server" Width="200px" Visible="false"></asp:Label><br /><br />
            <h2 is="hcancel" runat="server" visible="false"> Cancel request</h2><br />
            <asp:Label ID="lblRefNum" runat="server" Width="200px" Visible="false">Reference# </asp:Label><br /><br />
            <asp:TextBox ID="txtRefnum" runat="server" Visible="false" TextMode="SingleLine" ></asp:TextBox><br /><br />
             <asp:Label ID="lblAcnum" runat="server" Width="200px" Visible="false">Account# </asp:Label><br /><br />
            <asp:TextBox ID="txtAccnum" runat="server" Visible="false" TextMode="SingleLine" ></asp:TextBox><br /><br />
            <asp:Button ID="btnCancel" runat="server" Visible="false" OnClick="btnCancel_Click" Text="Cancel Request" /><br /><br />
            <h2 id="hresponse" runat="server" visible="false"> Response</h2><br />
            <asp:Label ID="refNum" runat="server" Width="200px" Visible="false"></asp:Label><br /><br />
            <asp:Label ID="expirydate" runat="server" Width="200px" Visible="false"></asp:Label><br /><br />
             <asp:Label ID="dailyLimit" runat="server" Width="200px" Visible="false"></asp:Label><br /><br />
            <asp:Label ID="mnthlyLimit" runat="server" Width="200px" Visible="false"></asp:Label><br /><br />
            <asp:Label ID="errmsg" runat="server" Width="200px" Visible="false" ForeColor="Red"> </asp:Label>
            <asp:Label ID="succmsg" runat="server" Width="200px" Visible="false" ForeColor="Green"> </asp:Label>
               
        </div>
    </form>
</body>
</html>
