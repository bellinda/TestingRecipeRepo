using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace BackgroundAudioTask
{
    public sealed class BackgroundAudioTask : IBackgroundTask
{
    private BackgroundTaskDeferral deferral;
    private SystemMediaTransportControls systemMediaTransportControl;
    MediaElement media = new MediaElement();

    public void Run(IBackgroundTaskInstance taskInstance)
    {
        deferral = taskInstance.GetDeferral();

        //do my stuff
        media.AudioCategory = AudioCategory.BackgroundCapableMedia;
        media.Source = new Uri("BindingRecepies.Shared/Music/music.mp3");
        media.Play();
        SystemMediaTransportControls _systemMediaTransportControl = SystemMediaTransportControls.GetForCurrentView();

        taskInstance.Canceled += OnCanceled;
        taskInstance.Task.Completed += OnCompleted;

        deferral.Complete();
    }

    private void OnCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
    {
        throw new NotImplementedException();
    }

    private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
    {
        throw new NotImplementedException();
    }
}
}
