﻿using Newtonsoft.Json;
using SteamApp.Infrastructure.Services;
using SteamApp.Models;
using SteamApp.Models.Proxy;
using System.Web;

namespace SteamApp.Services
{
    public class SteamService : ISteamService
    {
        private readonly HttpClient _httpClient;

        public SteamService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public Task<string[]> GetHatListingsUrls(short fromPage, short batchSize)
        {
            if (fromPage <= 0 || fromPage > 500)
            {
                throw new ArgumentOutOfRangeException("Page is either negative or greater than 500.");
            }

            var url = Constants.MANUAL_HATS_URL;
            var result = new List<string>();
            var currentPage = fromPage;

            for (; currentPage < fromPage + batchSize; currentPage++)
            {
                result.Add($"{url}p{currentPage}_price_asc`");
            }

            if (!result.Any())
            {
                throw new Exception("No URLs found.");
            }

            return Task.FromResult(result.ToArray());
        }

        public Task<string[]> GetWeaponListingsUrls(short fromIndex, short batchSize)
        {
            if (fromIndex < 0 || fromIndex > 500)
            {
                throw new ArgumentOutOfRangeException("Index is either negative or greater than 500.");
            }

            var url = Constants.MANUAL_WEAPONS_URL;
            var result = new List<string>();
            var currentIndex = fromIndex;

            for (; currentIndex < fromIndex + batchSize; currentIndex++)
            {
                result.Add($"{url}{StaticCollections.WEAPON_NAMES[currentIndex - 1]}`");
            }

            if (!result.Any())
            {
                throw new Exception("No URLs found.");
            }

            return Task.FromResult(result.ToArray());
        }

        public async Task<ProductProxy[]> GetFilterredListingsAsync(short page)
        {
            // Same page result ???
            var url = $"{Constants.JSON_100_LISTINGS_URL}{page}";
            var filteredResult = await GetFilterredResults(url);

            return filteredResult.Select(x => new ProductProxy { Name = x.Name, Price = x.SellPrice, Quantity = x.SellListings }).ToArray();
        }

        public async Task<(bool, string)> IsListingPaintedAsync(string name)
        {
            var formattedName = FormatListingNameForUrl(name);
            var url = $"{Constants.FIRST_PAGE_URL_PART_1}{formattedName}{Constants.FIRST_PAGE_URL_PART_2}";
            var jsonResponse = await GetHttpResposeAsync(url);
            var jsonString = FormatJsonStringForDeserialization(jsonResponse);
            var result = DeserializeFormattedJsonString<Listing_Json>(jsonString);

            //When Deserializing assets:440:2 doesn't deserialize the data correctly and leaves everything as null
            //Model binding problem probably

            var resultAssets = result?.Assets?.FirstOrDefault().Value?.FirstOrDefault().Value?.FirstOrDefault().Value?.Descriptions?.Where(x => x.Value != null);

            if (resultAssets != null && resultAssets.Any(x => StaticCollections.GoodPaintsStringValues.Contains(x.Value)))
            {
                var paint = resultAssets.FirstOrDefault(x => x.Value.StartsWith("Paint Color:"))?.Value;
                if (paint != null)
                {
                    return (true, paint);
                }
            }

            return (false, string.Empty);
        }

        public async Task<IEnumerable<ProductProxy>> GetPaintedListingsOnlyAsync(short page)
        {
            var url = $"{Constants.JSON_100_LISTINGS_URL}{page}";
            var results = await GetFilterredResults(url);

            if (!results.Any())
            {
                throw new Exception("No results found.");
            }

            var onlyPaintedResutls = await GetOnlyPaintedListingsFromResults(results);

            return onlyPaintedResutls;
        }


        #region Private Methods
        private async Task<Result[]> GetFilterredResults(string url)
        {
            var jsonResponse = await GetHttpResposeAsync(url);
            if (string.IsNullOrEmpty(jsonResponse))
            {
                throw new Exception("No JSON received or request failed.");
            }

            var jsonString = FormatJsonStringForDeserialization(jsonResponse);
            var result = DeserializeFormattedJsonString<ListingDetails_Json>(jsonString);
            if (result == null)
            {
                throw new Exception("Failed to deserialize JSON.");
            }

            return result.Results!.Where(x => x.SellPrice >= 150 && x.SellPrice <= 450).OrderBy(x => x.SellPrice).ToArray();
        }

        private async Task<IEnumerable<ProductProxy>> GetOnlyPaintedListingsFromResults(Result[] results)
        {
            var itemCollection = new List<ProductProxy>();
            foreach (var item in results)
            {
                var formattedName = FormatListingNameForUrl(item.Name!);
                var url = $"{Constants.FIRST_PAGE_URL_PART_1}{formattedName}{Constants.FIRST_PAGE_URL_PART_2}";
                var jsonResponse = await GetHttpResposeAsync(url);
                var jsonString = FormatJsonStringForDeserialization(jsonResponse);
                var deserializedResult = DeserializeFormattedJsonString<Listing_Json>(jsonString);

                //When Deserializing assets:440:2 doesn't deserialize the data correctly and leaves everything as null
                //Model binding problem *probably*
                if (deserializedResult == null)
                {
                    continue;
                }

                var firstAppAssets = deserializedResult.Assets?.FirstOrDefault().Value;
                var firstAssetDetail = firstAppAssets?.FirstOrDefault().Value?.FirstOrDefault().Value;
                var resultAssets = firstAssetDetail?.Descriptions?.Where(x => x.Value != null);
                var isContained = resultAssets?.Any(x => StaticCollections.GoodPaintsStringValues.Contains(x.Value)) ?? false;

                if (isContained)
                {
                    var color = resultAssets?.FirstOrDefault(x => x.Value!.StartsWith("Paint Color"))?.Value;

                    if (color != null)
                    {
                        var listing = new ProductProxy
                        {
                            Name = item.Name,
                            Price = item.SellPrice,
                            ImageUrl = item.AssetDescription?.IconUrl,  // Use null-conditional here as well
                            Quantity = item.SellListings,
                            Color = color
                        };

                        itemCollection.Add(listing);
                    }
                }
            }

            return itemCollection;
        }


        private async Task<string?> GetHttpResponseAsync(string url)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();

            return responseContent;
        }

        private async Task<string> GetHttpResposeAsync(string url)
        {
            string? httpResponse = await GetHttpResponseAsync(url);
            if (httpResponse == null)
            {
                return null!;
            }

            return httpResponse;
        }

        private T? DeserializeFormattedJsonString<T>(string formattedJsonObject) where T : class
        {
            if (string.IsNullOrEmpty(formattedJsonObject))
            {
                throw new JsonSerializationException("Object for Deserialization was null.");
            }

            return JsonConvert.DeserializeObject<T>(formattedJsonObject);
        }

        private string FormatJsonStringForDeserialization(string jsonString)
        {
            string formattedJson = jsonString.Replace("\\r\\n", "\r\n");
            return formattedJson;
        }

        private string FormatListingNameForUrl(string name)
        {
            return HttpUtility.UrlEncode(name);
        }
        #endregion
    }
}
