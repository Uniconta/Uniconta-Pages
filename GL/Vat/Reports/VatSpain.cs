using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports
{
    static public class VatSpain
    {
        static public double[] calc(VatReportLine[] arr)
        {
            double[] Model303 = new double[100];

            int l = arr.Length;
            for (int i = 0; (i < l); i++)
            {
                var v = arr[i];
                if (v.Order != 0 || v.VatOpr == null)
                    continue;

                int model = v.VatOpr._ModelBoxId;
                if (model != 0)
                {
                    if (v.AccountIsVat == 1 || (v._VatOperation >= 3 && v._VatOperation <= 9)) // v3 .. v9)
                    {
                        double sign = 1d;
                        if (v._VatOperation == 1) // v1
                        {
                            model = v.Vat._ModelBoxId1;
                            if (model == 0)
                                continue;
                            sign = -1d;
                        }
                        var idx = (model - 1) * 3;
                        if (v.AccountIsVat == 1)
                        {
                            if (v.IsOffsetAccount == 0)
                            {
                                Model303[idx] += v._BaseVAT * sign;
                                Model303[idx + 1] = v.Vat._Rate;
                                Model303[idx + 2] += v.AmountWithVat * sign;
                            }
                        }
                        else
                            Model303[idx] -= v.AmountWithVat;
                    }
                    else if (v.Account._AccountType == (byte)GLAccountTypes.FixedAssets ||
                             v.Account._SystemAccount == (byte)SystemAccountTypes.ExpenseFixedAssets) // c1, c3, c6
                    {
                        if (v._VatOperation == 101) // c1
                            model = 10;
                        else if (v._VatOperation == 103) // c3
                            model = 14;
                        else if (v._VatOperation == 106) // c6
                            model = 12;
                        else
                            continue;

                        var idx = (model - 1 + 1) * 3;  // one line more
                        Model303[idx] += v.AmountWithVat;
                        Model303[idx + 1] = v.Vat._Rate;
                        Model303[idx + 2] += v._PostedVAT;
                    }
                }
            }

            // c4 needs to updates from line 5 goes into line 10 as well
            Model303[(10 - 1) * 3]     += Model303[(5 - 1) * 3];
            Model303[(10 - 1) * 3 + 2] += Model303[(5 - 1) * 3 + 2];

            // c3,c8 needs to updates from line 4 goes into line 14 as well
            Model303[(14 - 1) * 3]     += Model303[(4 - 1) * 3];
            Model303[(14 - 1) * 3 + 2] += Model303[(4 - 1) * 3 + 2];

            // remove bienes inversiones
            Model303[(10 - 1) * 3]     -= Model303[(11 - 1) * 3];
            Model303[(10 - 1) * 3 + 2] -= Model303[(11 - 1) * 3 + 2];
            // remove bienes inversiones
            Model303[(12 - 1) * 3]     -= Model303[(13 - 1) * 3];
            Model303[(12 - 1) * 3 + 2] -= Model303[(13 - 1) * 3 + 2];
            // remove bienes inversiones
            Model303[(14 - 1) * 3]     -= Model303[(15 - 1) * 3];
            Model303[(14 - 1) * 3 + 2] -= Model303[(15 - 1) * 3 + 2];

            // sum line 9.
            for (int i = 1; (i <= 8); i++)
                Model303[(9 - 1) * 3 + 2] += Model303[(i - 1) * 3 + 2];

            // sum line 17.
            for (int i = 10; (i <= 16); i++)
                Model303[(17 - 1) * 3 + 2] += Model303[(i - 1) * 3 + 2];

            Model303[(18 - 1) * 3 + 2] = Model303[(9 - 1) * 3 + 2] - Model303[(17 - 1) * 3 + 2];

            Model303[(22 - 1) * 3 + 2] = Model303[(18 - 1) * 3 + 2];
            
            return Model303;
        }
    }
}
