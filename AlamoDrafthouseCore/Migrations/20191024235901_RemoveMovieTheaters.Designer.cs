﻿// <auto-generated />
using System;
using MaguSoft.ComeAndTicket.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace MaguSoft.ComeAndTicket.Core.Migrations
{
    [DbContext(typeof(ComeAndTicketContext))]
    [Migration("20191024235901_RemoveMovieTheaters")]
    partial class RemoveMovieTheaters
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("MaguSoft.ComeAndTicket.Core.Model.Market", b =>
                {
                    b.Property<string>("Url")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Url");

                    b.ToTable("Markets");
                });

            modelBuilder.Entity("MaguSoft.ComeAndTicket.Core.Model.Movie", b =>
                {
                    b.Property<string>("Title")
                        .HasColumnType("text");

                    b.HasKey("Title");

                    b.ToTable("Movies");
                });

            modelBuilder.Entity("MaguSoft.ComeAndTicket.Core.Model.ShowTime", b =>
                {
                    b.Property<string>("TicketsUrl")
                        .HasColumnType("text");

                    b.Property<DateTime?>("Date")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("MovieTitle")
                        .HasColumnType("text");

                    b.Property<int?>("SeatsLeft")
                        .HasColumnType("integer");

                    b.Property<string>("TheaterUrl")
                        .HasColumnType("text");

                    b.Property<int>("TicketsStatus")
                        .HasColumnType("integer");

                    b.HasKey("TicketsUrl");

                    b.HasIndex("MovieTitle");

                    b.HasIndex("TheaterUrl");

                    b.ToTable("ShowTimes");
                });

            modelBuilder.Entity("MaguSoft.ComeAndTicket.Core.Model.Theater", b =>
                {
                    b.Property<string>("Url")
                        .HasColumnType("text");

                    b.Property<string>("MarketUrl")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Url");

                    b.HasIndex("MarketUrl");

                    b.ToTable("Theaters");
                });

            modelBuilder.Entity("MaguSoft.ComeAndTicket.Core.Model.ShowTime", b =>
                {
                    b.HasOne("MaguSoft.ComeAndTicket.Core.Model.Movie", "Movie")
                        .WithMany("ShowTimes")
                        .HasForeignKey("MovieTitle");

                    b.HasOne("MaguSoft.ComeAndTicket.Core.Model.Theater", "Theater")
                        .WithMany("ShowTimes")
                        .HasForeignKey("TheaterUrl");
                });

            modelBuilder.Entity("MaguSoft.ComeAndTicket.Core.Model.Theater", b =>
                {
                    b.HasOne("MaguSoft.ComeAndTicket.Core.Model.Market", "Market")
                        .WithMany("Theaters")
                        .HasForeignKey("MarketUrl");
                });
#pragma warning restore 612, 618
        }
    }
}
