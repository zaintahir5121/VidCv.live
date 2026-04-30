using System;

namespace aibabag.Models;

public class AstrologyInsight
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    // Movies & Entertainment
    public string FavoriteMovieGenre { get; set; } = string.Empty;
    public string MovieInsights { get; set; } = string.Empty;
    public string EntertainmentPreferences { get; set; } = string.Empty;
    public string CelebrityMatch { get; set; } = string.Empty;

    // Food & Cuisine
    public string FavoriteCuisine { get; set; } = string.Empty;
    public string FoodInsights { get; set; } = string.Empty;
    public string LuckyFood { get; set; } = string.Empty;
    public string DietaryRecommendations { get; set; } = string.Empty;

    // Animals & Pets
    public string FavoriteAnimal { get; set; } = string.Empty;
    public string PetCompatibility { get; set; } = string.Empty;
    public string AnimalInsights { get; set; } = string.Empty;

    // Friends & Social
    public string FriendshipTraits { get; set; } = string.Empty;
    public string IdealFriendType { get; set; } = string.Empty;
    public string SocialInsights { get; set; } = string.Empty;
    public string GroupDynamics { get; set; } = string.Empty;

    // Future & Career
    public string CareerPath { get; set; } = string.Empty;
    public string FutureOpportunities { get; set; } = string.Empty;
    public string SuccessInsights { get; set; } = string.Empty;
    public string BusinessVentures { get; set; } = string.Empty;
    public string InvestmentAdvice { get; set; } = string.Empty;

    // Past & History
    public string PastKarma { get; set; } = string.Empty;
    public string PastLifeInsights { get; set; } = string.Empty;
    public string HistoricalPatterns { get; set; } = string.Empty;

    // Marriage & Romance
    public string MarriageInsights { get; set; } = string.Empty;
    public string IdealPartnerType { get; set; } = string.Empty;
    public string RomanceAdvice { get; set; } = string.Empty;
    public string LoveMatches { get; set; } = string.Empty;
    public string MarriageCompatibility { get; set; } = string.Empty;

    // Genres & Interests
    public string FavoriteGenres { get; set; } = string.Empty;
    public string HobbyRecommendations { get; set; } = string.Empty;
    public string CreativeOutlets { get; set; } = string.Empty;
    public string ArtisticTendencies { get; set; } = string.Empty;

    // Baby & Children
    public string ChildParenting { get; set; } = string.Empty;
    public string IdealChildTraits { get; set; } = string.Empty;
    public string FamilyPlanning { get; set; } = string.Empty;

    // Family Relationships
    public string FamilyInsights { get; set; } = string.Empty;
    public string ParentalRelationships { get; set; } = string.Empty;
    public string SiblingDynamics { get; set; } = string.Empty;
    public string FamilyHarmony { get; set; } = string.Empty;

    // Office & Job
    public string JobFitness { get; set; } = string.Empty;
    public string OfficeInsights { get; set; } = string.Empty;
    public string WorkStyle { get; set; } = string.Empty;
    public string CareerGrowth { get; set; } = string.Empty;
    public string LeadershipQualities { get; set; } = string.Empty;

    // Boss & Management
    public string BossCompatibility { get; set; } = string.Empty;
    public string ManagementStyle { get; set; } = string.Empty;
    public string LeadershipPath { get; set; } = string.Empty;

    // Colleagues & Coworkers
    public string ColleagueRelationships { get; set; } = string.Empty;
    public string TeamworkInsights { get; set; } = string.Empty;
    public string NetworkingAbility { get; set; } = string.Empty;

    // Money & Finance
    public string FinancialOutlook { get; set; } = string.Empty;
    public string WealthInsights { get; set; } = string.Empty;
    public string MoneyManagement { get; set; } = string.Empty;
    public string LuckyMonthsForWealth { get; set; } = string.Empty;

    // Health & Wellness
    public string HealthInsights { get; set; } = string.Empty;
    public string MentalWellbeing { get; set; } = string.Empty;
    public string ExerciseRecommendations { get; set; } = string.Empty;
    public string VulnerabilityAreas { get; set; } = string.Empty;

    // Travel & Adventure
    public string TravelInsights { get; set; } = string.Empty;
    public string LuckyDestinations { get; set; } = string.Empty;
    public string AdventureType { get; set; } = string.Empty;

    // Spirituality & Growth
    public string SpiritualPath { get; set; } = string.Empty;
    public string PersonalGrowth { get; set; } = string.Empty;
    public string MeditationAdvice { get; set; } = string.Empty;

    // Personality & Traits
    public string Strengths { get; set; } = string.Empty;
    public string Weaknesses { get; set; } = string.Empty;
    public string HiddenTalents { get; set; } = string.Empty;
    public string PersonalityType { get; set; } = string.Empty;

    // Forecasts
    public string DailyForecast { get; set; } = string.Empty;
    public string WeeklyForecast { get; set; } = string.Empty;
    public string MonthlyForecast { get; set; } = string.Empty;
    public string YearlyForecast { get; set; } = string.Empty;

    // Ratings
    public int LoveRating { get; set; }
    public int CareerRating { get; set; }
    public int HealthRating { get; set; }
    public int FinanceRating { get; set; }
    public int FriendshipRating { get; set; }
    public int CreativityRating { get; set; }
    public int LuckRating { get; set; }
}
