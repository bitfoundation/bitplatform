﻿using Microsoft.Maui.Hosting;

namespace Bit.MauiAppSample.WinUI
{
    public partial class App
    {
        public App()
        {
            InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
