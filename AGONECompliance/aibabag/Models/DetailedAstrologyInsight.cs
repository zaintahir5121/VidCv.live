namespace aibabag.Models;

public class DetailedAstrologyInsight
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public string FavoriteMovieGenre { get; set; } = string.Empty;
    public string MovieInsights { get; set; } = string.Empty;
    public string EntertainmentPreferences { get; set; } = string.Empty;
    public string CelebrityMatch { get; set; } = string.Empty;

    public string FavoriteCuisine { get; set; } = string.Empty;
    public string FoodInsights { get; set; } = string.Empty;
    public string LuckyFood { get; set; } = string.Empty;
    public string DietaryRecommendations { get; set; } = string.Empty;

    public string FavoriteAnimal { get; set; } = string.Empty;
    public string PetCompatibility { get; set; } = string.Empty;
    public string AnimalInsights { get; set; } = string.Empty;

    public string FriendshipTraits { get; set; } = string.Empty;
    public string IdealFriendType { get; set; } = string.Empty;
    public string SocialInsights { get; set; } = string.Empty;
    public string GroupDynamics { get; set; } = string.Empty;

    public string CareerPath { get; set; } = string.Empty;
    public string FutureOpportunities { get; set; } = string.Empty;
    public string SuccessInsights { get; set; } = string.Empty;
    public string BusinessVentures { get; set; } = string.Empty;
    public string InvestmentAdvice { get; set; } = string.Empty;

    public string PastKarma { get; set; } = string.Empty;
    public string PastLifeInsights { get; set; } = string.Empty;
    public string HistoricalPatterns { get; set; } = string.Empty;

    public string MarriageInsights { get; set; } = string.Empty;
    public string IdealPartnerType { get; set; } = string.Empty;
    public string RomanceAdvice { get; set; } = string.Empty;
    public string LoveMatches { get; set; } = string.Empty;
    public string MarriageCompatibility { get; set; } = string.Empty;

    public string FavoriteGenres { get; set; } = string.Empty;
    public string HobbyRecommendations { get; set; } = string.Empty;
    public string CreativeOutlets { get; set; } = string.Empty;
    public string ArtisticTendencies { get; set; } = string.Empty;

    public string ChildParenting { get; set; } = string.Empty;
    public string IdealChildTraits { get; set; } = string.Empty;
    public string FamilyPlanning { get; set; } = string.Empty;

    public string FamilyInsights { get; set; } = string.Empty;
    public string ParentalRelationships { get; set; } = string.Empty;
    public string SiblingDynamics { get; set; } = string.Empty;
    public string FamilyHarmony { get; set; } = string.Empty;

    public string JobFitness { get; set; } = string.Empty;
    public string OfficeInsights { get; set; } = string.Empty;
    public string WorkStyle { get; set; } = string.Empty;
    public string CareerGrowth { get; set; } = string.Empty;
    public string LeadershipQualities { get; set; } = string.Empty;

    public string BossCompatibility { get; set; } = string.Empty;
    public string ManagementStyle { get; set; } = string.Empty;
    public string LeadershipPath { get; set; } = string.Empty;

    public string ColleagueRelationships { get; set; } = string.Empty;
    public string TeamworkInsights { get; set; } = string.Empty;
    public string NetworkingAbility { get; set; } = string.Empty;

    public string FinancialOutlook { get; set; } = string.Empty;
    public string WealthInsights { get; set; } = string.Empty;
    public string MoneyManagement { get; set; } = string.Empty;
    public string LuckyMonthsForWealth { get; set; } = string.Empty;

    public string HealthInsights { get; set; } = string.Empty;
    public string MentalWellbeing { get; set; } = string.Empty;
    public string ExerciseRecommendations { get; set; } = string.Empty;
    public string VulnerabilityAreas { get; set; } = string.Empty;

    public string TravelInsights { get; set; } = string.Empty;
    public string LuckyDestinations { get; set; } = string.Empty;
    public string AdventureType { get; set; } = string.Empty;

    public string SpiritualPath { get; set; } = string.Empty;
    public string PersonalGrowth { get; set; } = string.Empty;
    public string MeditationAdvice { get; set; } = string.Empty;

    public string Strengths { get; set; } = string.Empty;
    public string Weaknesses { get; set; } = string.Empty;
    public string HiddenTalents { get; set; } = string.Empty;
    public string PersonalityType { get; set; } = string.Empty;

    public string DailyForecast { get; set; } = string.Empty;
    public string WeeklyForecast { get; set; } = string.Empty;
    public string MonthlyForecast { get; set; } = string.Empty;
    public string YearlyForecast { get; set; } = string.Empty;

    public int LoveRating { get; set; }
    public int CareerRating { get; set; }
    public int HealthRating { get; set; }
    public int FinanceRating { get; set; }
    public int FriendshipRating { get; set; }
    public int CreativityRating { get; set; }
    public int LuckRating { get; set; }
}
