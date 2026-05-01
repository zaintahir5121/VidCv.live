using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aibabag.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GoogleId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProfileImageUrl = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    GoogleBirthday = table.Column<DateTime>(type: "TEXT", nullable: true),
                    BirthDateSource = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BirthDateRawText = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PhotoData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    ZodiacSign = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ChineseZodiac = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AstrologyInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PersonalityTraits = table.Column<string>(type: "TEXT", nullable: false),
                    LuckyNumbers = table.Column<string>(type: "TEXT", nullable: false),
                    LuckyColor = table.Column<string>(type: "TEXT", nullable: false),
                    Element = table.Column<string>(type: "TEXT", nullable: false),
                    HealthInsights = table.Column<string>(type: "TEXT", nullable: false),
                    CareerInsights = table.Column<string>(type: "TEXT", nullable: false),
                    LoveInsights = table.Column<string>(type: "TEXT", nullable: false),
                    MonthlyForecast = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AstrologyInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AstrologyInsights_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompatibilityMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TargetZodiacSign = table.Column<string>(type: "TEXT", nullable: false),
                    CompatibilityPercentage = table.Column<int>(type: "INTEGER", nullable: false),
                    CompatibilityDescription = table.Column<string>(type: "TEXT", nullable: false),
                    RelationshipTips = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompatibilityMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompatibilityMatches_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DetailedAstrologyInsights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FavoriteMovieGenre = table.Column<string>(type: "TEXT", nullable: false),
                    MovieInsights = table.Column<string>(type: "TEXT", nullable: false),
                    EntertainmentPreferences = table.Column<string>(type: "TEXT", nullable: false),
                    CelebrityMatch = table.Column<string>(type: "TEXT", nullable: false),
                    FavoriteCuisine = table.Column<string>(type: "TEXT", nullable: false),
                    FoodInsights = table.Column<string>(type: "TEXT", nullable: false),
                    LuckyFood = table.Column<string>(type: "TEXT", nullable: false),
                    DietaryRecommendations = table.Column<string>(type: "TEXT", nullable: false),
                    FavoriteAnimal = table.Column<string>(type: "TEXT", nullable: false),
                    PetCompatibility = table.Column<string>(type: "TEXT", nullable: false),
                    AnimalInsights = table.Column<string>(type: "TEXT", nullable: false),
                    FriendshipTraits = table.Column<string>(type: "TEXT", nullable: false),
                    IdealFriendType = table.Column<string>(type: "TEXT", nullable: false),
                    SocialInsights = table.Column<string>(type: "TEXT", nullable: false),
                    GroupDynamics = table.Column<string>(type: "TEXT", nullable: false),
                    CareerPath = table.Column<string>(type: "TEXT", nullable: false),
                    FutureOpportunities = table.Column<string>(type: "TEXT", nullable: false),
                    SuccessInsights = table.Column<string>(type: "TEXT", nullable: false),
                    BusinessVentures = table.Column<string>(type: "TEXT", nullable: false),
                    InvestmentAdvice = table.Column<string>(type: "TEXT", nullable: false),
                    PastKarma = table.Column<string>(type: "TEXT", nullable: false),
                    PastLifeInsights = table.Column<string>(type: "TEXT", nullable: false),
                    HistoricalPatterns = table.Column<string>(type: "TEXT", nullable: false),
                    MarriageInsights = table.Column<string>(type: "TEXT", nullable: false),
                    IdealPartnerType = table.Column<string>(type: "TEXT", nullable: false),
                    RomanceAdvice = table.Column<string>(type: "TEXT", nullable: false),
                    LoveMatches = table.Column<string>(type: "TEXT", nullable: false),
                    MarriageCompatibility = table.Column<string>(type: "TEXT", nullable: false),
                    FavoriteGenres = table.Column<string>(type: "TEXT", nullable: false),
                    HobbyRecommendations = table.Column<string>(type: "TEXT", nullable: false),
                    CreativeOutlets = table.Column<string>(type: "TEXT", nullable: false),
                    ArtisticTendencies = table.Column<string>(type: "TEXT", nullable: false),
                    ChildParenting = table.Column<string>(type: "TEXT", nullable: false),
                    IdealChildTraits = table.Column<string>(type: "TEXT", nullable: false),
                    FamilyPlanning = table.Column<string>(type: "TEXT", nullable: false),
                    FamilyInsights = table.Column<string>(type: "TEXT", nullable: false),
                    ParentalRelationships = table.Column<string>(type: "TEXT", nullable: false),
                    SiblingDynamics = table.Column<string>(type: "TEXT", nullable: false),
                    FamilyHarmony = table.Column<string>(type: "TEXT", nullable: false),
                    JobFitness = table.Column<string>(type: "TEXT", nullable: false),
                    OfficeInsights = table.Column<string>(type: "TEXT", nullable: false),
                    WorkStyle = table.Column<string>(type: "TEXT", nullable: false),
                    CareerGrowth = table.Column<string>(type: "TEXT", nullable: false),
                    LeadershipQualities = table.Column<string>(type: "TEXT", nullable: false),
                    BossCompatibility = table.Column<string>(type: "TEXT", nullable: false),
                    ManagementStyle = table.Column<string>(type: "TEXT", nullable: false),
                    LeadershipPath = table.Column<string>(type: "TEXT", nullable: false),
                    ColleagueRelationships = table.Column<string>(type: "TEXT", nullable: false),
                    TeamworkInsights = table.Column<string>(type: "TEXT", nullable: false),
                    NetworkingAbility = table.Column<string>(type: "TEXT", nullable: false),
                    FinancialOutlook = table.Column<string>(type: "TEXT", nullable: false),
                    WealthInsights = table.Column<string>(type: "TEXT", nullable: false),
                    MoneyManagement = table.Column<string>(type: "TEXT", nullable: false),
                    LuckyMonthsForWealth = table.Column<string>(type: "TEXT", nullable: false),
                    HealthInsights = table.Column<string>(type: "TEXT", nullable: false),
                    MentalWellbeing = table.Column<string>(type: "TEXT", nullable: false),
                    ExerciseRecommendations = table.Column<string>(type: "TEXT", nullable: false),
                    VulnerabilityAreas = table.Column<string>(type: "TEXT", nullable: false),
                    TravelInsights = table.Column<string>(type: "TEXT", nullable: false),
                    LuckyDestinations = table.Column<string>(type: "TEXT", nullable: false),
                    AdventureType = table.Column<string>(type: "TEXT", nullable: false),
                    SpiritualPath = table.Column<string>(type: "TEXT", nullable: false),
                    PersonalGrowth = table.Column<string>(type: "TEXT", nullable: false),
                    MeditationAdvice = table.Column<string>(type: "TEXT", nullable: false),
                    Strengths = table.Column<string>(type: "TEXT", nullable: false),
                    Weaknesses = table.Column<string>(type: "TEXT", nullable: false),
                    HiddenTalents = table.Column<string>(type: "TEXT", nullable: false),
                    PersonalityType = table.Column<string>(type: "TEXT", nullable: false),
                    DailyForecast = table.Column<string>(type: "TEXT", nullable: false),
                    WeeklyForecast = table.Column<string>(type: "TEXT", nullable: false),
                    MonthlyForecast = table.Column<string>(type: "TEXT", nullable: false),
                    YearlyForecast = table.Column<string>(type: "TEXT", nullable: false),
                    LoveRating = table.Column<int>(type: "INTEGER", nullable: false),
                    CareerRating = table.Column<int>(type: "INTEGER", nullable: false),
                    HealthRating = table.Column<int>(type: "INTEGER", nullable: false),
                    FinanceRating = table.Column<int>(type: "INTEGER", nullable: false),
                    FriendshipRating = table.Column<int>(type: "INTEGER", nullable: false),
                    CreativityRating = table.Column<int>(type: "INTEGER", nullable: false),
                    LuckRating = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetailedAstrologyInsights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetailedAstrologyInsights_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AstrologyInsights_UserId",
                table: "AstrologyInsights",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompatibilityMatches_UserId_TargetZodiacSign_CalculatedAt",
                table: "CompatibilityMatches",
                columns: new[] { "UserId", "TargetZodiacSign", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DetailedAstrologyInsights_UserId",
                table: "DetailedAstrologyInsights",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleId",
                table: "Users",
                column: "GoogleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AstrologyInsights");

            migrationBuilder.DropTable(
                name: "CompatibilityMatches");

            migrationBuilder.DropTable(
                name: "DetailedAstrologyInsights");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
