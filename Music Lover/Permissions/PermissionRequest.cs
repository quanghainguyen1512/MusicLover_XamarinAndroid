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
    public class PermissionRequest
    {
        public List<string> Permissions { get; }
        public IPermissionCalback PermissionCalback { get; }
        public int RequestCode { get; }
        

        public PermissionRequest(int reqCode)
        {
            RequestCode = reqCode;
        }

        public PermissionRequest(List<string> permissions, IPermissionCalback permissionCalback)
        {
            Permissions = permissions;
            PermissionCalback = permissionCalback;
            var rand = new Random();
            RequestCode = rand.Next(10000);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (obj is PermissionRequest req)
                return req.RequestCode == RequestCode;
            return false;
        }

        public override int GetHashCode()
        {
            return RequestCode;
        }
    }
}