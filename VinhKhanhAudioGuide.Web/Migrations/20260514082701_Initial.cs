using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinhKhanhAudioGuide.Web.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QrCodeValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentActivity = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentActivityAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthUserAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthUserAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPackages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DefaultPoiPriority = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoiAdminLocationAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoiAdminLocationAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PoiChangeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmittedByUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubmittedByName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LocationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetEntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangeSetJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoiChangeRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tours",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUserActivityLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ActivityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ActivityContext = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsForeground = table.Column<bool>(type: "bit", nullable: false),
                    LoggedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserActivityLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserActivityLog_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAppSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TokenValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastValidatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAppSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAppSessions_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FCMToken = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RegisteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDeviceTokens_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    DetectionRadiusMeters = table.Column<double>(type: "float", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PoiRegistrationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PackageId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoiRegistrationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoiRegistrationRequests_PaymentPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "PaymentPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthUserId = table.Column<int>(type: "int", nullable: true),
                    PackageId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PurchasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastVerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_AuthUserAccounts_AuthUserId",
                        column: x => x.AuthUserId,
                        principalTable: "AuthUserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_PaymentPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "PaymentPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AudioGuides",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AudioUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CloudinaryAudioUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CloudinaryPublicId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TranscriptText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    LocationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GeneratedFromTts = table.Column<bool>(type: "bit", nullable: false),
                    TtsSourceText = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioGuides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudioGuides_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationReviews_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TourLocations",
                columns: table => new
                {
                    TourId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LocationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourLocations", x => new { x.TourId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_TourLocations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TourLocations_Tours_TourId",
                        column: x => x.TourId,
                        principalTable: "Tours",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AudioScriptSegments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AudioGuideId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartTimeSeconds = table.Column<int>(type: "int", nullable: false),
                    EndTimeSeconds = table.Column<int>(type: "int", nullable: false),
                    ScriptText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AudioScriptSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AudioScriptSegments_AudioGuides_AudioGuideId",
                        column: x => x.AudioGuideId,
                        principalTable: "AudioGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListeningHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AudioGuideId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LocationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AudioTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LocationImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AudioDuration = table.Column<int>(type: "int", nullable: false),
                    Progress = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    ListenedSeconds = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    LastListenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListeningHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListeningHistory_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListeningHistory_AudioGuides_AudioGuideId",
                        column: x => x.AudioGuideId,
                        principalTable: "AudioGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListeningHistory_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserActivityLog_DeviceId",
                table: "AppUserActivityLog",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserActivityLog_LoggedAtUtc",
                table: "AppUserActivityLog",
                column: "LoggedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserActivityLog_SessionToken",
                table: "AppUserActivityLog",
                column: "SessionToken");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserActivityLog_UserId",
                table: "AppUserActivityLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserActivityLog_UserId_LoggedAtUtc",
                table: "AppUserActivityLog",
                columns: new[] { "UserId", "LoggedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_CurrentActivityAtUtc",
                table: "AppUsers",
                column: "CurrentActivityAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_DeviceId",
                table: "AppUsers",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_QrCodeValue",
                table: "AppUsers",
                column: "QrCodeValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_Status",
                table: "AppUsers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AudioGuides_LocationId",
                table: "AudioGuides",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AudioScriptSegments_AudioGuideId",
                table: "AudioScriptSegments",
                column: "AudioGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthUserAccounts_Role_IsActive",
                table: "AuthUserAccounts",
                columns: new[] { "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthUserAccounts_Username",
                table: "AuthUserAccounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListeningHistory_AudioGuideId",
                table: "ListeningHistory",
                column: "AudioGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_ListeningHistory_LastListenedAtUtc",
                table: "ListeningHistory",
                column: "LastListenedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ListeningHistory_LocationId",
                table: "ListeningHistory",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ListeningHistory_LocationId_LastListenedAtUtc",
                table: "ListeningHistory",
                columns: new[] { "LocationId", "LastListenedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ListeningHistory_UserId",
                table: "ListeningHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ListeningHistory_UserId_LastListenedAtUtc",
                table: "ListeningHistory",
                columns: new[] { "UserId", "LastListenedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationReviews_CreatedAtUtc",
                table: "LocationReviews",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LocationReviews_LocationId",
                table: "LocationReviews",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationReviews_LocationId_Status_CreatedAtUtc",
                table: "LocationReviews",
                columns: new[] { "LocationId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationReviews_Status",
                table: "LocationReviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CategoryId",
                table: "Locations",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPackages_IsActive",
                table: "PaymentPackages",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPackages_Price",
                table: "PaymentPackages",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_PoiAdminLocationAssignments_LocationId",
                table: "PoiAdminLocationAssignments",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PoiAdminLocationAssignments_Username",
                table: "PoiAdminLocationAssignments",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_PoiAdminLocationAssignments_Username_LocationId",
                table: "PoiAdminLocationAssignments",
                columns: new[] { "Username", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoiChangeRequests_LocationId",
                table: "PoiChangeRequests",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PoiChangeRequests_Status",
                table: "PoiChangeRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PoiChangeRequests_SubmittedAtUtc",
                table: "PoiChangeRequests",
                column: "SubmittedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PoiChangeRequests_SubmittedByUsername",
                table: "PoiChangeRequests",
                column: "SubmittedByUsername");

            migrationBuilder.CreateIndex(
                name: "IX_PoiRegistrationRequests_ExpiresAtUtc",
                table: "PoiRegistrationRequests",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PoiRegistrationRequests_PackageId",
                table: "PoiRegistrationRequests",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PoiRegistrationRequests_Status",
                table: "PoiRegistrationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TourLocations_LocationId",
                table: "TourLocations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAppSessions_DeviceId",
                table: "UserAppSessions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAppSessions_ExpiresAtUtc",
                table: "UserAppSessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserAppSessions_IsActive",
                table: "UserAppSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserAppSessions_TokenValue",
                table: "UserAppSessions",
                column: "TokenValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAppSessions_UserId",
                table: "UserAppSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAppSessions_UserId_DeviceId",
                table: "UserAppSessions",
                columns: new[] { "UserId", "DeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_DeviceId",
                table: "UserDeviceTokens",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_FCMToken",
                table: "UserDeviceTokens",
                column: "FCMToken");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_UserId",
                table: "UserDeviceTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDeviceTokens_UserId_DeviceId",
                table: "UserDeviceTokens",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_AuthUserId",
                table: "UserSubscriptions",
                column: "AuthUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_ExpiresAtUtc",
                table: "UserSubscriptions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PackageId",
                table: "UserSubscriptions",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_Status",
                table: "UserSubscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_Status",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUserActivityLog");

            migrationBuilder.DropTable(
                name: "AudioScriptSegments");

            migrationBuilder.DropTable(
                name: "ListeningHistory");

            migrationBuilder.DropTable(
                name: "LocationReviews");

            migrationBuilder.DropTable(
                name: "PoiAdminLocationAssignments");

            migrationBuilder.DropTable(
                name: "PoiChangeRequests");

            migrationBuilder.DropTable(
                name: "PoiRegistrationRequests");

            migrationBuilder.DropTable(
                name: "TourLocations");

            migrationBuilder.DropTable(
                name: "UserAppSessions");

            migrationBuilder.DropTable(
                name: "UserDeviceTokens");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "AudioGuides");

            migrationBuilder.DropTable(
                name: "Tours");

            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "AuthUserAccounts");

            migrationBuilder.DropTable(
                name: "PaymentPackages");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
