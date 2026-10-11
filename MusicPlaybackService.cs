using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;

namespace EVLAMP.Music.Platforms.Android;

[Service(Name = "com.evlamp.music.MusicPlaybackService", Exported = false,
    ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMediaPlayback)]
public class MusicPlaybackService : Service
{
    public const string ActionPlay = "com.evlamp.music.PLAY";
    public const string ActionPause = "com.evlamp.music.PAUSE";
    public const string ActionStop = "com.evlamp.music.STOP";
    public const string ExtraUri = "uri";
    public const string ExtraTitle = "title";
    private const string ChannelId = "evlamp_playback";
    private const int NotificationId = 4107;
    private MediaPlayer? _player;

    public override void OnCreate()
    {
        base.OnCreate();
        CreateChannel();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action;
        if (action == ActionPlay)
        {
            var uriText = intent?.GetStringExtra(ExtraUri);
            var title = intent?.GetStringExtra(ExtraTitle) ?? "EVLAMP Music";
            if (!string.IsNullOrWhiteSpace(uriText))
                PlayUri(Android.Net.Uri.Parse(uriText), title);
        }
        else if (action == ActionPause)
        {
            if (_player?.IsPlaying == true) _player.Pause();
            ShowNotification("На паузе", false);
        }
        else if (action == ActionStop)
        {
            StopPlayer();
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
        }
        return StartCommandResult.Sticky;
    }

    private void PlayUri(Android.Net.Uri uri, string title)
    {
        try
        {
            StopPlayer();
            _player = new MediaPlayer();
            _player.SetAudioAttributes(new AudioAttributes.Builder()
                .SetUsage(AudioUsageKind.Media).SetContentType(AudioContentType.Music).Build());
            _player.SetDataSource(this, uri);
            _player.SetWakeMode(this, WakeLockFlags.Partial);
            _player.Prepared += (_, _) =>
            {
                _player?.Start();
                ShowNotification(title, true);
            };
            _player.Completion += (_, _) =>
            {
                // MainPage controls queue/auto-next in this first version.
                ShowNotification("Трек завершён", false);
            };
            _player.Error += (_, args) =>
            {
                ShowNotification("Ошибка аудио", false);
                args.Handled = true;
            };
            StartForeground(NotificationId, BuildNotification(title, true));
            _player.PrepareAsync();
        }
        catch
        {
            StopPlayer();
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
        }
    }

    private Notification BuildNotification(string title, bool playing)
    {
        var pauseIntent = new Intent(this, Class).SetAction(playing ? ActionPause : ActionPlay);
        var pausePending = PendingIntent.GetService(this, 1, pauseIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle(title)
            .SetContentText(playing ? "EVLAMP Music • воспроизведение" : "EVLAMP Music • пауза")
            .SetSmallIcon(global::Android.Resource.Drawable.IcMediaPlay)
            .SetOngoing(playing)
            .SetOnlyAlertOnce(true)
            .AddAction(playing ? global::Android.Resource.Drawable.IcMediaPause : global::Android.Resource.Drawable.IcMediaPlay,
                playing ? "Пауза" : "Продолжить", pausePending);
        return builder.Build();
    }

    private void ShowNotification(string title, bool playing)
    {
        var manager = NotificationManagerCompat.From(this);
        manager.Notify(NotificationId, BuildNotification(title, playing));
    }

    private void CreateChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "EVLAMP playback", NotificationImportance.Low)
            {
                Description = "Управление воспроизведением EVLAMP Music"
            };
            GetSystemService(NotificationService) is NotificationManager manager
                .CreateNotificationChannel(channel);
        }
    }

    private void StopPlayer()
    {
        if (_player is null) return;
        try { _player.Stop(); } catch { }
        _player.Release();
        _player.Dispose();
        _player = null;
    }

    public override void OnDestroy()
    {
        StopPlayer();
        base.OnDestroy();
    }

    public override IBinder? OnBind(Intent? intent) => null;
}
