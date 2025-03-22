﻿using SteamApp.Models.Dto;

namespace SteamApp.Models.DTOs
{
    public class PaintedListingsDto : ListingDto
    {
        public bool IsPainted { get; set; }
        public string? PaintText { get; set; }
        public bool IsGoodPaint { get; set; }
    }
}
