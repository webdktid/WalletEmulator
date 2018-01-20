using System;
using System.Collections.Generic;

namespace WalletEmulator.Model
{
    public class Transaction
    {
        private static double DateTimeToUnixTimestamp(DateTime dateTime)
        {
            return (dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
        }


        public Transaction(string address, double amount, int confirmations, DateTime dateTime, string txid)
        {
            this.address = address;

            this.amount = amount;

            if (Math.Abs(amount) < 0.00000001)
                throw new Exception("amount is 0");


            category = amount < 0 ? "Send" : "Receive";

            this.confirmations = confirmations;
            time = (int)DateTimeToUnixTimestamp(dateTime);

            this.txid = txid;
        }
        public Transaction() { }

        public string address { get; set; }
        public string account { get; set; }
        public double amount { get; set; }
        public int confirmations { get; set; }
		public string category { get; set; }
		public int time { get; set; }
        public List<string> txids { get; set; }
        public string txid { get; set; }

     
    }
}


