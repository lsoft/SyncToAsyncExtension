using SyncToAsync.Shared.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SyncToAsync.Extension
{
    /// <summary>
    /// Interaction logic for CodeLenseUserControl.xaml
    /// </summary>
    public partial class CodeLenseUserControl : UserControl
    {
        public CodeLenseUserControl()
        {
            InitializeComponent();
        }

        private async void Goto_OnMouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e
            )
        {
            try
            {
                var viewModel = (sender as FrameworkElement)?.Tag as CodeLenseUserControlViewModel;
                if (viewModel is null)
                {
                    return;
                }

                await viewModel.GotoSiblingAsync();
            }
            catch
            {
                //nothing to do
            }
        }

    }
}
