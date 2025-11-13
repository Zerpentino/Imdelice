using System;
using CommunityToolkit.Maui.Views;
using Imdeliceapp.Pages;

namespace Imdeliceapp.Popups;

public partial class KdsTicketDetailPopup : Popup
{
    public KdsTicketDetailPopup(KdsPage.KdsTicketVm ticket)
    {
        InitializeComponent();
        BindingContext = ticket;
    }

    void CloseButton_Clicked(object sender, EventArgs e)
    {
        Close();
    }
}
