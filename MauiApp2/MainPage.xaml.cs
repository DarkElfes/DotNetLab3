using Microsoft.AspNetCore.Components.WebView.Maui;

namespace MauiApp2
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("MyBlazorCustomization", (handler, view) => {
#if IOS
        handler.PlatformView.Opaque = false;
        handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
#elif WINDOWS
                handler.PlatformView.Opacity = 0;
                handler.PlatformView.DefaultBackgroundColor = new Windows.UI.Color() { A = 0, R = 0, G = 0, B = 0 };
#endif
            });
        }
    }
}
