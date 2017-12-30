using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Music_Lover.Permissions
{
    public interface IPermissionCalback
    {
        void PermissionGranted();
        void PermissionDenied();
    }
}