/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using OxygenNEL;
using OxygenNEL.Component;

using Windows.UI;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Serilog;

namespace OxygenNEL.Page
{
    public sealed partial class HomePage : Microsoft.UI.Xaml.Controls.Page
    {
        public static string PageTitle => "概括";

        public HomePage()
        {
            InitializeComponent();
        }
    }
}
