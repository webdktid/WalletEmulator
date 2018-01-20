using System;
using System.Net;
using System.Text;
using System.Threading;

namespace WalletEmulator
{
    public delegate void WebserverChange(object sender, EventArgs e, string msg);


    public class MiniWebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;


        public event WebserverChange WebserverChange;

        protected virtual void OnStatusChange(EventArgs e, string msg)
        {
            WebserverChange?.Invoke(this, e, msg);
        }


        public MiniWebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");

            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");

            foreach (string s in prefixes)
            {
                if (!s.EndsWith("/"))
                {
                    _listener.Prefixes.Add(s + "/");
                }else
                    _listener.Prefixes.Add(s);
            }

            _responderMethod = method;
            _listener.Start();
        }

        public MiniWebServer(Func<HttpListenerRequest, string> method,params string[] prefixes)
            : this(prefixes, method)
        {
        }


        public void Run()
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                //Thread.Sleep(1000);

                                string rstr = _responderMethod(ctx.Request);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                         
                            }
                            catch (Exception ex)
                            {
                                ctx.Response.StatusCode = 500;
                                OnStatusChange(EventArgs.Empty, "Error " + ex.Message);
                            } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch (Exception ex)
                {
                    OnStatusChange(EventArgs.Empty, "Error " + ex.Message);
                } 
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}
