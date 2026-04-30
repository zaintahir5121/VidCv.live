using aibabag.Models;

namespace aibabag.Services;

public interface IDetailedAstrologyService
{
    DetailedAstrologyInsight GenerateDetailedInsights(User user, string zodiacSign);
}

public class DetailedAstrologyService : IDetailedAstrologyService
{
    public DetailedAstrologyInsight GenerateDetailedInsights(User user, string zodiacSign)
    {
        return new DetailedAstrologyInsight
        {
            UserId = user.Id,
            CalculatedAt = DateTime.UtcNow,
            FavoriteMovieGenre = "Adventure and drama",
            MovieInsights = $"As a {zodiacSign}, you enjoy meaningful stories with emotional depth.",
            EntertainmentPreferences = "Balanced mix of fun and thoughtful content.",
            CelebrityMatch = "People with confident and creative personalities.",
            FavoriteCuisine = "International mixed cuisine",
            FoodInsights = "You prefer comfort + quality food together.",
            LuckyFood = "Fresh fruit and colorful meals",
            DietaryRecommendations = "Balanced meals with hydration and moderate sugar.",
            FavoriteAnimal = "Loyal and intelligent companions",
            PetCompatibility = "High with dogs, medium with cats.",
            AnimalInsights = "You connect well with calm but friendly animals.",
            FriendshipTraits = "Loyal, empathetic, practical",
            IdealFriendType = "Supportive, honest, growth-minded",
            SocialInsights = "Small trusted circle works best for you.",
            GroupDynamics = "You naturally mediate conflicts.",
            CareerPath = "Leadership with creativity and communication",
            FutureOpportunities = "Strong chance for growth in next 12 months.",
            SuccessInsights = "Consistency and relationships are your success keys.",
            BusinessVentures = "Service and digital products are favorable.",
            InvestmentAdvice = "Diversify and avoid impulsive decisions.",
            PastKarma = "You often carry responsibility for others.",
            PastLifeInsights = "Themes of guidance and teaching appear repeatedly.",
            HistoricalPatterns = "Major shifts occur every few years with career gains.",
            MarriageInsights = "Stable partnership with shared values suits you best.",
            IdealPartnerType = "Emotionally mature and respectful partner.",
            RomanceAdvice = "Speak openly about expectations early.",
            LoveMatches = "Best matches are signs with emotional stability.",
            MarriageCompatibility = "High when communication is consistent.",
            FavoriteGenres = "Self-growth, mystery, and human stories",
            HobbyRecommendations = "Journaling, travel, reading, design",
            CreativeOutlets = "Writing and visual planning",
            ArtisticTendencies = "Strong aesthetic balance and storytelling instinct.",
            ChildParenting = "Protective but modern parenting style",
            IdealChildTraits = "Curious, kind, disciplined",
            FamilyPlanning = "Plan-based family growth is favorable.",
            FamilyInsights = "Family role is central to your long-term happiness.",
            ParentalRelationships = "Healing and understanding increase with age.",
            SiblingDynamics = "Supportive if boundaries are clear.",
            FamilyHarmony = "Keep regular shared time for better harmony.",
            JobFitness = "Excellent in people + process roles.",
            OfficeInsights = "You perform best with autonomy and clear goals.",
            WorkStyle = "Organized and dependable",
            CareerGrowth = "Strong growth with certifications and networking.",
            LeadershipQualities = "Calm under pressure and fair-minded.",
            BossCompatibility = "Best with transparent and practical managers.",
            ManagementStyle = "Coaching and accountability blend.",
            LeadershipPath = "Team lead -> strategy role progression likely.",
            ColleagueRelationships = "Professional and dependable rapport.",
            TeamworkInsights = "You improve team stability quickly.",
            NetworkingAbility = "Strong in meaningful one-to-one connections.",
            FinancialOutlook = "Positive with budget discipline.",
            WealthInsights = "Steady wealth creation beats rapid risk.",
            MoneyManagement = "Use allocation buckets for goals.",
            LuckyMonthsForWealth = "March, July, November",
            HealthInsights = "Prioritize sleep and stress management.",
            MentalWellbeing = "Routine and movement improve mood strongly.",
            ExerciseRecommendations = "Walking + strength training mix.",
            VulnerabilityAreas = "Stress spikes during heavy workload seasons.",
            TravelInsights = "Travel refreshes your decision-making clarity.",
            LuckyDestinations = "Nature + culture destinations suit you.",
            AdventureType = "Moderate adventure with planned itineraries.",
            SpiritualPath = "Grounded spirituality and reflective practice.",
            PersonalGrowth = "Big growth through service and boundaries.",
            MeditationAdvice = "10-15 minutes daily breath focus.",
            Strengths = "Empathy, consistency, planning, integrity",
            Weaknesses = "Overthinking and overcommitment",
            HiddenTalents = "Mentoring and systems thinking",
            PersonalityType = "Balanced strategist with emotional intelligence",
            DailyForecast = "Today favors communication and completion.",
            WeeklyForecast = "Good week for planning and family decisions.",
            MonthlyForecast = "Progress in career and personal clarity.",
            YearlyForecast = "Year of structured growth and long-term wins.",
            LoveRating = 8,
            CareerRating = 8,
            HealthRating = 7,
            FinanceRating = 8,
            FriendshipRating = 8,
            CreativityRating = 7,
            LuckRating = 7
        };
    }
}
