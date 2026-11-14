using System;
using System.Collections.Generic;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    [Serializable]
    public class SIEDimension
    {
        public string Number { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// True for the dimensions reserved by the standard that were added by default and not declared in the file.
        /// </summary>
        public bool IsDefault = false;

        /// <summary>
        /// True when at least one object of the dimension is referenced in a balance or transaction item.
        /// </summary>
        public bool IsUsed = false;

        private SIEDimension _parent = null;
        public SIEDimension SuperDim
        {
            get
            {
                return _parent;
            }
            set
            {
                if (_parent != null) _parent.SubDim.Remove(this);
                _parent = value;
                if (_parent != null) _parent.SubDim.Add(this);
            }
        }

        public HashSet<SIEDimension> SubDim = new HashSet<SIEDimension>();
        public Dictionary<string, SIEObject> Objects = new Dictionary<string, SIEObject>();

        public bool IsSubDimension
        {
            get { return _parent != null; }
        }
    }
}
