﻿using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Utils;
using Dopamine.Data.Metadata;
using Dopamine.Services.Metadata;
using Prism.Mvvm;
using System;
using System.Globalization;
using System.IO;

namespace Dopamine.ViewModels.Common
{
    public class FileInformationViewModel : BindableBase
    {
        private IMetadataService metaDataService;

        // Song
        private string songTitle;
        private string songArtists;
        private string songComposers;
        private string songAlbum;
        private string songYear;
        private string songGenres;
        private string songTrackNumber;

        // File
        private string fileName;
        private string fileFolder;
        private string filePath;
        private string fileSize;
        private string fileLastModified;

        // Audio
        private string audioDuration;
        private string audioType;
        private string audioSampleRate;
        private string audioBitrate;

        public string SongTitle
        {
            get { return this.songTitle; }
            set { SetProperty<string>(ref this.songTitle, value); }
        }

        public string SongArtists
        {
            get { return this.songArtists; }
            set { SetProperty<string>(ref this.songArtists, value); }
        }

        public string SongComposers
        {
            get { return this.songComposers; }
            set { SetProperty<string>(ref this.songComposers, value); }
        }

        public string SongAlbum
        {
            get { return this.songAlbum; }
            set { SetProperty<string>(ref this.songAlbum, value); }
        }

        public string SongYear
        {
            get { return this.songYear; }
            set { SetProperty<string>(ref this.songYear, value); }
        }

        public string SongGenres
        {
            get { return this.songGenres; }
            set { SetProperty<string>(ref this.songGenres, value); }
        }

        public string SongTrackNumber
        {
            get { return this.songTrackNumber; }
            set { SetProperty<string>(ref this.songTrackNumber, value); }
        }

        public string FileName
        {
            get { return this.fileName; }
            set { SetProperty<string>(ref this.fileName, value); }
        }

        public string FileFolder
        {
            get { return this.fileFolder; }
            set { SetProperty<string>(ref this.fileFolder, value); }
        }

        public string FilePath
        {
            get { return this.filePath; }
            set { SetProperty<string>(ref this.filePath, value); }
        }

        public string FileSize
        {
            get { return this.fileSize; }
            set { SetProperty<string>(ref this.fileSize, value); }
        }

        public string FileLastModified
        {
            get { return this.fileLastModified; }
            set { SetProperty<string>(ref this.fileLastModified, value); }
        }

        public string AudioDuration
        {
            get { return this.audioDuration; }
            set { SetProperty<string>(ref this.audioDuration, value); }
        }


        public string AudioType
        {
            get { return this.audioType; }
            set { SetProperty<string>(ref this.audioType, value); }
        }

        public string AudioSampleRate
        {
            get { return this.audioSampleRate; }
            set { SetProperty<string>(ref this.audioSampleRate, value); }
        }

        public string AudioBitrate
        {
            get { return this.audioBitrate; }
            set { SetProperty<string>(ref this.audioBitrate, value); }
        }
        
        public FileInformationViewModel(IMetadataService metaDataService, string path)
        {
            this.metaDataService = metaDataService;

            this.GetFileMetadata(path);
            this.GetFileInformation(path);
        }

        public bool SongHasComposers => !string.IsNullOrEmpty(this.SongComposers);

        private void GetFileMetadata(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return;
                }

                var fm = new FileMetadata(path);

                this.SongTitle = fm.Title.Value;
                this.SongAlbum = fm.Album.Value;
                this.SongArtists = string.Join(", ", fm.Artists.Values);
                this.SongComposers = string.Join(", ", fm.Composers.Values);
                this.SongGenres = string.Join(", ", fm.Genres.Values);
                this.SongYear = fm.Year.Value.ToString();
                this.SongTrackNumber = fm.TrackNumber.Value.ToString();
                this.AudioDuration = FormatUtils.FormatTime(fm.Duration);
                this.AudioType = fm.Type;
                this.AudioSampleRate = string.Format("{0} {1}", fm.SampleRate.ToString(), "Hz");
                this.AudioBitrate = string.Format("{0} {1}", fm.BitRate.ToString(), "kbps");
            }
            catch (Exception ex)
            {
                LogClient.Error("Error while getting file Metadata. Exception: {0}", ex.Message);
            }
        }

        private void GetFileInformation(string path)
        {
            try
            {
                this.FileName = FileUtils.FileName(path);
                this.FileFolder = FileUtils.DirectoryName(path);
                this.FilePath = path;
                this.FileSize = FormatUtils.FormatFileSize(FileUtils.SizeInBytes(path));
                this.FileLastModified = FileUtils.DateModified(path).ToString("D", new CultureInfo(ResourceUtils.GetString("Language_ISO639-1")));
            }
            catch (Exception ex)
            {
                LogClient.Error("Error while getting file Information. Exception: {0}", ex.Message);
            }
        }
    }
}