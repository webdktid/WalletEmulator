using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using WalletEmulator.Model;
using WalletEmulator.Tools;

namespace WalletEmulator
{
    public  partial class FormMain : Form
    {
        private MiniWebServer _webserver;
        private List<Transaction> _transactions;
        private Dictionary<string, string> _currencies;
        private Dictionary<string, double> _feeDictionary;

        private void SetFee(string currency, double value)
        {
            if (!_feeDictionary.ContainsKey(currency))
            {
                _feeDictionary.Add(currency, value);
                return;
            }
            _feeDictionary[currency] = value;
        }

        private double GetFee(string currency)
        {
            if (_feeDictionary.ContainsKey(currency))
            {
                return _feeDictionary[currency];
            }
            return 0;
        }


        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            Init();
            _webserver.Run();
            AddInfo("Global", "Server started");
        }


        void AddInfo(string way, string txt)
        {
             listBoxInfo.Invoke(new Action(() =>
             {
                 listBoxInfo.Items.Add(way + " - " + txt);
                 listBoxInfo.TopIndex = listBoxInfo.Items.Count - 1;
             }));
        }


        void AddToListView(ListViewItem item)
        {
            listViewOverview.Invoke(new Action(() =>
            {
                listViewOverview.Items.Add(item);
            }));
        }

        void Init()
        {
   
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            _currencies = new Dictionary<string, string>();
            _feeDictionary = new Dictionary<string, double>();

            var appSettings = ConfigurationManager.AppSettings;

            SetStartValue();

            _webserver = new MiniWebServer(SendResponse, _currencies.Select(x => appSettings[x.Key]).ToArray());
            _webserver.WebserverChange += _webserver_WebserverChange;
        }


        private void SetStartValue()
        {

            _transactions = new List<Transaction>();
            var appSettings = ConfigurationManager.AppSettings;

            foreach (var key in appSettings.AllKeys)
            {
                _currencies.Add(key, appSettings[key]);
                AddInfo("Global", "listing to " + appSettings[key]);

                var t = new Transaction
                {
                    amount = 1000,
                    address = key + "-StartValue",
                    category = "RECEIVE",
                    confirmations = 11,
                    time = Convert.ToInt32(UnixTime.GetFromDateTime(DateTime.UtcNow.AddMinutes(-11))),
                    txid = key + "-" + Guid.NewGuid(),
                    account = "",

                };
                _transactions.Add(t);
            }
        }


        private void _webserver_WebserverChange(object sender, EventArgs e, string msg)
        {
            AddInfo("Webserver", msg);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        public string SendResponse(HttpListenerRequest request)
        {
            StreamReader stream = new StreamReader(request.InputStream);
            string codedInputstring= stream.ReadToEnd();
            string clearInputString  = HttpUtility.UrlDecode(codedInputstring);

            string name = _currencies.FirstOrDefault(x => x.Value == request.Url.OriginalString).Key;
            return GetResponse(name, clearInputString);
        }

        private string GetResponse(string shortname, string clearInputString)
        {
            ListViewItem item = new ListViewItem();
            var method = (string)JObject.Parse(clearInputString)["method"];

            item.Text = shortname;
            item.SubItems.Add(method);
            item.SubItems.Add(clearInputString);

            var joe = new JObject
            {
                ["jsonrpc"] = "1.0",
                ["id"] = "1",
                ["method"] = method
            };

            switch (method)
            {
                case "resettransactions":
                {
                    SetStartValue();
                    break;
                }
                case "getbalance":
                {
                    joe["result"] = GetBlanace(shortname);
                    break;
                }
                case "gettransaction":
                {
                        JToken token = JToken.Parse(clearInputString)["params"];
                        string aTxid = (string)token[0];
                        Transaction transaction = _transactions.FirstOrDefault(x => x.txid == aTxid);

                    if (transaction != null)
                    {
                        JObject jArrayTranasction = new JObject
                        {
                            {"address", transaction.address},
                            { "account", ""},
                            { "amount",  transaction.amount},
                            { "category",transaction.category},
                            { "confirmations",transaction.confirmations},
                            { "time", transaction.time},
                            { "txid", transaction.txid},
                            { "fee",GetFee(shortname)}
                        };

                        joe["result"] = jArrayTranasction;
                    }
                        else
                            joe["result"] = new JArray();
                        break;
                    }
                case "getinfo":
                {
                        string[,] array=
                        {
                            { "WalletEmulator","true" }, {"version","yes" }
                        };
                    joe["result"] = new JArray(array);
                        break;
                }
                case "getconnectioncount":
                {
                    joe["result"] = "8";
                    break;
                }
                case "getnewaddress":
                {
                    joe["result"] = shortname + "-" + Guid.NewGuid();
                    break;
                }

                case "getreceivedbyaddress":
                {
                    JToken token = JToken.Parse(clearInputString)["params"];
                    string aAddress = (string) token[0];
                    int aMinconf = (int) token[1];

                    double value = 0;
                    Transaction transaction = _transactions.FirstOrDefault(x => x.address == aAddress && x.confirmations >= aMinconf);
                    if (transaction != null)
                            value= transaction.amount;

                        joe["result"] = new JValue(value);
                        break;
                }

                case "validateaddress":
                {
                        JToken token = JToken.Parse(clearInputString)["params"];
                        string aAddress = (string)token[0];
                        bool valid = aAddress.Substring(0, shortname.Length).ToLower() == shortname.ToLower();
                        JToken validtoken = JObject.Parse("{ \"isvalid\" :\""+valid+"\"}");

                    joe["result"] = validtoken;

                        break;
                    }

                case "reveivefromaddress":
                    {
                        JToken token = JToken.Parse(clearInputString)["params"];
                        string aAddress = (string)token[0];
                        double amount = (double)token[1];

                        if (!aAddress.StartsWith(shortname))
                            aAddress = shortname + "**" + aAddress;

                        var t = new Transaction
                        {
                            amount = amount,
                            address = aAddress,
                            category = "RECEIVE",
                            confirmations = 10,
                            time = Convert.ToInt32(UnixTime.GetFromDateTime(DateTime.UtcNow.AddMinutes(-11))),
                            txid =  shortname +"-" + Guid.NewGuid(),
                            account = ""
                        };
                        _transactions.Add(t);
                        string txid = t.txid;
                        joe["result"] = new JValue(txid);

                        break;
                    }

                case "settxfee":
                {

                    JToken token = JToken.Parse(clearInputString)["params"];
                    double amount = (double)token[0];
                    SetFee(shortname, amount);
                    break;
                }

                case "sendtoaddress":
                {
                    JToken token = JToken.Parse(clearInputString)["params"];
                    string aAddress = (string) token[0];
                    double amount = (double) token[1];
                    //string string1 = (string) token[2];
                    //string string2 = (string) token[3];

                    if (!aAddress.StartsWith(shortname))
                        aAddress = shortname + "**" + aAddress;

                    var t = new Transaction
                    {
                        amount = amount,
                        address = aAddress,
                        category = "SEND",
                        confirmations = 10,
                        time = Convert.ToInt32(UnixTime.GetFromDateTime(DateTime.UtcNow.AddMinutes(-11))),
                        txid = shortname + "-" + Guid.NewGuid(),
                        account = ""
                    };
                    _transactions.Add(t);
                    string txid = t.txid;
                    joe["result"] = new JValue(txid); 

                    break;
                }
                case "listtransactions":
                {
                    List<Transaction> transactions = _transactions.Where(x => x.address.StartsWith(shortname)).ToList();
                    joe["result"] = JArray.FromObject(transactions);
                    break;
                }

                default:
                {
                     throw new Exception("unknown command " + method + " json " + clearInputString);
                }
            }
            string json = joe.ToString(Newtonsoft.Json.Formatting.None);

            item.SubItems.Add(json);
            AddToListView(item);
            return json;

        }


        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddInfo("Global","Server started");
            _webserver.Run();
        }

        private void endToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddInfo("Global","Server stoped");
            _webserver.Stop();
        }

        private void addTransActionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddTransactionForm form = new AddTransactionForm(_transactions);
            form.ShowDialog();

        }

        private void getBalanceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowInfo();
        }

        private void ShowInfo()
        {
            foreach (KeyValuePair<string, string> pair in _currencies)
            {
                AddInfo("Global", pair.Key + " balance: " + GetBlanace(pair.Key));
            }
        }


        double GetBlanace(string shortname)
        {
            var inserts = _transactions.Where(x => x.address.StartsWith(shortname) && x.category == "RECEIVE").Sum(x => x.amount);
            var payouts  = _transactions.Where(x => x.address.StartsWith(shortname) && x.category == "SEND").Sum(x => x.amount);

            return inserts - payouts;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listBoxInfo.Items.Clear();
            listViewOverview.Items.Clear();
        }

        private void listTransactionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Transaction transaction in _transactions)
            {
                AddInfo(transaction.address.Substring(0, 3),$"{transaction.category} {transaction.amount} {transaction.address}");
            }
        }
    }
}
