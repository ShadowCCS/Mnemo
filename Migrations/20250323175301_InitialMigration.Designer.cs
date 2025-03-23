﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MnemoProject.Data;

#nullable disable

namespace MnemoProject.Migrations
{
    [DbContext(typeof(LearningPathContext))]
    [Migration("20250323175301_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.3");

            modelBuilder.Entity("MnemoProject.Models.LearningPath", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("LearningPaths");
                });

            modelBuilder.Entity("MnemoProject.Models.Unit", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("LearningPathId")
                        .HasColumnType("TEXT");

                    b.Property<string>("TheoryContent")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("UnitNumber")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("LearningPathId");

                    b.ToTable("Units");
                });

            modelBuilder.Entity("MnemoProject.Models.Unit", b =>
                {
                    b.HasOne("MnemoProject.Models.LearningPath", "LearningPath")
                        .WithMany("Units")
                        .HasForeignKey("LearningPathId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LearningPath");
                });

            modelBuilder.Entity("MnemoProject.Models.LearningPath", b =>
                {
                    b.Navigation("Units");
                });
#pragma warning restore 612, 618
        }
    }
}
