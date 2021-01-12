﻿using System;

namespace b7.Packets.Services.Remote.GEarth
{
    public class GEarthOptions
    {
        public int Port { get; init; }
        public string Title { get; init; }
        public string Author { get; init; }
        public string Version { get; init; }
        public string Description { get; init; }
        public string FilePath { get; init; }
        public string Cookie { get; init; }
        public bool EnableOnClick { get; init; }
        public bool CanLeave { get; init; }
        public bool CanDelete { get; init; }

        public GEarthOptions()
        {
            Title =
            Author =
            Version =
            Description =
            FilePath =
            Cookie = string.Empty;
        }
    }
}
