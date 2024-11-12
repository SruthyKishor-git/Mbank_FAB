using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace mBank_FAB
{
    public class EnumValues
    {
        public enum transactionStatusEnum
        {
            [EnumMember(Value = "pending")]
            CRT,
            [EnumMember(Value = "expired")]
            EXP,
            [EnumMember(Value = "cancelled")]
            CNL,
           
            [EnumMember(Value = "completed")]
            COM,
            [EnumMember(Value = "partial")]
            PCD,
            [EnumMember(Value = "completed")]
            PRC,
        }
    }
}
//•	Pending
//•	Partial
//•	Completed 
//•	Cancelled 
//•	Expired
