using Content.Shared.PDA;
using Content.Shared.Traitor.Uplink;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Traitor.Uplink
{
    /// <summary>
    ///     Contains and controls all items in traitors uplink shop
    /// </summary>
    public sealed class UplinkListingSytem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private readonly Dictionary<string, UplinkListingData> _listings = new();

        public override void Initialize()
        {
            base.Initialize();

            foreach (var item in _prototypeManager.EnumeratePrototypes<UplinkStoreListingPrototype>())
            {
                var newListing = new UplinkListingData(item.ListingName, item.ItemId,
                    item.Price, item.Category, item.Description, item.Icon, item.JobWhitelist);

                RegisterUplinkListing(newListing);
            }
        }

        private void RegisterUplinkListing(UplinkListingData listing)
        {
            if (!ContainsListing(listing))
            {
                _listings.Add(listing.ItemId, listing);
            }
        }

        public bool ContainsListing(UplinkListingData listing)
        {
            return _listings.ContainsKey(listing.ItemId);
        }

        public bool TryGetListing(string itemID, [NotNullWhen(true)] out UplinkListingData? data)
        {
            return _listings.TryGetValue(itemID, out data);
        }

        public IReadOnlyDictionary<string, UplinkListingData> GetListings()
        {
            return _listings;
        }
    }
}
