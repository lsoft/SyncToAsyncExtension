using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Threading;
using SyncToAsync.Extension.Helper;
using SyncToAsync.Shared.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SyncToAsync.Extension
{
    public class CodeLenseUserControlViewModel : BaseViewModel
    {
        private readonly SiblingInformationContainer _sic;

        public Visibility ShowStrictComplianceMessage
        {
            get;
            set;
        }

        public string StrictComplianceMessage
        {
            get;
            set;
        }

        public string MethodBody
        {
            get;
            set;
        }

        public CodeLenseUserControlViewModel(
            SiblingInformationContainer sic
            )
        {
            _sic = sic;

            if (sic.Sibling == null)
            {
                ShowStrictComplianceMessage = Visibility.Collapsed;
                StrictComplianceMessage = string.Empty;
                MethodBody = string.Empty;
                return;
            }

            ShowStrictComplianceMessage = sic.Sibling.IsStrictCompliance
                ? Visibility.Collapsed
                : Visibility.Visible
                ;
            StrictComplianceMessage = sic.Sibling.IsStrictCompliance
                ? string.Empty
                : "We are not sure this method is a real sibling due to method name discrepancies." + Environment.NewLine + "We usually expect that sync<->async methods will have names likes MyMethod<->MyMethodAsync."
                ;
            MethodBody = sic.Sibling.MethodBody;
        }
    }
}
