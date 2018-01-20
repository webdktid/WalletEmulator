using System;
using System.Collections.Generic;
using System.Windows.Forms;
using WalletEmulator.Model;
using WalletEmulator.Tools;

namespace WalletEmulator
{
    public partial class AddTransactionForm : Form
    {
        private readonly List<Transaction> _transactions;

        public AddTransactionForm(List<Transaction> transactions)
        {
            InitializeComponent();
            _transactions = transactions;
        }

        private void buttonFind_Click(object sender, EventArgs e)
        {


            var t = new Transaction
            {
                address = textBoxAddress.Text,
                amount = Convert.ToDouble(textBoxValue.Text.Replace(",", ".")),
                confirmations = Convert.ToInt32(textBoxConfirmations.Text),
                time = (int) UnixTime.DateTimeToUnixTimestamp(DateTime.Now),
                account = "",
            };
            t.category = t.amount>0 ? "RECEIVE" : "SEND";

            _transactions.Add(t);

            Close();
        }
    }
}