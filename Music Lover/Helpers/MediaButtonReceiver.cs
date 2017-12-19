using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Support.V4.Content;
using Android.Views;
using Music_Lover.Utils;

namespace Music_Lover.Helpers
{
    [BroadcastReceiver(Enabled = true)]
    [IntentFilter(new []{Intent.ActionMediaButton, AudioManager.ActionAudioBecomingNoisy})]
    public class MediaButtonReceiver : WakefulBroadcastReceiver
    {
        private const int MSG_LONGPRESS_TIMEOUT = 1;
        private const int MSG_HEADSET_DOUBLE_CLICK_TIMEOUT = 2;

        private const int LONG_PRESS_DELAY = 1000;
        private const int DOUBLE_CLICK = 800;

        private static PowerManager.WakeLock _wakeLock = null;

        private static int _clickCounter;
        private static long _lastClickTime;
        private static bool _down;
        private static bool _launched;

        private readonly Handler _handler = new HeadSetHandler();

        public override void OnReceive(Context context, Intent intent)
        {
            var intentAction = intent.Action;
            if (intentAction.Equals(AudioManager.ActionAudioBecomingNoisy))
            {
                if (PreferencesUtility.GetInstance(context).PauseOnDetach())
                    StartTheService(context, MusicService.CMDPAUSE);
            }
            else if (intentAction.Equals(Intent.ActionMediaButton))
            {
                var key = intent.GetParcelableExtra(Intent.ExtraKeyEvent) as KeyEvent;
                if (key == null)
                    return;
                var keycode = key.KeyCode;
                var action = key.Action;
                var eventTime = key.EventTime;
                var cmd = "";
                switch (keycode)
                {
                    case Keycode.MediaStop:
                        cmd = MusicService.CMDSTOP;
                        break;
                    case Keycode.Headsethook:
                    case Keycode.MediaPlayPause:
                        cmd = MusicService.CMDTOGGLEPAUSE;
                        break;
                    case Keycode.MediaNext:
                        cmd = MusicService.CMDNEXT;
                        break;
                    case Keycode.MediaPrevious:
                        cmd = MusicService.CMDPREVIOUS;
                        break;
                    case Keycode.MediaPlay:
                        cmd = MusicService.CMDPLAY;
                        break;
                }

                if (!string.IsNullOrEmpty(cmd))
                {
                    if (action == KeyEventActions.Down)
                    {
                        if (_down)
                        {
                            if (cmd.Equals(MusicService.CMDTOGGLEPAUSE) || cmd.Equals(MusicService.CMDPLAY))
                            {
                                if (_lastClickTime != 0 && eventTime - _lastClickTime > LONG_PRESS_DELAY)
                                    AcquireWakeLockAndSendMessage(context,
                                        _handler.ObtainMessage(MSG_LONGPRESS_TIMEOUT, context), 0);
                            }
                        }
                    }
                    else if (key.RepeatCount == 0)
                    {
                        if (keycode == Keycode.Headsethook)
                        {
                            if (eventTime - _lastClickTime > DOUBLE_CLICK)
                            {
                                _clickCounter = 0;
                            }

                            _clickCounter++;
                            _handler.RemoveMessages(MSG_HEADSET_DOUBLE_CLICK_TIMEOUT);

                            var msg = _handler.ObtainMessage(MSG_HEADSET_DOUBLE_CLICK_TIMEOUT, _clickCounter, 0,
                                context);

                            var delay = _clickCounter < 3 ? DOUBLE_CLICK : 0;

                            if (_clickCounter >= 3)
                                _clickCounter = 0;

                            _lastClickTime = eventTime;
                            AcquireWakeLockAndSendMessage(context, msg, delay);
                        }
                        else
                        {
                            StartTheService(context, cmd);
                        }

                        _launched = false;
                        _down = true;
                    }
                }
                else
                {
                    _handler.RemoveMessages(MSG_LONGPRESS_TIMEOUT);
                    _down = false;
                }

                if (IsOrderedBroadcast) InvokeAbortBroadcast();
                ReleaseWakeLockIfHandlerIdle();
            }
        }

        private static void StartTheService(Context context, string cmd)
        {
            var i = new Intent(context, typeof(MusicService));
            i.SetAction(MusicService.SERVICECMD);
            i.PutExtra(MusicService.CMDNAME, cmd);
            i.PutExtra(MusicService.FROM_MEDIA_BUTTON, true);
            StartWakefulService(context, i);
        }

        private void AcquireWakeLockAndSendMessage(Context context, Message msg, long delay)
        {
            if (_wakeLock == null)
            {
                Context appContext = context.ApplicationContext;
                PowerManager pm = (PowerManager)appContext.GetSystemService(Context.PowerService);
                _wakeLock = pm.NewWakeLock(WakeLockFlags.Partial, "Headset button");
                _wakeLock.SetReferenceCounted(false);
            }
            _wakeLock.Acquire(10000);

            _handler.SendMessageDelayed(msg, delay);
        }

        private void ReleaseWakeLockIfHandlerIdle()
        {
            if (_handler.HasMessages(MSG_LONGPRESS_TIMEOUT)
                || _handler.HasMessages(MSG_HEADSET_DOUBLE_CLICK_TIMEOUT))
            {
                return;
            }

            if (_wakeLock != null)
            {
                _wakeLock.Release();
                _wakeLock = null;
            }
        }

        private class HeadSetHandler : Handler
        {
            public override void HandleMessage(Message msg)
            {
                switch (msg.What)
                {
                    case MSG_LONGPRESS_TIMEOUT:
                    {
                        if (!_launched)
                        {
                            var context = (Context) msg.Obj;
                            var i = new Intent();
                            i.SetClass(context, typeof(MainActivity));
                            i.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                            context.StartActivity(i);
                            _launched = true;
                        }
                        break;
                    }
                    case MSG_HEADSET_DOUBLE_CLICK_TIMEOUT:
                    {
                        var clickcount = msg.Arg1;
                        var command = "";
                        switch (clickcount)
                        {
                            case 1:
                                command = MusicService.CMDPAUSE;
                                break;
                            case 2:
                                command = MusicService.CMDNEXT;
                                break;
                            case 3:
                                command = MusicService.CMDPREVIOUS;
                                break;
                        }

                        if (!string.IsNullOrEmpty(command))
                        {
                            var context = (Context) msg.Obj;
                            StartTheService(context, command);
                        }
                        break;
                    }
                }
            }
        }
    }
}