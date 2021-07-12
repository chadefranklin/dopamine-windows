using Digimezzo.Foundation.Core.Settings;
using Dopamine.Core.Base;
using Dopamine.Core.Utils;
using Dopamine.Data;
using Dopamine.Services.Collection;
using Dopamine.Services.Entities;
using Dopamine.Services.Indexing;
using Dopamine.Services.Metadata;
using Dopamine.Services.Search;
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
        private ICollectionService collectionService;
        private ISearchService searchService;
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
            this.collectionService = container.Resolve<ICollectionService>();
            this.searchService = container.Resolve<ISearchService>();
            this.eventAggregator = container.Resolve<IEventAggregator>();

            // Settings
            SettingsClient.SettingChanged += async (_, e) =>
            {
                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableRating"))
                {
                    this.EnableRating = (bool)e.Entry.Value;
                    this.SetTrackOrder("AlbumsTrackOrder");
                    await this.GetTracksAsync(null, null, SelectiveSelectedAlbums, this.TrackOrder);
                }

                if (SettingsClient.IsSettingChanged(e, "Behaviour", "EnableLove"))
                {
                    this.EnableLove = (bool)e.Entry.Value;
                    this.SetTrackOrder("AlbumsTrackOrder");
                    await this.GetTracksAsync(null, null, SelectiveSelectedAlbums, this.TrackOrder);
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
        }

        protected override IList<AlbumViewModel> GetSelectiveSelectedAlbums()
        {
            IList<AlbumViewModel> albumViewModels = (this.SelectedAlbums == null || this.SelectedAlbums.Count == 0) ? this.Albums : this.SelectedAlbums;
            return albumViewModels;
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
            await this.GetTracksAsync(null, null, SelectiveSelectedAlbums, this.TrackOrder);
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
            await this.GetTracksAsync(null, null, SelectiveSelectedAlbums, this.TrackOrder);
        }

        protected override void RefreshLanguage()
        {
            this.UpdateAlbumOrderText(this.AlbumOrder);
            this.UpdateTrackOrderText(this.TrackOrder);
            base.RefreshLanguage();
        }

        protected async override void PlaybackService_PlaybackCountersChanged(IList<PlaybackCounter> counters)
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

            await Task.Run(() =>
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

                        if (this.AlbumsHolder[i].AlbumLove || this.searchService.SearchText != string.Empty) {
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
                            this.Albums.Insert(0, this.AlbumsHolder[i]);
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

            if (this.searchService.SearchText != string.Empty) // nothing below should apply when we are searching
            {
                return;
            }

            await Task.Run(() =>
            {
                if (e.Love) {
                    // Insert in correct order rather than calling await this.GetAlbumsCommonAsync(this.AlbumsHolder, this.AlbumOrder, true);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AlbumViewModel albumToInsert = this.AlbumsHolder.Where(x => x.AlbumKey == e.AlbumKey).FirstOrDefault();

                        if (albumToInsert == null) return;

                        int insertIndex;
                        switch (this.AlbumOrder)
                        {
                            case AlbumOrder.Alphabetical:
                                for (insertIndex = 0; insertIndex < this.Albums.Count && FormatUtils.GetSortableString(this.Albums[insertIndex].AlbumTitle).CompareTo(FormatUtils.GetSortableString(albumToInsert.AlbumTitle)) < 0; insertIndex++) ;
                                this.Albums.Insert(insertIndex, albumToInsert);
                                break;
                            case AlbumOrder.ByDateAdded:
                                for (insertIndex = 0; insertIndex < this.Albums.Count && ((long)this.Albums[insertIndex].DateAdded).CompareTo(albumToInsert.DateAdded) > 0; insertIndex++) ;
                                this.Albums.Insert(insertIndex, albumToInsert);
                                break;
                            case AlbumOrder.ByDateCreated:
                                for (insertIndex = 0; insertIndex < this.Albums.Count && ((long)this.Albums[insertIndex].DateFileCreated).CompareTo(albumToInsert.DateFileCreated) > 0; insertIndex++) ;
                                this.Albums.Insert(insertIndex, albumToInsert);
                                break;
                            case AlbumOrder.ByAlbumArtist:
                                for (insertIndex = 0; insertIndex < this.Albums.Count && FormatUtils.GetSortableString(this.Albums[insertIndex].AlbumArtist).CompareTo(FormatUtils.GetSortableString(albumToInsert.AlbumArtist)) < 0; insertIndex++) ;
                                this.Albums.Insert(insertIndex, albumToInsert);
                                break;
                            case AlbumOrder.ByYearAscending:
                                for (insertIndex = 0; insertIndex < this.Albums.Count && this.Albums[insertIndex].SortYear.CompareTo(albumToInsert.SortYear) < 0; insertIndex++) ;
                                this.Albums.Insert(insertIndex, albumToInsert);
                                break;
                            case AlbumOrder.ByYearDescending:
                                for (insertIndex = 0; insertIndex < this.Albums.Count && this.Albums[insertIndex].SortYear.CompareTo(albumToInsert.SortYear) > 0; insertIndex++) ;
                                this.Albums.Insert(insertIndex, albumToInsert);
                                break;
                            case AlbumOrder.ByDateLastPlayed:
                                for (insertIndex = 0; insertIndex < this.Albums.Count && ((long)this.Albums[insertIndex].DateLastPlayed).CompareTo(albumToInsert.DateLastPlayed) > 0; insertIndex++) ;
                                this.Albums.Insert(insertIndex, albumToInsert);
                                break;
                            default:
                                // Alphabetical
                                for (insertIndex = 0; insertIndex < this.Albums.Count && FormatUtils.GetSortableString(this.Albums[insertIndex].AlbumTitle).CompareTo(FormatUtils.GetSortableString(albumToInsert.AlbumTitle)) < 0; insertIndex++) ;
                                this.Albums.Insert(insertIndex, albumToInsert);
                                break;
                        }

                        // Update count
                        this.AlbumsCount = this.AlbumsCvs.View.Cast<AlbumViewModel>().Count();
                    });
                } else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        for (int i = 0, count = this.Albums.Count; i < count; i++)
                        {
                            if (this.Albums[i].AlbumKey.Equals(e.AlbumKey))
                            {
                                this.Albums.RemoveAt(i);
                                break;
                            }
                        }

                        // Update count
                        this.AlbumsCount = this.AlbumsCvs.View.Cast<AlbumViewModel>().Count();
                    });
                }
            });
        }
    }
}
