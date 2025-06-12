using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedicalTourismApp.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<Tourist> Tourists { get; set; }
    public DbSet<PlanDetails> planDetails { get; set; }
    public DbSet<payment> BankAccounts { get; set; }
    public DbSet<app_doctor> app_Doctors { get; set; }
    public DbSet<calapartment> calapartments { get; set; }
    public DbSet<BookCar> bookCars { get; set; }
    public DbSet<BookFreelance> bookFreelances { get; set; }
    public DbSet<Testimonial> testimonials { get; set; }
    public DbSet<Apartment> apartments { get; set; }
    public DbSet<Hotel> hotels { get; set; }
    public DbSet<Serviceprovider> Serviceproviders { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Avaliabledoctor> Avaliabledoctors { get; set; }
    public DbSet<avaliableFree> AvaliableFree { get; set; }
    public DbSet<Hospital> Hospitals { get; set; }
    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<Freelancer> Freelancers { get; set; }
    public DbSet<Transportation> Transportations { get; set; }
    public DbSet<Guide> Guides { get; set; }
    public DbSet<GuideBooking> GuideBookings { get; set; }
 
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Image> Images { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlanDetails>()
          .Property(p => p.Price)
          .HasPrecision(18, 2);


        modelBuilder.Entity<Review>()
            .HasOne(r => r.Tourist)
            .WithMany(h=>h.Reviews)
            .HasForeignKey(r => r.TouristId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.ServiceProvider)
            .WithMany(v => v.Reviews)
            .HasForeignKey(r => r.ServiceProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Testimonial>()
            .HasOne(r => r.User)
            .WithMany(h => h.testimonials)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<app_doctor>()
            .HasOne(r => r.Doctor)
            .WithMany(h => h.app_Doctors)
            .HasForeignKey(r => r.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<app_doctor>()
            .HasOne(r => r.Tourist)
            .WithMany(h => h.app_Doctors)
            .HasForeignKey(r => r.TouristId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity <calapartment>()
            .HasOne(b => b.Tourist)
            .WithMany(u => u.calapartments)
            .HasForeignKey(b => b.TouristId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<calapartment>()
            .HasOne(b => b.apartment)
            .WithMany(a => a.calapartments)
            .HasForeignKey(b => b.ApartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GuideBooking>()
          .HasOne(r => r.Tourist)
          .WithMany(h => h.guideBookings)
          .HasForeignKey(r => r.TouristId)
          .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BookFreelance>()
          .HasOne(r => r.Tourist)
          .WithMany(h => h.bookFreelances)
          .HasForeignKey(r => r.TouristId)
          .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BookCar>()
        .HasOne(r => r.Tourist)
        .WithMany(h => h.bookCars)
        .HasForeignKey(r => r.TouristId)
        .OnDelete(DeleteBehavior.Restrict);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Image>()
         .HasOne(i => i.ServiceProvider)
         .WithMany(sp => sp.Images)
         .HasForeignKey(i => i.ServiceProviderId);



    }
}