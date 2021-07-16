using Digimezzo.Foundation.Core.Settings;
using Dopamine.Core.Base;
using Dopamine.Core.Extensions;
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
            IList<AlbumViewModel> albumViewModels = (this.SelectedAlbums == null || this.SelectedAlbums.Count == 0) ? (this.AlbumsCvs != null ? this.AlbumsCvs.View.Cast<AlbumViewModel>().ToList() : this.SelectedAlbums) : this.SelectedAlbums;
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
            await this.GetAlbumsCommonAsync(this.Albums, this.AlbumOrder, true);
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

            lastAlbumsSelectNoneAlbumOrder = this.AlbumOrder;
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

        protected override bool PassedSecondaryAlbumFilter(AlbumViewModel avm)
        {
            if (!avm.AlbumLove) {
                return false;
            }

            switch (this.AlbumOrder)
            {
                case AlbumOrder.ByDateLastPlayed:
                    return avm.DateLastPlayed.HasValue;
                default:
                    return true;
            }
        }
    }
}
