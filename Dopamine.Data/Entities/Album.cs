using Dopamine.Core.Extensions;
using SQLite;
using System;

namespace Dopamine.Data.Entities
{
    public class Album
    {
        public string AlbumKey { get; set; }

        public long? AlbumLove { get; set; }

        public long? DateAlbumLoved { get; set; }

        public static Album CreateDefault(string albumKey)
        {
            var album = new Album()
            {
                AlbumKey = albumKey
            };

            return album;
        }

        public Album ShallowCopy()
        {
            return (Album)this.MemberwiseClone();
        }
    }
}
