using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.Models
{
    class BoxNumberAttribute : Attribute
    {
        private int boxNumber;
        public BoxNumberAttribute(int boxNo)
        {
            boxNumber = boxNo;
        }
        public int BoxNumber { get { return boxNumber; } }
    }
}
