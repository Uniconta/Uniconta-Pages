using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common.Utility;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.API.GeneralLedger;

#if WPF
using UnicontaClient.Pages;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
#else
namespace ImportVoucher.Utility
#endif
{
    public class ImportVoucher
    {
        PropValuePair[] Transfilter;
        PostingAPI postAPI;
        CrudAPI Capi;
        ImportLogVoucher _logs;
        int cnt;

        public ImportVoucher(CrudAPI capi, ImportLogVoucher logs)
        {
            this.Capi = capi;
            this._logs = logs;
            postAPI = new PostingAPI(capi);
            Transfilter = new PropValuePair[2];
        }

        public async Task Import(string Voucher, DateTime Date, FileextensionsTypes ext, string Text, Unistream Data)
        {
            try
            {
                ErrorCodes err;

                Transfilter[0] = PropValuePair.GenereteWhereElements(nameof(GLTransClient.Date), Date, CompareOperator.Equal);
                Transfilter[1] = PropValuePair.GenereteWhereElements(nameof(GLTransClient.Voucher), typeof(string), Voucher);
                var transList = await Capi.Query<GLTrans>(Transfilter);
                if (transList != null && transList.Length > 0)
                {
                    var tran = transList[0];
                    if (tran._DocumentRef != 0)
                    {
                        Transfilter[0] = PropValuePair.GenereteWhereElements("RowId", typeof(int), NumberConvert.ToString(tran._DocumentRef));
                        Transfilter[1] = null;
                        var oldDoc = await Capi.Query<Document>(Transfilter);
                        if (oldDoc != null && oldDoc.Length > 0)
                        {
                            var old = oldDoc[0];
                            if (old._Fileextension == ext && old._PostingDate == Date)
                                return; // already imported
                        }
                    }

                    var vc = new Document
                    {
                        _Data = Data.ToArray(),
                        _Text = Text,
                        _Fileextension = ext,
                        _Content = ContentTypes.Invoice,
                        _Voucher = (int)NumberConvert.ToInt(Voucher),
                        _PostingDate = Date
                    };
                    err = await Capi.Insert(vc);
                    if (err != ErrorCodes.Succes)
                    {
                        _logs.AppendLogLine("Insert error: " + err.ToString());
                        return;
                    }
                    err = await postAPI.AddPhysicalVoucher(tran, vc, true, true);
                    if (err == ErrorCodes.Succes)
                        _logs.WriteMsg = "Saved: " + NumberConvert.ToString(++cnt);
                    else
                        _logs.AppendLogLine("Attach error: " + err.ToString());
                }
            }
            catch (Exception ex)
            {
                _logs.AppendLog("Exception");
                _logs.AppendLogLine(ex.Message);
            }
        }
    }
}
