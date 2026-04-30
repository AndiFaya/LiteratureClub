using BookSwap.Models;

namespace BookSwap.ViewModels
{
    public class DashboardViewModel
    {
        // ── Profile summary ────────────────────────────────────────────────
        public string DisplayUsername { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Campus { get; set; } = string.Empty;
        public decimal EarningsBalance { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        // ── Stats ──────────────────────────────────────────────────────────
        public int ActiveListingsCount { get; set; }
        public int TotalSalesCount { get; set; }
        public int TotalPurchasesCount { get; set; }
        public int PendingBidsCount { get; set; }
        public int WatchlistCount { get; set; }
        public int UnreadMessagesCount { get; set; }

        // ── My listings ────────────────────────────────────────────────────
        public List<DashboardListingRow> MyListings { get; set; } = new();

        // ── My purchases ──────────────────────────────────────────────────
        public List<DashboardTransactionRow> MyPurchases { get; set; } = new();

        // ── My sales ──────────────────────────────────────────────────────
        public List<DashboardTransactionRow> MySales { get; set; } = new();

        // ── Watchlist ──────────────────────────────────────────────────────
        public List<ListingCardViewModel> Watchlist { get; set; } = new();

        // ── Incoming bids (on my listings) ────────────────────────────────
        public List<DashboardBidRow> IncomingBids { get; set; } = new();
    }

    public class DashboardListingRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ListingStatus Status { get; set; }
        public int BidCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DashboardTransactionRow
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionStatus Status { get; set; }
        public string OtherPartyUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasReceipt { get; set; }
    }

    public class DashboardBidRow
    {
        public int BidId { get; set; }
        public int ListingId { get; set; }
        public string ListingTitle { get; set; } = string.Empty;
        public string BidderUsername { get; set; } = string.Empty;
        public decimal OfferAmount { get; set; }
        public int Quantity { get; set; }
        public BidStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
