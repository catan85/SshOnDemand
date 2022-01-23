using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace ApiServer.Infrastructure.Models
{
    public partial class sshondemandContext : DbContext
    {
        public sshondemandContext()
        {
        }

        public sshondemandContext(DbContextOptions<sshondemandContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Client> Clients { get; set; }
        public virtual DbSet<ClientConnection> ClientConnections { get; set; }
        public virtual DbSet<DeveloperAuthorization> DeveloperAuthorizations { get; set; }
        public virtual DbSet<DeviceRequest> DeviceRequests { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseNpgsql("Host=localhost;Database=sshondemand;Username=postgres;Password=postgres");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "Italian_Italy.1252");

            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("clients");

                entity.HasIndex(e => e.ClientName, "unique_client_name")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ClientKey).HasColumnName("client_key");

                entity.Property(e => e.ClientName).HasColumnName("client_name");

                entity.Property(e => e.IsDeveloper).HasColumnName("is_developer");

                entity.Property(e => e.IsDevice).HasColumnName("is_device");
            });

            modelBuilder.Entity<ClientConnection>(entity =>
            {
                entity.HasKey(e => e.ClientId)
                    .HasName("client_connections_pk");

                entity.ToTable("client_connections");

                entity.Property(e => e.ClientId)
                    .ValueGeneratedNever()
                    .HasColumnName("client_id");

                entity.Property(e => e.ConnectionTimestamp).HasColumnName("connection_timestamp");

                entity.Property(e => e.SshForwarding).HasColumnName("ssh_forwarding");

                entity.Property(e => e.SshIp).HasColumnName("ssh_ip");

                entity.Property(e => e.SshPort).HasColumnName("ssh_port");

                entity.Property(e => e.SshUser).HasColumnName("ssh_user");

                entity.Property(e => e.Status).HasColumnName("status");

                entity.HasOne(d => d.Client)
                    .WithOne(p => p.ClientConnection)
                    .HasForeignKey<ClientConnection>(d => d.ClientId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_client_id");
            });

            modelBuilder.Entity<DeveloperAuthorization>(entity =>
            {
                entity.HasKey(e => new { e.DeveloperId, e.DeviceId })
                    .HasName("developer_authorizations_pk");

                entity.ToTable("developer_authorizations");

                entity.Property(e => e.DeveloperId).HasColumnName("developer_id");

                entity.Property(e => e.DeviceId).HasColumnName("device_id");

                entity.HasOne(d => d.Developer)
                    .WithMany(p => p.DeveloperAuthorizationDevelopers)
                    .HasForeignKey(d => d.DeveloperId)
                    .HasConstraintName("fk_developer_id");

                entity.HasOne(d => d.Device)
                    .WithMany(p => p.DeveloperAuthorizationDevices)
                    .HasForeignKey(d => d.DeviceId)
                    .HasConstraintName("fk_device_id");
            });

            modelBuilder.Entity<DeviceRequest>(entity =>
            {
                entity.HasKey(e => e.ClientId)
                    .HasName("device_requests_pk");

                entity.ToTable("device_requests");

                entity.Property(e => e.ClientId)
                    .ValueGeneratedNever()
                    .HasColumnName("client_id");

                entity.Property(e => e.IsRequested).HasColumnName("is_requested");

                entity.Property(e => e.RequestTimestamp).HasColumnName("request_timestamp");

                entity.Property(e => e.RequestedByClientId).HasColumnName("requested_by_client_id");

                entity.HasOne(d => d.Client)
                    .WithOne(p => p.DeviceRequestClient)
                    .HasForeignKey<DeviceRequest>(d => d.ClientId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fk_client_id");

                entity.HasOne(d => d.RequestedByClient)
                    .WithMany(p => p.DeviceRequestRequestedByClients)
                    .HasForeignKey(d => d.RequestedByClientId)
                    .HasConstraintName("fk_requester_client_id");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
