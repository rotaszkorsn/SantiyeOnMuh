﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SantiyeOnMuh.DataAccess.Concrete;

#nullable disable

namespace SantiyeOnMuh.DataAccess.Migrations
{
    [DbContext(typeof(Context))]
    [Migration("20230225223423_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("SantiyeOnMuh.Entity.Santiye", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Ad")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Adres")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Durum")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("Santiyes");
                });

            modelBuilder.Entity("SantiyeOnMuh.Entity.SantiyeKasa", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Aciklama")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("Durum")
                        .HasColumnType("bit");

                    b.Property<decimal>("Gelir")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("Gider")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("ImgUrl")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Kisi")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("No")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("SantiyeId")
                        .HasColumnType("int");

                    b.Property<DateTime>("SistemeGiris")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("SonGuncelleme")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("Tarih")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.HasIndex("SantiyeId");

                    b.ToTable("SantiyesKasa");
                });

            modelBuilder.Entity("SantiyeOnMuh.Entity.SantiyeKasa", b =>
                {
                    b.HasOne("SantiyeOnMuh.Entity.Santiye", "Santiye")
                        .WithMany("SantiyeKasas")
                        .HasForeignKey("SantiyeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Santiye");
                });

            modelBuilder.Entity("SantiyeOnMuh.Entity.Santiye", b =>
                {
                    b.Navigation("SantiyeKasas");
                });
#pragma warning restore 612, 618
        }
    }
}
