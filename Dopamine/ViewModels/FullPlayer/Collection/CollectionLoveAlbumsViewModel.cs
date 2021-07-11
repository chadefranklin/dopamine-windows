using Digimezzo.Foundation.Core.Settings;
using Dopamine.Core.Base;
using Dopamine.Data;
using Dopamine.Services.Collection;
using Dopamine.Services.Entities;
using Dopamine.Services.Indexing;
using Dopamine.Services.Metadata;
using Dopamine.Services.Playback;
using Dopamine.ViewModels.Common.Base;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Dopamine.ViewModels.FullPlayer.Collection
{
    public class CollectionLoveAlbumsViewModel : AlbumsViewModelBase
    {
        private IIndexingService indexingService;
        private IPlaybackService playbackService;
        private ICollectionService collectionService;
        private IEventAggregator eventAggregator;
        private double leftPaneWidthPercent;

        public double LeftPaneWidthPercent
        {
            get { return this.leftPaneWidthPercent; }
            set
            {
                SetProperty<double>(ref this.leftPaneWidthPercent, value);
                SettingsClient.Set<int>("ColumnWidths", "AlbumsLeftPaneWidthPercent", Convert.ToInt32(value));
            }
        }

        public CollectionLoveAlbumsViewModel(IContainerProvider container) : base(container)
        {
            // Dependency injection
            this.indexingService = container.Resolve<IIndexingService>();
            this.playbackService = container.Resolve<IPlaybackService>();
            this.collectionService = container.Resolve<ICollectionService>();
            this.eventAggregator = container.Resolve<IEventAggregator>();

            // Settings
            SettingsClient.SettingChanged += async (_, e) =>
            {
                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
                {
                    this.EnableRating = (bool)e.Entry.Value;
                    this.SetTrackOrder("AlbumsTrackOrder");
                    await this.GetTracksAsync(null, null, this.SelectedAlbums, this.TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
                {
                    this.EnableLove = (bool)e.Entry.Value;
                    this.SetTrackOrder("AlbumsTrackOrder");
                    await this.GetTracksAsync(null, null, this.SelectedAlbums, this.TrackOrder);
                }
            };

            //  Commands
            this.ToggleAlbumOrderCommand = new DelegateCommand(async () => await this.ToggleAlbumOrderAsync());
            this.ToggleTrackOrderCommand = new DelegateCommand(async () => await this.ToggleTrackOrderAsync());
            this.RemoveSelectedTracksCommand = new DelegateCommand(async () => await this.RemoveTracksFromCollectionAsync(this.SelectedTracks), () => !this.IsIndexing);

            // Set the initial AlbumOrder
            this.AlbumOrder = (AlbumOrder)SettingsClient.Get<int>("Ordering", "AlbumsAlbumOrder");

            // Set the initial TrackOrder
            this.SetTrackOrder("AlbumsTrackOrder");

            // Set width of the panels
            this.LeftPaneWidthPercent = SettingsClient.Get<int>("ColumnWidths", "AlbumsLeftPaneWidthPercent");

            // Cover size
            this.SetCoversizeAsync((CoverSizeType)SettingsClient.Get<int>("CoverSizes", "AlbumsCoverSize"));

            this.playbackService.PlaybackCountersChanged += PlaybackService_PlaybackCountersChanged;
        }

        private async Task ToggleTrackOrderAsync()
        {
            base.ToggleTrackOrder();

            SettingsClient.Set<int>("Ordering", "AlbumsTrackOrder", (int)this.TrackOrder);
            await this.GetTracksCommonAsync(this.Tracks, this.TrackOrder);
        }

        private async Task ToggleAlbumOrderAsync()
        {
            base.ToggleAlbumOrder();

            SettingsClient.Set<int>("Ordering", "AlbumsAlbumOrder", (int)this.AlbumOrder);
            await this.GetAlbumsCommonAsync(this.AlbumsHolder, this.AlbumOrder, true);
        }

        protected async override Task SetCoversizeAsync(CoverSizeType iCoverSize)
        {
            await base.SetCoversizeAsync(iCoverSize);
            SettingsClient.Set<int>("CoverSizes", "AlbumsCoverSize", (int)iCoverSize);
        }

        protected async override Task FillListsAsync()
        {
            await this.GetAllAlbumsAsync(this.AlbumOrder, true);
            await this.GetTracksAsync(null, null, this.SelectedAlbums, this.TrackOrder);
        }

        protected async override Task EmptyListsAsync()
        {
            this.ClearAlbums();
            this.ClearTracks();
        }

        protected async override Task SelectedAlbumsHandlerAsync(object parameter)
        {
            await base.SelectedAlbumsHandlerAsync(parameter);

            this.SetTrackOrder("AlbumsTrackOrder");
            await this.GetTracksAsync(null, null, this.SelectedAlbums, this.TrackOrder);
        }

        protected override void RefreshLanguage()
        {
            this.UpdateAlbumOrderText(this.AlbumOrder);
            this.UpdateTrackOrderText(this.TrackOrder);
            base.RefreshLanguage();
        }

        private async void PlaybackService_PlaybackCountersChanged(IList<PlaybackCounter> counters)
        {
            if (this.Albums == null || this.AlbumsHolder == null)
            {
                return;
            }

            if (counters == null)
            {
                return;
            }

            if (counters.Count == 0)
            {
                return;
            }

            await Task.Run(async () =>
            {
                HashSet<int> albumViewModelsToReorder = new HashSet<int>();
                HashSet<int> albumViewModelsToAdd = new HashSet<int>();

                for (int i = 0, count = this.AlbumsHolder.Count; i < count; i++)
                {
                    if (counters.Select(c => c.AlbumKey).Contains(this.AlbumsHolder[i].AlbumKey))
                    {
                        // The UI is only updated if PropertyChanged is fired on the UI thread
                        PlaybackCounter counter = counters.Where(c => c.AlbumKey.Equals(this.AlbumsHolder[i].AlbumKey)).FirstOrDefault();
                        Application.Current.Dispatcher.Invoke(() => this.AlbumsHolder[i].UpdateCounters(counter));

                        if (this.Albums.Contains(this.AlbumsHolder[i]))
                        {
                            albumViewModelsToReorder.Add(this.Albums.IndexOf(this.AlbumsHolder[i]));
                        }
                        else
                        {
                            albumViewModelsToAdd.Add(i);
                        }
                    }
                }


                if (this.AlbumOrder == AlbumOrder.ByDateLastPlayed)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (int i in albumViewModelsToReorder)
                        {
                            this.Albums.Move(i, 0);
                        }
                        foreach (int i in albumViewModelsToAdd)
                        {
                            this.Albums.Add(this.AlbumsHolder[i]);
                            this.Albums.Move(this.Albums.Count - 1, 0);
                        }

                        // Update count
                        this.AlbumsCount = this.AlbumsCvs.View.Cast<AlbumViewModel>().Count();
                    });
                }
            });
        }

        protected async override void MetadataService_AlbumLoveChangedAsync(AlbumLoveChangedEventArgs e)
        {
            base.MetadataService_AlbumLoveChangedAsync(e);

            if (this.Albums == null || this.AlbumsHolder == null)
            {
                return;
            }

            await this.GetAlbumsCommonAsync(this.AlbumsHolder, this.AlbumOrder, true);
        }
    }
}
