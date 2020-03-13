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
            TemplateDataContext items = new TemplateDataContext();
            List<BalanceClient> newBalance = new List<BalanceClient>();

            TemplateSumContext SumContext = new TemplateSumContext(Cols);
            var colCount = PassedCriteria.selectedCriteria.Count;
            foreach (var line in reportline)
            {
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
                        for (int i = Cols; (--i >= 0); )
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
                    for (int i = Cols; (--i >= 0); )
                        amounts[i] = -amounts[i];
                }
                if (line._SaveTotal != 0)
                    SumContext.CalcMethod.AddSum(line._SaveTotal, amounts);

                var newBalanceCol = new BalanceClient(amounts);
                newBalance.Add(newBalanceCol);
                hdrData.TextSize = template._TextSize == 0 ? 70 * SizeFactor : (template._TextSize * SizeFactor);
                hdrData.AmountSize = template._AmountSize == 0 ? 20 * SizeFactor : (template._AmountSize * SizeFactor);
                TemplateDataItems item = new TemplateDataItems(newBalanceCol, hdrData, line);
                item.Masterfontsize = template._FontSize;
                items.TemplateReportlist.Add(item);
            }

            // Now we will take all expressions and update.
            var pars = new parser(SumContext);

            foreach (TemplateDataItems item in items.TemplateReportlist)
            {
                var line = item.line;
                if (line._ExpressionSum)
                {
                    var InvertSign = line._InvertSign;
                    var e = pars.parse(line._Accounts, Uniconta.Script.ValueType.Double);
                    if (e != null)
                    {
                        var amounts = item.blc.amount;
                        for (int i = Cols; (--i >= 0); )
                        {
                            SumContext.CurIndex = i;
                            var val = NumberConvert.ToLong(e.Value());
                            if (InvertSign)
                                val = -val;
                            amounts[i] = val;
                        }
                        if (line._SaveTotal != 0)
                            SumContext.CalcMethod.AddSum(line._SaveTotal, amounts);
                    }
                }
            }

            AccountName.Visible = AccountNo.Visible = false;
            Text.Visible = true;
            dgBalanceReport.ItemsSource = items.TemplateReportlist;
            templateReportData = new object[4];
            templateReportData[0] = items;
            templateReportData[1] = hdrData;
            templateReportData[2] = PassedCriteria.ObjBalance;
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
