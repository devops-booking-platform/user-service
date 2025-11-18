using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserService.Common.Constants;
using UserService.Domain.Entities;

namespace UserService.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Username)
                .IsRequired()
                .HasMaxLength(ValidationConstants.MaxStringLength);

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(ValidationConstants.MaxStringLength);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(ValidationConstants.MaxStringLength);

            builder.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(ValidationConstants.MaxStringLength);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(ValidationConstants.MaxStringLength);

            builder.Property(x => x.Address)
                .IsRequired()
                .HasMaxLength(ValidationConstants.MaxStringLength);

            builder.Property(x => x.Role)
                .IsRequired()
                .HasConversion<string>();
        }
    }
}
