using System;

namespace Dopamine.Services.Metadata
{
    public class AlbumLoveChangedEventArgs : EventArgs
    {
        public string AlbumKey { get; }
        public bool Love { get; }

        public long? DateAlbumLoved { get; }

        public AlbumLoveChangedEventArgs(string albumKey, bool love, long? dateAlbumLoved)
        {
            this.AlbumKey = albumKey;
            this.Love = love;
            this.DateAlbumLoved = dateAlbumLoved;
        }
    }
}
