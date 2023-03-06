using Uniconta.Script;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.Common.Utility;
using DevExpress.Xpf.Grid;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class BalanceReport
    {
        async Task<List<BalanceClient>> GenerateTemplateGrid(int Cols)
        {
            GLReportTemplate template;
            var templateCache = api.GetCache(typeof(GLReportTemplate));
            if (templateCache != null)
                template = (GLReportTemplate)templateCache.Get(AppliedTemplate);
            else
            {
                template = new GLReportTemplate() { _Name = AppliedTemplate };
                await api.Read(template);
            }
            if (template == null || template.RowId == 0)
                return null;

            var reportline = await api.Query<GLReportLine>(template);
            var items = new TemplateDataContext();
            var TemplateReportlist = items.TemplateReportlist;
            TemplateReportlist.Capacity = reportline.Length;

            var newBalance = new List<BalanceClient>(reportline.Length);

            var SumContext = new TemplateSumContext(Cols);
            var colCount = PassedCriteria.selectedCriteria.Count;
            bool AnyHidden = false;
            int i, j;
            for (j = 0; (j < reportline.Length); j++)
            {
                var line = reportline[j];
                var amounts = new long[colCount];
                if (line._Accounts != null && !line._ExpressionSum)
                {
                    var SumAccounts = PropValuePair.GenereteWhereElements("Account", typeof(string), line._Accounts);
                    foreach (var balSum in balanceClient)
                    {
                        if (balSum.AccountTypeEnum > GLAccountTypes.CalculationExpression && AccountSum.IsIncluded(SumAccounts, balSum.AccountNo))
                            balSum.SumUpAmount(amounts);
                    }
                    if (Skip0Account)
                    {
                        bool found = false;
                        for (i = 0; (i < Cols); i++)
                            if (amounts[i] != 0)
                            {
                                found = true;
                                break;
                            }
                        if (!found)
                            continue;
                    }
                }
                if (line._InvertSign)
                {
                    for (i = 0; (i < Cols); i++)
                        amounts[i] = -amounts[i];
                }
                if (line._SaveTotal != 0)
                    SumContext.CalcMethod.AddSum(line._SaveTotal, amounts);
                if (line._Hide)
                    AnyHidden = true;

                var newBalanceCol = new BalanceClient(amounts);
                newBalanceCol.Acc._Name = line._Text;
                newBalance.Add(newBalanceCol);
                TemplateReportlist.Add(new TemplateDataItems(newBalanceCol, hdrData, line) { Masterfontsize = template._FontSize });
            }

            // Now we will take all expressions and update.
            var pars = new parser(SumContext);

            for (j = 0; (j < TemplateReportlist.Count); j++)
            {
                var item = TemplateReportlist[j];
                var line = item.line;
                if (line._ExpressionSum)
                {
                    var InvertSign = line._InvertSign;
                    var e = pars.parse(line._Accounts, Uniconta.Script.ValueType.Double);
                    if (e != null)
                    {
                        var amounts = item.blc.amount;
                        for (i = 0; (i < Cols); i++)
                        {
                            SumContext.CurIndex = i;
                            var val = NumberConvert.ToLong(e.Value());
                            amounts[i] = !InvertSign ? val : -val;
                        }
                        if (line._SaveTotal != 0)
                            SumContext.CalcMethod.AddSum(line._SaveTotal, amounts);
                    }
                }
            }

            if (AnyHidden)
            {
                for (i = TemplateReportlist.Count; (--i >= 0);)
                {
                    if (TemplateReportlist[i].line._Hide)
                        TemplateReportlist.RemoveAt(i);
                }
            }

            AccountName.Visible = AccountNo.Visible = false;
            Text.Visible = true;
            ((TableView)dgBalanceReport.View).RowStyle = Application.Current.Resources["TemplateRowStyle"] as Style;
            dgBalanceReport.ItemsSource = TemplateReportlist;
            templateReportData = new object[] { items, hdrData, PassedCriteria.ObjBalance, null };
            return newBalance;
        }
    }

    public class TemplateSumContext : ExpessionContext
    {
        public readonly SumMethod CalcMethod;
        public TemplateSumContext(int Cols)
        {
            var methods = new UserMethod[1];
            this.MethodList = methods;
            CalcMethod = new SumMethod(this, Cols);
            methods[0] = CalcMethod;
        }
    }
}
