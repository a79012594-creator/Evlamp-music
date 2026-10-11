using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;

namespace EVLAMP.Music;

public partial class MainPage : ContentPage
{
    private readonly ObservableCollection<TrackItem> _queue = new();
    private int _current = -1;
    private bool _isPlaying;
    private bool _shuffle;
    private bool _repeat;
    private readonly Random _random = new();

    public MainPage()
    {
        InitializeComponent();
        QueueView.ItemsSource = _queue;
    }

    private async void OnAddSongs(object sender, EventArgs e)
    {
        try
        {
            var picked = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Выбери музыкальные файлы",
                FileTypes = FilePickerFileType.Audio
            });

            foreach (var file in picked)
            {
                if (_queue.Any(t => t.FullPath == file.FullPath)) continue;
                _queue.Add(new TrackItem
                {
                    Name = Path.GetFileNameWithoutExtension(file.FileName),
                    Subtitle = file.FileName,
                    FullPath = file.FullPath
                });
            }

            if (_current < 0 && _queue.Count > 0)
                NowPlayingLabel.Text = "Готово к воспроизведению";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Не удалось добавить файлы", ex.Message, "OK");
        }
    }

    private async void OnSongSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is TrackItem item)
        {
            _current = _queue.IndexOf(item);
            QueueView.SelectedItem = null;
            await StartCurrentAsync();
        }
    }

    private async Task StartCurrentAsync()
    {
        if (_current < 0 || _current >= _queue.Count) return;
#if ANDROID
        try
        {
            var path = _queue[_current].FullPath;
            var uri = Android.Net.Uri.Parse(path);
            var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(Platforms.Android.MusicPlaybackService));
            intent.SetAction(Platforms.Android.MusicPlaybackService.ActionPlay);
            intent.PutExtra(Platforms.Android.MusicPlaybackService.ExtraUri, uri.ToString());
            intent.PutExtra(Platforms.Android.MusicPlaybackService.ExtraTitle, _queue[_current].Name);
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                Android.App.Application.Context.StartForegroundService(intent);
            else
                Android.App.Application.Context.StartService(intent);
            _isPlaying = true;
            PlayButton.Text = "⏸";
            NowPlayingLabel.Text = _queue[_current].Name;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка воспроизведения", ex.Message, "OK");
        }
#else
        await DisplayAlert("Только Android", "Этот плеер предназначен для Android.", "OK");
#endif
    }

    private void SendServiceAction(string action)
    {
#if ANDROID
        var intent = new Android.Content.Intent(Android.App.Application.Context, typeof(Platforms.Android.MusicPlaybackService));
        intent.SetAction(action);
        Android.App.Application.Context.StartService(intent);
#endif
    }

    private async void OnPlayPause(object sender, EventArgs e)
    {
        if (_current < 0)
        {
            if (_queue.Count == 0) { await DisplayAlert("Очередь пуста", "Сначала добавь песни.", "OK"); return; }
            _current = 0;
            await StartCurrentAsync();
            return;
        }
        if (_isPlaying)
        {
            SendServiceAction(Platforms.Android.MusicPlaybackService.ActionPause);
            _isPlaying = false;
            PlayButton.Text = "▶";
        }
        else
        {
            await StartCurrentAsync();
        }
    }

    private async void OnNext(object sender, EventArgs e)
    {
        if (_queue.Count == 0) return;
        _current = _shuffle ? _random.Next(_queue.Count) : (_current + 1) % _queue.Count;
        await StartCurrentAsync();
    }

    private async void OnPrevious(object sender, EventArgs e)
    {
        if (_queue.Count == 0) return;
        _current = _shuffle ? _random.Next(_queue.Count) : (_current - 1 + _queue.Count) % _queue.Count;
        await StartCurrentAsync();
    }

    private void OnShuffle(object sender, EventArgs e)
    {
        _shuffle = !_shuffle;
        ShuffleButton.Text = _shuffle ? "⇄ ВКЛ" : "⇄";
        ShuffleButton.TextColor = _shuffle ? Colors.White : Color.FromArgb("#E9D5FF");
        ShuffleButton.BackgroundColor = _shuffle ? Color.FromArgb("#7E22CE") : Color.FromArgb("#29163D");
    }

    private void OnRepeat(object sender, EventArgs e)
    {
        _repeat = !_repeat;
        RepeatButton.Text = _repeat ? "Повтор: вкл." : "Повтор: выкл.";
        RepeatButton.TextColor = _repeat ? Colors.White : Color.FromArgb("#C084FC");
    }

    private void OnRemoveSong(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not TrackItem item) return;
        var index = _queue.IndexOf(item);
        _queue.Remove(item);
        if (index == _current) { _current = -1; _isPlaying = false; NowPlayingLabel.Text = "Ничего не играет"; PlayButton.Text = "▶"; SendServiceAction(Platforms.Android.MusicPlaybackService.ActionStop); }
        else if (index < _current) _current--;
    }

    private void OnClear(object sender, EventArgs e)
    {
        SendServiceAction(Platforms.Android.MusicPlaybackService.ActionStop);
        _queue.Clear();
        _current = -1;
        _isPlaying = false;
        NowPlayingLabel.Text = "Ничего не играет";
        PlayButton.Text = "▶";
    }

    public class TrackItem
    {
        public string Name { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string FullPath { get; set; } = "";
    }
}
