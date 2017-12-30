using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using static Android.Manifest.Permission;

namespace Music_Lover.Permissions
{
    public class PermissionCenter
    {
        private static Context _context;
        private static List<PermissionRequest> _permissionRequests = new List<PermissionRequest>();
        private static ISharedPreferences _sharedPreferences;

        private static string GRANTED_PERMISSIONS_KEY = "granted_permissions";

        private static void Init(Context context)
        {
            _context = context;
        }

        public static bool CheckPermission(string permissionName)
        {
            return _context?.CheckSelfPermission(permissionName) == Permission.Granted;
        }

        public static bool HasPermission(Activity activity, string[] permissions)
        {
            return permissions.All(permission => activity.CheckSelfPermission(permission) == Permission.Granted);
        }

        public static bool ShouldShowRequestPermissionRationale(Activity activity, string permission)
        {
            return activity.ShouldShowRequestPermissionRationale(permission);
        }

        public static void AskForPermission(Activity activity, string permission, IPermissionCalback permissionCallback)
        {
            AskForPermission(activity, new[] { permission }, permissionCallback);
        }

        public static void AskForPermission(Activity activity, string[] permissions, IPermissionCalback permissionCallback)
        {
            if (permissionCallback is null) return;
            if (HasPermission(activity, permissions))
            {
                permissionCallback.PermissionGranted();
            }

            var request = new PermissionRequest(permissions.ToList(), permissionCallback);
            _permissionRequests.Add(request);

            activity.RequestPermissions(permissions, request.RequestCode);
        }

        public static void OnRequestPermissionsResult(int reqCode, int[] grantResults)
        {
            var req = new PermissionRequest(reqCode);
            if (_permissionRequests.Contains(req))
            {
                if (VerifyPermissions(grantResults))
                    req.PermissionCalback.PermissionGranted();
                else
                    req.PermissionCalback.PermissionDenied();

                _permissionRequests.Remove(req);
            }

            RefreshMonitoredList();
        }

        private static void RefreshMonitoredList()
        {
            var set = new HashSet<string>(GetGrantedPermissions());
            _sharedPreferences.Edit().PutStringSet(GRANTED_PERMISSIONS_KEY, set);
        }

        private static IEnumerable<string> GetGrantedPermissions()
        {
            if (_context is null) yield return null; 

            var perms = new List<string>
            {
                ReadExternalStorage,
                WriteExternalStorage,
                Internet,
                RecordAudio,
                ReadPhoneState
            };

            foreach (var perm in perms)
            {
                if (_context.CheckSelfPermission(perm) == Permission.Granted)
                    yield return perm;
            }
        }

        private static bool VerifyPermissions(int[] grantResults)
        {
            return grantResults.All(r => r == (int) Permission.Granted);
        }
    }
}