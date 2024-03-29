﻿using System.Windows;

namespace Qualia.Tools;

public class Messages
{
    public static void ShowError(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static void ShowApplyOrCancel()
    {
        MessageBox.Show("Apply or cancel changes!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
    }
}