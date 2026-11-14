using Duende.IdentityModel.OidcClient.Browser;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Controls.Dialogs;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.Norway.Helpers
{
    /// <summary>
    /// IBrowser for Duende OidcClient that receives the ID-porten authorization code on a loopback HTTP listener (RFC 8252 section 7.3).
    /// The login page opens in the user's system browser, as ID-porten requires for native apps (no embedded web view).
    /// If no system browser can be started, Uniconta's embedded browser dialog is used instead.
    /// The ID-porten client is registered with the fixed redirect URI NorwayVATReportConsts.RedirectUrl, which must match exactly
    /// </summary>
    public class NorwayVATLoopbackBrowser : IBrowser
    {
        /// <summary>
        /// Open the login in the system browser. When false, or when the system browser cannot be started, the embedded dialog is used
        /// </summary>
        public bool UseSystemBrowser { get; set; } = true;

        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            using (var http = new HttpListener())
            {
                //Listen on the whole port: HttpListener prefixes must end with "/" and ID-porten redirects to .../token?code=...
                var endUri = new Uri(options.EndUrl);
                http.Prefixes.Add($"{endUri.Scheme}://{endUri.Host}:{endUri.Port}/");

                try
                {
                    http.Start();
                }
                catch (HttpListenerException ex)
                {
                    //Port already taken: another login on this machine, or another program on the port. The port is fixed because
                    //it is part of the redirect URI registered on the ID-porten clients
                    return new BrowserResult
                    {
                        ResultType = BrowserResultType.UnknownError,
                        Error = string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATLoopbackPortBusy"), endUri.Port, ex.Message)
                    };
                }

                var dialog = UseSystemBrowser && TryOpenSystemBrowser(options.StartUrl) ? 
                             CreateWaitingDialog() : 
                             new CWBrowserDialog(options.StartUrl, Uniconta.ClientTools.Localization.lookup("WebLogin"));

                //Closing the dialog (Escape, X, OK on the waiting dialog) stops the listener, which makes GetContextAsync throw
                dialog.Closed += (sender, args) =>
                {
                    if (http.IsListening)
                        http.Stop();
                };

                //OidcClient passes no cancellation token, so the browser enforces BrowserOptions.Timeout itself.
                //Without it a closed browser tab would keep the listener and the login lock forever
                using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    timeout.CancelAfter(options.Timeout > TimeSpan.Zero ? options.Timeout : TimeSpan.FromMinutes(5));

                    using (timeout.Token.Register(() =>
                    {
                        if (http.IsListening)
                            http.Stop();

                        //Timeout: close the dialog too, otherwise it stays open with nothing listening
                        dialog.Dispatcher.BeginInvoke((Action)(() => dialog.SetDialogResult(false)));
                    }))
                    {
                        var contextTask = http.GetContextAsync();
                        dialog.Dispatcher.BeginInvoke((Action)(() => dialog.Show()));

                        HttpListenerContext context;
                        while (true)
                        {
                            try
                            {
                                context = await contextTask;
                            }
                            catch (Exception ex) when (ex is HttpListenerException || ex is ObjectDisposedException || ex is InvalidOperationException)
                            {
                                return new BrowserResult { ResultType = timeout.IsCancellationRequested ? BrowserResultType.Timeout : BrowserResultType.UserCancel };
                            }

                            //Only the redirect from ID-porten ends the login. Any other request to the port (another page, a favicon) is ignored
                            if (IsRedirect(context.Request, endUri))
                                break;

                            try
                            {
                                context.Response.StatusCode = 404;
                                context.Response.Close();
                            }
                            catch
                            {
                            }

                            contextTask = http.GetContextAsync();
                        }

                        //The full redirect URL with code and state. OidcClient validates state and exchanges the code
                        var response = context.Request.Url.ToString();

                        try
                        {
                            var body = Encoding.UTF8.GetBytes(CompletedPage());
                            context.Response.ContentType = "text/html; charset=utf-8";
                            context.Response.ContentLength64 = body.Length;
                            context.Response.OutputStream.Write(body, 0, body.Length);
                            context.Response.Close();
                        }
                        catch
                        {
                            //The page is cosmetic; the code is already captured
                        }

                        dialog.Dispatcher.Invoke(() =>
                        {
                            dialog.SetDialogResult(true);

                            //Back from the browser: bring Uniconta to the front again
                            System.Windows.Application.Current?.MainWindow?.Activate();
                        });

                        return new BrowserResult
                        {
                            ResultType = BrowserResultType.Success,
                            Response = response
                        };
                    }
                }
            }
        }

        /// <summary>
        /// The request is ID-porten's redirect: the registered path with state and either a code or an error
        /// </summary>
        private static bool IsRedirect(HttpListenerRequest request, Uri endUri)
        {
            var query = request.QueryString;
            return String.Equals(request.Url.AbsolutePath.TrimEnd('/'), endUri.AbsolutePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase) &&
                   !String.IsNullOrEmpty(query["state"]) &&
                   (!String.IsNullOrEmpty(query["code"]) || !String.IsNullOrEmpty(query["error"]));
        }

        /// <summary>
        /// Opens the URL in the default browser. False when no browser could be started
        /// </summary>
        private static bool TryOpenSystemBrowser(string url)
        {
            try
            {
                using (Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }))
                {
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Small dialog shown while the user logs in in the browser. Closing it cancels the login
        /// </summary>
        private static ChildWindow CreateWaitingDialog()
        {
            var text = new TextBlock()
            {
                Text = Uniconta.ClientTools.Localization.lookup("NorwayVATWaitingForBrowserLogin"),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400,
                Margin = new Thickness(10)
            };

            return new CWDynamicControl(text, Uniconta.ClientTools.Localization.lookup("WebLogin"));
        }

        private static string CompletedPage()
        {
            return "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Uniconta</title></head>" +
                   "<body style=\"font-family:sans-serif\"><p>" +
                   WebUtility.HtmlEncode(Uniconta.ClientTools.Localization.lookup("LoginCompletedCloseWindow")) +
                   "</p></body></html>";
        }
    }
}
