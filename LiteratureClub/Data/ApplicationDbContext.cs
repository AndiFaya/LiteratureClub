using LiteratureClub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace LiteratureClub.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Core entities 
        public DbSet<Campus> Campuses { get; set; }
        public DbSet<CourseCode> CourseCodes { get; set; }
        public DbSet<TextbookCategory> TextbookCategories { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<Bid> Bids { get; set; }
        public DbSet<PickupPoint> PickupPoints { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Receipt> Receipts { get; set; }
        public DbSet<SellerReview> SellerReviews { get; set; }
        public DbSet<PickupPointReview> PickupPointReviews { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<WatchlistItem> WatchlistItems { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<TextbookRequest> TextbookRequests { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<WantedAd> WantedAds { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser 
            builder.Entity<ApplicationUser>(e =>
            {
                e.HasOne(u => u.Campus)
                 .WithMany(c => c.Users)
                 .HasForeignKey(u => u.CampusId)
                 .OnDelete(DeleteBehavior.Restrict);

               
                e.Property(u => u.EarningsBalance)
                 .HasColumnType("decimal(10,2)");
            });

            // Listing 
            builder.Entity<Listing>(e =>
            {
                e.HasOne(l => l.Seller)
                 .WithMany(u => u.Listings)
                 .HasForeignKey(l => l.SellerId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(l => l.Category)
                 .WithMany(c => c.Listings)
                 .HasForeignKey(l => l.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(l => l.CourseCode)
                 .WithMany(cc => cc.Listings)
                 .HasForeignKey(l => l.CourseCodeId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.Property(l => l.Price)
                 .HasColumnType("decimal(10,2)");
            });

            // Bid
            builder.Entity<Bid>(e =>
            {
                e.HasOne(b => b.Listing)
                 .WithMany(l => l.Bids)
                 .HasForeignKey(b => b.ListingId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(b => b.Bidder)
                 .WithMany(u => u.Bids)
                 .HasForeignKey(b => b.BidderId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.Property(b => b.OfferAmount)
                 .HasColumnType("decimal(10,2)");
            });

            // Transaction 
            builder.Entity<Transaction>(e =>
            {
                e.HasOne(t => t.Listing)
                 .WithMany(l => l.Transactions)
                 .HasForeignKey(t => t.ListingId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Buyer)
                 .WithMany(u => u.Purchases)
                 .HasForeignKey(t => t.BuyerId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Seller)
                 .WithMany(u => u.Sales)
                 .HasForeignKey(t => t.SellerId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.PickupPoint)
                 .WithMany(pp => pp.Transactions)
                 .HasForeignKey(t => t.PickupPointId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.Property(t => t.Amount)
                 .HasColumnType("decimal(10,2)");
            });

            // Receipt 
            builder.Entity<Receipt>(e =>
            {
                e.HasOne(r => r.Transaction)
                 .WithOne(t => t.Receipt)
                 .HasForeignKey<Receipt>(r => r.TransactionId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.Property(r => r.AmountPaid)
                 .HasColumnType("decimal(10,2)");
            });

            // SellerReview 
            builder.Entity<SellerReview>(e =>
            {
                e.HasOne(sr => sr.Transaction)
                 .WithMany(t => t.SellerReviews)
                 .HasForeignKey(sr => sr.TransactionId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(sr => sr.Reviewer)
                 .WithMany(u => u.ReviewsGiven)
                 .HasForeignKey(sr => sr.ReviewerId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(sr => sr.Seller)
                 .WithMany(u => u.ReviewsReceived)
                 .HasForeignKey(sr => sr.SellerId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Prevent duplicate reviews per transaction per reviewer
                e.HasIndex(sr => new { sr.TransactionId, sr.ReviewerId })
                 .IsUnique();
            });

            // PickupPointReview 
            builder.Entity<PickupPointReview>(e =>
            {
                e.HasOne(ppr => ppr.PickupPoint)
                 .WithMany(pp => pp.Reviews)
                 .HasForeignKey(ppr => ppr.PickupPointId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ppr => ppr.Reviewer)
                 .WithMany(u => u.PickupPointReviews)
                 .HasForeignKey(ppr => ppr.ReviewerId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Message 
            builder.Entity<Message>(e =>
            {
                e.HasOne(m => m.Transaction)
                 .WithMany(t => t.Messages)
                 .HasForeignKey(m => m.TransactionId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(m => m.Sender)
                 .WithMany(u => u.MessagesSent)
                 .HasForeignKey(m => m.SenderId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(m => m.Receiver)
                 .WithMany(u => u.MessagesReceived)
                 .HasForeignKey(m => m.ReceiverId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // WatchlistItem 
            builder.Entity<WatchlistItem>(e =>
            {
                e.HasOne(w => w.User)
                 .WithMany(u => u.WatchlistItems)
                 .HasForeignKey(w => w.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(w => w.Listing)
                 .WithMany(l => l.WatchlistItems)
                 .HasForeignKey(w => w.ListingId)
                 .OnDelete(DeleteBehavior.Cascade);

                // A user can only watch a given listing once
                e.HasIndex(w => new { w.UserId, w.ListingId })
                 .IsUnique();
            });

            // Report 
            builder.Entity<Report>(e =>
            {
                e.HasOne(r => r.Reporter)
                 .WithMany(u => u.ReportsSubmitted)
                 .HasForeignKey(r => r.ReporterId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.Listing)
                 .WithMany(l => l.Reports)
                 .HasForeignKey(r => r.ListingId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.SellerReview)
                 .WithMany(sr => sr.Reports)
                 .HasForeignKey(r => r.SellerReviewId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.Message)
                 .WithMany(m => m.Reports)
                 .HasForeignKey(r => r.MessageId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(r => r.ReportedUser)
                 .WithMany()
                 .HasForeignKey(r => r.ReportedUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // TextbookRequest 
            builder.Entity<TextbookRequest>(e =>
            {
                e.HasOne(tr => tr.Requester)
                 .WithMany(u => u.TextbookRequests)
                 .HasForeignKey(tr => tr.RequesterId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Donation 
            builder.Entity<Donation>(e =>
            {
                e.HasOne(d => d.Donor)
                 .WithMany(u => u.Donations)
                 .HasForeignKey(d => d.DonorId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.Property(d => d.Amount)
                 .HasColumnType("decimal(10,2)");
            });

            // Announcement
            builder.Entity<Announcement>(e =>
            {
                e.HasOne(a => a.PostedByAdmin)
                 .WithMany()
                 .HasForeignKey(a => a.PostedByAdminId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // PickupPoint 
            builder.Entity<PickupPoint>(e =>
            {
                e.HasOne(pp => pp.Campus)
                 .WithMany(c => c.PickupPoints)
                 .HasForeignKey(pp => pp.CampusId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // CourseCode 
            builder.Entity<CourseCode>(e =>
            {
                e.HasOne(cc => cc.Campus)
                 .WithMany(c => c.CourseCodes)
                 .HasForeignKey(cc => cc.CampusId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}