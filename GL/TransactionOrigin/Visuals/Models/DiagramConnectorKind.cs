using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public enum DiagramConnectorKind
    {
        VoucherToTransaction = 0,
        TransactionToSection = 1,
        SectionToReason = 2
    }
}
